namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontEndpointDependencyBoundaryTests
    {
        [Fact]
        public void StorefrontLocalEndpointMappings_DoNotInjectConcreteStorefrontApiClient()
        {
            var endpointFiles = Directory.GetFiles(
                RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints"),
                "*.cs",
                SearchOption.TopDirectoryOnly);

            var offenders = endpointFiles
                .Where(path => File.ReadAllText(path).Contains("StorefrontApiClient apiClient", StringComparison.Ordinal))
                .Select(path => Path.GetFileName(path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(offenders);
        }

        private static string RepositoryPath(string relativePath)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, relativePath);
                if (Directory.Exists(candidate) || File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException($"Could not locate repository path '{relativePath}'.");
        }
    }
}
