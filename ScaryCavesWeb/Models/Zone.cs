
namespace ScaryCavesWeb.Models;

[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.ZoneDefinition")]
public class ZoneDefinition(string name, List<RoomDefinition> rooms)
{
    [Id(0)] public string Name { get; set; } = name;
    [Id(1)] public List<RoomDefinition> Rooms { get; set; } = rooms;
}

[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.Zone")]
public class Zone
{
    public Zone()
    {
        ZoneName = DefaultZoneName;
        Rooms = [];
    }
    public Zone(ZoneDefinition zoneDefinition)
    {
        ArgumentNullException.ThrowIfNull(zoneDefinition);
        ArgumentException.ThrowIfNullOrEmpty(zoneDefinition.Name);
        ArgumentNullException.ThrowIfNull(zoneDefinition.Rooms);
        ZoneName = zoneDefinition.Name;
        Rooms = zoneDefinition.Rooms;
    }

    public const string DefaultZoneName = "scary-cave";

    [Id(0)]
    public string ZoneName { get; set; }

    [Id(1)] public List<RoomDefinition> Rooms { get; set; }

    public RoomDefinition? GetRoom(long roomId)
    {
        return Rooms.SingleOrDefault(r => r.Id == roomId);
    }
}
