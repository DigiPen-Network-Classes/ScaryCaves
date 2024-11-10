namespace ScaryCavesWeb.Models;

/// <summary>
/// Reflects the state of a Room: players, items, mobs
/// as seen by a particular player (for their actions)
/// </summary>
[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.RoomState")]
public class RoomState(Player player, Room room)
{
    [Id(0)]
    public Player Player { get; } = player;
    [Id(1)]
    public Room Room { get; } = room;
}
