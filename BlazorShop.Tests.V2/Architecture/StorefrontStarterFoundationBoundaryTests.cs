namespace BlazorShop.Tests.Architecture
{
    using System.Xml.Linq;
    using System.Text;

    using BlazorShop.Storefront.Client;
    using BlazorShop.Storefront.Runtime;

    using Xunit;

    [Collection("V2 serial host and process tests")]
    public sealed class StorefrontStarterFoundationBoundaryTests
    {
        private static readonly string[] StarterProjectPaths =
        [
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj",
        ];

        [Fact]
        public void StarterArchitectureRoles_AreDocumented()
        {
            var adr = ReadRepositoryFile("docs/architecture/adr/2026-07-24-storefront-starter-foundation.md");
            var systemMap = ReadRepositoryFile("docs/architecture/01-system-map.md");
            var folderGuide = ReadRepositoryFile("docs/architecture/05-project-and-folder-guide.md");
            var contractOwnership = ReadRepositoryFile("docs/architecture/10-v2-contract-ownership.md");
            var cleanupPlan = ReadRepositoryFile("docs/visual-reverse-engineering-skill/04-StorefrontBuilder-Generated-Store-Cleanup.todo.md");

            Assert.Contains("Storefront.Starter` is the neutral skeleton source", adr, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Storefront.GeneratedProof", cleanupPlan, StringComparison.Ordinal);
            Assert.Contains("Storefront.V2` remains the real storefront implementation and behavior reference", adr, StringComparison.Ordinal);
            Assert.Contains("manual `StorefrontApiClient` transport from Storefront V2", adr, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter", systemMap, StringComparison.Ordinal);
            Assert.Contains("Future `BlazorShop.Storefront.Starter`", folderGuide, StringComparison.Ordinal);
            Assert.Contains("generated storefront manifests", folderGuide, StringComparison.Ordinal);
            Assert.Contains("StorefrontStarterFoundationBoundaryTests", contractOwnership, StringComparison.Ordinal);
        }

        [Fact]
        public void StarterProtectedAreas_AreDocumented()
        {
            var adr = ReadRepositoryFile("docs/architecture/adr/2026-07-24-storefront-starter-foundation.md");
            var folderGuide = ReadRepositoryFile("docs/architecture/05-project-and-folder-guide.md");

            foreach (var expected in new[]
            {
                "generated client source",
                "runtime security primitives",
                "BFF transport/security code",
                "package/version manifests",
                "generated storefront manifests",
            })
            {
                Assert.Contains(expected, adr, StringComparison.Ordinal);
                Assert.Contains(expected, folderGuide, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void StarterAndGeneratedStorefrontConsumerRules_AreDocumented()
        {
            var architecture = ReadRepositoryFile("docs/architecture/11-storefront-builder.md");
            var folderGuide = ReadRepositoryFile("docs/architecture/05-project-and-folder-guide.md");
            var reference = ReadRepositoryFile("docs/visual-reverse-engineering-skill/reference.md");
            var howTo = ReadRepositoryFile("docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md");
            var agentGuide = ReadRepositoryFile("docs/agents/storefront-builder.md");
            var combined = string.Join(Environment.NewLine, architecture, folderGuide, reference, howTo, agentGuide);

            foreach (var expected in new[]
            {
                "Runtime owns direct `BlazorShop.Storefront.Client` transport usage",
                "Presentation composes Runtime internally",
                "BlazorShop.Storefront.Components",
                "same-origin BFF endpoints",
                "Do not reference `BlazorShop.Storefront.V2`",
                "Do not reference backend/API/core projects",
                "BlazorShop.Web.SharedV2",
                "Web.SharedV2",
                "Use generated package contracts instead of guessing API response shapes",
                "presentation-specific CSS, assets, generated pages",
            })
            {
                Assert.Contains(expected, combined, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void StarterProjects_DoNotReferenceForbiddenProjects()
        {
            foreach (var relativeProjectPath in StarterProjectPaths)
            {
                if (!File.Exists(RepositoryPath(relativeProjectPath)))
                {
                    continue;
                }

                var references = ReadProjectReferences(relativeProjectPath);
                var offenders = references
                    .Where(IsForbiddenStarterProjectReference)
                    .OrderBy(reference => reference, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                Assert.Empty(offenders);
            }
        }

        [Fact]
        public void StarterSource_DoesNotUseWebSharedV2OrCopyManualTransport()
        {
            var sourceRoots = new[]
            {
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter",
            };

            var violations = sourceRoots
                .Select(RepositoryPath)
                .Where(Directory.Exists)
                .SelectMany(root => Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
                .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                        || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)))
                .Select(path => new
                {
                    RelativePath = ToRepositoryRelativePath(path),
                    Source = File.ReadAllText(path),
                })
                .Where(file => file.Source.Contains("BlazorShop.Web.SharedV2", StringComparison.Ordinal)
                    || file.Source.Contains("Web.SharedV2", StringComparison.Ordinal)
                    || file.Source.Contains("StorefrontApiClient", StringComparison.Ordinal)
                    || file.Source.Contains("Generated/StorefrontClient.g.cs", StringComparison.Ordinal))
                .Select(file => file.RelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(violations);
        }

        [Fact]
        public void StarterProject_ConsumesPresentationAndComponentsWithoutDirectRuntimeOrClient()
        {
            var project = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj");
            var versionProps = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/StorefrontPackageVersions.props");
            var nugetConfig = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/nuget.config");
            var compatibility = ReadRepositoryFile("docs/storefront-platform/storefront-package-compatibility.md");
            var changelog = ReadRepositoryFile("docs/storefront-platform/storefront-client-changelog.md");

            Assert.DoesNotContain("<PackageReference Include=\"BlazorShop.Storefront.Client\"", project, StringComparison.Ordinal);
            Assert.DoesNotContain("<PackageReference Include=\"BlazorShop.Storefront.Runtime\"", project, StringComparison.Ordinal);
            Assert.Contains("<PackageReference Include=\"BlazorShop.Storefront.Components\" Version=\"$(StorefrontComponentsPackageVersion)\"", project, StringComparison.Ordinal);
            Assert.Contains(@"<ProjectReference Include=""..\BlazorShop.Storefront.Presentation\BlazorShop.Storefront.Presentation.csproj""", project, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.Client.csproj", project, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.Runtime.csproj", project, StringComparison.Ordinal);
            Assert.Contains("<StorefrontClientPackageVersion>1.0.0-local</StorefrontClientPackageVersion>", versionProps, StringComparison.Ordinal);
            Assert.Contains("<StorefrontRuntimePackageVersion>1.0.0-local</StorefrontRuntimePackageVersion>", versionProps, StringComparison.Ordinal);
            Assert.Contains("<StorefrontPresentationPackageVersion>1.0.0-local</StorefrontPresentationPackageVersion>", versionProps, StringComparison.Ordinal);
            Assert.Contains("<StorefrontComponentsPackageVersion>1.0.0-local</StorefrontComponentsPackageVersion>", versionProps, StringComparison.Ordinal);
            Assert.Contains("<StorefrontBrowserPackageVersion>1.0.0-local</StorefrontBrowserPackageVersion>", versionProps, StringComparison.Ordinal);
            Assert.Contains("local-storefront-packages", nugetConfig, StringComparison.Ordinal);
            Assert.Contains("| v1 | 1.x | compatible |", compatibility, StringComparison.Ordinal);
            Assert.Contains("| 1.x | 1.x |", compatibility, StringComparison.Ordinal);
            Assert.Contains("1.0.0-local", changelog, StringComparison.Ordinal);
        }

        [Fact]
        public void StarterWasmProject_IsNeutralBrowserRuntimeTemplate()
        {
            var project = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/BlazorShop.Storefront.Starter.WASM.csproj");
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/Program.cs");
            var imports = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/_Imports.razor");
            var accountHost = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/Components/Account/StorefrontAccountApp.razor");
            var cartHost = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/Components/Cart/StorefrontCartApp.razor");
            var checkoutHost = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/Components/Checkout/StorefrontCheckoutApp.razor");
            var source = string.Concat(project, program, imports, accountHost, cartHost, checkoutHost);

            Assert.Contains("Microsoft.NET.Sdk.BlazorWebAssembly", project, StringComparison.Ordinal);
            Assert.Contains("<TargetFramework>net10.0</TargetFramework>", project, StringComparison.Ordinal);
            Assert.Contains("<NoDefaultLaunchSettingsFile>true</NoDefaultLaunchSettingsFile>", project, StringComparison.Ordinal);
            Assert.Contains("<StaticWebAssetProjectMode>Default</StaticWebAssetProjectMode>", project, StringComparison.Ordinal);
            Assert.Contains(@"<RootNamespace>BlazorShop.Storefront.Starter.WASM</RootNamespace>", project, StringComparison.Ordinal);
            Assert.Contains(@"<PackageReference Include=""Microsoft.AspNetCore.Components.WebAssembly""", project, StringComparison.Ordinal);
            Assert.Contains(@"<ProjectReference Include=""..\BlazorShop.Storefront.Browser\BlazorShop.Storefront.Browser.csproj""", project, StringComparison.Ordinal);
            Assert.Contains(@"<ProjectReference Include=""..\BlazorShop.Storefront.Components\BlazorShop.Storefront.Components.csproj""", project, StringComparison.Ordinal);
            Assert.Contains("WebAssemblyHostBuilder.CreateDefault(args)", program, StringComparison.Ordinal);
            Assert.Contains("AddStorefrontBrowserRuntime(builder.HostEnvironment)", program, StringComparison.Ordinal);
            Assert.Contains("@namespace BlazorShop.Storefront.Starter.WASM", imports, StringComparison.Ordinal);
            Assert.Contains("IStorefrontBrowserAccountController", accountHost, StringComparison.Ordinal);
            Assert.Contains("IStorefrontBrowserCartController", cartHost, StringComparison.Ordinal);
            Assert.Contains("IStorefrontBrowserCheckoutController", checkoutHost, StringComparison.Ordinal);
            Assert.DoesNotContain("@page", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.V2", source, StringComparison.Ordinal);
            Assert.DoesNotContain("CommerceNode", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ControlPlane", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.Runtime", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.Client", source, StringComparison.Ordinal);
        }

        [Fact]
        public void StarterProject_RestoresAndBuildsFromLocalStorefrontPackages()
        {
            var repositoryRoot = FindRepositoryRoot();
            var packageFeed = RepositoryPath("artifacts/storefront-packages");
            var packageCache = RepositoryPath("obj/storefront-starter-foundation-boundary/nuget-packages");
            var starterProject = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj");
            var presentationProject = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj");
            var componentsProject = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj");

            if (Directory.Exists(packageFeed))
            {
                Directory.Delete(packageFeed, recursive: true);
            }

            if (Directory.Exists(packageCache))
            {
                Directory.Delete(packageCache, recursive: true);
            }

            Directory.CreateDirectory(packageFeed);
            Directory.CreateDirectory(packageCache);

            var packResult = RunProcess(
                "dotnet",
                [
                    "pack",
                    RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj"),
                    "--output",
                    packageFeed,
                    "/p:PackageVersion=1.0.0-local",
                ],
                repositoryRoot);

            Assert.True(packResult.ExitCode == 0, FormatProcessFailure("Storefront client package did not pack.", packResult));

            var runtimePackResult = RunProcess(
                "dotnet",
                [
                    "pack",
                    RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj"),
                    "--output",
                    packageFeed,
                    "/p:PackageVersion=1.0.0-local",
                ],
                repositoryRoot);

            Assert.True(runtimePackResult.ExitCode == 0, FormatProcessFailure("Storefront runtime package did not pack.", runtimePackResult));

            var presentationPackResult = RunProcess(
                "dotnet",
                [
                    "pack",
                    presentationProject,
                    "--output",
                    packageFeed,
                    "/p:PackageVersion=1.0.0-local",
                ],
                repositoryRoot);

            Assert.True(presentationPackResult.ExitCode == 0, FormatProcessFailure("Storefront presentation package did not pack.", presentationPackResult));

            var componentsPackResult = RunProcess(
                "dotnet",
                [
                    "pack",
                    componentsProject,
                    "--output",
                    packageFeed,
                    "/p:PackageVersion=1.0.0-local",
                ],
                repositoryRoot);

            Assert.True(componentsPackResult.ExitCode == 0, FormatProcessFailure("Storefront components package did not pack.", componentsPackResult));

            var restoreResult = RunProcess(
                "dotnet",
                [
                    "restore",
                    starterProject,
                    "--no-cache",
                    "--force-evaluate",
                    $"/p:RestorePackagesPath={packageCache}",
                ],
                repositoryRoot);
            Assert.True(restoreResult.ExitCode == 0, FormatProcessFailure("Starter did not restore from the local Storefront client package.", restoreResult));

            var buildResult = RunProcess("dotnet", ["build", starterProject, "--no-restore"], repositoryRoot);
            Assert.True(buildResult.ExitCode == 0, FormatProcessFailure("Starter did not build after package restore.", buildResult));
        }

        [Fact]
        public void RuntimeProject_ContainsOnlyNeutralPackageDependencies()
        {
            var project = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj");
            var references = ReadProjectReferences("BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj");
            var forbidden = references
                .Where(IsForbiddenStarterProjectReference)
                .OrderBy(reference => reference, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Assert.Empty(forbidden);
            Assert.Contains("<PackageId>BlazorShop.Storefront.Runtime</PackageId>", project, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Storefront.Client.csproj", project, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.V2", project, StringComparison.Ordinal);
        }

        [Fact]
        public void RuntimeCapabilityReader_CombinesSupportedEnabledAndReason()
        {
            var reader = new StorefrontCapabilityReader();
            var capabilities = new Dictionary<string, StorefrontRuntimeCapability>(StringComparer.Ordinal)
            {
                ["cart"] = new(Supported: true, Enabled: true, Reason: null),
                ["reviews"] = new(Supported: false, Enabled: false, Reason: "not_installed"),
                ["newsletter"] = new(Supported: true, Enabled: false, Reason: "disabled"),
            };

            Assert.True(reader.IsSupported(capabilities, "cart"));
            Assert.True(reader.IsEnabled(capabilities, "cart"));
            Assert.True(reader.IsSupported(capabilities, "newsletter"));
            Assert.False(reader.IsEnabled(capabilities, "newsletter"));
            Assert.False(reader.IsSupported(capabilities, "reviews"));
            Assert.Equal("not_installed", reader.GetReason(capabilities, "reviews"));
            Assert.Equal("not_installed", reader.GetReason(capabilities, "missing"));
        }

        [Fact]
        public void StarterSsrAndPresentationAggregationTracerBullets_AreImplemented()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Program.cs");
            var presentationAggregation = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontPresentationApplicationBuilderExtensions.cs");
            var presentationCart = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Endpoints/StorefrontPresentationCartEndpoints.cs");
            var home = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Ssr/Home/HomePage.razor");

            Assert.Contains("\"/api/cart/lines\"", presentationCart, StringComparison.Ordinal);
            Assert.Contains("ValidateRequestAsync", presentationCart, StringComparison.Ordinal);
            Assert.Contains("AddLineAsync", presentationCart, StringComparison.Ordinal);
            Assert.Contains("MapStorefrontPresentationCartEndpoints", presentationAggregation, StringComparison.Ordinal);
            Assert.DoesNotContain("MapStaticAssets", program, StringComparison.Ordinal);
            Assert.Contains("UseStorefrontApplication", program, StringComparison.Ordinal);
            Assert.Contains("MapStorefrontApplication", program, StringComparison.Ordinal);
            Assert.DoesNotContain("MapStarterBffEndpoints", program, StringComparison.Ordinal);
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Endpoints/StarterBffEndpoints.cs")));
            Assert.False(File.Exists(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Services/StorefrontBootstrapService.cs")));
            Assert.DoesNotContain("StorefrontBootstrapService", program + home, StringComparison.Ordinal);
            Assert.Contains("Context.LatestProductSummaries", home, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(401, "auth.session_expired")]
        [InlineData(403, "policy.forbidden")]
        [InlineData(409, "cart.version_conflict")]
        [InlineData(422, "validation.failed")]
        public void RuntimeErrorMapper_PreservesStatusCodeAndMachineCode(int statusCode, string code)
        {
            var exception = new StorefrontApiException<CommerceNodeApiErrorResponse>(
                "mapped",
                statusCode,
                "{}",
                new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal),
                new CommerceNodeApiErrorResponse
                {
                    Success = false,
                    Code = code,
                    Message = "Mapped failure.",
                    TraceId = "trace-1",
                    FieldErrors = new Dictionary<string, ICollection<string>>(StringComparer.Ordinal)
                    {
                        ["field"] = ["error"],
                    },
                },
                innerException: null);

            var mapped = StorefrontRuntimeErrorMapper.FromApiException(exception);

            Assert.Equal(statusCode, mapped.Status);
            Assert.Equal(code, mapped.Code);
            Assert.Equal("Mapped failure.", mapped.Message);
            Assert.Equal("trace-1", mapped.TraceId);
            Assert.Equal(["error"], mapped.FieldErrors["field"]);
        }

        [Fact]
        public void StarterBrowserOutput_DoesNotContainCommerceUrlOrTokens()
        {
            var browserRoots = new[]
            {
                RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Components"),
                RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/wwwroot"),
            };

            var forbiddenTokens = new[]
            {
                "CommerceNodeBaseUrl",
                "http://localhost:5180",
                "https://localhost:5180",
                "accessToken",
                "refreshToken",
                "store secret",
                "provider credentials",
            };

            var violations = browserRoots
                .Where(Directory.Exists)
                .SelectMany(root => Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
                .Where(path => path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                .Select(path => new
                {
                    RelativePath = ToRepositoryRelativePath(path),
                    Source = File.ReadAllText(path),
                })
                .Where(file => forbiddenTokens.Any(token => file.Source.Contains(token, StringComparison.OrdinalIgnoreCase)))
                .Select(file => file.RelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(violations);
        }

        [Fact]
        public void StarterRouteSkeleton_RecordsRenderOwnershipAndHydrationModes()
        {
            var expectedRoutes = new Dictionary<string, string[]>(StringComparer.Ordinal);

            foreach (var (relativePath, routes) in expectedRoutes)
            {
                var source = ReadRepositoryFile(relativePath);
                foreach (var route in routes)
                {
                    Assert.Contains(route, source, StringComparison.Ordinal);
                }

                Assert.Contains("PlaceholderState", source, StringComparison.Ordinal);
            }

            var starterViews = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Ssr/Home/HomePage.razor"] = "StorefrontHomePageContext",
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Catalog/CategoryPage.razor"] = "StorefrontCategoryPageContext",
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Catalog/ProductPage.razor"] = "StorefrontProductPageContext",
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Catalog/SearchPage.razor"] = "StorefrontSearchPageContext",
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Commerce/CartPage.razor"] = "StorefrontCartPageContext",
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Commerce/CheckoutPage.razor"] = "StorefrontCheckoutPageContext",
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Commerce/PaymentResultPage.razor"] = "StorefrontPaymentResultPageContext",
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Commerce/DealsPage.razor"] = "StorefrontDealsPageContext",
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Catalog/NewReleasesPage.razor"] = "StorefrontNewReleasesPageContext",
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Ssr/Content/ContentPage.razor"] = "StorefrontContentPageContext",
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Ssr/Auth/AuthShellPage.razor"] = "StorefrontAuthPageContext",
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/WasmHost/Account/AccountHostPage.razor"] = "StorefrontAccountPageContext",
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Ssr/System/MaintenancePage.razor"] = "StorefrontSystemStateContext",
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Ssr/System/NotFoundPage.razor"] = "StorefrontSystemStateContext",
            };

            foreach (var (relativePath, contextType) in starterViews)
            {
                var source = ReadRepositoryFile(relativePath);
                Assert.DoesNotContain("@page", source, StringComparison.Ordinal);
                Assert.Contains(contextType, source, StringComparison.Ordinal);
            }

            var productPage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Catalog/ProductPage.razor");
            var productShell = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Components/Catalog/ProductDetailShell.razor");
            var purchasePanel = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Components/Catalog/PurchasePanelPlaceholder.razor");
            Assert.Contains("PurchasePanel=\"@Context.PurchasePanel\"", productPage, StringComparison.Ordinal);
            Assert.Contains("PurchaseActions=\"@Context.PurchaseActions\"", productPage, StringComparison.Ordinal);
            Assert.Contains("ProductPurchasePanelModel", productShell + purchasePanel, StringComparison.Ordinal);
            Assert.Contains("ProductPurchaseActionDescriptor", productShell + purchasePanel, StringComparison.Ordinal);
            foreach (var descriptor in new[]
            {
                "data-storefront-product-purchase",
                "data-selection-preview-route",
                "data-product-id",
                "data-product-name",
                "data-resolved-variant-id",
                "data-currency-code",
                "data-storefront-command=\"cart.add-line\"",
                "data-storefront-product-purchase-submit",
                "data-storefront-purchase-quantity",
                "data-storefront-purchase-feedback",
            })
            {
                Assert.Contains(descriptor, purchasePanel, StringComparison.Ordinal);
            }

            var home = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Ssr/Home/HomePage.razor");
            Assert.DoesNotContain("BootstrapService.LoadAsync", home, StringComparison.Ordinal);
            Assert.Contains("Context.LatestProductSummaries", home, StringComparison.Ordinal);
            Assert.Contains("StarterHydrationMode.InitialSnapshot", home, StringComparison.Ordinal);

            var hydration = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Composition/StarterHydrationMode.cs");
            var pagesReadme = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/README.md");

            Assert.Contains("InitialSnapshot", hydration, StringComparison.Ordinal);
            Assert.Contains("BrowserFetch", hydration, StringComparison.Ordinal);
            Assert.Contains("RefreshAfterHydration", hydration, StringComparison.Ordinal);
            Assert.Contains("ShouldFetchOnFirstLoad", hydration, StringComparison.Ordinal);
            Assert.Contains("must not duplicate the first fetch", pagesReadme, StringComparison.Ordinal);
        }

        [Fact]
        public void StarterGenerationContract_FreezesRoutesSlotsActionsAndMetadata()
        {
            var contract = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml");
            var generator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/new-storefront-project.ps1");
            var validator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderGeneratedProject.ps1");
            var presentationRoutes = string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(
                    RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages"),
                    "*.razor",
                    SearchOption.AllDirectories)
                    .Select(File.ReadAllText));

            foreach (var expected in new[]
            {
                "contractVersion: 1",
                "starter-generation.contract.yaml",
                "generatedConstraints:",
                "routeDeclarations: forbidden",
                "additionalRouteAssemblies: forbidden",
                "metadata:",
                "starterContractVersion: contractVersion",
                "packageVersionSource: StorefrontPackageVersions.props",
            })
            {
                Assert.Contains(expected, contract, StringComparison.Ordinal);
            }

            foreach (var route in new[]
            {
                "/",
                "/category/{Slug}",
                "/product/{Slug}",
                "/search",
                "/pages/{Slug}",
                "/cart",
                "/my-cart",
                "/checkout",
                "/account",
                "/account/{*Path}",
                "/signin",
                "/register",
                "/forgot-password",
                "/reset-password",
                "/payment/result",
                "/payment-success",
                "/payment-cancel",
                "/todays-deals",
                "/new-releases",
                "/maintenance",
                "/{*Path:nonfile}",
            })
            {
                Assert.Contains($"route: {route}", contract, StringComparison.Ordinal);
                Assert.Contains($"@page \"{route}\"", presentationRoutes, StringComparison.Ordinal);
            }

            foreach (var accountChildRoute in new[] { "/account/profile", "/account/addresses", "/account/orders", "/account/change-password" })
            {
                Assert.Contains($"- {accountChildRoute}", contract, StringComparison.Ordinal);
                Assert.Contains("path: Pages/WasmHost/Account/AccountHostPage.razor", contract, StringComparison.Ordinal);
            }

            Assert.DoesNotContain("route: /payment/success", contract, StringComparison.Ordinal);
            Assert.DoesNotContain("route: /payment/cancel", contract, StringComparison.Ordinal);
            Assert.Contains("path: Components/States/ErrorState.razor", contract, StringComparison.Ordinal);

            foreach (var slot in new[]
            {
                "layout.header",
                "layout.footer",
                "layout.main-navigation",
                "layout.mobile-navigation",
                "layout.cart-badge",
                "layout.account-menu",
                "home.sections",
                "catalog.product-card",
                "catalog.filters",
                "catalog.sorting",
                "catalog.pagination",
                "product.gallery",
                "product.information",
                "product.purchase",
                "cart.page",
                "checkout.page",
                "account.shell",
                "system.error",
            })
            {
                Assert.Contains($"id: {slot}", contract, StringComparison.Ordinal);
            }

            foreach (var action in new[]
            {
                "product.selection-preview",
                "cart.add-line",
                "cart.update-line",
                "cart.remove-line",
                "checkout.start",
                "checkout.review",
                "checkout.place-order",
                "account.profile",
                "account.password",
                "account.address",
                "account.order",
                "auth.login",
                "auth.logout",
                "auth.register",
                "auth.recovery",
                "consent.save",
                "consent.revoke",
            })
            {
                Assert.Contains($"id: {action}", contract, StringComparison.Ordinal);
            }

            foreach (var mode in new[] { "renderOwner: SSR", "renderOwner: Hybrid", "renderOwner: WASM-host", "InitialSnapshot", "BrowserFetch", "RefreshAfterHydration" })
            {
                Assert.Contains(mode, contract, StringComparison.Ordinal);
            }

            foreach (var metadataMarker in new[]
            {
                "starterContractVersion:",
                "packageVersions:",
                "BlazorShop.Storefront.Client:",
                "BlazorShop.Storefront.Runtime:",
                "BlazorShop.Storefront.Presentation:",
                "BlazorShop.Storefront.Components:",
                "BlazorShop.Storefront.Browser:",
            })
            {
                Assert.Contains(metadataMarker, generator, StringComparison.Ordinal);
                Assert.Contains(metadataMarker, validator, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void StarterGenerationContract_MatchesPresentationPageRoutes()
        {
            var presentationRoutes = ReadPresentationPageRoutes();
            var contractRouteMetadata = ReadStarterContractRouteMetadata();

            var missing = presentationRoutes
                .Where(route => !IsRouteCoveredByContract(route, contractRouteMetadata))
                .OrderBy(route => route, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(missing);
        }

        [Fact]
        public void StarterGenerationContract_CoversGeneratorRelevantRouteConstants()
        {
            var storefrontRoutes = ReadGeneratorRelevantStorefrontRouteConstants();
            var contractRouteMetadata = ReadStarterContractRouteMetadata();

            var missing = storefrontRoutes
                .Where(route => !IsRouteCoveredByContract(route.Route, contractRouteMetadata))
                .Select(route => $"{route.Name}: {route.Route}")
                .OrderBy(route => route, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(missing);
        }

        [Fact]
        public void StarterPages_DoNotImportStorefrontV2ComponentsOrCss()
        {
            var roots = new[]
            {
                RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages"),
                RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Components"),
                RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/wwwroot"),
            };

            var violations = roots
                .Where(Directory.Exists)
                .SelectMany(root => Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
                .Where(path => path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
                .Select(path => new
                {
                    RelativePath = ToRepositoryRelativePath(path),
                    Source = File.ReadAllText(path),
                })
                .Where(file => file.Source.Contains("BlazorShop.Storefront.V2", StringComparison.Ordinal)
                    || file.Source.Contains("BlazorShop.Storefront.Components.Features", StringComparison.Ordinal)
                    || file.Source.Contains("storefront.css", StringComparison.OrdinalIgnoreCase))
                .Select(file => file.RelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(violations);
        }

        [Fact]
        public void StarterClientPolicy_HasExceptionRegistryAndNoSilentManualContracts()
        {
            var policy = ReadRepositoryFile("docs/storefront-platform/storefront-client-adoption-policy.md");
            var registry = ReadRepositoryFile("docs/storefront-platform/storefront-client-exception-registry.md");
            var backlog = ReadRepositoryFile("docs/storefront-platform/storefront-v2-generated-client-backlog.md");

            Assert.Contains("uses generated `BlazorShop.Storefront.Client` contracts", policy, StringComparison.Ordinal);
            Assert.Contains("Manual `HttpClient` transport is forbidden", policy, StringComparison.Ordinal);
            Assert.Contains("| Capability | Exception | Reason | Owner | Test | Revisit trigger |", registry, StringComparison.Ordinal);
            Assert.Contains("| none | none | Starter currently has no manual transport exceptions.", registry, StringComparison.Ordinal);
            Assert.Contains("address", backlog, StringComparison.Ordinal);
            Assert.Contains("cart", backlog, StringComparison.Ordinal);
            Assert.Contains("checkout", backlog, StringComparison.Ordinal);
            Assert.Contains("consent", backlog, StringComparison.Ordinal);
            Assert.Contains("customer/account", backlog, StringComparison.Ordinal);
            Assert.Contains("payment", backlog, StringComparison.Ordinal);

            var starterFiles = Directory
                .EnumerateFiles(
                    RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter"),
                    "*.*",
                    SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                        || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)))
                .Select(path => new
                {
                    RelativePath = ToRepositoryRelativePath(path),
                    Source = File.ReadAllText(path),
                })
                .ToArray();

            var manualTransportViolations = starterFiles
                .Where(file => file.Source.Contains("new HttpClient", StringComparison.Ordinal)
                    || file.Source.Contains("StorefrontApiClient", StringComparison.Ordinal)
                    || file.Source.Contains("SendAsync(", StringComparison.Ordinal))
                .Select(file => file.RelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            var duplicateDtoViolations = starterFiles
                .Where(file => file.Source.Contains("CommerceNodeApiResponse", StringComparison.Ordinal)
                    || file.Source.Contains("StorefrontPublicConfigurationResponse", StringComparison.Ordinal)
                    || file.Source.Contains("StorefrontCartResponse", StringComparison.Ordinal))
                .Select(file => file.RelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(manualTransportViolations);
            Assert.Empty(duplicateDtoViolations);
        }

        [Fact]
        public void StarterFeatureManifest_AlignsWithBackendCapabilitiesAndPlacementRules()
        {
            var manifest = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Features/feature-manifest.json");
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Program.cs");
            var home = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Ssr/Home/HomePage.razor");

            foreach (var key in new[]
            {
                "customerAccounts",
                "registration",
                "cart",
                "checkout",
                "payments",
                "newsletter",
                "recommendations",
                "contactForm",
            })
            {
                Assert.Contains($"\"{key}\"", manifest, StringComparison.Ordinal);
            }

            foreach (var placement in new[] { "home", "productDetail", "category", "cart", "checkout", "account" })
            {
                Assert.Contains($"\"{placement}\"", manifest, StringComparison.Ordinal);
            }

            Assert.DoesNotContain("StarterFeatureManifest.Load", program, StringComparison.Ordinal);
            Assert.DoesNotContain("StarterFeatureActivationService", program + home, StringComparison.Ordinal);
            Assert.Contains("RecommendationsVisible", home, StringComparison.Ordinal);
            Assert.Contains("Context.FeatureCapabilities", home, StringComparison.Ordinal);
            Assert.DoesNotContain("@inject", home, StringComparison.Ordinal);
            Assert.DoesNotContain("Storefront.Features.", manifest, StringComparison.Ordinal);
        }

        [Fact]
        public void StarterNeutralLayoutAndStateComponents_ArePresent()
        {
            var expectedComponents = new[]
            {
                "Components/States/LoadingState.razor",
                "Components/States/SkeletonBlock.razor",
                "Components/States/EmptyState.razor",
                "Components/States/ErrorState.razor",
                "Components/States/ValidationSummary.razor",
                "Components/States/RetryAction.razor",
                "Components/States/UnavailableFeatureState.razor",
                "Components/Catalog/ProductSummaryCard.razor",
                "Components/Catalog/ProductGrid.razor",
                "Components/Catalog/ProductDetailShell.razor",
                "Components/Catalog/ProductGalleryPlaceholder.razor",
                "Components/Catalog/PurchasePanelPlaceholder.razor",
                "Components/Commerce/CartLineList.razor",
                "Components/Commerce/CheckoutStepShell.razor",
                "Components/Account/AccountShell.razor",
                "Components/Layout/StarterConsentBanner.razor",
            };

            foreach (var component in expectedComponents)
            {
                Assert.True(
                    File.Exists(RepositoryPath($"BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/{component}")),
                    $"Missing Starter component '{component}'.");
            }

            var layout = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Components/Layout/MainLayout.razor");
            var consent = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Components/Layout/StarterConsentBanner.razor");
            var css = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/wwwroot/css/starter.css");

            Assert.Contains("starter-header", layout, StringComparison.Ordinal);
            Assert.Contains("starter-footer", layout, StringComparison.Ordinal);
            Assert.Contains("starter-breadcrumb", layout, StringComparison.Ordinal);
            Assert.Contains("starter-toast-region", layout, StringComparison.Ordinal);
            Assert.Contains("Context.Links.Cart.Href", layout, StringComparison.Ordinal);
            Assert.Contains("Context.Links.AccountRoot.Href", layout, StringComparison.Ordinal);
            Assert.Contains("data-storefront-consent-banner", consent, StringComparison.Ordinal);
            Assert.Contains("StorefrontConsentContext", consent, StringComparison.Ordinal);
            Assert.Contains("starter-consent-banner", css, StringComparison.Ordinal);
            Assert.Contains("@media (max-width: 720px)", css, StringComparison.Ordinal);
            Assert.Contains("border-radius: 8px", css, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Storefront.V2", css, StringComparison.Ordinal);
        }

        [Fact]
        public void StarterIsolationGateScript_PacksBuildsPublishesAndRejectsMonorepoReferences()
        {
            var script = ReadRepositoryFile("scripts/qa/run-storefront-starter-isolation-gate.ps1");
            var workflow = ReadRepositoryFile(".github/workflows/ci.yml");

            Assert.Contains("dotnet pack $clientProject", script, StringComparison.Ordinal);
            Assert.Contains("dotnet pack $runtimeProject", script, StringComparison.Ordinal);
            Assert.Contains("dotnet pack $presentationProject", script, StringComparison.Ordinal);
            Assert.Contains("dotnet pack $componentsProject", script, StringComparison.Ordinal);
            Assert.Contains("Rewrite isolated Starter to package mode", script, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Storefront.Presentation", script, StringComparison.Ordinal);
            Assert.Contains("obj\\storefront-starter-isolation", script, StringComparison.Ordinal);
            Assert.Contains("Storefront.Sample", script, StringComparison.Ordinal);
            Assert.Contains("dotnet restore $starterProject", script, StringComparison.Ordinal);
            Assert.Contains("dotnet build $starterProject", script, StringComparison.Ordinal);
            Assert.Contains("dotnet publish $starterProject", script, StringComparison.Ordinal);
            Assert.Contains("ProjectReference", script, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Storefront.V2", script, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Web.SharedV2", script, StringComparison.Ordinal);
            Assert.Contains("Web.SharedV2", script, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Application", script, StringComparison.Ordinal);
            Assert.Contains("[switch]$Describe", script, StringComparison.Ordinal);
            Assert.Contains("run-storefront-starter-isolation-gate.ps1 -Describe", workflow, StringComparison.Ordinal);
        }

        [Fact]
        public void GeneratedStorefrontReleaseGateScript_CoversPackageContractSeoSecurityAndRouteSmoke()
        {
            var script = ReadRepositoryFile("scripts/qa/run-storefront-sample-release-gate.ps1");
            var workflow = ReadRepositoryFile(".github/workflows/ci.yml");

            Assert.Contains("BlazorShop.Storefront.GeneratedProof", script, StringComparison.Ordinal);
            Assert.Contains("artifacts\\storefront-builder\\generated", script, StringComparison.Ordinal);
            Assert.Contains("dotnet pack $clientProject", script, StringComparison.Ordinal);
            Assert.Contains("dotnet pack $runtimeProject", script, StringComparison.Ordinal);
            Assert.Contains("dotnet restore $sampleProject", script, StringComparison.Ordinal);
            Assert.Contains("dotnet build $sampleProject", script, StringComparison.Ordinal);
            Assert.Contains("dotnet publish $sampleProject", script, StringComparison.Ordinal);
            Assert.Contains("Assert-SourceDoesNotContain $forbiddenSourcePatterns", script, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Web.SharedV2", script, StringComparison.Ordinal);
            Assert.Contains("Web.SharedV2", script, StringComparison.Ordinal);
            Assert.Contains("IStorefrontCheckoutClient", script, StringComparison.Ordinal);
            Assert.Contains("Place a COD order from a checkout session.", script, StringComparison.Ordinal);
            Assert.Contains("/robots.txt", script, StringComparison.Ordinal);
            Assert.Contains("/sitemap.xml", script, StringComparison.Ordinal);
            Assert.Contains("application/ld+json", script, StringComparison.Ordinal);
            Assert.Contains("ValidateRequestAsync", script, StringComparison.Ordinal);
            Assert.Contains("[InlineData(401", script, StringComparison.Ordinal);
            Assert.Contains("[InlineData(403", script, StringComparison.Ordinal);
            Assert.Contains("[InlineData(409", script, StringComparison.Ordinal);
            Assert.Contains("[InlineData(422", script, StringComparison.Ordinal);
            Assert.Contains("Start-DotnetSample", script, StringComparison.Ordinal);
            Assert.Contains("Assert-HttpContains", script, StringComparison.Ordinal);
            Assert.Contains("[switch]$SkipRuntime", script, StringComparison.Ordinal);
            Assert.Contains("run-storefront-sample-release-gate.ps1 -Describe", workflow, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontAiGeneratorPlan_ConstrictsAiToPresentationAndProtectsContracts()
        {
            var plan = ReadRepositoryFile("docs/storefront-platform/storefront-ai-generator-plan.md");
            var contract = ReadRepositoryFile("docs/visual-reverse-engineering-skill/generated-storefront-foundation-contract.md");

            Assert.Contains("Planning only", plan, StringComparison.Ordinal);
            Assert.Contains("scripts/generate-storefront-sample.ps1", plan, StringComparison.Ordinal);
            Assert.Contains("scripts/qa/run-storefront-sample-release-gate.ps1", plan, StringComparison.Ordinal);
            Assert.Contains("Allowed AI Edit Areas", plan, StringComparison.Ordinal);
            Assert.Contains("Protected Areas", plan, StringComparison.Ordinal);
            Assert.Contains("generated client source and generated API DTOs", plan, StringComparison.Ordinal);
            Assert.Contains("same-origin BFF transport", plan, StringComparison.Ordinal);
            Assert.Contains("cart commands", plan, StringComparison.Ordinal);
            Assert.Contains("checkout commands", plan, StringComparison.Ordinal);
            Assert.Contains("copies Storefront V2 source", plan, StringComparison.Ordinal);
            Assert.Contains("exposes Commerce Node base URL", plan, StringComparison.Ordinal);
            Assert.Contains("Generated Storefront Foundation Contract", contract, StringComparison.Ordinal);
            Assert.Contains("same-origin browser actions", contract, StringComparison.Ordinal);
            Assert.Contains("Forbidden References", contract, StringComparison.Ordinal);
        }

        [Fact]
        public void GeneratedStorefrontGeneration_IsDeterministicAndV2Independent()
        {
            var script = ReadRepositoryFile("scripts/generate-storefront-sample.ps1");
            var proof = ReadRepositoryFile("scripts/qa/run-storefront-builder-generated-proof.ps1");

            Assert.Contains("Copy-StarterTemplate", script, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Storefront.Starter", script, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Storefront.V2", script, StringComparison.Ordinal);
            Assert.Contains("Generated\\StorefrontClient.g.cs", script, StringComparison.Ordinal);
            Assert.Contains("ProjectReference", script, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Web.SharedV2", script, StringComparison.Ordinal);
            Assert.Contains("Web.SharedV2", script, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Storefront.GeneratedProof", script, StringComparison.Ordinal);
            Assert.Contains("artifacts/storefront-builder/generated", script, StringComparison.Ordinal);
            Assert.Contains("StorefrontBuilder generated proof workflow", proof, StringComparison.Ordinal);
            Assert.Contains("run-storefront-builder-isolation-gate.ps1", proof, StringComparison.Ordinal);
            Assert.Contains("validate-storefront.ps1", proof, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.PresentationV2\\$Name", script, StringComparison.Ordinal);
        }

        [Fact]
        public void GeneratedStorefrontProjects_AreNotActiveSolutionDependencies()
        {
            var solution = ReadRepositoryFile("BlazorShop.sln");
            var cleanupPlan = ReadRepositoryFile("docs/visual-reverse-engineering-skill/04-StorefrontBuilder-Generated-Store-Cleanup.todo.md");

            Assert.DoesNotContain("BlazorShop.PresentationV2\\BlazorShop.Storefront.Sample\\BlazorShop.Storefront.Sample.csproj", solution, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.PresentationV2\\BlazorShop.Storefront.BuilderDemo\\BlazorShop.Storefront.BuilderDemo.csproj", solution, StringComparison.Ordinal);
            Assert.Contains("Generated output policy", cleanupPlan, StringComparison.Ordinal);
            Assert.Contains("Generated output must not be added to `BlazorShop.sln` by default", cleanupPlan, StringComparison.Ordinal);
        }

        [Fact]
        public void StarterDocs_SayStorefrontV2IsBehaviorReferenceOnly()
        {
            var adr = ReadRepositoryFile("docs/architecture/adr/2026-07-24-storefront-starter-foundation.md");
            var folderGuide = ReadRepositoryFile("docs/architecture/05-project-and-folder-guide.md");

            Assert.Contains("behavior reference", adr, StringComparison.Ordinal);
            Assert.Contains("must not be copied into Starter", adr, StringComparison.Ordinal);
            Assert.Contains("Copy Storefront V2 source", folderGuide, StringComparison.Ordinal);
            Assert.Contains("Storefront V2 into a neutral template", folderGuide, StringComparison.Ordinal);
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
                .ToList();
        }

        private static IReadOnlyList<string> ReadPresentationPageRoutes()
        {
            return Directory
                .EnumerateFiles(
                    RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages"),
                    "*.razor",
                    SearchOption.AllDirectories)
                .SelectMany(File.ReadLines)
                .Select(TryReadPageDirectiveRoute)
                .Where(route => !string.IsNullOrWhiteSpace(route))
                .Select(route => route!)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(route => route, StringComparer.Ordinal)
                .ToArray();
        }

        private static IReadOnlyList<(string Name, string Route)> ReadGeneratorRelevantStorefrontRouteConstants()
        {
            var ignoredConstantNames = new HashSet<string>(StringComparer.Ordinal)
            {
                "CurrencyPreference",
                "PagesBase",
                "ProductSelectionPreview",
                "Robots",
                "Sitemap",
            };

            return File
                .ReadLines(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/StorefrontRoutes.cs"))
                .Select(TryReadStorefrontRouteConstant)
                .Where(route => route.HasValue)
                .Select(route => route!.Value)
                .Where(route => !ignoredConstantNames.Contains(route.Name))
                .Where(route => route.Route.StartsWith("/", StringComparison.Ordinal))
                .Where(route => !route.Route.StartsWith("/api/", StringComparison.Ordinal))
                .OrderBy(route => route.Name, StringComparer.Ordinal)
                .ToArray();
        }

        private static IReadOnlySet<string> ReadStarterContractRouteMetadata()
        {
            var routes = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var line in File.ReadLines(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml")))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("route: ", StringComparison.Ordinal))
                {
                    routes.Add(trimmed["route: ".Length..]);
                    continue;
                }

                if (trimmed.StartsWith("- route: ", StringComparison.Ordinal))
                {
                    routes.Add(trimmed["- route: ".Length..]);
                    continue;
                }

                if (trimmed.StartsWith("- /", StringComparison.Ordinal))
                {
                    routes.Add(trimmed["- ".Length..]);
                }
            }

            return routes;
        }

        private static string? TryReadPageDirectiveRoute(string line)
        {
            var trimmed = line.Trim();
            const string marker = "@page \"";

            if (!trimmed.StartsWith(marker, StringComparison.Ordinal))
            {
                return null;
            }

            var routeEnd = trimmed.IndexOf('"', marker.Length);
            return routeEnd < 0 ? null : trimmed[marker.Length..routeEnd];
        }

        private static (string Name, string Route)? TryReadStorefrontRouteConstant(string line)
        {
            var trimmed = line.Trim();
            const string declaration = "public const string ";

            if (!trimmed.StartsWith(declaration, StringComparison.Ordinal))
            {
                return null;
            }

            var separator = trimmed.IndexOf(" = \"", StringComparison.Ordinal);
            if (separator < 0)
            {
                return null;
            }

            var name = trimmed[declaration.Length..separator].Trim();
            var valueStart = separator + " = \"".Length;
            var valueEnd = trimmed.IndexOf('"', valueStart);
            if (valueEnd < 0)
            {
                return null;
            }

            return (name, trimmed[valueStart..valueEnd]);
        }

        private static bool IsRouteCoveredByContract(string route, IReadOnlySet<string> contractRouteMetadata)
        {
            return contractRouteMetadata.Contains(route)
                || contractRouteMetadata.Any(contractRoute => RouteTemplateCoversRoute(contractRoute, route));
        }

        private static bool RouteTemplateCoversRoute(string routeTemplate, string route)
        {
            if (routeTemplate.StartsWith("/{*", StringComparison.Ordinal))
            {
                return false;
            }

            if (routeTemplate.EndsWith("/{*Path}", StringComparison.Ordinal))
            {
                var prefix = routeTemplate[..^"{*Path}".Length];
                return route.StartsWith(prefix, StringComparison.Ordinal);
            }

            var parameterStart = routeTemplate.IndexOf('{', StringComparison.Ordinal);
            if (parameterStart < 0)
            {
                return false;
            }

            var parameterEnd = routeTemplate.IndexOf('}', parameterStart);
            if (parameterEnd < 0)
            {
                return false;
            }

            var prefixBeforeParameter = routeTemplate[..parameterStart];
            var suffixAfterParameter = routeTemplate[(parameterEnd + 1)..];
            if (!route.StartsWith(prefixBeforeParameter, StringComparison.Ordinal)
                || !route.EndsWith(suffixAfterParameter, StringComparison.Ordinal))
            {
                return false;
            }

            var routeParameterValue = route[prefixBeforeParameter.Length..^suffixAfterParameter.Length];
            return routeParameterValue.Length > 0
                && !routeParameterValue.Contains('/', StringComparison.Ordinal);
        }

        private static bool IsForbiddenStarterProjectReference(string reference)
        {
            var normalized = reference.Replace('\\', '/');

            return normalized.Contains("/BlazorShop.Domain/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.Application/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.Infrastructure/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.CommerceNode.API/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.ControlPlane.API/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.ControlPlane.Web/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.Storefront.V2/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.Web.SharedV2/", StringComparison.OrdinalIgnoreCase);
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
            return Path.GetRelativePath(FindRepositoryRoot(), path)
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');
        }

        private static string FormatProcessFailure(string message, ProcessResult result)
        {
            return string.Join(
                Environment.NewLine,
                message,
                $"Exit code: {result.ExitCode}",
                "stdout:",
                result.StandardOutput,
                "stderr:",
                result.StandardError);
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
        }

        private static ProcessResult RunProcess(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
        {
            var standardOutput = new StringBuilder();
            var standardError = new StringBuilder();
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = System.Diagnostics.Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start process '{fileName}'.");
            process.OutputDataReceived += (_, eventArgs) =>
            {
                if (eventArgs.Data is not null)
                {
                    standardOutput.AppendLine(eventArgs.Data);
                }
            };
            process.ErrorDataReceived += (_, eventArgs) =>
            {
                if (eventArgs.Data is not null)
                {
                    standardError.AppendLine(eventArgs.Data);
                }
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!process.WaitForExit(TimeSpan.FromMinutes(3)))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                }

                return new ProcessResult(
                    -1,
                    standardOutput.ToString(),
                    standardError.AppendLine($"Process '{fileName}' exceeded the 3 minute test step timeout.").ToString());
            }

            process.WaitForExit();
            return new ProcessResult(process.ExitCode, standardOutput.ToString(), standardError.ToString());
        }

        private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
    }
}
