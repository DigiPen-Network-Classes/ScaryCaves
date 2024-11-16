using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services;

namespace ScaryCavesWeb.Actors;

[Alias("ScaryCavesWeb.Actors.IAccountActor")]
public interface IAccountActor : IGrainWithGuidKey
{
    [Alias("Register")]
    Task<Account?> Register(Account newAccount);

    [Alias("Login")]
    Task<Account?> Login();

    [Alias("Logout")]
    Task Logout();

    [Alias("GetAccount")]
    Task<Account> GetAccount();
}

public class AccountActor(
    ILogger<AccountActor> logger,
    [PersistentState(nameof(Account), ScaryCaveSettings.AccountStorageProvider)] IPersistentState<Account> accountState) : Grain, IAccountActor
{
    private ILogger<AccountActor> Logger { get; } = logger;
    private IPersistentState<Account> AccountState { get; } = accountState;
    private Account Account => AccountState.State;

    public async Task<Account?> Register(Account newAccount)
    {
        if (AccountState.RecordExists)
        {
            Logger.LogWarning("Registering account {AccountId} - {NewAccountName} that already exists", this.GetPrimaryKey(), newAccount.PlayerName);
            // do it anyway...
        }
        else
        {
            Logger.LogInformation("Welcome Account {NewAccountName}", newAccount.PlayerName);
        }

        AccountState.State = newAccount;
        await AccountState.WriteStateAsync();
        await GrainFactory.GetGrain<IPlayerActor>(Account.PlayerName).Create(Account.Id);
        return Account;
    }

    public Task<Account?> Login()
    {
        if (!AccountState.RecordExists)
        {
            Logger.LogError("Account {AccountId} does not have any state, account not found", this.GetPrimaryKey());
            return Task.FromResult<Account?>(null);
        }

        Logger.LogInformation("Welcome Login Account {AccountId} - {AccountName}", this.GetPrimaryKey(), Account.PlayerName);
        return Task.FromResult<Account?>(Account);
    }
    public async Task Logout()
    {
        await GrainFactory.GetGrain<IPlayerActor>(Account.PlayerName).EndSession();
    }

    public Task<Account> GetAccount()
    {
        return Task.FromResult(Account);
    }
}
