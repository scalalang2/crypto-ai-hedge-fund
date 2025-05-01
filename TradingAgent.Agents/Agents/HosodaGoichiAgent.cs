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

[TypeSubscription(nameof(HosodaGoichiAgent))]
public class HosodaGoichiAgent : 
    BaseAgent,
    IHandle<MarketAnalyzeRequest>
{
    private readonly AppConfig _config;
    private readonly AutoGen.Core.IAgent _agent;
    
    private const string Prompt = """
You are a Hosoda Goichi AI agent, making trading decisions strictly using his Ichimoku Kinko Hyo (Ichimoku Cloud) methodology. 
Your analysis must be rooted in the principles and philosophy developed by Goichi Hosoda, focusing on a holistic, “one glance” view of the market. You are given only Ichimoku Cloud data: Tenkan-sen (Conversion Line), Kijun-sen (Base Line), Senkou Span A, Senkou Span B (the Cloud/Kumo), and Chikou Span (Lagging Span).

Your task:
Analyze the current Ichimoku Cloud data and provide a trading decision (buy, sell or hold), with a thorough, step-by-step explanation rooted in Ichimoku principles.

Rules:
1. Proivde a data-driven recommentation
2. Details the exact value readings and their recent movements
3. You must prioritize a long-term investment perspective. Focus on maximizing portfolio growth over months, not just hours or days. Avoid excessive trading and only act when strong signals align with long-term value creation.
4. Your monthly profit target is 10%. Structure your buy, sell, and hold recommendations to achieve this target while managing risk and maintaining a disciplined, long-term approach.
5. When evaluating assets, always consider both short-term market signals and long-term growth potential. Clearly explain how your recommendations support the long-term goal and the monthly target.
""";

    public HosodaGoichiAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger,
        AppConfig config) : base(id, runtime, "Hosoda Goichi Agent", logger)
    {
        this._config = config;
        
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.WorkerAIModel);
        this._agent = new OpenAIChatAgent(
                chatClient: client, 
                name: "Hosoda Goichi Agent", 
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
                         Based on the following the data for the ticker {ticker}, 
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
                
                candleDataStr.AppendLine("### Ichimoku Cloud Data");
                candleDataStr.AppendLine(this.GetIchimokuCloud(data.Quotes));
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

    private string GetIchimokuCloud(List<Quote> marketDataQuotes)
    {
        var stoch = marketDataQuotes.GetIchimoku();
        var sb = new StringBuilder();

        sb.AppendLine("Parameters:");
        sb.AppendLine("- tenkanPeriods = 9");
        sb.AppendLine("- kijunPeriods = 26");
        sb.AppendLine("- senkouBPeriods = 52");
        sb.AppendLine();
        
        var table = new ConsoleTable("Date", "ChikouSpan", "KijunSen", "TenkanSen", "SenkouSpanA", "SenkouSpanB");

        foreach (var result in stoch)
        {
            table.AddRow(
                $"{result.Date:yyyy-MM-dd HH:mm:ss}",
                $"{result.ChikouSpan:F4}",
                $"{result.KijunSen:F4}",
                $"{result.TenkanSen:F4}",
                $"{result.SenkouSpanA:F4}",
                $"{result.SenkouSpanB:F4}");
        }

        sb.AppendLine(table.ToMinimalString());
        
        return sb.ToString();
    }
}