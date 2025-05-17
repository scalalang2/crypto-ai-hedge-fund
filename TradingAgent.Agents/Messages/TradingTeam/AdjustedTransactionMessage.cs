using Json.Schema.Generation;

namespace TradingAgent.Agents.Messages.TradingTeam;

[Title("adjusted-transaction-proposal")]
public class AdjustedTransactionProposal
{
    [Description("Summary of the risk manager's assessment and the overall reasoning for the adjustments.")]
    public string RiskAssessmentSummary { get; set; } = string.Empty;

    [Description("The list of adjusted transaction proposals after risk management review.")]
    public List<TransactionProposal> Recommendations { get; set; } = new();
}
