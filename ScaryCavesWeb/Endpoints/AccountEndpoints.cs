using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ScaryCavesWeb.Actors;

namespace ScaryCavesWeb.Endpoints;

/// <summary>
/// The navbar's Logout and Start Over buttons: form POSTs that redirect.
/// </summary>
public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/logout", async (HttpContext http, IAntiforgery antiforgery, IClusterClient clusterClient, ILoggerFactory loggers) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            var logger = loggers.CreateLogger("Account.Logout");
            if (http.User.FindFirst(ClaimTypes.NameIdentifier) is { Value: { } id } && Guid.TryParse(id, out var accountId))
            {
                logger.LogInformation("Logout Request for '{PlayerName}'", http.User.Identity?.Name);
                await clusterClient.GetGrain<IAccountActor>(accountId).Logout();
            }
            else
            {
                logger.LogWarning("Logout Request without account id");
            }
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/");
        }).DisableAntiforgery(); // validated explicitly above

        app.MapPost("/startover", async (HttpContext http, IAntiforgery antiforgery, IClusterClient clusterClient) =>
        {
            await antiforgery.ValidateRequestAsync(http);
            await clusterClient.GetGrain<IPlayerActor>(http.User.Identity!.Name!).StartOver();
            return Results.Redirect("/scarycave");
        }).RequireAuthorization().DisableAntiforgery(); // validated explicitly above

        return app;
    }
}
