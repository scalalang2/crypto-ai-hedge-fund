using System.Diagnostics;
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
public class TraderAgent : BaseAgent, IHandle<FinalDecisionMessage>
{
    private readonly AppConfig config;
    private readonly Dictionary<string, Func<string, Task<string>>> traderFunctionMap;
    private readonly IUpbitClient _upbitClient;
    private readonly IMessageSender _messageSender;

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
    }

    public async ValueTask HandleAsync(FinalDecisionMessage item, MessageContext messageContext)
    {
        try
        {
            foreach (var decision in item.FinalDecisions)
            {
                if (decision.Action == "Hold")
                {
                    continue;
                }

                await Task.Delay(1000); // rest to avoid rate limit
                await this.PlaceOrder(decision);
            }
        } catch (Exception ex)
        {
            await this._messageSender.SendMessage($"**Trading Error** \n\n {ex.Message}");
            this._logger.LogError(ex, "Error handling FinalDecisionMessage {@Request}", item.FinalDecisions);
        }
    }

    public async Task<PlaceOrder.Response> PlaceOrder(FinalDecision decision)
    {
        const string buy = "Buy";
        const string sell = "Sell";
        
        if(decision.Action != buy && decision.Action != sell)
        {
            throw new ArgumentException($"Invalid action. Only 'buy' and 'sell' are allowed. but {decision.Action} was given");
        }
        
        var quantity = string.Format("{0:F8}", decision.Quantity);
        var ordType = decision.Action == buy ? "price" : "market";
        var side = decision.Action == buy ? "bid" : "ask";
        
        var request = new PlaceOrder.Request
        {
            market = decision.Ticker,
            side = side,
            ord_type = ordType
        };
        
        if(decision.Action == buy)
        {
            request.price = quantity;
        }
        else
        {
            request.volume = quantity;
        }
        
        var response = await this._upbitClient.PlaceOrder(request);
        return response;
    }
}