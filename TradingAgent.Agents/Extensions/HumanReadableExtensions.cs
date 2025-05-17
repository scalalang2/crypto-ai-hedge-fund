using ConsoleTables;
using Skender.Stock.Indicators;
using TradingAgent.Agents.Messages;

namespace TradingAgent.Agents.Extensions;

public static class HumanReadableExtensions
{
    public static string ToReadableString(this List<Quote> candles)
    {
        return ConsoleTable.From(candles).ToString();
    }
}