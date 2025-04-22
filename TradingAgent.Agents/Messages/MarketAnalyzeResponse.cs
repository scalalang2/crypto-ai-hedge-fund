using Json.Schema.Generation;

namespace TradingAgent.Agents.Messages;

/// <summary>
/// 시장 분석 응답
/// </summary>
public class MarketAnalyzeResponse
{
    public List<MarketAnalysisResult> Results { get; set; } = [];
}

[Title("marketanalysisresult")]
public class MarketAnalysisResult
{
    [Description("Market Name")]
    public string Market { get; set; } = string.Empty;
    
    [Description("Analysis is a short description of your analysis, including any relevant indicators or patterns you observed.")]
    public string Analysis { get; set; } = string.Empty;
    
    [Description("Sentiment is either High Bullish, Bullish, Neutral, Bearish, or High Bearish")]
    public string Sentiment { get; set; } = string.Empty;
    
    [Description("Cofidence is a number between 0 and 1, where 1 means you are very confident in your analysis")]
    public double Confidence { get; set; } = 0.0;
}