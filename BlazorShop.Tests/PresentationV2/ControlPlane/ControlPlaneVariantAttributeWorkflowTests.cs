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
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/CommerceProducts.razor");

            Assert.Contains("Combination signature:", markup, StringComparison.Ordinal);
            Assert.Contains("<StatusBadge Text=\"Default\"", markup, StringComparison.Ordinal);
            Assert.Contains("GetVariantWorkflowWarnings", markup, StringComparison.Ordinal);
            Assert.Contains("Required option", markup, StringComparison.Ordinal);
            Assert.Contains("is no longer active in the template", markup, StringComparison.Ordinal);
            Assert.Contains("Needs review", markup, StringComparison.Ordinal);
            Assert.Contains("SetVariantActiveAsync", markup, StringComparison.Ordinal);
        }

        [Fact]
        public void ProductManager_ExposesAvailabilityQuantityWorkflowControls()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/CommerceProducts.razor");

            Assert.Contains("Availability & purchase", markup, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.MinOrderQuantity\"", markup, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.MaxOrderQuantity\"", markup, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.QuantityStep\"", markup, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.PurchasingDisabled\"", markup, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.PurchasingDisabledReason\"", markup, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.ManageStock\"", markup, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.HideWhenOutOfStock\"", markup, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.ShippingRequired\"", markup, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.FreeShipping\"", markup, StringComparison.Ordinal);
            Assert.Contains("@bind=\"basicForm.DeliveryEstimateText\"", markup, StringComparison.Ordinal);
            Assert.Contains("MinOrderQuantity = basicForm.MinOrderQuantity", markup, StringComparison.Ordinal);
            Assert.Contains("PurchasingDisabled = basicForm.PurchasingDisabled", markup, StringComparison.Ordinal);
            Assert.Contains("DeliveryEstimateText = basicForm.DeliveryEstimateText", markup, StringComparison.Ordinal);
        }

        [Fact]
        public void ProductManager_ShowsAvailabilityQuantityWorkflowState()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/CommerceProducts.razor");

            Assert.Contains("InventoryStatusLabel", markup, StringComparison.Ordinal);
            Assert.Contains("Low stock", markup, StringComparison.Ordinal);
            Assert.Contains("Out of stock", markup, StringComparison.Ordinal);
            Assert.Contains("Stock unmanaged", markup, StringComparison.Ordinal);
            Assert.Contains("Purchase paused", markup, StringComparison.Ordinal);
            Assert.Contains("Hide when out", markup, StringComparison.Ordinal);
            Assert.Contains("PurchasingDisabledReason", markup, StringComparison.Ordinal);
            Assert.Contains("AdminInventoryItemDto?", markup, StringComparison.Ordinal);
            Assert.Contains("AdminInventoryVariantDto", markup, StringComparison.Ordinal);
        }

        [Fact]
        public void ControlPlaneWeb_UsesControlPlaneCommerceGatewayRoutesOnly()
        {
            var client = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Services/Catalog/ControlPlaneCatalogClient.cs");
            var webSource = ReadControlPlaneWebSource();

            Assert.Contains("api/controlplane/commerce/stores/{storePublicId:D}", client, StringComparison.Ordinal);
            Assert.DoesNotContain("api/commerce", webSource, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("api/storefront", webSource, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CommerceNodeApi", webSource, StringComparison.OrdinalIgnoreCase);
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
