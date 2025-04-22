using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
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
You will receive market & indicator data for multiple markets.

Your task is to analyze the data thoroughly and rank the markets in order of how bullish they currently are, from most to least bullish.

## Example
[
    {
        "Market": "KRW-BTC",
        "Sentiment": "Bullish",
        "Confidence": 0.8,
        "Analysis": "The market is showing strong bullish momentum with increasing volume and positive sentiment."
    },
    {
        "Market": "KRW-ETH",
        "Sentiment": "Bearish",
        "Confidence": 0.5,
        "Analysis": "The market is showing some bearish signals, but the overall trend is still uncertain."
    }
]
""";
    
    public MarketAgent(
        IUpbitClient upbitClient,
        AgentId id, 
        IAgentRuntime runtime, 
        string description, 
        ILogger<BaseAgent> logger, 
        AppConfig config) : base(id, runtime, description, logger)
    {
        this._upbitClient = upbitClient;
        this._config = config;
        
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.WorkerAIModel);
        this._agent = new OpenAIChatAgent(client, "MarketAgent", systemMessage: Prompt)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(MarketAnalyzeRequest item, MessageContext messageContext)
    {
        var prompt = "";
        
        foreach (var market in this._config.AvailableMarkets)
        {
            var response = await this._upbitClient.GetMinuteCandles(60, new Candles.Request
            {
                market = market,
                count = "100"
            });
        }
        
        var promptMessage = new TextMessage(Role.User, "Please help me grow my money");
    }
}