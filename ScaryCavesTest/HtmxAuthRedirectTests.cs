using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using ScaryCavesWeb.Services.Authentication;

namespace ScaryCavesTest;

public class HtmxAuthRedirectTests
{
    private static RedirectContext<CookieAuthenticationOptions> Context(bool htmx)
    {
        var http = new DefaultHttpContext();
        if (htmx)
        {
            http.Request.Headers["HX-Request"] = "true";
        }
        var scheme = new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler));
        return new RedirectContext<CookieAuthenticationOptions>(http, scheme, new CookieAuthenticationOptions(),
            new AuthenticationProperties(), "/login?ReturnUrl=%2Fcave%2Fmove%2FNorth");
    }

    [Fact]
    public async Task HtmxRequest_Gets401AndHxRedirectToLogin()
    {
        var context = Context(htmx: true);

        await HtmxAuthRedirects.RedirectToLogin(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("/login", context.Response.Headers["HX-Redirect"]);
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }

    [Fact]
    public async Task HtmxRequest_SkipsStatusCodePages()
    {
        // otherwise the 401 is re-executed as the Not Found page (and comes back a 400)
        var context = Context(htmx: true);
        var statusCodePages = new StatusCodePagesFeature();
        context.HttpContext.Features.Set<IStatusCodePagesFeature>(statusCodePages);

        await HtmxAuthRedirects.RedirectToLogin(context);

        Assert.False(statusCodePages.Enabled);
    }

    [Fact]
    public async Task OrdinaryRequest_IsRedirectedAsUsual()
    {
        var context = Context(htmx: false);

        await HtmxAuthRedirects.RedirectToLogin(context);

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal("/login?ReturnUrl=%2Fcave%2Fmove%2FNorth", context.Response.Headers.Location);
        Assert.False(context.Response.Headers.ContainsKey("HX-Redirect"));
    }
}
