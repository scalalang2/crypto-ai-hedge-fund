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
    /// <param name="market">available markets are KRW-BTC, KRW-ETH and KRW-SOL</param>
    /// <returns>30 minutes candlestick data</returns>
    [Function]
    public async Task<string> Get30MinuteCandlestickData(string market)
    {
        var request = new Candles.Request
        {
            market = market,
            count = "100"
        };
        
        var response = await this._upbitClient.GetMinuteCandles(30, request);
        return response.GenerateDataPrompt("30 Minute Candlestick Data");
    }
    
    /// <summary>
    /// Get 60 minutes candlestick data for the market
    /// </summary>
    /// <param name="market">available markets are KRW-BTC, KRW-ETH and KRW-SOL</param>
    /// <returns>30 minutes candlestick data</returns>
    [Function]
    public async Task<string> Get60MinuteCandlestickData(string market)
    {
        var request = new Candles.Request
        {
            market = market,
            count = "100"
        };
        
        var response = await this._upbitClient.GetMinuteCandles(60, request);
        return response.GenerateDataPrompt("60 Minute Candlestick Data");
    }
    
    /// <summary>
    /// Get day candlestick data for the market
    /// </summary>
    /// <param name="market">available markets are KRW-BTC, KRW-ETH and KRW-SOL</param>
    /// <returns>30 minutes candlestick data</returns>
    [Function]
    public async Task<string> GetDayCandlestickData(string market)
    {
        var request = new DayCandles.Request
        {
            market = market,
            count = "100"
        };
        
        var response = await this._upbitClient.GetDayCandles(request);
        return response.GenerateDataPrompt("Day Candlestick Data");
    }

    /// <summary>
    /// Returns the current portfolio of the user given market
    /// 
    /// </summary>
    /// <param name="market">available markets are KRW-BTC, KRW-ETH and KRW-SOL</param>
    /// <returns>Portfolio data for the market</returns>
    [Function]
    public async Task<string> GetMyPortfolio(string market)
    {
        var request = new Chance.Request();
        request.market
            = market;
        var response = await this._upbitClient.GetChance(request);
        return response.GenerateDataPrompt("Portfolio");
    }

    /// <summary>
    /// Purchases a specified amount of cryptocurrency in the given market using KRW.
    /// This method uses a price order type to buy the desired KRW amount at market price.
    /// </summary>
    /// <param name="market">The market code to trade in (e.g., KRW-BTC)</param>
    /// <param name="krwAmount">The amount of KRW to spend on the purchase</param>
    /// <returns>A response object containing the order result information</returns>
    [Function]
    public async Task<string> BuyCoin(string market, double krwAmount)
    {
        var request = new PlaceOrder.Request
        {
            market = market,
            side = "bid",
            price = krwAmount.ToString(),
            ord_type = "price"
        };
        
        var response = await this._upbitClient.PlaceOrder(request);
        return response.GenerateDataPrompt("Order Result");
    }
    
    /// <summary>
    /// Sells a specified amount of cryptocurrency in the given market.
    /// This method uses a market order type to sell the specified coin amount at the current market price.
    /// </summary>
    /// <param name="market">The market code to trade in (e.g., KRW-BTC)</param>
    /// <param name="coinAmount">The amount of cryptocurrency to sell</param>
    /// <returns>A response object containing the order result information</returns>
    [Function]
    public async Task<string> SellCoin(string market, double coinAmount)
    {
        var request = new PlaceOrder.Request
        {
            market = market,
            side = "ask",
            volume = coinAmount.ToString(),
            ord_type = "market"
        };
        
        var response = await this._upbitClient.PlaceOrder(request);
        return response.GenerateDataPrompt("Order Result");
    }
}