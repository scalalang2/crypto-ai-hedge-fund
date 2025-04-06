using AutoGen.OpenAI;
using Discord.WebSocket;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using TradingAgent.Agents.Messages;

namespace TradingAgent.Agents;

[TypeSubscription(nameof(TraderAgent))]
public class TraderAgent : 
    BaseAgent,
    IHandle<AnalystSummaryRequest>
{
    public TraderAgent(
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