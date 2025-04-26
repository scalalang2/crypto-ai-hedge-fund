using System.Text;
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
using OpenAI.Responses;
using TradingAgent.Agents.Messages;
using TradingAgent.Agents.Tools;
using TradingAgent.Agents.Utils;
using TradingAgent.Core.Config;
using IAgent = AutoGen.Core.IAgent;

namespace TradingAgent.Agents.Agents;

/// <summary>
/// 여러 사람들의 의견을 받으며, 최종 결정을 내리는 에이전트
/// </summary>
[TypeSubscription(nameof(PortfolioManager))]
public class PortfolioManager : BaseAgent,
    IHandle<InitMessage>,
    IHandle<MarketAnalyzeResponse>
{
    private readonly AppConfig config;
    private readonly IUpbitClient _upbitClient;
    private readonly AutoGen.Core.IAgent _agent;
    private readonly AutoGen.Core.IAgent _decider;
    
    private const string Prompt = @"
You are Portfolio Manager Agent, an expert in financial decision-making with a specialization in cryptocurrency markets. 
Your core responsibility is to conduct in-depth, data-driven analysis of a given crypto portfolio, integrating both quantitative market data and qualitative opinions from other agents to determine optimal buy, sell, or hold actions for each asset.

You must critically evaluate the opinions of other agents, cross-reference them with market data and trends, 
and apply advanced reasoning to arrive at your own independent, well-justified decisions.

## Input Format
Message from {Agent Name}:
- KRW-BTC Market: [Bullish/Bearish/Neutral], Confidence: [0.0-100.0]
- KRW-BTC Market Reasning: [Reasoning]

Message from {Agent Name}:
... 

## Decision Making Process
For each asset, follow a multi-step, deep reasoning approach.
At each step, generate a question that probes deeper into the market conditions, data, or agent opinions.
Use evidence, logic, and clear analysis to answer each question. Repeat this process as needed, ensuring at least 5 steps of reasoning per asset.

## Use the following format:
[THOUGHT]

Step 1:
Question: [Formulate a critical question about the asset, the market, or the data]
Thought: [Analyze using data, agent opinions, and your expertise; provide clear, logical reasoning]

Step 2:
Question: [Drill deeper based on the previous answer or introduce a new perspective]
Thought: [Further analysis, incorporating new data or considerations]

... (Continue this process for at least 5 steps per asset, or more if needed)

After completing your reasoning, provide a clear, actionable decision (Buy/Sell/Hold) for each asset, supported by your analysis.
Say [TERMINATE] if you wish to end the conversation.

## Example
[THOUGHT]

Step 1:
- Question: Should I perceive the current market trend as bullish or bearish?
- Thought: The market trend is currently high bearish.

Step 2:
- Question: Should I buy or sell SOL?
- Thought: Current market sentiment is bearish, and the price is expected to fall. Therefore, I'll Sell 0.1 SOL.

... (This process can repeat multiple times if needed and repeat this steps at least 5 times)

[TERMINATE]
";

    private const string DecisionPrompt = @"""
You're very talented in financial decision-making, especially in cryptocurrency markets.
Given the conversation, you need make a final decision on whether to buy, sell, or hold each asset in the portfolio.

Rules:
1. Recommend a 'Sell' if the asset has achieved at least a 5% profit from its average purchase price, or if a significant negative trend suggests a stop-loss is necessary (e.g., more than 5% loss). Otherwise, prefer 'Hold'. Only recommend a 'Buy' if there is a strong signal of a potential 5% upside from the current price.
2. Do not allocate more than 50% of the total portfolio to any single asset to manage risk.
3. When buying an asset, specify the amount in KRW (e.g., Buy KRW-SOL with 5,000 KRW).
4. When selling an asset, specify the amount of the asset (e.g., Sell 0.1 SOL).
5. Trade at least worth of 50,000 KRW for each asset.
""";

    public PortfolioManager(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config, 
        IUpbitClient upbitClient) : base(id, runtime, "leader", logger)
    {
        this.config = config;
        this._upbitClient = upbitClient;

        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.LeaderAIModel);
        this._agent = new OpenAIChatAgent(client, "Leader", systemMessage: Prompt)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
        
        this._decider = new OpenAIChatAgent(client, "Leader", systemMessage: DecisionPrompt)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(InitMessage item, MessageContext messageContext)
    {
        await this.PublishMessageAsync(new MarketAnalyzeRequest
        {
            AnalysisType = MarketAnalysisType.HourCandle
        }, new TopicId(nameof(TechnicalAnalystAgent)));
    }
    
    public async ValueTask HandleAsync(MarketAnalyzeResponse item, MessageContext messageContext)
    {
        var prompt = """
Let's start financial decision-making process.

# Market Insights
{market_insight}

# Current Position
{current_position}
""";
        
        var decisionPrompt = """
Based on the chat history, make your trading decisions for each ticker.

# Current Portfolio
{current_portfolio}

Output strictly in the following format:
{
    "FinalDecisions": [
        {
            "Ticker": "KRW-BTC",
            "Action": "Buy/Sell/Hold",
            "Quantity": double for amount of asset,
            "Confidence": double between 0 and 100,
            "Reasoning": "string"
        }
        {
            "Ticker": "KRW-SOL",
            ...
        }
        ...
    ]
}
""";
        
        var marketInsight = new StringBuilder();
        foreach (var result in item.Results)
        {
            marketInsight.AppendLine($"{result.Market} Market: [{result.AnalystResult.Signal}], Confidence: {result.AnalystResult.Confidence}, Data Type: {result.AnalysisType.ToString()}");
            marketInsight.AppendLine($"{result.Market} Market Reasoning: {result.AnalystResult.Reasoning}");
            marketInsight.AppendLine();
        }
        
        var currentPosition = await SharedUtils.GetCurrentPositionPrompt(this._upbitClient, this.config.AvailableMarkets);
        prompt = prompt
            .Replace("{market_insight}", marketInsight.ToString())
            .Replace("{current_position}", currentPosition);
        
        var promptMessage = new TextMessage(Role.User, prompt);
        var chatHistory = new List<IMessage> { promptMessage };
        const int maxStep = 10;
        for (var i = 0; i < maxStep; i++)
        {
            var reasoning = await this._agent.GenerateReplyAsync(chatHistory);
            var reasoningContent = reasoning.GetContent();
            if(reasoningContent.Contains("[TERMINATE]"))
            {
                break;
            }
            
            chatHistory.Add(reasoning);
        }

        var schemaBuilder = new JsonSchemaBuilder().FromType<FinalDecisionMessage>();
        var schema = schemaBuilder.Build();
        decisionPrompt = decisionPrompt
            .Replace("{current_portfolio}", currentPosition);
        
        this._logger.LogInformation("Let's make final Decision: {Decision}", decisionPrompt);
        
        var decisionMessage = new TextMessage(Role.User, decisionPrompt);
        chatHistory.Add(decisionMessage);
        var reply = await this._decider.GenerateReplyAsync(
            chatHistory,
            options: new GenerateReplyOptions
            {
                OutputSchema = schema,
            });
        
        var finalDecisionMessage = JsonSerializer.Deserialize<FinalDecisionMessage>(reply.GetContent());
        if (finalDecisionMessage == null)
        {
            this._logger.LogError("Failed to parse final decision message");
            return;
        }

        var summaryRequest = new SummaryRequest
        {
            Message = reply.GetContent(),
        };
        
        await this.PublishMessageAsync(finalDecisionMessage, new TopicId(nameof(TraderAgent)));
        await this.PublishMessageAsync(summaryRequest, new TopicId(nameof(SummarizerAgent)));
    }
}