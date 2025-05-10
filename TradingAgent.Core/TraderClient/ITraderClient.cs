namespace TradingAgent.Core.TraderClient;

/// <summary>
/// ITraderClient is an interface for a client that interacts with a trading platform.
/// (e.g. Upbit, Binance, etc..)
/// </summary>
public interface ITraderClient
{
    public Task<Chance.Response> GetChance(Chance.Request args);
    
    public Task<PlaceOrder.Response> PlaceOrder(PlaceOrder.Request args);
    
    public Task<List<Ticker>> GetTicker(string symbol);
    
    public Task<List<DayCandles.Response>> GetDayCandles(DayCandles.Request args);
    
    public Task<List<Candles.Response>> GetMinuteCandles(int unit, Candles.Request args);   
}