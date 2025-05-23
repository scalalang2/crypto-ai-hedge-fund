using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
using TradingAgent.Agents.Agents.AnalysisTeam;
using TradingAgent.Agents.Messages.AnalysisTeam;
using TradingAgent.Agents.State;
using TradingAgent.Agents.Utils;
using TradingAgent.Core.Config;
using TradingAgent.Core.MessageSender;
using TradingAgent.Core.Storage;
using TradingAgent.Core.TraderClient;

namespace TradingAgent.Agents.Agents;

[TypeSubscription(nameof(GateKeeperAgent))]
public class GateKeeperAgent :
    BaseAgent,
    IHandle<StartAnalysisRequest>
{
    private const string AgentName = "GateKeeper Agent";
    private readonly AppConfig _config;
    private readonly IUpbitClient _upbitClient;
    private readonly IMessageSender _messageSender;
    private readonly IStorageService _storageService;
    private readonly AgentSharedState _state;
        
    public GateKeeperAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config, 
        AgentSharedState state, 
        IUpbitClient upbitClient, 
        IMessageSender messageSender, 
        IStorageService storageService) : base(id, runtime, AgentName, logger)
    {
        this._config = config;
        this._state = state;
        this._upbitClient = upbitClient;
        this._messageSender = messageSender;
        this._storageService = storageService;
    }

    public async ValueTask HandleAsync(StartAnalysisRequest item, MessageContext messageContext)
    {
        var lastReasoning = await _storageService.GetReasoningRecordAsync(item.MarketContext.Ticker);
        if(lastReasoning != null && lastReasoning.LastReasoningTime + TimeSpan.FromHours(3) > DateTime.UtcNow)
        {
            await this._messageSender.SendMessage($"[{item.MarketContext.Ticker}]는 마지막 추론 시간이 6시간 이내입니다. 트레이딩 팀은 휴식을 취합니다.");
            return;
        }
        
        // 추론 12시간 미만인 경우에는 
        if(lastReasoning != null && lastReasoning.LastReasoningTime + TimeSpan.FromHours(12) > DateTime.UtcNow)
        {
            var tickerResponse = await this._upbitClient.GetTicker(item.MarketContext.Ticker);
            if (tickerResponse.Count > 0)
            {
                var currentPrice = Convert.ToDecimal(tickerResponse[0].trade_price);
                var request = new Chance.Request
                {
                    market = item.MarketContext.Ticker
                };
                var response = await this._upbitClient.GetChance(request);

                var averageBuyingPrice = Convert.ToDecimal(response.ask_account.avg_buy_price);
                var currentPosition = Convert.ToDecimal(response.ask_account.balance);

                if (currentPosition == 0)
                {
                    await this._messageSender.SendMessage($"[{item.MarketContext.Ticker}]는 현재 포지션이 없습니다. 트레이딩 팀은 휴식을 취합니다.");
                    return;
                }

                if (currentPrice < averageBuyingPrice * 1.05m)
                {
                    await this._messageSender.SendMessage($"[{item.MarketContext.Ticker}]는 현재 가격이 평균 매입가의 5% 미만입니다. 트레이딩 팀은 휴식을 취합니다.");
                    return;
                }
            }
        }
        
        this._state.Candidates.Add(item.MarketContext.Ticker, true);
        await this.PublishMessageAsync(item, new TopicId(nameof(NewsAnalystAgent)));
        await this.PublishMessageAsync(item, new TopicId(nameof(SentimentAnalystAgent)));
        await this.PublishMessageAsync(item, new TopicId(nameof(TechnicalAnalystAgent)));
    }
}