using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
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
    private readonly AutoGen.Core.IAgent _agent;
        
    public GateKeeperAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config) : base(id, runtime, AgentName, logger)
    {
        this._config = config;
        
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.FastAIModel);
        this._agent = new OpenAIChatAgent(
                chatClient: client, 
                name: AgentName, 
                systemMessage: "")
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public ValueTask HandleAsync(StartAnalysisRequest item, MessageContext messageContext)
    {
        throw new NotImplementedException();
    }
}