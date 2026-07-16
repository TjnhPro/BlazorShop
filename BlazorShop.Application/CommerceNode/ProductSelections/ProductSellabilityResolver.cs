namespace BlazorShop.Application.CommerceNode.ProductSelections
{
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;

    public sealed class ProductSellabilityResolver : IProductSellabilityResolver
    {
        public ProductSellabilityResult Resolve(ProductSellabilityRequest request)
        {
            var product = request.Product;
            var variant = request.Variant;
            var nowUtc = request.NowUtc?.UtcDateTime ?? DateTime.UtcNow;
            var reasons = new List<string>();
            var messages = new List<string>();
            var minOrderQuantity = Math.Max(1, product.MinOrderQuantity);
            var quantityStep = Math.Max(1, product.QuantityStep);

            AddVisibilityReasons(request.StoreId, product, nowUtc, reasons, messages);
            AddPurchaseDisabledReason(product, reasons, messages);
            AddVariantReasons(product, variant, reasons, messages);
            AddQuantityReasons(request.RequestedQuantity, minOrderQuantity, product.MaxOrderQuantity, quantityStep, reasons, messages);

            var availableQuantity = ResolveAvailableQuantity(product, variant);
            var stockStatus = ResolveStockStatus(product, variant, availableQuantity, reasons);
            AddStockReasons(product, availableQuantity, request.RequestedQuantity, reasons, messages);

            return new ProductSellabilityResult(
                product.Id,
                variant?.Id,
                reasons.Count == 0,
                reasons.Distinct(StringComparer.Ordinal).ToArray(),
                messages.Distinct(StringComparer.Ordinal).ToArray(),
                stockStatus,
                availableQuantity,
                minOrderQuantity,
                product.MaxOrderQuantity,
                quantityStep,
                product.ManageStock,
                product.ShippingRequired,
                product.FreeShipping,
                product.DeliveryEstimateText);
        }

        private static void AddVisibilityReasons(
            Guid storeId,
            Product product,
            DateTime nowUtc,
            List<string> reasons,
            List<string> messages)
        {
            if (product.StoreId != storeId
                || product.ArchivedAt is not null
                || string.IsNullOrWhiteSpace(product.Slug)
                || product.Category is null
                || product.Category.StoreId != product.StoreId
                || product.Category.ArchivedAt is not null
                || !product.Category.IsPublished)
            {
                Add(reasons, messages, ProductPurchaseBlockReasons.NotVisible, "Product is not available for this store.");
            }

            if (!product.IsPublished || product.PublishedOn is null)
            {
                Add(reasons, messages, ProductPurchaseBlockReasons.NotPublished, "Product is not published.");
            }

            if (product.AvailableStartUtc.HasValue && product.AvailableStartUtc.Value > nowUtc)
            {
                Add(reasons, messages, ProductPurchaseBlockReasons.NotStarted, "Product is not available yet.");
            }

            if (product.AvailableEndUtc.HasValue && product.AvailableEndUtc.Value <= nowUtc)
            {
                Add(reasons, messages, ProductPurchaseBlockReasons.Expired, "Product is no longer available.");
            }
        }

        private static void AddPurchaseDisabledReason(Product product, List<string> reasons, List<string> messages)
        {
            if (!product.PurchasingDisabled)
            {
                return;
            }

            Add(
                reasons,
                messages,
                ProductPurchaseBlockReasons.PurchaseDisabled,
                string.IsNullOrWhiteSpace(product.PurchasingDisabledReason)
                    ? "Product cannot be purchased right now."
                    : product.PurchasingDisabledReason.Trim());
        }

        private static void AddVariantReasons(
            Product product,
            ProductVariant? variant,
            List<string> reasons,
            List<string> messages)
        {
            var hasVariants = product.Variants.Count > 0;
            var requiresVariant = string.Equals(product.ProductType, ProductTypes.VariantInventory, StringComparison.OrdinalIgnoreCase)
                && hasVariants
                && variant is null;
            if (requiresVariant)
            {
                Add(reasons, messages, ProductPurchaseBlockReasons.VariantRequired, "Please select a product variant before adding it to the cart.");
            }

            if (variant is not null && !variant.IsActive)
            {
                Add(reasons, messages, ProductPurchaseBlockReasons.VariantInactive, "Selected product variant is not available.");
            }
        }

        private static void AddQuantityReasons(
            int requestedQuantity,
            int minOrderQuantity,
            int? maxOrderQuantity,
            int quantityStep,
            List<string> reasons,
            List<string> messages)
        {
            if (requestedQuantity < minOrderQuantity)
            {
                Add(reasons, messages, ProductPurchaseBlockReasons.BelowMinQuantity, $"Quantity must be at least {minOrderQuantity}.");
            }

            if (maxOrderQuantity.HasValue && requestedQuantity > maxOrderQuantity.Value)
            {
                Add(reasons, messages, ProductPurchaseBlockReasons.AboveMaxQuantity, $"Quantity must be {maxOrderQuantity.Value} or fewer.");
            }

            if (requestedQuantity >= minOrderQuantity && ((requestedQuantity - minOrderQuantity) % quantityStep) != 0)
            {
                Add(reasons, messages, ProductPurchaseBlockReasons.InvalidQuantityStep, $"Quantity must increase by {quantityStep}.");
            }
        }

        private static int? ResolveAvailableQuantity(Product product, ProductVariant? variant)
        {
            return product.ManageStock ? variant?.Stock ?? product.Quantity : null;
        }

        private static string ResolveStockStatus(
            Product product,
            ProductVariant? variant,
            int? availableQuantity,
            List<string> reasons)
        {
            if (!product.ManageStock)
            {
                return ProductStockStatuses.NotManaged;
            }

            if (reasons.Contains(ProductPurchaseBlockReasons.VariantRequired)
                || (string.Equals(product.ProductType, ProductTypes.VariantInventory, StringComparison.OrdinalIgnoreCase)
                    && product.Variants.Count > 0
                    && variant is null))
            {
                return ProductStockStatuses.VariantRequired;
            }

            return availableQuantity.GetValueOrDefault() > 0
                ? ProductStockStatuses.InStock
                : ProductStockStatuses.OutOfStock;
        }

        private static void AddStockReasons(
            Product product,
            int? availableQuantity,
            int requestedQuantity,
            List<string> reasons,
            List<string> messages)
        {
            if (!product.ManageStock || !availableQuantity.HasValue)
            {
                return;
            }

            if (reasons.Contains(ProductPurchaseBlockReasons.VariantRequired))
            {
                return;
            }

            if (availableQuantity.Value <= 0)
            {
                Add(reasons, messages, ProductPurchaseBlockReasons.OutOfStock, "One or more cart items are out of stock.");
                return;
            }

            if (availableQuantity.Value < requestedQuantity)
            {
                Add(reasons, messages, ProductPurchaseBlockReasons.NotEnoughStock, "One or more cart items are out of stock.");
            }
        }

        private static void Add(List<string> reasons, List<string> messages, string reason, string message)
        {
            reasons.Add(reason);
            messages.Add(message);
        }
    }
}
