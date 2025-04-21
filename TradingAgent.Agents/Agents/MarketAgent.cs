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
/// 시장 분석 에이전트
/// </summary>
[TypeSubscription(nameof(MarketAgent))]
public class MarketAgent : BaseAgent
{
    private readonly AppConfig config;

    private const string Prompt = @"
";
    
    public MarketAgent(
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