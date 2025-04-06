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

    public static string GeneratePrompt(this Chance.Response response)
    {
        if (response == null)
            return "No data available to generate a prompt.";

        var prompt = new StringBuilder();

        // Market Information
        prompt.AppendLine($"Market Information:");
        prompt.AppendLine($"- Market.ID: {response.market.id}");
        prompt.AppendLine($"- Market.Name: {response.market.name}");
        prompt.AppendLine($"- Market.OrderTypes: {string.Join(", ", response.market.order_types)}");
        prompt.AppendLine($"- Market.OrderSides: {string.Join(", ", response.market.order_sides)}");
        prompt.AppendLine($"- Market.State: {response.market.state}");
        prompt.AppendLine($"- Market.MaximumTotal: {response.market.max_total}");
        prompt.AppendLine();

        // Bid Details
        prompt.AppendLine($"Bid Details:");
        prompt.AppendLine($"- Bid.Currency: {response.market.bid.currency}");
        prompt.AppendLine($"- Bid.PriceUnit: {(response.market.bid.price_unit ?? "N/A")}");
        prompt.AppendLine($"- Bid.MinimumTotal: {response.market.bid.min_total}");
        prompt.AppendLine();

        // Ask Details
        prompt.AppendLine($"Ask Details:");
        prompt.AppendLine($"- Ask.Currency: {response.market.ask.currency}");
        prompt.AppendLine($"- Ask.PriceUnit: {(response.market.ask.price_unit ?? "N/A")}");
        prompt.AppendLine($"- Ask.MinimumTotal: {response.market.ask.min_total}");
        prompt.AppendLine();

        // Fee Information
        prompt.AppendLine($"Fee Information:");
        prompt.AppendLine($"- Fee.BidFee: {response.bid_fee}");
        prompt.AppendLine($"- Fee.AskFee: {response.ask_fee}");
        prompt.AppendLine();
        
        if (response.bid_account != null)
        {
            prompt.AppendLine($"Bid Account Information:");
            prompt.AppendLine($"- BidAccount.Currency: {response.bid_account.currency}");
            prompt.AppendLine($"- BidAccount.Balance: {response.bid_account.balance}");
            prompt.AppendLine($"- BidAccount.Locked: {response.bid_account.locked}");
            prompt.AppendLine($"- BidAccount.AverageBuyPrice: {response.bid_account.avg_buy_price} (Modified: {response.bid_account.avg_buy_price_modified})");
            prompt.AppendLine($"- BidAccount.UnitCurrency: {response.bid_account.unit_currency}");
            prompt.AppendLine();
        }

        // Ask Account Information
        if (response.ask_account != null)
        {
            prompt.AppendLine($"Ask Account Information:");
            prompt.AppendLine($"- AskAccount.Currency: {response.ask_account.currency}");
            prompt.AppendLine($"- AskAccount.Balance: {response.ask_account.balance}");
            prompt.AppendLine($"- AskAccount.Locked: {response.ask_account.locked}");
            prompt.AppendLine($"- AskAccount.AverageBuyPrice: {response.ask_account.avg_buy_price} (Modified: {response.ask_account.avg_buy_price_modified})");
            prompt.AppendLine($"- AskAccount.UnitCurrency: {response.ask_account.unit_currency}");
            prompt.AppendLine();
        }
        return prompt.ToString();
    }
}