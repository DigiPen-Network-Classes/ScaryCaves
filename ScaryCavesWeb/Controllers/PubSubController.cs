using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services;

namespace ScaryCavesWeb.Controllers;

[Authorize]
public class PubSubController : ScaryController
{
    public PubSubController(ILogger<ScaryController> logger, ScaryCaveSettings settings, PlayerDatabase playerDatabase, RoomDatabase roomDatabase) : base(logger, settings, playerDatabase, roomDatabase)
    {
    }

    [HttpGet]
    public IActionResult TestClient()
    {
        return View(new TestClientModel(PlayerName ?? throw new InvalidOperationException("no player name")));
    }
}
