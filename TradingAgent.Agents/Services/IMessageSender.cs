namespace TradingAgent.Agents.Services;

public interface IMessageSender
{
    /// <summary>
    /// 지정된 채널로 메시지를 전송한다
    /// </summary>
    public Task SendMessage(string message);
}