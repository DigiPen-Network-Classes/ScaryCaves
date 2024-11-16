
namespace ScaryCavesWeb.Services;

public class ScaryCaveSettings
{
    public const string AccountStorageProvider = "accounts-player-storage";
    public const string PlayerStorageProvider = AccountStorageProvider;


    private const int DefaultTimeToLiveSeconds = 300;
    public int? AccountTimeToLiveSeconds { get; set; }

    public string DefaultZoneName { get; set; } = "scary-cave";
    public long DefaultRoomId { get; set; } = 0;

    public string RedisConnectionString { get; set; } = "";

    /// <summary>
    /// How long do account records (and therefore, Player records) stay around?
    /// After this point they are deleted
    /// </summary>
    public TimeSpan AccountExpires => TimeSpan.FromSeconds(AccountTimeToLiveSeconds ?? DefaultTimeToLiveSeconds);
}
