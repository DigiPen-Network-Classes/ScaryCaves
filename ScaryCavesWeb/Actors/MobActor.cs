using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Actors;

[Alias("ScaryCavesWeb.Actors.IMobActor")]
public interface IMobActor : IGrainWithStringKey
{
    [Alias("Reload")]
    Task Reload(string zoneName, long roomId, MobDefinition definition);
}

public class MobActor(ILogger<MobActor> logger,
    [PersistentState(nameof(Mob))] IPersistentState<Mob> mobState) : Grain, IMobActor
{
    private ILogger<MobActor> Logger { get; } = logger;
    private IPersistentState<Mob> MobState { get; } = mobState;
    private Mob Mob => MobState.State;

    public async Task Reload(string zoneName, long roomId, MobDefinition mobDefinition)
    {
        var location = new Location(roomId, zoneName);
        Logger.LogInformation("Reloading {MobName} into {Location}", mobDefinition.Name, location);
        MobState.State = new Mob(this.GetPrimaryKeyString(), mobDefinition, location);
        await MobState.WriteStateAsync();
    }
}
