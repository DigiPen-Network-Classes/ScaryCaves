using Microsoft.AspNetCore.Mvc;

namespace ScaryCavesWeb.Controllers;

public class RegisterController : Controller
{

    private readonly ILogger<RegisterController> _logger;

    public RegisterController(ILogger<RegisterController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Index(string playerName, string password)
    {
        _logger.LogDebug("register new player attempt for {PlayerName}", playerName);
        // todo authentication
        if (!string.IsNullOrEmpty(playerName))
        {
            return RedirectToAction("Room", "Room", new { id = 99 });
        }

        ViewBag.ErrorMessage = "Invalid registration attempt";
        return View();
    }
}
