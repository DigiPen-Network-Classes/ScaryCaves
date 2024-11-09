using Newtonsoft.Json;

namespace ScaryCavesWeb.Models;

[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.MobIdentifier")]
public class MobIdentifier(string instanceId, string definitionId)
{
    [Id(0)]
    public string InstanceId { get; } = instanceId;
    [Id(1)]
    public string DefinitionId { get; } = definitionId;

    private bool Equals(MobIdentifier other)
    {
        return InstanceId == other.InstanceId && DefinitionId == other.DefinitionId;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((MobIdentifier)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(InstanceId, DefinitionId);
    }

    public static bool operator ==(MobIdentifier? left, MobIdentifier? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(MobIdentifier? left, MobIdentifier? right)
    {
        return !Equals(left, right);
    }

}

/// <summary>
/// An instance of a mob, walking around doing stuff.
/// </summary>
[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.Mob")]
public class Mob
{
    [Id(0)]
    public string InstanceId { get; }

    [Id(1)]
    public string DefinitionId { get; }

    [Id(2)]
    public int CurrentHitPoints { get; }

    [Id(3)]
    public int CurrentArmorClass { get; }

    [Id(4)]
    public string CurrentZone { get; set; }

    [Id(5)]
    public long CurrentRoomId { get; set; }

    /// <summary>
    /// An instance of a mob, walking around doing stuff.
    /// </summary>
    [JsonConstructor]
    public Mob(string instanceId, string definitionId, int currentHitPoints, int currentArmorClass, string currentZone, long currentRoomId)
    {
        InstanceId = instanceId;
        DefinitionId = definitionId;
        CurrentHitPoints = currentHitPoints;
        CurrentArmorClass = currentArmorClass;
        CurrentZone = currentZone;
        CurrentRoomId = currentRoomId;
    }

    public Mob(string instanceId, MobDefinition definition, Location currentLocation)
    {
        InstanceId = instanceId;
        DefinitionId = definition.DefinitionId;
        CurrentHitPoints = definition.HitPoints;
        CurrentArmorClass = definition.ArmorClass;
        CurrentZone = currentLocation.ZoneName;
        CurrentRoomId = currentLocation.RoomId;
    }
}

[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.MobDefinition")]
public class MobDefinition
{
    [JsonConstructor]
    public MobDefinition(string definitionId, string name, string description,
        double challenge, int hitPoints, int armorClass, int strength, int dexterity, int constitution, int intelligence, int wisdom, int charisma, List<AttackDefinition> attacks)
    {
        DefinitionId = definitionId;
        Name = name;
        Description = description;
        Challenge = challenge;
        HitPoints = hitPoints;
        ArmorClass = armorClass;
        Strength = strength;
        Dexterity = dexterity;
        Constitution = constitution;
        Intelligence = intelligence;
        Wisdom = wisdom;
        Charisma = charisma;
        Attacks = attacks;
    }

    [Id(0)]
    public string DefinitionId { get; }
    [Id(1)]
    public string Name { get; }
    [Id(2)]
    public string Description { get; }
    [Id(3)]
    public double Challenge { get; }
    [Id(4)]
    public int HitPoints { get; }
    [Id(5)]
    public int ArmorClass { get; }
    [Id(6)]
    public int Strength { get; }
    [Id(7)]
    public int Dexterity { get; }
    [Id(8)]
    public int Constitution { get; }
    [Id(9)]
    public int Intelligence { get; }
    [Id(10)]
    public int Wisdom { get; }
    [Id(11)]
    public int Charisma { get; }
    [Id(12)]
    public List<AttackDefinition> Attacks { get; }
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
