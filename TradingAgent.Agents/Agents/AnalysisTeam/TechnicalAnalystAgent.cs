using System.Diagnostics;
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
using TradingAgent.Agents.AgentPrompts;
using TradingAgent.Agents.Agents.ResearchTeam;
using TradingAgent.Agents.Extensions;
using TradingAgent.Agents.Messages.AnalysisTeam;
using TradingAgent.Core.Config;
using TradingAgent.Core.TraderClient;
using TradingAgent.Core.Utils;

namespace TradingAgent.Agents.Agents.AnalysisTeam;

[TypeSubscription(nameof(TechnicalAnalystAgent))]
public class TechnicalAnalystAgent : 
    BaseAgent,
    IHandle<StartAnalysisRequest>
{
    private const string AgentName = "TechnicalAnalystAgent";
    
    private readonly AppConfig _config;
    private readonly AutoGen.Core.IAgent _agent;
    private readonly IUpbitClient _upbitClient;

    private enum ReasoningStep
    {
        WithHourCandle,
        WithFourHourCandle,
        WithDayCandle,
        FinalStep
    }
    
    private OrderedDictionary<ReasoningStep, string> userMessageSteps = new();
    
    public TechnicalAnalystAgent(
    
        AgentId id, 
        IAgentRuntime runtime, 
        IUpbitClient upbitClient,
        ILogger<BaseAgent> logger, 
        AppConfig config) : base(id, runtime, AgentName, logger)
    {
        this._upbitClient = upbitClient;
        this._config = config;
        
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.FastAIModel);
        this._agent = new OpenAIChatAgent(
                chatClient: client, 
                name: AgentName, 
                systemMessage: TechnicalAnalystPrompt.SystemPrompt)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
        
        this.userMessageSteps.Add(ReasoningStep.WithHourCandle, TechnicalAnalystPrompt.UserPromptStep1);
        this.userMessageSteps.Add(ReasoningStep.WithFourHourCandle, TechnicalAnalystPrompt.UserPromptStep2);
        this.userMessageSteps.Add(ReasoningStep.WithDayCandle, TechnicalAnalystPrompt.UserPromptStep3);
        this.userMessageSteps.Add(ReasoningStep.FinalStep, TechnicalAnalystPrompt.UserPromptFinalStep);
    }

    public async ValueTask HandleAsync(StartAnalysisRequest item, MessageContext messageContext)
    {
        // ISO-8061 format
        var currentDateTime = DateTimeUtil.CurrentDateTimeToString();
        var chatHistory = new List<IMessage>();
        
        var schemaBuilder = new JsonSchemaBuilder().FromType<TechnicalAnalysisResult>();
        var schema = schemaBuilder.Build();
        
        foreach(var step in userMessageSteps)
        {
            var message = await this.GenerateReasoningMessage(step.Key, item.MarketContext.Ticker, currentDateTime);
            this._logger.LogInformation("[{name}] {message}", nameof(TechnicalAnalystAgent), message);
            var promptMessage = new TextMessage(Role.User, message);
            chatHistory.Add(promptMessage);
            
            var reply = await this._agent.GenerateReplyAsync(
                messages: chatHistory,
                options: new GenerateReplyOptions
                {
                    OutputSchema = schema,
                });
            chatHistory.Add(reply);
        }

        var analysisResult = JsonSerializer.Deserialize<TechnicalAnalysisResult>(chatHistory.Last().GetContent());
        if (analysisResult == null)
        {
            throw new InvalidOperationException("Failed to deserialize the analysis result.");
        }

        var response = new TechnicalAnalysisResponse
        {
            MarketContext = item.MarketContext,
            AnalysisResult = analysisResult
        };
        await this.PublishMessageAsync(response, new TopicId(nameof(ResearchTeamAgent)));
    }

    private async Task<string> GenerateReasoningMessage(ReasoningStep reasoningStep, string ticker, string currentDateTime)
    {
        switch (reasoningStep)
        {
            case ReasoningStep.WithHourCandle:
            {
                var message = this.userMessageSteps[reasoningStep];
                var request = new Candles.Request
                {
                    market = ticker,
                    to = currentDateTime,
                    count = "100"
                };
                
                var candles = await this._upbitClient.GetMinuteCandles(60, request);
                message = message
                    .Replace("{ticker}", ticker)
                    .Replace("{current_date_time}", currentDateTime)
                    .Replace("{one_hour_candle}", candles.ToReadableString())
                    .Replace("{one_hour_candle_macd}", this.GetMacd(candles))
                    .Replace("{four_hour_candle_rsi}", this.GetRsi(candles))
                    .Replace("{one_hour_candle_bollinger_bands}", this.GetBollingerBand(candles));

                return message;
            }
                break;
            case ReasoningStep.WithFourHourCandle:
            {
                var message = this.userMessageSteps[reasoningStep];
                var request = new Candles.Request
                {
                    market = ticker,
                    to = currentDateTime,
                    count = "100"
                };
                
                var candles = await this._upbitClient.GetMinuteCandles(240, request);
                message = message
                    .Replace("{ticker}", ticker)
                    .Replace("{four_hour_candle}", candles.ToReadableString())
                    .Replace("{four_hour_candle_macd}", this.GetMacd(candles))
                    .Replace("{four_hour_candle_rsi}", this.GetRsi(candles))
                    .Replace("{four_hour_candle_bollinger_bands}", this.GetBollingerBand(candles));

                return message;
            }
                break;
            case ReasoningStep.WithDayCandle:
            {
                var message = this.userMessageSteps[reasoningStep];
                var request = new DayCandles.Request
                {
                    market = ticker,
                    to = currentDateTime,
                    count = "100"
                };
                
                var candles = await this._upbitClient.GetDayCandles(request);
                message = message
                    .Replace("{ticker}", ticker)
                    .Replace("{daily_candle}", candles.ToReadableString())
                    .Replace("{daily_candle_macd}", this.GetMacd(candles))
                    .Replace("{daily_candle_rsi}", this.GetRsi(candles))
                    .Replace("{daily_candle_bollinger_bands}", this.GetBollingerBand(candles));
                
                return message;
            }
                break;
            case ReasoningStep.FinalStep:
            {
                var message = this.userMessageSteps[reasoningStep];
                
                message = message
                    .Replace("{ticker}", ticker);
                
                return message;
            }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(reasoningStep), reasoningStep, null);
        }
    }
    
    private string GetBollingerBand(List<Quote> quotes)
    {
        var band = quotes.GetBollingerBands(20).Condense();
        var table = new ConsoleTable("Date", "SMA", "UpperBand", "LowerBand", "PercentB", "Z-Score", "BandWidth");
        foreach (var quote in band)
        {
            table.AddRow($"{quote.Date:yyyy-MM-dd HH:mm:ss}", quote.Sma, quote.UpperBand, quote.LowerBand, quote.PercentB, quote.ZScore, quote.Width);
        }
        return table.ToString();
    }
    
    private string GetRsi(List<Quote> quotes)
    {
        var table = new ConsoleTable("Date", "RSI");
        var rsi = quotes.GetRsi(14).Condense();
        foreach (var quote in rsi)
        {
            table.AddRow($"{quote.Date:yyyy-MM-dd HH:mm:ss}", quote.Rsi);
        }
        return table.ToString();
    }
    
    private string GetMacd(List<Quote> quotes)
    {
        var table = new ConsoleTable("Date", "MACD", "Signal", "Histogram");
        var macd = quotes.GetMacd(12, 26, 9).Condense();
        foreach (var quote in macd)
        {
            table.AddRow($"{quote.Date:yyyy-MM-dd HH:mm:ss}", quote.Macd, quote.Signal, quote.Histogram);
        }
        return table.ToString();
    }
}