using Newtonsoft.Json;

namespace ScaryCavesWeb.Models;

/// <summary>
/// Serialized to player database for storage
/// </summary>
[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.Player")]
public class Player : IMobile
{
    [Id(0)] public string Name { get; set; }
    [Id(1)] public long CurrentRoomId { get; set; }
    [Id(2)] public string CurrentZoneName { get; set; }
    [Id(3)] public Guid OwnerAccountId { get; set; }

    /// <summary>
    /// SignalR Connection ID, updated on reconnect
    /// </summary>
    [Id(4)]
    public string ConnectionId { get; set; }

    [JsonIgnore] public string Id => Name;

    [JsonConstructor]
    public Player(Guid ownerAccountId, string name, long currentRoomId, string currentZoneName, string? connectionId)
    {
        OwnerAccountId = ownerAccountId;
        Name = name;
        CurrentRoomId = currentRoomId;
        CurrentZoneName = currentZoneName;
        ConnectionId = connectionId ?? "";
    }

    public Location GetCurrentLocation() => new(CurrentRoomId, CurrentZoneName);

    public void SetCurrentLocation(Location currentLocation)
    {
        CurrentRoomId = currentLocation.RoomId;
        CurrentZoneName = currentLocation.ZoneName;
    }
}
