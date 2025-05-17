using System.Net;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Json.Schema;
using Json.Schema.Generation;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI;
using TradingAgent.Agents.AgentPrompts;
using TradingAgent.Agents.Agents.Summarizer;
using TradingAgent.Agents.Agents.TradingTeam;
using TradingAgent.Agents.Messages.AnalysisTeam;
using TradingAgent.Agents.Messages.ResearchTeam;
using TradingAgent.Agents.Messages.Summarizer;
using TradingAgent.Core.Config;

namespace TradingAgent.Agents.Agents.ResearchTeam;

[TypeSubscription(nameof(ResearchTeamAgent))]
public class ResearchTeamAgent :
    BaseAgent,
    IHandle<NewsAnalysisResponse>,
    IHandle<SentimentAnalysisResponse>,
    IHandle<TechnicalAnalysisResponse>
{
    private const string AgentName = "ResearchTeamAgent";
    
    private readonly AppConfig _config;
    private readonly AutoGen.Core.IAgent _bullishResearcher;
    private readonly AutoGen.Core.IAgent _bearishResearcher;

    private NewsAnalysisResponse? _newsAnalysisResponse = null;
    private SentimentAnalysisResponse? _sentimentAnalysisResponse = null;
    private TechnicalAnalysisResponse? _technicalAnalysisResponse = null;
        
    public ResearchTeamAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config) : base(id, runtime, AgentName, logger)
    {
        this._config = config;
            
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.SmartAIModel);
        this._bullishResearcher = new OpenAIChatAgent(
                chatClient: client, 
                name: AgentName, 
                systemMessage: ResearchTeamPrompt.BullishSystemMessage)
            .RegisterMessageConnector()
            .RegisterPrintMessage();

        this._bearishResearcher = new OpenAIChatAgent(
            chatClient: client,
            name: AgentName,
            systemMessage: ResearchTeamPrompt.BearishSystemMessage)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(NewsAnalysisResponse item, MessageContext messageContext)
    {
        this._newsAnalysisResponse = item;
        await this.TryStartResearch();
    }

    public async ValueTask HandleAsync(SentimentAnalysisResponse item, MessageContext messageContext)
    {
        this._sentimentAnalysisResponse = item;
        await this.TryStartResearch();
    }

    public async ValueTask HandleAsync(TechnicalAnalysisResponse item, MessageContext messageContext)
    {
        this._technicalAnalysisResponse = item;
        await this.TryStartResearch();
    }
    
    private async Task TryStartResearch()
    {
        // if (this._newsAnalysisResponse == null ||
        //     this._sentimentAnalysisResponse == null ||
        //     this._technicalAnalysisResponse == null)
        // {
        //     return;
        // }

        if (this._technicalAnalysisResponse == null)
        {
            return;
        }

        var chatHistory = new List<IMessage>();
        var bullishHistory = new List<IMessage>();
        var bearishHistory = new List<IMessage>();
        var discussionHistory = new List<Discussion>();
        
        var schemaBuilder = new JsonSchemaBuilder();
        var discussionSchema = schemaBuilder.FromType<Discussion>().Build();
        
        // initially makes a bullish analysis
        var hourCandleAnalysis = JsonConvert.SerializeObject(this._technicalAnalysisResponse.OneHourCandleAnalysis);
        var fourHourCandleAnalysis = JsonConvert.SerializeObject(this._technicalAnalysisResponse.FourHourCandleAnalysis);
        var dayCandleAnalysis = JsonConvert.SerializeObject(this._technicalAnalysisResponse.DayCandleAnalysis);
        var message = ResearchTeamPrompt.BullishInitialThinkingPrompt;
        message = message
            .Replace("{ticker}", this._technicalAnalysisResponse.MarketContext.Ticker)
            .Replace("{name}", this._technicalAnalysisResponse.MarketContext.Name)
            .Replace("{hour_technical_analysis_result}", hourCandleAnalysis)
            .Replace("{four_hour_technical_analysis_result}", fourHourCandleAnalysis)
            .Replace("{day_technical_analysis_result}", dayCandleAnalysis);
        
        this._logger.LogInformation("[{name}] {message}", nameof(ResearchTeamAgent), message);

        var initMessage = new TextMessage(Role.User, message);
        chatHistory.Add(initMessage);
        
        var reply = await this._bullishResearcher.GenerateReplyAsync(
            messages: chatHistory,
            options: new GenerateReplyOptions
            {
                OutputSchema = discussionSchema,
            });
        
        chatHistory.Add(reply);
        bullishHistory.Add(reply);
        
        var initialDiscussion = JsonConvert.DeserializeObject<Discussion>(bullishHistory.Last().GetContent());
        if (initialDiscussion == null)
        {
            throw new InvalidOperationException("Failed to deserialize the discussion result.");
        }
        discussionHistory.Add(initialDiscussion);
        
        // Start the discussion with the bearish researcher
        const int maxRounds = 5;
        for (var i = 0; i < maxRounds; i++)
        {
            // bearish
            var lastBullishJson = bullishHistory.Last().GetContent();
            var bearishPrompt = ResearchTeamPrompt.BearishDiscussionPrompt
                .Replace("{last_bullish}", lastBullishJson);
            chatHistory.Add(new TextMessage(Role.User, bearishPrompt));
            
            var bearishReply = await _bearishResearcher.GenerateReplyAsync(
                messages: chatHistory,
                options: new GenerateReplyOptions { OutputSchema = discussionSchema }
            );
            chatHistory.Add(bearishReply);
            bearishHistory.Add(bearishReply);
            
            // bullish
            var lastBearishJson = bearishHistory.Last().GetContent();
            var bullishPrompt = ResearchTeamPrompt.BullishDiscussionPrompt
                .Replace("{last_bearish}", lastBearishJson);
            chatHistory.Add(new TextMessage(Role.User, bullishPrompt));

            var bullishReply = await _bullishResearcher.GenerateReplyAsync(
                messages: chatHistory,
                options: new GenerateReplyOptions { OutputSchema = discussionSchema }
            );
            chatHistory.Add(bullishReply);
            bullishHistory.Add(bullishReply);
            
            var bullishDiscussion = JsonConvert.DeserializeObject<Discussion>(bullishHistory.Last().GetContent());
            if (bullishDiscussion == null)
            {
                throw new InvalidOperationException("Failed to deserialize the discussion result.");
            }
            
            var bearishDiscussion = JsonConvert.DeserializeObject<Discussion>(bearishHistory.Last().GetContent());
            if (bearishDiscussion == null)
            {
                throw new InvalidOperationException("Failed to deserialize the discussion result.");
            }
            discussionHistory.Add(bullishDiscussion);
        }

        var response = new ResearchResultResponse
        {
            MarketContext = this._technicalAnalysisResponse.MarketContext,
            DiscussionHistory = discussionHistory,
        };
        await this.PublishMessageAsync(new SummarizeRequest
        {
            Message = response,
        }, new TopicId(nameof(SummarizerAgent)));
        await this.PublishMessageAsync(response, new TopicId(nameof(TraderAgent)));
    }
}