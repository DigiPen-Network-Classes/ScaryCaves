using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using ScaryCavesWeb.Actors;
using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Hubs;

public class GameHub(ILogger<GameHub> logger, IClusterClient clusterClient) : Hub
{
    private ILogger<GameHub> Logger { get; } = logger;
    private IClusterClient ClusterClient { get; } = clusterClient;

    private string? PlayerName => Context.User?.FindFirst(ClaimTypes.Name)?.Value;

    private Guid AccountId => Context.User?.FindFirst(ClaimTypes.NameIdentifier) is { Value: { } id } ? Guid.Parse(id) : Guid.Empty;

    private async Task<RoomState?> GetPlayerRoomState()
    {
        var player = await GetPlayer();
        if (player == null)
        {
            return null;
        }
        var room = await GetRoom(player.GetCurrentLocation());
        if (room == null)
        {
            return null;
        }

        return new RoomState(player, room);
    }

    private async Task<Player?> GetPlayer()
    {
        if (AccountId  == Guid.Empty || string.IsNullOrEmpty(PlayerName))
        {
            Logger.LogError("Can't get Player: AccountId {AccountId} or PlayerName {PlayerName} not set", AccountId, PlayerName);
            return null;
        }
        var player = await ClusterClient.GetGrain<IPlayerActor>(PlayerName).Get();
        if (!player.IsValid(AccountId))
        {
            Logger.LogError("Retrieved invalid player: {@Player}", player);
        }
        return player;
    }

    private async Task<Room?> GetRoom(Location location)
    {
        return await ClusterClient.GetGrain<IRoomActor>(location.RoomId, location.ZoneName).GetRoom(PlayerName);
    }

    public override async Task OnConnectedAsync()
    {
        var playerName = Context.User?.Identity?.Name;
        if (playerName == null)
        {
            Logger.LogError("PlayerName is null");
            await Clients.Caller.SendAsync("ReceiveMessage", "You must be logged in to be in the Scary Cave! (Authentication failure)");
            // TODO disconnect?
            return;
        }

        await Clients.Caller.SendAsync("ReceiveMessage", $"Welcome {playerName} to the Scary Cave!");
        var player = await GetPlayer();
        if (player == null)
        {
            Logger.LogError("Failed to retrieve player for {Account}/{PlayerName}", AccountId, PlayerName);
            await Clients.Caller.SendAsync("ReceiveMessage", "You must be logged in to be in the Scary Cave! (Player not found)");
            // TODO disconnect?
            return;
        }
        var room = await GetRoom(player.GetCurrentLocation());
        if (room == null)
        {
            Logger.LogError("Failed to retrieve room for {Account}/{PlayerName} in {Location}", AccountId, PlayerName, player.GetCurrentLocation());
            await Clients.Caller.SendAsync("ReceiveMessage", "You must be logged in to be in the Scary Cave! (Room not found)");
            return;
        }

        await Clients.Caller.SendAsync("UpdateRoomState", new RoomState(player, room));

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Player requests to move from their current location in a given direction
    /// </summary>
    /// <param name="direction"></param>
    /// <returns>true if they can, false if they cannot (for any reason)</returns>
    public async Task<bool> MoveTo(string direction)
    {
        if (!Enum.TryParse<Direction>(direction, out var d))
        {
            Logger.LogError("Player {PlayerName} MoveTo called with invalid direction {Direction}", PlayerName, direction);
            await Clients.Caller.SendAsync("ReceiveMessage", "Invalid direction");
            return false;
        }

        Logger.LogInformation("Player {PlayerName} is attempting to go {Direction}", PlayerName, d);
        var success = await ClusterClient.GetGrain<IPlayerActor>(PlayerName).MoveTo(d);
        if (!success)
        {
            Logger.LogInformation("Player {PlayerName} tried to go {Direction} but was not allowed to.", PlayerName, d);
            await Clients.Caller.SendAsync("ReceiveMessage", "You can't go that way!");
        }
        else
        {
            var roomState = await GetPlayerRoomState();
            await Clients.Caller.SendAsync("UpdateRoomState", roomState);
        }
        return success;
    }

    //
    // public async Task PlayerJoined(string playerName)
    // {
    //     await Clients.All.SendAsync("PlayerJoined", playerName);
    // }
}
