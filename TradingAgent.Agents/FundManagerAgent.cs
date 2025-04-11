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
    private readonly IAgent agent;
    private readonly IAgent reasoner;
    private readonly IAgent actor;
    private readonly ILogger<FundManagerAgent> logger;
    
    private readonly int maxSteps = 10;
    
    private const string Prompt = @"
You're very talented fund manager, your final goal is to make decision to buy, sell or hold.

Answer the following questions as best you can.
You can invoke the following tools:
{tools}

Use the following format:

Question: the input question you must answer
Thought: you should always think about what to do
Tool: the tool to invoke
Tool Input: the input to the tool
Observation: the invoke result of the tool
... (this process can repeat multiple times)

Once you have the final answer, provide the final answer in the following format:
Thought: I made this decision because ...
Final Answer: Make a decision to buy, sell or hold

Begin!
Question: {input}";
    
    public FundManagerAgent(
        DiscordSocketClient discordClient,
        IOptions<LLMConfiguration> config,
        AgentId id, 
        IAgentRuntime runtime, 
        FunctionTools tools,
        ILogger<FundManagerAgent> logger) : base(id, runtime, "FundManagerAgent", logger)
    {
        this.logger = logger;
        
        var client = new OpenAIClient(config.Value.OpenAIApiKey).GetChatClient(config.Value.Model);
        this.agent = new OpenAIChatAgent(
                chatClient: client,
                name: "cfo agent",
                systemMessage: "")
            .RegisterMessageConnector()
            .RegisterPrintMessage();
        
        this.reasoner = new OpenAIChatAgent(client, "reasoner")
            .RegisterMessageConnector()
            .RegisterPrintMessage();
        
        var toolCallMiddleware = new FunctionCallMiddleware(
            functions: [
                tools.BuyCoinFunctionContract,
                tools.SellCoinFunctionContract,
                tools.GetMyPortfolioFunctionContract,
                tools.Get30MinuteCandlestickDataFunctionContract,
                tools.GetDayCandlestickDataFunctionContract,
                tools.Get60MinuteCandlestickDataFunctionContract,
            ],
            functionMap: new Dictionary<string, Func<string, Task<string>>>
            {
                { nameof(tools.BuyCoin), tools.BuyCoinWrapper },
                { nameof(tools.SellCoin), tools.SellCoinWrapper },
                { nameof(tools.GetMyPortfolio), tools.GetMyPortfolioWrapper },
                { nameof(tools.Get30MinuteCandlestickData), tools.Get30MinuteCandlestickDataWrapper },
                { nameof(tools.GetDayCandlestickData), tools.GetDayCandlestickDataWrapper },
                { nameof(tools.Get60MinuteCandlestickData), tools.Get60MinuteCandlestickDataWrapper },
            });
        
        this.actor = new OpenAIChatAgent(client, "actor")
            .RegisterMessageConnector()
            .RegisterMiddleware(toolCallMiddleware)
            .RegisterPrintMessage();
    }

    public ValueTask HandleAsync(InitMessage item, MessageContext messageContext)
    {
        throw new NotImplementedException();
    }
}