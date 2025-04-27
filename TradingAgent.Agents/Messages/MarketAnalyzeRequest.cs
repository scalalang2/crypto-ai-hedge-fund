using Skender.Stock.Indicators;

namespace TradingAgent.Agents.Messages;

public enum QuoteType 
{
    None,
    DayCandle,
    HourCandle,
}

public class MarketData
{
    public string Ticker { get; set; } = string.Empty;
    
    public QuoteType QuoteType { get; set; } = QuoteType.None;
    
    public List<Quote> Quotes { get; set; } = [];
}

/// <summary>
/// 시장 분석 요청
/// </summary>
public class MarketAnalyzeRequest
{
    public List<MarketData> MarketDataList { get; set; } = [];
}