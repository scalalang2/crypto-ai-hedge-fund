using Json.Schema.Generation;

namespace TradingAgent.Agents.Messages.ResearchTeam;

[Title("buy-evidence")]
public class BuyEvidence
{
    [Description("Ticker is the ticker symbol of the market you are analyzing. (e.g. 'KRW-BTC', 'KRW-ETH', 'KRW-SOL')")]
    public string Ticker { get; set; } = string.Empty;
    
    [Description("The full name of the asset. (e.g. 'Bitcoin', 'Ethereum', 'Solana' and etc.)")]
    public string Name { get; set; } = string.Empty;
    
    [Description("Confidence is a number between 0 and 100, where 100 means you are very confident in your analysis")]
    public double Confidence { get; set; } = 0.0;
    
    [Description("A concise explanation of the evidence and reasoning supporting a buy recommendation for this asset.")]
    public string Reasoning { get; set; } = string.Empty;
}

[Title("sell-evidence")]
public class SellEvidence
{
    [Description("Ticker is the ticker symbol of the market you are analyzing. (e.g. 'KRW-BTC', 'KRW-ETH', 'KRW-SOL')")]
    public string Ticker { get; set; } = string.Empty;

    [Description("The full name of the asset. (e.g. 'Bitcoin', 'Ethereum', 'Solana')")]
    public string Name { get; set; } = string.Empty;

    [Description("A concise explanation of the evidence and reasoning supporting a sell recommendation for this asset.")]
    public string Reasoning { get; set; } = string.Empty;
}

[Title("research-result-response")]
public class ResearchResultResponse
{
    [Description("Buy Evidence Results")]
    public List<BuyEvidence> BuyEvidences { get; set; } = [];
    
    [Description("Sell Evidence Results")]
    public List<SellEvidence> SellEvidences { get; set; } = [];
}