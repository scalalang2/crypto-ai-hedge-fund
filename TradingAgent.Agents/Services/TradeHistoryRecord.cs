namespace TradingAgent.Agents.Services;

/// <summary>
/// Represents one completed buy‐sell cycle.
/// </summary>
public class TradeHistoryRecord
{
    public DateTime Date { get; set; }
    
    public string Ticker { get; set; }

    public double BuyingPrice { get; set; }
    
    public double SellingPrice { get; set; }
    
    public double Amount { get; set; }

    public double ProfitRate => BuyingPrice > 0 ? (SellingPrice - BuyingPrice) / BuyingPrice : 0d;

    public double LossRate => ProfitRate < 0 ? Math.Abs(ProfitRate) : 0d;
}