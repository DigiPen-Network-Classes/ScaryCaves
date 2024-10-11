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

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string playerName, string password)
    {
        Logger.LogDebug("login attempt for {PlayerName}", playerName);
        var account = await AccountSession.Login(playerName, password);
        if (account == null)
        {
            ViewBag.ErrorMessage = "Invalid login attempt";
            return View();
        }

        // create the auth cookie
        await HttpContext.ScaryCaveSignIn(account, DateTime.UtcNow.Add(Settings.PlayerExpires));
        return RedirectToRoom();
    }

    public async Task<IActionResult> Logout()
    {
        var g = AccountId;
        if (g != null)
        {
            await GrainFactory.GetGrain<IAccountActor>(AccountId ?? Guid.Empty).Logout();
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index");
    }

    public IActionResult AccessDenied()
    {
        Logger.LogError("Access denied");
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string playerName, string password)
    {
        Logger.LogDebug("register new player: {PlayerName}", playerName);
        var account =  await AccountSession.Register(playerName, password);
        if (account == null)
        {
            ViewBag.ErrorMessage = "Cannot use that name.";
            return View();
        }
        await HttpContext.ScaryCaveSignIn(account, DateTime.UtcNow.Add(Settings.PlayerExpires));
        return RedirectToRoom();
    }

    [Authorize]
    public async Task<IActionResult> StartOver()
    {
        var success = await GrainFactory.GetGrain<IPlayerActor>(PlayerName).StartOver();
        return success ? RedirectToRoom() : RedirectToAction("Login");
    }

    public IActionResult Enter()
    {
        // let us begin!
        return RedirectToRoom();
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
