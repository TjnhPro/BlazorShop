namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Infrastructure.Data.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Microsoft.EntityFrameworkCore.Migrations;
    using Microsoft.EntityFrameworkCore.Migrations.Operations;

    using Xunit;

    public sealed class CommerceNodeMigrationModelConsistencyTests
    {
        [Fact]
        public void DatabaseFacade_DoesNotReportPendingCommerceNodeModelChanges()
        {
            using var context = CreateContext();

            Assert.False(context.Database.HasPendingModelChanges());
        }

        [Fact]
        public void RuntimeModel_MatchesCommerceNodeMigrationSnapshot()
        {
            using var context = CreateContext();
            var operations = GetSnapshotDifferences(context);

            Assert.True(
                operations.Count == 0,
                $"Commerce Node runtime model differs from migration snapshot: {string.Join(", ", operations.Select(operation => operation.GetType().Name))}");
        }

        private static IReadOnlyList<MigrationOperation> GetSnapshotDifferences(CommerceNodeDbContext context)
        {
            var differ = context.GetService<IMigrationsModelDiffer>();
            var modelRuntimeInitializer = context.GetService<IModelRuntimeInitializer>();
            var validationLogger = context.GetService<IDiagnosticsLogger<DbLoggerCategory.Model.Validation>>();
            var designTimeModel = context.GetService<IDesignTimeModel>().Model;
            var snapshot = CreateSnapshot();
            var snapshotModel = modelRuntimeInitializer.Initialize(snapshot.Model, designTime: true, validationLogger);

            return differ.GetDifferences(snapshotModel.GetRelationalModel(), designTimeModel.GetRelationalModel());
        }

        private static ModelSnapshot CreateSnapshot()
        {
            var assembly = typeof(CommerceNodeDbContext).Assembly;
            var snapshotType = assembly.GetType(
                "BlazorShop.Infrastructure.Data.CommerceNode.Migrations.CommerceNodeDbContextModelSnapshot",
                throwOnError: true)!;
            return (ModelSnapshot)Activator.CreateInstance(snapshotType, nonPublic: true)!;
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseNpgsql(
                    "Host=localhost;Port=5434;Database=blazorshop_commerce_node;Username=blazorshop_commerce_node;Password=blazorshop_commerce_node_dev",
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(CommerceNodeDbContext).Assembly.FullName);
                        npgsqlOptions.EnableRetryOnFailure();
                    })
                .Options;

            return new CommerceNodeDbContext(options);
        }
    }
}
