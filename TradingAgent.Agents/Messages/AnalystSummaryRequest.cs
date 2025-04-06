namespace TradingAgent.Agents.Messages;

/// <summary>
/// A request to the analyst agent to summarize the current market conditions.
/// </summary>
public class AnalystSummaryRequest
{
    public string Market { get; set; } = string.Empty;
}