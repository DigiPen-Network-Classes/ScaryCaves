using ScaryCavesWeb.Actors;
using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Services.Databases;

public interface IWorldDatabase
{
    IEnumerable<ZoneDefinition> Zones { get; }
}

/// <summary>
/// The World consists of zone definition files
/// </summary>
public class WorldDatabase(ILogger<WorldDatabase> logger, IZoneDatabase zoneDatabase) : IWorldDatabase
{
    private ILogger<WorldDatabase> Logger { get; } = logger;
    private IZoneDatabase ZoneDatabase { get; } = zoneDatabase;

    public IEnumerable<ZoneDefinition> Zones => ZoneDatabase.Zones;
}
