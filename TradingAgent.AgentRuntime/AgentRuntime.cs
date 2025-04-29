using Discord;
using Discord.WebSocket;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingAgent.Agents;
using TradingAgent.Agents.Agents;
using TradingAgent.Agents.Messages;
using TradingAgent.Agents.Services;
using TradingAgent.Agents.Tools;
using TradingAgent.Core.Config;

namespace TradingAgent.AgentRuntime;

public class AgentRuntime(
    IOptions<AppConfig> options,
    ILogger<AgentRuntime> logger)
    : IHostedService
{
    private readonly DiscordSocketClient _client = new();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var upbitClient = new UpbitClient(options.Value.UpbitAccessKey, options.Value.UpbitSecretKey);
        
        var appBuilder = new AgentsAppBuilder();
        appBuilder.Services.AddSingleton(options.Value);
        appBuilder.Services.AddSingleton<FunctionTools>();
        appBuilder.Services.AddSingleton<IUpbitClient>(upbitClient);
        appBuilder.Services.AddSingleton(_client);
        appBuilder.Services.AddSingleton<IMessageSender, MessageSender>();
        appBuilder.Services.AddSingleton<ITradingHistoryService, TradingHistoryService>();
        appBuilder.Services.AddLogging(builder => builder.AddConsole());

        appBuilder.UseInProcessRuntime(deliverToSelf: true)
            .AddAgent<PortfolioManager>(nameof(PortfolioManager))
            .AddAgent<TechnicalAnalystAgent>(nameof(TechnicalAnalystAgent))
            .AddAgent<HosodaGoichiAgent>(nameof(HosodaGoichiAgent))
            .AddAgent<GeorgeLaneAgent>(nameof(GeorgeLaneAgent))
            .AddAgent<CriticAgent>(nameof(CriticAgent))
            .AddAgent<TraderAgent>(nameof(TraderAgent))
            .AddAgent<RiskManagerAgent>(nameof(RiskManagerAgent))
            .AddAgent<SummarizerAgent>(nameof(SummarizerAgent));
        
        var agentApp = await appBuilder.BuildAsync();
        await agentApp.StartAsync();
        
        await _client.LoginAsync(TokenType.Bot, options.Value.DiscordBotToken);
        _client.Log += LogAsync;
        _client.MessageReceived += MessageReceivedAsync;
        await _client.StartAsync();
        
        var message = new InitMessage { };
        await agentApp.PublishMessageAsync(message, new TopicId(nameof(PortfolioManager)), cancellationToken: cancellationToken).ConfigureAwait(false);
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