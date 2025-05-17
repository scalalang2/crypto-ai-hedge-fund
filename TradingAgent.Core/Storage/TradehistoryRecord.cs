namespace TradingAgent.Core.Storage;

public class TradeHistoryRecord
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    
    /// <summary>
    /// 거래된 자산의 심볼 (예: BTCUSDT) 
    /// </summary>
    public string Symbol { get; set; }
    
    /// <summary>
    /// 매수(Buy), 매도(Sell)
    /// </summary>
    public string OrderType { get; set; }
    
    /// <summary>
    /// 체결 가격
    /// </summary>
    public double Price { get; set; }
    
    /// <summary>
    /// 수량
    /// </summary>
    public double Amount { get; set; }
}