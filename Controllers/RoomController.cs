using Microsoft.AspNetCore.Mvc;
using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Controllers;

[Route("/api/[controller]")]
public class RoomController : Controller
{
    private readonly ILogger<RoomController> _logger;

    public RoomController(ILogger<RoomController> logger)
    {
        _logger = logger;
    }

    [Route("{id:int}")]
    public IActionResult Room(int id)
    {
        var roomDescription = "You are in room " + id + ". It's very dark.";
        return View("RoomView", new RoomModel(id, "Sample", roomDescription));
    }
}
