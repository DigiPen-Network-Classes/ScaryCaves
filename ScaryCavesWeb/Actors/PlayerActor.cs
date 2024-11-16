using ScaryCavesWeb.Actors.Extensions;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services;

namespace ScaryCavesWeb.Actors;

[Alias("ScaryCavesWeb.Actors.IPlayerActor")]
public interface IPlayerActor : IGrainWithStringKey
{
    [Alias("GetAccountId")]
    Task<Guid?> GetAccountId();

    /// <summary>
    /// Begin a play session; track the connectionId for updates
    /// </summary>
    /// <param name="connectionId"></param>
    /// <returns></returns>
    [Alias("BeginSession")]
    Task<ClientPlayerView> BeginSession(string connectionId);

    /// <summary>
    /// End a play session, returning the room the player was in (if any)
    /// </summary>
    /// <returns>Current Room or null if not in a room for some reason</returns>
    [Alias("EndSession")]
    Task<Room?> EndSession();

    [Alias("StartOver")]
    Task<bool> StartOver();

    [Alias("TeleportTo")]
    Task<bool> TeleportTo(long roomId, string zoneName);

    [Alias("MoveTo")]
    Task<Room?> MoveTo(Direction direction);

    /// <summary>
    /// Create a player with the given owner account ID
    /// </summary>
    /// <param name="ownerAccountId"></param>
    /// <returns></returns>
    [Alias("Create")]
    Task<bool> Create(Guid ownerAccountId);
}

public class PlayerActor(ILogger<PlayerActor> logger,
    ScaryCaveSettings settings,
    [PersistentState(nameof(Player), ScaryCaveSettings.PlayerStorageProvider)] IPersistentState<Player> playerState) : Grain, IPlayerActor
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
        PlayerState.State  = new Player(ownerAccountId, this.GetPrimaryKeyString(),  Settings.DefaultRoomId, Settings.DefaultZoneName, null);
        await PlayerState.WriteStateAsync();
        return true;
    }

    public Task<Guid?> GetAccountId()
    {
        return Task.FromResult<Guid?>(PlayerState.RecordExists ? Player.OwnerAccountId : null);
    }

    public async Task<ClientPlayerView> BeginSession(string connectionId)
    {
        if (Player == null)
        {
            Logger.LogError("BeginSession: Null Player?");
            throw new Exception("Null Player");
        }
        Player.ConnectionId = connectionId;
        await PlayerState.WriteStateAsync();

        // simple keep alive for now (to be replaced by streams ... someday)
        await GrainFactory.GetGrain<IAccountActor>(Player.OwnerAccountId).Ping();

        // at the beginning of a session, lets wake up the mobs:
        await GrainFactory.GetGrain<IZoneActor>(Player.GetCurrentLocation().ZoneName).ActivateMobs();

        var location = Player.GetCurrentLocation();
        var room = await GrainFactory.GetRoomActor(location).EnterPlayer(Player);
        return new ClientPlayerView(Player, room);
    }

    public async Task<Room?> EndSession()
    {
        var location = Player.GetCurrentLocation();
        var room = await GrainFactory.GetRoomActor(location).Leave(Player);

        Player.ConnectionId = "";
        await PlayerState.WriteStateAsync();
        return room;
    }

    public Task<Player> Get()
    {
        return Task.FromResult(Player);
    }

    public async Task<bool> StartOver()
    {
        // TODO reset player stats (hp, etc)
        // TODO reset player inventory
        return await TeleportTo(Settings.DefaultRoomId, Settings.DefaultZoneName);
    }

    public async Task<bool> TeleportTo(long roomId, string zoneName)
    {
        var location = new Location(roomId, zoneName);
        if (location == Player.GetCurrentLocation())
        {
            // nothing to do
            return true;
        }
        Logger.LogInformation("Player {PlayerName} is teleporting to location {Location}", Player.Name, location);
        _ = await GrainFactory.GetRoomActor(Player.GetCurrentLocation()).Leave(Player);
        var newRoom = await GrainFactory.GetRoomActor(location).EnterPlayer(Player);
        Player.SetCurrentLocation(newRoom.Location);
        await PlayerState.WriteStateAsync();
        return true;
    }

    public async Task<Room?> MoveTo(Direction direction)
    {
        Logger.LogInformation("Player {PlayerName} wants to move {Direction}", Player.Name, direction);
        var destination = await GrainFactory.GetRoomActor(Player.GetCurrentLocation()).Move(Player, direction);
        if (destination == null)
        {
            // can't go that way
            Logger.LogInformation("Player {PlayerName} tried to go {Direction} but was not allowed to.", Player.Name, direction);
            return null;
        }

        Player.SetCurrentLocation(destination.Location);
        await PlayerState.WriteStateAsync();
        Logger.LogInformation("Player {PlayerName} is now at {Location}", Player.Name, Player.GetCurrentLocation());
        return destination;
    }
}
