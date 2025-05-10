using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using TradingAgent.Agents.Messages;
using TradingAgent.Agents.Services;
using TradingAgent.Agents.Utils;
using TradingAgent.Core.Config;
using TradingAgent.Core.TraderClient;
using Chance = TradingAgent.Core.TraderClient.Chance;
using PlaceOrder = TradingAgent.Core.TraderClient.PlaceOrder;

namespace TradingAgent.Agents.Agents;

/// <summary>
/// 리더의 메시지를 해석하고 실제 트레이딩을 수행하는 에이전트
/// </summary>
[TypeSubscription(nameof(TraderAgent))]
public class TraderAgent : BaseAgent, IHandle<FinalDecisionMessage>
{
    private readonly AppConfig config;
    private readonly Dictionary<string, Func<string, Task<string>>> traderFunctionMap;
    private readonly ITraderClient _upbitClient;
    private readonly IMessageSender _messageSender;
    private readonly ITradingHistoryService _tradingHistoryService;

    public TraderAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config, 
        ITraderClient upbitClient, 
        IMessageSender messageSender, 
        ITradingHistoryService tradingHistoryService) : base(id, runtime, "trader", logger)
    {
        this.config = config;
        this._upbitClient = upbitClient;
        this._messageSender = messageSender;
        _tradingHistoryService = tradingHistoryService;
    }

    public async ValueTask HandleAsync(FinalDecisionMessage item, MessageContext messageContext)
    {
        var ticker = await this._upbitClient.GetTicker(string.Join(",", this.config.AvailableMarkets));
        if (ticker.Count == 0)
        {
            throw new Exception($"Ticker not found");
        }
        
        try
        {
            foreach (var decision in item.FinalDecisions)
            {
                if (decision.Action == "Hold" || decision.Quantity == 0)
                {
                    continue;
                }
                
                var price = ticker.FirstOrDefault(x => x.market == decision.Ticker)?.trade_price;
                if (price == null)
                {
                    throw new Exception($"Ticker {decision.Ticker} not found");
                }

                await Task.Delay(500); // rest to avoid rate limit
                await this.PlaceOrder(decision, price.Value);
            }
        }
        catch (Exception ex)
        {
            await this._messageSender.SendMessage($"**Trading Error** \n\n {ex.Message}");
            this._logger.LogError(ex, "Error handling FinalDecisionMessage {@Request}", item.FinalDecisions);
        }
        finally
        {
            var currentPosition = await SharedUtils.GetCurrentPositionPrompt(this._upbitClient, this.config.AvailableMarkets, ticker);
            var tradingHistory = await SharedUtils.GetTradingHistoryPrompt(this._tradingHistoryService);
            await this._messageSender.SendMessage($"**Current Position**\n{currentPosition}\n");
            await this._messageSender.SendMessage($"**Trading History**\n{tradingHistory}\n");
        }
    }

    public async Task PlaceOrder(FinalDecision decision, double price)
    {
        const string buy = "Buy";
        const string sell = "Sell";
        
        if(decision.Action != buy && decision.Action != sell)
        {
            throw new ArgumentException($"Invalid action. Only 'buy' and 'sell' are allowed. but {decision.Action} was given");
        }
            
        // 거래 금액이 2만원 미만이면 그냥 무시
        if(price * decision.Quantity < 20000)
        {
            return;
        }
        await Task.Delay(500);
        
        var quantity = string.Format("{0:F8}", decision.Quantity);
        var ordType = decision.Action == buy ? "price" : "market";
        var side = decision.Action == buy ? "bid" : "ask";
        
        var request = new PlaceOrder.Request
        {
            market = decision.Ticker,
            side = side,
            ord_type = ordType
        };
        
        if(decision.Action == buy)
        {
            request.price = quantity;
        }
        else
        {
            request.volume = quantity;
        }
        
        // 주문 시작
        await this._upbitClient.PlaceOrder(request);
        
        await Task.Delay(500);

        // 판매인 경우 매매 기록을 추가한다.
        if (decision.Action == sell)
        {
            var chanceRequest = new Chance.Request
            {
                market = decision.Ticker
            };
            var chance = await this._upbitClient.GetChance(chanceRequest);
            
            var tradeHistoryRecord = new TradeHistoryRecord
            {
                Date = DateTime.UtcNow,
                Ticker = decision.Ticker,
                BuyingPrice = Convert.ToDouble(chance.ask_account.avg_buy_price),
                SellingPrice = price,
                Amount = decision.Quantity
            };
            await this._tradingHistoryService.AddTradeHistoryAsync(tradeHistoryRecord);
        }
    }
}