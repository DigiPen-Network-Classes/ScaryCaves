
namespace ScaryCavesWeb.Services;

public class ScaryCaveSettings
{
    private const int defaultSeconds = 300;
    public int? DefaultTimeToLive { get; set; }

    public string DefaultZoneName { get; set; } = "scary-cave";
    public long DefaultRoomId { get; set; }

    /// <summary>
    /// How long do player records stay around?
    /// After this point they are deleted, so auth should be tied to this too.
    /// </summary>
    public TimeSpan PlayerExpires => TimeSpan.FromSeconds(DefaultTimeToLive ?? defaultSeconds);
}
