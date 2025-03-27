using StackExchange.Redis;

namespace ScaryCavesWeb.Services.Chat;

public interface IChannelPartition
{
    Task<string> GetChannelActorId(string channelName);
    Task<RedisChannel> GetChannel(string channelName, Guid? accountId=null);

    Task<RedisChannel> GetChannelFromActorId(string actorId);
}


public class ChannelPartition : IChannelPartition
{
    public Task<string> GetChannelActorId(string channelName)
    {
        return Task.FromResult($"{channelName}_0");
    }

    public async Task<RedisChannel> GetChannel(string channelName, Guid? accountId=null)
    {
        var actorId = await GetChannelActorId(channelName);
        return await GetChannelFromActorId(actorId);
    }

    public Task<RedisChannel> GetChannelFromActorId(string actorId)
    {
        return Task.FromResult(new RedisChannel($"chat:{actorId}", RedisChannel.PatternMode.Literal));
    }
}
