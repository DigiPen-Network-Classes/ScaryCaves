using StackExchange.Redis;

namespace ScaryCavesWeb.Services.Databases;

public interface IMobInstanceDatabase
{
}

/// <summary>
/// Access to the instances of mobs in the game.
/// </summary>
public class MobInstanceDatabase(IConnectionMultiplexer redis, IMobDatabase mobDefinitions) : IMobInstanceDatabase
{
    private IMobDatabase MobDefinitions { get; } = mobDefinitions;
    private IConnectionMultiplexer Redis { get; } = redis;


}
