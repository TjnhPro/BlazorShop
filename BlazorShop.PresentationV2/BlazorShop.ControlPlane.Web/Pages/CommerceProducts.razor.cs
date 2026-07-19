namespace BlazorShop.ControlPlane.Web.Pages
{
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.ControlPlane.Web.Services.Common;
    using BlazorShop.ControlPlane.Web.Services.Stores;
    using BlazorShop.Domain.Constants;

    using Microsoft.AspNetCore.Components.Web;
    using ProductCatalogQuery = BlazorShop.Domain.Contracts.ProductCatalogQuery;
    using ProductCatalogSortBy = BlazorShop.Domain.Contracts.ProductCatalogSortBy;

    public partial class CommerceProducts
    {
        private const int PageSize = 20;

        private readonly List<StoreSummary> stores = [];
        private readonly List<GetCategory> categories = [];
        private readonly List<GetCatalogProduct> products = [];
        private readonly List<ProductMediaDto> mediaItems = [];
        private readonly Dictionary<Guid, string> thumbnailUrls = [];
        private readonly Dictionary<Guid, string> mediaPreviewUrls = [];
        private readonly Dictionary<Guid, int> variantStocks = [];
        private readonly List<StoreSeoSlugHistoryDto> productSlugHistory = [];
        private Guid? selectedStorePublicId;
        private string? searchTerm;
        private string categoryId = string.Empty;
        private string publishedFilter = string.Empty;
        private ProductCatalogSortBy sortBy = ProductCatalogSortBy.Newest;
        private int pageNumber = 1;
        private int totalCount;
        private GetProduct? selectedProduct;
        private ProductSeoDto seoForm = new();
        private ProductBasicForm basicForm = new();
        private string basicCategoryId = string.Empty;
        private string? mediaSourceUrls;
        private AdminInventoryItemDto? inventoryItem;
        private StoreSeoSlugPolicyResult? productSlugResult;
        private int productQuantity;
        private bool isLoading;
        private bool isSaving;
        private bool isSlugActionBusy;
        private bool isDrawerOpen;
        private string? errorMessage;
        private string? successMessage;

        private bool HasStore => selectedStorePublicId.HasValue && selectedStorePublicId.Value != Guid.Empty;

        private bool CanNextPage => pageNumber * PageSize < totalCount;

        protected override async Task OnInitializedAsync()
        {
            var response = await StoreClient.ListAsync(status: "active", pageSize: 100);
            stores.AddRange(response.Items);
            selectedStorePublicId = stores.FirstOrDefault()?.PublicId;
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            if (!HasStore)
            {
                return;
            }

            isLoading = true;
            errorMessage = null;
            try
            {
                var categoryResult = await CategoryClient.ListCategoriesAsync(selectedStorePublicId!.Value);
                if (categoryResult.Success)
                {
                    categories.Clear();
                    categories.AddRange(categoryResult.Data?.Items ?? []);
                }

                await LoadProductsAsync();
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task SearchAsync()
        {
            pageNumber = 1;
            await LoadProductsAsync();
        }

        private async Task LoadProductsAsync()
        {
            if (!HasStore)
            {
                return;
            }

            var result = await ProductClient.QueryProductsAsync(
                selectedStorePublicId!.Value,
                new ProductCatalogQuery
                {
                    PageNumber = pageNumber,
                    PageSize = PageSize,
                    SearchTerm = searchTerm,
                    CategoryId = Guid.TryParse(categoryId, out var selectedCategoryId) ? selectedCategoryId : null,
                    IsPublished = bool.TryParse(publishedFilter, out var isPublished) ? isPublished : null,
                    SortBy = sortBy,
                });

            if (!result.Success || result.Data is null)
            {
                errorMessage = result.Message;
                return;
            }

            products.Clear();
            products.AddRange(result.Data.Items);
            totalCount = result.Data.TotalCount;
            await LoadThumbnailsAsync();
        }

        private async Task LoadThumbnailsAsync()
        {
            thumbnailUrls.Clear();
            foreach (var product in products.Where(product => product.PrimaryMediaPublicId.HasValue))
            {
                var preview = await MediaClient.GetProductMediaPreviewAsync(
                    selectedStorePublicId!.Value,
                    product.Id,
                    product.PrimaryMediaPublicId!.Value,
                    new ProductMediaPreviewQuery(80, 80, "cover", "webp"));

                if (preview.Success && preview.Content is { Length: > 0 })
                {
                    thumbnailUrls[product.Id] = ToDataUrl(preview);
                }
            }
        }

        private async Task OpenProductAsync(Guid productId)
        {
            if (!HasStore)
            {
                return;
            }

            errorMessage = null;
            var productResult = await ProductClient.GetProductAsync(selectedStorePublicId!.Value, productId);
            if (!productResult.Success || productResult.Data is null)
            {
                errorMessage = productResult.Message;
                return;
            }

            selectedProduct = productResult.Data;
            basicForm = ProductBasicForm.FromProduct(selectedProduct);
            basicCategoryId = selectedProduct.CategoryId?.ToString("D") ?? string.Empty;
            productQuantity = selectedProduct.Quantity;
            seoForm = new ProductSeoDto
            {
                ProductId = selectedProduct.Id,
                Slug = selectedProduct.Slug,
                MetaTitle = selectedProduct.MetaTitle,
                MetaDescription = selectedProduct.MetaDescription,
                CanonicalUrl = selectedProduct.CanonicalUrl,
                OgTitle = selectedProduct.OgTitle,
                OgDescription = selectedProduct.OgDescription,
                OgImage = selectedProduct.OgImage,
                SeoContent = selectedProduct.SeoContent,
                RobotsIndex = selectedProduct.RobotsIndex,
                RobotsFollow = selectedProduct.RobotsFollow,
                IsPublished = selectedProduct.IsPublished,
                PublishedOn = selectedProduct.PublishedOn,
            };

            var seoResult = await ProductClient.GetProductSeoAsync(selectedStorePublicId.Value, productId);
            if (seoResult.Success && seoResult.Data is not null)
            {
                seoForm = seoResult.Data;
            }

            await LoadProductSlugLifecycleAsync();
            await LoadMediaAsync();
            await LoadInventoryAsync();
            isDrawerOpen = true;
        }

        private async Task LoadProductSlugLifecycleAsync()
        {
            productSlugResult = null;
            productSlugHistory.Clear();
            if (!HasStore || selectedProduct is null)
            {
                return;
            }

            var validation = await SeoClient.ValidateSeoSlugAsync(
                selectedStorePublicId!.Value,
                new StoreSeoSlugValidateRequest
                {
                    EntityType = "product",
                    Slug = seoForm.Slug,
                    ExcludedEntityId = selectedProduct.Id,
                });
            if (validation.Success)
            {
                productSlugResult = validation.Data;
            }

            var history = await SeoClient.ListSeoSlugHistoryAsync(
                selectedStorePublicId.Value,
                new StoreSeoSlugHistoryQuery
                {
                    EntityType = "product",
                    EntityId = selectedProduct.Id,
                });
            if (history.Success)
            {
                productSlugHistory.AddRange(history.Data ?? []);
            }
        }

        private async Task GenerateProductSlugAsync()
        {
            if (!HasStore || selectedProduct is null)
            {
                return;
            }

            isSlugActionBusy = true;
            errorMessage = null;
            try
            {
                var result = await SeoClient.GenerateSeoSlugAsync(
                    selectedStorePublicId!.Value,
                    new StoreSeoSlugGenerateRequest
                    {
                        EntityType = "product",
                        SourceName = basicForm.Name ?? selectedProduct.Name,
                        ExcludedEntityId = selectedProduct.Id,
                    });
                productSlugResult = result.Data;
                if (!result.Success)
                {
                    errorMessage = result.Message;
                    return;
                }

                if (result.Data?.Success == true)
                {
                    seoForm.Slug = result.Data.Slug;
                }
            }
            finally
            {
                isSlugActionBusy = false;
            }
        }

        private async Task ValidateProductSlugAsync()
        {
            if (!HasStore || selectedProduct is null)
            {
                return;
            }

            isSlugActionBusy = true;
            errorMessage = null;
            try
            {
                var result = await SeoClient.ValidateSeoSlugAsync(
                    selectedStorePublicId!.Value,
                    new StoreSeoSlugValidateRequest
                    {
                        EntityType = "product",
                        Slug = seoForm.Slug,
                        ExcludedEntityId = selectedProduct.Id,
                    });
                productSlugResult = result.Data;
                if (!result.Success)
                {
                    errorMessage = result.Message;
                }
            }
            finally
            {
                isSlugActionBusy = false;
            }
        }

        private async Task SaveSeoAsync()
        {
            if (!HasStore || selectedProduct is null)
            {
                return;
            }

            isSaving = true;
            errorMessage = null;
            try
            {
                var result = await ProductClient.UpdateProductSeoAsync(
                    selectedStorePublicId!.Value,
                    selectedProduct.Id,
                    new UpdateProductSeoDto
                    {
                        ProductId = selectedProduct.Id,
                        Slug = seoForm.Slug,
                        MetaTitle = seoForm.MetaTitle,
                        MetaDescription = seoForm.MetaDescription,
                        CanonicalUrl = seoForm.CanonicalUrl,
                        OgTitle = seoForm.OgTitle,
                        OgDescription = seoForm.OgDescription,
                        OgImage = seoForm.OgImage,
                        SeoContent = seoForm.SeoContent,
                        RobotsIndex = seoForm.RobotsIndex,
                        RobotsFollow = seoForm.RobotsFollow,
                        IsPublished = seoForm.IsPublished,
                        PublishedOn = seoForm.PublishedOn,
                    });

                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                seoForm = result.Data;
                successMessage = result.Message;
                await LoadProductSlugLifecycleAsync();
                await LoadProductsAsync();
            }
            finally
            {
                isSaving = false;
            }
        }

        private async Task SaveBasicAsync()
        {
            if (!HasStore || selectedProduct is null)
            {
                return;
            }

            isSaving = true;
            errorMessage = null;
            try
            {
                var result = await ProductClient.UpdateProductAsync(
                    selectedStorePublicId!.Value,
                    selectedProduct.Id,
                    new UpdateProduct
                    {
                        Id = selectedProduct.Id,
                        Name = basicForm.Name,
                        Description = FirstNonEmpty(selectedProduct.Description, basicForm.FullDescription, basicForm.ShortDescription, basicForm.Name),
                        Sku = selectedProduct.Sku,
                        Gtin = basicForm.Gtin,
                        Barcode = basicForm.Barcode,
                        ManufacturerPartNumber = basicForm.ManufacturerPartNumber,
                        Condition = basicForm.Condition,
                        Weight = basicForm.Weight,
                        Length = basicForm.Length,
                        Width = basicForm.Width,
                        Height = basicForm.Height,
                        ShortDescription = basicForm.ShortDescription,
                        FullDescription = basicForm.FullDescription,
                        Price = basicForm.Price,
                        ComparePrice = basicForm.ComparePrice,
                        Image = selectedProduct.Image,
                        Quantity = selectedProduct.Quantity,
                        MinOrderQuantity = basicForm.MinOrderQuantity,
                        MaxOrderQuantity = basicForm.MaxOrderQuantity,
                        QuantityStep = basicForm.QuantityStep,
                        PurchasingDisabled = basicForm.PurchasingDisabled,
                        PurchasingDisabledReason = basicForm.PurchasingDisabledReason,
                        ManageStock = basicForm.ManageStock,
                        HideWhenOutOfStock = basicForm.HideWhenOutOfStock,
                        ShippingRequired = basicForm.ShippingRequired,
                        FreeShipping = basicForm.FreeShipping,
                        ShippingSurcharge = basicForm.ShippingSurcharge,
                        DeliveryEstimateText = basicForm.DeliveryEstimateText,
                        DisplayOrder = basicForm.DisplayOrder,
                        ProductType = selectedProduct.ProductType,
                        VariationTemplateId = selectedProduct.VariationTemplateId,
                        CategoryId = Guid.TryParse(basicCategoryId, out var parsedCategoryId) ? parsedCategoryId : null,
                        IsPublished = selectedProduct.IsPublished,
                        PublishedOn = selectedProduct.PublishedOn,
                        AvailableStartUtc = basicForm.AvailableStartUtc,
                        AvailableEndUtc = basicForm.AvailableEndUtc,
                    });

                if (!result.Success)
                {
                    errorMessage = result.Message;
                    return;
                }

                successMessage = result.Message;
                await OpenProductAsync(selectedProduct.Id);
                await LoadProductsAsync();
            }
            finally
            {
                isSaving = false;
            }
        }

        private async Task LoadMediaAsync()
        {
            if (!HasStore || selectedProduct is null)
            {
                return;
            }

            var result = await MediaClient.ListProductMediaAsync(
                selectedStorePublicId!.Value,
                selectedProduct.Id,
                new ProductMediaListQuery(PageSize: 100));
            mediaItems.Clear();
            mediaPreviewUrls.Clear();
            if (result.Success)
            {
                mediaItems.AddRange(result.Data?.Items ?? []);
                foreach (var media in mediaItems.Where(media => media.Status.Equals("stored", StringComparison.OrdinalIgnoreCase)))
                {
                    var preview = await MediaClient.GetProductMediaPreviewAsync(
                        selectedStorePublicId.Value,
                        selectedProduct.Id,
                        media.PublicId,
                        new ProductMediaPreviewQuery(160, 160, "cover", "webp", media.Version));

                    if (preview.Success && preview.Content is { Length: > 0 })
                    {
                        mediaPreviewUrls[media.PublicId] = ToDataUrl(preview);
                    }
                }
            }
        }

        private async Task LoadInventoryAsync()
        {
            if (!HasStore || selectedProduct is null)
            {
                return;
            }

            var result = await ProductClient.QueryInventoryAsync(
                selectedStorePublicId!.Value,
                new AdminInventoryQueryDto
                {
                    SearchTerm = selectedProduct.Sku ?? selectedProduct.Name,
                    PageNumber = 1,
                    PageSize = 20,
                });

            inventoryItem = result.Data?.Items.FirstOrDefault(item => item.ProductId == selectedProduct.Id);
            productQuantity = inventoryItem?.Quantity ?? selectedProduct.Quantity;
            variantStocks.Clear();
            foreach (var variant in inventoryItem?.Variants ?? [])
            {
                variantStocks[variant.VariantId] = variant.Stock;
            }
        }

        private async Task ImportMediaAsync()
        {
            if (!HasStore || selectedProduct is null || string.IsNullOrWhiteSpace(mediaSourceUrls))
            {
                return;
            }

            var items = mediaSourceUrls
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select((url, index) => new ImportProductMediaItem(url, mediaItems.Count + index))
                .ToArray();

            if (items.Length == 0)
            {
                return;
            }

            var result = await MediaClient.ImportProductMediaAsync(selectedStorePublicId!.Value, selectedProduct.Id, new ImportProductMediaRequest(items));
            if (!result.Success)
            {
                errorMessage = result.Message;
                return;
            }

            successMessage = result.Message;
            mediaSourceUrls = null;
            await LoadMediaAsync();
        }

        private async Task SetPrimaryMediaAsync(Guid mediaPublicId)
        {
            if (!HasStore || selectedProduct is null)
            {
                return;
            }

            var result = await MediaClient.SetPrimaryProductMediaAsync(selectedStorePublicId!.Value, selectedProduct.Id, mediaPublicId);
            if (!result.Success)
            {
                errorMessage = result.Message;
                return;
            }

            await LoadMediaAsync();
            await LoadProductsAsync();
        }

        private async Task RetryMediaAsync(Guid mediaPublicId)
        {
            if (!HasStore || selectedProduct is null)
            {
                return;
            }

            var result = await MediaClient.RetryProductMediaAsync(selectedStorePublicId!.Value, selectedProduct.Id, mediaPublicId);
            if (!result.Success)
            {
                errorMessage = result.Message;
                return;
            }

            await LoadMediaAsync();
        }

        private async Task DeleteMediaAsync(Guid mediaPublicId)
        {
            if (!HasStore || selectedProduct is null)
            {
                return;
            }

            var result = await MediaClient.DeleteProductMediaAsync(selectedStorePublicId!.Value, selectedProduct.Id, mediaPublicId);
            if (!result.Success)
            {
                errorMessage = result.Message;
                return;
            }

            await LoadMediaAsync();
            await LoadProductsAsync();
        }

        private async Task SaveProductInventoryAsync()
        {
            if (!HasStore || selectedProduct is null)
            {
                return;
            }

            var result = await ProductClient.UpdateProductStockAsync(selectedStorePublicId!.Value, selectedProduct.Id, new UpdateProductStockDto { Quantity = productQuantity });
            if (!result.Success)
            {
                errorMessage = result.Message;
                return;
            }

            successMessage = result.Message;
            await LoadInventoryAsync();
        }

        private async Task SaveVariantInventoryAsync(Guid variantId)
        {
            if (!HasStore)
            {
                return;
            }

            var result = await ProductClient.UpdateVariantStockAsync(selectedStorePublicId!.Value, variantId, new UpdateVariantStockDto { Stock = GetVariantStockValue(variantId) });
            if (!result.Success)
            {
                errorMessage = result.Message;
                return;
            }

            successMessage = result.Message;
            await LoadInventoryAsync();
        }

        private async Task SetVariantActiveAsync(GetProductVariant variant, bool isActive)
        {
            if (!HasStore || selectedProduct is null)
            {
                return;
            }

            if (!isActive && variant.IsDefault)
            {
                errorMessage = "Default variant must stay active. Set another active default before disabling this variant.";
                return;
            }

            isSaving = true;
            errorMessage = null;
            try
            {
                var result = await ProductClient.UpdateVariantAsync(
                    selectedStorePublicId!.Value,
                    selectedProduct.Id,
                    variant.Id,
                    new UpdateProductVariant
                    {
                        Id = variant.Id,
                        ProductId = selectedProduct.Id,
                        Sku = variant.Sku,
                        Attributes = variant.Attributes,
                        SizeScale = variant.SizeScale,
                        SizeValue = variant.SizeValue,
                        Price = variant.Price,
                        Stock = variant.Stock,
                        Color = variant.Color,
                        IsActive = isActive,
                        IsDefault = variant.IsDefault,
                    });

                if (!result.Success)
                {
                    errorMessage = result.Message;
                    return;
                }

                successMessage = result.Message;
                await OpenProductAsync(selectedProduct.Id);
            }
            finally
            {
                isSaving = false;
            }
        }

        private async Task PreviousPageAsync()
        {
            pageNumber = Math.Max(1, pageNumber - 1);
            await LoadProductsAsync();
        }

        private async Task NextPageAsync()
        {
            if (CanNextPage)
            {
                pageNumber++;
                await LoadProductsAsync();
            }
        }

        private async Task OnSearchKeyDown(KeyboardEventArgs args)
        {
            if (args.Key == "Enter")
            {
                await SearchAsync();
            }
        }

        private Task OnDrawerChanged(bool value)
        {
            isDrawerOpen = value;
            return Task.CompletedTask;
        }

        private static string SlugPreviewText(string? slug)
        {
            return string.IsNullOrWhiteSpace(slug) ? "not generated" : slug;
        }

        private static string SlugPreviewMessage(StoreSeoSlugPolicyResult result)
        {
            return result.Success
                ? "Slug is available."
                : string.IsNullOrWhiteSpace(result.Message) ? "Slug is not available." : result.Message;
        }

        private static string BuildSeoPublicPath(string entityType, string? slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return "not available";
            }

            var trimmedSlug = slug.Trim();
            return entityType switch
            {
                "product" => $"/product/{trimmedSlug}",
                "category" => $"/category/{trimmedSlug}",
                "page" => $"/pages/{trimmedSlug}",
                _ => $"/{trimmedSlug}",
            };
        }

        private string GetVariantStock(Guid variantId) => GetVariantStockValue(variantId).ToString();

        private int GetVariantStockValue(Guid variantId) => variantStocks.TryGetValue(variantId, out var stock) ? stock : 0;

        private void SetVariantStock(Guid variantId, string? value)
        {
            if (int.TryParse(value, out var stock))
            {
                variantStocks[variantId] = stock;
            }
        }

        private static string VariantName(GetProductVariant variant)
        {
            return FirstNonEmpty(variant.DisplayName, string.Join(" / ", variant.Attributes.Select(attribute => $"{attribute.Name}: {attribute.Value}")), variant.SizeValue, variant.Color, "Variant") ?? "Variant";
        }

        private static string VariantName(AdminInventoryVariantDto variant)
        {
            return FirstNonEmpty(variant.DisplayName, string.Join(" / ", variant.Attributes.Select(attribute => $"{attribute.Name}: {attribute.Value}")), variant.SizeValue, variant.Color, "Variant") ?? "Variant";
        }

        private string VariantSignatureText(GetProductVariant variant)
        {
            return FirstNonEmpty(variant.AttributeSignature, "not generated") ?? "not generated";
        }

        private IReadOnlyList<string> GetVariantWorkflowWarnings(GetProductVariant variant)
        {
            var template = selectedProduct?.VariationTemplate;
            if (template is null)
            {
                return Array.Empty<string>();
            }

            var warnings = new List<string>();
            var activeOptions = template.Options.ToArray();

            foreach (var option in activeOptions.Where(option => option.IsRequired))
            {
                var hasAttribute = variant.Attributes.Any(attribute =>
                    AttributeNameEquals(attribute.Name, option.Name)
                    && !string.IsNullOrWhiteSpace(attribute.Value));

                if (!hasAttribute)
                {
                    warnings.Add($"Required option '{option.Name}' is missing from this combination.");
                }
            }

            foreach (var attribute in variant.Attributes)
            {
                var option = activeOptions.FirstOrDefault(option => AttributeNameEquals(option.Name, attribute.Name));
                if (option is null)
                {
                    warnings.Add($"Option '{attribute.Name}' is no longer active in the template.");
                    continue;
                }

                var hasActiveValue = option.Values.Any(value =>
                    string.Equals(value.Value.Trim(), attribute.Value.Trim(), StringComparison.OrdinalIgnoreCase));
                if (!hasActiveValue)
                {
                    warnings.Add($"Value '{attribute.Value}' for option '{option.Name}' is no longer active in the template.");
                }
            }

            return warnings;
        }

        private static bool AttributeNameEquals(string? left, string? right)
        {
            return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static string VariationControlLabel(string? controlType)
        {
            return controlType?.Trim().ToLowerInvariant() switch
            {
                VariationControlTypes.Radio => "Radio",
                VariationControlTypes.Color => "Color",
                _ => "Dropdown",
            };
        }

        private static string ColorSwatchStyle(string? colorHex)
        {
            return string.IsNullOrWhiteSpace(colorHex) ? string.Empty : $"background-color:{colorHex}";
        }

        private static string PublicationStatusLabel(GetCatalogProduct product)
        {
            return PublicationStatus(product.IsPublished, product.AvailableStartUtc, product.AvailableEndUtc);
        }

        private static string PublicationStatusLabel(GetProduct product)
        {
            return PublicationStatus(product.IsPublished, product.AvailableStartUtc, product.AvailableEndUtc);
        }

        private static string PublicationStatusTone(GetCatalogProduct product)
        {
            return PublicationStatusTone(PublicationStatusLabel(product));
        }

        private static string PublicationStatusTone(GetProduct product)
        {
            return PublicationStatusTone(PublicationStatusLabel(product));
        }

        private static string PublicationStatus(bool isPublished, DateTime? availableStartUtc, DateTime? availableEndUtc)
        {
            if (!isPublished)
            {
                return "Draft";
            }

            var now = DateTime.UtcNow;
            if (availableStartUtc is not null && availableStartUtc.Value > now)
            {
                return "Scheduled";
            }

            if (availableEndUtc is not null && availableEndUtc.Value <= now)
            {
                return "Expired";
            }

            return "Published";
        }

        private static string PublicationStatusTone(string status)
        {
            return status switch
            {
                "Published" => "success",
                "Scheduled" => "warning",
                "Expired" => "danger",
                _ => "neutral",
            };
        }

        private static bool IsPurchaseDisabled(GetCatalogProduct product)
        {
            return product.PurchasingDisabled;
        }

        private static string PurchaseStatusLabel(GetProduct product)
        {
            if (product.PurchasingDisabled)
            {
                return "Purchase paused";
            }

            if (!product.IsPublished)
            {
                return "Draft";
            }

            var now = DateTime.UtcNow;
            if (product.AvailableStartUtc is not null && product.AvailableStartUtc.Value > now)
            {
                return "Scheduled";
            }

            if (product.AvailableEndUtc is not null && product.AvailableEndUtc.Value <= now)
            {
                return "Expired";
            }

            if (product.ManageStock && product.Quantity <= 0)
            {
                return "Out of stock";
            }

            if (string.Equals(product.ProductType, ProductTypes.VariantInventory, StringComparison.OrdinalIgnoreCase)
                && product.Variants.Any())
            {
                return "Variant selection required";
            }

            return "Purchasable";
        }

        private static string PurchaseStatusTone(GetProduct product)
        {
            return PurchaseStatusLabel(product) == "Purchasable" ? "success" : "warning";
        }

        private static string InventoryStatusLabel(GetCatalogProduct product)
        {
            if (!product.ManageStock)
            {
                return "Stock unmanaged";
            }

            if (product.Quantity <= 0)
            {
                return "Out of stock";
            }

            return product.Quantity <= 5 ? "Low stock" : "In stock";
        }

        private static string InventoryStatusTone(GetCatalogProduct product)
        {
            if (!product.ManageStock)
            {
                return "warning";
            }

            if (product.Quantity <= 0)
            {
                return "danger";
            }

            return product.Quantity <= 5 ? "warning" : "success";
        }

        private static string InventoryStatusLabel(GetProduct product)
        {
            if (!product.ManageStock)
            {
                return "Stock unmanaged";
            }

            if (product.Quantity <= 0)
            {
                return "Out of stock";
            }

            return product.Quantity <= 5 ? "Low stock" : "In stock";
        }

        private static string InventoryStatusTone(GetProduct product)
        {
            if (!product.ManageStock)
            {
                return "warning";
            }

            if (product.Quantity <= 0)
            {
                return "danger";
            }

            return product.Quantity <= 5 ? "warning" : "success";
        }

        private static string InventoryStatusLabel(AdminInventoryItemDto? item, GetProduct product)
        {
            if (!product.ManageStock)
            {
                return "Stock unmanaged";
            }

            if (item?.IsOutOfStock == true || product.Quantity <= 0)
            {
                return "Out of stock";
            }

            return item?.IsLowStock == true ? "Low stock" : "In stock";
        }

        private static string InventoryStatusTone(AdminInventoryItemDto? item, GetProduct product)
        {
            if (!product.ManageStock)
            {
                return "warning";
            }

            if (item?.IsOutOfStock == true || product.Quantity <= 0)
            {
                return "danger";
            }

            return item?.IsLowStock == true ? "warning" : "success";
        }

        private static string InventoryStatusLabel(AdminInventoryVariantDto variant)
        {
            if (variant.IsOutOfStock)
            {
                return "Out of stock";
            }

            return variant.IsLowStock ? "Low stock" : "In stock";
        }

        private static string InventoryStatusTone(AdminInventoryVariantDto variant)
        {
            if (variant.IsOutOfStock)
            {
                return "danger";
            }

            return variant.IsLowStock ? "warning" : "success";
        }

        private static string MediaTone(string? status)
        {
            return status?.ToLowerInvariant() switch
            {
                "stored" or "completed" => "success",
                "failed" => "danger",
                "pending" or "processing" or "queued" => "warning",
                _ => "neutral",
            };
        }

        private static string ToDataUrl(ControlPlaneFileResult result)
        {
            return $"data:{result.ContentType ?? "image/webp"};base64,{Convert.ToBase64String(result.Content ?? [])}";
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }

        private sealed class ProductBasicForm
        {
            public string? Name { get; set; }

            public string? ShortDescription { get; set; }

            public string? FullDescription { get; set; }

            public decimal Price { get; set; }

            public decimal? ComparePrice { get; set; }

            public int DisplayOrder { get; set; }

            public DateTime? AvailableStartUtc { get; set; }

            public DateTime? AvailableEndUtc { get; set; }

            public int MinOrderQuantity { get; set; } = 1;

            public int? MaxOrderQuantity { get; set; }

            public int QuantityStep { get; set; } = 1;

            public bool PurchasingDisabled { get; set; }

            public string? PurchasingDisabledReason { get; set; }

            public bool ManageStock { get; set; } = true;

            public bool HideWhenOutOfStock { get; set; }

            public bool ShippingRequired { get; set; } = true;

            public bool FreeShipping { get; set; }

            public decimal? ShippingSurcharge { get; set; }

            public string? DeliveryEstimateText { get; set; }

            public string? Gtin { get; set; }

            public string? Barcode { get; set; }

            public string? ManufacturerPartNumber { get; set; }

            public string? Condition { get; set; }

            public decimal? Weight { get; set; }

            public decimal? Length { get; set; }

            public decimal? Width { get; set; }

            public decimal? Height { get; set; }

            public static ProductBasicForm FromProduct(GetProduct product)
            {
                return new ProductBasicForm
                {
                    Name = product.Name,
                    ShortDescription = product.ShortDescription,
                    FullDescription = product.FullDescription,
                    Price = product.Price,
                    ComparePrice = product.ComparePrice,
                    DisplayOrder = product.DisplayOrder,
                    AvailableStartUtc = product.AvailableStartUtc,
                    AvailableEndUtc = product.AvailableEndUtc,
                    MinOrderQuantity = product.MinOrderQuantity,
                    MaxOrderQuantity = product.MaxOrderQuantity,
                    QuantityStep = product.QuantityStep,
                    PurchasingDisabled = product.PurchasingDisabled,
                    PurchasingDisabledReason = product.PurchasingDisabledReason,
                    ManageStock = product.ManageStock,
                    HideWhenOutOfStock = product.HideWhenOutOfStock,
                    ShippingRequired = product.ShippingRequired,
                    FreeShipping = product.FreeShipping,
                    ShippingSurcharge = product.ShippingSurcharge,
                    DeliveryEstimateText = product.DeliveryEstimateText,
                    Gtin = product.Gtin,
                    Barcode = product.Barcode,
                    ManufacturerPartNumber = product.ManufacturerPartNumber,
                    Condition = product.Condition,
                    Weight = product.Weight,
                    Length = product.Length,
                    Width = product.Width,
                    Height = product.Height,
                };
            }
        }
    }
}
