namespace BlazorShop.Tests.PresentationV2.Storefront;

using Xunit;

public sealed class StorefrontAccountWasmHostOwnershipTests
{
    private const string WasmHostAccountRoot = "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Account";
    private const string V2WasmAccountRoot = "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account";

    [Fact]
    public void AccountRuntimeLeaves_AreOwnedByWasmHostWithSharedContracts()
    {
        var contracts = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Account/StorefrontAccountViewClasses.cs")
            + ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Account/StorefrontAccountLabels.cs");

        foreach (var typeName in new[]
        {
            "StorefrontAccountFormClasses",
            "StorefrontAccountAddressBookClasses",
            "StorefrontAccountOrderListClasses",
            "StorefrontAccountOrderDetailClasses",
            "StorefrontAccountProfileLabels",
            "StorefrontAccountPasswordLabels",
            "StorefrontAccountAddressBookLabels",
            "StorefrontAccountOrderListLabels",
            "StorefrontAccountOrderDetailLabels"
        })
        {
            Assert.Contains(typeName, contracts, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("Profile could not be loaded.", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("Save profile", contracts, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("StorefrontAccountProfileEditor.razor", "data-storefront-account-profile", "InitializeProfile", "HydrateProfileAsync", "SaveProfileAsync")]
    [InlineData("StorefrontAccountChangePasswordForm.razor", "data-storefront-account-password", "InitializePassword", "ChangePasswordAsync")]
    [InlineData("StorefrontAccountAddressBook.razor", "data-storefront-account-addresses", "InitializeAddresses", "HydrateAddressesAsync", "CreateAddressAsync", "UpdateAddressAsync", "DeleteAddressAsync", "SetDefaultAddressAsync")]
    [InlineData("StorefrontAccountOrderList.razor", "data-storefront-account-orders", "InitializeOrders", "HydrateOrdersAsync")]
    [InlineData("StorefrontAccountOrderDetail.razor", "data-storefront-account-order-detail", "InitializeOrderDetail", "HydrateOrderDetailAsync")]
    public void AccountRuntimeLeaf_UsesBrowserControllerWithoutForbiddenDependencies(
        string fileName,
        string semanticHook,
        params string[] lifecycleMethods)
    {
        var source = ReadRepositoryFile($"{WasmHostAccountRoot}/{fileName}");

        Assert.Contains("IStorefrontBrowserAccountController", source, StringComparison.Ordinal);
        Assert.Contains(semanticHook, source, StringComparison.Ordinal);
        Assert.DoesNotContain("@rendermode", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StorefrontAccountViewOptions", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("api/storefront", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CommerceNode", source, StringComparison.OrdinalIgnoreCase);

        foreach (var lifecycleMethod in lifecycleMethods)
        {
            Assert.Contains(lifecycleMethod, source, StringComparison.Ordinal);
        }

        Assert.False(File.Exists(RepositoryPath($"{V2WasmAccountRoot}/{fileName}")));
    }

    [Fact]
    public void V2WasmRetainsOnlyAccountCompositionAndNavigation()
    {
        var app = ReadRepositoryFile($"{V2WasmAccountRoot}/StorefrontAccountApp.razor");

        Assert.True(File.Exists(RepositoryPath($"{V2WasmAccountRoot}/StorefrontAccountNavigation.razor")));
        Assert.DoesNotContain("IStorefrontBrowserAccountController", app, StringComparison.Ordinal);

        foreach (var lifecycleMethod in new[]
        {
            "InitializeProfile", "HydrateProfileAsync", "SaveProfileAsync", "InitializePassword", "ChangePasswordAsync",
            "InitializeAddresses", "HydrateAddressesAsync", "CreateAddressAsync", "UpdateAddressAsync", "DeleteAddressAsync", "SetDefaultAddressAsync",
            "InitializeOrders", "HydrateOrdersAsync", "InitializeOrderDetail", "HydrateOrderDetailAsync"
        })
        {
            Assert.DoesNotContain(lifecycleMethod, app, StringComparison.Ordinal);
        }
    }

    private static string ReadRepositoryFile(string relativePath) => File.ReadAllText(RepositoryPath(relativePath));

    private static string RepositoryPath(string relativePath) => Path.Combine(
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../")),
        relativePath.Replace('/', Path.DirectorySeparatorChar));
}
