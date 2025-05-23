namespace TradingAgent.Core.Storage;

public class ReasoningRecord
{
    public int Id { get; set; }
    
    /// <summary>
    /// 마켓 티커
    /// </summary>
    public string Ticker { get; set; } = string.Empty;
    
    /// <summary>
    /// 마지막 추론 시간
    /// </summary>
    public DateTime LastReasoningTime { get; set; } = DateTime.MinValue;
}