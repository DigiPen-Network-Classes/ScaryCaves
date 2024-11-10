using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services;

namespace ScaryCavesWeb.Actors;

[Alias("ScaryCavesWeb.Actors.IPlayerActor")]
public interface IPlayerActor : IGrainWithStringKey
{
    [Alias("BeginSession")]
    Task BeginSession();

    [Alias("EndSession")]
    Task EndSession();

    [Alias("Get")]
    Task<Player> Get();

    [Alias("StartOver")]
    Task<bool> StartOver();

    [Alias("TeleportTo")]
    Task<bool> TeleportTo(long roomId, string zoneName);

    [Alias("MoveTo")]
    Task<bool> MoveTo(Direction direction);

    [Alias("Create")]
    Task<bool> Create(Guid ownerAccountId);
}

public class PlayerActor(ILogger<PlayerActor> logger,
    ScaryCaveSettings settings,
    [PersistentState(nameof(Player))] IPersistentState<Player> playerState) : Grain, IPlayerActor
{
    private ILogger<PlayerActor> Logger { get; } = logger;
    public ScaryCaveSettings Settings { get; } = settings;

    /// <summary>
    /// TODO lifetime of state needs to line up with redis lifetime of account/player
    /// </summary>
    private IPersistentState<Player> PlayerState { get; } = playerState;

    private Player Player => PlayerState.State;

    public async Task<bool> Create(Guid ownerAccountId)
    {
        PlayerState.State  = new Player(ownerAccountId, this.GetPrimaryKeyString(),  Settings.DefaultRoomId, Settings.DefaultZoneName);
        await PlayerState.WriteStateAsync();
        return true;
    }

    public async Task BeginSession()
    {
        var location = Player.GetCurrentLocation();
        await GrainFactory.GetGrain<IRoomActor>(location.RoomId, location.ZoneName).Enter(Player);
    }

    public Task EndSession()
    {
        return Task.CompletedTask;
    }

    public Task<Player> Get()
    {
        return Task.FromResult(Player);
    }

    public async Task<bool> StartOver()
    {
        // TODO reset player stats (hp, etc)
        // TODO reset player inventory
        // TODO reset player location
        return await TeleportTo(Settings.DefaultRoomId, Settings.DefaultZoneName);
    }

    public async Task<bool> TeleportTo(long roomId, string zoneName)
    {
        // TODO permissions, etc.
        var location = new Location(roomId, zoneName);
        if (location == Player.GetCurrentLocation())
        {
            // nothing to do
            return true;
        }
        Logger.LogInformation("Player {PlayerName} is teleporting to location {Location}", Player.Name, location);
        _ = await GrainFactory.GetGrain<IRoomActor>(Player.CurrentRoomId, Player.CurrentZoneName).Leave(Player);
        var newRoom = await GrainFactory.GetGrain<IRoomActor>(location.RoomId, location.ZoneName).Enter(Player);
        Player.SetCurrentLocation(newRoom.Location);
        await PlayerState.WriteStateAsync();
        return true;
    }

    public async Task<bool> MoveTo(Direction direction)
    {
        Logger.LogInformation("Player {PlayerName} wants to move {Direction}", Player.Name, direction);
        var destination = await GrainFactory.GetGrain<IRoomActor>(Player.CurrentRoomId, Player.CurrentZoneName).Move(Player, direction);
        if (destination == null)
        {
            // can't go that way
            Logger.LogInformation("Player {PlayerName} tried to go {Direction} but was not allowed to.", Player.Name, direction);
            return false;
        }

        var start = Player.GetCurrentLocation();
        Player.SetCurrentLocation(destination.Location);
        await PlayerState.WriteStateAsync();
        var success = start != Player.GetCurrentLocation();
        if (!success)
        {
            Logger.LogWarning("Player {PlayerName} tried to move {Direction}, but failed.", Player.Name, direction);
        }
        Logger.LogInformation("Player {PlayerName} is now at {Location}", Player.Name, Player.GetCurrentLocation());
        return success;
    }
}
