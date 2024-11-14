namespace ScaryCavesWeb.Models;

[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.RoomState")]
public class RoomState(Room room)
{
    [Id(0)] public long Id { get; } = room.Id;
    [Id(1)] public string Name { get; } = room.Name;
    [Id(2)] public string Description { get; } = room.Description;
    [Id(3)] public List<Direction> Exits { get; } = room.Exits.Select(e => e.Direction).ToList();
    [Id(4)] public List<string> PlayersInRoom { get; } = room.PlayersInRoom.ToList();
    [Id(5)] public string ZoneName { get; set; } = room.ZoneName;
    [Id(6)] public List<MobState> MobsInRoom { get; } = room.MobsInRoom.ToList();
}
