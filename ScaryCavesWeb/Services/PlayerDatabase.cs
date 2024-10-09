using System.Text.Json;
using ScaryCavesWeb.Models;
using StackExchange.Redis;

namespace ScaryCavesWeb.Services;

public class PlayerDatabase
{
    private readonly IDatabase _database;
    private readonly ScaryCaveSettings _settings;
    private readonly PasswordHasher _passwordHasher;

    public PlayerDatabase(IConnectionMultiplexer redis, ScaryCaveSettings settings, PasswordHasher passwordHasher)
    {
        _settings = settings;
        _passwordHasher = passwordHasher;
        _database = redis.GetDatabase();
    }

    public async Task<Player?> Create(string playerName, string password)
    {
        playerName = playerName.ToLowerInvariant();
        string hashedPassword = _passwordHasher.HashPassword(password);
        var player = new Player
        {
            Name = playerName,
            Password = hashedPassword,
            CurrentRoomId = 0,
        };
        bool success = await Set(player, When.NotExists);
        return success ? player : null;
    }

    public async Task<Player?> Authenticate(string playerName, string password)
    {
        var player = await Get(playerName);
        if (player == null)
        {
            return null;
        }
        return _passwordHasher.VerifyPassword(player.Password, password) ? player : null;
    }

    public async Task<Player?> Get(string playerName)
    {
        var json = await _database.StringGetAsync($"player:{playerName.ToLowerInvariant()}");
        if (!json.HasValue)
        {
            return null;
        }
        return JsonSerializer.Deserialize<Player>(json.ToString());
    }

    public async Task<bool> Set(Player player, When when = When.Always)
    {
        return await _database.StringSetAsync($"player:{player.Name}", JsonSerializer.Serialize(player), _settings.PlayerExpires, when);
    }
}
