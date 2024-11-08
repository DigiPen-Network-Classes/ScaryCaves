using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Actors;

/// <summary>
/// Compound key:
/// long: the room Id
/// string: the zone identifier (name)
/// </summary>
[Alias("ScaryCavesWeb.Actors.IRoomDefinitionActor")]
public interface IRoomDefinitionActor : IGrainWithIntegerCompoundKey
{
    [Alias("ReloadFrom")]
    Task<Room> ReloadFrom(RoomDefinition roomDefinition);
}

/// <summary>
/// Compound key:
/// long: the room Id
/// string: the zone identifier (name)
/// </summary>
[Alias("ScaryCavesWeb.Actors.IRoomActor")]
public interface IRoomActor : IGrainWithIntegerCompoundKey
{
    [Alias("GetRoom")]
    Task<Room> GetRoom();

    [Alias("Enter")]
    Task<Room> Enter(Player player);

    [Alias("Leave")]
    Task<Room> Leave(Player player);

    [Alias("Move")]
    Task<Room?> Move(Player player, Direction direction);
}

/// <summary>
/// TODO Add pubsub for entering, leaving rooms
/// TODO add mobs to rooms
/// TODO Add TTL to state: if we notice our state has b een cleaned up, retrieve a new copy from world database
/// </summary>
public class RoomActor(ILogger<RoomActor> logger,
    [PersistentState(nameof(Room))] IPersistentState<Room> roomState) : Grain, IRoomActor, IRoomDefinitionActor
{
    private ILogger<RoomActor> Logger { get; } = logger;
    private IPersistentState<Room> RoomState { get; } = roomState;

    public async Task<Room> GetRoom()
    {
        if (!RoomState.RecordExists)
        {
            // we need to reload the room state from the zone
            await Reload();
        }
        return RoomState.State;
    }

    public async Task<Room> ReloadFrom(RoomDefinition roomDefinition)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(roomDefinition.Id, this.GetPrimaryKeyLong(out var zoneName));
        RoomState.State = new Room(zoneName, roomDefinition);
        roomDefinition.InitialMobs.ForEach(mob => RoomState.State.AddMob(mob));
        await RoomState.WriteStateAsync();
        return RoomState.State;
    }

    /// <summary>
    /// Reload this actor's room state
    ///
    /// Note: must use the Key of the actor to determine the
    /// Zone Name and Room ID, since the state at this point is not reliable.
    /// </summary>
    /// <returns></returns>
    public async Task<Room> Reload()
    {
        var myRoomId = this.GetPrimaryKeyLong(out var zoneName);
        var roomDefinition = await GrainFactory.GetGrain<IZoneDefinitionActor>(zoneName).GetRoomDefinition(myRoomId);
        if (roomDefinition == null)
        {
            throw new Exception($"Room {myRoomId} not found in zone {zoneName}");
        }

        return await ReloadFrom(roomDefinition);
    }

    public async Task<Room?> Move(Player player, Direction direction)
    {
        var room = await GetRoom();

        var destination = room[direction];
        if (destination == null)
        {
            // TODO log context of room ID and player name / id
            Logger.LogWarning("Room {RoomId}: Player {PlayerName} asked to go {Direction} but that is not a valid direction", room.Id, player.Name, direction);
            return null;
        }
        // special case: destination == source
        if (destination.Equals(ThisLocation))
        {
            // TODO log context of room ID and player name / id
            Logger.LogWarning("Room {RoomId}: Player {PlayerName} asked to go {Direction} but that goes to the same room!", room.Id, player.Name, direction);
            return room;
        }
        // otherwise, move the player
        await Leave(player);
        return await GrainFactory.GetGrain<IRoomActor>(destination.Value.RoomId, destination.Value.ZoneName).Enter(player);
    }

    private Location ThisLocation
    {
        get
        {
            var id = this.GetPrimaryKeyLong(out var zoneName);
            return new Location(id, zoneName);
        }
    }

    public async Task<Room> Enter(Player player)
    {
        var room = await GetRoom();
        room.AddPlayer(player.Name);
        await RoomState.WriteStateAsync();
        return room;
    }

    public async Task<Room> Leave(Player player)
    {
        var room = await GetRoom();
        room.RemovePlayer(player.Name);
        await RoomState.WriteStateAsync();
        return room;
    }
}
