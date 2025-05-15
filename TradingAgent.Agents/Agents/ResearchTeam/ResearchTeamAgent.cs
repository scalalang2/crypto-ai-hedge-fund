using System.Net;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
using TradingAgent.Agents.Agents.TradingTeam;
using TradingAgent.Agents.Messages.AnalysisTeam;
using TradingAgent.Agents.Messages.ResearchTeam;
using TradingAgent.Core.Config;

namespace TradingAgent.Agents.Agents.ResearchTeam;

[TypeSubscription(nameof(ResearchTeamAgent))]
public class ResearchTeamAgent :
    BaseAgent,
    IHandle<NewsAnalysisResponse>,
    IHandle<SentimentAnalysisResponse>,
    IHandle<TechnicalAnalysisResponse>
{
    private const string AgentName = "Research Team Agent";
    
    private readonly AppConfig _config;
    private readonly AutoGen.Core.IAgent _agent;

    private readonly NewsAnalysisResponse? _newsAnalysisResponse = null;
    private readonly SentimentAnalysisResponse? _sentimentAnalysisResponse = null;
    private readonly TechnicalAnalysisResponse? _technicalAnalysisResponse = null;
        
    public ResearchTeamAgent(
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

    public async ValueTask HandleAsync(NewsAnalysisResponse item, MessageContext messageContext)
    {
        await this.TryStartResearch();
    }

    public async ValueTask HandleAsync(SentimentAnalysisResponse item, MessageContext messageContext)
    {
        await this.TryStartResearch();
    }

    public async ValueTask HandleAsync(TechnicalAnalysisResponse item, MessageContext messageContext)
    {
        await this.TryStartResearch();
    }
    
    private async Task TryStartResearch()
    {
        if (this._newsAnalysisResponse == null ||
            this._sentimentAnalysisResponse == null ||
            this._technicalAnalysisResponse == null)
        {
            return;
        }

        var response = new ResearchResultResponse();
        await this.PublishMessageAsync(response, new TopicId(nameof(TraderAgent)));
    }
}