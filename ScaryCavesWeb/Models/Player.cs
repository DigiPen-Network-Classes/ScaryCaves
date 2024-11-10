
using Newtonsoft.Json;

namespace ScaryCavesWeb.Models;

/// <summary>
/// Serialized to player database for storage
/// </summary>
[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.Player")]
public class Player
{
    [Id(0)] public string Name { get; set; }
    [Id(1)] public long CurrentRoomId { get; set; }
    [Id(2)] public string CurrentZoneName { get; set; }
    [Id(3)] public Guid OwnerAccountId { get; set; }

    [JsonConstructor]
    public Player(Guid ownerAccountId, string name, long currentRoomId, string currentZoneName)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(ownerAccountId, Guid.Empty);
        ArgumentException.ThrowIfNullOrEmpty(name);
        OwnerAccountId = ownerAccountId;
        Name = name;
        CurrentRoomId = currentRoomId;
        CurrentZoneName = currentZoneName;
    }

    public Location GetCurrentLocation() => new(CurrentRoomId, CurrentZoneName);

    public void SetCurrentLocation(Location currentLocation)
    {
        CurrentRoomId = currentLocation.RoomId;
        CurrentZoneName = currentLocation.ZoneName;
    }

    /// <summary>
    /// Determine if the Player has deserialized into a valid state.
    /// </summary>
    /// <param name="ownerAccountId">must not be Guid.Empty</param>
    /// <returns>true if things are good, false otherwise</returns>
    public bool IsValid(Guid ownerAccountId)
    {
        // would fail the check anyway, but lets throw to catch this early if it happens:
        ArgumentOutOfRangeException.ThrowIfEqual(ownerAccountId, Guid.Empty);
        // determine that we've deserialized this into a valid state:
        return !string.IsNullOrEmpty(Name) && OwnerAccountId != Guid.Empty && OwnerAccountId == ownerAccountId;
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
