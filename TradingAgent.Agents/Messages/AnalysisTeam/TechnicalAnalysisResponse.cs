using Json.Schema.Generation;

namespace TradingAgent.Agents.Messages.AnalysisTeam;

[Title("technical-analysis-result")]
public class TechnicalAnalysisResult
{
    [Description("Analysis is a short description of your analysis, including any relevant indicators or patterns you observed.")]
    public string Reasoning { get; set; } = string.Empty;
    
    [Description("Sentiment is either Bullish, Neutral or Bearish")]
    public string Signal { get; set; } = string.Empty;
    
    [Description("Confidence is a number between 0 and 100, where 100 means you are very confident in your analysis")]
    public double Confidence { get; set; } = 0.0;
    
    /// <summary>
    /// Ticker is the ticker symbol of the asset to be traded
    /// </summary>
    public string Ticker { get; set; } = string.Empty;
    
    /// <summary>
    /// The full name of the asset. (e.g. "Bitcoin", "Ethereum", "Solana" and etc.)
    /// </summary>
    public string AssetFullName { get; set; } = string.Empty;
}

public class TechnicalAnalysisResponse
{
    /// <summary>
    /// The list of technical analysis results
    /// </summary>
    public List<TechnicalAnalysisResult> TechnicalAnalysisResults { get; set; } = [];
}