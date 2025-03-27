namespace ScaryCavesWeb.Services.Chat;

public class ChatMessage(string playerName, Guid accountId, string message)
{
    public string PlayerName { get; set; } = playerName;
    public Guid AccountId { get; set; } = accountId;
    public string Message { get; set; } = message;
}
