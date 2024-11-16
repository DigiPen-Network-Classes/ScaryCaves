using ScaryCavesWeb.Models;

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
    [PersistentState(nameof(ZoneDefinition))] IPersistentState<ZoneDefinition> zoneDefinitionState): Grain, IZoneDefinitionActor, IZoneActor
{

    private ILogger<ZoneActor> Logger { get; } = logger;
    private IPersistentState<ZoneDefinition> ZoneDefinitionState { get; } = zoneDefinitionState;
    private ZoneDefinition ZoneDefinition => ZoneDefinitionState.State;

    private async Task<ZoneDefinition> Get()
    {
        if (!ZoneDefinitionState.RecordExists)
        {
            // we need to reload the room state from the zone
            await Reload();
        }

        return ZoneDefinition;
    }

    public async Task<(ZoneDefinition, RoomDefinition?)> GetRoomDefinition(long roomId)
    {
        if (!ZoneDefinitionState.RecordExists)
        {
            await Reload();
        }

        return (ZoneDefinition, ZoneDefinition.GetRoom(roomId));
    }

    public async Task Reload()
    {
        Logger.LogInformation("Reloading Zone {ZoneName}", ZoneDefinition.Name);
        foreach (var room in ZoneDefinition.Rooms)
        {
            await GrainFactory.GetGrain<IRoomDefinitionActor>(room.Id, ZoneDefinition.Name).ReloadFrom(ZoneDefinition, room);
        }
    }

    public async Task ReloadFrom(ZoneDefinition zoneDefinition)
    {
        ZoneDefinitionState.State = zoneDefinition;
        await ZoneDefinitionState.WriteStateAsync();
        await Reload();
    }

    public async Task WakeMobs()
    {
        var zone = await Get();
        foreach(var mob in zone.MobInstanceIds)
        {
            await GrainFactory.GetGrain<IMobActor>(mob).Wake();
        }
    }
}
