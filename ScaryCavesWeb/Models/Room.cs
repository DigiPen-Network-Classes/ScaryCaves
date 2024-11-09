
using Newtonsoft.Json;

namespace ScaryCavesWeb.Models;

[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.RoomDefinition")]
public class RoomDefinition
{
    [JsonConstructor]
    public RoomDefinition(long id, string name, string description, IReadOnlyDictionary<Direction, long> exits, List<MobIdentifier>? initialMobs)
    {
        Id = id;
        Name = name;
        Description = description;
        InitialMobs = initialMobs ?? [];
        Exits = exits;
    }

    [Id(0)]
    public long Id { get; }
    [Id(1)]
    public string Name { get; }
    [Id(2)]
    public string Description { get; }
    [Id(3)]
    public List<MobIdentifier> InitialMobs { get; }
    [Id(4)]
    public IReadOnlyDictionary<Direction, long> Exits { get; }
}

[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.Room")]
public class Room
{
    [Id(0)] public long Id { get; }
    [Id(1)] public string Name { get; }
    [Id(2)] public string Description { get; }
    [Id(3)] public IReadOnlyDictionary<Direction, long> Exits { get; }
    [Id(4)] private HashSet<string> PlayersInRoom { get; }
    [Id(5)] public string ZoneName { get; set; }
    [Id(6)] public HashSet<MobState> MobsInRoom { get; }

    [JsonIgnore]
    public Location Location => new(Id, ZoneName);

    public Room(string zoneName, RoomDefinition roomDefinition)
    {
        Id = roomDefinition.Id;
        Name = roomDefinition.Name;
        Description = roomDefinition.Description;
        Exits = roomDefinition.Exits;
        ZoneName = zoneName;
        PlayersInRoom = [];
        MobsInRoom = [];
    }

    [JsonConstructor]
    public Room(long id, string name, string description, IReadOnlyDictionary<Direction, long> exits, HashSet<string>? playersInRoom, string zoneName, HashSet<MobState>? mobsInRoom)
    {
        Id = id;
        Name = name;
        Description = description;
        Exits = exits;
        PlayersInRoom = playersInRoom ?? [];
        ZoneName = zoneName;
        MobsInRoom = mobsInRoom ?? [];
    }

    private Location? GetExit(Direction d) => Exits.TryGetValue(d, out var roomId) ? new Location(roomId, ZoneName) : null;

    [JsonIgnore]
    public Location? this[Direction d] => GetExit(d);

    public void AddPlayer(string playerName)
    {
        PlayersInRoom.Add(playerName);
    }

    public void RemovePlayer(string playerName)
    {
        PlayersInRoom.Remove(playerName);
    }

    public void AddMob(MobState mob)
    {
        MobsInRoom.Add(mob);
    }

    public void RemoveMob(MobState mob)
    {
        MobsInRoom.Remove(mob);
    }

    public void ClearMobs()
    {
        MobsInRoom.Clear();
    }
}
