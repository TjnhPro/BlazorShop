namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Components.Contracts.Product;
using BlazorShop.Storefront.Components.Contracts.System;
using BlazorShop.Storefront.Components.Ssr.Security;

using Xunit;

public sealed class StorefrontSharedSemanticRenderContractTests
{
    [Fact]
    public void PurchaseLabelsExtendTheExistingContractAndKeepEmptyValuesNeutral()
    {
        Assert.Equal(
            ["AddToCart", "AddedToCart", "ViewCart", "FreeShipping", "Optional", "PurchaseHeading", "ChooseVariant", "SelectVariant", "Quantity", "SelectOptionFormat"],
            InstanceProperties(typeof(ProductPurchaseLabels)).Select(property => property.Name).ToArray());
        Assert.All(
            InstanceProperties(typeof(ProductPurchaseLabels)).Select(property => property.GetValue(ProductPurchaseLabels.Empty)),
            value => Assert.Equal(string.Empty, value));
    }

    [Theory]
    [InlineData(typeof(ProductPurchasePanelClasses))]
    [InlineData(typeof(StorefrontToastRegionClasses))]
    [InlineData(typeof(StorefrontConsentPanelClasses))]
    public void SharedClassSlotContractsDefaultToEmptyValues(global::System.Type contractType)
    {
        var constructor = contractType.GetConstructors().Single();
        var instance = constructor.Invoke(Enumerable.Repeat<object>(string.Empty, constructor.GetParameters().Length).ToArray());

        Assert.All(
            InstanceProperties(contractType).Select(property => property.GetValue(instance)),
            value => Assert.Equal(string.Empty, value));
    }

    [Theory]
    [InlineData(typeof(StorefrontToastRegionLabels))]
    [InlineData(typeof(StorefrontConsentPanelLabels))]
    public void LabelContractsExposeNeutralEmptyValues(global::System.Type contractType)
    {
        var empty = contractType.GetProperty("Empty")!.GetValue(null)!;

        Assert.All(
            InstanceProperties(contractType).Select(property => property.GetValue(empty)),
            value => Assert.Equal(string.Empty, value));
    }

    [Theory]
    [InlineData("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Product/ProductPurchasePanelClasses.cs")]
    [InlineData("BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/System/StorefrontToastRegionClasses.cs")]
    [InlineData("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Security/StorefrontConsentPanelClasses.cs")]
    public void ClassSlotContractsDoNotContainFinalVisualValues(string relativePath)
    {
        var source = File.ReadAllText(RepositoryPath(relativePath));

        foreach (var token in new[] { "bg-", "text-", "rounded", "shadow", "px-", "bs-" })
        {
            Assert.DoesNotContain(token, source, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static IEnumerable<global::System.Reflection.PropertyInfo> InstanceProperties(global::System.Type type)
    {
        return type.GetProperties().Where(property => !property.GetMethod!.IsStatic);
    }

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
