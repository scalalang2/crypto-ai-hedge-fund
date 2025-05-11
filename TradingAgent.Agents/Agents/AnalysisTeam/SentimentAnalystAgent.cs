using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
using TradingAgent.Core.Config;

namespace TradingAgent.Agents.Agents.AnalysisTeam;

public class SentimentAnalystAgent :
    BaseAgent
{
    private const string AgentName = "Sentiment Analyst Agent";
    
    private readonly AppConfig _config;
    private readonly AutoGen.Core.IAgent _agent;
        
    public SentimentAnalystAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config) : base(id, runtime, AgentName, logger)
    {
        this._config = config;
            
        var client = new OpenAIClient(config.FastAIModel).GetChatClient(config.FastAIModel);
        this._agent = new OpenAIChatAgent(
                chatClient: client, 
                name: AgentName, 
                systemMessage: "")
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }
}