
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services.Authentication;
using ScaryCavesWeb.Services.Databases;

namespace ScaryCavesWeb.Services;

public static class ServiceDependency
{
    public static IServiceCollection AddScaryCaveWeb(this IServiceCollection services)
    {
        services.AddSingleton(RandomNumberGenerator.Create());
        services.AddSingleton<IRandomService, RandomService>();

        // authentication
        services.AddSingleton<IAccountSession, AccountSession>();
        services.AddSingleton<IPasswordHasher<Account>, PasswordHasher<Account>>();

        // databases
        services.AddSingleton<IZoneDatabase, ZoneDatabase>(_ => ZoneDatabase.Build());
        services.AddSingleton<IWorldDatabase, WorldDatabase>();

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
