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
using TradingAgent.Core.UpbitClient.Extensions;
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
        var content = "Your [Wallet] information given as below\n";
        content += chance.GeneratePrompt();
        content += "You can only buy amount of BidAccount.Balance - BidAccount.Locked - Fee.BidFee\n";
        content += "You can only sell amount of AskAccount.Balance - AskAccount.Locked - Fee.AskFee\n";
        content += "When you decide to buy or sell, you must trade at least 50,000 KRW.\n";
        content += "If you decide to buy, you must use BidAccount.Balance - BidAccount.Locked amount.\n";
        content += "If you decide to sell, you must use AskAccount.Balance - AskAccount.Locked amount.\n";
        content += "[Analyst's Summary]\n";
        content += "1. Market Summary\n";
        content += $"{item.MarketOverview}\n";
        content += "2. Technical Analysis\n";
        content += $"{item.TechnicalAnalysis}\n";
        content += "3. Sentiment, 0 means StrongBuy, 1 means Buy, 2 means Neutral, 3 means Sell, 4 means StrongSell\n";
        content += $"{item.AnalystSentiment}\n";
        content += "4. Target Price\n";
        content += $"{item.TargetPrice}\n";
        content += "5. Confidence(ranged in [1, 10])\n";
        content += $"{item.Confidence}\n";
        content += "As a CFO, you should make the final decision yourself.\n";
        
        var userMessage = new TextMessage(Role.User, content);
        
        var schemaBuilder = new JsonSchemaBuilder().FromType<AgentFinalDecision>();
        var schema = schemaBuilder.Build();
        var reply = await this.agent.GenerateReplyAsync(
            messages: [userMessage],
            options: new GenerateReplyOptions
            {
                OutputSchema = schema,
            });
        
        var response = JsonSerializer.Deserialize<AgentFinalDecision>(reply.GetContent());
        
        this._logger.LogInformation("CfoAgent received AgentFinalDecision: {DecisionType}, {Amount}, {Reason}", response.DecisionType, response.Amount, response.Reason);
    }
}