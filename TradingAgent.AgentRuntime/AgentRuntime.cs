using Discord;
using Discord.WebSocket;
using Microsoft.AutoGen.Contracts;
using Microsoft.AutoGen.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingAgent.Agents;
using TradingAgent.Agents.Config;
using TradingAgent.Agents.Messages;
using TradingAgent.Core.UpbitClient;

namespace TradingAgent.AgentRuntime;

public class AgentRuntime(
    IOptions<AgentRuntimeConfiguration> options,
    ILogger<AgentRuntime> logger)
    : IHostedService
{
    private readonly DiscordSocketClient _client = new();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var upbitClient = new UpbitClient(options.Value.UpbitAccessKey, options.Value.UpbitSecretKey);
        
        var appBuilder = new AgentsAppBuilder();
        appBuilder.Services.AddOptions<LLMConfiguration>()
            .Configure(option =>
            {
                option.Model = "gpt-4o";
                option.OpenAIApiKey = options.Value.OpenAIApiKey;
            });

        appBuilder.Services.AddSingleton<IUpbitClient>(upbitClient);
        appBuilder.Services.AddSingleton(_client);
        appBuilder.Services.AddLogging(builder => builder.AddConsole());

        appBuilder.UseInProcessRuntime(deliverToSelf: true)
            .AddAgent<CfoAgent>(nameof(CfoAgent))
            .AddAgent<TradingAnalystAgent>(nameof(TradingAnalystAgent));
        
        var agentApp = await appBuilder.BuildAsync();
        await agentApp.StartAsync();
        
        await _client.LoginAsync(TokenType.Bot, options.Value.DiscordBotToken);
        _client.Log += LogAsync;
        _client.MessageReceived += MessageReceivedAsync;
        await _client.StartAsync();
        
        var message = new InitMessage { Market = "KRW-ETH" };
        await agentApp.PublishMessageAsync(message, new TopicId(nameof(CfoAgent), source: "agent")).ConfigureAwait(false);
        
        await agentApp.WaitForShutdownAsync().ConfigureAwait(false);
        await Task.Delay(-1, cancellationToken);
    }

    private Task MessageReceivedAsync(SocketMessage arg)
    {
        logger.LogInformation("Message received: {Message}", arg.Content);
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