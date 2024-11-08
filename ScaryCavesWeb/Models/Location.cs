
namespace ScaryCavesWeb.Models;

public readonly record struct Location
{
    [Id(1)] public long RoomId { get; }
    [Id(2)] public string ZoneName { get; }

    public Location(long roomId, string zoneName)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(roomId, 0);
        RoomId = roomId;
        ZoneName = zoneName;
    }

    public override string ToString()
    {
        return $"Zone: {ZoneName} Room: {RoomId}";
    }
}
