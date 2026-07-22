namespace BlazorShop.Tests.Architecture
{
    using System.Text.RegularExpressions;

    using Xunit;

    public sealed class V2ProductionReadinessTests
    {
        [Fact]
        public void Phase3_CiWorkflow_UsesV2ReleaseGateAndLegacyCompatibility()
        {
            var workflow = ReadRepositoryFile(".github/workflows/ci.yml");

            Assert.Contains("ci-v2:", workflow, StringComparison.Ordinal);
            Assert.Contains("legacy-compatibility:", workflow, StringComparison.Ordinal);
            Assert.Contains("continue-on-error: true", workflow, StringComparison.Ordinal);
            Assert.Matches(
                new Regex("ci-v2:[\\s\\S]*dotnet restore BlazorShop\\.sln[\\s\\S]*dotnet build BlazorShop\\.sln --configuration Release --no-restore[\\s\\S]*dotnet test BlazorShop\\.Tests\\.V2/BlazorShop\\.Tests\\.V2\\.csproj --configuration Release --no-build", RegexOptions.CultureInvariant),
                workflow);
            Assert.Matches(
                new Regex("legacy-compatibility:[\\s\\S]*dotnet restore BlazorShop\\.Tests/BlazorShop\\.Tests\\.csproj[\\s\\S]*dotnet build BlazorShop\\.Tests/BlazorShop\\.Tests\\.csproj --configuration Release --no-restore[\\s\\S]*dotnet test BlazorShop\\.Tests/BlazorShop\\.Tests\\.csproj --configuration Release --no-build", RegexOptions.CultureInvariant),
                workflow);
            Assert.Contains("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/package-lock.json", workflow, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/package-lock.json", workflow, StringComparison.Ordinal);
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
        public void Phase3_V2TestProject_IncludesCoreCommerceAndControlPlaneTests()
        {
            var v2TestProject = ReadRepositoryFile("BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj");
            var applicationCommerceNodeTests = EnumerateFiles("BlazorShop.Tests/Application/CommerceNode", "*.cs").ToArray();
            var infrastructureCommerceNodeTests = EnumerateFiles("BlazorShop.Tests/Infrastructure/CommerceNode", "*.cs").ToArray();
            var infrastructureControlPlaneTests = EnumerateFiles("BlazorShop.Tests/Infrastructure/ControlPlane", "*.cs").ToArray();

            Assert.Contains(@"..\BlazorShop.Tests\Architecture\**\*.cs", v2TestProject, StringComparison.Ordinal);
            Assert.Contains(@"..\BlazorShop.Tests\Application\CommerceNode\**\*.cs", v2TestProject, StringComparison.Ordinal);
            Assert.Contains(@"..\BlazorShop.Tests\Infrastructure\CommerceNode\**\*.cs", v2TestProject, StringComparison.Ordinal);
            Assert.Contains(@"..\BlazorShop.Tests\Infrastructure\ControlPlane\**\*.cs", v2TestProject, StringComparison.Ordinal);
            Assert.Contains(@"..\BlazorShop.Tests\PresentationV2\**\*.cs", v2TestProject, StringComparison.Ordinal);
            Assert.True(applicationCommerceNodeTests.Length >= 10);
            Assert.True(infrastructureCommerceNodeTests.Length >= 20);
            Assert.True(infrastructureControlPlaneTests.Length >= 5);
            Assert.Contains(
                "BlazorShop.Tests/Application/CommerceNode/StorefrontCheckoutServiceTests.cs",
                applicationCommerceNodeTests.Select(ToRepositoryRelativePath));
            Assert.Contains(
                "BlazorShop.Tests/Infrastructure/CommerceNode/CommerceNodeDbContextModelTests.cs",
                infrastructureCommerceNodeTests.Select(ToRepositoryRelativePath));
            Assert.Contains(
                "BlazorShop.Tests/Infrastructure/ControlPlane/ControlPlaneDbContextModelTests.cs",
                infrastructureControlPlaneTests.Select(ToRepositoryRelativePath));
        }

        [Fact]
        public void Phase4_V2ProductionDockerfiles_ExistForActiveRuntime()
        {
            var commerceNodeDockerfile = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Dockerfile");
            var controlPlaneApiDockerfile = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/Dockerfile");
            var controlPlaneWebDockerfile = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Dockerfile");
            var controlPlaneWebEntrypoint = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/docker-entrypoint.d/10-controlplane-config.sh");
            var storefrontDockerfile = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Dockerfile");
            var serviceDefaults = ReadRepositoryFile("BlazorShop.ServiceDefaults/Extensions.cs");

            Assert.Contains("BlazorShop.CommerceNode.API.dll", commerceNodeDockerfile, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.ControlPlane.API.dll", controlPlaneApiDockerfile, StringComparison.Ordinal);
            Assert.Contains("COPY --from=build /app/publish/wwwroot /usr/share/nginx/html", controlPlaneWebDockerfile, StringComparison.Ordinal);
            Assert.Contains("CONTROLPLANE_API_BASE_URL", controlPlaneWebEntrypoint, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Storefront.Components.csproj", storefrontDockerfile, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Storefront.WASM.csproj", storefrontDockerfile, StringComparison.Ordinal);
            Assert.Contains("curl", commerceNodeDockerfile, StringComparison.Ordinal);
            Assert.Contains("curl", controlPlaneApiDockerfile, StringComparison.Ordinal);
            Assert.Contains("curl", storefrontDockerfile, StringComparison.Ordinal);
            Assert.Contains("Runtime:Health:ExposeInProduction", serviceDefaults, StringComparison.Ordinal);
        }

        [Fact]
        public void Phase4_V2ProductionCompose_TargetsActiveTopology()
        {
            var compose = ReadRepositoryFile("compose.v2.production.yml")
                .Replace('\\', '/');

            Assert.Contains("controlplane-postgres:", compose, StringComparison.Ordinal);
            Assert.Contains("commercenode-postgres:", compose, StringComparison.Ordinal);
            Assert.Contains("controlplane-api:", compose, StringComparison.Ordinal);
            Assert.Contains("controlplane-web:", compose, StringComparison.Ordinal);
            Assert.Contains("commercenode-api:", compose, StringComparison.Ordinal);
            Assert.Contains("commercenode-nginx:", compose, StringComparison.Ordinal);
            Assert.Contains("commercenode-imgproxy:", compose, StringComparison.Ordinal);
            Assert.Contains("storefront-v2:", compose, StringComparison.Ordinal);
            Assert.Contains("ConnectionStrings__ControlPlaneConnection", compose, StringComparison.Ordinal);
            Assert.Contains("ConnectionStrings__CommerceNodeConnection", compose, StringComparison.Ordinal);
            Assert.Contains("ControlPlane__Database__MigrateOnStartup", compose, StringComparison.Ordinal);
            Assert.Contains("CommerceNode__Database__MigrateOnStartup", compose, StringComparison.Ordinal);
            Assert.Contains("CommerceNode__DataProtection__KeyRingPath: /app/runtime/data-protection-keys", compose, StringComparison.Ordinal);
            Assert.Contains("commercenode_data_protection_keys:/app/runtime/data-protection-keys", compose, StringComparison.Ordinal);
            Assert.Contains("Runtime__Health__ExposeInProduction", compose, StringComparison.Ordinal);
            Assert.Contains("Api__StoreKey", compose, StringComparison.Ordinal);
            Assert.Contains("PublicUrl__BaseUrl", compose, StringComparison.Ordinal);
            Assert.Contains("StorefrontDeployment__AllowedImages__0: blazorshop-storefront-v2:latest", compose, StringComparison.Ordinal);
            Assert.DoesNotContain("ConnectionStrings__DefaultConnection", compose, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Presentation/", compose, StringComparison.Ordinal);
            Assert.DoesNotContain("SMTP", compose, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Phase4_CiWorkflow_BuildsV2ImagesAndValidatesV2Compose()
        {
            var workflow = ReadRepositoryFile(".github/workflows/ci.yml");

            Assert.Matches(
                new Regex("ci-v2:[\\s\\S]*docker compose -f compose\\.v2\\.production\\.yml config[\\s\\S]*BlazorShop\\.PresentationV2/BlazorShop\\.CommerceNode\\.API/Dockerfile[\\s\\S]*BlazorShop\\.PresentationV2/BlazorShop\\.ControlPlane\\.API/Dockerfile[\\s\\S]*BlazorShop\\.PresentationV2/BlazorShop\\.ControlPlane\\.Web/Dockerfile[\\s\\S]*BlazorShop\\.PresentationV2/BlazorShop\\.Storefront\\.V2/Dockerfile", RegexOptions.CultureInvariant),
                workflow);
            Assert.Contains("BLAZORSHOP_CONTROLPLANE_JWT_KEY", workflow, StringComparison.Ordinal);
            Assert.Contains("BLAZORSHOP_COMMERCENODE_NODE_SECRET", workflow, StringComparison.Ordinal);
            Assert.Contains("BLAZORSHOP_STOREFRONT_STORE_KEY", workflow, StringComparison.Ordinal);
        }

        [Fact]
        public void Phase6_StorefrontConcreteApiClientInjectionInventory_IsEmpty()
        {
            var concreteUsages = EnumerateFiles("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages", "*.razor")
                .Concat(EnumerateFiles("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components", "*.razor"))
                .Where(path =>
                {
                    var source = File.ReadAllText(path);
                    return source.Contains("@inject StorefrontApiClient", StringComparison.Ordinal)
                           || Regex.IsMatch(source, @"\bStorefrontApiClient\s+\w+", RegexOptions.CultureInvariant);
                })
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(concreteUsages);
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
        public void Phase5_OpenApiGeneratorGate_UsesPinnedNswagAndTypeScriptCompiler()
        {
            var storefrontOpenApiTests = ReadRepositoryFile("BlazorShop.Tests/PresentationV2/CommerceNode/CommerceNodeStorefrontOpenApiContractTests.cs");
            var toolManifest = ReadRepositoryFile(".config/dotnet-tools.json");
            var generatorPackage = ReadRepositoryFile("tools/openapi-generator-smoke/package.json");
            var workflow = ReadRepositoryFile(".github/workflows/ci.yml");

            Assert.Contains("StorefrontSwagger_GeneratesAndCompilesTypeScriptClientWithNswag", storefrontOpenApiTests, StringComparison.Ordinal);
            Assert.Contains("\"nswag.consolecore\"", toolManifest, StringComparison.Ordinal);
            Assert.Contains("\"version\": \"14.7.1\"", toolManifest, StringComparison.Ordinal);
            Assert.Contains("\"typescript\": \"7.0.2\"", generatorPackage, StringComparison.Ordinal);
            Assert.Contains("dotnet tool restore", workflow, StringComparison.Ordinal);
            Assert.Contains("npm ci --prefix tools/openapi-generator-smoke", workflow, StringComparison.Ordinal);
            Assert.Contains("tools/openapi-generator-smoke/package-lock.json", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("GenerateTypeScriptClient", storefrontOpenApiTests, StringComparison.Ordinal);
            Assert.DoesNotContain("Promise<unknown>", storefrontOpenApiTests, StringComparison.Ordinal);
            Assert.False(File.Exists(RepositoryPath("package.json")));
        }

        [Fact]
        public void Phase7_ReleaseSmokeScript_CoversProductionReadinessEndpoints()
        {
            var script = ReadRepositoryFile("scripts/qa/run-v2-production-release-smoke.ps1");
            var workflow = ReadRepositoryFile(".github/workflows/ci.yml");
            var commerceNodeQa = ReadRepositoryFile("docs/refactor-control-Commerce-storefront/QA-CommerceNode.todo.md");
            var controlPlaneQa = ReadRepositoryFile("docs/refactor-control-Commerce-storefront/QA-ControlPlane.todo.md");
            var storefrontQa = ReadRepositoryFile("docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md");
            var storefrontReleaseQa = ReadRepositoryFile("docs/refactor-control-Commerce-storefront/Storefront Playwright E2E Release.todo.md");

            Assert.Contains("ControlPlane API health", script, StringComparison.Ordinal);
            Assert.Contains("/health", script, StringComparison.Ordinal);
            Assert.Contains("ControlPlane Web root", script, StringComparison.Ordinal);
            Assert.Contains("CommerceNode API health", script, StringComparison.Ordinal);
            Assert.Contains("Storefront V2 health", script, StringComparison.Ordinal);
            Assert.Contains("/swagger/storefront/swagger.json", script, StringComparison.Ordinal);
            Assert.Contains("/swagger/commerce-admin/swagger.json", script, StringComparison.Ordinal);
            Assert.Contains("CommerceNode Nginx unknown host deny", script, StringComparison.Ordinal);
            Assert.Contains("unknown.invalid", script, StringComparison.Ordinal);
            Assert.Contains("ExpectedStatus = 403", script, StringComparison.Ordinal);
            Assert.Contains("run-v2-production-release-smoke.ps1 -Describe", workflow, StringComparison.Ordinal);
            Assert.Contains("## V2 Production Readiness Release Gate", commerceNodeQa, StringComparison.Ordinal);
            Assert.Contains("forged `X-Store-Host`", commerceNodeQa, StringComparison.Ordinal);
            Assert.Contains("Storefront OpenAPI generator artifact", commerceNodeQa, StringComparison.Ordinal);
            Assert.Contains("## V2 Production Readiness Release Gate", controlPlaneQa, StringComparison.Ordinal);
            Assert.Contains("ControlPlane Web direct-CommerceNode browser calls", controlPlaneQa, StringComparison.Ordinal);
            Assert.Contains("## V2 Production Readiness Release Gate", storefrontQa, StringComparison.Ordinal);
            Assert.Contains("Cart/account/checkout browser flows remain the production gate", storefrontQa, StringComparison.Ordinal);
            Assert.Contains("## V2 Production Readiness Final Release Checklist", storefrontReleaseQa, StringComparison.Ordinal);
            Assert.Contains("visible Playwright (`headless=false`)", storefrontReleaseQa, StringComparison.Ordinal);
            Assert.Contains("Checkout COD places exactly one real test-store order", storefrontReleaseQa, StringComparison.Ordinal);
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
