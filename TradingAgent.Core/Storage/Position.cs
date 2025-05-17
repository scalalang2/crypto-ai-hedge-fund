namespace TradingAgent.Core.Storage;

public class Position
{
    public int Id { get; set; }
    
    /// <summary>
    /// 자산 심볼
    /// </summary>
    public string Symbol { get; set; }
    
    /// <summary>
    /// 보유 수량
    /// </summary>
    public double Amount { get; set; }
    
    /// <summary>
    /// 평균 매수 단가
    /// </summary>
    public double AverageBuyPrice { get; set; }
    
    /// <summary>
    /// 마지막 업데이트 시간
    /// </summary>
    public DateTime LastUpdated { get; set; }
}