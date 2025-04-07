using System.Text.Json.Serialization;
using Json.Schema.Generation;

namespace TradingAgent.Agents.Messages;

[Title("AgentFinalDecision")]
public class AgentFinalDecision
{
    [JsonPropertyName("Reason")]
    [Description("why CFO agent made this decision")]
    [Required]
    public string Reason { get; set; } = string.Empty;
    
    [JsonPropertyName("DecisionType")]
    [Description("SELL, BUY or HOLD")]
    [Required]
    public string DecisionType { get; set; } = string.Empty;
    
    [JsonPropertyName("Volume")]
    [Description("Quantity of coins to sell when selling at market price, you must not use this field when buying at market price")]
    [Required]
    public double Volume { get; set; } = 0.0;
    
    [JsonPropertyName("Price")]
    [Description("Purchase price (KRW) when buying at market price, you must not use this field when selling at market price")]
    [Required]
    public double Price { get; set; } = 0.0;
}