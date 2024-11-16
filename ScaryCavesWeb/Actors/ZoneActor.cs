using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services.Databases;

namespace ScaryCavesWeb.Actors;

[Alias("ScaryCavesWeb.Actors.IZoneDefinitionActor")]
public interface IZoneDefinitionActor : IGrainWithStringKey
{
    [Alias("GetRoomDefinition")]
    Task<(ZoneDefinition,RoomDefinition?)> GetRoomDefinition(long roomId);

    [Alias("ReloadFrom")]
    Task ReloadFrom(ZoneDefinition roomDefinition);
}

[Alias("ScaryCavesWeb.Actors.IZoneActor")]
public interface IZoneActor : IGrainWithStringKey
{
    [Alias("WakeMobs")]
    Task WakeMobs();
}
public class ZoneActor(ILogger<ZoneActor> logger,
    IZoneDatabase zoneDatabase,
    [PersistentState(nameof(ZoneDefinition))] IPersistentState<ZoneDefinition> zoneDefinitionState): Grain, IZoneDefinitionActor, IZoneActor
{
    private ILogger<ZoneActor> Logger { get; } = logger;
    private IZoneDatabase ZoneDatabase { get; } = zoneDatabase;

    private IPersistentState<ZoneDefinition> ZoneDefinitionState { get; } = zoneDefinitionState;
    private ZoneDefinition ZoneDefinition => ZoneDefinitionState.State;

    private async Task<ZoneDefinition> Get()
    {
        if (!ZoneDefinitionState.RecordExists)
        {
            // we need to reload the room state from the zone
            var zoneDefinition = ZoneDatabase.GetZone(this.GetPrimaryKeyString());
            if (zoneDefinition == null)
            {
                Logger.LogError("Zone {ZoneId} not found in ZoneDatabase!", this.GetPrimaryKeyString());
                throw new InvalidOperationException($"Zone {this.GetPrimaryKeyString()} not found in ZoneDatabase!");
            }
            await ReloadFrom(zoneDefinition);
        }

        return ZoneDefinition;
    }

    public async Task<(ZoneDefinition, RoomDefinition?)> GetRoomDefinition(long roomId)
    {
        var zone = await Get();
        return (zone, zone.GetRoom(roomId));
    }

    public async Task ReloadFrom(ZoneDefinition zoneDefinition)
    {
        ZoneDefinitionState.State = zoneDefinition;
        await ZoneDefinitionState.WriteStateAsync();
        foreach(var room in zoneDefinition.Rooms)
        {
            await GrainFactory.GetGrain<IRoomDefinitionActor>(room.Id, zoneDefinition.Name).ReloadFrom(zoneDefinition, room);
        }
    }

    public async Task WakeMobs()
    {
        Logger.LogDebug("Waking up the mobs!");
        var zone = await Get();
        foreach(var mob in zone.MobInstanceIds)
        {
            await GrainFactory.GetGrain<IMobActor>(mob).Wake();
        }
    }
}
