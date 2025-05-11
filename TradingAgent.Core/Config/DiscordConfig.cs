using System.ComponentModel.DataAnnotations;

namespace TradingAgent.Core.Config;

public class DiscordConfig
{
    [Required]
    public string BotToken { get; set; }
    
    [Required]
    public ulong ChannelId { get; set; }
}