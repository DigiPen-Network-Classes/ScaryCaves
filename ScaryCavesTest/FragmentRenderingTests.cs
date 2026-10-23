using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScaryCavesWeb.Components.Shared;
using ScaryCavesWeb.Live;
using ScaryCavesWeb.Models;

namespace ScaryCavesTest;

public class FragmentRenderingTests : IAsyncDisposable
{
    private readonly ServiceProvider _services = new ServiceCollection().AddLogging().BuildServiceProvider();
    private readonly FragmentRenderer _renderer;

    public FragmentRenderingTests()
    {
        _renderer = new FragmentRenderer(
            _services.GetRequiredService<IServiceScopeFactory>(),
            _services.GetRequiredService<ILoggerFactory>());
    }

    private static RoomState Hall(params string[] players) => new(new Room(3, "Hall", "A long hall.",
        [new RoomExit(Direction.North, 4, "z"), new RoomExit(Direction.Up, 5, "z")],
        [..players], "z",
        [new MobState("bat-1", "bat", "Bat", "A bat hangs here.")]));

    private Task<string> RenderPanel(RoomState room, string viewer) =>
        _renderer.RenderAsync<RoomPanel>(new Dictionary<string, object?>
        {
            [nameof(RoomPanel.Room)] = room,
            [nameof(RoomPanel.ViewerName)] = viewer,
        });

    [Fact]
    public async Task RoomPanel_ShowsRoomAndExits()
    {
        var html = await RenderPanel(Hall("Bob", "Alice"), "Bob");
        Assert.Contains("Room 3: Hall", html);
        Assert.Contains("A bat hangs here.", html);
        Assert.Contains("hx-post=\"/cave/move/North\"", html);
        Assert.Contains("GO Up", html);
        Assert.Contains("Your Name: Bob", html);
    }

    [Fact]
    public async Task RoomPanel_OtherPlayersExcludesViewer()
    {
        var html = await RenderPanel(Hall("Bob", "Alice"), "Bob");
        Assert.Contains("<li>Alice</li>", html);
        Assert.DoesNotContain("<li>Bob</li>", html);
    }

    [Fact]
    public async Task RoomPanel_AloneHidesOtherPlayers()
    {
        var html = await RenderPanel(Hall("Bob"), "Bob");
        Assert.DoesNotContain("Other Players:", html);
    }

    [Fact]
    public async Task RoomPanel_EscapesMarkupInNames()
    {
        var html = await RenderPanel(Hall("Bob", "<b>x</b>"), "Bob");
        Assert.Contains("&lt;b&gt;x&lt;/b&gt;", html);
        Assert.DoesNotContain("<b>x</b>", html);
    }

    [Fact]
    public async Task MessageItem_FormatsTimestamp()
    {
        var html = await _renderer.RenderAsync<MessageItem>(new Dictionary<string, object?>
        {
            [nameof(MessageItem.Text)] = "Bob has entered the room.",
            [nameof(MessageItem.At)] = new DateTimeOffset(2026, 10, 23, 14, 5, 9, TimeSpan.Zero),
        });
        Assert.Equal("<li>Bob has entered the room. (14:05:09)</li>", html.Trim());
    }

    public async ValueTask DisposeAsync()
    {
        await _renderer.DisposeAsync();
        await _services.DisposeAsync();
    }
}
