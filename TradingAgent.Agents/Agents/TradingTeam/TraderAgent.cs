using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
using TradingAgent.Agents.Messages.AnalysisTeam;
using TradingAgent.Agents.Messages.ResearchTeam;
using TradingAgent.Agents.Messages.TradingTeam;
using TradingAgent.Core.Config;

namespace TradingAgent.Agents.Agents.TradingTeam;

[TypeSubscription(nameof(TraderAgent))]
public class TraderAgent :
    BaseAgent,
    IHandle<AdjustedTransactionMessage>,
    IHandle<ResearchResultResponse>
{
    private const string AgentName = "Trader Agent";
    
    private readonly AppConfig _config;
    private readonly AutoGen.Core.IAgent _agent;
    private readonly Dictionary<string, ResearchResultResponse> _researchResult = new();
        
    public TraderAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config) : base(id, runtime, AgentName, logger)
    {
        this._config = config;
            
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.FastAIModel);
        this._agent = new OpenAIChatAgent(
                chatClient: client, 
                name: AgentName, 
                systemMessage: "")
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
        
        // make a proposal and send to the risk manager
        var response = new ProposeTransactionMessage();
        await this.PublishMessageAsync(response, new TopicId(nameof(RiskManagerAgent)));
    }
}