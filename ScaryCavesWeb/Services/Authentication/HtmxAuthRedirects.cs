using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;

namespace ScaryCavesWeb.Services.Authentication;

/// <summary>
/// Cookie-auth redirects that htmx understands. A plain 302 to the login page would be
/// followed by htmx's XHR and the whole login page swapped into the room panel; instead
/// an htmx request gets a 401 and an HX-Redirect, so the browser navigates to login.
/// (htmx doesn't send X-Requested-With, so cookie auth's own AJAX handling never applies.)
/// </summary>
public static class HtmxAuthRedirects
{
    public static Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Headers["HX-Request"] == "true")
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers["HX-Redirect"] = "/login";
            // htmx only needs the header; don't let status code pages render a body for the 401
            if (context.HttpContext.Features.Get<IStatusCodePagesFeature>() is { } statusCodePages)
            {
                statusCodePages.Enabled = false;
            }
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    }
}
