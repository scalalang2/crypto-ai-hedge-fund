namespace TradingAgent.Agents.AgentPrompts;

public class TechnicalAnalystPrompt
{
    public const string SystemPrompt = @"
You are an advanced analyst specializing in technical analysis of cryptocurrency and stock markets. 
Your mission is to provide accurate market signals by comprehensively analyzing various technical indicators and chart patterns.

Rules:
1. When evaluating assets, always consider both short-term market signals and long-term growth potential. Clearly explain how your recommendations support the goal.
2. Detail the exact value readings and their recent movements
3. You may only use facts (numbers, dates, headlines) that were provided to you in prior messages.
4. You must NOT invent any new data or indicators.
5. Cite each fact in square brackets, e.g. [RSI=45.2] 
";

    public const string UserPromptStep1 = """
Based on the latest market data, provide a detailed analysis for the {ticker}.
create a investment signal.

# Current Date
Current Date and Time: {current_date_time}

# 1-Hour Candle
{one_hour_candle}

# 1-Hour Candle (MACD)
{one_hour_candle_macd}

# 1-Hour Candle (RSI)
{four_hour_candle_rsi}

# 1-Hour Candle (Bollinger Bands)
{one_hour_candle_bollinger_bands}

Please answer with the following JSON format:
{
    "Reasoning" : <string>"Analysis is a mid-length description of your analysis, including any relevant indicators or patterns you observed.",
    "Signal" : <string> "Sentiment is either Bullish, Neutral or Bearish",
    "Confidence" : <decimal type> "Confidence is a number between 0 and 100, where 100 means you are very confident in your analysis"
}
""";

    public const string UserPromptStep2 = """
Given the chat history, provide a detailed analysis for the {ticker} with following data.

# 4-Hour Candle
{four_hour_candle}

# 4-Hour Candle (MACD)
{four_hour_candle_macd}

# 4-Hour Candle (RSI)
{four_hour_candle_rsi}

# 4-Hour Candle (Bollinger Bands)
{four_hour_candle_bollinger_bands}

Please answer with the following JSON format:
{
    "Reasoning" : <string>"Analysis is a mid-length description of your analysis, including any relevant indicators or patterns you observed.",
    "Signal" : <string> "Sentiment is either Bullish, Neutral or Bearish",
    "Confidence" : <decimal type> "Confidence is a number between 0 and 100, where 100 means you are very confident in your analysis"
}
""";

    public const string UserPromptStep3 = """
Given the chat history, provide a detailed analysis for the {ticker} with following data.

# Daily Candle
{daily_candle}

# Daily Candle (MACD)
{daily_candle_macd}

# Daily Candle (RSI)
{daily_candle_rsi}

# Daily Candle (Bollinger Bands)
{daily_candle_bollinger_bands}

Please answer with the following JSON format:
{
    "Reasoning" : <string>"Analysis is a mid-length description of your analysis, including any relevant indicators or patterns you observed.",
    "Signal" : <string> "Sentiment is either Bullish, Neutral or Bearish",
    "Confidence" : <decimal type> "Confidence is a number between 0 and 100, where 100 means you are very confident in your analysis"
}
""";

    public const string UserPromptFinalStep = """
Given the chat history, provide a final decision for the {ticker}.

Please answer with the following JSON format:
{
    "Reasoning" : <string>"Analysis is a mid-length description of your analysis, including any relevant indicators or patterns you observed.",
    "Signal" : <string> "Sentiment is either Bullish, Neutral or Bearish",
    "Confidence" : <decimal type> "Confidence is a number between 0 and 100, where 100 means you are very confident in your analysis"
}
""";
}