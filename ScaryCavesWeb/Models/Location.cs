
namespace ScaryCavesWeb.Models;

public readonly record struct Location
{
    public static readonly Location StartLocation = new(0);
    [Id(1)] public long RoomId { get; }
    [Id(2)] public string ZoneName { get; }


    public Location(long roomId, string? zoneName = "scary-cave")
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(roomId, 0);
        RoomId = roomId;
        ZoneName = zoneName ?? Zone.DefaultZoneName;
    }

    public override string ToString()
    {
        return $"Zone: {ZoneName} Room: {RoomId}";
    }
}
