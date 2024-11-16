using ScaryCavesWeb.Actors.Extensions;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services;

namespace ScaryCavesWeb.Actors;

[Alias("ScaryCavesWeb.Actors.IMobActor")]
public interface IMobActor : IGrainWithStringKey
{
    [Alias("Reload")]
    Task Reload(string zoneName, long roomId, MobDefinition definition);

    /// <summary>
    /// Hey, wake up!
    ///
    /// Doesn't reset the state or position or anything though.
    /// </summary>
    /// <returns></returns>
    [Alias("Wake")]
    Task Wake();
}

public class MobActor(ILogger<MobActor> logger,
    [PersistentState(nameof(Mob))] IPersistentState<Mob> mobState,
    IRandomService randomService) : Grain, IMobActor
{
    private ILogger<MobActor> Logger { get; } = logger;
    private IPersistentState<Mob> MobState { get; } = mobState;
    private IRandomService Random { get; } = randomService;
    private IDisposable? MobTick { get; set; }
    // set low for testing:
    private const int MobTickSeconds = 10;

    private Mob Mob => MobState.State;

    public async Task Reload(string zoneName, long roomId, MobDefinition mobDefinition)
    {
        var location = new Location(roomId, zoneName);
        Logger.LogInformation("Reloading {MobName} into {Location}", mobDefinition.Name, location);
        MobState.State = new Mob(this.GetPrimaryKeyString(), mobDefinition, location);
        await MobState.WriteStateAsync();
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        MobTick = this.RegisterGrainTimer<object?>(OnTick, null, TimeSpan.FromSeconds(MobTickSeconds), TimeSpan.FromSeconds(MobTickSeconds));
        return base.OnActivateAsync(cancellationToken);
    }

    private async Task OnTick(object? _)
    {
        Logger.LogTrace("Mob {InstanceId} ticking", this.GetPrimaryKeyString());
        if (!MobState.RecordExists)
        {
            Logger.LogWarning("Mob {InstanceId} has no state - that's not good ... ", this.GetPrimaryKeyString());
            return;
        }
        if (Mob.Movement.Type == MovementType.Wander)
        {
            await Wander();
        }
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Mob {InstanceId} deactivating: {Reason}", this.GetPrimaryKeyString(), reason);
        MobTick?.Dispose();
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public Task Wake()
    {
        if (!MobState.RecordExists)
        {
            Logger.LogWarning("Woke up {InstanceId} but it doesn't have state - that's not good ... ", this.GetPrimaryKeyString());
        }

        return Task.CompletedTask;
    }

    private async Task Wander()
    {
        var chance = Random.Next();
        if (!(chance < Mob.Movement.Chance))
        {
            Logger.LogDebug("Mob {InstanceId} could wander, but isn't going to this time.", Mob.InstanceId);
            return;
        }

        // what room are we in?
        var direction = await GrainFactory.GetRoomActor(Mob.GetCurrentLocation()).PickMoveDirection(Mob.InstanceId);
        if (direction == null)
        {
            Logger.LogWarning("Mob instance is stuck in Location {Location} - no exits!", Mob.GetCurrentLocation());
            return;
        }

        await MoveTo(direction.Value);
    }

    private async Task MoveTo(Direction direction)
    {
        Logger.LogDebug("{InstanceId} thinks about moving {Direction}", Mob.InstanceId, direction);
        var destination = await GrainFactory.GetRoomActor(Mob.GetCurrentLocation()).Move(Mob, direction);
        if (destination == null)
        {
            // can't go that way!
            Logger.LogWarning("Mob {InstanceId} tried to go {Direction} but was not allowed to.", Mob.InstanceId, direction);
            return;
        }

        Mob.SetCurrentLocation(destination.Location);
        await MobState.WriteStateAsync();
        Logger.LogInformation("Mob {InstanceId} is now at {Location}", Mob.InstanceId, Mob.GetCurrentLocation());
    }
}
