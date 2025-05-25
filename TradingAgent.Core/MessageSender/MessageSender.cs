using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using TradingAgent.Core.Config;

namespace TradingAgent.Core.MessageSender;

public class MessageSender : IMessageSender
{
    private readonly ILogger<MessageSender> _logger;
    private readonly DiscordSocketClient _discordSocketClient;
    private readonly AppConfig config;
    
    public MessageSender(
        DiscordSocketClient discordSocketClient, 
        AppConfig config, ILogger<MessageSender> logger)
    {
        this._discordSocketClient = discordSocketClient;
        this.config = config;
        this._logger = logger;
    }

    public async Task SendMessage(string message)
    {
        var channel = await this._discordSocketClient.GetChannelAsync(this.config.Discord.ChannelId) as SocketTextChannel;
        if (channel != null)
        {
            await channel.SendMessageAsync(message);
        }
        else
        {
            this._logger.LogError("Discord channel not found.");
            throw new Exception("Discord channel not found.");
        }
    }
}