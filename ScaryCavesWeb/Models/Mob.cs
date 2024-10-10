using System.Text.Json;
using StackExchange.Redis;

namespace ScaryCavesWeb.Models;

/// <summary>
/// An instance of a mob, walking around doing stuff.
/// </summary>
public class Mob
{
    public Guid Id { get; }
    public string MobDefinitionId { get; }
    public int CurrentHitPoints { get; }
    public int CurrentArmorClass { get; }
    public MobDefinition Definition { get; }

    public Mob(MobDefinition definition)
    {
        Id = Guid.NewGuid();
        MobDefinitionId = definition.Id;
        CurrentHitPoints = definition.HitPoints;
        CurrentArmorClass = definition.ArmorClass;
        Definition = definition;
    }

    public Mob(Guid id, string mobDefinitionId, int currentHp, int currentAc, MobDefinition definition)
    {
        Id = id;
        MobDefinitionId = mobDefinitionId;
        Definition = definition;
        CurrentHitPoints = currentHp;
        CurrentArmorClass = currentAc;
    }

    public string MobKey => GetMobKey(Id.ToString());

    public static string GetMobKey(RedisValue id)
    {
        return $"mob:{id}";
    }

    public async Task Update(IDatabase database)
    {
        var instanceJson = JsonSerializer.Serialize(this);
        await database.StringSetAsync(MobKey, instanceJson);
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
