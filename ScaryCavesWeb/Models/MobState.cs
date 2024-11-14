
using Newtonsoft.Json;

namespace ScaryCavesWeb.Models;

[GenerateSerializer]
[Alias("ScaryCavesWeb.Models.MobState")]
public class MobState
{
    [Id(0)]
    public string InstanceId { get; }
    [Id(1)]
    public string DefinitionId { get; }
    [Id(2)]
    public string Name { get; }
    [Id(3)]
    public string Description { get; }

    [JsonConstructor]
    public MobState(string instanceId, string definitionId, string name, string description)
    {
        InstanceId = instanceId;
        DefinitionId = definitionId;
        Name = name;
        Description = description;
    }

    public MobState(Mob mob)
    {
        InstanceId = mob.InstanceId;
        DefinitionId = mob.DefinitionId;
        Name = mob.Name;
        Description = mob.Description;
    }

    public MobState(MobIdentifier identifier, MobDefinition definition)
    {
        InstanceId = identifier.InstanceId;
        DefinitionId = identifier.DefinitionId;
        Name = definition.Name;
        Description = definition.Description;
    }

    protected bool Equals(MobState other)
    {
        return InstanceId == other.InstanceId;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((MobState)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(InstanceId);
    }

    public static bool operator ==(MobState? left, MobState? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(MobState? left, MobState? right)
    {
        return !Equals(left, right);
    }
}
