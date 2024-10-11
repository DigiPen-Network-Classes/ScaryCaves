using ScaryCavesWeb.Services.Databases;

namespace ScaryCavesWeb.Services;

public class WorldInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public WorldInitializerHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Resolve IWorldDatabase using a scoped service scope
        using var scope = _serviceProvider.CreateScope();
        var worldDatabase = scope.ServiceProvider.GetRequiredService<IWorldDatabase>();
        await worldDatabase.ResetZones();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

}
