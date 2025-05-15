namespace TradingAgent.Core.Utils;

public class DateTimeUtil
{
    /// <summary>
    /// Return the current KST date and time as a string in the format "yyyy-MM-ddTHH:mm:ssZ".
    /// </summary>
    public static string CurrentDateTimeToString()
    {
        return DateTime.UtcNow.AddHours(9).ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}