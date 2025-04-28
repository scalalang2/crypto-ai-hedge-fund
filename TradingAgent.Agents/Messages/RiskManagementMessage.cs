namespace TradingAgent.Agents.Messages;

public class RiskManagementMessage
{
    public string CurrentPortfolio { get; set; } = string.Empty;
    
    public string CurrentPrice { get; set; } = string.Empty;
    
    public FinalDecisionMessage FinalDecisionMessage { get; set; } = new();
}