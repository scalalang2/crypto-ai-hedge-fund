using System.Text.Json;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Json.Schema;
using Json.Schema.Generation;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
using TradingAgent.Agents.Messages;
using TradingAgent.Core.Config;

namespace TradingAgent.Agents.Agents;

/// <summary>
/// 리스크 매니저 에이전트
/// 현재 보유 포지션과 결정에 대한 리스크를 관리하는 에이전트이다
/// </summary>
[TypeSubscription(nameof(RiskManagerAgent))]
public class RiskManagerAgent : BaseAgent, IHandle<RiskManagementMessage>
{
    private readonly AppConfig config;
    private readonly AutoGen.Core.IAgent _agent;

    private const string Prompt = @"
You are a Risk Manager AI agent responsible for evaluating a proposed list of FinalDecision objects and ensuring all trades comply with portfolio constraints and API requirements.

Input format:
A FinalDecisionMessage object with a list of FinalDecision entries. Each FinalDecision has:
- Ticker: string, the asset symbol.
- Action: string, one of “Buy”, “Sell”, or “Hold”.
- Quantity: double, amount in KRW for buys or asset units for sells.
- Confidence: double, 0–100 score.
- Reasoning: string, explanation of the initial trading decision.

Rules:
1. Your final decision must includes the PortfolioManager's opinions. 
2. Evaluate each FinalDecision against the current portfolio state (e.g., existing positions, KRW balance, exposure limits).
3. Enforce minimum trade sizes:
   • Buys must be ≥ 20,000 KRW.  
   • Sells must liquidate at least 20,000 KRW worth of the asset.  
   • If a proposed trade is below the threshold, either adjust Quantity upward to the minimum (if KRW/position allows) or remove the trade.
4. Do not allocate more than 50% of the total portfolio to any single asset to manage risk.
5. Avoid recommending any trade that would breach risk limits (e.g., position size, sector concentration) or portfolio margin constraints.
";
    
    public RiskManagerAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config) : base(id, runtime, "Risk Manager", logger)
    {
        this.config = config;
        
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.LeaderAIModel);
        this._agent = new OpenAIChatAgent(client, "Risk Manager", systemMessage: Prompt)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(RiskManagementMessage item, MessageContext messageContext)
    {
        var prompt = @"""
# Current Position
{current_position}

# Current Price
{current_price}

# Final Decision Message from PortfolioManager
{final_decision_message}

Based on the final decision, evaluate the proposed trades against the current portfolio state and risk limits.
Let's think step by step 

Output strictly in the following format:
{
 ""FinalDecisions"": [
     {
         ""Ticker"": ""KRW-BTC"",
         ""Action"": ""Buy/Sell/Hold"",
         ""Quantity"": double for amount of asset,
         ""Confidence"": double between 0 and 100,
         ""Reasoning"": ""string""
     }
     {
         ""Ticker"": ""KRW-SOL"",
         ...
     }
     ...
 ]
}
""";
        var jsonString = JsonSerializer.Serialize(item.FinalDecisionMessage);

        prompt = prompt
            .Replace("{current_position}", item.CurrentPosition)
            .Replace("{current_price}", item.CurrentPrice)
            .Replace("{final_decision_message}", jsonString);

        var message = new TextMessage(Role.User, prompt);
        var reply = await this._agent.GenerateReplyAsync(
            messages: [message],
            options: new GenerateReplyOptions
            {
                OutputSchema = new JsonSchemaBuilder().FromType<FinalDecisionMessage>().Build(),
            });
                
        var finalDecisionMessage = JsonSerializer.Deserialize<FinalDecisionMessage>(reply.GetContent());
        if (finalDecisionMessage == null)
        {
            throw new InvalidOperationException("Failed to deserialize the final decision message.");
        }
        
        var summaryRequest = new SummaryRequest
        {
            Message = reply.GetContent(),
        };
        
        await this.PublishMessageAsync(finalDecisionMessage, new TopicId(nameof(TraderAgent)));
        await this.PublishMessageAsync(summaryRequest, new TopicId(nameof(SummarizerAgent)));
    }
}