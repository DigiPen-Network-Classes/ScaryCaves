
namespace ScaryCavesWeb.Models;

[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.ZoneDefinition")]
public class ZoneDefinition(string name, List<RoomDefinition> rooms, List<MobDefinition> mobs)
{
    [Id(0)] public string Name { get; } = name;
    [Id(1)] public List<RoomDefinition> Rooms { get; } = rooms;
    [Id(2)] public List<MobDefinition> Mobs { get; } = mobs;

    public RoomDefinition? GetRoom(long roomId) => Rooms.SingleOrDefault(r => r.Id == roomId);
    public MobDefinition? GetMobDefinition(string definitionId) => Mobs.SingleOrDefault(m => m.DefinitionId == definitionId);
}
