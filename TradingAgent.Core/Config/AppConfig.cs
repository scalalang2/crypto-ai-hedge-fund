using System.ComponentModel.DataAnnotations;

namespace TradingAgent.Core.Config;

/// <summary>
/// Overall configuration for this application
/// </summary>
public class AppConfig
{
    /// <summary>
    /// List of markets you want to trade in.
    /// </summary>
    [Required]
    public List<MarketContext> Markets { get; set; } = [];
    
    /// <summary>
    /// Open AI API key for the application.
    /// </summary>
    [Required]
    public string OpenAIApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// e.g. gpt-4.1
    /// </summary>
    [Required]
    public string SmartAIModel { get; set; }
    
    /// <summary>
    /// e.g. gpt-4.1-mini
    /// </summary>
    [Required]
    public string FastAIModel { get; set; }
    
    public UpbitConfig Upbit { get; set; } = new();
    
    public DiscordConfig Discord { get; set; } = new();
}