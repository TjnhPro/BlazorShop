namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontIconHeadMarkupTests
    {
        [Fact]
        public void StorefrontIconHead_LivesInPresentationAndHasNoServiceInjection()
        {
            var presentationPath = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Head/StorefrontIconHead.razor");
            var v2Path = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Seo/StorefrontIconHead.razor");
            var markup = File.ReadAllText(presentationPath);

            Assert.True(File.Exists(presentationPath));
            Assert.False(File.Exists(v2Path));
            Assert.DoesNotContain("@inject", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("IJSRuntime", markup, StringComparison.Ordinal);
            Assert.Contains("public StorefrontDisplayContext DisplayContext", markup);
        }

        [Fact]
        public void StorefrontIconHead_RendersPrimaryFaviconWithFaviconPrecedence()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Head/StorefrontIconHead.razor");

            Assert.Contains("if (!string.IsNullOrWhiteSpace(DisplayContext.FaviconUrl))", markup, StringComparison.Ordinal);
            Assert.Contains("<link rel=\"icon\" href=\"@DisplayContext.FaviconUrl\" />", markup, StringComparison.Ordinal);
            Assert.Contains("else if (!string.IsNullOrWhiteSpace(DisplayContext.PngIconUrl))", markup, StringComparison.Ordinal);
            Assert.Contains("<link rel=\"icon\" type=\"image/png\" href=\"@DisplayContext.PngIconUrl\" />", markup, StringComparison.Ordinal);
            Assert.True(
                markup.IndexOf("DisplayContext.FaviconUrl", StringComparison.Ordinal) <
                markup.IndexOf("DisplayContext.PngIconUrl", StringComparison.Ordinal));
        }

        [Fact]
        public void StorefrontIconHead_RendersAppleAndMicrosoftIconMetadataIndependently()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Head/StorefrontIconHead.razor");

            Assert.Contains("if (!string.IsNullOrWhiteSpace(DisplayContext.AppleTouchIconUrl))", markup, StringComparison.Ordinal);
            Assert.Contains("<link rel=\"apple-touch-icon\" href=\"@DisplayContext.AppleTouchIconUrl\" />", markup, StringComparison.Ordinal);
            Assert.Contains("if (!string.IsNullOrWhiteSpace(DisplayContext.MsTileImageUrl))", markup, StringComparison.Ordinal);
            Assert.Contains("<meta name=\"msapplication-TileImage\" content=\"@DisplayContext.MsTileImageUrl\" />", markup, StringComparison.Ordinal);
            Assert.Contains("if (!string.IsNullOrWhiteSpace(DisplayContext.MsTileColor))", markup, StringComparison.Ordinal);
            Assert.Contains("<meta name=\"msapplication-TileColor\" content=\"@DisplayContext.MsTileColor\" />", markup, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontIconHead_EmitsNoIconTagsWithoutConfiguredIconValues()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Head/StorefrontIconHead.razor");

            Assert.Equal(5, CountOccurrences(markup, "string.IsNullOrWhiteSpace(DisplayContext."));
            Assert.DoesNotContain("<link rel=\"icon\" href=\"@StorefrontDisplayContext.Fallback", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("<link rel=\"apple-touch-icon\" href=\"@StorefrontDisplayContext.Fallback", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("<meta name=\"msapplication-TileImage\" content=\"@StorefrontDisplayContext.Fallback", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("<meta name=\"msapplication-TileColor\" content=\"@StorefrontDisplayContext.Fallback", markup, StringComparison.Ordinal);
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
            return File.ReadAllText(RepositoryPath(relativePath));
        }

        private static string RepositoryPath(string relativePath)
        {
            return Path.Combine(FindRepositoryRoot(), relativePath);
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
