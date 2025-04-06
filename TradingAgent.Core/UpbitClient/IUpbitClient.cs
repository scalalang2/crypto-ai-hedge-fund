namespace TradingAgent.Core.UpbitClient;

public interface IUpbitClient
{
    Task<List<Order.Response>> GetOrder(Order.Request args);
    Task<List<Orders.Response>> GetOrders(Orders.Request args);
    Task<List<CoinAddress.Response>> GetCoinAdresses();
    Task<CoinAddress.Response> GetCoinAdress(CoinAddress.Request args);
    Task<Chance.Response> GetChance(Chance.Request args);
    Task<Withdraw.Response> GetWithdraw(Withdraw.Request args);
    Task<List<Withdraws.Response>> GetWithdraws(Withdraws.Request args);
    Task<bool> CancelOrder(CancelOrder.Request args);
    Task<List<WalletStatus.Response>> GetWalletStatus();
    Task<List<ApiKeys.Response>> GetApiKeys();
    Task<List<Accounts.Response>> GetAccounts();
    Task<GenerateCoinAddress.Response> GenerateCoinAddress(GenerateCoinAddress.Request args);
    Task<DepositKrw.Response> DepositKrw(DepositKrw.Request args);
    Task<bool> PlaceOrder(PlaceOrder.Request args);
}