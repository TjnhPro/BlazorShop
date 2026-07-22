namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using Xunit;

    public sealed class MediaDeliveryHardeningTests
    {
        [Fact]
        public void ProductMediaController_UsesShortCacheForUnversionedRequests()
        {
            var source = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/ProductMediaController.cs");

            Assert.Contains("requestedVersion is > 0", source);
            Assert.Contains("public, max-age=31536000, immutable", source);
            Assert.Contains("public, max-age=3600", source);
            Assert.Contains("XContentTypeOptions = \"nosniff\"", source);
        }

        [Fact]
        public void ProductMediaController_ReturnsNotFoundBeforeImgproxyWhenOriginalFileIsMissing()
        {
            var source = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/ProductMediaController.cs");

            Assert.True(
                source.IndexOf("this.storageProvider.FileExists", StringComparison.Ordinal)
                < source.IndexOf("BuildImgproxyUrl", StringComparison.Ordinal));
            Assert.Contains("return this.NotFound();", source);
        }

        [Fact]
        public void StorefrontMediaProxy_CopiesNoSniffHeader()
        {
            var source = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Media/StorefrontMediaProxyService.cs");

            Assert.Contains("CopyHeaderIfPresent(response, httpContext.Response, \"X-Content-Type-Options\")", source);
        }

        [Fact]
        public void StorefrontMediaProxy_ForwardsPublicHostForMediaScope()
        {
            var source = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Media/StorefrontMediaProxyService.cs");

            Assert.Contains("request.Headers.Host = publicHost", source);
            Assert.DoesNotContain("X-Store-Key", source);
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
    }
}
