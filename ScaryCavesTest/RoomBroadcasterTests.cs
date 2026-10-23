using ScaryCavesWeb.Live;
using ScaryCavesWeb.Models;

namespace ScaryCavesTest;

public class RoomBroadcasterTests
{
    private static readonly Location A = new(1, "z");
    private static readonly Location B = new(2, "z");
    private static RoomEvent Ev(string m) => new(RoomEventKind.RoomChanged, null, m);

    private readonly RoomBroadcaster _broadcaster = new();

    [Fact]
    public void Publish_ReachesOnlyStreamsInThatRoom()
    {
        using var s1 = _broadcaster.Subscribe("Bob");
        using var s2 = _broadcaster.Subscribe("Alice");
        _broadcaster.Join(s1.StreamId, A);
        _broadcaster.Join(s2.StreamId, B);

        _broadcaster.Publish(A, Ev("x"));

        Assert.True(s1.Reader.TryRead(out var got));
        Assert.Equal("x", got.Message);
        Assert.False(s2.Reader.TryRead(out _));
    }

    [Fact]
    public void Leave_StopsDelivery()
    {
        using var s1 = _broadcaster.Subscribe("Bob");
        _broadcaster.Join(s1.StreamId, A);
        _broadcaster.Leave(s1.StreamId, A);

        _broadcaster.Publish(A, Ev("x"));

        Assert.False(s1.Reader.TryRead(out _));
    }

    [Fact]
    public void Dispose_RemovesFromRooms()
    {
        var s1 = _broadcaster.Subscribe("Bob");
        _broadcaster.Join(s1.StreamId, A);
        s1.Dispose();

        _broadcaster.Publish(A, Ev("x"));

        Assert.True(s1.Reader.Completion.IsCompleted);
    }

    [Fact]
    public void Claim_SamePlayerTwice_SupersedesFirst()
    {
        using var first = _broadcaster.Subscribe("Bob");
        _broadcaster.Claim(first);
        using var second = _broadcaster.Subscribe("Bob");
        Assert.False(first.Reader.TryRead(out _));

        _broadcaster.Claim(second);

        Assert.True(first.Reader.TryRead(out var got));
        Assert.Equal(RoomEventKind.Superseded, got.Kind);
        Assert.True(first.Reader.Completion.IsCompleted);
        Assert.False(second.Reader.Completion.IsCompleted);
        Assert.NotEqual(first.StreamId, second.StreamId);
    }

    [Fact]
    public void Claim_OtherPlayer_DoesNotSupersede()
    {
        using var bob = _broadcaster.Subscribe("Bob");
        _broadcaster.Claim(bob);
        using var alice = _broadcaster.Subscribe("Alice");
        _broadcaster.Claim(alice);

        Assert.False(bob.Reader.TryRead(out _));
        Assert.False(alice.Reader.TryRead(out _));
    }

    [Fact]
    public void Dispose_OfSupersededStream_DoesNotUnregisterNewer()
    {
        var first = _broadcaster.Subscribe("Bob");
        _broadcaster.Claim(first);
        using var second = _broadcaster.Subscribe("Bob");
        _broadcaster.Claim(second);
        first.Dispose();

        _broadcaster.Join(second.StreamId, A);
        _broadcaster.Publish(A, Ev("x"));

        Assert.True(second.Reader.TryRead(out var got));
        Assert.Equal("x", got.Message);

        // a third claim must still find `second` as the current stream and supersede it
        using var third = _broadcaster.Subscribe("Bob");
        _broadcaster.Claim(third);
        Assert.True(second.Reader.TryRead(out var superseded));
        Assert.Equal(RoomEventKind.Superseded, superseded.Kind);
    }

    [Fact]
    public void Publish_FullQueue_DropsOldestWithoutBlocking()
    {
        using var s1 = _broadcaster.Subscribe("Bob");
        _broadcaster.Join(s1.StreamId, A);

        for (var i = 0; i < 40; i++)
        {
            _broadcaster.Publish(A, Ev(i.ToString()));
        }

        var messages = new List<string?>();
        while (s1.Reader.TryRead(out var e))
        {
            messages.Add(e.Message);
        }
        Assert.Equal(32, messages.Count);
        Assert.Equal("8", messages[0]);
        Assert.Equal("39", messages[^1]);
    }
}
