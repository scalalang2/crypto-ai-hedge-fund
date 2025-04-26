using System.Text;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Discord.WebSocket;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
using TradingAgent.Agents.Messages;
using TradingAgent.Agents.Services;
using TradingAgent.Core.Config;
using IAgent = AutoGen.Core.IAgent;

namespace TradingAgent.Agents.Agents;

/// <summary>
/// 감정 분석 분석 에이전트
/// </summary>
[TypeSubscription(nameof(SummarizerAgent))]
public class SummarizerAgent : BaseAgent, IHandle<SummaryRequest>
{
    private readonly AppConfig config;
    private readonly IMessageSender _messageSender;
    private readonly IAgent _agent;

    private const string Prompt = @"
You are a summarizer agent.
Your task is to summarize the messages at most 10-15 lines.
";
    
    public SummarizerAgent(
        IMessageSender messageSender,
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config) : base(id, runtime, "summarizer", logger)
    {
        this.config = config;
        this._messageSender = messageSender;
        
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.WorkerAIModel);
        this._agent = new OpenAIChatAgent(client, "Summarizer", systemMessage: Prompt)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(SummaryRequest item, MessageContext messageContext)
    {
        var prompt = $"""
Please summarize the following messages:

Please notice that:
1. When the portfolio manager decided to buy, then the unit of quantity is KRW otherwise the unit of quantity is asset.

{item.Message}
""";
        
        var message = new TextMessage(Role.User, prompt);
        var result = await this._agent.GenerateReplyAsync(messages: [message]);
        var summary = result.GetContent();
        
        if (string.IsNullOrEmpty(summary))
        {
            throw new Exception("Summarizer agent failed to generate a summary.");
        }
        
        await this._messageSender.SendMessage(summary);
    }
}