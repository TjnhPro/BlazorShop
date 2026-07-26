namespace BlazorShop.Tests.Architecture
{
    using System.Xml.Linq;

    using BlazorShop.Storefront.Presentation.Views.Foundation;

    using Microsoft.AspNetCore.Components;

    using Xunit;

    public sealed class StorefrontPresentationFoundationBoundaryTests
    {
        [Fact]
        public void PresentationProject_ReferencesOnlyRuntimeAndComponents()
        {
            var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj");

            Assert.Equal(
                [
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj",
                ],
                references);
        }

        [Fact]
        public void PresentationProject_DoesNotReferenceHostOrBackendProjects()
        {
            var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj");
            var forbidden = references
                .Where(reference =>
                    reference.Contains("/BlazorShop.Storefront.V2/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.Storefront.V2.WASM/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.Storefront.Starter/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.ServiceDefaults/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.Application/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.Domain/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.Infrastructure/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.CommerceNode.API/", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("/BlazorShop.ControlPlane.API/", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.Empty(forbidden);
        }

        [Fact]
        public void BrowserAndComponentProjects_DoNotDependOnPresentationRuntimeOrClient()
        {
            var wasmReferences = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj");
            var componentReferences = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj");

            Assert.DoesNotContain(wasmReferences, IsPresentationRuntimeOrClientReference);
            Assert.DoesNotContain(componentReferences, IsPresentationRuntimeOrClientReference);
        }

        [Fact]
        public void ComponentsProject_RemainsLogicOnlyWithoutRazorFiles()
        {
            var componentRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components");
            var razorFiles = Directory.EnumerateFiles(componentRoot, "*.razor", SearchOption.AllDirectories)
                .Select(ToRepositoryRelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(razorFiles);
        }

        [Fact]
        public void PresentationProject_IsInSolutionAndHasFoundationFolders()
        {
            var solution = ReadRepositoryFile("BlazorShop.sln");
            Assert.Contains(
                "BlazorShop.PresentationV2\\BlazorShop.Storefront.Presentation\\BlazorShop.Storefront.Presentation.csproj",
                solution,
                StringComparison.Ordinal);

            var expectedFolders = new[]
            {
                "App",
                "Routing",
                "Pages",
                "PagePatterns",
                "Services",
                "Seo",
                "Endpoints",
                "Security",
                "Hosting",
                "Views/Foundation",
                "DependencyInjection",
            };

            foreach (var expectedFolder in expectedFolders)
            {
                Assert.True(
                    Directory.Exists(RepositoryPath($"BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/{expectedFolder}")),
                    $"{expectedFolder} must exist in the Presentation foundation project.");
            }
        }

        [Fact]
        public void FoundationViewOptionsValidator_FailsWhenRequiredSlotIsMissing()
        {
            var viewSet = StorefrontFoundationViewSet.CreateMinimal(typeof(ValidFoundationView));
            viewSet = new StorefrontFoundationViewSet
            {
                ApplicationHead = viewSet.ApplicationHead,
                ApplicationScripts = viewSet.ApplicationScripts,
                MainLayout = viewSet.MainLayout,
                HomePage = viewSet.HomePage,
                CategoryPage = viewSet.CategoryPage,
                ProductPage = null!,
                SearchPage = viewSet.SearchPage,
                DealsPage = viewSet.DealsPage,
                NewReleasesPage = viewSet.NewReleasesPage,
                ContentPage = viewSet.ContentPage,
                CartPage = viewSet.CartPage,
                CheckoutPage = viewSet.CheckoutPage,
                PaymentResultPage = viewSet.PaymentResultPage,
                AuthPage = viewSet.AuthPage,
                AccountPage = viewSet.AccountPage,
                MaintenanceState = viewSet.MaintenanceState,
                NotFoundState = viewSet.NotFoundState,
                ServiceUnavailableState = viewSet.ServiceUnavailableState,
                ErrorState = viewSet.ErrorState,
            };

            var result = new StorefrontFoundationViewOptionsValidator()
                .Validate(null, new StorefrontFoundationViewOptions { ViewSet = viewSet });

            Assert.True(result.Failed);
            Assert.Contains(result.Failures, failure => failure.Contains("ProductPage", StringComparison.Ordinal));
        }

        [Fact]
        public void FoundationViewTypeValidator_RequiresContextParameterWhenContextIsProvided()
        {
            var context = new FoundationContext("demo");

            StorefrontFoundationViewTypeValidator.Validate(typeof(ValidFoundationView), context);

            Assert.Throws<InvalidOperationException>(() =>
                StorefrontFoundationViewTypeValidator.Validate(typeof(MissingContextFoundationView), context));
            Assert.Throws<InvalidOperationException>(() =>
                StorefrontFoundationViewTypeValidator.Validate(typeof(WrongContextFoundationView), context));
        }

        [Fact]
        public void V2AndStarter_RegisterFoundationViewSetsWithoutOwningPresentation()
        {
            var v2References = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj");
            var starterReferences = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj");

            Assert.Contains(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
                v2References);
            Assert.Contains(
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj",
                starterReferences);

            Assert.Contains(
                "AddV2FoundationViews",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs"),
                StringComparison.Ordinal);
            Assert.Contains(
                "StorefrontFoundationViewSet.CreateMinimal",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/V2FoundationViewRegistration.cs"),
                StringComparison.Ordinal);
            Assert.Contains(
                "AddStarterFoundationViews",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Program.cs"),
                StringComparison.Ordinal);
            Assert.Contains(
                "StorefrontFoundationViewSet.CreateMinimal",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/StarterFoundationViewRegistration.cs"),
                StringComparison.Ordinal);
            Assert.Contains(
                "MapRazorComponents<StorefrontApp>()",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs"),
                StringComparison.Ordinal);
            Assert.Contains(
                "MapRazorComponents<StorefrontApp>()",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Program.cs"),
                StringComparison.Ordinal);
            Assert.Contains(
                "AdditionalAssemblies",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontRoutes.razor"),
                StringComparison.Ordinal);
            Assert.Contains(
                "DefaultLayout=\"@ViewSet.MainLayout\"",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontRoutes.razor"),
                StringComparison.Ordinal);
            Assert.Contains(
                "StorefrontFoundationViewOutlet",
                ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontApp.razor"),
                StringComparison.Ordinal);
        }

        [Fact]
        public void V2AndStarter_RetireOldAppAndRoutesRootFiles()
        {
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/App.razor")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Routes.razor")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Components/App.razor")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Components/Routes.razor")));
            Assert.True(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontApp.razor")));
            Assert.True(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontRoutes.razor")));
        }

        private static bool IsPresentationRuntimeOrClientReference(string reference)
        {
            return reference.Contains("/BlazorShop.Storefront.Presentation/", StringComparison.OrdinalIgnoreCase)
                || reference.Contains("/BlazorShop.Storefront.Runtime/", StringComparison.OrdinalIgnoreCase)
                || reference.Contains("/BlazorShop.Storefront.Client/", StringComparison.OrdinalIgnoreCase);
        }

        private static IReadOnlyList<string> ReadProjectReferences(string relativeProjectPath)
        {
            var projectPath = RepositoryPath(relativeProjectPath);
            var projectDirectory = Path.GetDirectoryName(projectPath)
                ?? throw new DirectoryNotFoundException($"Could not resolve project directory for {relativeProjectPath}.");
            var document = XDocument.Load(projectPath);

            return document.Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => Path.GetFullPath(Path.Combine(projectDirectory, value!)))
                .Select(path => Path.GetRelativePath(FindRepositoryRoot(), path).Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(RepositoryPath(relativePath));
        }

        private static string RepositoryPath(string relativePath)
        {
            return Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string ToRepositoryRelativePath(string path)
        {
            return Path.GetRelativePath(FindRepositoryRoot(), path).Replace('\\', '/');
        }

        private static string FindRepositoryRoot()
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

            throw new DirectoryNotFoundException("Could not locate BlazorShop.sln.");
        }

        private sealed record FoundationContext(string Name);

        private sealed class ValidFoundationView : IComponent
        {
            [Parameter]
            public FoundationContext Context { get; set; } = default!;

            public void Attach(RenderHandle renderHandle)
            {
            }

            public Task SetParametersAsync(ParameterView parameters)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class MissingContextFoundationView : IComponent
        {
            public void Attach(RenderHandle renderHandle)
            {
            }

            public Task SetParametersAsync(ParameterView parameters)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class WrongContextFoundationView : IComponent
        {
            [Parameter]
            public string Context { get; set; } = string.Empty;

            public void Attach(RenderHandle renderHandle)
            {
            }

            public Task SetParametersAsync(ParameterView parameters)
            {
                return Task.CompletedTask;
            }
        }
    }
}
