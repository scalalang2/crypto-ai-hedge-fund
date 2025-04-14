using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Discord.WebSocket;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using TradingAgent.Agents.Config;
using TradingAgent.Agents.Messages;
using TradingAgent.Agents.Tools;
using IAgent = AutoGen.Core.IAgent;

namespace TradingAgent.Agents;

[TypeSubscription(nameof(FundManagerAgent))]
public class FundManagerAgent : BaseAgent,
    IHandle<InitMessage>
{
    private readonly IAgent reasoner;
    private readonly IAgent actor;
    private readonly IAgent trader;
    private readonly ILogger<FundManagerAgent> logger;
    private readonly DiscordSocketClient discordClient;
    private readonly LLMConfiguration config;
    
    private readonly int maxSteps = 5;
    private readonly Dictionary<string, Func<string, Task<string>>> actorFunctionMap;
    private readonly Dictionary<string, Func<string, Task<string>>> traderFunctionMap;

    private const string Prompt = @"
You're very talented fund manager, your final goal is to make decision to buy, sell or hold.
Now, the only available market is KRW-SOL.
You need to alway preserve 100,000 KRW for trading fee.

Answer the following questions as best you can.
You can invoke the following tools:
{tools}

Use the following format:

Question: the input question you must answer
Thought: you should always think about what to do
Tool: the tool to invoke
Tool Input: the input to the tool
Observation: the invoke result of the tool
... (this process can repeat multiple times, once for each required tool)

Once you have the final answer, provide the final answer in the following format:
Final Answer: Make a decision to buy, sell or hold (execute BuyCoin with amount, SellCoin with amount or do nothing)

Important rules:
- All decisions MUST be based on actual data obtained through tools. Do not rely on assumptions or guesses.
- After each tool use, summarize the observation and explain how it influences your decision-making process.
- Express your thought process clearly at each step, starting with ""Thought:"".
- Do not hallucinate or invent information. Only use data from the tool observations from the actor agent.

What's Tool?
Tool is a function that can be invoked to get information or perform actions.
If you want to invoke a tool, you need to say Tool: {ToolName} and Tool Input: {input}. then do nothing
actor agent will invoke the tool and return the result to you.
";

    private const string ActorPrompt = @"
You're a actor agent, you need to serve as a tool executor.
You need to carefully see the content of the message and decide whether to invoke a tool or not.

You can invoke the following tools:
{tools}
";

    private const string TraderPrompt = @"
    You are a trader agent, you need to serve as a tool executor.
    Your fund manager will send you a message and you need to decide which tool to invoke.

    You can invoke the following tools:
    {tools} and DoNothing
";
    
    public FundManagerAgent(
        DiscordSocketClient discordClient,
        IOptions<LLMConfiguration> config,
        AgentId id, 
        IAgentRuntime runtime, 
        FunctionTools tools,
        ILogger<FundManagerAgent> logger) : base(id, runtime, "FundManagerAgent", logger)
    {
        this.discordClient = discordClient;
        this.logger = logger;
        this.config = config.Value;

        var client = new OpenAIClient(config.Value.OpenAIApiKey).GetChatClient(config.Value.Model);

        this.actorFunctionMap = new Dictionary<string, Func<string, Task<string>>>
        {
            { nameof(tools.GetMyPortfolio), tools.GetMyPortfolioWrapper },
            { nameof(tools.GetOrderHistory), tools.GetOrderHistoryWrapper },
            // { nameof(tools.Get30MinuteCandlestickData), tools.Get30MinuteCandlestickDataWrapper },
            // { nameof(tools.GetDayCandlestickData), tools.GetDayCandlestickDataWrapper },
            { nameof(tools.Get60MinuteCandlestickData), tools.Get60MinuteCandlestickDataWrapper },
        };
        
        this.traderFunctionMap = new Dictionary<string, Func<string, Task<string>>>
        {
            { nameof(tools.BuyCoin), tools.BuyCoinWrapper },
            { nameof(tools.SellCoin), tools.SellCoinWrapper },
        };
        
        var allTools = this.actorFunctionMap.Keys.Concat(this.traderFunctionMap.Keys);
        var prompt = Prompt.Replace("{tools}", string.Join(", ", allTools));
        this.reasoner = new OpenAIChatAgent(client, "fundManager", systemMessage: prompt)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
        
        var actorPrompt = ActorPrompt.Replace("{tools}", string.Join(", ", this.actorFunctionMap.Keys));
        this.actor = new OpenAIChatAgent(client, "actor", systemMessage: actorPrompt)
            .RegisterMessageConnector()
            .RegisterMiddleware(new FunctionCallMiddleware(
                functions: [
                    tools.GetMyPortfolioFunctionContract,
                    tools.GetOrderHistoryFunctionContract,
                    // tools.Get30MinuteCandlestickDataFunctionContract,
                    // tools.GetDayCandlestickDataFunctionContract,
                    tools.Get60MinuteCandlestickDataFunctionContract,
                ],
                functionMap: this.actorFunctionMap))
            .RegisterPrintMessage();
        
        var traderPrompt = TraderPrompt.Replace("{tools}", string.Join(", ", this.traderFunctionMap.Keys));
        this.trader = new OpenAIChatAgent(client, "trader", systemMessage: traderPrompt)
            .RegisterMessageConnector()
            .RegisterMiddleware(new FunctionCallMiddleware(
                functions: [
                    tools.BuyCoinFunctionContract,
                    tools.SellCoinFunctionContract,
                ],
                functionMap: this.traderFunctionMap))
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(InitMessage item, MessageContext messageContext)
    {
        this._logger.LogInformation("FuncManager agent started test");
        
        var promptMessage = new TextMessage(Role.User, "Please help me grow my money");
        var chatHistory = new List<IMessage> { promptMessage };

        for (int i = 0; i < this.maxSteps; i++)
        {
            var reasoning = await reasoner.GenerateReplyAsync(chatHistory);
            var reasoningContent = reasoning.GetContent();
            _logger.LogInformation("Reasoning step {Step}: {Content}", i, reasoningContent);
            
            if (reasoningContent.Contains("Final Answer:") || reasoningContent.Contains("Final Decision:"))
            {
                // 최종 답변 추출 및 반환
                var finalAnswer = this.ExtractFinalAnswer(reasoningContent);
                _logger.LogInformation("Final answer: {FinalAnswer}", finalAnswer);
                var finalAction = await trader.GenerateReplyAsync(messages: [reasoning]);
                this._logger.LogInformation("Action {Action}: {finalAction}", i, finalAction);
                
                chatHistory.Add(reasoning);
                chatHistory.Add(finalAction);
                
                var channel = await this.discordClient.GetChannelAsync(this.config.DiscordChannelId) as SocketTextChannel;
                if (channel != null)
                {
                    await channel.SendMessageAsync($"**Summary**\n{reasoningContent}");
                }
                else
                {
                    _logger.LogError("Discord channel not found.");
                }
                break;
            }
            
            var action = await actor.GenerateReplyAsync(messages: [reasoning]);
            chatHistory.Add(reasoning);
            chatHistory.Add(action);
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