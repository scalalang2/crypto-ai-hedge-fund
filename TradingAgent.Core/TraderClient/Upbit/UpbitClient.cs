using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Newtonsoft.Json;
using RestSharp;
using Skender.Stock.Indicators;
using TradingAgent.Core.Extensions;

namespace TradingAgent.Core.TraderClient.Upbit;

/// <summary>
/// Copied from https://github.com/airtaxi/.NET-UPbit-Api/blob/master/UPbit%20Api/Upbit.cs
/// </summary>
public class UpbitClient : IUpbitClient
{
    private readonly string _accessKey;
    private readonly string _secretKey;
    private static readonly Uri _baseUrl = new("https://api.upbit.com/v1/");
    
    public UpbitClient(string accessKey, string secretKey)
    {
        if (string.IsNullOrWhiteSpace(accessKey)) { throw new ArgumentNullException(nameof(accessKey)); }
        if (string.IsNullOrWhiteSpace(secretKey)) { throw new ArgumentNullException(nameof(secretKey)); }
        _accessKey = accessKey;
        _secretKey = secretKey;
    }
    
    private static Dictionary<string, string> GenerateApiCallArgs(object obj)
    {
        var type = obj.GetType();
        var properties = type.GetProperties();
        var apiCallArgs = new Dictionary<string, string>();
        foreach (var property in properties)
        {
            // ignore null
            if (property.GetValue(obj) is null)
                continue;
            apiCallArgs.Add(property.Name, (string)property.GetValue(obj));
        }
        return apiCallArgs;
    }
    
    private static string ToQueryString(Dictionary<string, string> parameters, bool isUrl = false)
    {
        List<string> parameterList = new List<string>();
        parameters.Keys.ToList().ForEach(key =>
        {
            var value = (string)parameters[key];
            if(!string.IsNullOrEmpty(value))
                parameterList.Add($"{(isUrl ? HttpUtility.UrlEncode(key) : key)}={(isUrl ? HttpUtility.UrlEncode(value) : value)}");
        });
        return string.Join("&", parameterList);
    }

