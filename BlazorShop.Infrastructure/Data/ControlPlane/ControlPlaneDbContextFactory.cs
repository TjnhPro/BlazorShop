namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;
    using Microsoft.Extensions.Configuration;

    public sealed class ControlPlaneDbContextFactory : IDesignTimeDbContextFactory<ControlPlaneDbContext>
    {
        public ControlPlaneDbContext CreateDbContext(string[] args)
        {
            var basePath = ResolveControlPlaneApiProjectPath();
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables();

            var configuration = builder.Build();
            var connectionString = configuration.GetConnectionString("ControlPlaneConnection")
                                   ?? "Host=localhost;Port=5433;Database=blazorshop_controlplane;Username=blazorshop_controlplane;Password=blazorshop_controlplane_dev";

            var optionsBuilder = new DbContextOptionsBuilder<ControlPlaneDbContext>();
            optionsBuilder.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ControlPlaneDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure();
                });

            return new ControlPlaneDbContext(optionsBuilder.Options);
        }

        private static string ResolveControlPlaneApiProjectPath()
        {
            var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (currentDirectory is not null)
            {
                var apiProjectPath = Path.Combine(
                    currentDirectory.FullName,
                    "BlazorShop.PresentationV2",
                    "BlazorShop.ControlPlane.API");

                if (Directory.Exists(apiProjectPath))
                {
                    return apiProjectPath;
                }

                if (File.Exists(Path.Combine(currentDirectory.FullName, "BlazorShop.ControlPlane.API.csproj")))
                {
                    return currentDirectory.FullName;
                }

                currentDirectory = currentDirectory.Parent;
            }

            return Directory.GetCurrentDirectory();
        }
    }
}
