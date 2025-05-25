using Microsoft.EntityFrameworkCore;

namespace TradingAgent.Core.Storage;

public class StorageService : IStorageService
{
    private readonly TradingDbContext _dbContext;

    public StorageService(TradingDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbContext.Database.EnsureCreated();
    }

    public async Task AddTradeHistoryAsync(TradeHistoryRecord record)
    {
        _dbContext.TradeHistoryRecords.Add(record);
        await _dbContext.SaveChangesAsync();
        await UpdatePositionAsync(record);
    }

    public async Task TryAddInitialPositionAsync(Position position)
    {
        var pos = await _dbContext.Positions.FirstOrDefaultAsync(p => p.Symbol == position.Symbol);
        if (pos == null)
        {
            _dbContext.Positions.Add(position);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task UpdatePositionAsync(TradeHistoryRecord trade)
    {
        var position = await _dbContext.Positions.FirstOrDefaultAsync(p => p.Symbol == trade.Symbol);

        if (position == null)
        {
            position = new Position { Symbol = trade.Symbol, Amount = 0, AverageBuyPrice = 0 };
            _dbContext.Positions.Add(position);
        }

        if (trade.OrderType.Equals("Buy"))
        {
            var totalCost = (position.AverageBuyPrice * position.Amount) + (trade.Price * trade.Amount);
            position.Amount += trade.Amount;
            position.AverageBuyPrice = (position.Amount > 0) ? totalCost / position.Amount : 0;
        }
        else if (trade.OrderType.Equals("Sell"))
        {
            position.Amount -= trade.Amount;
            
            if (position.Amount <= 0)
            {
                position.Amount = 0;
                position.AverageBuyPrice = 0;
            }
        }
        position.LastUpdated = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Position?> GetPositionAsync(string symbol)
    {
        return await _dbContext.Positions.FirstOrDefaultAsync(p => p.Symbol == symbol);
    }

    public async Task<List<Position>> GetAllPositionsAsync()
    {
        return await _dbContext.Positions.Where(p => p.Amount > 0).ToListAsync();
    }

    public async Task<List<TradeHistoryRecord>> GetTradeHistoryAsync(uint count)
    {
        return await _dbContext.TradeHistoryRecords
            .OrderByDescending(x => x.Date)
            .Take((int)count)
            .ToListAsync();
    }
    
    public async Task<PerformanceReport> GetPerformanceReportAsync()
    {
        var sellTrades = await _dbContext.TradeHistoryRecords
            .Where(t => t.OrderType.Equals("Sell") && t.Amount > 0)
            .OrderBy(t => t.Date)
            .ToListAsync();

        var returns = new List<double>();

        foreach (var sellTrade in sellTrades)
        {
            // 판매 이전에 매수 이력을 찾는다.
            var buyTrades = await _dbContext.TradeHistoryRecords
                .Where(t => t.Symbol == sellTrade.Symbol && t.OrderType.Equals("Buy") && t.Date <= sellTrade.Date)
                .OrderBy(t => t.Date)
                .ToListAsync();

            var totalBuyAmount = buyTrades.Sum(bt => bt.Amount);
            var totalBuyCost = buyTrades.Sum(bt => bt.Price * bt.Amount);
            var averageBuyPrice = totalBuyAmount > 0 ? totalBuyCost / totalBuyAmount : 0;

            if (averageBuyPrice > 0)
            {
                // 진입점 계산
                var tradeReturn = (sellTrade.Price - averageBuyPrice) / averageBuyPrice;
                returns.Add(tradeReturn);
            }
        }


        var calculator = new PerformanceMetricsCalculator(returns);
        return new PerformanceReport
        {
            CumulativeReturn = calculator.CalculateCumulativeReturn(),
            AnnualizedReturn = calculator.CalculateAnnualizedReturn(),
            SharpeRatio = calculator.CalculateSharpeRatio(riskFreeRate: 0.01),
            MaxDrawdown = calculator.CalculateMaxDrawdown()
        };
    }

    public async Task UpdateReasoningRecordAsync(string ticker, DateTime date)
    {
        var data = await _dbContext.ReasoningRecords.FirstOrDefaultAsync(p => p.Ticker == ticker);
        if (data == null)
        {
            data = new ReasoningRecord
            {
                Ticker = ticker,
                LastReasoningTime = date,
            };
            _dbContext.ReasoningRecords.Add(data);
        }

        data.LastReasoningTime = date;
        await _dbContext.SaveChangesAsync();
    }

    public async Task<ReasoningRecord?> GetReasoningRecordAsync(string ticker)
    {
        return await _dbContext.ReasoningRecords.FirstOrDefaultAsync(p => p.Ticker == ticker);
    }

    public async Task CleanAsync()
    {
        _dbContext.TradeHistoryRecords.RemoveRange(_dbContext.TradeHistoryRecords);
        _dbContext.Positions.RemoveRange(_dbContext.Positions);
        _dbContext.ReasoningRecords.RemoveRange(_dbContext.ReasoningRecords);
        await _dbContext.SaveChangesAsync();
    }
}
