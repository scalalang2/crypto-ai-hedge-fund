using AutoGen.OpenAI;
using Discord.WebSocket;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using TradingAgent.Agents.Messages;

namespace TradingAgent.Agents;

[TypeSubscription(nameof(DiscordAgent))]
public class DiscordAgent : 
    BaseAgent,
    IHandle<AnalystSummaryRequest>
{
    private DiscordSocketClient _client;
    
    public DiscordAgent(
        AgentId id,
        IAgentRuntime runtime,
        ILogger<DiscordAgent> logger,
        DiscordSocketClient client) : base(id, runtime, "Trading Analysis Agent", logger)
    {
        this._client = client;
    }

    public ValueTask HandleAsync(AnalystSummaryRequest item, MessageContext messageContext)
    {
        throw new NotImplementedException();
    }
}