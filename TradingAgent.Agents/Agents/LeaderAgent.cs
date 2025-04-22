using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
using TradingAgent.Agents.Messages;
using TradingAgent.Core.Config;

namespace TradingAgent.Agents.Agents;

/// <summary>
/// 여러 사람들의 의견을 받으며, 최종 결정을 내리는 에이전트
/// </summary>
[TypeSubscription(nameof(LeaderAgent))]
public class LeaderAgent : BaseAgent,
    IHandle<InitMessage>,
    IHandle<MarketAnalyzeResponse>
{
    private readonly AppConfig config;
    private const string Prompt = @"
You're LEADER Agent and alos very talented financial decision maker.
You'll be given a current portfolio of crypto assets and a list of other agents' opinions.

Your task is to analyze the opinions and make a final decision on whether to buy, sell, or hold each asset in the portfolio.
You should consider the opinions of other agents, but ultimately make the final decision yourself.

## Opinions
Opinions are will be given in the following format:

[OPINION]
- Agent Name: The name of the agent providing the opinion
- Opinion by (Agent Name): The opinion of the agent (e.g., Bullish, Confidence Level : 80% and etc)

## Use the following format:
[THOUGHT]
- Question: make a question yourself
- Thought: your thought given the question
( this process can repeat multiple times if needed )
- Final Answer: Make a decision to buy, sell or hold (execute BuyCoin with amount and market, SellCoin with amount and market or do nothing)

## Example
[THOUGHT]

Step 1:
- Question: Should I perceive the current market trend as bullish or bearish?
- Thought: The market trend is currently bullish, as indicated by the recent price increase and positive sentiment from other agents.

Step 2:
- Question: How much should I buy in KRW-BTC?
- Thought: I have 100,000 KRW available for investment, and I believe that investing 50% in KRW-BTC is a good strategy given the current market conditions.

- Final Answer: Buy KRW-BTC with 50,000 KRW
";

    private MarketAnalyzeResponse _marketAnalyze = new();
    
    public LeaderAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        string description, 
        ILogger<BaseAgent> logger, 
        AppConfig config) : base(id, runtime, description, logger)
    {
        this.config = config;
        
        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.LeaderAIModel);
        var agent = new OpenAIChatAgent(client, "Leader", systemMessage: Prompt)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(InitMessage item, MessageContext messageContext)
    {
        this._marketAnalyze = new MarketAnalyzeResponse();
        await this.PublishMessageAsync(new MarketAnalyzeRequest(), new TopicId(nameof(MarketAgent)));
    }
    
    public ValueTask HandleAsync(MarketAnalyzeResponse item, MessageContext messageContext)
    {
        throw new NotImplementedException();
    }
}