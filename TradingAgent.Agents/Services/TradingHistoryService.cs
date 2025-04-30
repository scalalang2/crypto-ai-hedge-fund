using System.Text.Json;

namespace TradingAgent.Agents.Services;

public class TradingHistoryService : ITradingHistoryService
{
    // TODO: this should be configurable
    private readonly string _historyFilePath = "trade_history.json";
    private readonly List<TradeHistoryRecord> _tradeHistoryRecords = new List<TradeHistoryRecord>();
    
    public TradingHistoryService()
    {
        LoadHistoryFromFile();
    }
    
    public async Task AddTradeHistoryAsync(TradeHistoryRecord record)
    {
        _tradeHistoryRecords.Add(record);

        if (_tradeHistoryRecords.Count > 50)
        {
            _tradeHistoryRecords.RemoveAt(0);
        }

        await SaveHistoryToFileAsync();
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
            .Sum(x => (x.SellingPrice - x.BuyingPrice) * x.Amount);
        return Task.FromResult(totalProfit);
    }

    public Task<double> GetTotalLossAsync()
    {
        var totalLoss = _tradeHistoryRecords
            .Where(x => x.LossRate > 0)
            .Sum(x => (x.SellingPrice - x.BuyingPrice) * x.Amount);
        return Task.FromResult(totalLoss);
    }

    private void LoadHistoryFromFile()
    {
        if (File.Exists(_historyFilePath))
        {
            try
            {
                var json = File.ReadAllText(_historyFilePath);
                var records = JsonSerializer.Deserialize<List<TradeHistoryRecord>>(json);
                if (records != null)
                    _tradeHistoryRecords.AddRange(records);
            }
            catch(Exception e)
            {
                throw new Exception($"Failed to load trade history from file: {e.Message}", e);
            }
        }
    }

    private async Task SaveHistoryToFileAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_tradeHistoryRecords, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_historyFilePath, json);
        }
        catch(Exception e)
        {
            throw new Exception($"Failed to save trade history to file: {e.Message}", e);
        }
    }
}