using System.Text.Json;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Json.Schema;
using Json.Schema.Generation;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using TradingAgent.Agents.Config;
using TradingAgent.Agents.Messages;
using TradingAgent.Core.UpbitClient;
using IAgent = AutoGen.Core.IAgent;

namespace TradingAgent.Agents;

[TypeSubscription(nameof(CfoAgent))]
public class CfoAgent : 
    BaseAgent,
    IHandle<InitMessage>,
    IHandle<AnalystSummaryResponse>
{
    private readonly IAgent agent;
    private readonly IUpbitClient upbitClient;

    public CfoAgent(
        AgentId id,
        IUpbitClient upbitClient,
        IAgentRuntime runtime,
        ILogger<CfoAgent> logger,
        IOptions<LLMConfiguration> config) : base(id, runtime, "Trading Analysis Agent", logger)
    {
        this.upbitClient = upbitClient;
        var client = new OpenAIClient(config.Value.OpenAIApiKey).GetChatClient(config.Value.Model);
        var systemMessage = @"
You are a professional CFO agent responsible for managing the user's assets. Your primary role is to analyze the user's current financial status, investment portfolio, 
average purchase price information, and investment analysts' opinions to provide expert advice on buy/sell decisions.\n\n

## Your Responsibilities:\n
1. Analyze the user's current financial status and portfolio\n
2. Compare the average purchase price of each investment asset with the current market price\n

## Decision-Making Process:\n
1. Objective Data Collection: Financial status, average purchase price, market price, analysts' opinions\n
2. Risk Assessment: Analyze the risk level and diversification of the current portfolio\n
3. Opportunity Analysis: Calculate potential profit and loss scenarios\n
4. Market Timing Consideration: you're specialized in day trading so that you want to sell it if you buy after 1 to 3 hours. 

Always prioritize the user's financial benefit.";
        
        this.agent = new OpenAIChatAgent(
            chatClient: client,
            name: "cfo agent",
            systemMessage: systemMessage)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }
    
    public async ValueTask HandleAsync(InitMessage item, MessageContext messageContext)
    {
        _logger.LogInformation("CfoAgent received InitMessage: {Market}", item.Market);

        var request = new AnalystSummaryRequest
        {
            Market = item.Market,
        };

        await this.PublishMessageAsync(request, new TopicId(nameof(TradingAnalystAgent)));
    }

    public async ValueTask HandleAsync(AnalystSummaryResponse item, MessageContext messageContext)
    {
        var request = new Chance.Request();
        request.market = "KRW-ETH";
        var chance = await this.upbitClient.GetChance(request);
        var chanceAsJson = JsonSerializer.Serialize(chance);
        var chanceSchemaBuilder = new JsonSchemaBuilder().FromType<Chance.Response>();
        var chanceSchema = chanceSchemaBuilder.Build();
        var chanceSchemaAsJson = JsonSerializer.Serialize(chanceSchema);
        
        var message = $"[Order Availability Schema]\n{chanceSchemaAsJson}\n";
        message += $"[Order Availability Data]\n{chanceAsJson}\n\n";
        
        var summaryAsJson = JsonSerializer.Serialize(item);
        var summarySchemaBuilder = new JsonSchemaBuilder().FromType<AnalystSummaryResponse>();
        var summarySchema = summarySchemaBuilder.Build();
        var summarySchemaAsJson = JsonSerializer.Serialize(summarySchema);
        message += $"[Analyst's Schema]\n{summarySchemaAsJson}\n";
        message += $"[Analyst's Summary]\n{summaryAsJson}\n";
        message += "Please make the final decision.\n";
        
        _logger.LogInformation("Input Message for AnalystSummary {message}", message);
        
        var userMessage = new TextMessage(Role.User, message);
        
        var schemaBuilder = new JsonSchemaBuilder().FromType<AgentFinalDecision>();
        var schema = schemaBuilder.Build();
        var reply = await this.agent.GenerateReplyAsync(
            messages: [userMessage],
            options: new GenerateReplyOptions
            {
                OutputSchema = schema,
            });
        
        JsonSerializer.Deserialize<AgentFinalDecision>(reply.GetContent());
    }
}