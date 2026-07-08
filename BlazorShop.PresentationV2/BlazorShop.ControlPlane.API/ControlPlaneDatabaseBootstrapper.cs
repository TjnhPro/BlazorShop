namespace BlazorShop.ControlPlane.API
{
    using BlazorShop.Infrastructure.Data.ControlPlane;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;

    internal static class ControlPlaneDatabaseBootstrapper
    {
        public static async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            using var scope = services.CreateScope();

            var controlPlaneDbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            await controlPlaneDbContext.Database.MigrateAsync(cancellationToken);

            var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            if (environment.IsDevelopment())
            {
                var developmentSeeder = scope.ServiceProvider.GetRequiredService<ControlPlaneDevelopmentSeeder>();
                await developmentSeeder.SeedConfiguredAccountsAsync(cancellationToken);
            }
        }
    }
}
