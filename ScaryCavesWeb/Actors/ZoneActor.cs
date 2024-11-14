using System.Runtime.CompilerServices;
using ScaryCavesWeb.Actors.Extensions;
using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Actors;

[Alias("ScaryCavesWeb.Actors.IZoneDefinitionActor")]
public interface IZoneDefinitionActor : IGrainWithStringKey
{
    [Alias("GetRoomDefinition")]
    Task<(ZoneDefinition,RoomDefinition?)> GetRoomDefinition(long roomId);

    [Alias("Reload")]
    Task Reload();

    [Alias("ReloadFrom")]
    Task ReloadFrom(ZoneDefinition roomDefinition);
}

public class ZoneActor(ILogger<ZoneActor> logger,
    [PersistentState(nameof(ZoneDefinition))] IPersistentState<ZoneDefinition> zoneDefinitionState): Grain, IZoneDefinitionActor
{

    private ILogger<ZoneActor> Logger { get; } = logger;
    private IPersistentState<ZoneDefinition> ZoneDefinitionState { get; } = zoneDefinitionState;
    private ZoneDefinition ZoneDefinition => ZoneDefinitionState.State;


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
}
