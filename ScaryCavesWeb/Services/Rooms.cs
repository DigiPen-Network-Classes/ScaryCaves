using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Services;

public class Rooms
{
    private FrozenDictionary<int, Room> RoomsById { get; set; }

    public Room this[int roomId] => RoomsById.TryGetValue(roomId, out var room) ? room : this[0];


    private Rooms(List<Room> roomList)
    {
        RoomsById = roomList.ToFrozenDictionary(r => r.Id);
    }

    public static Rooms Build()
    {
        // get rooms.json from the embedded resources
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("ScaryCavesWeb.Data.rooms.json");
        using var reader = new StreamReader(stream!);
        var json = reader.ReadToEnd();
        var rooms = JsonSerializer.Deserialize<List<Room>>(json);
        return new Rooms(rooms!);
    }

}
