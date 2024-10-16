using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services;

namespace ScaryCavesWeb.Controllers;

public abstract class ScaryController : Controller
{
    protected ILogger<ScaryController> Logger { get; }
    protected PlayerDatabase PlayerDatabase { get; }
    protected ScaryCaveSettings Settings { get; }
    protected Rooms RoomsDatabase { get; }

    protected ScaryController(ILogger<ScaryController> logger, ScaryCaveSettings settings, PlayerDatabase playerDatabase, Rooms rooms)
    {
        Logger = logger;
        PlayerDatabase = playerDatabase;
        RoomsDatabase = rooms;
        Settings = settings;
    }

    protected string? PlayerName =>  User.FindFirst(ClaimTypes.Name)?.Value;

    protected async Task<Player?> GetAuthPlayer()
    {
        return await PlayerDatabase.Get(PlayerName ?? throw new Exception("no player name"));
    }
}
