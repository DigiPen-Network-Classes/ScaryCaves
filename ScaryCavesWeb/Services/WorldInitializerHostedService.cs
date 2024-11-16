using ScaryCavesWeb.Actors;
using ScaryCavesWeb.Services.Databases;

namespace ScaryCavesWeb.Services;

public class WorldInitializerHostedService(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Resolve IWorldDatabase using a scoped service scope
        using var scope = serviceProvider.CreateScope();
        var worldDatabase = scope.ServiceProvider.GetRequiredService<IWorldDatabase>();
        var clusterClient = scope.ServiceProvider.GetRequiredService<IClusterClient>();
        foreach (var zone in worldDatabase.Zones)
        {
            await clusterClient.GetGrain<IZoneDefinitionActor>(zone.Name).ReloadFrom(zone);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

}
