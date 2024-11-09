using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScaryCavesWeb.Actors;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services;

namespace ScaryCavesWeb.Controllers;

[Authorize]
public class PlayerActionController(
    ILogger<PlayerActionController> logger,
    ScaryCaveSettings settings,
    IGrainFactory grainFactory) : ScaryController(logger, settings, grainFactory)
{

    [HttpGet]
    public async Task<IActionResult> Room()
    {
        var player = await GetPlayer();
        if (player == null)
        {
            return RedirectToAction("Register", "Home");
        }

        var currentLocation = player.GetCurrentLocation();
        var room = await GrainFactory.GetGrain<IRoomActor>(currentLocation.RoomId, currentLocation.ZoneName).GetRoom();
        return View("RoomView", new RoomState(player, room, []));
    }

    [HttpPost]
    public async Task<IActionResult> MoveTo([Required] Direction? direction)
    {
        Logger.LogInformation("Player {PlayerName} is attempting to go {Direction}", PlayerName, direction);
        if (direction == null)
        {
            Logger.LogError("Player {PlayerName} MoveTo called with null direction", PlayerName);
            return RedirectToAction("Index", "Home");
        }
        // succeed or fail, we want to redirect to the room page
        var success = await GrainFactory.GetGrain<IPlayerActor>(PlayerName).MoveTo(direction.Value);
        if (!success)
        {
            ViewBag.ErrorMesage = "You can't go that way";
        }
        return RedirectToAction("Room");
    }

    [HttpPost]
    public IActionResult Attack([Required] string? mobId)
    {
        Logger.LogInformation("Player {PlayerName} is attempting to attack mob {MobId}", PlayerName, mobId);
        /*
        if (string.IsNullOrEmpty(PlayerName) || mobId == null)
        {
            return RedirectToAction("Index", "Home");
        }
        var player = await PlayerDatabase.Get(PlayerName);
        if (player == null)
        {
            // TODO expiration case etc..
            return RedirectToAction("Index", "Home");
        }

        var room = RoomDatabase[player.CurrentRoomId];
        var mobs = await MobInstanceDatabase.GetMobsForRoom(room);
        //var target = mobs.Where(m => m.Id == mobId);
        var playerRoom = new PlayerRoom(player, room, mobs);
        if (playerRoom.Attack(mobId))
        {
            // write the results of the attack to the database?
            // no, that doesn't seem right ....
            // this should not DO the attack, but determine if it is valid and queue it up
            //await MobInstanceDatabase.Update(mob);
            //await PlayerDatabase.Update(player);
        }
        */
        // redirect to show the new state
        return RedirectToAction("Room");
    }
}
