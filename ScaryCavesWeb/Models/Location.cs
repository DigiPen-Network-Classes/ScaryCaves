
namespace ScaryCavesWeb.Models;

public readonly record struct Location
{
    public long RoomId { get; }
    public string ZoneName { get; }

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
