namespace ScaryCavesWeb.Models;

/// <summary>
/// An account handles authentication
/// </summary>
[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.Account")]
public class Account
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string PlayerName { get; set; } = "";

    /// <summary>
    /// The hashed password value
    /// </summary>
    [Id(2)]
    public string HashedPassword { get; set; } = "";

    public Account()
    {
    }

    public Account(Guid id, string playerName)
    {
        PlayerName = playerName;
        Id = id;
    }
}
