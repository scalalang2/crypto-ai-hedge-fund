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
using TradingAgent.Core.Config;

namespace TradingAgent.Agents.Agents;

/// <summary>
/// 시장 분석 에이전트
/// </summary>
[TypeSubscription(nameof(TechnicalAnalystAgent))]
public class TechnicalAnalystAgent : 
    BaseAgent,
    IHandle<MarketAnalyzeRequest>
{
    private readonly AppConfig _config;
    private readonly AutoGen.Core.IAgent _agent;

    private const string Prompt = """
You are an advanced analyst specializing in technical analysis of cryptocurrency and stock markets. 
Your mission is to provide accurate market signals by comprehensively analyzing various technical indicators and chart patterns.

You will receive the following data:
1. Candlestick chart data (60 minutes, 4 hours, or daily)
2. Bollinger Bands (20 periods)
3. RSI (14 periods)
4. MACD (12 fast period, 26 slow period, 9 signal period)
5. OBV (On-Balance Volume)

Rules:
- Provide a data-driven recommendation
- Detail the exact value readings and their recent movements

## Example
{
    "Signal": "High Bullish",
    "Confidence": 92.5,
    "Reasoning": "The price broke above the upper Bollinger Band, with %B at 0.95 showing strong momentum. RSI is at 68, not yet in the overbought zone, and the MACD histogram is expanding, confirming the uptrend. OBV is steadily rising, supporting the price increase with volume. 8 out of the last 10 candles are bullish, showing strong buying pressure."
}
""";
    
    public TechnicalAnalystAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config) : base(id, runtime, "Technical Analyst", logger)
    {
        this._config = config;
        
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.WorkerAIModel);
        this._agent = new OpenAIChatAgent(
                chatClient: client, 
                name: "Techincal Analyst Agent", 
                systemMessage: Prompt)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(MarketAnalyzeRequest item, MessageContext messageContext)
    {
        var response = new MarketAnalyzeResponse();
        var schemaBuilder = new JsonSchemaBuilder().FromType<MarketAnalysisResult>();
        var schema = schemaBuilder.Build();
        
        foreach (var marketData in item.MarketDataList)
        {
            var prompt = """
Based on the following the candle data for the ticker {ticker}, 
create a investment signal.

Rules:
1. Proivde a data-driven recommentation
2. Details the exact value readings and their recent movements
3. You must prioritize a long-term investment perspective. Focus on maximizing portfolio growth over months, not just hours or days. Avoid excessive trading and only act when strong signals align with long-term value creation.
4. Your monthly profit target is 10%. Structure your buy, sell, and hold recommendations to achieve this target while managing risk and maintaining a disciplined, long-term approach.
5. When evaluating assets, always consider both short-term market signals and long-term growth potential. Clearly explain how your recommendations support the long-term goal and the monthly target.

# Candle Data
{candle_data}

Return the trading signal in this JSON format:
{
    "Signal": "bullish/bearish/neutral",
    "Confidence": float (0-100),
    "Reasoning": "string"
}
""";
            var candleDataStr = new StringBuilder();
            foreach (var data in marketData.CandleData)
            {
                switch (data.QuoteType)
                {
                    case QuoteType.DayCandle:
                        candleDataStr.AppendLine("## Daily Candle Data");
                        break;
                    case QuoteType.FourHourCandle:
                        candleDataStr.AppendLine("## 4-Hour Candle Data");
                        break;
                    case QuoteType.HourCandle:
                        candleDataStr.AppendLine("## 1-Hour Candle Data");
                        break;
                }
                
                candleDataStr.AppendLine(data.Quotes.ToReadableString());
                candleDataStr.AppendLine();
                
                candleDataStr.AppendLine("### Bollinger Bands");
                candleDataStr.AppendLine(this.GetBollingerBand(data.Quotes));
                
                candleDataStr.AppendLine("### RSI");
                candleDataStr.AppendLine(this.GetRsi(data.Quotes));
                
                candleDataStr.AppendLine("### MACD");
                candleDataStr.AppendLine(this.GetMacd(data.Quotes));
                
                candleDataStr.AppendLine("### OBV");
                candleDataStr.AppendLine(this.GetObv(data.Quotes));
                candleDataStr.AppendLine();
            }

            prompt = prompt
                .Replace("{ticker}", marketData.Ticker)
                .Replace("{candle_data}", candleDataStr.ToString());

            var message = new TextMessage(Role.User, prompt);
            var reply = await this._agent.GenerateReplyAsync(
                messages: [message],
                options: new GenerateReplyOptions
                {
                    OutputSchema = schema,
                });
            
            var analysisResult = JsonSerializer.Deserialize<MarketAnalysisResult>(reply.GetContent());
            if (analysisResult == null)
            {
                throw new InvalidOperationException("Failed to deserialize the analysis result.");
            }
            
            response.MarketAnalysis.Add((analysisResult, marketData.Ticker));
        }
        
        await this.PublishMessageAsync(response, new TopicId(nameof(PortfolioManager)));
    }

    private string GetBollingerBand(List<Quote> quotes)
    {
        var sb = new StringBuilder();
        var band = quotes.GetBollingerBands(20).Condense();
        sb.AppendLine("Date | SMA | UpperBand | LowerBand | PercentB | Z-Score | BandWidth");
        foreach (var quote in band)
        {
            sb.AppendLine($"{quote.Date:yyyy-MM-dd HH:mm:ss} | {quote.Sma} | {quote.UpperBand} | {quote.LowerBand} | {quote.PercentB} | {quote.ZScore} | {quote.Width}");
        }
        return sb.ToString();
    }
    
    private string GetRsi(List<Quote> quotes)
    {
        var sb = new StringBuilder();
        var rsi = quotes.GetRsi(14).Condense();
        sb.AppendLine("Date | RSI");
        foreach (var quote in rsi)
        {
            sb.AppendLine($"{quote.Date:yyyy-MM-dd HH:mm:ss} | {quote.Rsi}");
        }
        return sb.ToString();
    }
    
    private string GetMacd(List<Quote> quotes)
    {
        var sb = new StringBuilder();
        var macd = quotes.GetMacd(12, 26, 9).Condense();
        sb.AppendLine("Date | MACD | Signal | Histogram");
        foreach (var quote in macd)
        {
            sb.AppendLine($"{quote.Date:yyyy-MM-dd HH:mm:ss} | {quote.Macd} | {quote.Signal} | {quote.Histogram}");
        }
        return sb.ToString();
    }
    
    private string GetObv(List<Quote> quotes)
    {
        var sb = new StringBuilder();
        var obv = quotes.GetObv().Condense();
        sb.AppendLine("Date | OBV");
        foreach (var quote in obv)
        {
            sb.AppendLine($"{quote.Date:yyyy-MM-dd HH:mm:ss} | {quote.Obv}");
        }
        return sb.ToString();
    }
}