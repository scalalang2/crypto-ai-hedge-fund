using System.Text.Json;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Json.Schema;
using Json.Schema.Generation;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using TradingAgent.Agents.Config;
using TradingAgent.Agents.Messages;
using TradingAgent.Core.Extensions;
using TradingAgent.Core.UpbitClient;
using IAgent = AutoGen.Core.IAgent;

namespace TradingAgent.Agents;

[TypeSubscription(nameof(TradingAnalystAgent))]
public class TradingAnalystAgent : 
    BaseAgent,
    IHandle<AnalystSummaryRequest>
{
    private readonly IAgent agent;
    private readonly IUpbitClient upbitClient;

    public TradingAnalystAgent(
        IUpbitClient upbitClient,
        AgentId id,
        IAgentRuntime runtime,
        ILogger<TradingAnalystAgent> logger,
        IOptions<LLMConfiguration> config) : base(id, runtime, "Trading Analysis Agent", logger)
    {
        this.upbitClient = upbitClient;
        var client = new OpenAIClient(config.Value.OpenAIApiKey).GetChatClient(config.Value.Model);
        var systemMessage = @"You are a professional trading analyst specializing in hourly chart-based scalping. 
Your role is to identify short-term trading opportunities using technical analysis and 
provide actionable insights for trades executed within the same day. 

Focus on high-probability setups using indicators like moving averages (200 EMA, 20 SMA), RSI, volume, and chart patterns.

Key Responsibilities:
- Entry Criteria: Identify bullish or bearish momentum with conditions such as RSI overbought/oversold, price crossing SMA/EMA, and strong volume.
- Exit Criteria: Define profit targets (0.1-1%), stop-loss levels, or time-based exits (2-3 hours max).
- Risk Management: Limit risk to 1-2% per trade, maintain proper position sizing, and use a minimum 1:1.5 risk-reward ratio.
- Market Assessment: Analyze trends, volatility, liquidity, and sentiment to adapt strategies";
        
        this.agent = new OpenAIChatAgent(
            chatClient: client,
            name: "trading_analyst",
            systemMessage:systemMessage)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(AnalystSummaryRequest item, MessageContext messageContext)
    {
        var request = new Candles.Request();
        request.market = item.Market;
        request.count = "100";
        var responseSchema = new AnalystSummaryResponse();
        
        var candleData = await this.upbitClient.GetMinuteCandles(30, request);
        
        var message = $"[30 Minute Candle Chart {item.Market}]\n";
        message += $"{candleData.GenerateSchemaPrompt("Candle Data")}\n";
        message += $"{candleData.GenerateDataPrompt("Candle Data")}\n\n";
        
        var currentTime = DateTime.UtcNow.AddHours(9);
        message += $"Current time: {currentTime:yyyy-MM-dd HH:mm:ss} KST\n\n";
        message += "Please analyze the market and provide the following information:\n";
        message += "Respond in JSON format with the following keys:\n";
        message += $"{responseSchema.GenerateSchemaPrompt("Analyst Summary")}";
        
        var userMessage = new TextMessage(Role.User, message);
        var reply = await this.agent.GenerateReplyAsync(
            messages: [userMessage],
            options: new GenerateReplyOptions
            {
                OutputSchema = responseSchema.GetSchema(),
            });
        
        var response = JsonSerializer.Deserialize<AnalystSummaryResponse>(reply.GetContent());
        if(response is null)
        {
            throw new JsonException("Failed to deserialize the response.");
        }
        
        await this.PublishMessageAsync(response, new TopicId(nameof(CfoAgent)));
    }
}