using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Actors;

public interface IMobActor : IGrainWithStringKey
{
    Task ReloadFrom(MobDefinition mob);
}


public class MobActor(ILogger<MobActor>  logger,
    [PersistentState(nameof(MobDefinition))] IPersistentState<MobDefinition> MobDefinitionState)  : Grain, IMobActor
{
    private ILogger<MobActor> Logger { get; } = logger;
    private IPersistentState<MobDefinition> MobDefinitionState { get; } = MobDefinitionState;
    private MobDefinition MobDefinition => MobDefinitionState.State;

    public async Task ReloadFrom(MobDefinition mob)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(mob.Id, this.GetPrimaryKeyString());
        MobDefinitionState.State = mob;
        await MobDefinitionState.WriteStateAsync();
    }

}
