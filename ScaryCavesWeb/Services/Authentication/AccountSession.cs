using Microsoft.AspNetCore.Identity;
using ScaryCavesWeb.Actors;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services.Databases;

namespace ScaryCavesWeb.Services.Authentication;

public interface IAccountSession
{
    Task<Account?> Login(string accountName, string password);
    Task<Account?> Register(string accountName, string password);
}

public class AccountSession(ILogger<AccountSession> logger, IClusterClient clusterClient, IAccountDatabase accountDatabase, IPasswordHasher<Account> passwordHasher) : IAccountSession
{
    private ILogger<AccountSession> Logger { get; } = logger;
    private IAccountDatabase AccountDatabase { get; } = accountDatabase;
    private IPasswordHasher<Account> PasswordHasher { get; } = passwordHasher;
    private IClusterClient ClusterClient { get; } = clusterClient;

    public async Task<Account?> Login(string accountName, string password)
    {
        // find account by username:
        var accountId = await AccountDatabase.GetAccountId(accountName);
        if (accountId == null)
        {
            return null;
        }

        var account = await ClusterClient.GetGrain<IAccountActor>(accountId.Value).GetAccount();
        var result = PasswordHasher.VerifyHashedPassword(account, account.HashedPassword, password);
        if (result != PasswordVerificationResult.Success)
        {
            return null;
        }
        return await ClusterClient.GetGrain<IAccountActor>(accountId.Value).Login();
    }

    public async Task<Account?> Register(string accountName, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);

        var existingAccountId = await AccountDatabase.GetAccountId(accountName);
        if (existingAccountId != null)
        {
            // already got one
            Logger.LogInformation("account {AccountName} already exists: {AccountId}", accountName, existingAccountId);
            return null;
        }
        var newAccount = new Account(Guid.NewGuid(), accountName);
        newAccount.HashedPassword = PasswordHasher.HashPassword(newAccount, password);
        var result = await ClusterClient.GetGrain<IAccountActor>(newAccount.Id).Register(newAccount);
        if (result != null)
        {
            return await AccountDatabase.SetAccountId(accountName, newAccount.Id) == false ? null : result;
        }
        return null;
    }
}
