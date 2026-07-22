namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using Xunit;

    public sealed class CommerceNodeConfigurationBoundaryTests
    {
        [Fact]
        public void LegacyAppDbContext_IsRemovedFromActiveInfrastructure()
        {
            Assert.False(RepositoryFileExists("BlazorShop.Infrastructure/Data/AppDbContext.cs"));
            Assert.False(RepositoryFileExists("BlazorShop.Infrastructure/Data/AppDbContextFactory.cs"));
            Assert.False(RepositoryFileExists("BlazorShop.Infrastructure/Migrations/AppDbContextModelSnapshot.cs"));
        }

        [Fact]
        public void CommerceNodeDbContext_UsesFilteredCommerceNodeConfigurationScan()
        {
            var source = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/CommerceNodeDbContext.cs");
            var extension = ReadRepositoryFile("BlazorShop.Infrastructure/Data/CommerceNode/Configurations/CommerceNodeModelBuilderExtensions.cs");

            Assert.Contains("ApplyCommerceNodeConfigurations", source, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Infrastructure.Data.CommerceNode.Configurations", extension, StringComparison.Ordinal);
            Assert.Contains("ApplyConfigurationsFromAssembly", extension, StringComparison.Ordinal);
            Assert.Contains("type.Namespace?.StartsWith", extension, StringComparison.Ordinal);
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException($"Could not locate repository file '{relativePath}'.");
        }

        private static bool RepositoryFileExists(string relativePath)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate))
                {
                    return true;
                }

                directory = directory.Parent;
            }

            return false;
        }
    }
}
