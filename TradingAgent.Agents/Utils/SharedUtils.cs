using System.Text;
using ConsoleTables;
using TradingAgent.Core.Config;
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

    public static string CurrentTickers(List<Ticker> tickers)
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
    
    public static async Task<string> GetCurrentPositionPrompt(IUpbitClient upbitClient, List<MarketContext> availableMarket, List<Ticker> tickerResponse)
    {
        var table = new ConsoleTable("Market", "Amount", "Average Buying Price", "Current Price");
        var totalKrw = 0d;
        foreach (var market in availableMarket)
        {
            var request = new Chance.Request
            {
                market = market.Ticker
            };
            var response = await upbitClient.GetChance(request);
            await Task.Delay(200);
            
            var ticker = tickerResponse.FirstOrDefault(t => t.market == market.Ticker);
            if (ticker == null)
            {
                continue;
            }
            
            var currentPrice = ticker.trade_price;
            table.AddRow(market.Ticker, response.ask_account.balance, response.ask_account.avg_buy_price, $"{currentPrice:N8}");
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
}