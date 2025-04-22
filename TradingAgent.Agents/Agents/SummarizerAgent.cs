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
    private readonly DiscordSocketClient _discordClient;
    private readonly IAgent _agent;

    private const string Prompt = @"
You are a summarizer agent.
Your task is to summarize the messages at most 10 lines.
";
    
    public SummarizerAgent(
        DiscordSocketClient discordClient,
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config) : base(id, runtime, "summarizer", logger)
    {
        this.config = config;
        _discordClient = discordClient;
        
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.WorkerAIModel);
        this._agent = new OpenAIChatAgent(client, "Summarizer", systemMessage: Prompt)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(SummaryRequest item, MessageContext messageContext)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Please summarize the following messages:");
        sb.AppendLine(item.Message);
        var message = new TextMessage(Role.User, sb.ToString());
        var result = await this._agent.GenerateReplyAsync(messages: [message]);
        var summary = result.GetContent();
        
        var channel = _discordClient.GetChannel(this.config.DiscordChannelId) as SocketTextChannel;
        if (channel != null)
        {
            await channel.SendMessageAsync(summary);
        }
        else
        {
            this._logger.LogError("Failed to find the Discord channel.");
        }
    }
}