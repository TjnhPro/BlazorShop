namespace BlazorShop.Tests.PresentationV2
{
    using System.Text.RegularExpressions;

    using Xunit;

    public sealed partial class LayoutAssetFoundationTests
    {
        private static readonly string[] StorefrontRootStylesheetAssetKeys = ["css/site.css", "css/wasm-site.css", "css/storefront.css"];
        private static readonly string[] StorefrontCoreScriptAllowlist = ["_framework/blazor.web.js", "_content/BlazorShop.Storefront.Presentation/js/storefront.application.js"];
        private static readonly string[] StorefrontVisualScriptAssetKeys = ["js/storefrontCommerce.js"];
        private static readonly string[] ControlPlaneRootStylesheetAllowlist = ["vendor/fontawesome/css/all.min.css", "css/site.css", "css/app.css"];
        private static readonly string[] ControlPlaneRootScriptAllowlist = ["_framework/blazor.webassembly.js", "js/downloads.js"];

        [Fact]
        public void StorefrontRoot_DefinesExpectedAssetsWithoutDuplicates()
        {
            var appMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/App/StorefrontApp.razor");
            var headMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontApplicationHead.razor");
            var coreScriptMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Views/Foundation/StorefrontFoundationCoreScripts.razor");
            var scriptMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontApplicationScripts.razor");

            Assert.Equal(StorefrontRootStylesheetAssetKeys, ExtractAssetKeys(headMarkup));
            Assert.Equal(StorefrontCoreScriptAllowlist, ExtractScriptSources(coreScriptMarkup));
            Assert.Equal(StorefrontVisualScriptAssetKeys, ExtractAssetKeys(scriptMarkup));
            AssertAssetOrder(headMarkup, StorefrontRootStylesheetAssetKeys);
            Assert.DoesNotContain("href=\"css/", headMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("src=\"js/", scriptMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("<link rel=\"icon\" type=\"image/png\" href=\"icon-192.png\" />", headMarkup, StringComparison.Ordinal);
            Assert.Contains("<StorefrontIconHead DisplayContext=\"Context.Display\" />", headMarkup);
            Assert.True(
                appMarkup.IndexOf("<StorefrontAntiforgeryHead />", StringComparison.Ordinal) <
                appMarkup.IndexOf("<StorefrontFoundationApplicationHead />", StringComparison.Ordinal));
            Assert.True(
                appMarkup.IndexOf("ComponentType=\"@ViewSet.ApplicationHead\"", StringComparison.Ordinal) <
                appMarkup.IndexOf("<HeadOutlet />", StringComparison.Ordinal));
            Assert.True(
                appMarkup.IndexOf("<StorefrontFoundationCoreScripts />", StringComparison.Ordinal) <
                appMarkup.IndexOf("ComponentType=\"@ViewSet.VisualScripts\"", StringComparison.Ordinal));
            Assert.True(
                coreScriptMarkup.IndexOf("_framework/blazor.web.js", StringComparison.Ordinal) <
                coreScriptMarkup.IndexOf("_content/BlazorShop.Storefront.Presentation/js/storefront.application.js", StringComparison.Ordinal));
            Assert.Contains("<StorefrontBrandHead DisplayContext=\"Context.Display\" />", headMarkup);
            Assert.True(
                headMarkup.IndexOf("<StorefrontIconHead DisplayContext=\"Context.Display\" />", StringComparison.Ordinal) <
                headMarkup.IndexOf("<StorefrontBrandHead DisplayContext=\"Context.Display\" />", StringComparison.Ordinal));
            Assert.DoesNotContain("<StorefrontAntiforgeryHead />", headMarkup, StringComparison.Ordinal);
            Assert.Contains("<html lang=\"@DocumentLanguage\" dir=\"@DocumentDirection\">", appMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("<html lang=\"en\">", appMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("document.documentElement.lang", headMarkup, StringComparison.Ordinal);
            AssertRootDoesNotReferenceLegacyPresentationAssets(appMarkup);
            AssertRootDoesNotReferenceLegacyPresentationAssets(headMarkup);
            AssertRootDoesNotReferenceLegacyPresentationAssets(coreScriptMarkup);
            AssertRootDoesNotReferenceLegacyPresentationAssets(scriptMarkup);
        }

        [Fact]
        public void StorefrontVisualHosts_UsePresentationLinkDescriptorsInsteadOfRouteConstants()
        {
            var visualFiles = Directory
                .EnumerateFiles(Path.Combine(FindRepositoryRoot(), "BlazorShop.PresentationV2/BlazorShop.Storefront.V2"), "*.razor", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(Path.Combine(FindRepositoryRoot(), "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter"), "*.razor", SearchOption.AllDirectories))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.NotEmpty(visualFiles);
            foreach (var file in visualFiles)
            {
                var source = File.ReadAllText(file);
                Assert.DoesNotContain("StorefrontRoutes", source, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void StorefrontLayout_KeepsSingleToastRegionAndGlobalShell()
        {
            var layoutMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor");
            var brandHeadMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Seo/StorefrontBrandHead.razor");
            var pageShellMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontPageShell.razor");

            Assert.Contains("<StorefrontHeader Context=\"Context.Header\" />", layoutMarkup);
            Assert.Contains("<main class=\"bs-storefront-main flex-1\">", layoutMarkup);
            Assert.Contains("<StorefrontFooter Context=\"Context.Footer\" />", layoutMarkup);
            Assert.Equal(1, CountOccurrences(layoutMarkup, "data-storefront-toast-region"));
            Assert.DoesNotContain("<HeadContent>", layoutMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("WasmProbe", layoutMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("data-wasm-probe", layoutMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("HostEnvironment.IsDevelopment()", layoutMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("<HeadContent>", brandHeadMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("<main", pageShellMarkup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<HeadContent>", pageShellMarkup, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontPageShell_DefinesOptionalRegionsWithoutOwningSeoOrMain()
        {
            var pageShellMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontPageShell.razor");

            Assert.Contains("public RenderFragment? Breadcrumb", pageShellMarkup);
            Assert.Contains("public RenderFragment? Header", pageShellMarkup);
            Assert.Contains("public RenderFragment? Actions", pageShellMarkup);
            Assert.Contains("public RenderFragment? Sidebar", pageShellMarkup);
            Assert.Contains("public RenderFragment ChildContent", pageShellMarkup);
            Assert.Contains("[EditorRequired]", pageShellMarkup);
            Assert.DoesNotContain("<main", pageShellMarkup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<SeoHead", pageShellMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("<HeadContent>", pageShellMarkup, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontContentRouteAndView_SplitSeoFromVisualShell()
        {
            var routeMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages/Ssr/Content/ContentRoutePage.razor");
            var pageMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Ssr/Content/StorefrontPage.razor");

            Assert.Contains("<StorefrontPage TContext=\"StorefrontContentPageContext\"", routeMarkup, StringComparison.Ordinal);
            Assert.Contains("Metadata=\"_result.Metadata\"", routeMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("<StorefrontSeoHead", pageMarkup, StringComparison.Ordinal);
            Assert.Contains("<StorefrontPageShell", pageMarkup, StringComparison.Ordinal);
            Assert.Contains("<Breadcrumb>", pageMarkup, StringComparison.Ordinal);
            Assert.Contains("<BreadcrumbNav", pageMarkup, StringComparison.Ordinal);
            Assert.Contains("<ChildContent>", pageMarkup, StringComparison.Ordinal);
            Assert.Contains("<h1", pageMarkup, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void StorefrontCategoryRouteAndView_SplitSeoFromVisualShell()
        {
            var routeMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages/Hybrid/Catalog/CategoryRoutePage.razor");
            var viewMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/CategoryPage.razor");

            Assert.Contains("<StorefrontPage TContext=\"StorefrontCategoryPageContext\"", routeMarkup, StringComparison.Ordinal);
            Assert.Contains("Metadata=\"_result.Metadata\"", routeMarkup, StringComparison.Ordinal);
            Assert.Contains("StorefrontCategoryPageService", routeMarkup, StringComparison.Ordinal);
            Assert.Contains("<StorefrontPageShell", viewMarkup, StringComparison.Ordinal);
            Assert.Contains("<Breadcrumb>", viewMarkup, StringComparison.Ordinal);
            Assert.Contains("<BreadcrumbNav", viewMarkup, StringComparison.Ordinal);
            Assert.Contains("<ChildContent>", viewMarkup, StringComparison.Ordinal);
            Assert.Contains("<h1", viewMarkup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<StorefrontSeoHead", viewMarkup, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontCatalogFilterPanel_PreservesQueryStringContract()
        {
            var filterMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/CatalogFilterPanel.razor");

            Assert.Contains("method=\"get\"", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("name=\"category\"", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("name=\"q\"", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("name=\"minPrice\"", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("name=\"maxPrice\"", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("name=\"sortBy\"", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("name=\"pageSize\"", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("name=\"inStock\"", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("ProductCatalogSortBy.DisplayOrder.ToApiValue()", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("ProductCatalogSortBy.PriceLowToHigh.ToApiValue()", filterMarkup, StringComparison.Ordinal);
            Assert.Contains("ProductCatalogSortBy.PriceHighToLow.ToApiValue()", filterMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("onclick", filterMarkup, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void StorefrontCategoryAndSearchPages_UseCatalogFilterPanelWithoutRouteChanges()
        {
            var categoryMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/CategoryPage.razor");
            var searchMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/SearchPage.razor");

            Assert.Contains("<CatalogFilterPanel", categoryMarkup, StringComparison.Ordinal);
            Assert.Contains("ShowPriceRange=\"true\"", categoryMarkup, StringComparison.Ordinal);
            Assert.Contains("ShowSort=\"true\"", categoryMarkup, StringComparison.Ordinal);
            Assert.Contains("ShowStock=\"true\"", categoryMarkup, StringComparison.Ordinal);
            Assert.Contains("MinPrice=\"Context.MinPrice\"", categoryMarkup, StringComparison.Ordinal);
            Assert.Contains("MaxPrice=\"Context.MaxPrice\"", categoryMarkup, StringComparison.Ordinal);
            Assert.Contains("ShowPageSize=\"true\"", categoryMarkup, StringComparison.Ordinal);
            Assert.Contains("PageSize=\"Context.PageSize\"", categoryMarkup, StringComparison.Ordinal);
            Assert.Contains("InStock=\"Context.InStock\"", categoryMarkup, StringComparison.Ordinal);
            Assert.Contains("Context.Links.CategoryUrl(Context.Slug, pageNumber, Context.PageSize, Context.SortBy, Context.MinPrice, Context.MaxPrice, Context.InStock ? true : null)", categoryMarkup, StringComparison.Ordinal);

            Assert.Contains("<CatalogFilterPanel", searchMarkup, StringComparison.Ordinal);
            Assert.Contains("Action=\"@Context.Links.Search.Href\"", searchMarkup, StringComparison.Ordinal);
            Assert.Contains("ShowCategory=\"true\"", searchMarkup, StringComparison.Ordinal);
            Assert.Contains("ShowSearch=\"true\"", searchMarkup, StringComparison.Ordinal);
            Assert.Contains("SearchTerm=\"@Context.Q\"", searchMarkup, StringComparison.Ordinal);
            Assert.Contains("<SubmitIcon>", searchMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("SubmitIconCssClass", searchMarkup, StringComparison.Ordinal);
            Assert.Contains("ShowPageSize=\"true\"", searchMarkup, StringComparison.Ordinal);
            Assert.Contains("Context.Links.SearchUrl(Context.Q, Context.Category, pageNumber, Context.PageSize, Context.SortBy, Context.MinPrice, Context.MaxPrice, Context.InStock ? true : null)", searchMarkup, StringComparison.Ordinal);
            Assert.Contains("CatalogSearchPolicy.MinimumSearchTermLength", searchMarkup, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontProgram_KeepsStaticAssetsAndFaviconFallback()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");
            var pipeline = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Hosting/StorefrontApplicationBuilderExtensions.cs");
            var options = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Options/StorefrontApplicationOptions.cs");

            Assert.Contains("app.UseStorefrontApplication();", program);
            Assert.Contains("app.UseStaticFiles();", pipeline);
            Assert.Contains("app.MapStaticAssets();", pipeline);
            Assert.Contains("public string FaviconRedirectPath { get; set; }", options);
            Assert.Contains("applicationOptions.FaviconRedirectPath", pipeline);
            Assert.Contains("app.MapGet(\"/favicon.ico\", () => Results.Redirect(applicationOptions.FaviconRedirectPath, permanent: false));", pipeline);
        }

        [Fact]
        public void StorefrontRuntime_DoesNotApplyImmutableCachePolicyToDynamicPipeline()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");
            var responseHeaders = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/PagePatterns/StorefrontResponseHeaders.cs");

            Assert.DoesNotContain("OnPrepareResponse", program, StringComparison.Ordinal);
            Assert.DoesNotContain("max-age=31536000, immutable", program, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("public const string ErrorCacheControl = \"no-store, no-cache, max-age=0\"", responseHeaders);
            Assert.Contains("public const string RobotsCacheControl = \"public, max-age=3600, must-revalidate\"", responseHeaders);
            Assert.Contains("public const string SitemapCacheControl = \"public, max-age=900, must-revalidate\"", responseHeaders);
            Assert.DoesNotContain("immutable", responseHeaders, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ControlPlaneRoot_DefinesExpectedAssetsWithoutDuplicates()
        {
            var indexMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/wwwroot/index.html");

            Assert.Equal(ControlPlaneRootStylesheetAllowlist, ExtractStylesheetHrefs(indexMarkup));
            Assert.Equal(ControlPlaneRootScriptAllowlist, ExtractScriptSources(indexMarkup));
            Assert.Contains("<script type=\"importmap\"></script>", indexMarkup);
            AssertRootDoesNotReferenceLegacyPresentationAssets(indexMarkup);
        }

        [Fact]
        public void ControlPlaneProject_KeepsDeterministicAssetBuildTargets()
        {
            var project = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj");

            Assert.Contains("Target Name=\"RestoreNodeModules\"", project);
            Assert.Contains("Target Name=\"CopyFontAwesomeAssets\"", project);
            Assert.Contains("Target Name=\"TailwindBuild\"", project);
            Assert.Contains("npm ci", project);
            Assert.Contains("npm run tailwind:build", project);
            Assert.Contains("@fortawesome\\fontawesome-free\\css\\all.min.css", project);
        }

        [Fact]
        public void ControlPlaneBrowserAssets_DoNotExposeCommerceNodeBoundaryDetails()
        {
            var root = FindRepositoryRoot();
            var assetFiles = Directory
                .EnumerateFiles(Path.Combine(root, "BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/wwwroot"), "*.*", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}vendor{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Where(path => Path.GetExtension(path) is ".html" or ".json" or ".js" or ".css")
                .ToArray();

            Assert.NotEmpty(assetFiles);
            foreach (var assetFile in assetFiles)
            {
                var content = File.ReadAllText(assetFile);

                Assert.DoesNotContain("localhost:5180", content, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("api/commerce", content, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("api/internal", content, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("X-Node-Key", content, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("X-Node-Secret", content, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("dev-node-secret", content, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void ControlPlaneDownloadsScript_StaysHostGlobalDownloadOnly()
        {
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/wwwroot/js/downloads.js");

            Assert.Contains("window.controlPlaneDownloads", script);
            Assert.Contains("downloadBytes", script);
            Assert.Contains("URL.createObjectURL", script);
            Assert.DoesNotContain("fetch(", script, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("eval(", script, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("api/", script, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ControlPlanePageHeader_DefinesOperationalHeaderExtensionPoint()
        {
            var component = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Components/ControlPlanePageHeader.razor");

            Assert.Contains("public string? Eyebrow", component);
            Assert.Contains("public string Title", component);
            Assert.Contains("public string? Description", component);
            Assert.Contains("public RenderFragment? Actions", component);
            Assert.Contains("[EditorRequired]", component);
            Assert.Contains("<h1 class=\"text-2xl font-bold text-ink-900\">@Title</h1>", component);
            Assert.DoesNotContain("cp-card", component, StringComparison.Ordinal);
            Assert.DoesNotContain("<main", component, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/Home.razor")]
        [InlineData("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/Stores.razor")]
        [InlineData("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/CommerceOrders.razor")]
        [InlineData("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/CommercePaymentMethods.razor")]
        public void ControlPlaneHighTrafficPages_UseSharedPageHeader(string relativePath)
        {
            var pageMarkup = ReadRepositoryFile(relativePath);

            Assert.Contains("<PageTitle>", pageMarkup, StringComparison.Ordinal);
            Assert.Contains("<ControlPlanePageHeader", pageMarkup, StringComparison.Ordinal);
            Assert.Contains("Title=", pageMarkup, StringComparison.Ordinal);
            Assert.Contains("<Actions>", pageMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("ControlPlane.Web -> CommerceNode", pageMarkup, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SharedV2BrowserHelpers_KeepJsModuleImports()
        {
            var sessionStorage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/BrowserStorage/BrowserSessionStorageService.cs");
            var cookieStorage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/CookieStorage/BrowserCookieStorageService.cs");
            var authSync = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/Authentication/AuthenticationSessionSyncService.cs");

            Assert.Contains("./js/sessionStorage.js", sessionStorage);
            Assert.Contains("./js/cookieStorage.js", cookieStorage);
            Assert.Contains("./js/authSessionSync.js", authSync);
        }

        [Fact]
        public void ArchitectureDocs_RecordStorefrontAssetOwnershipRules()
        {
            var projectGuide = ReadRepositoryFile("docs/architecture/05-project-and-folder-guide.md");
            var decisionRules = ReadRepositoryFile("docs/architecture/08-agent-decision-rules.md");

            Assert.Contains("Root Storefront CSS and scripts must stay explicit in `BlazorShop.Storefront.Presentation/App/StorefrontApp.razor` through host-provided head/script slots, and host-provided asset entries must resolve static web assets through Razor `@Assets[...]`", projectGuide);
            Assert.Contains("Storefront V2 host CSS owns `css/site.css`, Storefront V2.WASM interactive CSS owns `css/wasm-site.css`, and handwritten V2 structural overrides own `css/storefront.css`", projectGuide);
            Assert.Contains("`StorefrontIconHead` owns store favicon/png/apple/MS tile tags; `StorefrontBrandHead` owns non-icon storefront metadata such as the language marker.", projectGuide);
            Assert.Contains("Page-specific JavaScript should prefer `IJSRuntime` module imports.", projectGuide);
            Assert.Contains("Store configuration must not accept arbitrary public script or stylesheet injection.", projectGuide);
            Assert.Contains("Keep root CSS and script entries in `BlazorShop.Storefront.Presentation/App/StorefrontApp.razor` fingerprint-resolved through Razor `@Assets[...]` and allowlisted by tests.", decisionRules);
            Assert.Contains("Keep Storefront V2 host CSS `css/site.css`, Storefront V2.WASM interactive CSS `css/wasm-site.css`, then handwritten host CSS `css/storefront.css`", decisionRules);
            Assert.Contains("Keep `_framework/blazor.web.js`, then Presentation `_content/BlazorShop.Storefront.Presentation/js/storefront.application.js`, then host visual scripts such as `storefrontCommerce.js`; host visual scripts must also use `@Assets[...]`", decisionRules);
            Assert.Contains("Do not add DB-configured or store-configured arbitrary public scripts/styles.", decisionRules);
            Assert.Contains("Dynamic Storefront pages, maintenance pages, current-store/config reads, checkout/auth pages, SEO documents, and error states must not receive immutable cache headers.", decisionRules);
            Assert.Contains("Browser static assets and `wwwroot` config must point only to Control Plane API", decisionRules);
            Assert.Contains("Use `Web.SharedV2` as a forced visual design system", projectGuide);
            Assert.Contains("Share browser behavior helpers through `BlazorShop.Web.SharedV2` only when both active V2 frontends have a real use case.", decisionRules);
            Assert.Contains("Do not create a shared visual shell or asset registry just to reduce superficial markup similarity.", decisionRules);
        }

        private static IReadOnlyList<string> ExtractStylesheetHrefs(string markup)
        {
            return StylesheetRegex()
                .Matches(markup)
                .Select(match => match.Groups["href"].Value)
                .Pipe(AssertNoDuplicates)
                .ToArray();
        }

        private static IReadOnlyList<string> ExtractScriptSources(string markup)
        {
            return ScriptRegex()
                .Matches(markup)
                .Select(match => match.Groups["src"].Value)
                .Pipe(AssertNoDuplicates)
                .ToArray();
        }

        private static IReadOnlyList<string> ExtractAssetKeys(string markup)
        {
            return AssetKeyRegex()
                .Matches(markup)
                .Select(match => match.Groups["asset"].Value)
                .Pipe(AssertNoDuplicates)
                .ToArray();
        }

        private static void AssertAssetOrder(string markup, IReadOnlyList<string> assetKeys)
        {
            var previousIndex = -1;
            foreach (var assetKey in assetKeys)
            {
                var token = $"@Assets[\"{assetKey}\"]";
                var index = markup.IndexOf(token, StringComparison.Ordinal);
                Assert.True(index > previousIndex, $"{token} must appear after the previous asset.");
                previousIndex = index;
            }
        }

        private static IEnumerable<string> AssertNoDuplicates(IEnumerable<string> values)
        {
            var items = values.ToArray();
            var duplicates = items
                .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            Assert.Empty(duplicates);
            return items;
        }

        private static void AssertRootDoesNotReferenceLegacyPresentationAssets(string markup)
        {
            Assert.DoesNotContain("BlazorShop.Presentation", markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("../BlazorShop.Presentation", markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("api/internal/", markup, StringComparison.OrdinalIgnoreCase);
        }

        private static int CountOccurrences(string source, string value)
        {
            var count = 0;
            var index = 0;
            while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));
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

            throw new InvalidOperationException("Unable to locate BlazorShop.sln from the test output directory.");
        }

        [GeneratedRegex("<link\\s+[^>]*rel=\"stylesheet\"[^>]*href=\"(?<href>[^\"]+)\"", RegexOptions.IgnoreCase)]
        private static partial Regex StylesheetRegex();

        [GeneratedRegex("<script\\s+[^>]*src=\"(?<src>[^\"]+)\"", RegexOptions.IgnoreCase)]
        private static partial Regex ScriptRegex();

        [GeneratedRegex("@Assets\\[\"(?<asset>[^\"]+)\"\\]")]
        private static partial Regex AssetKeyRegex();
    }

    internal static class LayoutAssetFoundationEnumerableExtensions
    {
        public static TResult Pipe<TValue, TResult>(this TValue value, Func<TValue, TResult> next)
        {
            return next(value);
        }
    }
}
