using TradingAgent.Agents.Messages.ResearchTeam;
using TradingAgent.Core.Config;

namespace TradingAgent.Agents.State;

public class AgentSharedState
{
    public List<MarketContext> Candidates { get; set; } = new();
    
    public readonly Dictionary<string, ResearchResultResponse> ResearchResults = new();
}