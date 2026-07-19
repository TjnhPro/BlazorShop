namespace BlazorShop.Tests.PresentationV2.ControlPlane
{
    using Xunit;

    public sealed class ControlPlaneVariantAttributeWorkflowTests
    {
        [Fact]
        public void VariationTemplateManager_ExposesOptionMetadataControls()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/CommerceVariationTemplates.razor");

            Assert.Contains("@bind=\"optionForm.ControlType\"", markup, StringComparison.Ordinal);
            Assert.Contains("@bind=\"optionForm.IsRequired\"", markup, StringComparison.Ordinal);
            Assert.Contains("@bind=\"optionForm.IsActive\"", markup, StringComparison.Ordinal);
            Assert.Contains("GetValueColorHex", markup, StringComparison.Ordinal);
            Assert.Contains("NormalizeColorHexForOption", markup, StringComparison.Ordinal);
        }

        [Fact]
        public void ProductManager_ShowsVariantCombinationWorkflowState()
        {
            var pageSource = ReadCommerceProductsSource();

            Assert.Contains("Combination signature:", pageSource, StringComparison.Ordinal);
            Assert.Contains("<StatusBadge Text=\"Default\"", pageSource, StringComparison.Ordinal);
            Assert.Contains("GetVariantWorkflowWarnings", pageSource, StringComparison.Ordinal);
            Assert.Contains("Required option", pageSource, StringComparison.Ordinal);
            Assert.Contains("is no longer active in the template", pageSource, StringComparison.Ordinal);
            Assert.Contains("Needs review", pageSource, StringComparison.Ordinal);
            Assert.Contains("SetVariantActiveAsync", pageSource, StringComparison.Ordinal);
        }

        [Fact]
        public void ProductManager_ExposesAvailabilityQuantityWorkflowControls()
        {
            var pageSource = ReadCommerceProductsSource();

            Assert.Contains("Availability & purchase", pageSource, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.MinOrderQuantity\"", pageSource, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.MaxOrderQuantity\"", pageSource, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.QuantityStep\"", pageSource, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.PurchasingDisabled\"", pageSource, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.PurchasingDisabledReason\"", pageSource, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.ManageStock\"", pageSource, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.HideWhenOutOfStock\"", pageSource, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.ShippingRequired\"", pageSource, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.FreeShipping\"", pageSource, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.DeliveryEstimateText\"", pageSource, StringComparison.Ordinal);
            Assert.Contains("MinOrderQuantity = basicForm.MinOrderQuantity", pageSource, StringComparison.Ordinal);
            Assert.Contains("PurchasingDisabled = basicForm.PurchasingDisabled", pageSource, StringComparison.Ordinal);
            Assert.Contains("DeliveryEstimateText = basicForm.DeliveryEstimateText", pageSource, StringComparison.Ordinal);
        }

        [Fact]
        public void ProductManager_ShowsAvailabilityQuantityWorkflowState()
        {
            var pageSource = ReadCommerceProductsSource();

            Assert.Contains("InventoryStatusLabel", pageSource, StringComparison.Ordinal);
            Assert.Contains("Low stock", pageSource, StringComparison.Ordinal);
            Assert.Contains("Out of stock", pageSource, StringComparison.Ordinal);
            Assert.Contains("Stock unmanaged", pageSource, StringComparison.Ordinal);
            Assert.Contains("Purchase paused", pageSource, StringComparison.Ordinal);
            Assert.Contains("Hide when out", pageSource, StringComparison.Ordinal);
            Assert.Contains("PurchasingDisabledReason", pageSource, StringComparison.Ordinal);
            Assert.Contains("AdminInventoryItemDto?", pageSource, StringComparison.Ordinal);
            Assert.Contains("AdminInventoryVariantDto", pageSource, StringComparison.Ordinal);
        }

        [Fact]
        public void ControlPlaneWeb_UsesControlPlaneCommerceGatewayRoutesOnly()
        {
            var client = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Services/Commerce/ControlPlaneCommerceClientBase.cs");
            var webSource = ReadControlPlaneWebSource();

            Assert.Contains("api/controlplane/commerce/stores/{storePublicId:D}", client, StringComparison.Ordinal);
            Assert.DoesNotContain("@inject IControlPlane" + "CatalogClient", webSource, StringComparison.Ordinal);
            Assert.DoesNotContain("CatalogClient.", webSource, StringComparison.Ordinal);
            Assert.DoesNotContain("api/commerce", webSource, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("api/storefront", webSource, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CommerceNodeApi", webSource, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("X-Node-Key", webSource, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("X-Node-Secret", webSource, StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadControlPlaneWebSource()
        {
            var root = FindRepositoryRoot();
            var webRoot = Path.Combine(root.FullName, "BlazorShop.PresentationV2", "BlazorShop.ControlPlane.Web");
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".cs",
                ".razor",
                ".json",
            };

            var files = Directory.EnumerateFiles(webRoot, "*", SearchOption.AllDirectories)
                .Where(path => extensions.Contains(Path.GetExtension(path)))
                .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                                    || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)));

            return string.Join(Environment.NewLine, files.Select(File.ReadAllText));
        }

        private static string ReadCommerceProductsSource()
        {
            return ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/CommerceProducts.razor")
                + Environment.NewLine
                + ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/CommerceProducts.razor.cs");
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName, relativePath));
        }

        private static DirectoryInfo FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null && !File.Exists(Path.Combine(current.FullName, "BlazorShop.sln")))
            {
                current = current.Parent;
            }

            Assert.NotNull(current);
            return current!;
        }
    }
}
