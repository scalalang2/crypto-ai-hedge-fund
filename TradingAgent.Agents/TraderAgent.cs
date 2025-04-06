using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using TradingAgent.Agents.Messages;
using TradingAgent.Core.UpbitClient;

namespace TradingAgent.Agents;

[TypeSubscription(nameof(TraderAgent))]
public class TraderAgent : 
    BaseAgent,
    IHandle<AnalystSummaryRequest>
{
    public TraderAgent(
        IUpbitClient upbitClient,
        AgentId id,
        IAgentRuntime runtime,
        ILogger<TraderAgent> logger) : base(id, runtime, "Trading Analysis Agent", logger)
    {
    }

    public ValueTask HandleAsync(AnalystSummaryRequest item, MessageContext messageContext)
    {
        throw new NotImplementedException();
    }
}