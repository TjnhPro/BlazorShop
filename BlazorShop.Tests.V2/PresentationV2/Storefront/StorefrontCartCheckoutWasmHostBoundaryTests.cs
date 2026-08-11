namespace BlazorShop.Tests.PresentationV2.Storefront;

using Xunit;

public sealed class StorefrontWasmHostComponentOwnershipTests
{
    [Fact]
    public void WasmHostCartAndCheckoutStayInTheBrowserSafeWasmHostBoundary()
    {
        var project = StorefrontCartCheckoutWasmHostTestFiles.Read("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj");
        var sources = StorefrontCartCheckoutWasmHostTestFiles.ReadDirectory("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost");
        var cart = StorefrontCartCheckoutWasmHostTestFiles.Read("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Cart/StorefrontCartView.razor");
        var checkout = StorefrontCartCheckoutWasmHostTestFiles.Read("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Checkout/StorefrontCheckoutShell.razor");

        Assert.Contains("BlazorShop.Storefront.Components", project, StringComparison.Ordinal);
        Assert.Contains("BlazorShop.Storefront.Browser", project, StringComparison.Ordinal);

        foreach (var forbidden in new[]
        {
            "BlazorShop.Storefront.V2.WASM", "BlazorShop.Storefront.V2", "BlazorShop.Storefront.Runtime",
            "BlazorShop.Storefront.Client", "BlazorShop.CommerceNode", "BlazorShop.ControlPlane",
            "BlazorShop.Domain", "BlazorShop.Application", "BlazorShop.Infrastructure",
            "@rendermode", "InteractiveServer", "InteractiveWebAssembly", "InteractiveAuto"
        })
        {
            Assert.DoesNotContain(forbidden, sources, StringComparison.Ordinal);
        }

        Assert.Contains("@inject IStorefrontBrowserCartController CartController", cart, StringComparison.Ordinal);
        Assert.Contains("@inject IStorefrontBrowserCheckoutController CheckoutController", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/checkout\"", cart, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/search\"", cart, StringComparison.Ordinal);
    }
}

