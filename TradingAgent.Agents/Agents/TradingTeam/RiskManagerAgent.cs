using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using OpenAI;
using TradingAgent.Agents.Messages.TradingTeam;
using TradingAgent.Core.Config;

namespace TradingAgent.Agents.Agents.TradingTeam;

[TypeSubscription(nameof(RiskManagerAgent))]
public class RiskManagerAgent :
    BaseAgent,
    IHandle<ProposeTransactionMessage>
{
    private const string AgentName = "Rsik Manager Agent";
    
    private readonly AppConfig _config;
    private readonly AutoGen.Core.IAgent _agent;
        
    public RiskManagerAgent(
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

    public async ValueTask HandleAsync(ProposeTransactionMessage item, MessageContext messageContext)
    {
        // given a proposal from the trader agent, check if the risk is acceptable

        var response = new AdjustedTransactionProposal();
        await this.PublishMessageAsync(response, new TopicId(nameof(TraderAgent)));
    }
}