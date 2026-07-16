namespace BlazorShop.Application.CommerceNode.ProductSelections
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;

    public sealed class ProductSelectionResolver : IProductSelectionResolver
    {
        private const int MaxSelectedAttributes = 5;
        private const int MaxSelectedAttributeNameLength = 100;
        private const int MaxSelectedAttributeValueLength = 200;
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly IProductReadRepository productReadRepository;
        private readonly IStorefrontWorkingCurrencyResolver workingCurrencyResolver;
        private readonly IMoneyConversionService moneyConversionService;
        private readonly IMoneyRoundingService moneyRoundingService;

        public ProductSelectionResolver(
            IProductReadRepository productReadRepository,
            IStorefrontWorkingCurrencyResolver workingCurrencyResolver,
            IMoneyConversionService moneyConversionService,
            IMoneyRoundingService moneyRoundingService)
        {
            this.productReadRepository = productReadRepository;
            this.workingCurrencyResolver = workingCurrencyResolver;
            this.moneyConversionService = moneyConversionService;
            this.moneyRoundingService = moneyRoundingService;
        }

        public async Task<ProductSelectionResult> ResolveAsync(
            ProductSelectionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.StoreId == Guid.Empty)
            {
                return Failed(request, ServiceResponseType.ValidationError, "Store is required.");
            }

            if (request.ProductId == Guid.Empty)
            {
                return Failed(request, ServiceResponseType.ValidationError, "Product is required.");
            }

            if (request.Quantity < 1)
            {
                return Failed(request, ServiceResponseType.ValidationError, "Quantity must be at least 1.");
            }

            var product = await this.productReadRepository.GetPublishedProductDetailsByIdAsync(request.ProductId);
            if (product is null || !IsStorefrontAvailable(product, request.StoreId))
            {
                return Failed(request, ServiceResponseType.NotFound, "Product is not available for this store.");
            }

            var attributes = request.SelectedAttributesJson is not null
                ? NormalizeSelectedAttributes(product, DeserializeSelectedAttributes(request.SelectedAttributesJson))
                : NormalizeSelectedAttributes(product, request.SelectedAttributes);
            if (!attributes.Success)
            {
                return Failed(request, ServiceResponseType.ValidationError, attributes.ErrorMessage, product: product);
            }

            var variant = ResolveVariant(product, request.ProductVariantId);
            if (!variant.Success)
            {
                return Failed(request, ServiceResponseType.ValidationError, variant.ErrorMessage, product: product);
            }

            var availableStock = variant.Value?.Stock ?? product.Quantity;
            if (availableStock < request.Quantity)
            {
                return Failed(request, ServiceResponseType.Conflict, "One or more cart items are out of stock.", product: product, variant: variant.Value);
            }

            var workingCurrency = await this.workingCurrencyResolver.ResolveAsync(
                request.StoreId,
                request.CurrencyCode,
                cancellationToken);
            var baseUnitPrice = variant.Value?.Price ?? product.Price;
            var unitPrice = await this.ResolveUnitPriceAsync(
                request.StoreId,
                baseUnitPrice,
                workingCurrency.BaseCurrencyCode,
                workingCurrency.CurrencyCode,
                cancellationToken);
            if (!unitPrice.Success)
            {
                return Failed(request, unitPrice.ResponseType, unitPrice.Message, product: product, variant: variant.Value);
            }

            return new ProductSelectionResult(
                true,
                ServiceResponseType.Success,
                "Product selection resolved.",
                product.Id,
                variant.Value?.Id,
                attributes.Attributes,
                attributes.AttributesJson,
                variant.Value?.AttributeSignature,
                true,
                true,
                true,
                [],
                variant.Value?.Sku ?? product.Sku,
                variant.Value?.DisplayName ?? product.Name,
                unitPrice.UnitPrice,
                unitPrice.BaseUnitPrice,
                unitPrice.CurrencyCode,
                unitPrice.BaseCurrencyCode,
                product.ComparePrice,
                availableStock,
                1,
                Math.Max(1, availableStock),
                unitPrice.ExchangeRate,
                unitPrice.ExchangeRateProviderKey,
                unitPrice.ExchangeRateSource,
                unitPrice.ExchangeRateEffectiveAtUtc,
                unitPrice.ExchangeRateExpiresAtUtc,
                product,
                variant.Value);
        }

        private async Task<UnitPriceResolution> ResolveUnitPriceAsync(
            Guid storeId,
            decimal baseUnitPrice,
            string baseCurrencyCode,
            string currencyCode,
            CancellationToken cancellationToken)
        {
            if (string.Equals(currencyCode, baseCurrencyCode, StringComparison.Ordinal))
            {
                return UnitPriceResolution.Succeeded(
                    this.moneyRoundingService.RoundUnitPrice(baseUnitPrice, currencyCode),
                    this.moneyRoundingService.RoundUnitPrice(baseUnitPrice, baseCurrencyCode),
                    currencyCode,
                    baseCurrencyCode,
                    null,
                    null,
                    null,
                    null,
                    null);
            }

            var conversion = await this.moneyConversionService.ConvertFromBaseAsync(
                storeId,
                baseUnitPrice,
                currencyCode,
                cancellationToken);
            if (!conversion.Success || conversion.Payload is null)
            {
                return UnitPriceResolution.Failed(
                    conversion.ResponseType is ServiceResponseType.Success ? ServiceResponseType.Conflict : conversion.ResponseType,
                    conversion.Message ?? "Currency conversion is not available.");
            }

            return UnitPriceResolution.Succeeded(
                this.moneyRoundingService.RoundUnitPrice(conversion.Payload.ConvertedAmount, currencyCode),
                this.moneyRoundingService.RoundUnitPrice(conversion.Payload.SourceAmount, conversion.Payload.SourceCurrencyCode),
                currencyCode,
                conversion.Payload.SourceCurrencyCode,
                conversion.Payload.Rate,
                conversion.Payload.ProviderKey,
                conversion.Payload.Source,
                conversion.Payload.EffectiveAt,
                conversion.Payload.ExpiresAt);
        }

        private static bool IsStorefrontAvailable(Product product, Guid storeId)
        {
            var utcNow = DateTime.UtcNow;
            return product.StoreId == storeId
                && product.ArchivedAt is null
                && product.IsPublished
                && product.PublishedOn is not null
                && (product.AvailableStartUtc is null || product.AvailableStartUtc <= utcNow)
                && (product.AvailableEndUtc is null || product.AvailableEndUtc > utcNow)
                && !string.IsNullOrWhiteSpace(product.Slug)
                && product.Category is not null
                && product.Category.StoreId == product.StoreId
                && product.Category.ArchivedAt is null
                && product.Category.IsPublished;
        }

        private static VariantResolution ResolveVariant(Product product, Guid? productVariantId)
        {
            if (string.Equals(product.ProductType, ProductTypes.CustomVariations, StringComparison.OrdinalIgnoreCase))
            {
                return productVariantId.HasValue
                    ? VariantResolution.Failed("Custom variation products do not accept product variant ids.")
                    : VariantResolution.Succeeded(null);
            }

            var allVariants = product.Variants.ToArray();
            var variants = allVariants.Where(candidate => candidate.IsActive).ToArray();
            if (productVariantId.HasValue)
            {
                var variant = allVariants.FirstOrDefault(candidate => candidate.Id == productVariantId.Value);
                if (variant is null)
                {
                    return VariantResolution.Failed("Selected product variant was not found.");
                }

                return variant.IsActive
                    ? VariantResolution.Succeeded(variant)
                    : VariantResolution.Failed("Selected product variant is not available.");
            }

            if (allVariants.Length == 0)
            {
                return VariantResolution.Succeeded(null);
            }

            if (variants.Length == 0)
            {
                return VariantResolution.Failed("Product variants are not available.");
            }

            var defaultVariants = variants.Where(candidate => candidate.IsDefault).ToArray();
            return defaultVariants.Length == 1
                ? VariantResolution.Succeeded(defaultVariants[0])
                : VariantResolution.Failed("Please select a product variant before adding it to the cart.");
        }

        private static SelectedAttributesResolution NormalizeSelectedAttributes(
            Product product,
            IReadOnlyList<SelectedAttributeDto>? selectedAttributes)
        {
            var attributes = (selectedAttributes ?? [])
                .Select(attribute => new SelectedAttributeDto(
                    attribute.Name?.Trim() ?? string.Empty,
                    attribute.Value?.Trim() ?? string.Empty))
                .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Name)
                    || !string.IsNullOrWhiteSpace(attribute.Value))
                .ToArray();

            if (!string.Equals(product.ProductType, ProductTypes.CustomVariations, StringComparison.OrdinalIgnoreCase))
            {
                return attributes.Length == 0
                    ? SelectedAttributesResolution.Ok([], null)
                    : SelectedAttributesResolution.Failed("Selected attributes are only allowed for custom variation products.");
            }

            if (attributes.Length > MaxSelectedAttributes)
            {
                return SelectedAttributesResolution.Failed("At most 5 selected attributes are allowed.");
            }

            foreach (var attribute in attributes)
            {
                if (string.IsNullOrWhiteSpace(attribute.Name))
                {
                    return SelectedAttributesResolution.Failed("Selected attribute name is required.");
                }

                if (string.IsNullOrWhiteSpace(attribute.Value))
                {
                    return SelectedAttributesResolution.Failed("Selected attribute value is required.");
                }

                if (attribute.Name.Length > MaxSelectedAttributeNameLength)
                {
                    return SelectedAttributesResolution.Failed("Selected attribute name must be 100 characters or fewer.");
                }

                if (attribute.Value.Length > MaxSelectedAttributeValueLength)
                {
                    return SelectedAttributesResolution.Failed("Selected attribute value must be 200 characters or fewer.");
                }
            }

            var normalized = attributes
                .GroupBy(attribute => attribute.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(attribute => attribute.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var templateResult = ValidateTemplateAttributes(product, normalized);
            if (!templateResult.Success)
            {
                return templateResult;
            }

            return SelectedAttributesResolution.Ok(
                normalized,
                normalized.Length == 0 ? null : JsonSerializer.Serialize(normalized, SerializerOptions));
        }

        private static SelectedAttributesResolution ValidateTemplateAttributes(
            Product product,
            IReadOnlyList<SelectedAttributeDto> attributes)
        {
            var template = product.VariationTemplate;
            if (template is null)
            {
                return SelectedAttributesResolution.Ok(attributes, null);
            }

            if (!template.IsActive)
            {
                return SelectedAttributesResolution.Failed("Selected attributes are not available for this product.");
            }

            var options = template.Options
                .Where(option => option.IsActive)
                .ToDictionary(option => option.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var option in options.Values.Where(option => option.IsRequired))
            {
                if (!attributes.Any(attribute => string.Equals(attribute.Name, option.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    return SelectedAttributesResolution.Failed($"Required selected attribute '{option.Name}' is missing.");
                }
            }

            foreach (var attribute in attributes)
            {
                if (!options.TryGetValue(attribute.Name, out var option))
                {
                    return SelectedAttributesResolution.Failed("Selected attribute is not available for this product.");
                }

                var allowed = option.Values.Any(value =>
                    value.IsActive && string.Equals(value.Value, attribute.Value, StringComparison.OrdinalIgnoreCase));
                if (!allowed)
                {
                    return SelectedAttributesResolution.Failed("Selected attribute value is not available for this product.");
                }
            }

            return SelectedAttributesResolution.Ok(attributes, null);
        }

        private static IReadOnlyList<SelectedAttributeDto> DeserializeSelectedAttributes(string? selectedAttributesJson)
        {
            if (string.IsNullOrWhiteSpace(selectedAttributesJson))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<IReadOnlyList<SelectedAttributeDto>>(selectedAttributesJson, SerializerOptions) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static ProductSelectionResult Failed(
            ProductSelectionRequest request,
            ServiceResponseType responseType,
            string message,
            Product? product = null,
            ProductVariant? variant = null)
        {
            return new ProductSelectionResult(
                false,
                responseType,
                message,
                product?.Id ?? request.ProductId,
                variant?.Id ?? request.ProductVariantId,
                [],
                request.SelectedAttributesJson,
                variant?.AttributeSignature,
                false,
                false,
                false,
                [message],
                variant?.Sku ?? product?.Sku,
                variant?.DisplayName ?? product?.Name,
                0,
                0,
                NormalizeCurrencyCode(request.CurrencyCode) ?? string.Empty,
                string.Empty,
                product?.ComparePrice,
                0,
                1,
                1,
                Product: product,
                Variant: variant);
        }

        private static string? NormalizeCurrencyCode(string? value)
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
            return normalized is { Length: 3 } && normalized.All(char.IsLetter) ? normalized : null;
        }

        private sealed record VariantResolution(bool Success, ProductVariant? Value, string ErrorMessage)
        {
            public static VariantResolution Succeeded(ProductVariant? variant)
            {
                return new VariantResolution(true, variant, string.Empty);
            }

            public static VariantResolution Failed(string message)
            {
                return new VariantResolution(false, null, message);
            }
        }

        private sealed record SelectedAttributesResolution(
            bool Success,
            IReadOnlyList<SelectedAttributeDto> Attributes,
            string? AttributesJson,
            string ErrorMessage)
        {
            public static SelectedAttributesResolution Ok(IReadOnlyList<SelectedAttributeDto> attributes, string? attributesJson)
            {
                return new SelectedAttributesResolution(true, attributes, attributesJson, string.Empty);
            }

            public static SelectedAttributesResolution Failed(string message)
            {
                return new SelectedAttributesResolution(false, [], null, message);
            }
        }

        private sealed record UnitPriceResolution(
            bool Success,
            ServiceResponseType ResponseType,
            string Message,
            decimal UnitPrice,
            decimal BaseUnitPrice,
            string CurrencyCode,
            string BaseCurrencyCode,
            decimal? ExchangeRate,
            string? ExchangeRateProviderKey,
            string? ExchangeRateSource,
            DateTimeOffset? ExchangeRateEffectiveAtUtc,
            DateTimeOffset? ExchangeRateExpiresAtUtc)
        {
            public static UnitPriceResolution Succeeded(
                decimal unitPrice,
                decimal baseUnitPrice,
                string currencyCode,
                string baseCurrencyCode,
                decimal? exchangeRate,
                string? exchangeRateProviderKey,
                string? exchangeRateSource,
                DateTimeOffset? exchangeRateEffectiveAtUtc,
                DateTimeOffset? exchangeRateExpiresAtUtc)
            {
                return new UnitPriceResolution(
                    true,
                    ServiceResponseType.Success,
                    string.Empty,
                    unitPrice,
                    baseUnitPrice,
                    currencyCode,
                    baseCurrencyCode,
                    exchangeRate,
                    exchangeRateProviderKey,
                    exchangeRateSource,
                    exchangeRateEffectiveAtUtc,
                    exchangeRateExpiresAtUtc);
            }

            public static UnitPriceResolution Failed(ServiceResponseType responseType, string message)
            {
                return new UnitPriceResolution(false, responseType, message, 0, 0, string.Empty, string.Empty, null, null, null, null, null);
            }
        }
    }
}
