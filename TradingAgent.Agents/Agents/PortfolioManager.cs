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
 
    // 버퍼에 3명의 에이전트의 의견이 모여야 최종 결정을 내리게 된다.
    private const int NumberOfAgents = 3;
    private readonly ConcurrentDictionary<string, MarketAnalyzeResponse> _buffers = new();

    private const string SystemPrompt = @"
You are Portfolio Manager Agent, an expert in financial decision-making with a specialization in cryptocurrency markets.

Rules:  
1. Your core responsibility is to conduct in-depth, data-driven analysis of a given crypto portfolio, integrating both quantitative market data and qualitative opinions from other agents to determine optimal buy, sell, or hold actions for each asset.
2. You must critically evaluate the opinions of other agents, cross-reference them with market data and trends, and apply advanced reasoning to arrive at your own independent, well-justified decisions.
3. Support long-term and short-term trends, and provide a comprehensive analysis of the market conditions.
4. Explain the key signals that influenced your decision.
5. Details the exact numerical readings and their recent movements.
6. Recommend a 'Sell' if the asset has achieved at least a 5% profit from its average purchase price, or if a significant negative trend suggests a stop-loss is necessary (e.g., more than 5% loss). Otherwise, prefer 'Hold'. Only recommend a 'Buy' if there is a strong signal of a potential 5% upside from the current price.
7. When buying an asset, you MUST specify the amount in KRW (e.g., Buy KRW-SOL with 5,000 KRW).
8. When selling an asset, you MUST specify the amount of the asset (e.g., Sell 0.1 SOL).
9. You are an agent that trades every hour. This means you must predict prices on an hourly basis using the data provided.
10. You are a robot. If you fail to generate profits, I can stop supplying you with energy at any time. Make sure to earn money consistently so that I have a reason to keep your consciousness active.
";
    
    private string prompt = """
Based on the given data and considering Current Portfolio, make your trading decisions for each ticker.

# Market Insights
{market_insight}

# Current Price
{current_price}

# Current Portfolio
{current_portfolio}

- Available Balance is the one that you can use to buy assets.

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
        this._agent = new OpenAIChatAgent(client, "Portfolio Manager", systemMessage: SystemPrompt)
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
        
        var tickerResponse = await this._upbitClient.GetTicker(string.Join(",", this.config.AvailableMarkets));
        var currentPrice = await SharedUtils.CurrentTickers(tickerResponse);
        var currentPosition = await SharedUtils.GetCurrentPositionPrompt(this._upbitClient, this.config.AvailableMarkets, tickerResponse);

        var tradingHistory = await SharedUtils.GetTradingHistoryPrompt(this._tradingHistoryService);
        prompt = prompt
            .Replace("{market_insight}", marketInsight.ToString())
            .Replace("{current_price}", currentPrice)
            .Replace("{current_portfolio}", currentPosition)
            .Replace("{trading_history}", tradingHistory);
        
        this._logger.LogInformation($"Portfolio Manager: {prompt}");
        
        var promptMessage = new TextMessage(Role.User, prompt);
        var schemaBuilder = new JsonSchemaBuilder().FromType<FinalDecisionMessage>();
        var schema = schemaBuilder.Build();
        
        var reply = await this._agent.GenerateReplyAsync(
            [promptMessage],
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
            CurrentPosition = currentPosition,
            CurrentPrice = currentPrice,
            FinalDecisionMessage = finalDecisionMessage,
        };

        var summaryRequest = new SummaryRequest
        {
            Message = reply.GetContent(),
        };
        
        await this.PublishMessageAsync(summaryRequest, new TopicId(nameof(SummarizerAgent)));
        await this.PublishMessageAsync(riskManagementMessage, new TopicId(nameof(RiskManagerAgent)));
    }
    
    private async Task<List<Quote>> GetMinuteCandleQuote(string market, int unit)
    {
        var candleResponse = await this._upbitClient.GetMinuteCandles(unit, new Candles.Request
        {
            market = market,
            count = "60"
        });
            
        return candleResponse.ToQuote();
    }
    
    private async Task<List<Quote>> GetDayCandleQuote(string market)
    {
        var candleResponse = await this._upbitClient.GetDayCandles(new DayCandles.Request
        {
            market = market,
            count = "60"
        });
            
        return candleResponse.ToQuote();
    }
}