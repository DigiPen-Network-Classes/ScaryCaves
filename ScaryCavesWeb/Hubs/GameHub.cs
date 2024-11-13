using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ScaryCavesWeb.Actors;
using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Hubs;


[Authorize]
public class GameHub(ILogger<GameHub> logger, IClusterClient clusterClient) : Hub
{
    private ILogger<GameHub> Logger { get; } = logger;
    private IClusterClient ClusterClient { get; } = clusterClient;

    private string? PlayerName => Context.User?.FindFirst(ClaimTypes.Name)?.Value;

    private Guid AccountId => Context.User?.FindFirst(ClaimTypes.NameIdentifier) is { Value: { } id } ? Guid.Parse(id) : Guid.Empty;

    public override async Task OnConnectedAsync()
    {
        var playerName = Context.User?.Identity?.Name;
        if (playerName == null)
        {
            Logger.LogError("PlayerName is null");
            await Clients.Caller.SendAsync("ReceiveMessage", "You must be logged in to be in the Scary Cave! (Authentication failure)");
            return;
        }
        var roomState = await ClusterClient.GetGrain<IPlayerActor>(playerName).BeginSession(Context.ConnectionId);

        await Clients.Caller.SendAsync("ReceiveMessage", $"Welcome {playerName} to the Scary Cave!");
        await Clients.Caller.SendAsync("UpdateRoomState", roomState);
        //await EnterRoom(roomState.Room, roomState.Player.Name);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Logger.LogInformation("Goodbye Player {PlayerName}", PlayerName);
        var room = await ClusterClient.GetGrain<IPlayerActor>(PlayerName).EndSession();
        Logger.LogInformation("Player {PlayerName} end session; left {RoomId}", PlayerName, room?.Id);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Player requests to move from their current location in a given direction
    /// </summary>
    /// <param name="direction"></param>
    /// <returns>true if they can, false if they cannot (for any reason)</returns>
    public async Task<RoomState?> MoveTo(string direction)
    {
        if (!Enum.TryParse<Direction>(direction, out var d))
        {
            Logger.LogError("Player {PlayerName} MoveTo called with invalid direction {Direction}", PlayerName, direction);
            await Clients.Caller.SendAsync("ReceiveMessage", "Invalid direction");
            return null;
        }

        Logger.LogInformation("Player {PlayerName} is attempting to go {Direction}", PlayerName, d);
        var destinationRoom = await ClusterClient.GetGrain<IPlayerActor>(PlayerName).MoveTo(d);
        if (destinationRoom == null)
        {
            Logger.LogInformation("Player {PlayerName} tried to go {Direction} but was not allowed to.", PlayerName, d);
            await Clients.Caller.SendAsync("ReceiveMessage", "You can't go that way!");
            return null;
        }
        var roomState = new RoomState(new Player(AccountId, PlayerName ?? "", destinationRoom.Id, destinationRoom.ZoneName, Context.ConnectionId), destinationRoom);
        await Clients.Caller.SendAsync("UpdateRoomState", roomState);
        return roomState;
    }
}
