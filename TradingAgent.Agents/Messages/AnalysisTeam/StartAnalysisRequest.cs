using TradingAgent.Agents.Messages.Shared;

namespace TradingAgent.Agents.Messages.AnalysisTeam;

/// <summary>
/// This initiates the analysis process to analyze the market.
/// </summary>
public class StartAnalysisRequest
{
    public MarketContext MarketContext { get; set; } = new MarketContext();
}