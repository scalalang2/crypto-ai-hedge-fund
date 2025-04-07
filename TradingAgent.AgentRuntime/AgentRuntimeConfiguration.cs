using System.ComponentModel.DataAnnotations;

namespace TradingAgent.AgentRuntime;

// Configuration class for the Agents
public class AgentRuntimeConfiguration
{
    [Required]
    public string OpenAIApiKey { get; set; }
    
    [Required]
    public string UpbitAccessKey { get; set; }
    
    [Required]
    public string UpbitSecretKey { get; set; }
    
    [Required]
    public string DiscordBotToken { get; set; }
    
    [Required]
    public ulong DiscordChannelId { get; set; }
}