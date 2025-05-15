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
        AppConfig config)
    {
        this._discordSocketClient = discordSocketClient;
        this.config = config;
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
            _logger.LogError("Discord channel not found.");
            throw new Exception("Discord channel not found.");
        }
    }
}