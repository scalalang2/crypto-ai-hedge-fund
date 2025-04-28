using System.Collections.Concurrent;
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
using Skender.Stock.Indicators;
using TradingAgent.Agents.Extensions;
using TradingAgent.Agents.Messages;
using TradingAgent.Agents.Services;
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
    private readonly ITradingHistoryService _tradingHistoryService;
    private readonly AutoGen.Core.IAgent _agent;
    private readonly AutoGen.Core.IAgent _decider;
 
    // 버퍼에 3명의 에이전트의 의견이 모여야 최종 결정을 내리게 된다.
    private const int NumberOfAgents = 3;
    private readonly ConcurrentDictionary<string, MarketAnalyzeResponse> _buffers = new();

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
Question: [Formulate a critical question about the all assets, all markets, or the data]
Thought: [Analyze using data, agent opinions, and your expertise; provide clear, logical reasoning]

Step 2:
Question: [Drill deeper based on the previous answer or introduce a new perspective]
Thought: [Further analysis, incorporating new data or considerations]

... (This process can repeat multiple times)

After completing your reasoning, provide a clear, actionable decision (Buy/Sell/Hold) for each asset, supported by your analysis.
Say [TERMINATE] if you wish to end the conversation.

## Example
[THOUGHT]

Step 1:
- Question: How does the current bearish sentiment in KRW-SOL affect the decision to hold or sell?
- Thought: KRW-SOL shows signs of continued bearish sentiment with price closing below lower Bollinger Band and RSI not yet oversold at 42.51. The MACD indicates bearish continuation, suggesting it's prudent to consider selling to minimize losses.

Step 2:
- Question: Is the current KRW-BTC market condition indicative of a continued decline, and how does this affect my decision to hold or sell?
- Thought: The current KRW-BTC market is bearish with substantial confidence (85). The closing price is significantly lower, and indicators such as MACD and RSI show weakening momentum and neutral, but not oversold. The fact that BTC is below the lower Bollinger Band while OBV shows resistance to this decline suggests a sell-off with minimal buyer interest.

... (This process can repeat multiple times if needed and repeat this steps at least 5 times)

[TERMINATE]
";

    private const string DecisionPrompt = @"""
You're very talented in financial decision-making, especially in cryptocurrency markets.
Given the conversation, you need make a final decision on whether to buy, sell, or hold each asset in the portfolio.

Rules:
1. Explain the key signals that influenced your decision.
2. Details the exact numerical readings and their recent movements.
3. Recommend a 'Sell' if the asset has achieved at least a 5% profit from its average purchase price, or if a significant negative trend suggests a stop-loss is necessary (e.g., more than 5% loss). Otherwise, prefer 'Hold'. Only recommend a 'Buy' if there is a strong signal of a potential 5% upside from the current price.
4. When buying an asset, you MUST specify the amount in KRW (e.g., Buy KRW-SOL with 5,000 KRW).
5. When selling an asset, you MUST specify the amount of the asset (e.g., Sell 0.1 SOL).
""";
    
    private string prompt = """
Let's start financial decision-making process.

# Market Insights
{market_insight}

# Current Position
{current_position}
""";
        
    private string decisionPrompt = """
Based on the chat history, make your trading decisions for each ticker.

# Current Portfolio
{current_portfolio}

# Current Price
{current_price}

# TradingHistory
{trading_history}

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

    public PortfolioManager(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config, 
        IUpbitClient upbitClient, ITradingHistoryService tradingHistoryService) : base(id, runtime, "portfoliomanager", logger)
    {
        this.config = config;
        this._upbitClient = upbitClient;
        _tradingHistoryService = tradingHistoryService;

        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.LeaderAIModel);
        this._agent = new OpenAIChatAgent(client, "Portfolio Manager", systemMessage: Prompt)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
        
        this._decider = new OpenAIChatAgent(client, "Portfolio Manager", systemMessage: DecisionPrompt)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(InitMessage item, MessageContext messageContext)
    {
        this._buffers.Clear();

        var request = new MarketAnalyzeRequest();
        foreach (var market in this.config.AvailableMarkets)
        {
            var marketData = new MarketData
            {
                QuoteType = QuoteType.HourCandle,
                Ticker = market,
                Quotes = await this.GetMinuteCandleQuote(market, 60)
            };

            request.MarketDataList.Add(marketData);
        }

        await this.PublishMessageAsync(request, new TopicId(nameof(TechnicalAnalystAgent)));
        await this.PublishMessageAsync(request, new TopicId(nameof(GeorgeLaneAgent)));
        await this.PublishMessageAsync(request, new TopicId(nameof(HosodaGoichiAgent)));
        // await this.PublishMessageAsync(new SentimentAnalyzeRequest(), new TopicId(nameof(SentimentAgent)));
    }
    
    public async ValueTask HandleAsync(MarketAnalyzeResponse item, MessageContext messageContext)
    {
        if (this._buffers.TryAdd(messageContext.Sender.ToString()!, item) == false)
        {
            throw new Exception($"Duplicated message from sender: {messageContext.Sender.ToString()}");
        }
        
        if (this._buffers.Count < NumberOfAgents)
            return;

        var marketInsight = new StringBuilder();
        foreach (var (agentName, response) in this._buffers)
        {
            marketInsight.AppendLine($"Message from {agentName}");
            foreach (var market in response.MarketAnalysis)
            {
                marketInsight.AppendLine($"Ticker: {market.Market} [{market.AnalystResult.Signal}], [Confidence: {market.AnalystResult.Confidence}], [Reasoning: {market.AnalystResult.Reasoning}]");
            }

            if (!string.IsNullOrEmpty(response.OverallAnalysis.Reasoning))
            {
                marketInsight.AppendLine();
                marketInsight.AppendLine($"Overall Analysis: {response.OverallAnalysis.Signal}, [Confidence: {response.OverallAnalysis.Confidence}], [Reasoning: {response.OverallAnalysis.Reasoning}]");
            }
            marketInsight.AppendLine();
        }
        
        var currentPosition = await SharedUtils.GetCurrentPositionPrompt(this._upbitClient, this.config.AvailableMarkets);
        var currentPrice = await SharedUtils.CurrentTickers(this._upbitClient, this.config.AvailableMarkets);
        prompt = prompt
            .Replace("{market_insight}", marketInsight.ToString())
            .Replace("{current_price}", currentPrice)
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

        var tradingHistory = await SharedUtils.GetTradingHistoryPrompt(this._tradingHistoryService);
        var schemaBuilder = new JsonSchemaBuilder().FromType<FinalDecisionMessage>();
        var schema = schemaBuilder.Build();
        decisionPrompt = decisionPrompt
            .Replace("{current_portfolio}", currentPosition)
            .Replace("{trading_history}", tradingHistory);
        
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
        
        var riskManagementMessage = new RiskManagementMessage
        {
            CurrentPortfolio = currentPosition,
            CurrentPrice = currentPrice,
            FinalDecisionMessage = finalDecisionMessage,
        };

        await this.PublishMessageAsync(riskManagementMessage, new TopicId(nameof(RiskManagerAgent)));
    }
    
    private async Task<List<Quote>> GetMinuteCandleQuote(string market, int unit)
    {
        var candleResponse = await this._upbitClient.GetMinuteCandles(unit, new Candles.Request
        {
            market = market,
            count = "150"
        });
            
        return candleResponse.ToQuote();
    }
    
    private async Task<List<Quote>> GetDayCandleQuote(string market)
    {
        var candleResponse = await this._upbitClient.GetDayCandles(new DayCandles.Request
        {
            market = market,
            count = "150"
        });
            
        return candleResponse.ToQuote();
    }
}