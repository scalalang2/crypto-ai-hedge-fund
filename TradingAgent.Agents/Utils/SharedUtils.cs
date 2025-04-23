using System.Text;
using TradingAgent.Agents.Tools;

namespace TradingAgent.Agents.Utils;

public static class SharedUtils
{
    public static async Task<string> GetCurrentPositionPrompt(IUpbitClient upbitClient, List<string> availableMarket)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Market | Amount | Avg Price | Buying Fee | Selling Fee | Minimum Buying Amount | Minimum Selling Amount");
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
        sb.AppendLine();
        return sb.ToString();
    }
}