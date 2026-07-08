namespace BlazorShop.ControlPlane.API
{
    using BlazorShop.Infrastructure.Data;
    using BlazorShop.Infrastructure.Data.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    internal static class ControlPlaneDatabaseBootstrapper
    {
        public static async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            using var scope = services.CreateScope();

            var authDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await authDbContext.Database.MigrateAsync(cancellationToken);

            var controlPlaneDbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            await controlPlaneDbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
