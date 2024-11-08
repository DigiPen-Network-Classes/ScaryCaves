using System.Text.Json;
using System.Text.Json.Serialization;
using StackExchange.Redis;

namespace ScaryCavesWeb.Models;

/// <summary>
/// An instance of a mob, walking around doing stuff.
/// </summary>
public class Mob
{
    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string MobDefinitionId { get; }
    public int CurrentHitPoints { get; }
    public int CurrentArmorClass { get; }

    [JsonConstructor]
    public Mob(Guid id, string mobDefinitionId, string name, int currentHitPoints, int currentArmorClass, string description)
    {
       Id = id;
       Name = name;
       MobDefinitionId = mobDefinitionId;
       CurrentHitPoints = currentHitPoints;
       CurrentArmorClass = currentArmorClass;
       Description = description;

    }
    public Mob(MobDefinition definition) : this(Guid.NewGuid(), definition.Name, definition.Id, definition.HitPoints, definition.ArmorClass, definition.Description)
    {
    }


}

public class MobDefinition(string id, string name, string description, double challenge, int hitPoints, int armorClass, int strength, int dexterity, int constitution, int intelligence, int wisdom, int charisma, List<AttackDefinition> attacks)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string Description { get; } = description;
    public double Challenge { get; } = challenge;
    public int HitPoints { get; } = hitPoints;
    public int ArmorClass { get; } = armorClass;
    public int Strength { get; } = strength;
    public int Dexterity { get; } = dexterity;
    public int Constitution { get; } = constitution;
    public int Intelligence { get; } = intelligence;
    public int Wisdom { get; } = wisdom;
    public int Charisma { get; } = charisma;
    public List<AttackDefinition> Attacks { get; } = attacks;
}

public class AttackDefinition(string name, string description, string damage, int attackBonus)
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public string Damage { get; } = damage;
    public int AttackBonus { get; } = attackBonus;
}
