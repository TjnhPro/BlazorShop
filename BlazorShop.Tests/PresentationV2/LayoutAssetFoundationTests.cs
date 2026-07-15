namespace BlazorShop.Tests.PresentationV2
{
    using System.Text.RegularExpressions;

    using Xunit;

    public sealed partial class LayoutAssetFoundationTests
    {
        [Fact]
        public void StorefrontRoot_DefinesExpectedAssetsWithoutDuplicates()
        {
            var appMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/App.razor");

            Assert.Equal(["css/site.css", "css/storefront.css"], ExtractStylesheetHrefs(appMarkup));
            Assert.Equal(["_framework/blazor.web.js", "js/storefrontCommerce.js"], ExtractScriptSources(appMarkup));
            Assert.Contains("<link rel=\"icon\" type=\"image/png\" href=\"icon-192.png\" />", appMarkup);
            Assert.True(
                appMarkup.IndexOf("<StorefrontBrandHead />", StringComparison.Ordinal) <
                appMarkup.IndexOf("<HeadOutlet />", StringComparison.Ordinal));
            Assert.True(
                appMarkup.IndexOf("_framework/blazor.web.js", StringComparison.Ordinal) <
                appMarkup.IndexOf("js/storefrontCommerce.js", StringComparison.Ordinal));
            AssertRootDoesNotReferenceLegacyPresentationAssets(appMarkup);
        }

        [Fact]
        public void StorefrontLayout_KeepsSingleToastRegionAndGlobalShell()
        {
            var layoutMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor");
            var brandHeadMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Seo/StorefrontBrandHead.razor");

            Assert.Contains("<StorefrontHeader />", layoutMarkup);
            Assert.Contains("<main class=\"bs-storefront-main flex-1\">", layoutMarkup);
            Assert.Contains("<StorefrontFooter />", layoutMarkup);
            Assert.Equal(1, CountOccurrences(layoutMarkup, "data-storefront-toast-region"));
            Assert.DoesNotContain("<HeadContent>", layoutMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("<HeadContent>", brandHeadMarkup, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontProgram_KeepsStaticAssetMiddleware()
        {
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");

            Assert.Contains("app.UseStaticFiles();", program);
            Assert.Contains("app.MapStaticAssets();", program);
        }

        [Fact]
        public void ControlPlaneRoot_DefinesExpectedAssetsWithoutDuplicates()
        {
            var indexMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/wwwroot/index.html");

            Assert.Equal(
                ["vendor/fontawesome/css/all.min.css", "css/site.css", "css/app.css"],
                ExtractStylesheetHrefs(indexMarkup));
            Assert.Equal(["_framework/blazor.webassembly.js", "js/downloads.js"], ExtractScriptSources(indexMarkup));
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
        public void SharedV2BrowserHelpers_KeepJsModuleImports()
        {
            var sessionStorage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/BrowserStorage/BrowserSessionStorageService.cs");
            var cookieStorage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/CookieStorage/BrowserCookieStorageService.cs");
            var authSync = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/Authentication/AuthenticationSessionSyncService.cs");

            Assert.Contains("./js/sessionStorage.js", sessionStorage);
            Assert.Contains("./js/cookieStorage.js", cookieStorage);
            Assert.Contains("./js/authSessionSync.js", authSync);
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
    }

    internal static class LayoutAssetFoundationEnumerableExtensions
    {
        public static TResult Pipe<TValue, TResult>(this TValue value, Func<TValue, TResult> next)
        {
            return next(value);
        }
    }
}
