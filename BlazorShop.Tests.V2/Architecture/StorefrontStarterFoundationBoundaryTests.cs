namespace BlazorShop.Tests.Architecture
{
    using System.Xml.Linq;

    using BlazorShop.Storefront.Client;
    using BlazorShop.Storefront.Runtime;

    using Xunit;

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
        public void StarterSource_DoesNotUseSharedBusinessModelsOrCopyManualTransport()
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
                .Where(file => file.Source.Contains("BlazorShop.Web.SharedV2.Models", StringComparison.Ordinal)
                    || file.Source.Contains("StorefrontApiClient", StringComparison.Ordinal)
                    || file.Source.Contains("Generated/StorefrontClient.g.cs", StringComparison.Ordinal)
                    || file.Source.Contains("ProjectReference", StringComparison.Ordinal))
                .Select(file => file.RelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(violations);
        }

        [Fact]
        public void StarterProject_ConsumesStorefrontClientAsPackage()
        {
            var project = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj");
            var versionProps = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/StorefrontPackageVersions.props");
            var nugetConfig = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/nuget.config");
            var compatibility = ReadRepositoryFile("docs/storefront-platform/storefront-package-compatibility.md");
            var changelog = ReadRepositoryFile("docs/storefront-platform/storefront-client-changelog.md");

            Assert.Contains("<PackageReference Include=\"BlazorShop.Storefront.Client\" Version=\"$(StorefrontClientPackageVersion)\"", project, StringComparison.Ordinal);
            Assert.Contains("<PackageReference Include=\"BlazorShop.Storefront.Runtime\" Version=\"$(StorefrontRuntimePackageVersion)\"", project, StringComparison.Ordinal);
            Assert.DoesNotContain("<ProjectReference", project, StringComparison.Ordinal);
            Assert.Contains("<StorefrontClientPackageVersion>1.0.0-local</StorefrontClientPackageVersion>", versionProps, StringComparison.Ordinal);
            Assert.Contains("<StorefrontRuntimePackageVersion>1.0.0-local</StorefrontRuntimePackageVersion>", versionProps, StringComparison.Ordinal);
            Assert.Contains("local-storefront-packages", nugetConfig, StringComparison.Ordinal);
            Assert.Contains("| v1 | 1.x | compatible |", compatibility, StringComparison.Ordinal);
            Assert.Contains("| 1.x | 1.x |", compatibility, StringComparison.Ordinal);
            Assert.Contains("1.0.0-local", changelog, StringComparison.Ordinal);
        }

        [Fact]
        public void StarterProject_RestoresAndBuildsFromLocalStorefrontClientPackage()
        {
            var repositoryRoot = FindRepositoryRoot();
            var packageFeed = RepositoryPath("artifacts/storefront-packages");
            var starterProject = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj");

            if (Directory.Exists(packageFeed))
            {
                Directory.Delete(packageFeed, recursive: true);
            }

            Directory.CreateDirectory(packageFeed);

            var packResult = RunProcess(
                "dotnet",
                [
                    "pack",
                    RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj"),
                    "--no-restore",
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
                    "--no-restore",
                    "--output",
                    packageFeed,
                    "/p:PackageVersion=1.0.0-local",
                ],
                repositoryRoot);

            Assert.True(runtimePackResult.ExitCode == 0, FormatProcessFailure("Storefront runtime package did not pack.", runtimePackResult));

            var restoreResult = RunProcess("dotnet", ["restore", starterProject], repositoryRoot);
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
        public void StarterSsrAndBffTracerBullets_AreImplemented()
        {
            var bootstrap = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Services/StorefrontBootstrapService.cs");
            var bff = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Endpoints/StarterBffEndpoints.cs");
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Program.cs");
            var home = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Ssr/Home/HomePage.razor");

            Assert.Contains("GetCurrentAsync", bootstrap, StringComparison.Ordinal);
            Assert.Contains("configurationClient.GetAsync", bootstrap, StringComparison.Ordinal);
            Assert.Contains("QueryProductsAsync", bootstrap, StringComparison.Ordinal);
            Assert.Contains("StorefrontRuntimeErrorMapper.FromApiException", bootstrap, StringComparison.Ordinal);
            Assert.Contains("\"/api/cart/lines\"", bff, StringComparison.Ordinal);
            Assert.Contains("ValidateRequestAsync", bff, StringComparison.Ordinal);
            Assert.Contains("CreateSessionAsync", bff, StringComparison.Ordinal);
            Assert.Contains("AddLineAsync", bff, StringComparison.Ordinal);
            Assert.Contains("HttpOnly = true", bff, StringComparison.Ordinal);
            Assert.Contains("SameSite = SameSiteMode.Lax", bff, StringComparison.Ordinal);
            Assert.Contains("UseStaticFiles", program, StringComparison.Ordinal);
            Assert.DoesNotContain("MapStaticAssets", program, StringComparison.Ordinal);
            Assert.Contains("MapStarterBffEndpoints", program, StringComparison.Ordinal);
            Assert.Contains("BootstrapService.LoadAsync", home, StringComparison.Ordinal);
            Assert.Contains("data-error-code", home, StringComparison.Ordinal);
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
            var expectedRoutes = new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Ssr/Home/HomePage.razor"] = ["@page \"/\""],
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Ssr/Content/ContentPage.razor"] = ["@page \"/content/{Slug}\""],
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Ssr/Auth/AuthShellPage.razor"] = ["@page \"/signin\""],
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Ssr/System/MaintenancePage.razor"] = ["@page \"/maintenance\""],
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Ssr/System/NotFoundPage.razor"] = ["@page \"/not-found\""],
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Catalog/ProductPage.razor"] = ["@page \"/product/{Slug}\""],
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Catalog/CategoryPage.razor"] = ["@page \"/category/{Slug}\""],
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Catalog/SearchPage.razor"] = ["@page \"/search\""],
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Commerce/CartPage.razor"] = ["@page \"/cart\""],
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Commerce/CheckoutPage.razor"] = ["@page \"/checkout\""],
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Commerce/PaymentResultPage.razor"] = ["@page \"/payment/result\""],
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Commerce/DealsPage.razor"] = ["@page \"/deals\""],
                ["BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/WasmHost/Account/AccountHostPage.razor"] = ["@page \"/account\"", "@page \"/account/{*Path}\""],
            };

            foreach (var (relativePath, routes) in expectedRoutes)
            {
                var source = ReadRepositoryFile(relativePath);
                foreach (var route in routes)
                {
                    Assert.Contains(route, source, StringComparison.Ordinal);
                }

                if (relativePath.EndsWith("HomePage.razor", StringComparison.Ordinal))
                {
                    Assert.Contains("BootstrapService.LoadAsync", source, StringComparison.Ordinal);
                    Assert.Contains("StarterHydrationMode.InitialSnapshot", source, StringComparison.Ordinal);
                }
                else
                {
                    Assert.Contains("PlaceholderState", source, StringComparison.Ordinal);
                }
            }

            var hydration = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Composition/StarterHydrationMode.cs");
            var pagesReadme = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/README.md");

            Assert.Contains("InitialSnapshot", hydration, StringComparison.Ordinal);
            Assert.Contains("BrowserFetch", hydration, StringComparison.Ordinal);
            Assert.Contains("RefreshAfterHydration", hydration, StringComparison.Ordinal);
            Assert.Contains("ShouldFetchOnFirstLoad", hydration, StringComparison.Ordinal);
            Assert.Contains("must not duplicate the first fetch", pagesReadme, StringComparison.Ordinal);
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
            var parser = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Features/StarterFeatureManifest.cs");
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
                Assert.Contains($"\"{key}\"", parser, StringComparison.Ordinal);
            }

            foreach (var placement in new[] { "home", "productDetail", "category", "cart", "checkout", "account" })
            {
                Assert.Contains($"\"{placement}\"", parser, StringComparison.Ordinal);
            }

            Assert.Contains("JsonSerializer.Deserialize", parser, StringComparison.Ordinal);
            Assert.Contains("IStorefrontCapabilityReader", parser, StringComparison.Ordinal);
            Assert.Contains("BackendSupported", parser, StringComparison.Ordinal);
            Assert.Contains("StoreEnabled", parser, StringComparison.Ordinal);
            Assert.Contains("PresentationPlaced", parser, StringComparison.Ordinal);
            Assert.Contains("StarterFeatureManifest.Load", program, StringComparison.Ordinal);
            Assert.Contains("FeatureActivationService.Evaluate", home, StringComparison.Ordinal);
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
            };

            foreach (var component in expectedComponents)
            {
                Assert.True(
                    File.Exists(RepositoryPath($"BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/{component}")),
                    $"Missing Starter component '{component}'.");
            }

            var layout = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Components/Layout/MainLayout.razor");
            var css = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/wwwroot/css/starter.css");

            Assert.Contains("starter-header", layout, StringComparison.Ordinal);
            Assert.Contains("starter-footer", layout, StringComparison.Ordinal);
            Assert.Contains("starter-breadcrumb", layout, StringComparison.Ordinal);
            Assert.Contains("starter-toast-region", layout, StringComparison.Ordinal);
            Assert.Contains("/cart", layout, StringComparison.Ordinal);
            Assert.Contains("/account", layout, StringComparison.Ordinal);
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
            Assert.Contains("obj\\storefront-starter-isolation", script, StringComparison.Ordinal);
            Assert.Contains("Storefront.Sample", script, StringComparison.Ordinal);
            Assert.Contains("dotnet restore $starterProject", script, StringComparison.Ordinal);
            Assert.Contains("dotnet build $starterProject", script, StringComparison.Ordinal);
            Assert.Contains("dotnet publish $starterProject", script, StringComparison.Ordinal);
            Assert.Contains("ProjectReference", script, StringComparison.Ordinal);
            Assert.Contains("BlazorShop.Storefront.V2", script, StringComparison.Ordinal);
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
            var todo = ReadRepositoryFile("docs/refactor-control-Commerce-storefront/Storefront Starter Foundation.todo.md");

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
            Assert.Contains("AI Generator Planning", todo, StringComparison.Ordinal);
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

        private static bool IsForbiddenStarterProjectReference(string reference)
        {
            var normalized = reference.Replace('\\', '/');

            return normalized.Contains("/BlazorShop.Domain/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.Application/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.Infrastructure/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.CommerceNode.API/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.ControlPlane.API/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.ControlPlane.Web/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/BlazorShop.Storefront.V2/", StringComparison.OrdinalIgnoreCase);
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
            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return new ProcessResult(process.ExitCode, standardOutput, standardError);
        }

        private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
    }
}