    private static string ToSHA512(string queryString)
    {
        SHA512 sha = SHA512.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(queryString));
        var hexaHash = BitConverter.ToString(hash).Replace("-", "").ToLower();
        return hexaHash;
    }
    
    private string GenerateAuthenticationToken(Dictionary<string, string> args)
    {
        var queryString = ToQueryString(args);
        var payload = new Dictionary<string, string>
        {
            { "access_key" , _accessKey },
            { "nonce" , Guid.NewGuid().ToString() },
            { "query_hash" , ToSHA512(queryString) },
            { "query_hash_alg" , "SHA512" },
        };
        if (args.Count == 0)
        {
            payload.Remove("query_hash");
            payload.Remove("query_hash_alg");
        }

        IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
        IJsonSerializer serializer = new JsonNetSerializer();
        IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
        IJwtEncoder encoder = new JwtEncoder(algorithm, serializer, urlEncoder);
        var token = encoder.Encode(payload, _secretKey);
        return $"Bearer {token}";
    }

    private async Task<RestResponse> NonAuthApiCall(string endPoint, Dictionary<string, string> args = null)
    {
        Uri url = new(_baseUrl, endPoint);
        var urlString = url.ToString().TrimEnd('/');
        if(args != null)
            urlString += $"/?{ToQueryString(args)}";   
        RestClient client = new(new Uri(urlString));
        RestRequest request = new()
        {
            Method = Method.Get
        };
        
        request.AddHeader("Content-Type", "application/json");
        return await client.ExecuteAsync(request);
    } 
    
    private async Task<RestResponse> ApiCall(string endPoint, Method method, Dictionary<string, string> args = null)
    {
        if (args is null)
            args = new Dictionary<string, string>();

        var authenticationToken = GenerateAuthenticationToken(args);
        Uri url = new(_baseUrl, endPoint);
        RestClient client = new(url);
        RestRequest request = new();
        request.Method = method;
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Authorization", authenticationToken);
        if (method == Method.Get || method == Method.Delete)
        {
            var urlString = url.ToString().TrimEnd('/');
            urlString += $"/?{ToQueryString(args)}";
            client = new(new Uri(urlString));
        }
        else
        {
            request.AddJsonBody(args);
        }

        return await client.ExecuteAsync(request);
    }
    
    public async Task<List<ClosedOrderHistory.Response>> GetOrderHistory(ClosedOrderHistory.Request args)
    {
        var response = await ApiCall("orders/closed", Method.Get, GenerateApiCallArgs(args));
        return JsonConvert.DeserializeObject<List<ClosedOrderHistory.Response>>(response.Content);
    }
    
    public async Task<List<Order.Response>> GetOrder(Order.Request args)
    {
        var response = await ApiCall("order", Method.Get, GenerateApiCallArgs(args));
        return JsonConvert.DeserializeObject<List<Order.Response>>(response.Content);
    }
    
    public async Task<List<Orders.Response>> GetOrders(Orders.Request args)
    {
        var response = await ApiCall("orders", Method.Get, GenerateApiCallArgs(args));
        return JsonConvert.DeserializeObject<List<Orders.Response>>(response.Content);
    }
    
    public async Task<List<CoinAddress.Response>> GetCoinAdresses()
    {
        var response = await ApiCall("deposits/coin_addresses", Method.Get);
        return JsonConvert.DeserializeObject<List<CoinAddress.Response>>(response.Content);
    }
    
    public async Task<CoinAddress.Response> GetCoinAdress(CoinAddress.Request args)
    {
        var response = await ApiCall("deposits/coin_address", Method.Get, GenerateApiCallArgs(args));
        return JsonConvert.DeserializeObject<CoinAddress.Response>(response.Content);
    }
    
    public async Task<Chance.Response> GetChance(Chance.Request args)
    {
        var response = await ApiCall("orders/chance", Method.Get, GenerateApiCallArgs(args));
        return JsonConvert.DeserializeObject<Chance.Response>(response.Content);
    }
    
    public async Task<Withdraw.Response> GetWithdraw(Withdraw.Request args)
    {
        var response = await ApiCall("deposit", Method.Get, GenerateApiCallArgs(args));
        return JsonConvert.DeserializeObject<Withdraw.Response>(response.Content);
    }
    
    public async Task<List<Withdraws.Response>> GetWithdraws(Withdraws.Request args)
    {
        var response = await ApiCall("deposits", Method.Get, GenerateApiCallArgs(args));
        return JsonConvert.DeserializeObject<List<Withdraws.Response>>(response.Content);
    }
    
    public async Task<bool> CancelOrder(CancelOrder.Request args)
    {
        var response = await ApiCall("order", Method.Delete, GenerateApiCallArgs(args));
        return response.StatusCode == HttpStatusCode.OK;
    }
    
    public async Task<List<WalletStatus.Response>> GetWalletStatus()
    {
        var response = await ApiCall("status/wallet", Method.Get);
        return JsonConvert.DeserializeObject<List<WalletStatus.Response>>(response.Content);
    }
    
    public async Task<List<ApiKeys.Response>> GetApiKeys()
    {
        var response = await ApiCall("api_keys", Method.Get);
        return JsonConvert.DeserializeObject<List<ApiKeys.Response>>(response.Content);
    }
    
    public async Task<List<Accounts.Response>> GetAccounts()
    {
        var response = await ApiCall("accounts", Method.Get);
        return JsonConvert.DeserializeObject<List<Accounts.Response>>(response.Content);
    }
    
    public async Task<GenerateCoinAddress.Response> GenerateCoinAddress(GenerateCoinAddress.Request args)
    {
        var response = await ApiCall("deposits/generate_coin_address", Method.Post, GenerateApiCallArgs(args));
        return JsonConvert.DeserializeObject<GenerateCoinAddress.Response>(response.Content);
    }
    
    public async Task<DepositKrw.Response> DepositKrw(DepositKrw.Request args)
    {
        var response = await ApiCall("deposits/krw", Method.Post, GenerateApiCallArgs(args));
        return JsonConvert.DeserializeObject<DepositKrw.Response>(response.Content);
    }
    
    public async Task<PlaceOrder.Response> PlaceOrder(PlaceOrder.Request args)
    {
        var response = await ApiCall("orders", Method.Post, GenerateApiCallArgs(args));
        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new Exception($"Error placing order: {response.Content}");
        }
        return JsonConvert.DeserializeObject<PlaceOrder.Response>(response.Content);
    }
    
    public async Task<List<Ticks.Response>> GetTicks(Ticks.Request args)
    {
        var response = await this.NonAuthApiCall("trades/ticks", GenerateApiCallArgs(args));
        return JsonConvert.DeserializeObject<List<Ticks.Response>>(response.Content);
    }
    
    public async Task<List<Ticker>> GetTicker(string symbol)
    {
        var response = await this.NonAuthApiCall($"ticker/?markets={symbol}", null);
        return JsonConvert.DeserializeObject<List<Ticker>>(response.Content);
    }
    
    public async Task<List<MarketCodes>> GetMarketCodes()
    {
        var response = await this.NonAuthApiCall("market/all?isDetails=true", null);
        return JsonConvert.DeserializeObject<List<MarketCodes>>(response.Content);
    }
    
    public async Task<List<Quote>> GetDayCandles(DayCandles.Request args)
    {
        var response = await this.NonAuthApiCall("candles/days", GenerateApiCallArgs(args));
        return JsonConvert.DeserializeObject<List<DayCandles.Response>>(response.Content).ToQuote();
    }
    
    public async Task<List<Candles.Response>> GetWeekCandles(Candles.Request args)
    {
        var response = await this.NonAuthApiCall("candles/weeks", GenerateApiCallArgs(args));
        return JsonConvert.DeserializeObject<List<Candles.Response>>(response.Content);
    }
    
    public async Task<List<Candles.Response>> GetMonthCandles(Candles.Request args)
    {
        var response = await this.NonAuthApiCall("candles/months", GenerateApiCallArgs(args));
        return JsonConvert.DeserializeObject<List<Candles.Response>>(response.Content);
    }
    
    public async Task<List<Quote>> GetMinuteCandles(int unit, Candles.Request args)
    {
        var response = await this.NonAuthApiCall($"candles/minutes/{unit}", GenerateApiCallArgs(args));
        return JsonConvert.DeserializeObject<List<Candles.Response>>(response.Content).ToQuote();
    }
    
    public async Task<List<OrderBook.Response>> GetOrderBooks(string symbol)
    {
        var response = await this.NonAuthApiCall($"/orderbook/?markets={symbol}", null);
        return JsonConvert.DeserializeObject<List<OrderBook.Response>>(response.Content);
    }
}