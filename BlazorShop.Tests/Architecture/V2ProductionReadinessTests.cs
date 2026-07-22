namespace BlazorShop.Tests.Architecture
{
    using System.Text.RegularExpressions;

    using Xunit;

    public sealed class V2ProductionReadinessTests
    {
        [Fact]
        public void Phase0_CiBaseline_RecordsLegacyReleaseGateGap()
        {
            var workflow = ReadRepositoryFile(".github/workflows/ci.yml");

            Assert.Contains("dotnet restore BlazorShop.sln", workflow, StringComparison.Ordinal);
            Assert.Contains("dotnet build BlazorShop.sln --configuration Release --no-restore", workflow, StringComparison.Ordinal);
            Assert.Contains("dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --configuration Release --no-build", workflow, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Presentation/BlazorShop.API/Dockerfile", workflow, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Presentation/BlazorShop.Storefront/Dockerfile", workflow, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Presentation/BlazorShop.Web/Dockerfile", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("ci-v2", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.V2.slnf", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj", workflow, StringComparison.Ordinal);
        }

        [Fact]
        public void Phase0_ProductionComposeBaseline_RecordsLegacyProductionTargetGap()
        {
            var compose = ReadRepositoryFile("compose.production.yml")
                .Replace('\\', '/');

            Assert.Contains("BlazorShop.Presentation/BlazorShop.API/Dockerfile", compose, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Presentation/BlazorShop.Storefront/Dockerfile", compose, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Presentation/BlazorShop.Web/Dockerfile", compose, StringComparison.Ordinal);
            Assert.Contains("ConnectionStrings__DefaultConnection", compose, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.PresentationV2/", compose, StringComparison.Ordinal);
            Assert.DoesNotContain("ConnectionStrings__ControlPlaneConnection", compose, StringComparison.Ordinal);
            Assert.DoesNotContain("ConnectionStrings__CommerceNodeConnection", compose, StringComparison.Ordinal);
        }

        [Fact]
        public void Phase0_V2TestProjectBaseline_RecordsCoreCommerceTestsMissingFromGate()
        {
            var v2TestProject = ReadRepositoryFile("BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj");
            var applicationCommerceNodeTests = EnumerateFiles("BlazorShop.Tests/Application/CommerceNode", "*.cs").ToArray();
            var infrastructureCommerceNodeTests = EnumerateFiles("BlazorShop.Tests/Infrastructure/CommerceNode", "*.cs").ToArray();

            Assert.Contains(@"..\BlazorShop.Tests\Architecture\**\*.cs", v2TestProject, StringComparison.Ordinal);
            Assert.Contains(@"..\BlazorShop.Tests\PresentationV2\**\*.cs", v2TestProject, StringComparison.Ordinal);
            Assert.DoesNotContain(@"..\BlazorShop.Tests\Application\**\*.cs", v2TestProject, StringComparison.Ordinal);
            Assert.DoesNotContain(@"..\BlazorShop.Tests\Infrastructure\**\*.cs", v2TestProject, StringComparison.Ordinal);
            Assert.True(applicationCommerceNodeTests.Length >= 10);
            Assert.True(infrastructureCommerceNodeTests.Length >= 20);
            Assert.Contains(
                "BlazorShop.Tests/Application/CommerceNode/StorefrontCheckoutServiceTests.cs",
                applicationCommerceNodeTests.Select(ToRepositoryRelativePath));
            Assert.Contains(
                "BlazorShop.Tests/Infrastructure/CommerceNode/CommerceNodeDbContextModelTests.cs",
                infrastructureCommerceNodeTests.Select(ToRepositoryRelativePath));
        }

        [Fact]
        public void Phase0_StorefrontConcreteApiClientInjectionInventory_MatchesCurrentBaseline()
        {
            var concreteUsages = EnumerateFiles("BlazorShop.PresentationV2/BlazorShop.Storefront.V2", "*.*")
                .Where(path => path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                               || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .Where(path =>
                {
                    var source = File.ReadAllText(path);
                    return source.Contains("@inject StorefrontApiClient", StringComparison.Ordinal)
                           || Regex.IsMatch(source, @"\bStorefrontApiClient\s+\w+", RegexOptions.CultureInvariant);
                })
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(
                [
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontHeader.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountAddressesPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountOrderDetailPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountOrdersPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/AccountProfilePage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/CategoryPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/CheckoutPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Home.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/NewReleases.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/PaymentCancelPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/PaymentSuccessPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/ProductPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/SearchPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/StorefrontPage.razor",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/TodaysDeals.razor",
                ],
                concreteUsages);
        }

        [Fact]
        public void Phase0_RateLimitIdentityBaseline_RecordsCurrentUserAndRemoteIpPartitioning()
        {
            var commerceNodeProgram = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Program.cs");
            var commerceNodeIdentity = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Configuration/StorefrontRateLimitIdentity.cs");
            var storefrontPolicy = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontRateLimitPolicies.cs");
            var storefrontIdentity = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontRateLimitIdentity.cs");

            Assert.Contains("StorefrontRateLimitIdentity.ResolveActor(httpContext)", commerceNodeProgram, StringComparison.Ordinal);
            Assert.Contains("ClaimTypes.NameIdentifier", commerceNodeIdentity, StringComparison.Ordinal);
            Assert.Contains("CartTokenHeaderName = \"X-Cart-Token\"", commerceNodeIdentity, StringComparison.Ordinal);
            Assert.Contains("httpContext.Connection.RemoteIpAddress", commerceNodeIdentity, StringComparison.Ordinal);
            Assert.Contains("StorefrontRateLimitIdentity.ResolveLocalCartActor(httpContext)", storefrontPolicy, StringComparison.Ordinal);
            Assert.Contains("httpContext.Connection.RemoteIpAddress", storefrontIdentity, StringComparison.Ordinal);
            Assert.DoesNotContain("X-Forwarded-For", commerceNodeIdentity, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("X-Forwarded-For", storefrontPolicy, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("X-Forwarded-For", storefrontIdentity, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("StorefrontRateLimitPolicyNames.AuthStrict", commerceNodeProgram, StringComparison.Ordinal);
            Assert.Contains("StorefrontRateLimitPolicyNames.Cart", commerceNodeProgram, StringComparison.Ordinal);
            Assert.Contains("StorefrontRateLimitPolicyNames.Checkout", commerceNodeProgram, StringComparison.Ordinal);
            Assert.Contains("StorefrontRateLimitPolicies.LocalCartPolicyName", ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontCartEndpoints.cs"), StringComparison.Ordinal);
        }

        [Fact]
        public void Phase0_OpenApiGeneratorBaseline_RecordsHandWrittenSmokeOnly()
        {
            var storefrontOpenApiTests = ReadRepositoryFile("BlazorShop.Tests/PresentationV2/CommerceNode/CommerceNodeStorefrontOpenApiContractTests.cs");

            Assert.Contains("StorefrontSwagger_CanGenerateTypeScriptClientSmoke", storefrontOpenApiTests, StringComparison.Ordinal);
            Assert.Contains("GenerateTypeScriptClient", storefrontOpenApiTests, StringComparison.Ordinal);
            Assert.Contains("Promise<unknown>", storefrontOpenApiTests, StringComparison.Ordinal);
            Assert.False(File.Exists(RepositoryPath(".config/dotnet-tools.json")));
            Assert.False(File.Exists(RepositoryPath("package.json")));
        }

        private static IEnumerable<string> EnumerateFiles(string relativeRoot, string searchPattern)
        {
            var root = RepositoryPath(relativeRoot);
            return Directory.Exists(root)
                ? Directory.EnumerateFiles(root, searchPattern, SearchOption.AllDirectories)
                    .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                                        || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)
                                        || segment.Equals("node_modules", StringComparison.OrdinalIgnoreCase)))
                : [];
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(RepositoryPath(relativePath));
        }

        private static string RepositoryPath(string relativePath)
        {
            return Path.Combine(FindRepositoryRoot().FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string ToRepositoryRelativePath(string path)
        {
            return Path.GetRelativePath(FindRepositoryRoot().FullName, path)
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');
        }

        private static DirectoryInfo FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null && !File.Exists(Path.Combine(current.FullName, "BlazorShop.sln")))
            {
                current = current.Parent;
            }

            Assert.NotNull(current);
            return current!;
        }
    }
}
