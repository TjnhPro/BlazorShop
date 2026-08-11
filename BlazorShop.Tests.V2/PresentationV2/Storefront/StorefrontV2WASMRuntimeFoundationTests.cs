namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontV2WASMRuntimeFoundationTests
    {
        [Fact]
        public void WasmStartup_RegistersSameOriginClientWithoutCommerceNodeConfiguration()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Program.cs");

            Assert.Contains("AddStorefrontBrowserRuntime(builder.HostEnvironment)", program, StringComparison.Ordinal);
            Assert.DoesNotContain("new HttpClient", program, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontLocalApiClient", program, StringComparison.Ordinal);
            Assert.DoesNotContain("IStorefrontAntiforgeryTokenReader", program, StringComparison.Ordinal);
            Assert.DoesNotContain("CommerceNode", program, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("NodeKey", program, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("NodeSecret", program, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("accessToken", program, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void WasmProject_DoesNotReferenceServerRuntimeOrGeneratedStorefrontClient()
        {
            var root = RepositoryRoot();
            var project = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj");
            var source = string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(
                        Path.Combine(root, "BlazorShop.PresentationV2", "BlazorShop.Storefront.V2.WASM"),
                        "*.*",
                        SearchOption.AllDirectories)
                    .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                    .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                            || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));

            Assert.DoesNotContain("BlazorShop.Storefront.Runtime", project, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.Client", project, StringComparison.Ordinal);
            Assert.DoesNotContain("CommerceNodeBaseUrl", source, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontRuntimeOptions", source, StringComparison.Ordinal);
            Assert.DoesNotContain("using BlazorShop.Storefront.Runtime", source, StringComparison.Ordinal);
        }

        [Fact]
        public void WasmProjectIdentity_IsExplicitlyScopedToStorefrontV2()
        {
            var solution = ReadRepositoryFile("BlazorShop.sln");
            var hostProject = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj");
            var testsProject = ReadRepositoryFile("BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj");
            var wasmProject = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj");

            Assert.Contains("BlazorShop.Storefront.V2.WASM", solution, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Storefront.V2.WASM.csproj", hostProject, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Storefront.V2.WASM.csproj", testsProject, StringComparison.Ordinal);
            Assert.Contains("<RootNamespace>BlazorShop.Storefront.V2.WASM</RootNamespace>", wasmProject, StringComparison.Ordinal);
            Assert.False(File.Exists(ResolveRepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj")));
            Assert.DoesNotContain("BlazorShop.Storefront.WASM", solution, StringComparison.Ordinal);
        }

        [Fact]
        public void WasmTailwindPipeline_OwnsInteractiveCssWithoutScanningOtherProjects()
        {
            var package = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/package.json");
            var tailwindConfig = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/tailwind.config.js");
            var project = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj");
            var css = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/wwwroot/css/wasm-site.css");

            Assert.Contains("\"tailwind:build\": \"tailwindcss -c tailwind.config.js -i ./wwwroot/css/input.css -o ./wwwroot/css/wasm-site.css --minify\"", package, StringComparison.Ordinal);
            Assert.Contains("\"./**/*.razor\"", tailwindConfig, StringComparison.Ordinal);
            Assert.DoesNotContain("../BlazorShop.Storefront.V2", tailwindConfig, StringComparison.Ordinal);
            Assert.DoesNotContain("artifacts/storefront-builder", tailwindConfig, StringComparison.Ordinal);
            Assert.Contains("<Content Remove=\"wwwroot\\css\\input.css\" />", project, StringComparison.Ordinal);
            Assert.False(File.Exists(ResolveRepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/wwwroot/css/site.css")));
            Assert.True(new FileInfo(ResolveRepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/wwwroot/css/wasm-site.css")).Length > 1024);
            Assert.Contains(".rounded-3xl", css, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontProgram_DelegatesLocalBrowserApiMappingToEndpointExtensions()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");
            var applicationBuilder = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontApplicationBuilderExtensions.cs");
            var presentationAggregation = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontPresentationApplicationBuilderExtensions.cs");

            Assert.Contains("app.UseStorefrontApplication();", program, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontApplication(", program, StringComparison.Ordinal);
            Assert.Contains("app.UseStorefrontPresentation();", applicationBuilder, StringComparison.Ordinal);
            Assert.Contains("app.MapStorefrontPresentation();", applicationBuilder, StringComparison.Ordinal);
            Assert.Contains("endpoints.MapStorefrontPresentationCartEndpoints();", presentationAggregation, StringComparison.Ordinal);
            Assert.Contains("endpoints.MapStorefrontPresentationAccountEndpoints();", presentationAggregation, StringComparison.Ordinal);
            Assert.Contains("endpoints.MapStorefrontPresentationCheckoutEndpoints();", presentationAggregation, StringComparison.Ordinal);
            Assert.Contains("app.MapStaticAssets();", applicationBuilder, StringComparison.Ordinal);
            Assert.Contains("components.AddInteractiveWebAssemblyRenderMode();", applicationBuilder, StringComparison.Ordinal);

            Assert.DoesNotContain("app.MapGet(\"/api/cart\"", program, StringComparison.Ordinal);
            Assert.DoesNotContain("app.MapGet(\"/api/account/profile\"", program, StringComparison.Ordinal);
            Assert.DoesNotContain("app.MapGet(\"/api/checkout\"", program, StringComparison.Ordinal);
            Assert.DoesNotContain("ProxyCommerceNodeMediaAsync", program, StringComparison.Ordinal);
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(ResolveRepositoryPath(relativePath));
        }

        private static string ResolveRepositoryPath(string relativePath)
        {
            return Path.Combine(RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string RepositoryRoot()
        {
            var current = AppContext.BaseDirectory;
            while (!string.IsNullOrWhiteSpace(current))
            {
                if (File.Exists(Path.Combine(current, "BlazorShop.sln")))
                {
                    return current;
                }

                current = Directory.GetParent(current)?.FullName;
            }

            throw new DirectoryNotFoundException("Could not find repository root.");
        }
    }
}
