namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using Xunit;

    public sealed class CommerceNodeConfigurationBoundaryTests
    {
        [Fact]
        public void AppDbContext_UsesFilteredConfigurationScan()
        {
            var source = ReadRepositoryFile("BlazorShop.Infrastructure/Data/AppDbContext.cs");

            Assert.Contains("ApplyConfigurationsFromAssembly", source, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Infrastructure.Data.Configurations", source, StringComparison.Ordinal);
            Assert.Contains("type.Namespace?.StartsWith", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);", source, StringComparison.Ordinal);
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
    }
}
