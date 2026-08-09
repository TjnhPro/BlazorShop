namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontRequiredVisualContractsHardeningTests
    {
        [Fact]
        public void CartPage_RequiresPresentationOwnedContextAndPassesCartRootContracts()
        {
            var page = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor");

            Assert.Contains("[Parameter, EditorRequired]", page, StringComparison.Ordinal);
            Assert.Contains("public StorefrontCartPageContext Context { get; set; } = default!;", page, StringComparison.Ordinal);
            Assert.Contains("ArgumentNullException.ThrowIfNull(Context);", page, StringComparison.Ordinal);
            Assert.DoesNotContain("new StorefrontCartPageContext", page, StringComparison.Ordinal);
            Assert.DoesNotContain("= new(", page, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontLinkContext.Default", page, StringComparison.Ordinal);

            foreach (var requiredAttribute in new[]
            {
                "InitialCart=\"Context.Cart\"",
                "InitialAlerts=\"Context.Alerts\"",
                "DataMode=\"StorefrontFeatureDataMode.InitialSnapshot\"",
                "Actions=\"@Context.CartActions\"",
                "Classes=\"StorefrontCartViewOptions.Classes\"",
                "CheckoutUrl=\"@Context.CheckoutUrl\"",
                "ContinueShoppingUrl=\"@Context.ContinueShoppingUrl\"",
                "SecondaryShoppingUrl=\"@Context.Links.Home.Href\""
            })
            {
                Assert.Contains(requiredAttribute, page, StringComparison.Ordinal);
            }
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(
                RepositoryRoot(),
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
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
