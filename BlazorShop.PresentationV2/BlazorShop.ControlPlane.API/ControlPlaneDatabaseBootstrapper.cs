namespace BlazorShop.ControlPlane.API
{
    using System.Diagnostics;

    using BlazorShop.Infrastructure.Data.ControlPlane;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    internal static class ControlPlaneDatabaseBootstrapper
    {
        public static async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            using var scope = services.CreateScope();

            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("BlazorShop.ControlPlane.API.DatabaseMigration");
            var logMigrationState = configuration.GetValue("ControlPlane:Database:LogMigrationState", true);
            var failStartupOnMigrationError = configuration.GetValue("ControlPlane:Database:FailStartupOnMigrationError", true);
            var controlPlaneDbContext = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            var migrationSucceeded = false;

            try
            {
                string[] pendingMigrations = [];
                if (logMigrationState)
                {
                    var appliedMigrations = (await controlPlaneDbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
                    pendingMigrations = (await controlPlaneDbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();

                    logger.LogInformation(
                        "Control Plane database migration state for {ContextName} using {ConnectionName}: {AppliedCount} applied, {PendingCount} pending. Pending migrations: {PendingMigrations}",
                        nameof(ControlPlaneDbContext),
                        "ControlPlaneConnection",
                        appliedMigrations.Length,
                        pendingMigrations.Length,
                        pendingMigrations);
                }

                var stopwatch = Stopwatch.StartNew();
                logger.LogInformation(
                    "Control Plane database migration started for {ContextName} using {ConnectionName}.",
                    nameof(ControlPlaneDbContext),
                    "ControlPlaneConnection");

                await controlPlaneDbContext.Database.MigrateAsync(cancellationToken);

                stopwatch.Stop();
                logger.LogInformation(
                    "Control Plane database migration completed for {ContextName} in {ElapsedMilliseconds} ms.",
                    nameof(ControlPlaneDbContext),
                    stopwatch.ElapsedMilliseconds);
                migrationSucceeded = true;
            }
            catch (Exception exception) when (!failStartupOnMigrationError)
            {
                logger.LogError(
                    exception,
                    "Control Plane database migration failed for {ContextName}, but startup will continue because ControlPlane:Database:FailStartupOnMigrationError is false.",
                    nameof(ControlPlaneDbContext));
            }

            var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            if (environment.IsDevelopment() && migrationSucceeded)
            {
                var developmentSeeder = scope.ServiceProvider.GetRequiredService<ControlPlaneDevelopmentSeeder>();
                await developmentSeeder.SeedConfiguredAccountsAsync(cancellationToken);
            }
        }
    }
}
