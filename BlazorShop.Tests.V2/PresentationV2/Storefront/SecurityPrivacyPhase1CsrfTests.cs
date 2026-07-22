namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class SecurityPrivacyPhase1CsrfTests
    {
        [Fact]
        public void StorefrontHead_ProjectsAntiforgeryTokenForJavaScript()
        {
            var app = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/App.razor");
            var component = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Security/StorefrontAntiforgeryHead.razor");

            Assert.Contains("<StorefrontAntiforgeryHead />", app, StringComparison.Ordinal);
            Assert.Contains("Antiforgery.GetAndStoreTokens", component, StringComparison.Ordinal);
            Assert.Contains("blazorshop-antiforgery-token", component, StringComparison.Ordinal);
            Assert.Contains("X-CSRF-TOKEN", component, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontCartJavaScript_SendsAntiforgeryHeaderForMutationsOnly()
        {
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");

            Assert.Contains("blazorshop-antiforgery-token", script, StringComparison.Ordinal);
            Assert.Contains("blazorshop-antiforgery-header", script, StringComparison.Ordinal);
            Assert.Contains("normalizedMethod !== \"GET\"", script, StringComparison.Ordinal);
            Assert.Contains("options.headers[antiforgery.headerName] = antiforgery.token", script, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontCartMutationEndpoints_ValidateAntiforgery()
        {
            var support = ReadStorefrontLocalEndpointSupportSource();
            var services = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontServiceCollectionExtensions.cs");

            Assert.Contains("options.HeaderName = \"X-CSRF-TOKEN\"", services, StringComparison.Ordinal);
            Assert.Contains("ValidateLocalCartAntiforgeryAsync", support, StringComparison.Ordinal);
            Assert.Contains("antiforgery.ValidateRequestAsync", support, StringComparison.Ordinal);
            Assert.Contains("Security validation failed", support, StringComparison.Ordinal);
        }

        private static string ReadStorefrontLocalEndpointSupportSource()
        {
            var root = FindStorefrontSupportRepositoryRoot();
            var endpointDirectory = Path.Combine(root, "BlazorShop.PresentationV2", "BlazorShop.Storefront.V2", "Endpoints");
            return string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(endpointDirectory, "StorefrontLocalEndpointSupport*.cs")
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
        }
        private static string FindStorefrontSupportRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Unable to locate BlazorShop.sln from the test output directory.");
        }
        private static string ReadRepositoryFile(string relativePath)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, relativePath);
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
