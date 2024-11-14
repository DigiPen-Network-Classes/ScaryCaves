using ScaryCavesWeb.Actors.Extensions;
using ScaryCavesWeb.Models;
using ScaryCavesWeb.Services;

namespace ScaryCavesWeb.Actors;

[Alias("ScaryCavesWeb.Actors.IMobActor")]
public interface IMobActor : IGrainWithStringKey
{
    [Alias("Reload")]
    Task Reload(string zoneName, long roomId, MobDefinition definition);
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
        Logger.LogInformation("Mob {InstanceId} ticking", this.GetPrimaryKeyString());
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

    private async Task Wander()
    {
        var chance = Random.Next();
        if (!(chance < Mob.Movement.Chance))
        {
            Logger.LogInformation("Mob {InstanceId} staying put.", Mob.InstanceId);
            return;
        }

        // what room are we in?
        var direction = await GrainFactory.GetRoomActor(Mob.GetCurrentLocation()).PickMoveDirection(Mob.InstanceId);
        if (direction == null)
        {
            Logger.LogWarning("Mob instance is stuck in Location {Location} - no exits!", Mob.GetCurrentLocation());
            return;
        }

        var room = await MoveTo(direction.Value);
        Logger.LogInformation("Moved to {Location}", room?.Location);
    }

    private async Task<Room?> MoveTo(Direction direction)
    {
        Logger.LogInformation("Mob {InstanceId} wants to move {Direction}", Mob.InstanceId, direction);
        var destination = await GrainFactory.GetRoomActor(Mob.GetCurrentLocation()).Move(Mob, direction);
        if (destination == null)
        {
            // can't go that way!
            Logger.LogInformation("Mob {InstanceId} tried to go {Direction} but was not allowed to.", Mob.InstanceId, direction);
            return null;
        }

        Mob.SetCurrentLocation(destination.Location);
        await MobState.WriteStateAsync();
        Logger.LogInformation("Mob {InstanceId} is now at {Location}", Mob.InstanceId, Mob.GetCurrentLocation());
        return destination;
    }
}
