
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ScaryCavesWeb.Services;

public static class ServiceDependency
{
    public static IServiceCollection AddScaryCaveWeb(this IServiceCollection services)
    {
        services.AddScoped<PlayerDatabase>();
        services.AddSingleton<PasswordHasher>();
        services.AddSingleton<Rooms>(_ => Rooms.Build());
        return services;
    }

    public static async Task ScaryCaveSignIn(this HttpContext context, string playerName, DateTime expiresUtc)
    {
        List<Claim> claims = [new(ClaimTypes.Name, playerName)];
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
