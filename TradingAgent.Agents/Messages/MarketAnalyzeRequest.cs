namespace TradingAgent.Agents.Messages;

public enum MarketAnalysisType
{
    DayCandle,
    FourHourCandle,
    HourCandle,
}

/// <summary>
/// 시장 분석 요청
/// </summary>
public class MarketAnalyzeRequest
{
    public MarketAnalysisType AnalysisType { get; set; }
}