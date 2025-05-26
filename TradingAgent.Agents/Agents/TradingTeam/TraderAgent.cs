using System.Text;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using ConsoleTables;
using Json.Schema;
using Json.Schema.Generation;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI;
using TradingAgent.Agents.AgentPrompts;
using TradingAgent.Agents.Agents.Summarizer;
using TradingAgent.Agents.Messages.ResearchTeam;
using TradingAgent.Agents.Messages.Summarizer;
using TradingAgent.Agents.Messages.TradingTeam;
using TradingAgent.Agents.State;
using TradingAgent.Agents.Utils;
using TradingAgent.Core.Config;
using TradingAgent.Core.Storage;
using TradingAgent.Core.TraderClient;

namespace TradingAgent.Agents.Agents.TradingTeam;

[TypeSubscription(nameof(TraderAgent))]
public class TraderAgent :
    BaseAgent,
    IHandle<AdjustedTransactionProposal>,
    IHandle<ResearchResultResponse>
{
    private const string AgentName = "TraderAgent";
    
    private readonly IUpbitClient _upbitClient;
    private readonly IStorageService _storageService;
    private readonly AppConfig _config;
    private readonly AutoGen.Core.IAgent _agent;
    private readonly AgentSharedState _state;
        
    public TraderAgent(
        AgentId id, 
        IAgentRuntime runtime, 
        IUpbitClient upbitClient,
        ILogger<BaseAgent> logger, 
        AppConfig config, 
        IStorageService storageService, 
        AgentSharedState state) : base(id, runtime, AgentName, logger)
    {
        this._upbitClient = upbitClient;
        this._config = config;
        this._storageService = storageService;
        this._state = state;

        var client = new OpenAIClient(config.OpenAIApiKey).GetChatClient(config.SmartAIModel);
        this._agent = new OpenAIChatAgent(
                chatClient: client, 
                name: AgentName, 
                systemMessage: TraderPrompt.TraderSystemMessage)
            .RegisterMessageConnector()
            .RegisterPrintMessage();
    }

    public async ValueTask HandleAsync(AdjustedTransactionProposal item, MessageContext messageContext)
    {
        var ticker = await this._upbitClient.GetTicker(string.Join(",", this._config.Markets.Select(x => x.Ticker)));
        foreach (var proposal in item.Recommendations)
        {
            await this._storageService.UpdateReasoningRecordAsync(proposal.Ticker, DateTime.UtcNow);
            if (proposal.Action == "Hold" || proposal.Quantity == 0)
            {
                continue;
            }
                
            var price = ticker.FirstOrDefault(x => x.market == proposal.Ticker)?.trade_price;
            if (price == null)
            {
                throw new Exception($"Ticker {proposal.Ticker} not found");
            }

            await Task.Delay(500); // rest to avoid rate limit
            await this.PlaceOrder(proposal, price.Value);
        }
        await this.PublishMessageAsync(new SendPerformanceMessage(), new TopicId(nameof(SummarizerAgent)));
    }

    public async ValueTask HandleAsync(ResearchResultResponse item, MessageContext messageContext)
    {
        this._logger.LogInformation("[TraderAgent] {sender} {research_result}", messageContext.Sender, item);
        this._state.ResearchResults[item.MarketContext.Ticker] = item;
        await this.TryProposeTrade();
    }

    private async Task TryProposeTrade()
    {
        // ensure that all research results are received
        if (this._state.Candidates.Any(market => this._state.ResearchResults.ContainsKey(market.Ticker) == false))
        {
            return;
        }

        var candidates = this._state
            .Candidates
            .Where(market => this._state.ResearchResults.ContainsKey(market.Ticker))
            .ToList();
        
        var tickerResponse = await this._upbitClient.GetTicker(string.Join(",", candidates.Select(x => x.Ticker)));
        var currentPrice = SharedUtils.CurrentTickers(tickerResponse);
        var currentPosition = await SharedUtils.GetCurrentPositionPrompt(this._upbitClient, candidates, tickerResponse);
        var chatHistory = new StringBuilder();

        foreach (var (market, result) in _state.ResearchResults)
        {
            var jsonString = JsonConvert.SerializeObject(result.DiscussionHistory);
            chatHistory.AppendLine($"## {result.MarketContext.Ticker} ({result.MarketContext.Name}) Research Result");
            chatHistory.AppendLine($"### Confidence: {jsonString}");
            chatHistory.AppendLine();
        }
        
        var message = TraderPrompt.TraderUserMessage
            .Replace("{research_team_chat_history}", chatHistory.ToString())
            .Replace("{current_price}", currentPrice)
            .Replace("{current_portfolio}", currentPosition);
        
        this._logger.LogInformation("Trader's prompt {message}", message);
        
        var userMessage = new TextMessage(Role.User, message);
        var reply = await this._agent.GenerateReplyAsync(
            messages: [userMessage],
            options: new GenerateReplyOptions
            {
                OutputSchema = new JsonSchemaBuilder()
                    .FromType<ProposeTransactionMessage>()
                    .Build(),
            });
        
        var response = JsonConvert.DeserializeObject<ProposeTransactionMessage>(reply.GetContent());

        await this.PublishMessageAsync(response, new TopicId(nameof(SummarizerAgent)));
        await this.PublishMessageAsync(response, new TopicId(nameof(RiskManagerAgent)));
    }
    
    private async Task PlaceOrder(TransactionProposal proposal, double price)
    {
        const string buy = "Buy";
        const string sell = "Sell";
        
        if(proposal.Action != buy && proposal.Action != sell)
        {
            throw new ArgumentException($"Invalid action. Only 'buy' and 'sell' are allowed. but {proposal.Action} was given");
        }
            
        // ignore if the price is less than 20,000 KRW
        if(price * proposal.Quantity < 20000)
        {
            return;
        }
        await Task.Delay(500);
        
        var quantity = string.Format("{0:F8}", proposal.Quantity);
        var ordType = proposal.Action == buy ? "price" : "market";
        var side = proposal.Action == buy ? "bid" : "ask";
        
        var request = new PlaceOrder.Request
        {
            market = proposal.Ticker,
            side = side,
            ord_type = ordType
        };
        
        if(proposal.Action == buy)
        {
            request.price = quantity;
        }
        else
        {
            request.volume = quantity;
        }
        
        var orderPlaced = await this._upbitClient.PlaceOrder(request);
        this._logger.LogInformation("order placed {@order}", orderPlaced);
        
        // 언제 체결될지는 모르겠으나 2초 정도 대기해봄
        await Task.Delay(2000);

        var order= await _upbitClient.GetOrder(orderPlaced.uuid);
        foreach(var trade in order.trades)
        {
            if(order.uuid != orderPlaced.uuid)
            {
                continue;
            }
            
            this._logger.LogInformation("order completed {@order}", order);
            
            // 주문이 부분체결하고 짤짤이가 남아있으면 상태가 cancel로 들어온다.
            if (order.state == "done" || order.state == "cancel")
            {
                var orderPrice = Convert.ToDouble(trade.price);
                var funds = Convert.ToDouble(trade.funds);
                var orderAmount = Convert.ToDouble(trade.volume);
                var orderType = trade.side == "bid" ? buy : sell;
                var tradeRecord = new TradeHistoryRecord
                {
                    Symbol = proposal.Ticker,
                    OrderType = orderType,
                    Price = orderPrice,
                    Amount = orderAmount,
                    Funds = funds,
                    Date = order.created_at
                };
                
                await this._storageService.AddTradeHistoryAsync(tradeRecord);
            }
        }
    }
}