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
    [Description("SELL or BUY")]
    [Required]
    public string DecisionType { get; set; } = string.Empty;
    
    [JsonPropertyName("Amount")]
    [Description("amount to buy or sell")]
    [Required]
    public double Amount { get; set; } = 0.0;
}