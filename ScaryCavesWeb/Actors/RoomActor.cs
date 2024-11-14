using Microsoft.AspNetCore.SignalR;
using ScaryCavesWeb.Actors.Extensions;
using ScaryCavesWeb.Hubs;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services;

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
    Task<Room> ReloadFrom(ZoneDefinition zoneDefinition, RoomDefinition roomDefinition);
}

/// <summary>
/// Compound key:
/// long: the room Id
/// string: the zone identifier (name)
/// </summary>
[Alias("ScaryCavesWeb.Actors.IRoomActor")]
public interface IRoomActor : IGrainWithIntegerCompoundKey
{
    [Alias("PickMoveDirection")]
    Task<Direction?> PickMoveDirection(string mobInstanceId);

    [Alias("EnterPlayer")]
    Task<Room> EnterPlayer(Player player);

    [Alias("EnterMob")]
    Task<Room> EnterMob(MobState mob);

    [Alias("Leave")]
    Task<Room> Leave(IMobile mobile);

    [Alias("Move")]
    Task<Room?> Move(IMobile mobile, Direction direction);
}

public class RoomActor(ILogger<RoomActor> logger,
    [PersistentState(nameof(Room))] IPersistentState<Room> roomState,
    IHubContext<GameHub> hubContext,
    IRandomService randomService) : Grain, IRoomActor, IRoomDefinitionActor
{
    private ILogger<RoomActor> Logger { get; } = logger;
    private IPersistentState<Room> RoomState { get; } = roomState;
    private IHubContext<GameHub> HubContext { get; } = hubContext;
    private IRandomService Random { get; } = randomService;

    public async Task<Room> Get()
    {
        if (!RoomState.RecordExists)
        {
            // we need to reload the room state from the zone
            await Reload();
        }

        return RoomState.State;
    }

    public async Task<Room> ReloadFrom(ZoneDefinition zoneDefinition, RoomDefinition roomDefinition)
    {
        var roomId = this.GetPrimaryKeyLong(out var zoneName);
        ArgumentOutOfRangeException.ThrowIfNotEqual(roomDefinition.Id, roomId);
        RoomState.State = new Room(zoneName, roomDefinition);
        RoomState.State.ClearMobs();
        foreach (var mobIdentifier in roomDefinition.InitialMobs)
        {
            var mobDefinition = zoneDefinition.GetMobDefinition(mobIdentifier.DefinitionId);
            if (mobDefinition == null)
            {
                throw new Exception("No mob definition found for " + mobIdentifier.DefinitionId);
            }
            RoomState.State.AddMob(new MobState(mobIdentifier, mobDefinition));
            // tell the individual to reload too:
            await GrainFactory.GetGrain<IMobActor>(mobIdentifier.InstanceId).Reload(zoneName, roomId, mobDefinition);
        }
        await RoomState.WriteStateAsync();
        return RoomState.State;
    }

    /// <summary>
    /// Reload this room state
    /// Note: must use the Key of the actor to determine the Zone Name and Room ID,
    /// since the state at this point is not reliable.
    /// </summary>
    /// <returns></returns>
    private async Task Reload()
    {
        var myRoomId = this.GetPrimaryKeyLong(out var zoneName);
        var (zoneDefinition, roomDefinition) = await GrainFactory.GetGrain<IZoneDefinitionActor>(zoneName).GetRoomDefinition(myRoomId);
        if (roomDefinition == null)
        {
            throw new Exception($"Room {myRoomId} not found in zone {zoneName}");
        }

        await ReloadFrom(zoneDefinition, roomDefinition);
    }

    public async Task<Direction?> PickMoveDirection(string mobInstanceId)
    {
        var room = await Get();
        // debug check:
        if (room.MobsInRoom.SingleOrDefault(m => m.InstanceId == mobInstanceId) == null)
        {
            Logger.LogWarning("Mob {MobInstanceId} wants to wander, but it is not in room {RoomId}", mobInstanceId, RoomState.State.Id);
            return null;
        }

        // pick a random direction from this room's exits
        return Random.PickFrom(room.Exits.Select(exit => exit.Direction).ToList());
    }

    public async Task<Room?> Move(IMobile mover, Direction direction)
    {
        var room = await Get();

        var destination = room[direction];
        if (destination == null)
        {
            Logger.LogWarning("Room {RoomId}: {MoverType} {Id} asked to go {Direction} but that is not a valid direction", room.Id, mover.GetType().Name, mover.Id, direction);
            return null;
        }
        // special case: destination == source
        if (destination.Equals(ThisLocation))
        {
            Logger.LogWarning("Room {RoomId}: {MoverType} {Id} asked to go {Direction} but that goes to the same room!", room.Id, mover.GetType().Name, mover.Id, direction);
            return room;
        }
        await Leave(mover);
        var actor = GrainFactory.GetRoomActor(destination.Value);
        switch (mover)
        {
            case Player p:
                return await actor.EnterPlayer(p);
            case Mob m:
                return await actor.EnterMob(new MobState(m));
            default:
                throw new Exception("Unknown mobile type");
        }
    }

    private Location ThisLocation
    {
        get
        {
            var id = this.GetPrimaryKeyLong(out var zoneName);
            return new Location(id, zoneName);
        }
    }

    public async Task<Room> EnterPlayer(Player player)
    {
        var room = await Get();
        Logger.LogInformation("Player {Name} is entering room {RoomId} in zone {ZoneName}", player.Name, room.Id, room.ZoneName);
        room.AddPlayer(player.Name);
        // add player connection to the room group for messages:
        var groupName = room.Location.ToString();
        await HubContext.Clients.Group(groupName).SendAsync("PlayerEntered", player.Name);
        await HubContext.Groups.AddToGroupAsync(player.ConnectionId, groupName);
        await RoomState.WriteStateAsync();
        return room;
    }
    public async Task<Room> EnterMob(MobState mob)
    {
        var room = await Get();
        Logger.LogInformation("Mob {InstanceId} is entering room {RoomId} in zone {ZoneName}", mob.InstanceId, room.Id, room.ZoneName);
        room.AddMob(mob);

        // tell room mob is here
        var groupName = room.Location.ToString();
        await HubContext.Clients.Group(groupName).SendAsync("MobEntered", mob);

        await RoomState.WriteStateAsync();
        return room;
    }

    public async Task<Room> Leave(IMobile mobile)
    {
        var room = await Get();
        var roomGroup = room.Location.ToString();
        switch (mobile)
        {
            case Player p:
            {
                room.RemovePlayer(p.Name);
                Logger.LogInformation("Player {Name} is leaving Room {RoomGroup}", p.Name, roomGroup);
                await HubContext.Groups.RemoveFromGroupAsync(p.ConnectionId, roomGroup);
                await HubContext.Clients.Group(roomGroup).SendAsync("PlayerLeft", p.Name);
                break;
            }
            case Mob m:
                var mobState = room.RemoveMob(m.InstanceId);
                if (mobState == null) {
                    Logger.LogWarning("Mob {InstanceId} said they left this room, but they aren't found in room {RoomId}?", m.InstanceId, room.Id);
                    return room;
                }
                await HubContext.Clients.Group(roomGroup).SendAsync("MobLeft", mobState);
                break;
            default:
                throw new Exception("Unknown mobile type");
        }
        await RoomState.WriteStateAsync();
        Logger.LogInformation("{Id} is leaving room {RoomId} ", mobile.Id, room.Id);

        return room;
    }
}
