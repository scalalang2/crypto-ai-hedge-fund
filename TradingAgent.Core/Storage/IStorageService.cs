namespace TradingAgent.Core.Storage;

public interface IStorageService
{
    Task AddTradeHistoryAsync(TradeHistoryRecord record);
    
    Task TryAddInitialPositionAsync(Position position);
    
    Task<List<Position>> GetAllPositionsAsync();
    
    Task<List<TradeHistoryRecord>> GetTradeHistoryAsync(uint count);
    
    Task<PerformanceReport> GetPerformanceReportAsync();
    
    Task UpdateReasoningRecordAsync(string ticker, DateTime date);
    
    Task<ReasoningRecord?> GetReasoningRecordAsync(string ticker);

    Task CleanAsync();
}