using System.Text.Json.Serialization;

namespace ScaryCavesWeb.Models;

/// <summary>
/// An instance of a mob, walking around doing stuff.
/// </summary>
[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.Mob")]
public class Mob
{
    [Id(0)]
    public string Id { get; }
    [Id(1)]
    public string Name { get; }
    [Id(2)]
    public string Description { get; }
    [Id(3)]
    public string MobDefinitionId { get; }
    [Id(4)]
    public int CurrentHitPoints { get; }
    [Id(5)]
    public int CurrentArmorClass { get; }

    [JsonConstructor]
    public Mob(string id, string mobDefinitionId, string name, int currentHitPoints, int currentArmorClass, string description)
    {
       Id = id;
       Name = name;
       MobDefinitionId = mobDefinitionId;
       CurrentHitPoints = currentHitPoints;
       CurrentArmorClass = currentArmorClass;
       Description = description;

    }
    public Mob(string id, MobDefinition definition) : this(id, definition.Name, definition.Id, definition.HitPoints, definition.ArmorClass, definition.Description)
    {
    }
}

[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.MobDefinition")]
public class MobDefinition(string id, string name, string description, double challenge, int hitPoints, int armorClass, int strength, int dexterity, int constitution, int intelligence, int wisdom, int charisma, List<AttackDefinition> attacks)
{
    [Id(0)]
    public string Id { get; } = id;
    [Id(1)]
    public string Name { get; } = name;
    [Id(2)]
    public string Description { get; } = description;
    [Id(3)]
    public double Challenge { get; } = challenge;
    [Id(4)]
    public int HitPoints { get; } = hitPoints;
    [Id(5)]
    public int ArmorClass { get; } = armorClass;
    [Id(6)]
    public int Strength { get; } = strength;
    [Id(7)]
    public int Dexterity { get; } = dexterity;
    [Id(8)]
    public int Constitution { get; } = constitution;
    [Id(9)]
    public int Intelligence { get; } = intelligence;
    [Id(10)]
    public int Wisdom { get; } = wisdom;
    [Id(11)]
    public int Charisma { get; } = charisma;
    [Id(12)]
    public List<AttackDefinition> Attacks { get; } = attacks;
}

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
