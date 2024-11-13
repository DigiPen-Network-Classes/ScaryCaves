namespace ScaryCavesWeb.Models;

[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.AttackDefinition")]
public class AttackDefinition(string name, string description, string damage, int attackBonus)
{
    [Id(0)]
    public string Name { get; } = name;
    [Id(1)]
    public string Description { get; } = description;
    [Id(2)]
    public string Damage { get; } = damage;
    [Id(3)]
    public int AttackBonus { get; } = attackBonus;
}
