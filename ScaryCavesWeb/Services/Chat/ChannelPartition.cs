using ScaryCavesWeb.Actors;
using StackExchange.Redis;

namespace ScaryCavesWeb.Services.Chat;

public interface IChatChannelPartitionService
{
    /// <summary>
    /// Build a RedisChannel for the channel, determining which partition id to use.
    /// </summary>
    /// <param name="channelName">friendly, non-partitioned, channel name like "global"</param>
    /// <param name="accountId">Optional. Who's asking? (Might use features like language, location, friends...)</param>
    /// <returns>RedisChannel ex. "chat:actor:global_0"</returns>
    Task<RedisChannel> FindPartition(string channelName, Guid? accountId=null);

    /// <summary>
    /// Given an actor id, build the RedisChannel it is subscribing to
    /// </summary>
    /// <param name="actorId">ex. global_0</param>
    /// <returns>ex. "chat:actor:global_0"</returns>
    Task<RedisChannel> BuildActorChannel(string actorId);

    Task WakeActorForChannel(RedisChannel channel);
}

public class ChatChannelPartitionService(IClusterClient clusterClient) : IChatChannelPartitionService
{

    private IClusterClient ClusterClient { get; } = clusterClient;
    public async Task<RedisChannel> FindPartition(string channelName, Guid? accountId=null)
    {
        var actorId = $"{channelName}_0";
        return await BuildActorChannel(actorId);
    }

    public Task<RedisChannel> BuildActorChannel(string actorId) => Task.FromResult(new RedisChannel($"chat:actor:{actorId}", RedisChannel.PatternMode.Literal));

    private static string GetActorIdFromChannel(string channel)
    {
        return channel.Split(":").Last();
    }

    public async Task WakeActorForChannel(RedisChannel channel)
    {
        var actorId = GetActorIdFromChannel(channel.ToString());
        await ClusterClient.GetGrain<IChatSubscriberActor>(actorId).Awake();
    }
}
