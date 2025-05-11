using Json.Schema.Generation;

namespace TradingAgent.Agents.Messages.TradingTeam;

[Title("propose-transaction-message")]
public class ProposeTransactionMessage
{
    [Description("Transaction Proposals")]
    public List<TransactionProposal> Proposals { get; set; } = [];
}