
namespace ScaryCavesWeb.Models;

[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.ZoneDefinition")]
public class ZoneDefinition(string name, List<RoomDefinition> rooms, List<MobDefinition> mobs)
{
    [Id(0)] public string Name { get; set; } = name;
    [Id(1)] public List<RoomDefinition> Rooms { get; set; } = rooms;
    [Id(2)] public List<MobDefinition> Mobs { get; set; } = mobs;

    public RoomDefinition? GetRoom(long roomId) => Rooms.SingleOrDefault(r => r.Id == roomId);
    public MobDefinition? GetMob(string mobId) => Mobs.SingleOrDefault(m => m.Id == mobId);
}
