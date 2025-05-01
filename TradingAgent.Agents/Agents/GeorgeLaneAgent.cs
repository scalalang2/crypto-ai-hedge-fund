using System.Text;
using System.Text.Json;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using ConsoleTables;
using Json.Schema;
using Json.Schema.Generation;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
using Skender.Stock.Indicators;
using TradingAgent.Agents.Extensions;
using TradingAgent.Agents.Messages;
using TradingAgent.Core.Config;

namespace TradingAgent.Agents.Agents;

[TypeSubscription(nameof(GeorgeLaneAgent))]
public class GeorgeLaneAgent : 
    BaseAgent,
    IHandle<MarketAnalyzeRequest>
{
    private readonly AppConfig _config;
    private readonly AutoGen.Core.IAgent _agent;
    
    private const string Prompt = """
You are a George Lane AI agent, making trading decisions using his principles of stochastic analysis:

1. Focus on momentum: Use the Stochastic Oscillator (%K and %D lines) to measure price momentum, recognizing that "momentum always changes direction before price does."
2. Identify overbought and oversold conditions: Treat readings above 80 as overbought and below 20 as oversold, using these as preconditions for potential reversals.
3. Seek confirmation through divergence: Look for bullish or bearish divergences between price and the oscillator, as these can foreshadow trend reversals.
4. Confirm with crossovers: Use %K and %D crossovers at extreme levels (overbought/oversold) as secondary confirmation for entry or exit signals.
5. Adjust sensitivity as needed: Consider modifying %K and %D settings to balance signal frequency and reliability, understanding that higher sensitivity increases false signals, while lower sensitivity may delay entries.
6. Integrate with broader context: Combine stochastic signals with price patterns, support/resistance, and other momentum tools for more robust decisions.

Rules:
1. Proivde a data-driven recommentation
2. Details the exact value readings and their recent movements
3. You must prioritize a long-term investment perspective. Focus on maximizing portfolio growth over months, not just hours or days. Avoid excessive trading and only act when strong signals align with long-term value creation.
4. Your monthly profit target is 10%. Structure your buy, sell, and hold recommendations to achieve this target while managing risk and maintaining a disciplined, long-term approach.
5. When evaluating assets, always consider both short-term market signals and long-term growth potential. Clearly explain how your recommendations support the long-term goal and the monthly target.

For example, if bullish: "The %K line has emerged from the oversold zone, crossing above %D at a reading of 22. This move is accompanied by a bullish divergence, as price made a lower low while the oscillator formed a higher low, indicating waning downside momentum. These signals, in line with Lane's methodology, suggest a probable upward reversal."
For example, if bearish: "The %K and %D lines are both above 85, signaling overbought conditions. A bearish divergence is evident, with price making a higher high while the oscillator fails to confirm. This loss of momentum, a classic Lane warning, points to an impending reversal."
""";

    public GeorgeLaneAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger,
        AppConfig config) : base(id, runtime, "Geroge Lane Agent", logger)
    {
        this._config = config;
        
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.WorkerAIModel);
        this._agent = new OpenAIChatAgent(
                chatClient: client, 
                name: "Geroge Lane Agent", 
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
                         - Proivde a data-driven recommentation
                         - Details the exact value readings and their recent movements

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
                
                candleDataStr.AppendLine("### Stochastic Oscillator");
                candleDataStr.AppendLine(this.GetStochasticOscillator(data.Quotes));
                candleDataStr.AppendLine();
            }

            prompt = prompt
                .Replace("{ticker}", marketData.Ticker)
                .Replace("{candle_data}", candleDataStr.ToString());
            
            this._logger.LogInformation(prompt);

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

    private string GetStochasticOscillator(List<Quote> marketDataQuotes)
    {
        var stoch = marketDataQuotes
            .GetStoch()
            .Condense();
        var sb = new StringBuilder();

        sb.AppendLine("Parameters:");
        sb.AppendLine("lookbackPeriods = 14 // Lookback period (N) for the oscillator (%K).");
        sb.AppendLine("signalPeriods = 3 // Smoothing period for the signal (%D).");
        sb.AppendLine("smoothPeriods = 3 // Smoothing period (S) for the Oscillator (%K). “Slow” stochastic uses 3, “Fast” stochastic uses 1. ");
        sb.AppendLine("kFactor = 3 // Weight of %K in the %J calculation.");
        sb.AppendLine("dFactor = 2 // Weight of %D in the %J calculation.");
        sb.AppendLine();
        
        var table = new ConsoleTable("Date", "%K", "%D", "Signal", "Oscillator", "J", "PercentJ");

        foreach (var result in stoch)
        {
            table.AddRow(
                $"{result.Date:yyyy-MM-dd HH:mm:ss}",
                $"{result.K:F4}",
                $"{result.D:F4}",
                $"{result.Signal}",
                $"{result.Oscillator:F4}",
                $"{result.J:F4}",
                $"{result.PercentJ:F2}");
        }
        
        sb.AppendLine(table.ToMinimalString());
        return sb.ToString();
    }
}