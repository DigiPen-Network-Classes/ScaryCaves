using Newtonsoft.Json;
using StackExchange.Redis;

namespace ScaryCavesWeb.Services.Chat;

public interface IChatPublisher
{
    Task<long> Publish(string channelName, ChatMessage message);

}

public class ChatPublisher(ILogger<ChatPublisher> logger, IChatChannelPartitionService channelPartitionService, IConnectionMultiplexer connectionMultiplexer): IChatPublisher
{
    private ILogger<ChatPublisher> Logger { get; } = logger;
    private IChatChannelPartitionService ChannelPartitionService { get; } = channelPartitionService;
    private IConnectionMultiplexer Redis { get; } = connectionMultiplexer;

    public async Task<long> Publish(string channelName, ChatMessage message)
    {
        var channel = await ChannelPartitionService.FindPartition(channelName, message.AccountId);
        await ChannelPartitionService.WakeActorForChannel(channel);

        var db = Redis.GetSubscriber();
        var payload = JsonConvert.SerializeObject(message);
        return await db.PublishAsync(channel, payload);
    }
}
