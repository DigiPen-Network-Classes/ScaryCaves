using Microsoft.AspNetCore.Mvc;
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
}
