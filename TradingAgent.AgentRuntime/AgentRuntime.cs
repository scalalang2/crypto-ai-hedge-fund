using Discord;
using Discord.WebSocket;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingAgent.Agents.Agents;
using TradingAgent.Agents.Agents.AnalysisTeam;
using TradingAgent.Agents.Agents.ResearchTeam;
using TradingAgent.Agents.Agents.Summarizer;
using TradingAgent.Agents.Agents.TradingTeam;
using TradingAgent.Agents.Messages.AnalysisTeam;
using TradingAgent.Agents.Services;
using TradingAgent.Core.Config;
using TradingAgent.Core.MessageSender;
using TradingAgent.Core.TraderClient;
using TradingAgent.Core.TraderClient.Upbit;

namespace TradingAgent.AgentRuntime;

public class AgentRuntime(
    IOptions<AppConfig> options,
    ILogger<AgentRuntime> logger)
    : IHostedService
{
    private readonly DiscordSocketClient _client = new();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var upbitClient = new UpbitClient(options.Value.Upbit.AccessKey, options.Value.Upbit.SecretKey);
        
        var appBuilder = new AgentsAppBuilder();
        appBuilder.Services.AddSingleton(options.Value);
        appBuilder.Services.AddSingleton<IUpbitClient>(upbitClient);
        appBuilder.Services.AddSingleton(_client);
        appBuilder.Services.AddSingleton<IMessageSender, MessageSender>();
        appBuilder.Services.AddSingleton<ITradingHistoryService, TradingHistoryService>();
        appBuilder.Services.AddLogging(builder => builder.AddConsole());

        appBuilder.UseInProcessRuntime(deliverToSelf: true)
            .AddAgent<GateKeeperAgent>(nameof(GateKeeperAgent))
            .AddAgent<NewsAnalystAgent>(nameof(NewsAnalystAgent))
            .AddAgent<SentimentAnalystAgent>(nameof(SentimentAnalystAgent))
            .AddAgent<TechnicalAnalystAgent>(nameof(TechnicalAnalystAgent))
            .AddAgent<ResearchTeamAgent>(nameof(ResearchTeamAgent))
            .AddAgent<TraderAgent>(nameof(TraderAgent))
            .AddAgent<RiskManagerAgent>(nameof(RiskManagerAgent))
            .AddAgent<SummarizerAgent>(nameof(SummarizerAgent));
        
        var agentApp = await appBuilder.BuildAsync();
        await agentApp.StartAsync();
        
        await _client.LoginAsync(TokenType.Bot, options.Value.Discord.BotToken);
        _client.Log += LogAsync;
        _client.MessageReceived += MessageReceivedAsync;
        await _client.StartAsync();


        foreach (var marketContext in options.Value.Markets)
        {
            var message = new StartAnalysisRequest
            {
                MarketContext = new MarketContext
                {
                    Ticker = marketContext.Ticker,
                    Name = marketContext.Name,
                }
            };
            await agentApp.PublishMessageAsync(message, new TopicId(nameof(GateKeeperAgent)), cancellationToken: cancellationToken);
        }
        
        System.Environment.Exit(0);
    }

    private Task MessageReceivedAsync(SocketMessage arg)
    {
        return Task.CompletedTask;
    }

    private Task LogAsync(LogMessage arg)
    {
        logger.LogInformation(arg.ToString());
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Agent runtime stopped");
        return Task.CompletedTask;
    }
}