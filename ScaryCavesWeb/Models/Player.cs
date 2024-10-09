namespace ScaryCavesWeb.Models;

public class Player
{
    public string Name { get; init; } = "";
    public string Password { get; init; } = "";
    public int CurrentRoomId { get; set; }


}

// Constructor that takes all required properties
public class Room(int id, string name, string description, IReadOnlyDictionary<Direction, int> exits)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public string Description { get; } = description;
    public IReadOnlyDictionary<Direction, int> Exits { get; } = exits;

    public int? this[Direction d] {
        get
        {
            if (Exits.TryGetValue(d, out var roomId))
            {
                return roomId;
            }
            return null;
        }
    }
}

public class PlayerRoom(Player p, Room r)
{
    public Player Player { get; } = p;
    public Room Room { get; } = r;

    public List<PlayerAction> GetAvailableActions()
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
