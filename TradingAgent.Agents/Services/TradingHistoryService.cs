namespace TradingAgent.Agents.Services;

public class TradingHistoryService : ITradingHistoryService
{
    private readonly List<TradeHistoryRecord> _tradeHistoryRecords = new List<TradeHistoryRecord>();
    
    public Task AddTradeHistoryAsync(TradeHistoryRecord record)
    {
        _tradeHistoryRecords.Add(record);

        if (_tradeHistoryRecords.Count > 50)
        {
            _tradeHistoryRecords.RemoveAt(0);
        }
        
        return Task.CompletedTask;
    }

    public Task<List<TradeHistoryRecord>> GetTradeHistoryAsync(uint count)
    {
        var records = _tradeHistoryRecords
            .OrderByDescending(x => x.Date)
            .Take((int)count)
            .ToList();
        return Task.FromResult(records);
    }

    public Task<double> GetTotalProfitRateAsync()
    {
        var totalProfitRate = _tradeHistoryRecords
            .Where(x => x.ProfitRate > 0)
            .Sum(x => x.ProfitRate);
        return Task.FromResult(totalProfitRate);
    }

    public Task<double> GetTotalLossRateAsync()
    {
        var totalLossRate = _tradeHistoryRecords
            .Where(x => x.LossRate > 0)
            .Sum(x => x.LossRate);
        return Task.FromResult(totalLossRate);
    }

    public Task<double> GetTotalProfitAsync()
    {
        var totalProfit = _tradeHistoryRecords
            .Where(x => x.ProfitRate > 0)
            .Sum(x => x.SellingPrice - x.BuyingPrice);
        return Task.FromResult(totalProfit);
    }

    public Task<double> GetTotalLossAsync()
    {
        var totalLoss = _tradeHistoryRecords
            .Where(x => x.LossRate > 0)
            .Sum(x => x.SellingPrice - x.BuyingPrice);
        return Task.FromResult(totalLoss);
    }
}