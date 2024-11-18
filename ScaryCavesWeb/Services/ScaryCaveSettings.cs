
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
    /// This is how long mobs will wait between actions
    /// </summary>
    public int MobActivityTimerSeconds { get; set; } = 15;

    public TimeSpan MobActivityTimeSpan => TimeSpan.FromSeconds(MobActivityTimerSeconds);

    /// <summary>
    /// How long do account records (and therefore, Player records) stay around?
    /// After this point they are deleted
    /// </summary>
    public TimeSpan AccountExpires => TimeSpan.FromSeconds(AccountTimeToLiveSeconds ?? DefaultTimeToLiveSeconds);

    public string ReCaptchaSecretKeyFile { get; set; } = "";
    public string ReCaptchaSecretKey { get; set; } = "";
    public float ReCaptchaScoreThreshold { get; set; } = 0.5f;

    public string DataProtectionKeyPath { get; set; } = "./certs";
    public string DataProtectionCertFile { get; set; } = "./certs/dp-cert.pfx";
    public string DataProtectionCertPasswordFile { get; set; } = "";
    public string DataProtectionCertPassword { get; set; } = "";

    public string ReadReCaptchaSecretKey()
    {
        if (!string.IsNullOrEmpty(ReCaptchaSecretKey))
        {
            Console.WriteLine("Using ReCaptchaSecretKey from settings (length: {0})", ReCaptchaSecretKey.Length);
            return ReCaptchaSecretKey;
        }

        if (!File.Exists(ReCaptchaSecretKeyFile))
        {
            throw new FileNotFoundException("ReCaptchaSecretKeyFile not found", ReCaptchaSecretKeyFile);
        }

        Console.WriteLine($"Reading Recaptcha secret from {ReCaptchaSecretKeyFile}");
        return File.ReadAllText(ReCaptchaSecretKeyFile);
    }

    public string ReadDataProtectionCertPassword()
    {
        if (!string.IsNullOrEmpty(DataProtectionCertPassword))
        {
            Console.WriteLine("*** Using DataProtectionCertPassword from settings (length: {0})", DataProtectionCertPassword.Length);
            return DataProtectionCertPassword;
        }
        if (!File.Exists(DataProtectionCertPasswordFile))
        {
            throw new FileNotFoundException("DataProtectionCertPasswordFile not found", DataProtectionCertPasswordFile);
        }
        Console.WriteLine($"Reading cert password from {DataProtectionCertPasswordFile}");
        return File.ReadAllText(DataProtectionCertPasswordFile);
    }
}
