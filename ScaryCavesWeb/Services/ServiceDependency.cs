
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services.Authentication;
using ScaryCavesWeb.Services.Chat;
using ScaryCavesWeb.Services.Databases;
using StackExchange.Redis;

namespace ScaryCavesWeb.Services;

public static class ServiceDependency
{
    public static IDataProtectionBuilder AddScaryDataProtection(this IServiceCollection services, ScaryCaveSettings settings)
    {
        /*
        Console.WriteLine($"DataProtectionKeyPath: {settings.DataProtectionKeyPath}");
        Console.WriteLine($"DataProtectionCertFile: {settings.DataProtectionCertFile}");
        Console.WriteLine($"password from settings: {settings.DataProtectionCertPassword}");
        Console.WriteLine($"password from secret file: '{settings.ReadDataProtectionCertPassword()}'");
        */

        var dataProtectionPath = new DirectoryInfo(settings.DataProtectionKeyPath);
        if (!dataProtectionPath.Exists)
        {
            dataProtectionPath.Create();
        }
        if (!File.Exists(settings.DataProtectionCertFile))
        {
            throw new FileNotFoundException("DataProtectionCertFile not found", settings.DataProtectionCertFile);
        }

        var certificate = new X509Certificate2(settings.DataProtectionCertFile, settings.ReadDataProtectionCertPassword());

        return services.AddDataProtection()
            .PersistKeysToFileSystem(dataProtectionPath)
            .ProtectKeysWithCertificate(certificate);
    }

    public static IServiceCollection AddScaryCaveWeb(this IServiceCollection services)
    {
        // redis
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var settings = sp.GetRequiredService<ScaryCaveSettings>();
            var options = ConfigurationOptions.Parse(settings.ChatRedisConnectionString);
            return ConnectionMultiplexer.Connect(options);
        });
        // google recaptcha checker
        services.AddTransient<IReCaptchaService, ReCaptchaService>();

        services.AddSingleton(RandomNumberGenerator.Create());
        services.AddSingleton<IRandomService, RandomService>();

        // authentication
        services.AddSingleton<IAccountSession, AccountSession>();
        services.AddSingleton<IPasswordHasher<Account>, PasswordHasher<Account>>();

        // databases
        services.AddSingleton<IZoneDatabase, ZoneDatabase>(_ => ZoneDatabase.Build());
        services.AddSingleton<IWorldDatabase, WorldDatabase>();

        // chat
        services.AddSingleton<IChatChannelPartitionService, ChatChannelPartitionService>();
        services.AddSingleton<IChatPublisher, ChatPublisher>();

        return services;
    }

    public static async Task ScaryCaveSignIn(this HttpContext context, Account account, DateTime expiresUtc)
    {
        ArgumentException.ThrowIfNullOrEmpty(account.PlayerName);
        ArgumentOutOfRangeException.ThrowIfEqual(account.Id, Guid.Empty);

        List<Claim> claims =
        [
            new(ClaimTypes.Name, account.PlayerName),
            new(ClaimTypes.NameIdentifier, account.Id.ToString())
        ];
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // Sign in the player with the claims
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            new AuthenticationProperties
            {
                IsPersistent = true, // Keep the user logged in
                ExpiresUtc = expiresUtc,
            });
    }
}
