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
public class SummarizerAgent : BaseAgent, 
    IHandle<SummaryRequest>
{
    private readonly AppConfig config;
    private readonly IMessageSender _messageSender;
    private readonly IAgent _agent;
    
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
        this._agent = new OpenAIChatAgent(client, "Summarizer")
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(SummaryRequest item, MessageContext messageContext)
    {
        var prompt = $"""
Please translate the given message into Korean.

Please use the following format:
**{messageContext.Sender!.Value.Type}**
- Ticker 1: 최종 결정 **매수**, 수량: **800000**, 신뢰도: 85.6, 이유: Ticker 1 시장은 강한 상승 모멘텀을 보이며 매수량이 증가하고 있으며, MACD와 같은 기술 지표가 긍정적이고 RSI가 과매도 상태가 아니므로 성장 가능성이 있습니다. 지금 매수하면 이 상승 추세를 활용할 수 있습니다. 리스크 관리와 포트폴리오 균형 유지를 위해 800,000 KRW로 매수에 한정합니다
- Ticker 2: ...
- Ticker 3: ...

The message is 
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