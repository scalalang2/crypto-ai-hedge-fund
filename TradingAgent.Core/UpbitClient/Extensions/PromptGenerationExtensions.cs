using System.Text;

namespace TradingAgent.Core.UpbitClient.Extensions;

public static class PromptGenerationExtensions
{
    public static string GeneratePrompt(this List<Candles.Response> marketData)
    {
        if (!marketData.Any())
            return "No market data available for analysis.";

        var latest = marketData.Last();
    
        var prompt = new StringBuilder();
        prompt.AppendLine($"📈 Market Update for {latest.market}");
        prompt.AppendLine($"🕒 As of {latest.candle_date_time_kst:yyyy-MM-dd HH:mm} KST");
        prompt.AppendLine();
    
        // Price Movement Summary
        prompt.AppendLine("## Current Session Summary");
        prompt.AppendLine($"🔹 Open: {latest.opening_price:N0}");
        prompt.AppendLine($"🔹 High: {latest.high_price:N0}");
        prompt.AppendLine($"🔹 Low: {latest.low_price:N0}");
        prompt.AppendLine($"🔹 Close: {latest.trade_price:N0}");
        prompt.AppendLine($"📦 Volume: {latest.candle_acc_trade_volume:N2} units");
        prompt.AppendLine($"💵 Trade Value: {latest.candle_acc_trade_price:N0}");
        prompt.AppendLine();

        // Historical Context
        prompt.AppendLine("## Historical Context");
        prompt.AppendLine("Date(KST)|Open|High|Low|Close|Volume");

        foreach (var entry in marketData)
        {
            prompt.AppendLine($"{entry.candle_date_time_kst:MM-dd HH:mm}|" +
                              $"{entry.opening_price,7:N0}|" +
                              $"{entry.high_price,7:N0}|" +
                              $"{entry.low_price,7:N0}|" +
                              $"{entry.trade_price,7:N0}|" +
                              $"{entry.candle_acc_trade_volume}");
        }

        return prompt.ToString();
    }
}