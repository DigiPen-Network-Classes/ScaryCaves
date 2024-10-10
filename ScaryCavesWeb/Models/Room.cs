using StackExchange.Redis;

namespace ScaryCavesWeb.Models;

// Constructor that takes all required properties for serialization
public class Room(int id, string name, string description, List<string>? initialMobs, IReadOnlyDictionary<Direction, int> exits)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public string Description { get; } = description;
    public List<string> InitialMobs { get; } = initialMobs ?? [];
    public IReadOnlyDictionary<Direction, int> Exits { get; } = exits;

    public string RoomMobsKey => $"room:{Id}:mobs";

    public int? this[Direction d]
    {
        get
        {
            if (Exits.TryGetValue(d, out var roomId))
            {
                return roomId;
            }

            return null;
        }
    }

    public async Task AddMob(IDatabase database, Mob mob)
    {
        await database.ListRightPushAsync(RoomMobsKey, mob.Id.ToString());
    }

    public async Task RemoveAllMobs(IDatabase database)
    {
        // delete any mobs left in this room
        foreach (var mobInstanceId in await database.ListRangeAsync(RoomMobsKey))
        {
            var mobInstanceKey = Mob.GetMobKey(mobInstanceId);
            await database.KeyDeleteAsync(mobInstanceKey);
        }
        await database.KeyDeleteAsync(RoomMobsKey);
    }
}
