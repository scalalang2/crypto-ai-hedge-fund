using System.ComponentModel.DataAnnotations;

namespace TradingAgent.Core.Config;

/// <summary>
/// 트레이딩 애플리케이션의 설정값 정의
/// </summary>
public class AppConfig
{
    [Required]
    public List<string> AvailableMarkets { get; set; } = [];
    
    [Required]
    public string OpenAIApiKey { get; set; }
    
    [Required]
    public string LeaderAIModel { get; set; }
    
    [Required]
    public string WorkerAIModel { get; set; }
    
    [Required]
    public string UpbitAccessKey { get; set; }
    
    [Required]
    public string UpbitSecretKey { get; set; }
    
    [Required]
    public string DiscordBotToken { get; set; }
    
    [Required]
    public ulong DiscordChannelId { get; set; }
}