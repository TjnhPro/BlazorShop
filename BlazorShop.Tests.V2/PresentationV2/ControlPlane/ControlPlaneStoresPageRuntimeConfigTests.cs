namespace BlazorShop.Tests.PresentationV2.ControlPlane
{
    using Xunit;

    public sealed class ControlPlaneStoresPageRuntimeConfigTests
    {
        [Fact]
        public void StoresPage_ExposesRuntimeStoreConfigSectionsAndFields()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/Stores.razor");

            foreach (var section in new[] { "Identity", "Branding", "Locale", "Contact", "Availability", "Advanced URLs" })
            {
                Assert.Contains(section, markup);
            }

            foreach (var field in new[]
            {
                "runtimeName",
                "runtimeLogoUrl",
                "runtimeFaviconUrl",
                "runtimePngIconUrl",
                "runtimeAppleTouchIconUrl",
                "runtimeMsTileImageUrl",
                "runtimeMsTileColor",
                "runtimeDefaultCurrencyCode",
                "runtimeDefaultCulture",
                "runtimeCdnHost",
            })
            {
                Assert.Contains(field, markup);
            }

            Assert.Contains("Existing carts, orders, and payment attempts keep their stored currency.", markup);
        }

        [Fact]
        public void StoresPage_SavesRuntimeConfigThroughControlPlaneClientOnly()
        {
            var markup = ReadControlPlanePageSource("Stores");

            Assert.Contains("StoreClient.UpdateRuntimeStoreAsync", markup);
            Assert.Contains("EmptyToNull(runtimeLogoUrl)", markup);
            Assert.Contains("EmptyToNull(runtimeFaviconUrl)", markup);
            Assert.Contains("runtimeDefaultCurrencyCode.Trim().ToUpperInvariant()", markup);
            Assert.DoesNotContain("CommerceNode.API", markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("api/commerce/admin/stores", markup, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void StoresPage_UsesSafePreviewGuardsForImageAndTileColor()
        {
            var markup = ReadControlPlanePageSource("Stores");

            Assert.Contains("CanPreviewImage(runtimeLogoUrl)", markup);
            Assert.Contains("CanPreviewImage(runtimeFaviconUrl)", markup);
            Assert.Contains("CanPreviewTileColor(runtimeMsTileColor)", markup);
            Assert.Contains("transparent", markup);
            Assert.Contains("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", markup);
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));
        }

        private static string ReadControlPlanePageSource(string pageName)
        {
            return ReadRepositoryFile($"BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/{pageName}.razor")
                + Environment.NewLine
                + ReadRepositoryFile($"BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/{pageName}.razor.cs");
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
    }
}
