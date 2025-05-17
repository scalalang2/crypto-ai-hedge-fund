namespace TradingAgent.Core.Storage;

public interface IStorageService
{
    Task AddTradeHistoryAsync(TradeHistoryRecord record);
    
    Task UpdatePositionAsync(TradeHistoryRecord trade);
    
    Task<Position?> GetPositionAsync(string symbol);
    
    Task<List<Position>> GetAllPositionsAsync();
    
    Task<List<TradeHistoryRecord>> GetTradeHistoryAsync(uint count);
    
    Task<PerformanceReport> GetPerformanceReportAsync();
}