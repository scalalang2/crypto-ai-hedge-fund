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
/// 리더의 메시지를 해석하고 실제 트레이딩을 수행하는 에이전트
/// </summary>
[TypeSubscription(nameof(TraderAgent))]
public class TraderAgent : BaseAgent
{
    private readonly AppConfig config;

    private const string Prompt = @"
";
    
    public TraderAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        string description, 
        ILogger<BaseAgent> logger, 
        AppConfig config) : base(id, runtime, description, logger)
    {
        this.config = config;
        
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.LeaderAIModel);
        var agent = new OpenAIChatAgent(client, "Leader", systemMessage: Prompt)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }
}