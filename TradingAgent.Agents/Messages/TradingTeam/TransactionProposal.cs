using Json.Schema.Generation;

namespace TradingAgent.Agents.Messages.TradingTeam;

[Title("transaction-proposal")]
public class TransactionProposal
{
    [Description("Ticker is the ticker symbol of the asset to be traded")]
    public string Ticker { get; set; } = string.Empty;
    
    [Description("Action is the action to be taken, either Buy, Sell or Hold")]
    public string Action { get; set; } = string.Empty;
    
    [Description("Quantity is the amount to be traded. If you decided to buy, then you MUST specify the amount in KRW, If you decided to sell, you MUST specify the amount of the asset.")]
    public double Quantity { get; set; } = 0.0;
    
    [Description("Confidence is a number between 0 and 100, where 100 means you are very confident in your analysis")]
    public double Confidence { get; set; } = 0.0;
    
    [Description("An explanation of reasoning including any relevant indicators or patterns you observed.")]
    public string Reasoning { get; set; } = string.Empty;
}