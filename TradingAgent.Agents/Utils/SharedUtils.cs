using System.Text;
using TradingAgent.Agents.Services;
using TradingAgent.Agents.Tools;

namespace TradingAgent.Agents.Utils;

public static class SharedUtils
{
    public static string CurrentDate()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public static async Task<string> CurrentTickers(IUpbitClient upbitClient, List<string> availableMarket)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Ticker | Current Price | Opening Price | High Price | Low Price");
        var response = await upbitClient.GetTicker(string.Join(",", availableMarket));
        foreach (var tick in response)
        {
            sb.AppendLine($"{tick.market} | {tick.trade_price} | {tick.opening_price} | {tick.high_price} | {tick.low_price}");
        }
        
        sb.AppendLine();
        return sb.ToString();
    }
    
    public static async Task<string> GetCurrentPositionPrompt(IUpbitClient upbitClient, List<string> availableMarket)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Market | Amount | Avg Price | Buying Fee | Selling Fee | Minimum Buying Amount (KRW) | Minimum Selling Amount (KRW)");
        var totalKrw = 0d;
        foreach (var market in availableMarket)
        {
            var request = new Chance.Request
            {
                market = market
            };
            var response = await upbitClient.GetChance(request);
            await Task.Delay(200);

            sb.AppendLine($"{market} | {response.ask_account.balance} | {response.ask_account.avg_buy_price} | {response.bid_fee} | {response.ask_fee} | {response.market.bid.min_total} | {response.market.ask.min_total}");
            totalKrw = Convert.ToDouble(response.bid_account.balance);
        }

        sb.AppendLine();
        sb.AppendLine($"Available Balance : {totalKrw} KRW");
        return sb.ToString();
    }
    
    public static async Task<string> GetTradingHistoryPrompt(ITradingHistoryService tradingHistoryService)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Date | Ticker | Buying Price | Selling Price | Profit Rate | Loss Rate");
        var tradeHistory = await tradingHistoryService.GetTradeHistoryAsync(10);
        foreach (var record in tradeHistory)
        {
            sb.AppendLine($"{record.Date} | {record.Ticker} | {record.BuyingPrice:N2} | {record.SellingPrice:N2} | {record.ProfitRate:N3} | {record.LossRate:N3}");
        }
        
        sb.AppendLine();
        sb.AppendLine($"Total Profit Rate : {await tradingHistoryService.GetTotalProfitRateAsync():N3}");
        sb.AppendLine($"Total Loss Rate : {await tradingHistoryService.GetTotalLossRateAsync():N3}");
        sb.AppendLine($"Total Profit : {await tradingHistoryService.GetTotalProfitAsync():N3}");
        sb.AppendLine($"Total Loss : {await tradingHistoryService.GetTotalLossAsync():N3}");

        return sb.ToString();
    }
}