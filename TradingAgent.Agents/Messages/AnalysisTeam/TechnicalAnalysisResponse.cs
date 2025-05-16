using Json.Schema.Generation;
using TradingAgent.Core.Config;

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
}

public class TechnicalAnalysisResponse
{
    public MarketContext MarketContext { get; set; } = new();

    public TechnicalAnalysisResult OneHourCandleAnalysis { get; set; } = new();
    public TechnicalAnalysisResult FourHourCandleAnalysis { get; set; } = new();
    public TechnicalAnalysisResult DayCandleAnalysis { get; set; } = new();
}