using System.Reflection;
using Newtonsoft.Json;
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
        if (stream == null)
        {
            throw new InvalidOperationException("scary-cave.json not found in assembly manifest");
        }
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var zone = JsonConvert.DeserializeObject<ZoneDefinition>(json);
        if (zone?.Name == null || zone.Rooms == null || zone.Mobs == null)
        {
            throw new Exception("deserialization of scary-cave.json failed");
        }
        return new ZoneDatabase([zone]);
    }
}
