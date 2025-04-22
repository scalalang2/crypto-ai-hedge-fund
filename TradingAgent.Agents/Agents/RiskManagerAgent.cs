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
/// 리스크 매니저 에이전트
/// 현재 보유 포지션과 결정에 대한 리스크를 관리하는 에이전트이다
/// </summary>
[TypeSubscription(nameof(RiskManagerAgent))]
public class RiskManagerAgent : BaseAgent
{
    private readonly AppConfig config;

    private const string Prompt = @"
";
    
    public RiskManagerAgent(
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