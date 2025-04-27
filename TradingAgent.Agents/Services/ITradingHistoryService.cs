namespace TradingAgent.Agents.Services;

public interface ITradingHistoryService
{
    Task AddTradeHistoryAsync(TradeHistoryRecord record);
    
    Task<List<TradeHistoryRecord>> GetTradeHistoryAsync(uint count);
    
    Task<double> GetTotalProfitRateAsync();
    
    Task<double> GetTotalLossRateAsync();
    
    Task<double> GetTotalProfitAsync();
    
    Task<double> GetTotalLossAsync();
}