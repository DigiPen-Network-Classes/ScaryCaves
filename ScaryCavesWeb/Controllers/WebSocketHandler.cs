using System.Net.WebSockets;
using System.Text;

namespace ScaryCavesWeb.Controllers;

public static class WebSocketHandler
{
    public static async Task HandleWebSocketAsync(HttpContext context, WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!result.CloseStatus.HasValue)
        {
            // Echo the message back to the client
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var serverMessage = $"Server received: {message}";

            // Send message back to client
            var responseBuffer = Encoding.UTF8.GetBytes(serverMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(responseBuffer, 0, responseBuffer.Length), result.MessageType, result.EndOfMessage, CancellationToken.None);

            // Wait for the next message
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }
}
