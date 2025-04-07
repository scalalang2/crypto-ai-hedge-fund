namespace TradingAgent.Agents.Config;

public class LLMConfiguration
{
    public string OpenAIApiKey { get; set; } = string.Empty;
    
    public string Model { get; set; } = string.Empty;
    
    public ulong DiscordChannelId { get; set; } = 0;
}