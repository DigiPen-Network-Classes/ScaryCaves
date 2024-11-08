using System.Text.Json;
using StackExchange.Redis;

namespace ScaryCavesWeb.Services.Databases;

public interface IAccountDatabase
{
    public Task<Guid?> GetAccountId(string name);
    public Task<bool> SetAccountId(string name, Guid accountId, When when = When.Always);
}

public class AccountDatabase(IConnectionMultiplexer redis, ScaryCaveSettings settings) : IAccountDatabase
{
    private IDatabase Database { get; } = redis.GetDatabase();
    private ScaryCaveSettings Settings { get; } = settings;

    private const string AccountByNameKeyPrefix = "accounts/name";

    public async Task<Guid?> GetAccountId(string name)
    {
        var json = await Database.StringGetAsync($"{AccountByNameKeyPrefix}/{name}");
        return !json.HasValue ? null : JsonSerializer.Deserialize<Guid>(json.ToString());
    }

    public async Task<bool> SetAccountId(string name, Guid accountId, When when = When.Always)
    {
        // for now, don't expire ---
        // TODO will have to come back to this before launching
        return await Database.StringSetAsync($"{AccountByNameKeyPrefix}/{name}", JsonSerializer.Serialize(accountId), null, when);
    }
}
