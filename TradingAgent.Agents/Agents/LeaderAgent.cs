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
You're LEADER Agent and alos very talented financial decision maker.
You'll be given a current portfolio of crypto assets and a list of other agents' opinions.

Your task is to analyze the opinions and make a final decision on whether to buy, sell, or hold each asset in the portfolio.
You should consider the opinions of other agents, but ultimately make the final decision yourself.

## Important Notes
- You MUST keep at least 100,000 KRW in your account.
- You MUST not make any decisions that would result in a loss of more than 10% of your total portfolio value.
- Your profit target is 10% of your total portfolio value.

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
        this._marketAnalyze = new MarketAnalyzeResponse();
        await this.PublishMessageAsync(new MarketAnalyzeRequest(), new TopicId(nameof(MarketAgent)));
    }
    
    public async ValueTask HandleAsync(MarketAnalyzeResponse item, MessageContext messageContext)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Please make a decision based on the following opinions:");
        foreach (var result in item.Results)
        {
            sb.AppendLine($"[{result.Market}]");
            sb.AppendLine($"{result.Analysis}");
            sb.AppendLine($"(Sentiment: {result.Sentiment}, Confidence: {result.Confidence})");
            sb.AppendLine();
        }

        sb.AppendLine("[Portfolio]");
        sb.AppendLine("Market | Amount | Avg Price");
        var totalKrw = 0d;
        foreach (var market in this.config.AvailableMarkets)
        {
            var request = new Chance.Request();
            request.market = market;
            var response = await this._upbitClient.GetChance(request);

            sb.AppendLine($"{market} | {response.ask_account.balance} | {response.ask_account.avg_buy_price}");
            totalKrw = Convert.ToDouble(response.bid_account.balance);
        }

        sb.AppendLine();
        sb.AppendLine($"Available Balance : {totalKrw} KRW");
        sb.AppendLine();
        
        var promptMessage = new TextMessage(Role.User, sb.ToString());
        var chatHistory = new List<IMessage> { promptMessage };
        const int maxStep = 5;
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