using Json.Schema.Generation;
using TradingAgent.Core.Config;

namespace TradingAgent.Agents.Messages.ResearchTeam;

[Title("evidence")]
public class Discussion
{
    [Description("Confidence is a number between 0 and 100, where 100 means you are very confident in your analysis")]
    public double Confidence { get; set; } = 0.0;
    
    [Description("A concise explanation of the evidence and reasoning.")]
    public string Reasoning { get; set; } = string.Empty;
    
    [Description("Sentiment is either Bullish, Neutral or Bearish")]
    public string Signal { get; set; } = string.Empty;
}

public class ResearchResultResponse
{
    public MarketContext MarketContext { get; set; } = new();
    
    public List<Discussion> DiscussionHistory { get; set; } = new();
}