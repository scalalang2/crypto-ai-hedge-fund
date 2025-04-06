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
[Title("AnalystSummaryResponse")]
public class AnalystSummaryResponse
{
    [JsonPropertyName("marketOverview")]
    [Description("summarize market")]
    [Required]
    public string MarketOverview { get; set; } = string.Empty;
    
    [JsonPropertyName("technicalAnalysis")]
    [Description("express your analysis result")]
    [Required]
    public string TechnicalAnalysis { get; set; } = string.Empty;
    
    [JsonPropertyName("confidence")]
    [Description("score your confidence level in the interval [1, 10]")]
    [Required]
    public int Confidence { get; set; }
    
    [JsonPropertyName("targetPrice")]
    [Description("target price you thought that the price should be")]
    [Required]
    public long TargetPrice { get; set; }
    
    [JsonPropertyName("analystSentiment")]
    [Description("Analyst sentiment is an integer value between 1 and 5, represent StrongBuy = 0, Buy = 1, Neutral = 2, Sell = 3, StrongSell = 4")]
    [Required]
    public int AnalystSentiment { get; set; } = new();
}