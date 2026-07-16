namespace BlazorShop.Application.CommerceNode.Carts
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;

    public sealed class StorefrontCartService : IStorefrontCartService
    {
        private const int MaxSelectedAttributes = 5;
        private const int MaxSelectedAttributeNameLength = 100;
        private const int MaxSelectedAttributeValueLength = 200;
        private const int MaxPersonalizationJsonLength = 8192;
        private const int MaxPersonalizationHashLength = 128;
        private const int MaxFulfillmentProviderKeyLength = 64;
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly IStorefrontCartSessionService sessionService;
        private readonly IProductReadRepository productReadRepository;
        private readonly IStoreCurrencyResolver storeCurrencyResolver;
        private readonly IStorefrontWorkingCurrencyResolver workingCurrencyResolver;
        private readonly IMoneyConversionService moneyConversionService;
        private readonly IMoneyRoundingService moneyRoundingService;

        public StorefrontCartService(
            IStorefrontCartSessionService sessionService,
            IProductReadRepository productReadRepository,
            IStoreCurrencyResolver storeCurrencyResolver,
            IStorefrontWorkingCurrencyResolver workingCurrencyResolver,
            IMoneyConversionService moneyConversionService,
            IMoneyRoundingService moneyRoundingService)
        {
            this.sessionService = sessionService;
            this.productReadRepository = productReadRepository;
            this.storeCurrencyResolver = storeCurrencyResolver;
            this.workingCurrencyResolver = workingCurrencyResolver;
            this.moneyConversionService = moneyConversionService;
            this.moneyRoundingService = moneyRoundingService;
        }

        public async Task<ServiceResponse<StorefrontCartResult>> CreateOrResumeAsync(
            StorefrontCartCreateOrResumeRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.StoreId == Guid.Empty)
            {
                return Failed<StorefrontCartResult>(ServiceResponseType.ValidationError, "Store is required.");
            }

            if (!string.IsNullOrWhiteSpace(request.Token))
            {
                var existing = await this.sessionService.ResolveAsync(request.StoreId, request.Token, cancellationToken);
                if (existing.Success && existing.Payload is not null)
                {
                    return Succeeded(
                        "Cart session resolved.",
                        new StorefrontCartResult(existing.Payload));
                }

                if (existing.ResponseType is not ServiceResponseType.NotFound)
                {
                    return Failed<StorefrontCartResult>(existing.ResponseType, existing.Message ?? "Cart session could not be resolved.");
                }
            }

            var created = await this.sessionService.CreateAsync(
                new StorefrontCartSessionCreateRequest(
                    request.StoreId,
                    request.CustomerId,
                    request.AppUserId),
                cancellationToken);
            if (!created.Success || created.Payload is null)
            {
                return Failed<StorefrontCartResult>(created.ResponseType, created.Message ?? "Cart session could not be created.");
            }

            var resolved = await this.sessionService.ResolveAsync(request.StoreId, created.Payload.Token, cancellationToken);
            if (!resolved.Success || resolved.Payload is null)
            {
                return Failed<StorefrontCartResult>(resolved.ResponseType, resolved.Message ?? "Cart session could not be resolved.");
            }

            return Succeeded(
                "Cart session created.",
                new StorefrontCartResult(resolved.Payload, created.Payload.Token));
        }

        public Task<ServiceResponse<StorefrontCartSessionDto>> GetAsync(
            Guid storeId,
            string token,
            CancellationToken cancellationToken = default)
        {
            return this.sessionService.ResolveAsync(storeId, token, cancellationToken);
        }

        public async Task<ServiceResponse<StorefrontCartSessionDto>> AddLineAsync(
            StorefrontCartAddLineRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.Quantity < 1)
            {
                return Failed<StorefrontCartSessionDto>(ServiceResponseType.ValidationError, "Quantity must be at least 1.");
            }

            var personalization = ValidatePersonalization(request.PersonalizationHash, request.PersonalizationJson, request.FulfillmentProviderKey);
            if (!personalization.Success)
            {
                return Failed<StorefrontCartSessionDto>(ServiceResponseType.ValidationError, personalization.Message);
            }

            var productResult = await this.ResolveProductForCartAsync(
                request.StoreId,
                request.ProductId,
                request.ProductVariantId,
                request.Quantity,
                request.SelectedAttributes,
                cancellationToken);
            if (!productResult.Success)
            {
                return Failed<StorefrontCartSessionDto>(productResult.ResponseType, productResult.Message);
            }

            var product = productResult.Product!;
            var variant = productResult.Variant;
            var workingCurrency = await this.workingCurrencyResolver.ResolveAsync(
                request.StoreId,
                request.CurrencyCode,
                cancellationToken);
            var currencyCode = workingCurrency.CurrencyCode;
            var baseUnitPrice = variant?.Price ?? product.Price;
            var unitPriceResult = await this.ResolveUnitPriceAsync(
                request.StoreId,
                baseUnitPrice,
                workingCurrency.BaseCurrencyCode,
                currencyCode,
                cancellationToken);
            if (!unitPriceResult.Success)
            {
                return Failed<StorefrontCartSessionDto>(
                    unitPriceResult.ResponseType,
                    unitPriceResult.Message);
            }

            return await this.sessionService.AddOrUpdateLineAsync(
                new StorefrontCartLineMutationRequest(
                    request.StoreId,
                    request.Token,
                    product.Id,
                    variant?.Id,
                    productResult.SelectedAttributesJson,
                    NormalizeNullable(request.PersonalizationHash),
                    NormalizeNullable(request.PersonalizationJson),
                    request.ArtworkAssetId,
                    request.ArtworkVersion,
                    NormalizeNullable(request.FulfillmentProviderKey),
                    request.Quantity,
                    unitPriceResult.UnitPrice,
                    currencyCode,
                    unitPriceResult.BaseUnitPrice,
                    unitPriceResult.BaseCurrencyCode,
                    unitPriceResult.ExchangeRate,
                    unitPriceResult.ExchangeRateProviderKey,
                    unitPriceResult.ExchangeRateSource,
                    unitPriceResult.ExchangeRateEffectiveAtUtc,
                    unitPriceResult.ExchangeRateExpiresAtUtc),
                cancellationToken);
        }

        public async Task<ServiceResponse<StorefrontCartSessionDto>> UpdateLineAsync(
            StorefrontCartUpdateLineRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.Quantity < 1)
            {
                return Failed<StorefrontCartSessionDto>(ServiceResponseType.ValidationError, "Quantity must be at least 1.");
            }

            var cart = await this.sessionService.ResolveAsync(request.StoreId, request.Token, cancellationToken);
            if (!cart.Success || cart.Payload is null)
            {
                return cart;
            }

            var line = cart.Payload.Lines.FirstOrDefault(candidate => candidate.Id == request.LineId);
            if (line is null)
            {
                return Failed<StorefrontCartSessionDto>(ServiceResponseType.NotFound, "Cart line was not found.");
            }

            var productResult = await this.ResolveProductForCartAsync(
                request.StoreId,
                line.ProductId,
                line.ProductVariantId,
                request.Quantity,
                null,
                cancellationToken,
                line.SelectedAttributesJson);
            if (!productResult.Success)
            {
                return Failed<StorefrontCartSessionDto>(productResult.ResponseType, productResult.Message);
            }

            return await this.sessionService.UpdateLineQuantityAsync(
                request.StoreId,
                request.Token,
                request.LineId,
                request.Quantity,
                cancellationToken);
        }

        public Task<ServiceResponse<StorefrontCartSessionDto>> RemoveLineAsync(
            Guid storeId,
            string token,
            Guid lineId,
            CancellationToken cancellationToken = default)
        {
            return this.sessionService.RemoveLineAsync(storeId, token, lineId, cancellationToken);
        }

        public async Task<ServiceResponse<StorefrontCartSessionDto>> ClearAsync(
            Guid storeId,
            string token,
            CancellationToken cancellationToken = default)
        {
            var cart = await this.sessionService.ResolveAsync(storeId, token, cancellationToken);
            if (!cart.Success || cart.Payload is null)
            {
                return cart;
            }

            var current = cart;
            foreach (var lineId in cart.Payload.Lines.Select(line => line.Id).ToArray())
            {
                current = await this.sessionService.RemoveLineAsync(storeId, token, lineId, cancellationToken);
                if (!current.Success)
                {
                    return current;
                }
            }

            return current;
        }

        public async Task<ServiceResponse<StorefrontCartValidationResult>> ValidateAsync(
            Guid storeId,
            string token,
            CancellationToken cancellationToken = default)
        {
            var cart = await this.sessionService.ResolveAsync(storeId, token, cancellationToken);
            if (!cart.Success || cart.Payload is null)
            {
                return Failed<StorefrontCartValidationResult>(cart.ResponseType, cart.Message ?? "Cart session could not be resolved.");
            }

            var issues = new List<StorefrontCartValidationIssueDto>();
            decimal totalAmount = 0;
            var baseCurrencyCode = await this.storeCurrencyResolver.ResolveDefaultCurrencyCodeAsync(storeId, cancellationToken);
            var currencyResolution = await this.ResolveCartSnapshotCurrencyAsync(
                storeId,
                cart.Payload.Lines,
                baseCurrencyCode,
                cancellationToken);
            var currencyCode = currencyResolution.CurrencyCode;
            if (currencyResolution.Issue is not null)
            {
                issues.Add(currencyResolution.Issue);
            }

            foreach (var line in cart.Payload.Lines)
            {
                var productResult = await this.ResolveProductForCartAsync(
                    storeId,
                    line.ProductId,
                    line.ProductVariantId,
                    line.Quantity,
                    null,
                    cancellationToken,
                    line.SelectedAttributesJson);
                if (!productResult.Success)
                {
                    issues.Add(new StorefrontCartValidationIssueDto(
                        line.Id,
                        line.ProductId,
                        ResponseTypeToCode(productResult.ResponseType),
                        productResult.Message));
                    continue;
                }

                var unitPrice = line.UnitPriceSnapshot.HasValue
                    ? this.moneyRoundingService.RoundUnitPrice(line.UnitPriceSnapshot.Value, currencyCode)
                    : this.moneyRoundingService.RoundUnitPrice(productResult.Variant?.Price ?? productResult.Product!.Price, currencyCode);
                totalAmount += this.moneyRoundingService.RoundLineTotal(unitPrice * line.Quantity, currencyCode);
            }

            totalAmount = this.moneyRoundingService.RoundOrderTotal(totalAmount, currencyCode);

            return Succeeded(
                issues.Count == 0 ? "Cart is valid." : "Cart has validation issues.",
                new StorefrontCartValidationResult(
                    cart.Payload.PublicId,
                    cart.Payload.Version,
                    issues.Count == 0,
                    totalAmount,
                    currencyCode,
                    issues));
        }

        private async Task<CartProductResolution> ResolveProductForCartAsync(
            Guid storeId,
            Guid productId,
            Guid? productVariantId,
            int quantity,
            IReadOnlyList<SelectedAttributeDto>? selectedAttributes,
            CancellationToken cancellationToken,
            string? selectedAttributesJson = null)
        {
            if (storeId == Guid.Empty)
            {
                return CartProductResolution.Failed(ServiceResponseType.ValidationError, "Store is required.");
            }

            if (productId == Guid.Empty)
            {
                return CartProductResolution.Failed(ServiceResponseType.ValidationError, "Product is required.");
            }

            var product = await this.productReadRepository.GetPublishedProductDetailsByIdAsync(productId);
            if (product is null || !IsStorefrontAvailable(product, storeId))
            {
                return CartProductResolution.Failed(ServiceResponseType.NotFound, "Product is not available for this store.");
            }

            var attributes = selectedAttributesJson is not null
                ? SelectedAttributesResolution.Ok(selectedAttributesJson)
                : NormalizeSelectedAttributes(product, selectedAttributes);
            if (!attributes.Success)
            {
                return CartProductResolution.Failed(ServiceResponseType.ValidationError, attributes.ErrorMessage);
            }

            var variant = ResolveVariant(product, productVariantId);
            if (!variant.Success)
            {
                return CartProductResolution.Failed(ServiceResponseType.ValidationError, variant.ErrorMessage);
            }

            var availableStock = variant.Value?.Stock ?? product.Quantity;
            if (availableStock < quantity)
            {
                return CartProductResolution.Failed(ServiceResponseType.Conflict, "One or more cart items are out of stock.");
            }

            return CartProductResolution.Succeeded(product, variant.Value, attributes.AttributesJson);
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
                conversion.Payload.SourceCurrencyCode,
                conversion.Payload.Rate,
                conversion.Payload.ProviderKey,
                conversion.Payload.Source,
                conversion.Payload.EffectiveAt,
                conversion.Payload.ExpiresAt);
        }

        private async Task<CartCurrencyResolution> ResolveCartSnapshotCurrencyAsync(
            Guid storeId,
            IReadOnlyList<StorefrontCartLineDto> lines,
            string baseCurrencyCode,
            CancellationToken cancellationToken)
        {
            var snapshotCurrencies = lines
                .Select(line => NormalizeCurrencyCode(line.CurrencyCodeSnapshot))
                .Where(currency => currency is not null)
                .Select(currency => currency!)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (snapshotCurrencies.Length == 0)
            {
                return new CartCurrencyResolution(baseCurrencyCode, null);
            }

            if (snapshotCurrencies.Length > 1)
            {
                return new CartCurrencyResolution(
                    baseCurrencyCode,
                    new StorefrontCartValidationIssueDto(
                        null,
                        null,
                        "cart.currency_mixed",
                        "Cart lines use mixed currencies."));
            }

            var snapshotCurrency = snapshotCurrencies[0];
            var resolution = await this.workingCurrencyResolver.ResolveAsync(
                storeId,
                snapshotCurrency,
                cancellationToken);
            if (!string.Equals(resolution.CurrencyCode, snapshotCurrency, StringComparison.Ordinal)
                || !resolution.CheckoutCurrencyEnabled)
            {
                return new CartCurrencyResolution(
                    baseCurrencyCode,
                    new StorefrontCartValidationIssueDto(
                        null,
                        null,
                        "cart.currency_unavailable",
                        $"Cart currency '{snapshotCurrency}' is not available for checkout."));
            }

            return new CartCurrencyResolution(snapshotCurrency, null);
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
                    ? SelectedAttributesResolution.Ok(null)
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

            return SelectedAttributesResolution.Ok(normalized.Length == 0
                ? null
                : JsonSerializer.Serialize(normalized, SerializerOptions));
        }

        private static SelectedAttributesResolution ValidateTemplateAttributes(
            Product product,
            IReadOnlyList<SelectedAttributeDto> attributes)
        {
            var template = product.VariationTemplate;
            if (template is null)
            {
                return SelectedAttributesResolution.Ok(null);
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

            return SelectedAttributesResolution.Ok(null);
        }

        private static PersonalizationValidation ValidatePersonalization(
            string? personalizationHash,
            string? personalizationJson,
            string? fulfillmentProviderKey)
        {
            if (NormalizeNullable(personalizationHash) is { Length: > MaxPersonalizationHashLength })
            {
                return PersonalizationValidation.Failed("Personalization hash must be 128 characters or fewer.");
            }

            if (NormalizeNullable(personalizationJson) is { Length: > MaxPersonalizationJsonLength })
            {
                return PersonalizationValidation.Failed("Personalization payload must be 8192 characters or fewer.");
            }

            if (NormalizeNullable(fulfillmentProviderKey) is { Length: > MaxFulfillmentProviderKeyLength })
            {
                return PersonalizationValidation.Failed("Fulfillment provider key must be 64 characters or fewer.");
            }

            return PersonalizationValidation.Succeeded();
        }

        private static string ResponseTypeToCode(ServiceResponseType responseType)
        {
            return responseType switch
            {
                ServiceResponseType.NotFound => "cart.product_unavailable",
                ServiceResponseType.Conflict => "cart.quantity_unavailable",
                ServiceResponseType.ValidationError => "cart.line_invalid",
                _ => "cart.validation_failed",
            };
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? NormalizeCurrencyCode(string? value)
        {
            var normalized = NormalizeNullable(value)?.ToUpperInvariant();
            return normalized is { Length: 3 } && normalized.All(char.IsLetter) ? normalized : null;
        }

        private static ServiceResponse<TPayload> Succeeded<TPayload>(string message, TPayload payload)
        {
            return new ServiceResponse<TPayload>(true, message)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<TPayload> Failed<TPayload>(
            ServiceResponseType responseType,
            string message)
        {
            return new ServiceResponse<TPayload>(false, message)
            {
                ResponseType = responseType,
            };
        }

        private sealed record CartProductResolution(
            bool Success,
            ServiceResponseType ResponseType,
            string Message,
            Product? Product,
            ProductVariant? Variant,
            string? SelectedAttributesJson)
        {
            public static CartProductResolution Succeeded(Product product, ProductVariant? variant, string? selectedAttributesJson)
            {
                return new CartProductResolution(true, ServiceResponseType.Success, "Product resolved.", product, variant, selectedAttributesJson);
            }

            public static CartProductResolution Failed(ServiceResponseType responseType, string message)
            {
                return new CartProductResolution(false, responseType, message, null, null, null);
            }
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

        private sealed record SelectedAttributesResolution(bool Success, string? AttributesJson, string ErrorMessage)
        {
            public static SelectedAttributesResolution Ok(string? attributesJson)
            {
                return new SelectedAttributesResolution(true, attributesJson, string.Empty);
            }

            public static SelectedAttributesResolution Failed(string message)
            {
                return new SelectedAttributesResolution(false, null, message);
            }
        }

        private sealed record PersonalizationValidation(bool Success, string Message)
        {
            public static PersonalizationValidation Succeeded()
            {
                return new PersonalizationValidation(true, string.Empty);
            }

            public static PersonalizationValidation Failed(string message)
            {
                return new PersonalizationValidation(false, message);
            }
        }

        private sealed record UnitPriceResolution(
            bool Success,
            ServiceResponseType ResponseType,
            string Message,
            decimal UnitPrice,
            decimal? BaseUnitPrice,
            string? BaseCurrencyCode,
            decimal? ExchangeRate,
            string? ExchangeRateProviderKey,
            string? ExchangeRateSource,
            DateTimeOffset? ExchangeRateEffectiveAtUtc,
            DateTimeOffset? ExchangeRateExpiresAtUtc)
        {
            public static UnitPriceResolution Succeeded(
                decimal unitPrice,
                decimal? baseUnitPrice,
                string? baseCurrencyCode,
                decimal? exchangeRate,
                string? exchangeRateProviderKey,
                string? exchangeRateSource,
                DateTimeOffset? exchangeRateEffectiveAtUtc,
                DateTimeOffset? exchangeRateExpiresAtUtc)
            {
                return new UnitPriceResolution(
                    true,
                    ServiceResponseType.Success,
                    "Unit price resolved.",
                    unitPrice,
                    baseUnitPrice,
                    baseCurrencyCode,
                    exchangeRate,
                    exchangeRateProviderKey,
                    exchangeRateSource,
                    exchangeRateEffectiveAtUtc,
                    exchangeRateExpiresAtUtc);
            }

            public static UnitPriceResolution Failed(ServiceResponseType responseType, string message)
            {
                return new UnitPriceResolution(false, responseType, message, 0m, null, null, null, null, null, null, null);
            }
        }

        private sealed record CartCurrencyResolution(
            string CurrencyCode,
            StorefrontCartValidationIssueDto? Issue);
    }
}
