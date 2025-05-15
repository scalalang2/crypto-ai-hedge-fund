using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
using TradingAgent.Agents.Agents.AnalysisTeam;
using TradingAgent.Agents.Messages.AnalysisTeam;
using TradingAgent.Core.Config;

namespace TradingAgent.Agents.Agents;

[TypeSubscription(nameof(GateKeeperAgent))]
public class GateKeeperAgent :
    BaseAgent,
    IHandle<StartAnalysisRequest>
{
    private const string AgentName = "GateKeeper Agent";
    private readonly AppConfig _config;
        
    public GateKeeperAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config) : base(id, runtime, AgentName, logger)
    {
        this._config = config;
    }

    public async ValueTask HandleAsync(StartAnalysisRequest item, MessageContext messageContext)
    {
        await this.PublishMessageAsync(item, new TopicId(nameof(NewsAnalystAgent)));
        await this.PublishMessageAsync(item, new TopicId(nameof(SentimentAnalystAgent)));
        await this.PublishMessageAsync(item, new TopicId(nameof(TechnicalAnalystAgent)));
    }
}