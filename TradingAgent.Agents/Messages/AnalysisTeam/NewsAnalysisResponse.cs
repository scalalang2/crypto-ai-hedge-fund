using Json.Schema.Generation;

namespace TradingAgent.Agents.Messages.AnalysisTeam;

[Title("news-analysis-response")]
public class NewsAnalysisResponse
{
    [Description("The list of news analysis results.")]
    public List<NewsAnalysisResult> NewsAnalysisResult { get; set; } = [];
}

[Title("news-analysis-result")]
public class NewsAnalysisResult
{
    [Description("The title of the news article.")]
    public string Headline { get; set; } = string.Empty;
    
    [Description("The summary of the news article.")]
    public string Summary { get; set; } = string.Empty;

    [Description("The sentiment of the news article. It must be use one of the following: Positive, Negative, Neutral.")]
    public string Sentiment { get; set; } = string.Empty;
    
    [Description("The confidence level of the news analysis. It must be a number between 0.0 and 100.0")]
    public double Confidence { get; set; } = 0.0;
    
    [Description("The timestamp of the news article.")]
    public DateTime Timestamp { get; set; }
    
    [Description("The list of related crypto assets mentioned in the news article.")]
    public List<string> RelatedAssets { get; set; } = [];
}