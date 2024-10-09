using Microsoft.AspNetCore.Mvc;

namespace ScaryCavesWeb.Controllers;

public class LoginController : Controller
{

    private readonly ILogger<LoginController> _logger;

    public LoginController(ILogger<LoginController> logger)
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
        _logger.LogDebug("login attempt for {PlayerName}", playerName);
        // todo authentication
        if (!string.IsNullOrEmpty(playerName))
        {
            return RedirectToAction("Room", "Room", new { id = 99 });
        }

        ViewBag.ErrorMessage = "Invalid login attempt";
        return View();

    }
}
