using System.Text;
using Skender.Stock.Indicators;
using TradingAgent.Agents.Tools;

namespace TradingAgent.Agents.Extensions;

public static class HumanReadableExtensions
{
    public static string ToReadableString(this List<Quote> candles)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("Date | Open Price | Close Price | High Price | Low Price | Volume");
        
        foreach (var candle in candles)
        {
            sb.AppendLine($"{candle.Date} | {candle.Open} | {candle.Close} | {candle.High} | {candle.Low} | {candle.Volume}");
        }
        
        return sb.ToString();
    }
    
    public static string ToReadableString(this List<Candles.Response> candles)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("Date | Open Price | Close Price | High Price | Low Price | Volume");
        
        foreach (var candle in candles)
        {
            sb.AppendLine($"{candle.candle_date_time_kst} | {candle.opening_price} | {candle.trade_price} | {candle.high_price} | {candle.low_price} | {candle.candle_acc_trade_volume}");
        }
        
        return sb.ToString();
    }

    public static string ToReadableString(this List<DayCandles.Response> candles)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Date | Open Price | Close Price | High Price | Low Price | Volume");
        
        foreach (var candle in candles)
        {
            sb.AppendLine($"{candle.candle_date_time_kst} | {candle.opening_price} | {candle.trade_price} | {candle.high_price} | {candle.low_price} | {candle.candle_acc_trade_volume}");
        }
        
        return sb.ToString();
    }
}