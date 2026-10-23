using System.Threading.Channels;
using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Live;

/// <summary>
/// Who is listening in which room. This replaces SignalR groups: RoomActor publishes
/// room events here, and each player's SSE stream reads its own queue.
///
/// In-process only - like SignalR without a backplane, this assumes one web instance.
/// </summary>
public interface IRoomBroadcaster
{
    /// <summary>
    /// New stream for this player. Does not touch other streams yet.
    /// </summary>
    StreamSubscription Subscribe(string playerName);

    /// <summary>
    /// Make this the player's current stream. Any earlier stream for the same player gets
    /// a Superseded event and is completed. Call this only after BeginSession(StreamId) has
    /// returned, so that the old stream's EndSession(oldId) is a no-op by the time it runs.
    /// </summary>
    void Claim(StreamSubscription subscription);

    void Join(string streamId, Location room);

    void Leave(string streamId, Location room);

    /// <summary>
    /// Queue an event for every stream in the room. Never blocks: a stream that has
    /// fallen behind drops its oldest events.
    /// </summary>
    void Publish(Location room, RoomEvent roomEvent);
}

public sealed class RoomBroadcaster : IRoomBroadcaster
{
    /// <summary>
    /// A browser that can't keep up loses its oldest events rather than stalling a RoomActor.
    /// </summary>
    private const int StreamQueueCapacity = 32;

    // Traffic is a handful of players, so one lock is simpler than lock-free bookkeeping.
    private readonly Lock _lock = new();
    private readonly Dictionary<string, Channel<RoomEvent>> _streams = new();
    private readonly Dictionary<Location, HashSet<string>> _rooms = new();
    private readonly Dictionary<string, string> _currentStreamByPlayer = new();

    public StreamSubscription Subscribe(string playerName)
    {
        var channel = Channel.CreateBounded<RoomEvent>(new BoundedChannelOptions(StreamQueueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
        var streamId = Guid.NewGuid().ToString("N");
        lock (_lock)
        {
            _streams[streamId] = channel;
        }
        return new StreamSubscription(this, streamId, playerName, channel.Reader);
    }

    public void Claim(StreamSubscription subscription)
    {
        lock (_lock)
        {
            if (_currentStreamByPlayer.TryGetValue(subscription.PlayerName, out var previous)
                && previous != subscription.StreamId
                && _streams.TryGetValue(previous, out var old))
            {
                old.Writer.TryWrite(new RoomEvent(RoomEventKind.Superseded, null, null));
                old.Writer.TryComplete();
            }
            _currentStreamByPlayer[subscription.PlayerName] = subscription.StreamId;
        }
    }

    public void Join(string streamId, Location room)
    {
        lock (_lock)
        {
            if (!_streams.ContainsKey(streamId))
            {
                return;
            }
            if (!_rooms.TryGetValue(room, out var members))
            {
                members = [];
                _rooms[room] = members;
            }
            members.Add(streamId);
        }
    }

    public void Leave(string streamId, Location room)
    {
        lock (_lock)
        {
            RemoveFromRoom(streamId, room);
        }
    }

    public void Publish(Location room, RoomEvent roomEvent)
    {
        lock (_lock)
        {
            if (!_rooms.TryGetValue(room, out var members))
            {
                return;
            }
            foreach (var streamId in members)
            {
                if (_streams.TryGetValue(streamId, out var channel))
                {
                    channel.Writer.TryWrite(roomEvent);
                }
            }
        }
    }

    private void RemoveFromRoom(string streamId, Location room)
    {
        if (_rooms.TryGetValue(room, out var members) && members.Remove(streamId) && members.Count == 0)
        {
            _rooms.Remove(room);
        }
    }

    internal void Unsubscribe(StreamSubscription subscription)
    {
        lock (_lock)
        {
            if (_streams.Remove(subscription.StreamId, out var channel))
            {
                channel.Writer.TryComplete();
            }
            foreach (var room in _rooms.Keys.ToList())
            {
                RemoveFromRoom(subscription.StreamId, room);
            }
            if (_currentStreamByPlayer.TryGetValue(subscription.PlayerName, out var current)
                && current == subscription.StreamId)
            {
                _currentStreamByPlayer.Remove(subscription.PlayerName);
            }
        }
    }
}

/// <summary>
/// One player's SSE stream. Dispose when the response ends.
/// </summary>
public sealed class StreamSubscription : IDisposable
{
    private readonly RoomBroadcaster _broadcaster;

    internal StreamSubscription(RoomBroadcaster broadcaster, string streamId, string playerName, ChannelReader<RoomEvent> reader)
    {
        _broadcaster = broadcaster;
        StreamId = streamId;
        PlayerName = playerName;
        Reader = reader;
    }

    public string StreamId { get; }
    public string PlayerName { get; }
    public ChannelReader<RoomEvent> Reader { get; }

    public void Dispose() => _broadcaster.Unsubscribe(this);
}
