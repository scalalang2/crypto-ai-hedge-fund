using Json.Schema.Generation;
using TradingAgent.Agents.Messages.Shared;

namespace TradingAgent.Agents.Messages.ResearchTeam;

[Title("buy-evidence")]
public class BuyEvidence
{
    [Description("Confidence is a number between 0 and 100, where 100 means you are very confident in your analysis")]
    public double Confidence { get; set; } = 0.0;
    
    [Description("A concise explanation of the evidence and reasoning supporting a buy recommendation for this asset.")]
    public string Reasoning { get; set; } = string.Empty;
}

[Title("sell-evidence")]
public class SellEvidence
{
    [Description("Confidence is a number between 0 and 100, where 100 means you are very confident in your analysis")]
    public double Confidence { get; set; } = 0.0;

    [Description("A concise explanation of the evidence and reasoning supporting a sell recommendation for this asset.")]
    public string Reasoning { get; set; } = string.Empty;
}

public class ResearchResultResponse
{
    public MarketContext MarketContext { get; set; } = new();
    
    public BuyEvidence BuyEvidences { get; set; } = new();

    public SellEvidence SellEvidences { get; set; } = new();
}