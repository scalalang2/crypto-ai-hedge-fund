namespace TradingAgent.Agents.Messages;

/// <summary>
/// 요약 요청
/// </summary>
public class SummaryRequest
{
    public string ChatHistory { get; set; } = string.Empty;
}