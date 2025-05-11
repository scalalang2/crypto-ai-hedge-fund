using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
using TradingAgent.Agents.Messages.AnalysisTeam;
using TradingAgent.Core.Config;

namespace TradingAgent.Agents.Agents.AnalysisTeam;

[TypeSubscription(nameof(TechnicalAnalystAgent))]
public class TechnicalAnalystAgent : 
    BaseAgent,
    IHandle<StartAnalysisRequest>
{
    private readonly AppConfig _config;
    private readonly AutoGen.Core.IAgent _agent;
    
    public TechnicalAnalystAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config) : base(id, runtime, "Technical Analyst", logger)
    {
        this._config = config;
        
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.WorkerAIModel);
        this._agent = new OpenAIChatAgent(
                chatClient: client, 
                name: "Techincal Analyst Agent", 
                systemMessage: "")
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public ValueTask HandleAsync(StartAnalysisRequest item, MessageContext messageContext)
    {
        throw new NotImplementedException();
    }
}