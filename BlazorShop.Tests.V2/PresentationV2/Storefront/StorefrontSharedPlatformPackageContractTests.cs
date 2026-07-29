namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Xml.Linq;

    using BlazorShop.Storefront.Client;
    using BlazorShop.Storefront.Runtime;

    using Microsoft.Extensions.DependencyInjection;

    using Xunit;

    public sealed class StorefrontSharedPlatformPackageContractTests
    {
        private static readonly string[] PackageProjectPaths =
        [
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
        ];

        [Theory]
        [InlineData("BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj", "BlazorShop.Storefront.Client")]
        [InlineData("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj", "BlazorShop.Storefront.Runtime")]
        [InlineData("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj", "BlazorShop.Storefront.Components")]
        public void StorefrontSharedPlatformPackages_HaveRequiredMetadata(string projectPath, string packageId)
        {
            var project = XDocument.Load(RepositoryPath(projectPath));
            var properties = project.Descendants("PropertyGroup").Elements().ToDictionary(element => element.Name.LocalName, element => element.Value);

            Assert.Equal(packageId, properties["PackageId"]);
            Assert.Equal("1.0.0", properties["Version"]);
            Assert.Equal("BlazorShop", properties["Authors"]);
            Assert.Equal("https://github.com/TjnhPro/BlazorShop", properties["RepositoryUrl"]);
            Assert.False(string.IsNullOrWhiteSpace(properties["Description"]));
        }

        [Fact]
        public void StorefrontComponentsPackage_MetadataAndReadmeDescribeLogicOnlyRole()
        {
            var project = XDocument.Load(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj"));
            var properties = project.Descendants("PropertyGroup").Elements().ToDictionary(element => element.Name.LocalName, element => element.Value);
            var readme = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/README.md");

            Assert.Equal(
                "Browser-safe Storefront contracts, headless interaction state, and browser primitives.",
                properties["Description"]);
            Assert.Contains("Contracts/{Capability}", readme, StringComparison.Ordinal);
            Assert.Contains("Headless/{Capability}", readme, StringComparison.Ordinal);
            Assert.Contains("Browser", readme, StringComparison.Ordinal);
            Assert.Contains("Browser interop modules are hosted by the concrete storefront project", readme, StringComparison.Ordinal);
            Assert.Contains("Razor components, shared visual wrappers, visual class bags, static web assets", readme, StringComparison.Ordinal);
            Assert.DoesNotContain("compatibility Razor wrappers", readme, StringComparison.Ordinal);
            Assert.Contains("same-origin BFF endpoints", readme, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontRuntime_SeparatesCoreRuntimeFromServerGeneratedClientRegistration()
        {
            var runtimeRegistration = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeServiceCollectionExtensions.cs");
            var applicationRegistration = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontApplicationServiceCollectionExtensions.cs");

            Assert.Contains("AddStorefrontRuntime(", runtimeRegistration, StringComparison.Ordinal);
            Assert.Contains("AddStorefrontPlatformRuntime(", runtimeRegistration, StringComparison.Ordinal);
            Assert.DoesNotContain("AddStorefrontServerGeneratedClients", runtimeRegistration, StringComparison.Ordinal);
            Assert.DoesNotContain("AddStorefrontGeneratedClients", runtimeRegistration, StringComparison.Ordinal);
            Assert.Contains("AddStorefrontPlatformRuntime", applicationRegistration, StringComparison.Ordinal);
            Assert.DoesNotContain("AddStorefrontServerGeneratedClients", applicationRegistration, StringComparison.Ordinal);
            Assert.DoesNotContain(".AddStorefrontGeneratedClients", applicationRegistration, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontRuntime_GeneratedClientRegistrationUsesHttpClientBaseAddressOnly()
        {
            var runtimeRegistration = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeServiceCollectionExtensions.cs");
            var generatedClient = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Client/Generated/StorefrontClient.g.cs");

            Assert.Contains("client.BaseAddress = new Uri(options.CommerceNodeBaseUrl, UriKind.Absolute)", runtimeRegistration, StringComparison.Ordinal);
            Assert.Contains("CreateClient(GeneratedClientHttpClientName)", runtimeRegistration, StringComparison.Ordinal);
            Assert.DoesNotContain("Activator.CreateInstance", runtimeRegistration, StringComparison.Ordinal);
            Assert.DoesNotContain("string.Empty, httpClient", runtimeRegistration, StringComparison.Ordinal);
            Assert.DoesNotContain("private string _baseUrl", generatedClient, StringComparison.Ordinal);
            Assert.DoesNotContain("string baseUrl, System.Net.Http.HttpClient httpClient", generatedClient, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontRuntime_GeneratedClientRegistration_DoesNotUseActivator()
        {
            var runtimeSource = ReadRuntimeSource();

            Assert.DoesNotContain("Activator.CreateInstance", runtimeSource, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontRuntime_EnvelopeMapping_DoesNotUseReflectionOrJsonProjection()
        {
            var runtimeSource = ReadRuntimeSource();

            Assert.DoesNotContain("GetProperty(\"Success\")", runtimeSource, StringComparison.Ordinal);
            Assert.DoesNotContain("GetProperty(\"Data\")", runtimeSource, StringComparison.Ordinal);
            Assert.DoesNotContain("GetProperty(\"Message\")", runtimeSource, StringComparison.Ordinal);
            Assert.DoesNotContain("JsonSerializer.Deserialize<TTarget>(JsonSerializer.Serialize(source", runtimeSource, StringComparison.Ordinal);
            Assert.DoesNotContain("dynamic", runtimeSource, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontRuntimeRegistration_ResolvesCurrentGeneratedClientsAndFacades()
        {
            var services = new ServiceCollection();
            services.AddStorefrontRuntime(options =>
            {
                options.StoreKey = "sample";
                options.CommerceNodeBaseUrl = "https://commerce-node.example/";
            });
            services.AddStorefrontPlatformRuntime();

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var scoped = scope.ServiceProvider;

            Assert.NotNull(scoped.GetRequiredService<IStorefrontAddressClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontAuthClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontCartClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontCatalogClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontCheckoutClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontConfigurationClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontConsentClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontContactClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontCurrencyClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontCustomerAddressesClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontCustomerProfileClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontNavigationClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontNewsletterClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontOrdersClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontPagesClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontPaymentsClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontRecommendationsClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontSeoClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontStoreClient>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontRuntimeCatalogFacade>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontRuntimeContentFacade>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontRuntimeNavigationFacade>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontRuntimeSeoFacade>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontRuntimeCatalogContentFacade>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontRuntimeCartFacade>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontRuntimeCheckoutFacade>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontRuntimeConfigurationFacade>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontRuntimeAddressFacade>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontRuntimeConsentFacade>());
            Assert.NotNull(scoped.GetRequiredService<IStorefrontRuntimePaymentFacade>());
        }

        [Fact]
        public void StorefrontRuntimeCapabilityRegistration_AllowsNarrowCatalogAndCartScopes()
        {
            var catalogServices = CreateRuntimeServices();
            catalogServices.AddStorefrontCatalogRuntime();
            using var catalogProvider = catalogServices.BuildServiceProvider();
            using var catalogScope = catalogProvider.CreateScope();

            Assert.NotNull(catalogScope.ServiceProvider.GetRequiredService<IStorefrontRuntimeCatalogFacade>());
            Assert.Null(catalogScope.ServiceProvider.GetService<IStorefrontRuntimeCartFacade>());
            Assert.Null(catalogScope.ServiceProvider.GetService<IStorefrontRuntimeCheckoutFacade>());
            Assert.Null(catalogScope.ServiceProvider.GetService<IStorefrontRuntimePaymentFacade>());

            var cartServices = CreateRuntimeServices();
            cartServices.AddStorefrontCartRuntime();
            using var cartProvider = cartServices.BuildServiceProvider();
            using var cartScope = cartProvider.CreateScope();

            Assert.NotNull(cartScope.ServiceProvider.GetRequiredService<IStorefrontCartClient>());
            Assert.NotNull(cartScope.ServiceProvider.GetRequiredService<IStorefrontRuntimeCartFacade>());
            Assert.Null(cartScope.ServiceProvider.GetService<IStorefrontRuntimeCheckoutFacade>());
            Assert.Null(cartScope.ServiceProvider.GetService<IStorefrontRuntimePaymentFacade>());
        }

        [Fact]
        public void StorefrontRuntimePlatformRegistration_ResolvesAllFacades()
        {
            var platformServices = CreateRuntimeServices();
            platformServices.AddStorefrontPlatformRuntime();

            using var platformProvider = platformServices.BuildServiceProvider();
            using var platformScope = platformProvider.CreateScope();

            Assert.NotNull(platformScope.ServiceProvider.GetRequiredService<IStorefrontRuntimeCatalogFacade>());
            Assert.NotNull(platformScope.ServiceProvider.GetRequiredService<IStorefrontRuntimeContentFacade>());
            Assert.NotNull(platformScope.ServiceProvider.GetRequiredService<IStorefrontRuntimeNavigationFacade>());
            Assert.NotNull(platformScope.ServiceProvider.GetRequiredService<IStorefrontRuntimeSeoFacade>());
            Assert.NotNull(platformScope.ServiceProvider.GetRequiredService<IStorefrontRuntimeCartFacade>());
            Assert.NotNull(platformScope.ServiceProvider.GetRequiredService<IStorefrontRuntimeCheckoutFacade>());
            Assert.NotNull(platformScope.ServiceProvider.GetRequiredService<IStorefrontRuntimeAddressFacade>());
            Assert.NotNull(platformScope.ServiceProvider.GetRequiredService<IStorefrontRuntimeConsentFacade>());
            Assert.NotNull(platformScope.ServiceProvider.GetRequiredService<IStorefrontRuntimePaymentFacade>());
        }

        [Fact]
        public void Runtime_UsesOfficialCapabilityRegistrationSurface()
        {
            var runtimeRegistration = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeServiceCollectionExtensions.cs");

            Assert.Contains("AddStorefrontPlatformRuntime(", runtimeRegistration, StringComparison.Ordinal);
            Assert.Contains("AddStorefrontCatalogRuntime(", runtimeRegistration, StringComparison.Ordinal);
            Assert.Contains("AddStorefrontCartRuntime(", runtimeRegistration, StringComparison.Ordinal);
            Assert.Contains("AddStorefrontCheckoutRuntime(", runtimeRegistration, StringComparison.Ordinal);
            Assert.Contains("AddStorefrontAccountRuntime(", runtimeRegistration, StringComparison.Ordinal);
            Assert.DoesNotContain("AddStorefrontServerGeneratedClients", runtimeRegistration, StringComparison.Ordinal);
            Assert.DoesNotContain("AddStorefrontGeneratedClients", runtimeRegistration, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontClientPackage_DoesNotReferenceRuntimeComponentsV2OrBackendProjects()
        {
            var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj");

            Assert.Empty(references);
        }

        [Fact]
        public void StorefrontRuntimePackage_OnlyReferencesStorefrontClient()
        {
            var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj");

            Assert.Equal(
                ["../BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj"],
                references);
        }

        [Fact]
        public void StorefrontComponentsPackage_DoesNotReferenceServerOnlyProjects()
        {
            var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj");
            var forbiddenFragments = new[]
            {
                "BlazorShop.CommerceNode.API",
                "BlazorShop.ControlPlane",
                "BlazorShop.Application",
                "BlazorShop.Infrastructure",
                "BlazorShop.Domain",
                "BlazorShop.Storefront.V2",
                "BlazorShop.Web.SharedV2",
            };

            var offenders = references
                .Where(reference => forbiddenFragments.Any(fragment => reference.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            Assert.Empty(offenders);
        }

        private static IReadOnlyList<string> ReadProjectReferences(string projectPath)
        {
            var project = XDocument.Load(RepositoryPath(projectPath));
            var projectDirectory = Path.GetDirectoryName(projectPath) ?? string.Empty;

            return project
                .Descendants("ProjectReference")
                .Select(reference => reference.Attribute("Include")?.Value)
                .Where(include => !string.IsNullOrWhiteSpace(include))
                .Select(include => include!.Replace('\\', '/'))
                .Select(include => include.StartsWith("../", StringComparison.Ordinal)
                    ? include
                    : Path.GetRelativePath(projectDirectory, Path.Combine(projectDirectory, include)).Replace('\\', '/'))
                .OrderBy(include => include, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(RepositoryPath(relativePath));
        }

        private static ServiceCollection CreateRuntimeServices()
        {
            var services = new ServiceCollection();
            services.AddStorefrontRuntime(options =>
            {
                options.StoreKey = "sample";
                options.CommerceNodeBaseUrl = "https://commerce-node.example/";
            });

            return services;
        }

        private static string ReadRuntimeSource()
        {
            var runtimeDirectory = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime");

            return string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(runtimeDirectory, "*.cs", SearchOption.AllDirectories)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
        }

        private static string RepositoryPath(string relativePath)
        {
            var root = AppContext.BaseDirectory;
            while (!File.Exists(Path.Combine(root, "BlazorShop.sln")))
            {
                root = Directory.GetParent(root)?.FullName
                    ?? throw new InvalidOperationException("Could not locate repository root.");
            }

            return Path.GetFullPath(Path.Combine(root, relativePath));
        }
    }
}
