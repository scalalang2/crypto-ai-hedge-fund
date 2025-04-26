using Json.Schema.Generation;

namespace TradingAgent.Agents.Messages;

/// <summary>
/// 시장 분석 응답
/// </summary>
public class MarketAnalyzeResponse
{
    public List<(MarketAnalysisResult AnalystResult, string Market, MarketAnalysisType AnalysisType)> Results { get; set; } = [];
}

[Title("marketanalysisresult")]
public class MarketAnalysisResult
{
    [Description("Analysis is a short description of your analysis, including any relevant indicators or patterns you observed.")]
    public string Reasoning { get; set; } = string.Empty;
    
    [Description("Sentiment is either Bullish, Neutral or Bearish")]
    public string Signal { get; set; } = string.Empty;
    
    [Description("Cofidence is a number between 0 and 100, where 100 means you are very confident in your analysis")]
    public double Confidence { get; set; } = 0.0;
}