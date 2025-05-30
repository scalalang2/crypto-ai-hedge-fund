using System.Text;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using ConsoleTables;
using Json.Schema;
using Json.Schema.Generation;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI;
using TradingAgent.Agents.AgentPrompts;
using TradingAgent.Agents.Agents.Summarizer;
using TradingAgent.Agents.Messages.ResearchTeam;
using TradingAgent.Agents.Messages.TradingTeam;
using TradingAgent.Agents.State;
using TradingAgent.Agents.Utils;
using TradingAgent.Core.Config;
using TradingAgent.Core.TraderClient;

namespace TradingAgent.Agents.Agents.TradingTeam;

[TypeSubscription(nameof(RiskManagerAgent))]
public class RiskManagerAgent :
    BaseAgent,
    IHandle<ProposeTransactionMessage>
{
    private const string AgentName = "Risk Manager Agent";
    
    private readonly AppConfig _config;
    private readonly AutoGen.Core.IAgent _agent;
    private readonly IUpbitClient _upbitClient;
    private readonly AgentSharedState _state;
        
    public RiskManagerAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config, 
        IUpbitClient upbitClient, 
        AgentSharedState state) : base(id, runtime, AgentName, logger)
    {
        this._config = config;
        this._upbitClient = upbitClient;
        this._state = state;

        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.SmartAIModel);
        this._agent = new OpenAIChatAgent(
                chatClient: client, 
                name: AgentName, 
                systemMessage: RiskManagerPrompt.SystemMessage)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(ProposeTransactionMessage item, MessageContext messageContext)
    {
        var candidates = this._state.Candidates;
        
        var tickerResponse = await this._upbitClient.GetTicker(string.Join(",", candidates.Select(market => market.Ticker)));
        var currentPrice = SharedUtils.CurrentTickers(tickerResponse);
        var currentPosition = await SharedUtils.GetCurrentPositionPrompt(this._upbitClient, candidates, tickerResponse);
        
        var chatHistory = new StringBuilder();
        foreach (var (market, result) in _state.ResearchResults)
        {
            var jsonString = JsonConvert.SerializeObject(result.DiscussionHistory);
            chatHistory.AppendLine($"## {result.MarketContext.Ticker} ({result.MarketContext.Name}) Research Result");
            chatHistory.AppendLine($"### Confidence: {jsonString}");
            chatHistory.AppendLine();
        }
        
        var message = RiskManagerPrompt.UserMessage
            .Replace("{research_team_chat_history}", chatHistory.ToString())
            .Replace("{transaction_proposal}", JsonConvert.SerializeObject(item.Proposals))
            .Replace("{current_price}", currentPrice)
            .Replace("{current_position}", currentPosition);
        var userMessage = new TextMessage(Role.User, message);
        
        var schemaBuilder = new JsonSchemaBuilder();
        var schema = schemaBuilder.FromType<AdjustedTransactionProposal>().Build();
        
        var reply = await this._agent.GenerateReplyAsync(
            messages: [userMessage],
            options: new GenerateReplyOptions
            {
                OutputSchema = schema,
            });
        
        var response = JsonConvert.DeserializeObject<AdjustedTransactionProposal>(reply.GetContent());
        if (response == null)
        {
            throw new InvalidOperationException("Failed to parse response from Risk Manager Agent.");
        }
        
        await this.PublishMessageAsync(response, new TopicId(nameof(SummarizerAgent)));
        await this.PublishMessageAsync(response, new TopicId(nameof(TraderAgent)));
    }
}