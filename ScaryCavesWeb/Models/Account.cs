namespace ScaryCavesWeb.Models;

/// <summary>
/// An account handles authentication
/// </summary>
[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.Account")]
public class Account(Guid id, string playerName)
{
    [Id(0)]
    public Guid Id { get; set; } = id;

    [Id(1)]
    public string PlayerName { get; set; } = playerName;

    /// <summary>
    /// The hashed password value
    /// </summary>
    [Id(2)]
    public string HashedPassword { get; set; } = "";
}
