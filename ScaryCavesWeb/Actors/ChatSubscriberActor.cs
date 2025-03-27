using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using ScaryCavesWeb.Hubs;
using ScaryCavesWeb.Services.Chat;
using StackExchange.Redis;

namespace ScaryCavesWeb.Actors;

[Alias("ScaryCavesWeb.Actors.IChatSubscriberActor")]
public interface IChatSubscriberActor : IGrainWithStringKey
{
    [Alias("Subscribe")]
    Task Subscribe();
    [Alias("Unsubscribe")]
    Task Unsubscribe();

    [Alias("Awake")]
    Task Awake();
}

public class ChatSubscriberActor(ILogger<ChatSubscriberActor> logger,
    IConnectionMultiplexer connectionMultiplexer,
    IHubContext<GameHub> hubContext,
    IChannelPartition channelPartition): Grain, IChatSubscriberActor
{
    private ILogger<ChatSubscriberActor> Logger { get; } = logger;
    private IConnectionMultiplexer Redis { get; } = connectionMultiplexer;
    private IHubContext<GameHub> HubContext { get; } = hubContext;
    private IChannelPartition ChannelPartition { get; } = channelPartition;

    private ChannelMessageQueue? Subscription { get; set; }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        await Subscribe();
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await Unsubscribe();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task Subscribe()
    {
        Logger.LogInformation("Actor {ActorId} preparing to subscribe", this.GetPrimaryKeyString());
        var channel = await ChannelPartition.GetChannelFromActorId(this.GetPrimaryKeyString());
        var subscriber = Redis.GetSubscriber();
        Subscription = await subscriber.SubscribeAsync(channel);
        Subscription.OnMessage(OnMessage);
        Logger.LogInformation("Actor {ActorId} subscribed to {Channel}", this.GetPrimaryKeyString(), channel);
    }

    private async Task OnMessage(ChannelMessage value)
    {
        Logger.LogInformation("Actor {ActorId} message received", this.GetPrimaryKeyString());
        var payload = JsonConvert.DeserializeObject<ChatMessage>(value.Message!);
        if (payload != null)
        {
            var formatted = $"{payload.PlayerName}: {payload.Message}";
            await HubContext.Clients.All.SendAsync("ReceiveMessage", formatted);
        }
    }

    public async Task Unsubscribe()
    {
        Logger.LogInformation("Actor {ActorId} preparing to unsubscribe", this.GetPrimaryKeyString());
        if (Subscription != null)
        {
            await Subscription.UnsubscribeAsync();
            Subscription = null;
        }
    }

    public Task Awake() => Task.CompletedTask;
}
