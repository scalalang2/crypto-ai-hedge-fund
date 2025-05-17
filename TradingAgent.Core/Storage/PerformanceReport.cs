namespace TradingAgent.Core.Storage;

public class PerformanceReport
{
    public double CumulativeReturn { get; set; }
    public double AnnualizedReturn { get; set; }
    public double SharpeRatio { get; set; }
    public double MaxDrawdown { get; set; }
}