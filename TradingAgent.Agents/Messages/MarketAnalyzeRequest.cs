namespace TradingAgent.Agents.Messages;

public enum MarketAnalysisType
{
    UseDayCandle,
    Use4HourCandle,
    Use60MinCandle,
}

/// <summary>
/// 시장 분석 요청
/// </summary>
public class MarketAnalyzeRequest
{
    public MarketAnalysisType AnalysisType { get; set; }
}