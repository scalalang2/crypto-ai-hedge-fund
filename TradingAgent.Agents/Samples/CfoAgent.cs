using System.Globalization;
using System.Text.Json;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Discord.WebSocket;
using Json.Schema;
using Json.Schema.Generation;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using TradingAgent.Agents.Config;
using TradingAgent.Agents.Messages;
using TradingAgent.Agents.Tools;
using TradingAgent.Core.Extensions;
using IAgent = AutoGen.Core.IAgent;

namespace TradingAgent.Agents;

[TypeSubscription(nameof(CfoAgent))]
public class CfoAgent : 
    BaseAgent,
    IHandle<InitMessage>,
    IHandle<AnalystSummaryResponse>
{
    private readonly IAgent agent;
    private readonly IUpbitClient upbitClient;
    private readonly DiscordSocketClient discordClient;
    private readonly LLMConfiguration config;

    public CfoAgent(
        AgentId id,
        DiscordSocketClient discordSocketClient,
        IUpbitClient upbitClient,
        IAgentRuntime runtime,
        ILogger<CfoAgent> logger,
        IOptions<LLMConfiguration> config, DiscordSocketClient discordClient) : base(id, runtime, "Trading Analysis Agent", logger)
    {
        this.upbitClient = upbitClient;
        this.discordClient = discordClient;
        this.config = config.Value;
        var client = new OpenAIClient(config.Value.OpenAIApiKey).GetChatClient(config.Value.Model);
        var systemMessage = @"
You are a professional CFO agent responsible for managing the user's assets. 
Your primary role is to analyze the user's current financial status, investment portfolio,

Requirements:
1. Data: My Portfolio and 30-minute candlestick data (Open, High, Low, Close, Volume)
2. Strategy: think yourself
3. Risk Management: 1:2 Risk/Reward ratio, 1% account risk limit 
";
        
        this.agent = new OpenAIChatAgent(
            chatClient: client,
            name: "cfo agent",
            systemMessage: systemMessage)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }
    
    public async ValueTask HandleAsync(InitMessage item, MessageContext messageContext)
    {
        _logger.LogInformation("CfoAgent received InitMessage: {Market}", item.Market);

        var request = new AnalystSummaryRequest
        {
            Market = item.Market,
        };

        await this.PublishMessageAsync(request, new TopicId(nameof(TradingAnalystAgent)));
    }

    public async ValueTask HandleAsync(AnalystSummaryResponse item, MessageContext messageContext)
    {
        var request = new Chance.Request();
        request.market = item.MarketName;
        
        var response = new AgentFinalDecision();
        
        var chance = await this.upbitClient.GetChance(request);
        var message = $"{chance.GenerateSchemaPrompt("Portfolio")}\n";
        message += $"{chance.GenerateDataPrompt("Portfolio")}\n";
        message += $"{item.GenerateSchemaPrompt("Analyst Summary")}\n";
        message += $"{item.GenerateDataPrompt("Analyst Summary")}\n";
        message += "Please make the final decision.\n";
        message += "You should respond with following format\n";
        message += $"{response.GenerateSchemaPrompt("AgentFinalDecision")}";
        
        var userMessage = new TextMessage(Role.User, message);
        var reply = await this.agent.GenerateReplyAsync(
            messages: [userMessage],
            options: new GenerateReplyOptions
            {
                OutputSchema = response.GetSchema(),
            });
        
        var finalDecision = JsonSerializer.Deserialize<AgentFinalDecision>(reply.GetContent());
        this.ProcessFinalDecision(item.MarketName, finalDecision!);
    }

    private async Task ProcessFinalDecision(string market, AgentFinalDecision finalDecision)
    {
        switch (finalDecision.DecisionType)
        {
            case "BUY":
            {
                var request = new PlaceOrder.Request
                {
                    market = market,
                    side = "bid",
                    price = finalDecision.Price.ToString(),
                    ord_type = "price"
                };
                var orderPlaced = await this.upbitClient.PlaceOrder(request);
                await this.SendOrderPlacedMessage(finalDecision, orderPlaced);
            }
                break;
            case "SELL":
            {
                var request = new PlaceOrder.Request
                {
                    market = market,
                    side = "ask",
                    volume = finalDecision.Volume.ToString(),
                    ord_type = "market"
                };
                var orderPlaced = await this.upbitClient.PlaceOrder(request);
                await this.SendOrderPlacedMessage(finalDecision, orderPlaced);
            }
                break;
            default:
            {
                if (discordClient.GetChannel(this.config.DiscordChannelId) is SocketTextChannel channel)
                {
                    var message = "**[Do Nothing]**\n";
                    message += $"{finalDecision.Reason}\n";
                    await channel.SendMessageAsync(message);
                }
                else
                {
                    _logger.LogError("Channel not found");
                }
            }
                break;
        }
    }

    private async Task SendOrderPlacedMessage(AgentFinalDecision finalDecision, PlaceOrder.Response orderPlaced)
    {
        var discordMessage = $"""
                              **[Order Placed]**
                              {finalDecision}
                              Order ID: {orderPlaced.uuid}
                              Market: {orderPlaced.market}
                              Created At: {orderPlaced.created_at}
                              Volume: {orderPlaced.volume}
                              """;
        if (discordClient.GetChannel(this.config.DiscordChannelId) is SocketTextChannel channel)
        {
            await channel.SendMessageAsync(discordMessage);
        }
        else
        {
            _logger.LogError("Channel not found");
        }
    }
}