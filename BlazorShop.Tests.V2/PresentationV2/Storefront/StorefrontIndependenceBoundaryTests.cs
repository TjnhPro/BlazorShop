namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Xml.Linq;
    using Xunit;

    public sealed class StorefrontIndependenceBoundaryTests
    {
        private static readonly string[] StorefrontProjectRoots =
        [
            "BlazorShop.PresentationV2/BlazorShop.Storefront.V2",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.WASM",
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
        public void StorefrontV2_WebSharedV2OffendersAreLimitedUntilSib3()
        {
            var offenders = FindTextOffenders("BlazorShop.PresentationV2/BlazorShop.Storefront.V2", "BlazorShop.Web.SharedV2")
                .Concat(FindTextOffenders("BlazorShop.PresentationV2/BlazorShop.Storefront.V2", "BlazorShop.Web.SharedV2.csproj"))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();

            var expected = new[]
            {
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontRateLimitIdentity.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Dockerfile",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontAccountEndpoints.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontAuthFormEndpoints.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontCartEndpoints.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontCheckoutEndpoints.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontConsentEndpoints.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontLocalEndpointSupport.Account.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontLocalEndpointSupport.Cart.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontLocalEndpointSupport.Checkout.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontLocalEndpointSupport.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontMediaEndpoints.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontSeoEndpoints.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontCartTokenService.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontDisplayContextProvider.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontSessionResolver.cs",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/tailwind.config.js"
            };

            Assert.Empty(offenders.Except(expected, StringComparer.Ordinal));
            Assert.Empty(expected.Except(offenders, StringComparer.Ordinal));
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
        public void StorefrontWasm_OnlyReferencesBrowserSafeStorefrontProjects()
        {
            var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj");

            Assert.Equal(
                ["../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj"],
                references);

            AssertNoSourceFragments(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.WASM",
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
                    .Append("BlazorShop.Storefront.WASM")
                    .ToArray());
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
