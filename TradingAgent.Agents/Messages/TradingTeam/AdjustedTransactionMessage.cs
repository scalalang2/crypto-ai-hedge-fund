using Json.Schema.Generation;

namespace TradingAgent.Agents.Messages.TradingTeam;

[Title("adjusted-transaction-message")]
public class AdjustedTransactionMessage
{
    [Description("Summary of the risk manager's assessment and the overall reasoning for the adjustments.")]
    public string RiskAssessmentSummary { get; set; } = string.Empty;

    [Description("The list of adjusted transaction proposals after risk management review.")]
    public List<AdjustedTransactionProposal> AdjustedProposals { get; set; } = new();
}

[Title("adjusted-transaction-proposal")]
public class AdjustedTransactionProposal
{
    [Description("The original transaction proposal before adjustment.")]
    public TransactionProposal OriginalProposal { get; set; } = new();

    [Description("The adjusted transaction proposal after risk management review. This may differ from the original in action, quantity, or confidence.")]
    public TransactionProposal AdjustedProposal { get; set; } = new();

    [Description("A detailed explanation of why the proposal was adjusted, including specific risk considerations (e.g., position size limits, diversification, volatility, etc.).")]
    public string AdjustmentReasoning { get; set; } = string.Empty;
}