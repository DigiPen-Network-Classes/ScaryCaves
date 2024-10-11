
using Newtonsoft.Json;

namespace ScaryCavesWeb.Models;

[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.RoomDefinition")]
public class RoomDefinition(long id, string name, string description, List<string>? initialMobs, IReadOnlyDictionary<Direction, long> exits)
{
    [Id(0)]
    public long Id { get; } = id;
    [Id(1)]
    public string Name { get; } = name;
    [Id(2)]
    public string Description { get; } = description;
    [Id(3)]
    public List<string> InitialMobs { get; } = initialMobs ?? [];
    [Id(4)]
    public IReadOnlyDictionary<Direction, long> Exits { get; } = exits;
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
    }

    [JsonConstructor]
    public Room(long id, string name, string description, IReadOnlyDictionary<Direction, long> exits, HashSet<string>? playersInRoom, string zoneName)
    {
        Id = id;
        Name = name;
        Description = description;
        Exits = exits;
        PlayersInRoom = playersInRoom ?? [];
        ZoneName = zoneName;
    }

    private Location? GetExit(Direction d) => Exits.TryGetValue(d, out var roomId) ? new Location(roomId) : null;

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
}
