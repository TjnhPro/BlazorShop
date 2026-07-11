namespace BlazorShop.CommerceNode.API
{
    using System.Diagnostics;

    using BlazorShop.Infrastructure.Data.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    internal static class CommerceNodeDatabaseBootstrapper
    {
        public static async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            using var scope = services.CreateScope();

            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("BlazorShop.CommerceNode.API.DatabaseMigration");
            var logMigrationState = configuration.GetValue("CommerceNode:Database:LogMigrationState", true);
            var failStartupOnMigrationError = configuration.GetValue("CommerceNode:Database:FailStartupOnMigrationError", true);
            var commerceNodeDbContext = scope.ServiceProvider.GetRequiredService<CommerceNodeDbContext>();
            var migrationSucceeded = false;

            try
            {
                if (logMigrationState)
                {
                    var appliedMigrations = (await commerceNodeDbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
                    var pendingMigrations = (await commerceNodeDbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();

                    logger.LogInformation(
                        "Commerce Node database migration state for {ContextName} using {ConnectionName}: {AppliedCount} applied, {PendingCount} pending. Pending migrations: {PendingMigrations}",
                        nameof(CommerceNodeDbContext),
                        "CommerceNodeConnection",
                        appliedMigrations.Length,
                        pendingMigrations.Length,
                        pendingMigrations);
                }

                var stopwatch = Stopwatch.StartNew();
                logger.LogInformation(
                    "Commerce Node database migration started for {ContextName} using {ConnectionName}.",
                    nameof(CommerceNodeDbContext),
                    "CommerceNodeConnection");

                await commerceNodeDbContext.Database.MigrateAsync(cancellationToken);

                stopwatch.Stop();
                logger.LogInformation(
                    "Commerce Node database migration completed for {ContextName} in {ElapsedMilliseconds} ms.",
                    nameof(CommerceNodeDbContext),
                    stopwatch.ElapsedMilliseconds);
                migrationSucceeded = true;
            }
            catch (Exception exception) when (!failStartupOnMigrationError)
            {
                logger.LogError(
                    exception,
                    "Commerce Node database migration failed for {ContextName}, but startup will continue because CommerceNode:Database:FailStartupOnMigrationError is false.",
                    nameof(CommerceNodeDbContext));
            }

            var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            if (environment.IsDevelopment() && migrationSucceeded)
            {
                var developmentSeeder = scope.ServiceProvider.GetRequiredService<CommerceNodeDevelopmentSeeder>();
                await developmentSeeder.SeedAsync(cancellationToken);
            }
        }
    }
}
