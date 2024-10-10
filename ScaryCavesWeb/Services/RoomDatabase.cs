using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Services;

/// <summary>
/// The arrangements of "rooms" in the game.  This is a singleton class that is
/// in charge of the immutable definition of the rooms -- not the 'state' of the
/// room (who and what it is in it); just the initial configuration.
/// </summary>
public class RoomDatabase
{
    private FrozenDictionary<int, Room> RoomsById { get; set; }

    public Room this[int roomId] => RoomsById.TryGetValue(roomId, out var room) ? room : this[0];

    public IQueryable<Room> AsQueryable() => RoomsById.Values.AsQueryable();

    private RoomDatabase(List<Room> roomList)
    {
        RoomsById = roomList.ToFrozenDictionary(r => r.Id);
    }

    public static RoomDatabase Build()
    {
        // get rooms.json from the embedded resources
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("ScaryCavesWeb.Data.rooms.json");
        using var reader = new StreamReader(stream!);
        var json = reader.ReadToEnd();
        var rooms = JsonSerializer.Deserialize<List<Room>>(json);
        return new RoomDatabase(rooms!);
    }

}
