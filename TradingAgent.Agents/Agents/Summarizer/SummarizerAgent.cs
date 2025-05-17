using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI;
using TradingAgent.Agents.AgentPrompts;
using TradingAgent.Agents.Messages.AnalysisTeam;
using TradingAgent.Agents.Messages.Summarizer;
using TradingAgent.Agents.Messages.TradingTeam;
using TradingAgent.Core.Config;
using TradingAgent.Core.MessageSender;
using Formatting = System.Xml.Formatting;

namespace TradingAgent.Agents.Agents.Summarizer;

[TypeSubscription(nameof(SummarizerAgent))]
public class SummarizerAgent :
    BaseAgent,
    IHandle<AdjustedTransactionProposal>,  
    IHandle<SummarizeRequest> {
    private const string AgentName = "SummarizerAgent";

    private readonly AppConfig _config;
    private readonly AutoGen.Core.IAgent _agent;
    private readonly IMessageSender _messageSender;
    
    public SummarizerAgent(
        AgentId id,
        IAgentRuntime runtime,
        ILogger<BaseAgent> logger,
        AppConfig config, 
        IMessageSender messageSender) : base(id, runtime, AgentName, logger)
    {
        this._config = config;
        this._messageSender = messageSender;

        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.FastAIModel);
        this._agent = new OpenAIChatAgent(
                chatClient: client,
                name: AgentName)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(AdjustedTransactionProposal item, MessageContext messageContext)
    {
        var jsonMessage = JsonConvert.SerializeObject(item);
        var prompt = $"""
                      Please translate the given message into Korean.

                      Please use the following format:
                      **{messageContext.Sender!.Value.Type}**
                      - Ticker 1: 최종 결정 **매수**, 수량: **800000**, 신뢰도: 85.6, 이유: Ticker 1 시장은 강한 상승 모멘텀을 보이며 매수량이 증가하고 있으며, MACD와 같은 기술 지표가 긍정적이고 RSI가 과매도 상태가 아니므로 성장 가능성이 있습니다. 지금 매수하면 이 상승 추세를 활용할 수 있습니다. 리스크 관리와 포트폴리오 균형 유지를 위해 800,000 KRW로 매수에 한정합니다
                      - Ticker 2: ...
                      - Ticker 3: ...

                      The message is 
                      {jsonMessage}
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
    
    public async ValueTask HandleAsync(SummarizeRequest item, MessageContext messageContext)
    {
        var prompt = $"""
                      Please summarize the following message in Korean at most 200 words.

                      The message is 
                      {item.Message}
                      
                      Emphasize the key points and important information. with **<message>** format.
                      
                      Please provide message with the following format:
                      
                      ### {messageContext.Sender}
                      messages ...
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