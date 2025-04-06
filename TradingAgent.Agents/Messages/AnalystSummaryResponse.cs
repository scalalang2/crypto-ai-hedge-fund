using System.Text.Json.Serialization;
using Json.Schema.Generation;

namespace TradingAgent.Agents.Messages;

/// <summary>
/// 애널리스트 감정
/// </summary>
public enum AnalystSentiment
{
    StrongBuy,
    Buy,
    Hold,
    Sell,
    StrongSell
}

/// <summary>
/// 트레이딩 애널리스트의 매수/매도 의견
/// </summary>
///
/// <summary>
/// Buy/Sell opinions of a trading analyst
/// </summary>
///
[Title("AnalystSummaryResponse")]
public class AnalystSummaryResponse
{
    [JsonPropertyName("marketOverview")]
    [Description("Current market conditions")]
    [Required]
    public string MarketOverview { get; set; } = string.Empty;
    
    [JsonPropertyName("technicalAnalysis")]
    [Description("Results of technical analysis")]
    [Required]
    public string TechnicalAnalysis { get; set; } = string.Empty;
    
    [JsonPropertyName("confidence")]
    [Description("Indicates the confidence level in the results, expressed as an integer from 1 to 10, where higher numbers indicate greater confidence.")]
    [Required]
    public int Confidence { get; set; }
    
    [JsonPropertyName("targetPrice")]
    [Description("The target price as estimated by the analyst.")]
    [Required]
    public long TargetPrice { get; set; }
    
    [JsonPropertyName("analystSentiment")]
    [Description("The analyst's buy/sell opinion, expressed as a number: 0 means StrongBuy, 1 means Buy, 2 means Hold, 3 means Sell, and 4 means StrongSell.")]
    [Required]
    public int AnalystSentiment { get; set; } = new();
}
