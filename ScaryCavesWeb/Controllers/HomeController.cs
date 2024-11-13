using System.Diagnostics;
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
/// TODO: replace auth with oauth/google
/// </summary>
public class HomeController(
    ILogger<HomeController> logger,
    ScaryCaveSettings settings,
    IGrainFactory grainFactory,
    IAccountSession accountSession) : ScaryController(logger, settings, grainFactory)
{
    private IAccountSession AccountSession { get; } = accountSession;

    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }
        if (string.IsNullOrEmpty(model.PlayerName) || string.IsNullOrEmpty(model.Password))
        {
            return BadRequest();
        }
        Logger.LogDebug("login attempt for {PlayerName}", model.PlayerName);
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
            await GrainFactory.GetGrain<IAccountActor>(AccountId ?? Guid.Empty).Logout();
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
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }
        if (string.IsNullOrEmpty(model.PlayerName) || string.IsNullOrEmpty(model.Password))
        {
            return BadRequest();
        }
        Logger.LogDebug("register new player: {PlayerName}", model.PlayerName);
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
        await GrainFactory.GetGrain<IPlayerActor>(PlayerName).StartOver();
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
