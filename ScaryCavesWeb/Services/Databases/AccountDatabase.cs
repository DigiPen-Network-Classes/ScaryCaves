using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using ScaryCavesWeb.Models;
using StackExchange.Redis;

namespace ScaryCavesWeb.Services.Databases;

public interface IAccountDatabase
{
    public Task<Guid?> GetAccountId(string name);
    public Task<bool> SetAccountId(string name, Guid accountId, When when = When.Always);
}

public class AccountDatabase(IConnectionMultiplexer redis, ScaryCaveSettings settings, IPasswordHasher<Account> passwordHasher) : IAccountDatabase
{
    private IDatabase Database { get; } = redis.GetDatabase();
    private ScaryCaveSettings Settings { get; } = settings;
    private IPasswordHasher<Account> PasswordHasher { get; } = passwordHasher;

    private const string AccountByNameKeyPrefix = "accounts/name";

    /*
    public async Task<Account?> Create(string accountName, string password)
    {
        var account = new Account(accountName);
        account.Password = PasswordHasher.HashPassword(account, password);
        var success = await Set(account, When.NotExists);
        return success ? account : null;
    }*/

    public async Task<Guid?> GetAccountId(string name)
    {
        var json = await Database.StringGetAsync($"{AccountByNameKeyPrefix}/{name}");
        return !json.HasValue ? null : JsonSerializer.Deserialize<Guid>(json.ToString());
    }

    public async Task<bool> SetAccountId(string name, Guid accountId, When when = When.Always)
    {
        return await Database.StringSetAsync($"{AccountByNameKeyPrefix}/{name}", JsonSerializer.Serialize(accountId), Settings.AccountExpires, when);
    }
}
