using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScaryCavesWeb.Actors;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services;
using ScaryCavesWeb.Services.Authentication;
using StackExchange.Redis;

namespace ScaryCavesWeb.Controllers;

public class HomeController(
    ILogger<HomeController> logger,
    ScaryCaveSettings settings,
    IClusterClient clusterClient,
    IAccountSession accountSession,
    IReCaptchaService captchaService) : Controller
{
    private IAccountSession AccountSession { get; } = accountSession;
    private ILogger<HomeController> Logger { get; } = logger;
    private ScaryCaveSettings Settings { get; } = settings;
    private IClusterClient ClusterClient { get; } = clusterClient;
    private IReCaptchaService CaptchaService { get; } = captchaService;

    private string? PlayerName =>  User.FindFirst(ClaimTypes.Name)?.Value;
    private Guid? AccountId => User.FindFirst(ClaimTypes.NameIdentifier) is { Value: { } id } ? Guid.Parse(id) : null;

    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (!ModelState.IsValid || string.IsNullOrEmpty(model.PlayerName) || string.IsNullOrEmpty(model.Password) || string.IsNullOrEmpty(model.Token))
        {
            return BadRequest();
        }

        var isValid = await CaptchaService.IsTokenValid(model.Token);
        if (!isValid)
        {
            return BadRequest();
        }

        Logger.LogDebug("login attempt for '{PlayerName}'", model.PlayerName);
        var account = await AccountSession.Login(model.PlayerName, model.Password);
        if (account == null)
        {
            return Unauthorized();
        }

        // create the auth cookie
        await HttpContext.ScaryCaveSignIn(account, DateTime.UtcNow.Add(Settings.AccountExpires));
        return Ok();
    }

    public async Task<IActionResult> Logout()
    {
        var g = AccountId ?? Guid.Empty;
        if (g == Guid.Empty)
        {
            Logger.LogWarning("Logout Request without account id");
        }
        else
        {
            Logger.LogInformation("Logout Request for '{PlayerName}'", PlayerName);
            await ClusterClient.GetGrain<IAccountActor>(g).Logout();
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
        if (!ModelState.IsValid || string.IsNullOrEmpty(model.PlayerName) || string.IsNullOrEmpty(model.Password) || string.IsNullOrEmpty(model.Token))
        {
            return BadRequest();
        }

        var isValid = await CaptchaService.IsTokenValid(model.Token);
        if (!isValid)
        {
            return BadRequest();
        }
        Logger.LogInformation("Register new player: '{PlayerName}'", model.PlayerName);
        var account =  await AccountSession.Register(model.PlayerName, model.Password);
        if (account == null)
        {
            return Unauthorized();
        }
        await HttpContext.ScaryCaveSignIn(account, DateTime.UtcNow.Add(Settings.AccountExpires));
        return Ok();
    }

    [Authorize]
    public async Task<IActionResult> StartOver()
    {
        await ClusterClient.GetGrain<IPlayerActor>(PlayerName).StartOver();
        return Ok();
    }

    public IActionResult Status()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Ok();
        }
        return Unauthorized();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return BadRequest(Activity.Current?.Id ?? HttpContext.TraceIdentifier);
    }

    [HttpGet]
    public async Task<IActionResult> Health()
    {
        var orleansHealth = false;
        try
        {
            // Test Orleans connection by pinging a test grain
            var testGrain = ClusterClient.GetGrain<ITestGrain>(0);
            var grainResponse = await testGrain.Ping();
            orleansHealth = grainResponse;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Health check failed!");
        }
        return Ok(new
        {
            redis = orleansHealth, // test grain tests redis too
            orleans = orleansHealth
        });
    }
}
