using System.Text.Json;
using ScaryCavesWeb.Models;
using StackExchange.Redis;

namespace ScaryCavesWeb.Services;

public class WorldDatabase
{
    private readonly ILogger<WorldDatabase> _logger;
    private readonly IDatabase _database;
    private readonly ScaryCaveSettings _settings;
    private readonly RoomDatabase _rooms;
    private readonly MobDatabase _mobs;

    public WorldDatabase(ILogger<WorldDatabase> logger, IConnectionMultiplexer redis, ScaryCaveSettings settings, RoomDatabase rooms, MobDatabase mobs)
    {
        _logger = logger;
        _settings = settings;
        _rooms = rooms;
        _mobs = mobs;
        _database = redis.GetDatabase();
    }


    public async Task Initialize()
    {
        // put all items and mobs in their starting places
        // reset doors and other world-state to its initial (zone reset) config
        foreach (var r in _rooms.AsQueryable())
        {
            await r.RemoveAllMobs(_database);
            foreach (var mobId in r.InitialMobs)
            {
                _logger.LogDebug("Initializing mob {MobId} in room {RoomId}", mobId, r.Id);

                var mobDefinition = _mobs[mobId];
                if (mobDefinition == null)
                {
                    _logger.LogError("Mob {MobId} (Room {RoomId}) not found in mob database", mobId, r.Id);
                }
                else
                {
                    var instance = new Mob(mobDefinition);
                    await instance.Update(_database);
                    // add the instance to the room
                    await r.AddMob(_database, instance);
                }
            }
        }
    }
}
