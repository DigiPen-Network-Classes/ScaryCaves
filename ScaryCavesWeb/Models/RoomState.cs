namespace ScaryCavesWeb.Models;

/// <summary>
/// Reflects the state of a Room: players, items, mobs
/// as seen by a particular player (for their actions)
/// </summary>
public class RoomState(Player player, Room room, List<MobState> mobs)
{
    public Player Player { get; } = player;
    public Room Room { get; } = room;
    public List<MobState> Mobs { get; } = mobs;

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
        return false; // TODO
    }
}
