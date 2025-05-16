using System.Text;
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
using TradingAgent.Agents.Messages.ResearchTeam;
using TradingAgent.Agents.Messages.TradingTeam;
using TradingAgent.Agents.Utils;
using TradingAgent.Core.Config;
using TradingAgent.Core.TraderClient;

namespace TradingAgent.Agents.Agents.TradingTeam;

[TypeSubscription(nameof(TraderAgent))]
public class TraderAgent :
    BaseAgent,
    IHandle<AdjustedTransactionMessage>,
    IHandle<ResearchResultResponse>
{
    private const string AgentName = "TraderAgent";
    
    private readonly IUpbitClient _upbitClient;
    private readonly AppConfig _config;
    private readonly AutoGen.Core.IAgent _agent;
    private readonly Dictionary<string, ResearchResultResponse> _researchResult = new();
        
    public TraderAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        IUpbitClient upbitClient,
        ILogger<BaseAgent> logger, 
        AppConfig config) : base(id, runtime, AgentName, logger)
    {
        this._upbitClient = upbitClient;
        this._config = config;
            
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.SmartAIModel);
        this._agent = new OpenAIChatAgent(
                chatClient: client, 
                name: AgentName, 
                systemMessage: TraderPrompt.TraderSystemMessage)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public ValueTask HandleAsync(AdjustedTransactionMessage item, MessageContext messageContext)
    {
        // start trading with adjusted transactions

        return ValueTask.CompletedTask;
    }

    public async ValueTask HandleAsync(ResearchResultResponse item, MessageContext messageContext)
    {
        this._logger.LogInformation("[TraderAgent] {sender} {research_result}", messageContext.Sender, item);
        this._researchResult[item.MarketContext.Ticker] = item;
        await this.TryProposeTrade();
    }

    private async Task TryProposeTrade()
    {
        // ensure that all research results are received
        if (this._config.Markets.Any(market => !this._researchResult.ContainsKey(market.Ticker)))
        {
            return;
        }
        
        var tickerResponse = await this._upbitClient.GetTicker(string.Join(",", this._config.Markets.Select(market => market.Ticker)));
        var currentPrice = await SharedUtils.CurrentTickers(tickerResponse);
        var currentPosition = await SharedUtils.GetCurrentPositionPrompt(this._upbitClient, this._config.Markets, tickerResponse);
        var chatHistory = new StringBuilder();

        foreach (var (market, result) in _researchResult)
        {
            var jsonString = JsonConvert.SerializeObject(result.DiscussionHistory);
            chatHistory.AppendLine($"## {result.MarketContext.Ticker} ({result.MarketContext.Name}) Research Result");
            chatHistory.AppendLine($"### Confidence: {jsonString}");
            chatHistory.AppendLine();
        }
        
        var message = TraderPrompt.TraderUserMessage
            .Replace("{research_team_chat_history}", chatHistory.ToString())
            .Replace("{current_price}", currentPrice)
            .Replace("{current_portfolio}", currentPosition);
        
        this._logger.LogInformation("Trader's prompt {message}", message);

        var chatHistories = new List<IMessage>();
        const int maxSteps = 10;
        
        var response = new ProposeTransactionMessage();
        var proposals = new List<TransactionProposal>();
        
        for(var i = 0; i < maxSteps; i++)
        {
            var userMessage = new TextMessage(Role.User, message);
            chatHistories.Add(userMessage);
            var reasoning = await this._agent.SendAsync(chatHistory: chatHistories);
            
            if (reasoning.GetContent() is not string reasoningContent)
            {
                throw new Exception("Failed to get reasoning content");
            }
            
            if (reasoningContent.Contains("[TERMINATE]"))
            {
                var terminateMessage = reasoningContent.Split("[TERMINATE]")[1];
                proposals = JsonConvert.DeserializeObject<List<TransactionProposal>>(terminateMessage);
                if (proposals == null)
                {
                    throw new Exception("Failed to deserialize ProposeTransactionMessage");
                }

                response.Proposals = proposals;
                break;
            }
            chatHistories.Add(reasoning);
        }
        
        // make a proposal and send to the risk manager
        await this.PublishMessageAsync(response, new TopicId(nameof(RiskManagerAgent)));
    }
}