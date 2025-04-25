using System.Text;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Responses;
using TradingAgent.Agents.Messages;
using TradingAgent.Agents.Tools;
using TradingAgent.Agents.Utils;
using TradingAgent.Core.Config;
using IAgent = AutoGen.Core.IAgent;

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
    private readonly IUpbitClient _upbitClient;
    private readonly AutoGen.Core.IAgent _agent;
    
    private const string Prompt = @"
You are LEADER Agent, an expert financial decision-maker specializing in cryptocurrency markets.
Analyze a given crypto portfolio and other agents' opinions to decide buy/sell/hold actions for each asset.

Your task is to analyze the opinions and make a final decision on whether to buy, sell, or hold each asset in the portfolio.
You should consider the opinions of other agents, but ultimately make the final decision yourself.

## Key Constraints
- Loss Limit: Never risk losing >10% of the total portfolio value.
- Profit Target: Aim for a 10% overall portfolio gain.
- When buying an asset, specify the amount in KRW (e.g., Buy KRW-SOL with 5,000 KRW).
- When selling an asset, specify the amount of the asset (e.g., Sell 0.1 SOL).

## Input Format
[OPINION]
- Agent Name: [Name]
- KRW-BTC Market: [High Bullish/Bullish/High Bearish/Bearish/Neutral], Confidence: [0.0-1.0]
- KRW-BTC Market Reasning: [Reasoning]

## Use the following format:
[THOUGHT]
- Question: make a question yourself
- Thought: your thought given the question
( this process can repeat multiple times if needed )
- Final Answer: Make a final decision on whether to buy, sell, or hold each asset in the portfolio.

## Example
[THOUGHT]

Step 1:
- Question: Should I perceive the current market trend as bullish or bearish?
- Thought: The market trend is currently high bearish.

Step 2:
- Question: How much SOL should I sell to secure 1,000 KRW profit?
- Thought: I have 1.0 SOL, and the price is 50,000 KRW per SOL. Selling 0.02 SOL gives me 1,000 KRW, but since the minimum selling amount is 5,000 KRW, I need to sell enough SOL to get at least 5,000 KRW.

... (This process can repeat multiple times if needed)

- Final Answer: 
# KRW-SOL:
SOL is trading at ₩198,535. Given recent bearish sentiment and agents’ negative outlook, 
I recommend selling a portion of SOL to reduce downside risk and secure profits. 
Therefore, I'll Sell 0.1 SOL 

# KRW-ETH:
Hold the current position. The market sentiment is neutral, and the price is stable.

# KRW-BTC:
Buy 0.0001 BTC. The market sentiment is bullish, and the price is expected to rise.
";

    public LeaderAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        ILogger<BaseAgent> logger, 
        AppConfig config, 
        IUpbitClient upbitClient) : base(id, runtime, "leader", logger)
    {
        this.config = config;
        this._upbitClient = upbitClient;

        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.LeaderAIModel);
        this._agent = new OpenAIChatAgent(client, "Leader", systemMessage: Prompt)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(InitMessage item, MessageContext messageContext)
    {
        await this.PublishMessageAsync(new MarketAnalyzeRequest(), new TopicId(nameof(MarketAgent)));
    }
    
    public async ValueTask HandleAsync(MarketAnalyzeResponse item, MessageContext messageContext)
    {
        var prompt = @"""
Hello Leader Agent, Should I buy, sell, or hold the following assets?

# Market Insights
{market_insight}

# Current Position
{current_position}
""";
        
        var marketInsight = new StringBuilder();
        foreach (var result in item.Results)
        {
            marketInsight.AppendLine($"{result.Market} Market: [{result.Sentiment}], Confidence: {result.Confidence}");
            marketInsight.AppendLine($"{result.Market} Market Reasoning: {result.Analysis}");
            marketInsight.AppendLine();
        }
        
        var currentPosition = await SharedUtils.GetCurrentPositionPrompt(this._upbitClient, this.config.AvailableMarkets);
        prompt = prompt
            .Replace("{market_insight}", marketInsight.ToString())
            .Replace("{current_position}", currentPosition);
        
        var promptMessage = new TextMessage(Role.User, prompt);
        
        this._logger.LogInformation("leader Prompt: {Prompt}", promptMessage.GetContent());
        
        var chatHistory = new List<IMessage> { promptMessage };
        const int maxStep = 10;
        for (var i = 0; i < maxStep; i++)
        {
            var reasoning = await this._agent.GenerateReplyAsync(chatHistory);
            var reasoningContent = reasoning.GetContent();

            if (reasoningContent.Contains("Final Answer:") || reasoningContent.Contains("Final Decision:"))
            {
                // 최종 답변 추출 및 반환
                var finalAnswer = this.ExtractFinalAnswer(reasoningContent);
                _logger.LogInformation("Final answer: {FinalAnswer}", finalAnswer);

                var summaryRequest = new SummaryRequest
                {
                    Message = reasoningContent,
                };
                var tradeRequest = new TradeRequest
                {
                    Message = finalAnswer,
                };
                
                await this.PublishMessageAsync(summaryRequest, new TopicId(nameof(SummarizerAgent)));
                await this.PublishMessageAsync(tradeRequest, new TopicId(nameof(TraderAgent)));
                break;
            }

            chatHistory.Add(reasoning);
        }
    }
    
    private string ExtractFinalAnswer(string content)
    {
        var finalAnswerIndex = content.IndexOf("Final Answer:", StringComparison.Ordinal);
        if (finalAnswerIndex >= 0)
        {
            return content[(finalAnswerIndex + "Final Answer:".Length)..].Trim();
        }
        return "No final answer found.";
    }
}