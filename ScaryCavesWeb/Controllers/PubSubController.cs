using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services;

namespace ScaryCavesWeb.Controllers;

[Authorize]
public class PubSubController : ScaryController
{
    public PubSubController(ILogger<PubSubController> logger,
        ScaryCaveSettings settings,
        IGrainFactory grainFactory) : base(logger, settings, grainFactory)
    {
    }

    [HttpGet]
    public IActionResult TestClient()
    {
        return View(new TestClientModel(PlayerName ?? throw new InvalidOperationException("no player name")));
    }
}
