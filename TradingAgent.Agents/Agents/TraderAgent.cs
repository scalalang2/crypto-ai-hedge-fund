using System.Text;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Discord.WebSocket;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
using TradingAgent.Agents.Messages;
using TradingAgent.Agents.Services;
using TradingAgent.Agents.Tools;
using TradingAgent.Agents.Utils;
using TradingAgent.Core.Config;

namespace TradingAgent.Agents.Agents;

/// <summary>
/// 리더의 메시지를 해석하고 실제 트레이딩을 수행하는 에이전트
/// </summary>
[TypeSubscription(nameof(TraderAgent))]
public class TraderAgent : BaseAgent, IHandle<TradeRequest>
{
    private readonly AppConfig config;
    private readonly AutoGen.Core.IAgent actor;
    private readonly Dictionary<string, Func<string, Task<string>>> traderFunctionMap;
    private readonly IUpbitClient _upbitClient;
    private readonly IMessageSender _messageSender;

    private const string Prompt = @"
You are a trader agent, you need to serve as a tool executor.
Your fund manager will send you a message and you need to decide which tool to invoke.

You can invoke the following tools:
{tools}

## Important Constraints
When using BuyCoin/SellCoin, enter the amount of money you want to use.
For example, if SOL costs 50,000 KRW and you want to buy 0.2 SOL, you should enter 10,000 as the amount.
";
    
    public TraderAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        FunctionTools tools,
        AppConfig config, 
        IUpbitClient upbitClient, 
        IMessageSender messageSender) : base(id, runtime, "trader", logger)
    {
        this.config = config;
        this._upbitClient = upbitClient;
        this._messageSender = messageSender;
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.LeaderAIModel);
        
        this.traderFunctionMap = new Dictionary<string, Func<string, Task<string>>>
        {
            { nameof(tools.BuyCoin), tools.BuyCoinWrapper },
            { nameof(tools.SellCoin), tools.SellCoinWrapper },
            { nameof(tools.HoldCoin), tools.HoldCoinWrapper },
        };
        
        var traderPrompt = Prompt.Replace("{tools}", string.Join(", ", this.traderFunctionMap.Keys));
        this.actor = new OpenAIChatAgent(client, "trader", systemMessage: traderPrompt)
            .RegisterMessageConnector()
            .RegisterMiddleware(new FunctionCallMiddleware(
                functions: [
                    tools.BuyCoinFunctionContract,
                    tools.SellCoinFunctionContract,
                    tools.HoldCoinFunctionContract,
                ],
                functionMap: this.traderFunctionMap))
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(TradeRequest item, MessageContext messageContext)
    {
        var prompt = @"""
Let's analyze the message from the leader and decide what to do.

# Current Position
{current_position}

# Message from the Leader Agent
{message}
""";

        try
        {
            var currentPosition =
                await SharedUtils.GetCurrentPositionPrompt(this._upbitClient, this.config.AvailableMarkets);
            prompt = prompt
                .Replace("{current_position}", currentPosition)
                .Replace("{message}", item.Message);

            var message = new TextMessage(Role.User, prompt);
            var response = await this.actor.GenerateReplyAsync(messages: [message]);
            var result = response.GetContent();
            if (result == null)
            {
                throw new InvalidOperationException("Failed to get a response from the actor.");
            }

            this._logger.LogInformation(result);
        }
        catch (Exception e)
        {
            var discordMessage = $"Error occurred in TraderAgent: {e.Message}";
            this._logger.LogError(e, discordMessage);
            await this._messageSender.SendMessage(discordMessage);
        }
    }
}