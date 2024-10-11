using ScaryCavesWeb.Actors;
using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Services.Databases;

public interface IWorldDatabase
{
    Task ResetZones();
    ZoneDefinition? GetZoneDefinition(string zoneName);
}

/// <summary>
/// The World consists of zone definition files
/// </summary>
public class WorldDatabase(ILogger<WorldDatabase> logger, IClusterClient clusterClient, IZoneDatabase zoneDatabase) : IWorldDatabase
{
    private ILogger<WorldDatabase> Logger { get; } = logger;
    private IClusterClient ClusterClient { get; } = clusterClient;
    private IZoneDatabase ZoneDatabase { get; } = zoneDatabase;

    public ZoneDefinition? GetZoneDefinition(string zoneName)
    {
        return ZoneDatabase.GetZone(zoneName);
    }

    public async Task ResetZones()
    {
        Logger.LogInformation("Resetting World State");

        foreach (var zone in ZoneDatabase.Zones)
        {
            await ResetZone(zone);
        }
    }


    /// <summary>
    /// Reset the world state back to its initial (zone reset) configuration.
    /// </summary>
    /// <remarks>
    /// Must be run at least once to prepare the state of the world (and Orleans)
    /// after that subsequent calls will revert the state back to its beginning.
    /// </remarks>
    public async Task ResetZone(ZoneDefinition zoneDefinition)
    {
        Logger.LogInformation("Resetting Zone State for {ZoneName}", zoneDefinition.Name);
        await ClusterClient.GetGrain<IZoneDefinitionActor>(zoneDefinition.Name).ReloadFrom(zoneDefinition);
    }
}
