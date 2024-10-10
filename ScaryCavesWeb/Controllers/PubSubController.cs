using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services;

namespace ScaryCavesWeb.Controllers;

[Authorize]
public class PubSubController : ScaryController
{
    public PubSubController(ILogger<ScaryController> logger, ScaryCaveSettings settings, PlayerDatabase playerDatabase, Rooms rooms) : base(logger, settings, playerDatabase, rooms)
    {
    }

    [HttpGet]
    public IActionResult TestClient()
    {
        return View(new TestClientModel(PlayerName ?? throw new InvalidOperationException("no player name")));
    }
}
