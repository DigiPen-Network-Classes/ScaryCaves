using Microsoft.AspNetCore.SignalR;

namespace ScaryCavesWeb.Hubs;

public class GameHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("ReceiveMessage", "Welcome to the Scary Cave!");
        await base.OnConnectedAsync();
    }

    public async Task PlayerJoined(string playerName)
    {
        await Clients.All.SendAsync("PlayerJoined", playerName);
    }

}
