using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
using TradingAgent.Agents.Messages;
using TradingAgent.Core.Config;

namespace TradingAgent.Agents.Agents;

/// <summary>
/// 여러 사람들의 의견을 받으며, 최종 결정을 내리는 에이전트
/// </summary>
[TypeSubscription(nameof(LeaderAgent))]
public class LeaderAgent : BaseAgent,
    IHandle<InitMessage>
{
    private readonly AppConfig config;

    private const string Prompt = @"
";
    
    public LeaderAgent(
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

    public ValueTask HandleAsync(InitMessage item, MessageContext messageContext)
    {
        return ValueTask.CompletedTask;
    }
}