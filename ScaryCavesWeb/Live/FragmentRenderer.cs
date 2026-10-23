using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ScaryCavesWeb.Live;

/// <summary>
/// Renders a Razor component to an HTML string, so the SSE stream and the htmx endpoints
/// push the same markup the page renders - one definition of what a room panel looks like.
/// </summary>
public sealed class FragmentRenderer : IAsyncDisposable
{
    private readonly AsyncServiceScope _scope;
    private readonly ILoggerFactory _loggerFactory;

    public FragmentRenderer(IServiceScopeFactory scopeFactory, ILoggerFactory loggerFactory)
    {
        // HtmlRenderer resolves scoped services, so it can't use the root provider;
        // fragments inject nothing, so one scope for the app's lifetime is enough.
        _scope = scopeFactory.CreateAsyncScope();
        _loggerFactory = loggerFactory;
    }

    public async Task<string> RenderAsync<TComponent>(IDictionary<string, object?> parameters) where TComponent : IComponent
    {
        // A renderer keeps every root component it has rendered until disposed,
        // so use a fresh one per fragment rather than leaking one tree per event.
        await using var renderer = new HtmlRenderer(_scope.ServiceProvider, _loggerFactory);
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters));
            return output.ToHtmlString();
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
    }
}
