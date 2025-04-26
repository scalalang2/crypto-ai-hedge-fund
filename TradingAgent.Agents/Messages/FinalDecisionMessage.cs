using Json.Schema.Generation;

namespace TradingAgent.Agents.Messages;

[Title("finaldecisionmessage")]
public class FinalDecisionMessage
{
    [Description("Final Decision Message")]
    public List<FinalDecision> FinalDecisions { get; set; } = [];
}

[Title("Finaldecision")]
public class FinalDecision
{
    [Description("Ticker is the ticker symbol of the asset to be traded")]
    public string Ticker { get; set; } = string.Empty;
    
    [Description("Action is the action to be taken, either Buy, Sell or Hold")]
    public string Action { get; set; } = string.Empty;
    
    [Description("Quantity is the quantity of the asset to be bought or sold")]
    public double Quantity { get; set; } = 0.0;
    
    [Description("Cofidence is a number between 0 and 100, where 100 means you are very confident in your analysis")]
    public double Confidence { get; set; } = 0.0;
    
    [Description("Final decision is the final decision made by the agent, including any relevant indicators or patterns you observed.")]
    public string Reasoning { get; set; } = string.Empty;
}