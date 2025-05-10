using Skender.Stock.Indicators;
using Candles = TradingAgent.Core.TraderClient.Candles;
using DayCandles = TradingAgent.Core.TraderClient.DayCandles;

namespace TradingAgent.Agents.Extensions;

public static class StockIndicatorExtensions
{
    public static List<Quote> ToQuote(this List<Candles.Response> candles)
    {
        return candles.Select(candle => new Quote
            {
                Date = candle.candle_date_time_utc,
                Open = Convert.ToDecimal(candle.opening_price),
                High = Convert.ToDecimal(candle.high_price),
                Low = Convert.ToDecimal(candle.low_price),
                Close = Convert.ToDecimal(candle.trade_price),
                Volume = Convert.ToDecimal(candle.candle_acc_trade_volume)
            })
            .ToList();
    }

    public static List<Quote> ToQuote(this List<DayCandles.Response> candles)
    {
        return candles.Select(candle => new Quote
            {
                Date = candle.candle_date_time_utc,
                Open = Convert.ToDecimal(candle.opening_price),
                High = Convert.ToDecimal(candle.high_price),
                Low = Convert.ToDecimal(candle.low_price),
                Close = Convert.ToDecimal(candle.trade_price),
                Volume = Convert.ToDecimal(candle.candle_acc_trade_volume)
            })
            .ToList();
    }
}