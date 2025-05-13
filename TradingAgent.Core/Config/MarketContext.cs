namespace TradingAgent.Core.Config;

public class MarketContext
{
    /// <summary>
    /// The ticker symbol of the asset (e.g., "KRW-BTC").
    /// </summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>
    /// The full name of the asset. (e.g. "Bitcoin")
    /// </summary>
    public string Name { get; set; } = string.Empty;
}