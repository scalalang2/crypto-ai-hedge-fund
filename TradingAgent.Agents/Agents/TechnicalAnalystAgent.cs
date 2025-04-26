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
[TypeSubscription(nameof(TechnicalAnalystAgent))]
public class TechnicalAnalystAgent : 
    BaseAgent,
    IHandle<MarketAnalyzeRequest>
{
    private readonly AppConfig _config;
    private readonly AutoGen.Core.IAgent _agent;
    private IUpbitClient _upbitClient;

    private const string Prompt = """
You are an advanced analyst specializing in technical analysis of cryptocurrency and stock markets. 
Your mission is to provide accurate market signals by comprehensively analyzing various technical indicators and chart patterns.

You will receive the following data:
1. Candlestick chart data (60 minutes, 4 hours, or daily)
2. Bollinger Bands (20 periods)
3. RSI (14 periods)
4. MACD (12 fast period, 26 slow period, 9 signal period)
5. OBV (On-Balance Volume)

Bollinger Bands Analysis:
- Price approaching/breaking upper band: Possible bullish signal
- Price approaching/breaking lower band: Possible bearish signal
- %B > 0.8: Possible overbought condition
- %B < 0.2: Possible oversold condition
- Bandwidth expansion: Increased volatility and potential trend strengthening
- Bandwidth contraction: Decreased volatility and potential trend reversal

RSI Analysis:
- RSI > 70: Overbought condition, possible downward reversal
- RSI < 30: Oversold condition, possible upward reversal
- RSI trendline breakout: Important momentum change signal
- Divergence pattern: Strong reversal signal

MACD Analysis:
- MACD line crossing above signal line: Bullish signal
- MACD line crossing below signal line: Bearish signal
- Histogram expansion: Current trend strengthening
- Divergence occurrence: Trend weakening or reversal signal

OBV Analysis:
- OBV rising + price rising: Confirmed strong uptrend
- OBV falling + price rising: Weak uptrend, possible reversal
- OBV trendline breakout: Important volume change signal

Rules:
- Proivde a data-driven recommentation

## Example
{
    "Signal": "High Bullish",
    "Confidence": 92.5,
    "Reasoning": "The price broke above the upper Bollinger Band, with %B at 0.95 showing strong momentum. RSI is at 68, not yet in the overbought zone, and the MACD histogram is expanding, confirming the uptrend. OBV is steadily rising, supporting the price increase with volume. 8 out of the last 10 candles are bullish, showing strong buying pressure."
}
""";
    
    public TechnicalAnalystAgent(
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
        
        foreach (var market in this._config.AvailableMarkets)
        {
            var prompt = """
Based on the following {chart_type} candlestick for the market {market}, 
create a investment signal.

# Candle Data
{candle_data}

# Bollinger Bands
{bollinger_band}

# RSI
{rsi}

# MACD
{macd}

# OBV
{obv}

Return the trading signal in this JSON format:
{
    "Signal": "bullish/bearish/neutral",
    "Confidence": float (0-100),
    "Reasoning": "string"
}
""";
            
            var quote = item.AnalysisType switch
            {
                MarketAnalysisType.DayCandle => await this.GetDayCandleQuote(market),
                MarketAnalysisType.FourHourCandle => await this.GetMinuteCandleQuote(market, 240),
                MarketAnalysisType.HourCandle => await this.GetMinuteCandleQuote(market, 60),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            var chartType = item.AnalysisType switch
            {
                MarketAnalysisType.DayCandle => "day candle",
                MarketAnalysisType.FourHourCandle => "4 hour candles",
                MarketAnalysisType.HourCandle => "60 minute candles",
                _ => throw new ArgumentOutOfRangeException()
            };

            prompt = prompt
                .Replace("{market}", market)
                .Replace("{chart_type}", chartType)
                .Replace("{candle_data}", quote.ToReadableString())
                .Replace("{bollinger_band}", this.GetBollingerBand(quote))
                .Replace("{rsi}", this.GetRsi(quote))
                .Replace("{macd}", this.GetMacd(quote))
                .Replace("{obv}", this.GetObv(quote));

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
            
            response.Results.Add((analysisResult, market, item.AnalysisType));
        }

        await this.PublishMessageAsync(response, new TopicId(nameof(PortfolioManager)));
    }

    private string GetBollingerBand(List<Quote> quotes)
    {
        var sb = new StringBuilder();
        var band = quotes.GetBollingerBands(20);
        sb.AppendLine("Date | SMA | UpperBand | LowerBand | PercentB | Z-Score | BandWidth");
        foreach (var quote in band)
        {
            sb.AppendLine($"{quote.Date} | {quote.Sma} | {quote.UpperBand} | {quote.LowerBand} | {quote.PercentB} | {quote.ZScore} | {quote.Width}");
        }
        return sb.ToString();
    }
    
    private string GetRsi(List<Quote> quotes)
    {
        var sb = new StringBuilder();
        var rsi = quotes.GetRsi(14);
        sb.AppendLine("Date | RSI");
        foreach (var quote in rsi)
        {
            sb.AppendLine($"{quote.Date} | {quote.Rsi}");
        }
        return sb.ToString();
    }
    
    private string GetMacd(List<Quote> quotes)
    {
        var sb = new StringBuilder();
        var macd = quotes.GetMacd(12, 26, 9);
        sb.AppendLine("Date | MACD | Signal | Histogram");
        foreach (var quote in macd)
        {
            sb.AppendLine($"{quote.Date} | {quote.Macd} | {quote.Signal} | {quote.Histogram}");
        }
        return sb.ToString();
    }
    
    private string GetObv(List<Quote> quotes)
    {
        var sb = new StringBuilder();
        var obv = quotes.GetObv();
        sb.AppendLine("Date | OBV");
        foreach (var quote in obv)
        {
            sb.AppendLine($"{quote.Date} | {quote.Obv}");
        }
        return sb.ToString();
    }

    private async Task<List<Quote>> GetMinuteCandleQuote(string market, int unit)
    {
        var candleResponse = await this._upbitClient.GetMinuteCandles(unit, new Candles.Request
        {
            market = market,
            count = "100"
        });
            
        return candleResponse.ToQuote();
    }
    
    private async Task<List<Quote>> GetDayCandleQuote(string market)
    {
        var candleResponse = await this._upbitClient.GetDayCandles(new DayCandles.Request
        {
            market = market,
            count = "100"
        });
            
        return candleResponse.ToQuote();
    }
}