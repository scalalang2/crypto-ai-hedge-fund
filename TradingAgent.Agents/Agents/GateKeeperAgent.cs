using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
using TradingAgent.Agents.Agents.AnalysisTeam;
using TradingAgent.Agents.Messages;
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
    IHandle<StartGateKeeperRequest>
{
    private const string AgentName = "GateKeeper Agent";
    private readonly AppConfig _config;
    private readonly IUpbitClient _upbitClient;
    private readonly IMessageSender _messageSender;
    private readonly IStorageService _storageService;
    private readonly AgentSharedState _state;
    private readonly ILogger<GateKeeperAgent> _logger;
        
    public GateKeeperAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<GateKeeperAgent> logger, 
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
        this._logger = logger;
    }

    public async ValueTask HandleAsync(StartGateKeeperRequest item, MessageContext messageContext)
    {
        foreach (var marketContext in this._config.Markets)
        {
            var chanceRequest = new Chance.Request
            {
                market = marketContext.Ticker
            };
            var chanceResponse = await this._upbitClient.GetChance(chanceRequest);
            var currentPosition = new Position
            {
                Symbol = marketContext.Ticker,
                Amount = Convert.ToDouble(chanceResponse.ask_account.balance),
                AverageBuyPrice = Convert.ToDouble(chanceResponse.ask_account.avg_buy_price),
                LastUpdated = DateTime.UtcNow,
            };
            await _storageService.TryAddInitialPositionAsync(currentPosition);

            var lastReasoning = await _storageService.GetReasoningRecordAsync(marketContext.Ticker);
            if(lastReasoning != null && lastReasoning.LastReasoningTime + TimeSpan.FromHours(3) > DateTime.UtcNow)
            {
                await this._messageSender.SendMessage($"[{marketContext.Ticker}]는 마지막 추론 시간이 {3}시간 이내입니다.");
                continue;
            }
        
            // 추론 12시간 미만인 경우에는 
            if(lastReasoning != null && lastReasoning.LastReasoningTime + TimeSpan.FromHours(12) > DateTime.UtcNow)
            {
                var tickerResponse = await this._upbitClient.GetTicker(marketContext.Ticker);
                if (tickerResponse.Count > 0)
                {
                    var currentPrice = Convert.ToDouble(tickerResponse[0].trade_price);
                    if (currentPosition.Amount == 0)
                    {
                        await this._messageSender.SendMessage($"[{marketContext.Ticker}]는 현재 포지션이 없습니다. 트레이딩 팀은 휴식을 취합니다.");
                        continue;
                    }

                    if (currentPrice < currentPosition.AverageBuyPrice * 1.05d)
                    {
                        await this._messageSender.SendMessage($"[{marketContext.Ticker}]는 현재 가격이 평균 매입가의 5% 미만입니다. 트레이딩 팀은 휴식을 취합니다.");
                        continue;
                    }
                }
            }
        
            this._state.Candidates.Add(marketContext);
            this._logger.LogInformation("[{Ticker}] was added to candidates", marketContext.Ticker);
        }

        foreach (var candidate in this._state.Candidates)
        {
            var message = new StartAnalysisRequest
            {
                MarketContext = candidate,
            };

            // await this.PublishMessageAsync(message, new TopicId(nameof(NewsAnalystAgent)));
            // await this.PublishMessageAsync(message, new TopicId(nameof(SentimentAnalystAgent)));
            await this.PublishMessageAsync(message, new TopicId(nameof(TechnicalAnalystAgent)));
        }
    }
}