using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Services.Databases;

public interface IMobDatabase
{
    MobDefinition? this[string mobId] { get; }
}

/// <summary>
/// Mob definitions (static)
/// </summary>
public class MobDatabase : IMobDatabase
{
    private FrozenDictionary<string, MobDefinition> MobsById { get; set; }

    public MobDefinition? this[string mobId] => MobsById.GetValueOrDefault(mobId);

    private MobDatabase(List<MobDefinition> mobList)
    {
        MobsById = mobList.ToFrozenDictionary(r => r.Id);
    }

    public static MobDatabase Build()
    {
        return new MobDatabase([]);
        /*
        // get mobs.json from the embedded resources
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("ScaryCavesWeb.Data.mobs.json");
        using var reader = new StreamReader(stream!);
        var json = reader.ReadToEnd();
        var mobs = JsonSerializer.Deserialize<List<MobDefinition>>(json);
        return new MobDatabase(mobs!);
        */
    }
}
