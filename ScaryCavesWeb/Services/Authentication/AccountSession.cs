using Microsoft.AspNetCore.Identity;
using ScaryCavesWeb.Actors;
using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Services.Authentication;

public interface IAccountSession
{
    Task<Account?> Login(string accountName, string password);
    Task<Account?> Register(string accountName, string password);
}

public class AccountSession(ILogger<AccountSession> logger, IClusterClient clusterClient, IPasswordHasher<Account> passwordHasher) : IAccountSession
{
    private ILogger<AccountSession> Logger { get; } = logger;
    private IPasswordHasher<Account> PasswordHasher { get; } = passwordHasher;
    private IClusterClient ClusterClient { get; } = clusterClient;

    public async Task<Account?> Login(string playerName, string password)
    {
        // find account id by username:
        var accountId = await ClusterClient.GetGrain<IPlayerActor>(playerName).GetAccountId();
        if (accountId == null)
        {
            Logger.LogWarning("Login attempt for {PlayerName}: No Account found", playerName);
            return null;
        }

        var account = await ClusterClient.GetGrain<IAccountActor>(accountId.Value).GetAccount();
        var result = PasswordHasher.VerifyHashedPassword(account, account.HashedPassword, password);
        if (result != PasswordVerificationResult.Success)
        {
            Logger.LogWarning("Login attempt for {PlayerName}: Password fail", playerName);
            return null;
        }
        Logger.LogDebug("Login Attempt for {PlayerName}: Success", playerName);
        return await ClusterClient.GetGrain<IAccountActor>(accountId.Value).Login();
    }

    public async Task<Account?> Register(string playerName, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(playerName);

        var exists = await ClusterClient.GetGrain<IPlayerActor>(playerName).Exists();
        if (exists)
        {
            Logger.LogWarning("Attempt to register existing player {PlayerName} (already exists)", playerName);
            return null;
        }

        var newAccount = new Account(Guid.NewGuid(), playerName);
        newAccount.HashedPassword = PasswordHasher.HashPassword(newAccount, password);
        return await ClusterClient.GetGrain<IAccountActor>(newAccount.Id).Register(newAccount);
    }
}
