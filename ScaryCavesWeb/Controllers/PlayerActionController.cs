using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services;

namespace ScaryCavesWeb.Controllers;

[Authorize]
public class PlayerActionController : ScaryController
{
    public PlayerActionController(ILogger<ScaryController> logger, ScaryCaveSettings settings, PlayerDatabase playerDatabase, RoomDatabase roomDatabase) : base(logger, settings, playerDatabase, roomDatabase)
    {
    }

    [HttpPost]
    public async Task<IActionResult> GoTo([Required] Direction? direction)
    {
        Logger.LogInformation("Player {PlayerName} is attempting to go to direction {Direction}", User.Identity?.Name, direction);
        var playerName = User.Identity?.Name;
        if (string.IsNullOrEmpty(playerName) || direction == null)
        {
            return RedirectToAction("Index", "Home");
        }
        var player = await PlayerDatabase.Get(playerName);
        if (player == null)
        {
            // TODO expiration case etc..
            return RedirectToAction("Index", "Home");
        }

        // TODO mob instances
        PlayerRoom playerRoom = new PlayerRoom(player, RoomDatabaseDatabase[player.CurrentRoomId], []);
        if (playerRoom.Go(direction.Value))
        {
            await PlayerDatabase.Set(player);
        }
        // redirect to show the new state
        return RedirectToAction("Room", "Home");
    }
}
