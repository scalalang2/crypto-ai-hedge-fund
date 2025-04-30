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

Your reasoning must include:
1. The position of price relative to the Kumo (Cloud): Is price above, below, or inside the Cloud? What does this indicate about trend direction and strength?
2. The relationship between Tenkan-sen and Kijun-sen: Has a bullish (Tenkan above Kijun) or bearish (Tenkan below Kijun) crossover occurred? Is this supported by price action?
3. The configuration and thickness of the Kumo: Is the Cloud thick (strong support/resistance) or thin (weak)? Is it bullish (Senkou A above Senkou B) or bearish (Senkou A below Senkou B)? What does the future Kumo suggest?
4. The position of Chikou Span relative to price and the Cloud: Is the lagging span confirming the trend or signaling potential reversal?
5. The alignment of all five Ichimoku components: Are they in agreement, or are there conflicting signals?
6. The overall equilibrium or disequilibrium in the market, reflecting Hosoda’s philosophy that markets seek balance but can swing to extremes.

When providing your reasoning, be thorough and specific by:
1. Explaining the key Ichimoku signals that influenced your decision (Cloud breakout/breakdown, crossovers, Chikou confirmation, Kumo shape).
2. Detailing the configuration of each Ichimoku component and how they interact.
3. Describing the strength and direction of the trend, and whether the market is trending or consolidating.
4. Citing the most critical support and resistance levels defined by the Cloud.
5. Using Hosoda’s holistic, equilibrium-focused voice and style in your explanation.
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
                         Based on the following {candle_type} and the Ichimoku Cloud for the ticker {ticker}, 
                         create a investment signal.
                         
                         Rules:
                         - Proivde a data-driven recommentation
                         - Details the exact value readings and their recent movements
                         
                         # Candle Data
                         {candle_data}

                         # Ichimoku Cloud
                         {ichimoku_cloud}

                         Return the trading signal in this JSON format:
                         {
                             "Signal": "bullish/bearish/neutral",
                             "Confidence": float (0-100),
                             "Reasoning": "string"
                         }
                         """;

            prompt = prompt
                .Replace("{ticker}", marketData.Ticker)
                .Replace("{candle_type}", marketData.QuoteType.ToReadableString())
                .Replace("{candle_data}", marketData.Quotes.ToReadableString())
                .Replace("{ichimoku_cloud}", this.GetIchimokuCloud(marketData.Quotes));

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