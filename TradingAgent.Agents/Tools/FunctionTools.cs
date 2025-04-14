using AutoGen.Core;
using Microsoft.Extensions.Logging;
using TradingAgent.Core.Extensions;

namespace TradingAgent.Agents.Tools;

public partial class FunctionTools
{
    private readonly ILogger<FunctionTools> _logger;
    private readonly IUpbitClient _upbitClient;
    
    public FunctionTools(IUpbitClient upbitClient, ILogger<FunctionTools> logger)
    {
        _upbitClient = upbitClient;
        _logger = logger;
    }
    
    /// <summary>
    /// Get 30 minutes candlestick data for the market
    /// </summary>
    /// <param name="market">available markets are KRW-SOL</param>
    /// <returns>30 minutes candlestick data</returns>
    [Function]
    public async Task<string> Get30MinuteCandlestickData(string market)
    {
        var request = new Candles.Request
        {
            market = market,
            count = "20"
        };
        
        var response = await this._upbitClient.GetMinuteCandles(30, request);
        await Task.Delay(1000);
        return response.GenerateSchemaAndDataPrompt("30 Minute Candlestick Data");
    }
    
    /// <summary>
    /// Get 60 minutes candlestick data for the market
    /// </summary>
    /// <param name="market">available markets are KRW-SOL</param>
    /// <returns>30 minutes candlestick data</returns>
    [Function]
    public async Task<string> Get60MinuteCandlestickData(string market)
    {
        var request = new Candles.Request
        {
            market = market,
            count = "20"
        };
        
        var response = await this._upbitClient.GetMinuteCandles(60, request);
        await Task.Delay(1000);
        
        var result = $"""
                      this is 60 minutes candlestick data for market {market}\n
                      Time | Open | High | Low | Close | Volume | Accumulated Amount\n
                      """;

        foreach (var item in response)
        {
            result += $"{item.candle_date_time_kst} | {item.opening_price} | {item.high_price} | {item.low_price} | {item.trade_price} | {item.candle_acc_trade_volume} | {item.candle_acc_trade_price}\n";
        }

        return result;
    }
    
    /// <summary>
    /// Get day candlestick data for the market
    /// </summary>
    /// <param name="market">available markets are KRW-SOL</param>
    /// <returns>30 minutes candlestick data</returns>
    [Function]
    public async Task<string> GetDayCandlestickData(string market)
    {
        var request = new DayCandles.Request
        {
            market = market,
            count = "10"
        };
        
        var response = await this._upbitClient.GetDayCandles(request);
        await Task.Delay(1000);
        
        var result = $"""
this is day candlestick data for market {market}\n
Time | Open | High | Low | Close | Volume | Accumulated Amount\n
""";

        foreach (var item in response)
        {
            result += $"{item.candle_date_time_kst} | {item.opening_price} | {item.high_price} | {item.low_price} | {item.trade_price} | {item.candle_acc_trade_volume} | {item.candle_acc_trade_price}\n";
        }

        return result;
    }

    /// <summary>
    /// Returns the current portfolio of the user given market
    /// 
    /// </summary>
    /// <param name="market">available markets are KRW-SOL</param>
    /// <returns>Portfolio data for the market</returns>
    [Function]
    public async Task<string> GetMyPortfolio(string market)
    {
        var request = new Chance.Request();
        request.market
            = market;
        var response = await this._upbitClient.GetChance(request);
        await Task.Delay(1000);
        return response.GenerateSchemaAndDataPrompt("Portfolio");
    }

    /// <summary>
    /// Get the order history that the fund manager has made.
    /// </summary>
    /// <param name="market">The market code to trade in (e.g., KRW-SOL)</param>
    /// <returns>Your order history</returns>
    [Function]
    public async Task<string> GetOrderHistory(string market)
    {
        var request = new ClosedOrderHistory.Request
        {
            market = market,
            state = "done",
            limit = "15",
        };
        
        var response = await this._upbitClient.GetOrderHistory(request);
        await Task.Delay(1000);
        
        var result = $"""
    This is the order history that the fund manager has made.\n
    Side : Type of order (bid means `buy`/ask means `sell`)\n
    Market | Time | Side | Price | Volume \n
""";
        
        foreach (var item in response)
        {
            result += $"{item.market} | {item.created_at} | {item.side} | {item.price} | {item.volume}\n";
        }
        
        return result;
    }

    /// <summary>
    /// Purchases a specified amount of cryptocurrency in the given market using KRW.
    /// This method uses a price order type to buy the desired KRW amount at market price.
    /// </summary>
    /// <param name="market">The market code to trade in (e.g., KRW-SOL)</param>
    /// <param name="krwAmount">The amount of KRW to spend on the purchase</param>
    /// <returns>A response object containing the order result information</returns>
    [Function]
    public async Task<string> BuyCoin(string market, double krwAmount)
    {
        var priceAmount = string.Format("{0:F2}", krwAmount);
        
        var request = new PlaceOrder.Request
        {
            market = market,
            side = "bid",
            price = priceAmount,
            ord_type = "price"
        };
        
        var response = await this._upbitClient.PlaceOrder(request);
        await Task.Delay(1000);
        return response.GenerateSchemaAndDataPrompt("Order Result");
    }
    
    /// <summary>
    /// Sells a specified amount of cryptocurrency in the given market.
    /// This method uses a market order type to sell the specified coin amount at the current market price.
    /// </summary>
    /// <param name="market">The market code to trade in (e.g., KRW-SOL)</param>
    /// <param name="coinAmount">The amount of cryptocurrency to sell</param>
    /// <returns>A response object containing the order result information</returns>
    [Function]
    public async Task<string> SellCoin(string market, double coinAmount)
    {
        var coinAmountAsString = string.Format("{0:F2}", coinAmount);
        
        var request = new PlaceOrder.Request
        {
            market = market,
            side = "ask",
            volume = coinAmountAsString,
            ord_type = "market"
        };
        
        var response = await this._upbitClient.PlaceOrder(request);
        await Task.Delay(1000);
        return response.GenerateSchemaAndDataPrompt("Order Result");
    }

    /// <summary>
    /// Hold the coin in the given market.
    /// </summary>
    /// <param name="market">The market code to trade in (e.g., KRW-SOL)</param>
    /// <returns></returns>
    [Function]
    public Task<string> HoldCoin(string market)
    {
        return Task.FromResult($"hold coin in {market}"); 
    }
}