using Newtonsoft.Json;

namespace ScaryCavesWeb.Models;

[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.ZoneDefinition")]
public class ZoneDefinition
{
    [JsonConstructor]
    public ZoneDefinition(string name, List<RoomDefinition> rooms, List<MobDefinition> mobs)
    {
        Name = name;
        Rooms = rooms;
        Mobs = mobs;
    }

    [Id(0)] public string Name { get; }
    [Id(1)] public List<RoomDefinition> Rooms { get; }
    [Id(2)] public List<MobDefinition> Mobs { get; }

    public RoomDefinition? GetRoom(long roomId) => Rooms.SingleOrDefault(r => r.Id == roomId);
    public MobDefinition? GetMobDefinition(string definitionId) => Mobs.SingleOrDefault(m => m.DefinitionId == definitionId);

    public List<string> MobInstanceIds
    {
        get
        {
            return Rooms.SelectMany(r => r.InitialMobs).Select(m => m.InstanceId).ToList();
        }
    }
}
