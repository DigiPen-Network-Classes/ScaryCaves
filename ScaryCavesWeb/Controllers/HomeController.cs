using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScaryCavesWeb.Actors;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services;
using ScaryCavesWeb.Services.Authentication;

namespace ScaryCavesWeb.Controllers;

/// <summary>
/// TODO: add auth with oauth/google
/// TODO: add captcha to register / login
/// </summary>
public class HomeController(
    ILogger<HomeController> logger,
    ScaryCaveSettings settings,
    IClusterClient clusterClient,
    IAccountSession accountSession) : Controller
{
    private IAccountSession AccountSession { get; } = accountSession;
    private ILogger<HomeController> Logger { get; } = logger;
    private ScaryCaveSettings Settings { get; } = settings;
    private IClusterClient ClusterClient { get; } = clusterClient;

    private string? PlayerName =>  User.FindFirst(ClaimTypes.Name)?.Value;
    private Guid? AccountId => User.FindFirst(ClaimTypes.NameIdentifier) is { Value: { } id } ? Guid.Parse(id) : null;

    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (!ModelState.IsValid || string.IsNullOrEmpty(model.PlayerName) || string.IsNullOrEmpty(model.Password))
        {
            return BadRequest();
        }
        Logger.LogDebug("login attempt for '{PlayerName}'", model.PlayerName);
        var account = await AccountSession.Login(model.PlayerName, model.Password);
        if (account == null)
        {
            return Unauthorized();
        }

        // create the auth cookie
        await HttpContext.ScaryCaveSignIn(account, DateTime.UtcNow.Add(Settings.PlayerExpires));
        return Ok();
    }

    public async Task<IActionResult> Logout()
    {
        var g = AccountId;
        if (g != null)
        {
            await ClusterClient.GetGrain<IAccountActor>(AccountId ?? Guid.Empty).Logout();
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    public IActionResult AccessDenied()
    {
        Logger.LogError("Access denied");
        return Unauthorized();
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (!ModelState.IsValid || string.IsNullOrEmpty(model.PlayerName) || string.IsNullOrEmpty(model.Password))
        {
            return BadRequest();
        }
        Logger.LogDebug("Register new player: '{PlayerName}'", model.PlayerName);
        var account =  await AccountSession.Register(model.PlayerName, model.Password);
        if (account == null)
        {
            return BadRequest();
        }
        await HttpContext.ScaryCaveSignIn(account, DateTime.UtcNow.Add(Settings.PlayerExpires));
        return Ok();
    }

    [Authorize]
    public async Task<IActionResult> StartOver()
    {
        await ClusterClient.GetGrain<IPlayerActor>(PlayerName).StartOver();
        return Ok();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