public sealed class StorefrontV2WasmWrapperBoundaryTests
{
    [Fact]
    public void CartAndCheckoutWrappersOnlySupplyV2OptionsToWasmHostComponents()
    {
        var cart = StorefrontCartCheckoutWasmHostTestFiles.Read("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartSection.razor");
        var checkout = StorefrontCartCheckoutWasmHostTestFiles.Read("BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutSection.razor");
        var wrappers = cart + checkout;

        Assert.Contains("BlazorShop.Storefront.Components.WasmHost.Components.Cart.StorefrontCartView", cart, StringComparison.Ordinal);
        Assert.Contains("StorefrontCartViewOptions.Classes", cart, StringComparison.Ordinal);
        Assert.Contains("StorefrontCartViewOptions.Labels", cart, StringComparison.Ordinal);
        Assert.Contains("CheckoutUrl=\"@CheckoutUrl\"", cart, StringComparison.Ordinal);
        Assert.Contains("ContinueShoppingUrl=\"@ContinueShoppingUrl\"", cart, StringComparison.Ordinal);
        Assert.Contains("SecondaryShoppingUrl=\"@SecondaryShoppingUrl\"", cart, StringComparison.Ordinal);
        Assert.Contains("BlazorShop.Storefront.Components.WasmHost.Components.Checkout.StorefrontCheckoutShell", checkout, StringComparison.Ordinal);
        Assert.Contains("StorefrontCheckoutShellOptions.Classes", checkout, StringComparison.Ordinal);
        Assert.Contains("StorefrontCheckoutShellOptions.Labels", checkout, StringComparison.Ordinal);

        foreach (var forbidden in new[]
        {
            "@inject IStorefrontBrowserCartController", "@inject IStorefrontBrowserCheckoutController",
            "HydrateAsync", "UpdateQuantityAsync", "ClearAsync", "RefreshAsync", "ReviewAsync", "PlaceOrderAsync"
        })
        {
            Assert.DoesNotContain(forbidden, wrappers, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void V2PagesKeepRenderModeAndServerRenderedCheckoutFormOwnership()
    {
        var cartPage = StorefrontCartCheckoutWasmHostTestFiles.Read("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor");
        var checkoutPage = StorefrontCartCheckoutWasmHostTestFiles.Read("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor");

        Assert.Contains("<StorefrontCartSection", cartPage, StringComparison.Ordinal);
        Assert.Contains("@rendermode=\"InteractiveWebAssembly\"", cartPage, StringComparison.Ordinal);
        Assert.Equal(2, StorefrontCartCheckoutWasmHostTestFiles.Count(checkoutPage, "<StorefrontCheckoutSection"));
        Assert.Equal(2, StorefrontCartCheckoutWasmHostTestFiles.Count(checkoutPage, "ShowPanel=\"false\""));
        Assert.Equal(2, StorefrontCartCheckoutWasmHostTestFiles.Count(checkoutPage, "@rendermode=\"InteractiveWebAssembly\""));
        Assert.Contains("<StorefrontCheckoutForm", checkoutPage, StringComparison.Ordinal);
        Assert.Contains("<StorefrontCheckoutAddressFields", checkoutPage, StringComparison.Ordinal);
        Assert.Contains("<StorefrontCheckoutPaymentFields", checkoutPage, StringComparison.Ordinal);
        Assert.Contains("<StorefrontCheckoutSubmit", checkoutPage, StringComparison.Ordinal);
    }
}

public sealed class StorefrontSharedContractOwnershipTests
{
    [Fact]
    public void CartAndCheckoutClassAndLabelContractsRemainInBaseComponents()
    {
        var componentsProject = StorefrontCartCheckoutWasmHostTestFiles.Read("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj");
        var contracts = StorefrontCartCheckoutWasmHostTestFiles.ReadDirectory("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts");

        Assert.DoesNotContain("<ProjectReference", componentsProject, StringComparison.Ordinal);

        foreach (var contract in new[]
        {
            "Contracts/Cart/StorefrontCartViewClasses.cs", "Contracts/Cart/StorefrontCartViewLabels.cs",
            "Contracts/Checkout/StorefrontCheckoutViewClasses.cs", "Contracts/Checkout/StorefrontCheckoutViewLabels.cs"
        })
        {
            Assert.True(File.Exists(StorefrontCartCheckoutWasmHostTestFiles.Path($"BlazorShop.PresentationV2/BlazorShop.Storefront.Components/{contract}")), $"Missing shared contract: {contract}");
        }

        foreach (var forbidden in new[]
        {
            "BlazorShop.Storefront.Browser", "BlazorShop.Storefront.V2", "BlazorShop.Storefront.Runtime",
            "BlazorShop.Storefront.Client", "BlazorShop.CommerceNode", "BlazorShop.ControlPlane",
            "BlazorShop.Domain", "BlazorShop.Application", "BlazorShop.Infrastructure"
        })
        {
            Assert.DoesNotContain(forbidden, contracts, StringComparison.Ordinal);
        }
    }
}

internal static class StorefrontCartCheckoutWasmHostTestFiles
{
    public static string Read(string relativePath) => File.ReadAllText(Path(relativePath));

    public static string ReadDirectory(string relativePath)
    {
        return string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(Path(relativePath), "*.*", SearchOption.AllDirectories)
                .Where(file => System.IO.Path.GetExtension(file) is ".cs" or ".razor" or ".csproj")
                .Where(file => !file.Contains($"{System.IO.Path.DirectorySeparatorChar}bin{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Where(file => !file.Contains($"{System.IO.Path.DirectorySeparatorChar}obj{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal)
                .Select(File.ReadAllText));
    }

    public static int Count(string source, string value)
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

    public static string Path(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = System.IO.Path.Combine(directory.FullName, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate repository path '{relativePath}'.");
    }
}
