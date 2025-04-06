using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using TradingAgent.Agents.Config;
using TradingAgent.Agents.Messages;
using IAgent = AutoGen.Core.IAgent;

namespace TradingAgent.Agents;

[TypeSubscription(nameof(CfoAgent))]
public class CfoAgent : 
    BaseAgent,
    IHandle<InitMessage>,
    IHandle<AnalystSummaryResponse>
{
    private readonly IAgent agent;

    public CfoAgent(
        AgentId id,
        IAgentRuntime runtime,
        ILogger<CfoAgent> logger,
        IOptions<LLMConfiguration> config) : base(id, runtime, "Trading Analysis Agent", logger)
    {
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
            name: "trading_analyst",
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

    public ValueTask HandleAsync(AnalystSummaryResponse item, MessageContext messageContext)
    {
        _logger.LogInformation("CfoAgent received AnalystSummaryResponse: {Response}", item);
        
        return ValueTask.CompletedTask;
    }
}