using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services;

namespace ScaryCavesWeb.Controllers;

public class HomeController : ScaryController
{
    public HomeController(ILogger<ScaryController> logger, ScaryCaveSettings settings, PlayerDatabase playerDatabase, Rooms rooms) : base(logger, settings, playerDatabase, rooms)
    {
    }

    private string? PlayerName =>  User.FindFirst(ClaimTypes.Name)?.Value;

    private async Task<Player?> GetAuthPlayer()
    {
        return await PlayerDatabase.Get(PlayerName ?? throw new Exception("no player name"));
    }

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
        var player = await PlayerDatabase.Authenticate(playerName, password);
        if (player == null)
        {
            ViewBag.ErrorMessage = "Invalid login attempt";
            return View();
        }

        await HttpContext.ScaryCaveSignIn(playerName, DateTime.UtcNow.Add(Settings.PlayerExpires));
        return this.RedirectTo(c => c.Enter());
    }

    public async Task<IActionResult> Logout()
    {
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

        var player = await PlayerDatabase.Create(playerName, password);
        if (player == null)
        {
            ViewBag.ErrorMessage = "Create player failed";
            return View();
        }
        await HttpContext.ScaryCaveSignIn(playerName, DateTime.UtcNow.Add(Settings.PlayerExpires));
        return RedirectToAction("Room");
    }

    [Authorize]
    public async Task<IActionResult> StartOver()
    {
        var player = await GetAuthPlayer();
        if (player == null)
        {
            return RedirectToAction("Login");
        }

        player.CurrentRoomId = 0;
        await PlayerDatabase.Set(player);
        return RedirectToAction("Enter");

    }

    [Authorize]
    public async Task<IActionResult> Enter()
    {
        return await Room();
    }

    [Authorize]
    public async Task<IActionResult> Room()
    {
        var player = await GetAuthPlayer();
        // TODO need a better fix for deleted / expired players
        // (login should reset TTL)
        if (player == null)
        {
            // TODO some better sort of 'expired' page
            return RedirectToAction("Login");
        }

        var room = RoomsDatabase[player.CurrentRoomId];
        return View("RoomView", new PlayerRoom(player, room));
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
