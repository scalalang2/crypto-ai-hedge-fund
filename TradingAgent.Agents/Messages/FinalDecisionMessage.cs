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
    
    [Description("If you decided to buy, then you MUST specify the amount in KRW, and the amount MUST be at least 50,000 KRW. Do NOT recommend any buy trades below 50,000 KRW.\n If you decided to sell, you MUST specify the amount of the asset, and the total value of the sale MUST be at least 50,000 KRW. Do NOT recommend any sell trades below 50,000 KRW.\n All buy or sell trades must be at least 50,000 KRW. If a trade does not meet this minimum, do not recommend the trade.")]
    public double Quantity { get; set; } = 0.0;
    
    [Description("Confidence is a number between 0 and 100, where 100 means you are very confident in your analysis")]
    public double Confidence { get; set; } = 0.0;
    
    [Description("Final decision is the final decision made by the agent, including any relevant indicators or patterns you observed.")]
    public string Reasoning { get; set; } = string.Empty;
}