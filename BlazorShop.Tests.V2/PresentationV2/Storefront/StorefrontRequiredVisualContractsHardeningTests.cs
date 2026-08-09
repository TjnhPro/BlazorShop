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

        [Fact]
        public void CartView_RequiresRootWiringWithoutOwningFallbackRoutesOrDescriptors()
        {
            var component = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartView.razor");

            foreach (var requiredParameter in new[]
            {
                "StorefrontBrowserCart? InitialCart",
                "IReadOnlyList<StorefrontBrowserCartAlert> InitialAlerts",
                "StorefrontFeatureDataMode DataMode",
                "StorefrontCartActionDescriptor Actions",
                "StorefrontCartViewClasses Classes",
                "string CheckoutUrl",
                "string ContinueShoppingUrl",
                "string SecondaryShoppingUrl"
            })
            {
                AssertParameterIsEditorRequired(component, requiredParameter);
            }

            Assert.DoesNotContain("InitialAlerts { get; set; } = []", component, StringComparison.Ordinal);
            Assert.DoesNotContain("DataMode { get; set; } = StorefrontFeatureDataMode.BrowserFetch", component, StringComparison.Ordinal);
            Assert.DoesNotContain("Actions { get; set; } = StorefrontCartActionDescriptor.Empty", component, StringComparison.Ordinal);
            Assert.DoesNotContain("Classes { get; set; } = StorefrontCartViewClasses.Empty", component, StringComparison.Ordinal);
            Assert.DoesNotContain("CheckoutUrl { get; set; } = \"/checkout\"", component, StringComparison.Ordinal);
            Assert.DoesNotContain("ContinueShoppingUrl { get; set; } = \"/search\"", component, StringComparison.Ordinal);
            Assert.DoesNotContain("SecondaryShoppingUrl { get; set; } = \"/\"", component, StringComparison.Ordinal);

            foreach (var validation in new[]
            {
                "ArgumentNullException.ThrowIfNull(InitialAlerts);",
                "ArgumentNullException.ThrowIfNull(Actions);",
                "ArgumentNullException.ThrowIfNull(Classes);",
                "ArgumentException.ThrowIfNullOrWhiteSpace(CheckoutUrl);",
                "ArgumentException.ThrowIfNullOrWhiteSpace(ContinueShoppingUrl);",
                "ArgumentException.ThrowIfNullOrWhiteSpace(SecondaryShoppingUrl);"
            })
            {
                Assert.Contains(validation, component, StringComparison.Ordinal);
            }

            Assert.DoesNotContain("ArgumentNullException.ThrowIfNull(InitialCart)", component, StringComparison.Ordinal);
            Assert.DoesNotContain("Actions == StorefrontCartActionDescriptor.Empty", component, StringComparison.Ordinal);
            Assert.DoesNotContain("Classes == StorefrontCartViewClasses.Empty", component, StringComparison.Ordinal);
            Assert.Contains("CartController.Initialize(InitialCart, InitialAlerts, DataMode, Actions);", component, StringComparison.Ordinal);
        }

        private static void AssertParameterIsEditorRequired(string source, string declaration)
        {
            Assert.Contains("[Parameter, EditorRequired]", source, StringComparison.Ordinal);
            Assert.Contains($"public {declaration}", source, StringComparison.Ordinal);
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
