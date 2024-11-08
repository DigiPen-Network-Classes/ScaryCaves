using ScaryCavesWeb.Services.Databases;

namespace ScaryCavesWeb.Services;

public class WorldInitializerHostedService(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Resolve IWorldDatabase using a scoped service scope
        using var scope = serviceProvider.CreateScope();
        var worldDatabase = scope.ServiceProvider.GetRequiredService<IWorldDatabase>();
        await worldDatabase.ResetZones();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

}
