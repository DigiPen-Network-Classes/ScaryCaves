using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ScaryCavesWeb.Actors;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services;

namespace ScaryCavesWeb.Controllers;

public abstract class ScaryController(ILogger<ScaryController> logger, ScaryCaveSettings settings, IGrainFactory grainFactory) : Controller
{
    protected ILogger<ScaryController> Logger { get; } = logger;
    protected ScaryCaveSettings Settings { get; } = settings;
    protected IGrainFactory GrainFactory { get; } = grainFactory;

    protected string? PlayerName =>  User.FindFirst(ClaimTypes.Name)?.Value;

    protected Guid? AccountId => User.FindFirst(ClaimTypes.NameIdentifier) is { Value: string id } ? Guid.Parse(id) : null;
    protected RedirectResult RedirectToRoom()
    {
        return Redirect("http://localhost:3000/");
    }

    protected async Task<Player?> GetPlayer()
    {
        if (string.IsNullOrEmpty(PlayerName))
        {
            return null;
        }
        var player = await GrainFactory.GetGrain<IPlayerActor>(PlayerName).Get();
        if (string.IsNullOrEmpty(player.Name) || player.OwnerAccountId == Guid.Empty || player.OwnerAccountId != AccountId)
        {
            return null;
        }

        return player;
    }
}
