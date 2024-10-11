
namespace ScaryCavesWeb.Models;

/// <summary>
/// Serialized to player database for storage
/// </summary>
[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.Player")]
public class Player
{
    public Player() {}

    public Player(Guid ownerAccountId, string playerName)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(ownerAccountId, Guid.Empty);
        ArgumentException.ThrowIfNullOrEmpty(playerName);
        OwnerAccountId = ownerAccountId;
        Name = playerName;
        CurrentRoomId = Location.StartLocation.RoomId;
        CurrentZoneName = Location.StartLocation.ZoneName;
    }

    public Location GetCurrentLocation() => new(CurrentRoomId, CurrentZoneName);

    [Id(0)] public string Name { get; set; } = "";

    [Id(1)] public long CurrentRoomId { get; set; }

    [Id(2)] public string CurrentZoneName { get; set; } = Zone.DefaultZoneName;

    [Id(3)] public Guid OwnerAccountId { get; set; }

    public void SetCurrentLocation(Location currentLocation)
    {
        CurrentRoomId = currentLocation.RoomId;
        CurrentZoneName = currentLocation.ZoneName;
    }
}


/// <summary>
/// Reflects a player that is in a room
/// </summary>
/// <param name="p"></param>
/// <param name="r"></param>
public class PlayerRoom(Player p, Room r, List<Mob>? mobs=null)
{
    public Player Player { get; } = p;
    public Room Room { get; } = r;
    public List<Mob> Mobs { get; } = mobs ?? [];

    public List<PlayerAction> GetAvailableMovement()
    {
        var actions = Room
            .Exits
            .Select(exit => new PlayerAction($"GO {exit.Key}", Verb.Go, exit.Key))
            .ToList();

        // pick up ITEM
        // Attack Mob
        // Open door
        return actions;
    }

    public bool Attack(string mobInstanceId)
    {
        // can we actually attack that mob?
        var mobGuid = Guid.Parse(mobInstanceId);
        var mob = Mobs?.FirstOrDefault(m => m.Id == mobGuid);
        if (mob == null)
        {
            // no such mob
            return false;
        }
        // eh... now what?
        return true;
    }
}

public enum Verb
{
    Go,
}

public enum Direction
{
    North,
    East,
    South,
    West,
    Up,
    Down
}

public class PlayerAction(string text, Verb v, Direction? direction)
{
    public string Text { get; } = text;
    public Verb Verb { get; } = v;
    public Direction? Direction { get; } = direction;

    public string ActionName =>
        Verb switch
        {
            Verb.Go => "/PlayerAction/MoveTo",
            _ => throw new ArgumentOutOfRangeException()
        };
}
