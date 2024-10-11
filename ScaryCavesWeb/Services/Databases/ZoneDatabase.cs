using System.Reflection;
using System.Text.Json;
using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Services.Databases;

public interface IZoneDatabase
{
    ZoneDefinition? GetZone(string zoneName);

    IEnumerable<ZoneDefinition> Zones { get; }
}

public class ZoneDatabase(List<ZoneDefinition> zones) : IZoneDatabase
{
    public IEnumerable<ZoneDefinition> Zones { get; } = zones;
    public ZoneDefinition? GetZone(string zoneName)
    {
        return Zones.SingleOrDefault(z => z.Name == zoneName);
    }

    public static ZoneDatabase Build()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("ScaryCavesWeb.Zones.scary-cave.json");
        using var reader = new StreamReader(stream!);
        var json = reader.ReadToEnd();
        var zone = JsonSerializer.Deserialize<ZoneDefinition>(json);
        if (zone == null || zone.Name == null || zone.Rooms == null)
        {
            throw new Exception("deserialization of scary-cave.json failed");
        }
        return new ZoneDatabase([zone]);
    }
}
