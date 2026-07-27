namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontHostCompositionTests
    {
        [Fact]
        public void Program_RemainsCompositionOnly()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");
            var logicalLines = program
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToArray();

            Assert.True(logicalLines.Length <= 45, $"Program.cs has {logicalLines.Length} logical lines.");
            Assert.Contains("builder.Services.AddStorefrontV2Services(", program, StringComparison.Ordinal);
            Assert.Contains("app.UseStorefrontV2HostPipeline(storefrontRateLimitingOptions);", program, StringComparison.Ordinal);
            Assert.Contains("app.UseStorefrontPresentation();", program, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontPresentation();", program, StringComparison.Ordinal);
            Assert.DoesNotContain("app.MapStorefrontPresentationAccountEndpoints();", program, StringComparison.Ordinal);
            Assert.DoesNotContain("app.MapStorefrontPresentationCartEndpoints();", program, StringComparison.Ordinal);
            Assert.DoesNotContain("app.MapStorefrontPresentationCheckoutEndpoints();", program, StringComparison.Ordinal);
            Assert.DoesNotContain("app.MapStorefrontPresentationSeoEndpoints();", program, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontApiClient", program, StringComparison.Ordinal);
            Assert.DoesNotContain("new HttpClient", program, StringComparison.Ordinal);
            Assert.DoesNotContain("AddHttpClient", program, StringComparison.Ordinal);
            Assert.DoesNotContain("app.MapPost(", program, StringComparison.Ordinal);
            Assert.DoesNotContain("app.MapPut(", program, StringComparison.Ordinal);
            Assert.DoesNotContain("app.MapDelete(", program, StringComparison.Ordinal);
            Assert.DoesNotContain("async ", program, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontServiceCollection_IsSplitByHostRuntimeGeneratedBffSeoAndAuthGroups()
        {
            var services = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontServiceCollectionExtensions.cs");

            Assert.Contains("AddStorefrontHostOptions(configuration)", services, StringComparison.Ordinal);
            Assert.Contains("AddStorefrontRuntimeRegistration(configuration)", services, StringComparison.Ordinal);
            Assert.Contains("AddStorefrontGeneratedClientRegistration()", services, StringComparison.Ordinal);
            Assert.Contains("AddStorefrontBffEndpointDependencies()", services, StringComparison.Ordinal);
            Assert.Contains("AddStorefrontPresentation(configuration)", services, StringComparison.Ordinal);
            Assert.Contains("AddStorefrontAuthSessionAndAntiforgeryPolicies(", services, StringComparison.Ordinal);
            Assert.DoesNotContain("AddStorefrontSeoMediaAndDeploymentServices()", services, StringComparison.Ordinal);
        }

        [Fact]
        public void ManualStorefrontApiClient_RemainsIsolatedToDocumentedExceptionFiles()
        {
            var root = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2");
            var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Configuration/StorefrontServiceCollectionExtensions.cs",
                "Services/GeneratedStorefrontCartClient.cs",
                "Services/GeneratedStorefrontCheckoutClient.cs",
                "Services/StorefrontApiClient.Address.cs",
                "Services/StorefrontApiClient.Cart.cs",
                "Services/StorefrontApiClient.Catalog.cs",
                "Services/StorefrontApiClient.Checkout.cs",
                "Services/StorefrontApiClient.Configuration.cs",
                "Services/StorefrontApiClient.Consent.cs",
                "Services/StorefrontApiClient.Content.cs",
                "Services/StorefrontApiClient.Customer.cs",
                "Services/StorefrontApiClient.Payment.cs",
                "Services/StorefrontApiClient.cs",
                "Services/StorefrontApiRoutes.cs",
                "Services/StorefrontApiTransport.cs",
            };

            var offenders = Directory
                .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(path => File.ReadAllText(path).Contains("StorefrontApiClient", StringComparison.Ordinal))
                .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
                .Where(path => !allowedFiles.Contains(path))
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(offenders);

            var qa = ReadRepositoryFile("docs/refactor-control-Commerce-storefront/Storefront V2 Shared Platform Functional MVP.qa.md");
            Assert.Contains("Manual Storefront API client", qa, StringComparison.Ordinal);
            Assert.Contains("protected customer/account bearer-token methods", qa, StringComparison.Ordinal);
            Assert.Contains("cart merge", qa, StringComparison.Ordinal);
            Assert.Contains("saved-address checkout bearer exception", qa, StringComparison.Ordinal);
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
                var candidate = Path.Combine(directory.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
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
