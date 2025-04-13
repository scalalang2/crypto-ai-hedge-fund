namespace TradingAgent.Agents.Tools;

public interface IUpbitClient
{
    public Task<List<Order.Response>> GetOrder(Order.Request args);
    
    public Task<List<Orders.Response>> GetOrders(Orders.Request args);
    
    public Task<List<CoinAddress.Response>> GetCoinAdresses();
    
    public Task<CoinAddress.Response> GetCoinAdress(CoinAddress.Request args);
    
    public Task<Chance.Response> GetChance(Chance.Request args);
    
    public Task<Withdraw.Response> GetWithdraw(Withdraw.Request args);
    
    public Task<List<Withdraws.Response>> GetWithdraws(Withdraws.Request args);
    
    public Task<bool> CancelOrder(CancelOrder.Request args);
    
    public Task<List<WalletStatus.Response>> GetWalletStatus();
    
    public Task<List<ApiKeys.Response>> GetApiKeys();
    
    public Task<List<Accounts.Response>> GetAccounts();
    
    public Task<GenerateCoinAddress.Response> GenerateCoinAddress(GenerateCoinAddress.Request args);
    
    public Task<DepositKrw.Response> DepositKrw(DepositKrw.Request args);
    
    public Task<PlaceOrder.Response> PlaceOrder(PlaceOrder.Request args);
    
    public Task<List<Ticks.Response>> GetTicks(Ticks.Request args);
    
    public Task<List<Ticker>> GetTicker(string symbol);
    
    public Task<List<MarketCodes>> GetMarketCodes();
    
    public Task<List<DayCandles.Response>> GetDayCandles(DayCandles.Request args);
    
    public Task<List<Candles.Response>> GetWeekCandles(Candles.Request args);
    
    public Task<List<Candles.Response>> GetMonthCandles(Candles.Request args);
    
    /// <summary>
    /// 분 단위 캔들 조회
    /// </summary>
    /// <param name="unit">몇 분 단위로 조회할 것인지 여부</param>
    /// <param name="args">캔들 요청</param>
    /// <returns></returns>
    public Task<List<Candles.Response>> GetMinuteCandles(int unit, Candles.Request args);
    
    public Task<List<OrderBook.Response>> GetOrderBooks(string symbol);
    
    public Task<List<ClosedOrderHistory.Response>> GetOrderHistory(ClosedOrderHistory.Request request);
}