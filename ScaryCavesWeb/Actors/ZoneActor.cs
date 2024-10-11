using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services.Databases;

namespace ScaryCavesWeb.Actors;

[Alias("ScaryCavesWeb.Actors.IZoneActor")]
public interface IZoneActor : IGrainWithStringKey
{

}

[Alias("ScaryCavesWeb.Actors.IZoneDefinitionActor")]
public interface IZoneDefinitionActor : IGrainWithStringKey
{
    [Alias("GetRoomDefinition")]
    Task<RoomDefinition?> GetRoomDefinition(long roomId);

    [Alias("Reload")]
    Task<Zone> Reload();

    [Alias("ReloadFrom")]
    Task<Zone> ReloadFrom(ZoneDefinition roomDefinition);
}

public class ZoneActor(ILogger<ZoneActor> logger,
    IWorldDatabase worldDatabase,
    [PersistentState(nameof(Zone))] IPersistentState<Zone> zoneState): Grain, IZoneActor, IZoneDefinitionActor
{
    private ILogger<ZoneActor> Logger { get; } = logger;
    private IPersistentState<Zone> ZoneState { get; } = zoneState;
    private IWorldDatabase WorldDatabase { get; } = worldDatabase;

    public async Task<RoomDefinition?> GetRoomDefinition(long roomId)
    {
        if (!ZoneState.RecordExists)
        {
            await Reload();
        }

        return ZoneState.State.GetRoom(roomId);
    }

    public async Task<Zone> Reload()
    {
        var zoneDefinition = WorldDatabase.GetZoneDefinition(this.GetPrimaryKeyString());
        if (zoneDefinition == null)
        {
            throw new InvalidOperationException($"No zone definition found for zone {this.GetPrimaryKeyString()}");
        }
        return await ReloadFrom(zoneDefinition);
    }

    public async Task<Zone> ReloadFrom(ZoneDefinition zoneDefinition)
    {
        ZoneState.State = new Zone(zoneDefinition);
        await ZoneState.WriteStateAsync();
        return ZoneState.State;
    }
}
