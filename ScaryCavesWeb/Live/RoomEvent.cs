using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Live;

public enum RoomEventKind
{
    /// <summary>Something in the room changed: a player or mob came or went.</summary>
    RoomChanged,

    /// <summary>The player opened a newer stream; this one should stop.</summary>
    Superseded,
}

/// <summary>
/// One thing to tell a player's stream.
/// </summary>
/// <param name="Kind">what happened</param>
/// <param name="Room">the room after the change (null when there is nothing to re-render)</param>
/// <param name="Message">the line for the player's message list (null for none)</param>
public sealed record RoomEvent(RoomEventKind Kind, RoomState? Room, string? Message);
