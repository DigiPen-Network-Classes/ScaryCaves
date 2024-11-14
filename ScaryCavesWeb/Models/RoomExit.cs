namespace ScaryCavesWeb.Models;

[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.RoomExit")]
public class RoomExit(Direction direction, long roomId, string zoneName)
{
    [Id(0)] public Direction Direction { get; set; } = direction;
    [Id(1)] public long RoomId { get; set; } = roomId;
    [Id(2)] public string ZoneName { get; set; } = zoneName;

    public Location GetLocation()
    {
        return new Location(RoomId, ZoneName);
    }
}
