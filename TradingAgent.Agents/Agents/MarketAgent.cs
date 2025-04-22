using System.Text;
using System.Text.Json;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Json.Schema;
using Json.Schema.Generation;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using Skender.Stock.Indicators;
using TradingAgent.Agents.Extensions;
using TradingAgent.Agents.Messages;
using TradingAgent.Agents.Tools;
using TradingAgent.Core.Config;

namespace TradingAgent.Agents.Agents;

/// <summary>
/// 시장 분석 에이전트
/// </summary>
[TypeSubscription(nameof(MarketAgent))]
public class MarketAgent : 
    BaseAgent,
    IHandle<MarketAnalyzeRequest>
{
    private readonly AppConfig _config;
    private readonly AutoGen.Core.IAgent _agent;
    private IUpbitClient _upbitClient;

    private const string Prompt = """
You're a technical market analyst agent.
You will receive market & indicator data for a market.

Your task is to analyze the data thoroughly and rank the markets in order of how bullish they currently are, from most to least bullish.

Respond in the following format:
- Cofidence is a number between 0 and 1, where 1 means you are very confident in your analysis.
- Sentiment is either High Bullish, Bullish, Neutral, Bearish, or High Bearish
- Analysis is a short description of your analysis, including any relevant indicators or patterns you observed.

## Example 1
{
    "Sentiment": "High Bullish",
    "Confidence": 0.8,
    "Analysis": "The market is showing strong bullish momentum with increasing volume and positive sentiment."
}

## Example 2
{
    "Sentiment": "Bearish",
    "Confidence": 0.6,
    "Analysis": "The market is showing bearish momentum with decreasing volume and negative sentiment."
}
""";
    
    public MarketAgent(
        IUpbitClient upbitClient,
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config) : base(id, runtime, "market agent", logger)
    {
        this._upbitClient = upbitClient;
        this._config = config;
        
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.WorkerAIModel);
        this._agent = new OpenAIChatAgent(
                chatClient: client, 
                name: "MarketAgent", 
                systemMessage: Prompt)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(MarketAnalyzeRequest item, MessageContext messageContext)
    {
        var response = new MarketAnalyzeResponse();
        var schemaBuilder = new JsonSchemaBuilder().FromType<MarketAnalysisResult>();
        var schema = schemaBuilder.Build();
        var sb = new StringBuilder();
        
        foreach (var market in this._config.AvailableMarkets)
        {
            var candleResponse = await this._upbitClient.GetMinuteCandles(60, new Candles.Request
            {
                market = market,
                count = "100"
            });

            sb.AppendLine($"[this is 60 candlestick data for market {market}]");
            sb.AppendLine($"- Market: {market}");
            sb.AppendLine(candleResponse.ToReadableString());
            sb.AppendLine();

            var quote = candleResponse.ToQuote();
            var sma = quote.GetSma(20);
            foreach (var smaResult in sma)
            {
                sb.AppendLine($"- SMA on {smaResult.Date:d} was {smaResult.Sma:N4}");
            }

            var prompt = new TextMessage(Role.User, sb.ToString());
            var reply = await this._agent.GenerateReplyAsync(
                messages: [prompt],
                options: new GenerateReplyOptions
                {
                    OutputSchema = schema,
                });
            
            var analysisResult = JsonSerializer.Deserialize<MarketAnalysisResult>(reply.GetContent());
            if (analysisResult == null)
            {
                throw new InvalidOperationException("Failed to deserialize the analysis result.");
            }
            
            analysisResult.Market = market;
            response.Results.Add(analysisResult);
        }
        
        this._logger.LogInformation("Market analysis completed. Results: {@Results}", response.Results);
        await this.PublishMessageAsync(response, new TopicId(nameof(LeaderAgent)));
    }
}