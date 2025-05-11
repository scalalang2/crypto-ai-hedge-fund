using Json.Schema.Generation;
using TradingAgent.Agents.Messages.Shared;

namespace TradingAgent.Agents.Messages.AnalysisTeam;

[Title("sentiment-analysis-response")]
public class SentimentAnalysisResponse
{
    [Description("The content")]
    public string Content { get; set; } = string.Empty;
    
    [Description("The sentiment of social activities. It must be use one of the following: Positive, Negative, Neutral.")]
    public string Sentiment { get; set; } = string.Empty;
    
    [Description("The impact score is a number between 0.0 and 100.0, where 100 means the social content is very impactful.")]
    public double ImpactScore { get; set; }

    [Description("the list of topics related to this")]
    public List<string> Topics { get; set; } = new List<string>();
    
    public MarketContext MarketContext { get; set; } = new MarketContext();
}