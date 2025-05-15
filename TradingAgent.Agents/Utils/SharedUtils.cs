using System.Text;
using ConsoleTables;
using TradingAgent.Agents.Services;
using TradingAgent.Core.TraderClient;
using Chance = TradingAgent.Core.TraderClient.Chance;
using Ticker = TradingAgent.Core.TraderClient.Ticker;

namespace TradingAgent.Agents.Utils;

public static class SharedUtils
{
    public static string CurrentDate()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public static async Task<string> CurrentTickers(List<Ticker> tickers)
    {
        var table = new ConsoleTable("Ticker", "Current Price", "Opening Price", "High Price", "Low Price");
        foreach (var tick in tickers)
        {
            table.AddRow(
                tick.market,
                tick.trade_price,
                tick.opening_price,
                tick.high_price,
                tick.low_price);
        }

        return table.ToMinimalString();
    }
    
    public static async Task<string> GetCurrentPositionPrompt(IUpbitClient upbitClient, List<string> availableMarket, List<Ticker> tickerResponse)
    {
        var table = new ConsoleTable("Market", "Amount", "Average Buying Price", "Current Price");
        var totalKrw = 0d;
        foreach (var market in availableMarket)
        {
            var request = new Chance.Request
            {
                market = market
            };
            var response = await upbitClient.GetChance(request);
            await Task.Delay(200);
            
            var ticker = tickerResponse.FirstOrDefault(t => t.market == market);
            if (ticker == null)
            {
                continue;
            }
            
            var currentPrice = ticker.trade_price;
            table.AddRow(market, response.ask_account.balance, response.ask_account.avg_buy_price, $"{currentPrice:N8}");
            totalKrw = Convert.ToDouble(response.bid_account.balance);
        }

        var result = table.ToMinimalString();
        result += $"\n\nAvailable Balance : {totalKrw} KRW";
        
        var sb = new StringBuilder();
        sb.AppendLine("```");
        sb.AppendLine(result);
        sb.AppendLine("```");
        return sb.ToString();
    }
    
    public static async Task<string> GetTradingHistoryPrompt(ITradingHistoryService tradingHistoryService)
    {
        var sb = new StringBuilder();
        var table = new ConsoleTable("Date", "Ticker", "Amount", "Buying Price", "Selling Price", "Profit Rate", "Loss Rate");
        var tradeHistory = await tradingHistoryService.GetTradeHistoryAsync(10);
        foreach (var record in tradeHistory)
        {
            table.AddRow(
                $"{record.Date:yyyy-MM-dd HH:mm:ss}",
                record.Ticker,
                $"{record.Amount:N8}",
                $"{record.BuyingPrice:N2}",
                $"{record.SellingPrice:N2}",
                $"{record.ProfitRate:N3}",
                $"{record.LossRate:N3}");
        }

        sb.AppendLine("```");
        sb.AppendLine(table.ToMinimalString());
        sb.AppendLine();
        sb.AppendLine($"Total Profit Rate : {await tradingHistoryService.GetTotalProfitRateAsync():N3}");
        sb.AppendLine($"Total Loss Rate : {await tradingHistoryService.GetTotalLossRateAsync():N3}");
        sb.AppendLine($"Total Profit : {await tradingHistoryService.GetTotalProfitAsync():N3}");
        sb.AppendLine($"Total Loss : {await tradingHistoryService.GetTotalLossAsync():N3}");
        sb.AppendLine("```");

        return sb.ToString();
    }
}