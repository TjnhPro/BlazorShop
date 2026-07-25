namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontContractOwnershipTests
    {
        [Fact]
        public void StorefrontComponents_DoNotImportBackendOrHostBoundaries()
        {
            var sources = ReadSources("BlazorShop.PresentationV2/BlazorShop.Storefront.Components");
            var project = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj");

            Assert.DoesNotContain("BlazorShop.Domain", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Application", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Infrastructure", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.ControlPlane", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.CommerceNode", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Web.SharedV2.Models", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("<ProjectReference", project, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontRuntime_DoesNotImportRazorUiHostOrWasmBoundaries()
        {
            var sources = ReadSources("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime");
            var project = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj");

            Assert.DoesNotContain("@page", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("@code", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.AspNetCore.Components", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.Components", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.WASM", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.V2", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.Services", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Web.SharedV2", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.Components", project, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.WASM", project, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.V2", project, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontClient_DoesNotImportRuntimeComponentsOrHostBoundaries()
        {
            var sources = ReadSources("BlazorShop.PresentationV2/BlazorShop.Storefront.Client");
            var project = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj");

            Assert.DoesNotContain("BlazorShop.Storefront.Runtime", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.Components", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.WASM", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.V2", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Web.SharedV2", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("<ProjectReference", project, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontV2_DoesNotUseWebSharedV2ModelsForBusinessContracts()
        {
            var sources = ReadSources("BlazorShop.PresentationV2/BlazorShop.Storefront.V2");

            Assert.DoesNotContain("BlazorShop.Web.SharedV2.Models", sources, StringComparison.Ordinal);
            Assert.DoesNotContain("using BlazorShop.Web.SharedV2.Models", sources, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontV2_ManualClientExceptionsRemainDocumented()
        {
            var qa = ReadRepositoryFile("docs/refactor-control-Commerce-storefront/Storefront V2 Shared Platform Functional MVP.qa.md");

            Assert.Contains("StorefrontApiClient.MergeCurrentCustomerCartAsync", qa, StringComparison.Ordinal);
            Assert.Contains("StorefrontApiClient.UpdateCheckoutAddressesAsync", qa, StringComparison.Ordinal);
            Assert.Contains("saved-address checkout call carries a bearer token", qa, StringComparison.Ordinal);
            Assert.Contains("IStorefrontCustomerClient", qa, StringComparison.Ordinal);
            Assert.Contains("Protected customer profile, customer address book, and customer order self-service", qa, StringComparison.Ordinal);
            Assert.Contains("StorefrontAuthClient", qa, StringComparison.Ordinal);
            Assert.DoesNotContain("IStorefrontAddressClient` through manual", qa, StringComparison.Ordinal);
            Assert.DoesNotContain("IStorefrontConsentClient` through manual", qa, StringComparison.Ordinal);
        }

        private static string ReadSources(string relativeDirectory)
        {
            var root = RepositoryPath(relativeDirectory);
            var files = Directory
                .EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal);

            return string.Join(Environment.NewLine, files.Select(File.ReadAllText));
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(RepositoryPath(relativePath));
        }

        private static string RepositoryPath(string relativePath)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, relativePath);
                if (File.Exists(candidate) || Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException($"Could not locate repository path '{relativePath}'.");
        }
    }
}
