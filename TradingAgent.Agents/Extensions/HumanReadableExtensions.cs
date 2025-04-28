using System.Text;
using ConsoleTables;
using Skender.Stock.Indicators;
using TradingAgent.Agents.Messages;
using TradingAgent.Agents.Tools;

namespace TradingAgent.Agents.Extensions;

public static class HumanReadableExtensions
{
    public static string ToReadableString(this QuoteType quoteType) {
        return quoteType switch
        {
            QuoteType.None => "None",
            QuoteType.DayCandle => "Day Candle",
            QuoteType.HourCandle => "Hour Candle",
            _ => "Unknown"
        };
    }
    
    public static string ToReadableString(this List<Quote> candles)
    {
        var table = new ConsoleTable("Date", "Open Price", "Close Price", "High Price", "Low Price", "Volume");
        foreach (var candle in candles)
        {
            table.AddRow(candle.Date, candle.Open, candle.Close, candle.High, candle.Low, candle.Volume);
        }

        return table.ToString();
    }
}