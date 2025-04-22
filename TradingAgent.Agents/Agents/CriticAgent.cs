using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
using TradingAgent.Core.Config;

namespace TradingAgent.Agents.Agents;

/// <summary>
/// 리더의 결정 사항에 대해 리뷰하고, 피드백을 주는 에이전트
/// </summary>
[TypeSubscription(nameof(CriticAgent))]
public class CriticAgent : BaseAgent
{
    private readonly AppConfig config;

    private const string Prompt = @"
";
    
    public CriticAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config) : base(id, runtime, "critic", logger)
    {
        this.config = config;
        
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.LeaderAIModel);
        var agent = new OpenAIChatAgent(client, "critic", systemMessage: Prompt)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }
}