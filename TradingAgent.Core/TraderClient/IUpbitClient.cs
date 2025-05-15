using Skender.Stock.Indicators;

namespace TradingAgent.Core.TraderClient;

/// <summary>
/// ITraderClient is an interface for a client that interacts with a trading platform.
/// (e.g. Upbit, Binance, etc..)
/// </summary>
public interface IUpbitClient
{
    public Task<Chance.Response> GetChance(Chance.Request args);
    
    public Task<PlaceOrder.Response> PlaceOrder(PlaceOrder.Request args);
    
    public Task<List<Ticker>> GetTicker(string symbol);
    
    public Task<List<Quote>> GetDayCandles(DayCandles.Request args);
    
    public Task<List<Quote>> GetMinuteCandles(int unit, Candles.Request args);   
}