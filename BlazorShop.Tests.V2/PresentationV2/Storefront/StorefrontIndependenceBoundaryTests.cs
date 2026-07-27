namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using Xunit;

    public sealed class StorefrontIndependenceBoundaryTests
    {
        private static readonly string[] StorefrontProjectRoots =
        [
            "BlazorShop.PresentationV2/BlazorShop.Storefront.V2",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Client",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter"
        ];

        private static readonly string[] ForbiddenBackendProjectFragments =
        [
            "BlazorShop.ControlPlane.API.csproj",
            "BlazorShop.ControlPlane.Web.csproj",
            "BlazorShop.CommerceNode.API.csproj",
            "BlazorShop.Application.csproj",
            "BlazorShop.Domain.csproj",
            "BlazorShop.Infrastructure.csproj"
        ];

        private static readonly string[] ForbiddenBackendNamespaceFragments =
        [
            "BlazorShop.ControlPlane",
            "BlazorShop.CommerceNode.API",
            "BlazorShop.Application",
            "BlazorShop.Domain",
            "BlazorShop.Infrastructure"
        ];

        [Fact]
        public void StorefrontV2_DoesNotReferenceOrImportWebSharedV2()
        {
            var offenders = FindTextOffenders("BlazorShop.PresentationV2/BlazorShop.Storefront.V2", "BlazorShop.Web.SharedV2")
                .Concat(FindTextOffenders("BlazorShop.PresentationV2/BlazorShop.Storefront.V2", "BlazorShop.Web.SharedV2.csproj"))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.True(
                offenders.Length == 0,
                $"Storefront V2 must not reference or import Web.SharedV2:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
        }

        [Fact]
        public void StorefrontV2_DoesNotImportControlPlaneCommerceNodeOrBackendCore()
        {
            AssertNoProjectReferences(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj",
                ForbiddenBackendProjectFragments);

            AssertNoSourceFragments(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2",
                ForbiddenBackendNamespaceFragments);
        }

        [Fact]
        public void StorefrontPlatform_DoesNotImportWebSharedModelsOrApplicationDtos()
        {
            foreach (var root in StorefrontProjectRoots)
            {
                AssertNoSourceFragments(
                    root,
                    [
                        "BlazorShop.Web.SharedV2.Models",
                        "BlazorShop.Application.DTOs",
                        "BlazorShop.Application.CommerceNode",
                        "BlazorShop.Application.ControlPlane"
                    ]);
            }
        }

        [Fact]
        public void StorefrontV2LocalContracts_DoNotImportBackendOrSharedBusinessContracts()
        {
            AssertNoSourceFragments(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts",
                ForbiddenBackendNamespaceFragments
                    .Append("BlazorShop.Web.SharedV2")
                    .Append("BlazorShop.Web.SharedV2.Models")
                    .ToArray());
        }

        [Fact]
        public void StorefrontV2WASM_OnlyReferencesBrowserSafeStorefrontProjects()
        {
            var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj");

            Assert.Equal(
                ["../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj"],
                references);

            AssertNoSourceFragments(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM",
                ForbiddenBackendNamespaceFragments.Append("BlazorShop.Web.SharedV2").ToArray());
        }

        [Fact]
        public void StorefrontComponents_DoNotReferenceHostRuntimeClientOrBackendProjects()
        {
            AssertNoProjectReferences(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                ForbiddenBackendProjectFragments
                    .Append("BlazorShop.Storefront.Runtime.csproj")
                    .Append("BlazorShop.Storefront.Client.csproj")
                    .Append("BlazorShop.Storefront.V2.csproj")
                    .Append("BlazorShop.Web.SharedV2.csproj")
                    .ToArray());

            AssertNoSourceFragments(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components",
                ForbiddenBackendNamespaceFragments
                    .Append("BlazorShop.Web.SharedV2")
                    .Append("BlazorShop.Storefront.Runtime")
                    .Append("BlazorShop.Storefront.Client")
                    .Append("BlazorShop.Storefront.V2")
                    .ToArray());
        }

        [Fact]
        public void StorefrontComponents_FeatureModelsDoNotExposeServerOwnedFields()
        {
            Assert.False(Directory.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features")));
        }

        [Fact]
        public void StorefrontRuntime_OnlyReferencesGeneratedClientAndNoHostOrBackendProjects()
        {
            var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj");

            Assert.Equal(
                ["../BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj"],
                references);

            AssertNoSourceFragments(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime",
                ForbiddenBackendNamespaceFragments
                    .Append("BlazorShop.Web.SharedV2")
                    .Append("BlazorShop.Storefront.V2")
                    .Append("BlazorShop.Storefront.Components")
                    .Append("BlazorShop.Storefront.V2.WASM")
                    .ToArray());
        }

        [Fact]
        public void StorefrontRuntime_DoesNotContainV2PresentationRouteOrCookiePrimitives()
        {
            AssertNoSourceFragments(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime",
                [
                    "@page",
                    "@code",
                    ".razor",
                    "StorefrontRoutes",
                    "StorefrontCookieNames",
                    "BlazorShop.Storefront.Endpoints",
                    "Microsoft.AspNetCore.Components"
                ]);
        }

        [Fact]
        public void StorefrontClient_DoesNotReferenceHostRuntimeComponentsOrBackendProjects()
        {
            AssertNoProjectReferences(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj",
                ForbiddenBackendProjectFragments
                    .Append("BlazorShop.Storefront.Runtime.csproj")
                    .Append("BlazorShop.Storefront.Components.csproj")
                    .Append("BlazorShop.Storefront.V2.csproj")
                    .Append("BlazorShop.Web.SharedV2.csproj")
                    .ToArray());

            AssertNoSourceFragments(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Client",
                ForbiddenBackendNamespaceFragments
                    .Append("BlazorShop.Web.SharedV2")
                    .Append("BlazorShop.Storefront.Runtime")
                    .Append("BlazorShop.Storefront.Components")
                    .Append("BlazorShop.Storefront.V2")
                    .ToArray());
        }

        [Fact]
        public void StorefrontClient_DoesNotAddHandwrittenRequestResponseDtoClones()
        {
            var handwrittenDtoOffenders = EnumerateSourceFiles("BlazorShop.PresentationV2/BlazorShop.Storefront.Client")
                .Where(file => file.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .Where(file => !file.RelativePath.Contains("/Generated/", StringComparison.OrdinalIgnoreCase))
                .SelectMany(file =>
                {
                    var source = File.ReadAllText(file.AbsolutePath);
                    return Regex.Matches(source, @"\b(class|record)\s+\w*(Request|Response|Dto)\b", RegexOptions.CultureInvariant)
                        .Select(match => $"{file.RelativePath}: {match.Value}");
                })
                .ToArray();

            Assert.True(
                handwrittenDtoOffenders.Length == 0,
                $"Storefront.Client request/response DTOs must remain generated from OpenAPI:{Environment.NewLine}{string.Join(Environment.NewLine, handwrittenDtoOffenders)}");
        }

        [Fact(Skip = "SPF16/SPF22 transitional guardrail: Starter still uses a monorepo ProjectReference until dependency cleanup selects and proves the final package mode.")]
        public void StorefrontStarter_UsesPackageFirstContractsAndNoForbiddenSourceDependencies()
        {
            var project = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj");

            Assert.DoesNotContain("<ProjectReference", project, StringComparison.Ordinal);
            Assert.Contains("Include=\"BlazorShop.Storefront.Client\"", project, StringComparison.Ordinal);
            Assert.Contains("Include=\"BlazorShop.Storefront.Runtime\"", project, StringComparison.Ordinal);

            AssertNoSourceFragments(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter",
                ForbiddenBackendNamespaceFragments
                    .Append("BlazorShop.Web.SharedV2")
                    .Append("BlazorShop.Storefront.V2")
                    .ToArray());
        }

        [Fact]
        public void StorefrontHttpContractDependency_IsAllowedOnlyThroughRuntimeAndGeneratedClient()
        {
            var v2References = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj");
            var runtimeReferences = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj");

            Assert.Contains("../BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj", v2References);
            Assert.Contains("../BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj", runtimeReferences);
            Assert.DoesNotContain(v2References, reference => reference.Contains("BlazorShop.CommerceNode.API", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(v2References, reference => reference.Contains("BlazorShop.ControlPlane", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void StorefrontBrowserProjects_CallSameOriginBffOnly()
        {
            foreach (var browserRoot in new[]
            {
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components"
            })
            {
                AssertNoSourceFragments(
                    browserRoot,
                    [
                        "BlazorShop.Storefront.Client",
                        "api/storefront/stores",
                        "CommerceNodeBaseUrl",
                        "http://localhost:5180",
                        "https://localhost:5180",
                        "NodeSecret",
                        "NodeKey",
                        "accessToken",
                        "refreshToken"
                    ]);
            }
        }

        [Fact]
        public void StorefrontV2Host_DoesNotCallControlPlaneOrReadNodeCredentials()
        {
            AssertNoSourceFragments(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2",
                [
                    "api/control-plane",
                    "api/controlplane",
                    "BlazorShop.ControlPlane",
                    "ControlPlaneConnection",
                    "ControlPlane:",
                    "NodeSecret",
                    "NodeKey",
                    "X-Node-Key",
                    "X-Node-Secret"
                ]);
        }

        [Fact]
        public void StorefrontV2ManualClientExceptions_AreRegisteredWithOwnerTestAndRevisitTrigger()
        {
            var registry = ReadRepositoryFile("docs/storefront-platform/storefront-client-exception-registry.md");
            var serviceCollection = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontServiceCollectionExtensions.cs");
            var cartAdapter = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/GeneratedStorefrontCartClient.cs");
            var checkoutAdapter = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/GeneratedStorefrontCheckoutClient.cs");

            Assert.Contains("## Storefront V2", registry, StringComparison.Ordinal);
            Assert.Contains("StorefrontApiClient.MergeCurrentCustomerCartAsync", registry, StringComparison.Ordinal);
            Assert.Contains("StorefrontApiClient.UpdateCheckoutAddressesAsync", registry, StringComparison.Ordinal);
            Assert.Contains("IStorefrontCustomerClient", registry, StringComparison.Ordinal);
            Assert.Contains("StorefrontAuthClient", registry, StringComparison.Ordinal);
            Assert.Contains("Owner", registry, StringComparison.Ordinal);
            Assert.Contains("Test", registry, StringComparison.Ordinal);
            Assert.Contains("Revisit trigger", registry, StringComparison.Ordinal);

            Assert.Contains("AddHttpClient<StorefrontApiClient>", serviceCollection, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IStorefrontCustomerClient>(serviceProvider => serviceProvider.GetRequiredService<StorefrontApiClient>())", serviceCollection, StringComparison.Ordinal);
            Assert.Contains("MergeCurrentCustomerCartAsync", cartAdapter, StringComparison.Ordinal);
            Assert.Contains("UpdateCheckoutAddressesAsync", checkoutAdapter, StringComparison.Ordinal);
        }

        private static void AssertNoProjectReferences(string relativeProjectPath, IReadOnlyCollection<string> forbiddenFragments)
        {
            var references = ReadProjectReferences(relativeProjectPath);
            var offenders = references
                .Where(reference => forbiddenFragments.Any(fragment => reference.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            Assert.True(
                offenders.Length == 0,
                $"Forbidden ProjectReference in {relativeProjectPath}:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
        }

        private static void AssertNoSourceFragments(string relativeDirectory, IReadOnlyCollection<string> forbiddenFragments)
        {
            var offenders = EnumerateSourceFiles(relativeDirectory)
                .SelectMany(file => forbiddenFragments
                    .Where(fragment => File.ReadAllText(file.AbsolutePath).Contains(fragment, StringComparison.Ordinal))
                    .Select(fragment => $"{file.RelativePath}: {fragment}"))
                .ToArray();

            Assert.True(
                offenders.Length == 0,
                $"Forbidden Storefront source dependency found:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
        }

        private static IReadOnlyList<string> FindTextOffenders(string relativeDirectory, string text)
        {
            return EnumerateSourceFiles(relativeDirectory)
                .Where(file => File.ReadAllText(file.AbsolutePath).Contains(text, StringComparison.Ordinal))
                .Select(file => file.RelativePath)
                .ToArray();
        }

        private static IReadOnlyList<string> ReadProjectReferences(string relativeProjectPath)
        {
            var document = XDocument.Load(RepositoryPath(relativeProjectPath));
            return document
                .Descendants("ProjectReference")
                .Select(element => NormalizePath(element.Attribute("Include")?.Value ?? string.Empty))
                .Order(StringComparer.Ordinal)
                .ToArray();
        }

        private static IEnumerable<SourceFile> EnumerateSourceFiles(string relativeDirectory)
        {
            var root = RepositoryPath(relativeDirectory);
            return Directory
                .EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Path.GetFileName(path), "Dockerfile", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Select(path => new SourceFile(path, NormalizePath(Path.GetRelativePath(RepositoryRoot(), path))));
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(RepositoryPath(relativePath));
        }

        private static string RepositoryPath(string relativePath)
        {
            return Path.Combine(RepositoryRoot(), relativePath);
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln"))
                    && File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate repository root.");
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        private sealed record SourceFile(string AbsolutePath, string RelativePath);
    }
}
