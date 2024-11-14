using Newtonsoft.Json.Converters;

namespace ScaryCavesWeb.Models;

[GenerateSerializer, Alias("ScaryCavesWeb.Models.MobIdentifier")]
// ReSharper disable once ClassNeverInstantiated.Global
public class MobIdentifier(string instanceId, string definitionId)
{
    [Id(0)]
    public string InstanceId { get; } = instanceId;
    [Id(1)]
    public string DefinitionId { get; } = definitionId;

    private bool Equals(MobIdentifier other)
    {
        return InstanceId == other.InstanceId;
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
        return HashCode.Combine(InstanceId);
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
public class Mob : IMobile
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
    public string CurrentZoneName { get; set; }

    [Id(5)]
    public long CurrentRoomId { get; set; }

    [Id(6)]
    public string Name { get; set; }

    [Id(7)]
    public MobMovement Movement { get; set; }

    [Id(8)]
    public string Description { get; set; }

    [Newtonsoft.Json.JsonIgnore] public string Id => InstanceId;

    public Location GetCurrentLocation() => new(CurrentRoomId, CurrentZoneName);
    public void SetCurrentLocation(Location currentLocation)
    {
        CurrentRoomId = currentLocation.RoomId;
        CurrentZoneName = currentLocation.ZoneName;
    }
    /// <summary>
    /// An instance of a mob, walking around doing stuff.
    /// </summary>
    [Newtonsoft.Json.JsonConstructor]
    public Mob(string instanceId, string definitionId, string name, string description, int currentHitPoints, int currentArmorClass, string currentZoneName, long currentRoomId, MobMovement movement)
    {
        InstanceId = instanceId;
        DefinitionId = definitionId;
        Name = name;
        Description = description;
        CurrentHitPoints = currentHitPoints;
        CurrentArmorClass = currentArmorClass;
        CurrentZoneName = currentZoneName;
        CurrentRoomId = currentRoomId;
        Movement = movement;
    }

    public Mob(string instanceId, MobDefinition definition, Location currentLocation)
    {
        InstanceId = instanceId;
        DefinitionId = definition.DefinitionId;
        Name = definition.Name;
        Description = definition.Description;
        CurrentHitPoints = definition.HitPoints;
        CurrentArmorClass = definition.ArmorClass;
        CurrentZoneName = currentLocation.ZoneName;
        CurrentRoomId = currentLocation.RoomId;
        Movement = definition.Movement;
    }
}

[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.MobDefinition")]
public class MobDefinition
{
    [Newtonsoft.Json.JsonConstructor]
    public MobDefinition(string definitionId, string name, string description,
        double challenge, int hitPoints, int armorClass, int strength, int dexterity, int constitution, int intelligence, int wisdom, int charisma,
        List<AttackDefinition> attacks, MobMovement movement)
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
        Movement = movement;
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
    [Id(13)]
    public MobMovement Movement { get; }
}

[Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
public enum MovementType
{
    Stationary = 0,
    Wander = 1,
}

[GenerateSerializer, Alias("ScaryCavesWeb.Models.MobMovement")]
public class MobMovement
{
    public MobMovement(MovementType type, double chance)
    {
        Type = type;
        Chance = chance;
    }

    [Id(0)] public MovementType Type { get; }
    [Id(1)] public double Chance { get; }
}
