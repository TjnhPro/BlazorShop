namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Xml.Linq;

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
        public void StorefrontRuntime_SeparatesCoreRuntimeFromServerGeneratedClientRegistration()
        {
            var runtimeRegistration = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeServiceCollectionExtensions.cs");
            var v2Registration = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontServiceCollectionExtensions.cs");

            Assert.Contains("AddStorefrontRuntime(", runtimeRegistration, StringComparison.Ordinal);
            Assert.Contains("AddStorefrontServerGeneratedClients(", runtimeRegistration, StringComparison.Ordinal);
            Assert.Contains("AddStorefrontServerGeneratedClients", v2Registration, StringComparison.Ordinal);
            Assert.DoesNotContain(".AddStorefrontGeneratedClients", v2Registration, StringComparison.Ordinal);
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
