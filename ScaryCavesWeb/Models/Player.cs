namespace ScaryCavesWeb.Models;

/// <summary>
/// Serialized to player database for storage
/// </summary>
public class Player
{
    public string Name { get; init; } = "";
    public string Password { get; init; } = "";
    public int CurrentRoomId { get; set; }
}


/// <summary>
/// Reflects a player that is in a room
/// </summary>
/// <param name="p"></param>
/// <param name="r"></param>
public class PlayerRoom(Player p, Room r, List<Mob> mobs)
{
    public Player Player { get; } = p;
    public Room Room { get; } = r;
    public List<Mob> Mobs { get; } = mobs;

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

    public bool Go(Direction direction)
    {
        // can we actually go that way?
        var roomId = Room[direction];
        if (roomId == null)
        {
            // can't get there from here
            return false;
        }
        Player.CurrentRoomId = roomId.Value;
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
            Verb.Go => "/PlayerAction/Goto",
            _ => throw new ArgumentOutOfRangeException()
        };
}
