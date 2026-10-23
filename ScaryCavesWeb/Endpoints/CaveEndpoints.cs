using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.Features;
using ScaryCavesWeb.Actors;
using ScaryCavesWeb.Components.Shared;
using ScaryCavesWeb.Live;
using ScaryCavesWeb.Models;

namespace ScaryCavesWeb.Endpoints;

/// <summary>
/// The cave page's live parts: one SSE stream per player (replacing the SignalR hub's
/// connection) and the htmx POST behind each exit button (replacing the hub's MoveTo).
/// </summary>
public static class CaveEndpoints
{
    /// <summary>Comment frame sent while idle, so proxies and browsers don't drop a quiet stream.</summary>
    private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(25);

    public static IEndpointRouteBuilder MapCaveEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/cave/stream", StreamAsync).RequireAuthorization();
        app.MapPost("/cave/move/{direction}", MoveAsync).RequireAuthorization();
        return app;
    }

    private static async Task StreamAsync(HttpContext http, IRoomBroadcaster broadcaster, FragmentRenderer renderer,
        IClusterClient clusterClient, ILoggerFactory loggers, CancellationToken cancellationToken)
    {
        var logger = loggers.CreateLogger("Cave.Stream");
        var playerName = http.User.Identity!.Name!;

        http.Response.Headers.ContentType = "text/event-stream";
        http.Response.Headers.CacheControl = "no-cache, no-transform";
        // Tells nginx not to buffer the stream.
        http.Response.Headers["X-Accel-Buffering"] = "no";
        http.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        var player = clusterClient.GetGrain<IPlayerActor>(playerName);
        using var subscription = broadcaster.Subscribe(playerName);
        try
        {
            // Order matters: the session must be ours before older streams are told to stop,
            // so their EndSession(oldId) arrives as a no-op.
            var view = await player.BeginSession(subscription.StreamId);
            broadcaster.Claim(subscription);
            logger.LogInformation("Player {PlayerName} stream {StreamId} connected", playerName, subscription.StreamId);

            await WriteAsync(http, SseFormatter.Format("room", await RenderPanel(renderer, view.Room, playerName)), cancellationToken);
            await WriteAsync(http, SseFormatter.Format("message", await RenderMessage(renderer, $"Welcome {playerName} to the Scary Cave!")), cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!await WaitForEventAsync(subscription, cancellationToken))
                {
                    await WriteAsync(http, ": ping\n\n", cancellationToken);
                    continue;
                }
                while (subscription.Reader.TryRead(out var roomEvent))
                {
                    if (roomEvent.Kind == RoomEventKind.Superseded)
                    {
                        await WriteAsync(http, SseFormatter.Format("superseded", ""), cancellationToken);
                        return;
                    }
                    if (roomEvent.Room != null)
                    {
                        await WriteAsync(http, SseFormatter.Format("room", await RenderPanel(renderer, roomEvent.Room, playerName)), cancellationToken);
                    }
                    if (roomEvent.Message != null)
                    {
                        await WriteAsync(http, SseFormatter.Format("message", await RenderMessage(renderer, roomEvent.Message)), cancellationToken);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // the browser went away
        }
        finally
        {
            try
            {
                var room = await player.EndSession(subscription.StreamId);
                logger.LogInformation("Player {PlayerName} stream {StreamId} closed; left {RoomId}", playerName, subscription.StreamId, room?.Id);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error ending session for {PlayerName}", playerName);
            }
        }
    }

    /// <summary>
    /// Waits for at least one event, or gives up after <see cref="KeepAliveInterval"/> so the
    /// loop can emit a keep-alive. Throws OperationCanceledException when the request ends,
    /// and when the subscription completes (superseded events are read before that).
    /// </summary>
    private static async Task<bool> WaitForEventAsync(StreamSubscription subscription, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(KeepAliveInterval);
        try
        {
            if (!await subscription.Reader.WaitToReadAsync(timeout.Token))
            {
                throw new OperationCanceledException("stream subscription completed");
            }
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
        {
            return false;
        }
    }

    private static async Task<IResult> MoveAsync(string direction, HttpContext http, IAntiforgery antiforgery,
        IClusterClient clusterClient, FragmentRenderer renderer, ILoggerFactory loggers)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Results.BadRequest();
        }
        var logger = loggers.CreateLogger("Cave.Move");
        var playerName = http.User.Identity!.Name!;

        if (!Enum.TryParse<Direction>(direction, out var d))
        {
            logger.LogError("Player {PlayerName} MoveTo called with invalid direction {Direction}", playerName, direction);
            return await MessageOnly(http, renderer, "Invalid direction");
        }

        logger.LogInformation("Player {PlayerName} wants to go {Direction}", playerName, d);
        try
        {
            var destination = await clusterClient.GetGrain<IPlayerActor>(playerName).MoveTo(d);
            if (destination == null)
            {
                logger.LogInformation("Player {PlayerName} tried to go {Direction} but was not allowed to.", playerName, d);
                return await MessageOnly(http, renderer, "You can't go that way!");
            }
            // new room: new panel, and the old room's messages no longer apply
            var panel = await RenderPanel(renderer, new RoomState(destination), playerName);
            return Results.Content(panel + "\n<ul id=\"messages\" hx-swap-oob=\"innerHTML\"></ul>", "text/html");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error moving player {PlayerName} {Direction}", playerName, d);
            return await MessageOnly(http, renderer, "Error moving player");
        }
    }

    /// <summary>
    /// A move that didn't happen: leave the room panel alone and add a line to the messages.
    /// </summary>
    private static async Task<IResult> MessageOnly(HttpContext http, FragmentRenderer renderer, string message)
    {
        http.Response.Headers["HX-Retarget"] = "#messages";
        http.Response.Headers["HX-Reswap"] = "afterbegin";
        return Results.Content(await RenderMessage(renderer, message), "text/html");
    }

    private static Task<string> RenderPanel(FragmentRenderer renderer, RoomState room, string viewerName) =>
        renderer.RenderAsync<RoomPanel>(new Dictionary<string, object?>
        {
            [nameof(RoomPanel.Room)] = room,
            [nameof(RoomPanel.ViewerName)] = viewerName,
        });

    private static Task<string> RenderMessage(FragmentRenderer renderer, string text) =>
        renderer.RenderAsync<MessageItem>(new Dictionary<string, object?>
        {
            [nameof(MessageItem.Text)] = text,
            [nameof(MessageItem.At)] = DateTimeOffset.Now,
        });

    private static async Task WriteAsync(HttpContext http, string text, CancellationToken cancellationToken)
    {
        await http.Response.WriteAsync(text, cancellationToken);
        await http.Response.Body.FlushAsync(cancellationToken);
    }
}
