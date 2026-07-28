namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Presentation.Services.Product;
using Xunit;

public sealed class StorefrontProductDecisionContextTests
{
    [Fact]
    public void ProductPageMapper_ConvertsPurchaseBlockReasonToDisplayMessage()
    {
        var context = MapProduct(new GetProduct
        {
            Id = Guid.NewGuid(),
            Name = "Paused product",
            Price = 10m,
            Purchasable = false,
            PurchaseBlockReasons = ["purchase_disabled"],
            CreatedOn = DateTime.UtcNow.AddDays(-30),
        });

        Assert.Equal("Purchasing is paused.", context.Purchase.PurchaseBlockMessage);
        Assert.Equal("Purchasing is paused.", context.PurchasePanel.PurchaseBlockMessage);
        Assert.Contains("Purchasing is paused.", context.PurchasePanel.InitialValidationMessages);
    }

    [Fact]
    public void ProductPageMapper_SetsAddToCartDisabledForHardBlock()
    {
        var context = MapProduct(new GetProduct
        {
            Id = Guid.NewGuid(),
            Name = "Sold out product",
            Price = 10m,
            Purchasable = true,
            PurchaseBlockReasons = ["out_of_stock"],
            Quantity = 0,
            CreatedOn = DateTime.UtcNow.AddDays(-30),
        });

        Assert.False(context.Purchase.CanAddToCart);
        Assert.False(context.PurchasePanel.CanSubmitInitialPurchase);
    }

    [Fact]
    public void ProductPageMapper_FormatsComparePriceOnlyWhenGreaterThanDisplayPrice()
    {
        var saleContext = MapProduct(new GetProduct
        {
            Id = Guid.NewGuid(),
            Name = "Sale product",
            Price = 10m,
            DisplayPrice = 8m,
            DisplayComparePrice = 12m,
            Purchasable = true,
            CreatedOn = DateTime.UtcNow.AddDays(-30),
        });

        var nonSaleContext = MapProduct(new GetProduct
        {
            Id = Guid.NewGuid(),
            Name = "Non sale product",
            Price = 10m,
            DisplayPrice = 8m,
            DisplayComparePrice = 7m,
            Purchasable = true,
            CreatedOn = DateTime.UtcNow.AddDays(-30),
        });

        Assert.Equal("USD 12.00", saleContext.Pricing.ComparePriceDisplay);
        Assert.Null(nonSaleContext.Pricing.ComparePriceDisplay);
    }

    [Fact]
    public void ProductPageMapper_HandlesUnmanagedStock()
    {
        var context = MapProduct(new GetProduct
        {
            Id = Guid.NewGuid(),
            Name = "Digital product",
            Price = 15m,
            Purchasable = true,
            ManageStock = false,
            Quantity = 0,
            AvailableQuantity = 0,
            CreatedOn = DateTime.UtcNow.AddDays(-30),
        });

        Assert.Equal("available", context.Availability.AvailabilityState);
        Assert.Equal("Available", context.Availability.StockLabel);
        Assert.Equal(999999, context.Purchase.InitialStockValue);
        Assert.Equal(999999, context.PurchasePanel.InitialStockValue);
    }

    [Fact]
    public void ProductSummaryMapper_SuppliesDirectAddCardDecisions()
    {
        var item = StorefrontProductSummaryMapper.ToProductSummary(
            new GetCatalogProduct
            {
                Id = Guid.NewGuid(),
                Name = "Paused summary product",
                Slug = "paused-summary-product",
                Price = 20m,
                DisplayPrice = 18m,
                DisplayComparePrice = 25m,
                Purchasable = true,
                InStock = true,
                ManageStock = false,
                PurchaseBlockReasons = ["purchase_disabled"],
                CreatedOn = DateTime.UtcNow.AddDays(-30),
            },
            Display,
            PriceFormatter);

        Assert.True(item.PurchasePaused);
        Assert.False(item.CanAddDirectly);
        Assert.Equal(999999, item.DirectAddStockValue);
        Assert.Equal("Purchasing is paused.", item.PurchaseBlockMessage);
        Assert.Equal("USD 18.00", item.PriceDisplay);
        Assert.Equal("USD 25.00", item.ComparePriceDisplay);
    }

    [Fact]
    public void ProductPageMapper_SuppliesVariantLabelsPricesAndStockLabels()
    {
        var variantId = Guid.NewGuid();
        var context = MapProduct(new GetProduct
        {
            Id = Guid.NewGuid(),
            Name = "Variant product",
            Price = 50m,
            Purchasable = true,
            CreatedOn = DateTime.UtcNow.AddDays(-30),
            Variants =
            [
                new GetProductVariant
                {
                    Id = variantId,
                    DisplayName = "Small",
                    Price = 48m,
                    DisplayPrice = 45m,
                    Stock = 0,
                    IsDefault = true,
                },
            ],
        });

        var variant = Assert.Single(context.Variants);
        Assert.Equal(variantId, variant.Id);
        Assert.Equal("Small", variant.DisplayName);
        Assert.Equal("USD 45.00", variant.PriceDisplay);
        Assert.Equal("Out of stock", variant.StockLabel);
        Assert.Equal("out-of-stock", variant.AvailabilityState);
    }

    [Fact]
    public void V2ProductView_DoesNotReferenceRawBusinessFields()
    {
        var view = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Theme/Pages/Product/V2ProductPageView.razor");

        foreach (var forbidden in new[]
        {
            "PurchaseBlockReasons",
            "ManageStock",
            "AvailableQuantity",
            "MinOrderQuantity",
            "MaxOrderQuantity",
            "EffectivePrice",
            "DisplayPriceAmount",
            "DisplayComparePriceAmount",
            "GetVariantDisplayPrice",
            "DateTime.UtcNow",
            "_product.Variants",
            "_product.Purchasable",
            "_product.Quantity",
            "_product.ComparePrice",
            "_product.DisplayPrice",
        })
        {
            Assert.DoesNotContain(forbidden, view, StringComparison.Ordinal);
        }
    }

    private static StorefrontProductPageContext MapProduct(GetProduct product)
    {
        return StorefrontProductPageMapper.Map(product, [], Display, PriceFormatter);
    }

    private static readonly StorefrontDisplayContext Display = StorefrontDisplayContext.Fallback with
    {
        CurrencyCode = "USD",
        DefaultCurrencyCode = "USD",
        CultureName = "en-US",
    };

    private static readonly StorefrontPriceFormatter PriceFormatter = new();

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
