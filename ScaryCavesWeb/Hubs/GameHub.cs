using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using ScaryCavesWeb.Actors;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services.Chat;
using StackExchange.Redis;

namespace ScaryCavesWeb.Hubs;


[Authorize]
public class GameHub(ILogger<GameHub> logger,
    IClusterClient clusterClient,
    IConnectionMultiplexer connectionMultiplexer,
    IChannelPartition channelPartition) : Hub
{
    private ILogger<GameHub> Logger { get; } = logger;
    private IClusterClient ClusterClient { get; } = clusterClient;
    private IConnectionMultiplexer Redis { get; } = connectionMultiplexer;
    private IChannelPartition ChannelPartition { get; } = channelPartition;

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
        var playerView = await ClusterClient.GetGrain<IPlayerActor>(playerName).BeginSession(Context.ConnectionId);

        await Clients.Caller.SendAsync("ReceiveMessage", $"Welcome {playerName} to the Scary Cave!");
        await Clients.Caller.SendAsync("UpdatePlayerView", playerView);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var playerName = PlayerName;
        if (string.IsNullOrEmpty(playerName))
        {
            Logger.LogInformation("Null Player, so not ending session, bye.");
            return;
        }
        Logger.LogInformation("Goodbye Player {PlayerName}", playerName);
        try
        {
            var room = await ClusterClient.GetGrain<IPlayerActor>(playerName).EndSession();
            Logger.LogInformation("Player {PlayerName} end session; left {RoomId}", playerName, room?.Id);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error ending session for {PlayerName}", PlayerName);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Player requests to move from their current location in a given direction
    /// </summary>
    /// <param name="direction"></param>
    /// <returns>new view if success, null otherwise</returns>
    public async Task<ClientPlayerView?> MoveTo(string direction)
    {
        if (!Enum.TryParse<Direction>(direction, out var d))
        {
            Logger.LogError("Player {PlayerName} MoveTo called with invalid direction {Direction}", PlayerName, direction);
            await Clients.Caller.SendAsync("ReceiveMessage", "Invalid direction");
            return null;
        }

        Logger.LogInformation("Player {PlayerName} wants to go {Direction}", PlayerName, d);
        try
        {
            var destinationRoom = await ClusterClient.GetGrain<IPlayerActor>(PlayerName).MoveTo(d);
            if (destinationRoom == null)
            {
                Logger.LogInformation("Player {PlayerName} tried to go {Direction} but was not allowed to.", PlayerName, d);
                await Clients.Caller.SendAsync("ReceiveMessage", "You can't go that way!");
                return null;
            }

            var roomState = new ClientPlayerView(new Player(AccountId, PlayerName ?? "", destinationRoom.Id, destinationRoom.ZoneName, Context.ConnectionId), destinationRoom);
            await Clients.Caller.SendAsync("UpdatePlayerView", roomState);
            return roomState;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error moving player {PlayerName} {Direction}, good bye", PlayerName, d);
            await Clients.Caller.SendAsync("ReceiveMessage", "Error moving player");
            Context.Abort();
            return null;
        }
    }

    public async Task<bool> SendMessage(string channelName, string message)
    {
        var actorId = await ChannelPartition.GetChannelActorId(channelName);
        await ClusterClient.GetGrain<IChatSubscriberActor>(actorId).Awake();

        var key = await ChannelPartition.GetChannel(channelName, AccountId);
        Logger.LogInformation("Player {PlayerName} sending message {Message} for channel {Channel}", PlayerName, message, key);

        var db = Redis.GetSubscriber();
        var payload = JsonConvert.SerializeObject(new ChatMessage(PlayerName ?? "Unknown", AccountId, message));
        await db.PublishAsync(key, payload);
        return true;
    }
}
