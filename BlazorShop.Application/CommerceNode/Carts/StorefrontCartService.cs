namespace BlazorShop.Application.CommerceNode.Carts
{
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.Services;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;

    using Microsoft.Extensions.Options;

    public sealed class StorefrontCartService : IStorefrontCartService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly IStorefrontCartSessionService sessionService;
        private readonly IProductReadRepository productReadRepository;
        private readonly IStoreCurrencyResolver storeCurrencyResolver;
        private readonly IStorefrontWorkingCurrencyResolver workingCurrencyResolver;
        private readonly IMoneyConversionService moneyConversionService;
        private readonly IMoneyRoundingService moneyRoundingService;
        private readonly IProductSelectionResolver productSelectionResolver;
        private readonly StorefrontCartOptions cartOptions;

        public StorefrontCartService(
            IStorefrontCartSessionService sessionService,
            IProductReadRepository productReadRepository,
            IStoreCurrencyResolver storeCurrencyResolver,
            IStorefrontWorkingCurrencyResolver workingCurrencyResolver,
            IMoneyConversionService moneyConversionService,
            IMoneyRoundingService moneyRoundingService,
            IProductSelectionResolver productSelectionResolver,
            IOptions<StorefrontCartOptions> cartOptions)
        {
            this.sessionService = sessionService;
            this.productReadRepository = productReadRepository;
            this.storeCurrencyResolver = storeCurrencyResolver;
            this.workingCurrencyResolver = workingCurrencyResolver;
            this.moneyConversionService = moneyConversionService;
            this.moneyRoundingService = moneyRoundingService;
            this.productSelectionResolver = productSelectionResolver;
            this.cartOptions = cartOptions.Value;
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
                    var enrichedExisting = await this.EnrichCartAsync(existing.Payload, request.StoreId, cancellationToken);
                    return Succeeded(
                        "Cart session resolved.",
                        new StorefrontCartResult(enrichedExisting));
                }

                if (existing.ResponseType is not ServiceResponseType.NotFound)
                {
                    return Failed<StorefrontCartResult>(existing.ResponseType, existing.Message ?? "Cart session could not be resolved.");
                }
            }

            var created = await this.sessionService.CreateAsync(
                new StorefrontCartSessionCreateRequest(request.StoreId),
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
                new StorefrontCartResult(
                    await this.EnrichCartAsync(resolved.Payload, request.StoreId, cancellationToken),
                    created.Payload.Token));
        }

        public async Task<ServiceResponse<StorefrontCartSessionDto>> AttachOrMergeCurrentCustomerAsync(
            StorefrontCartAttachCurrentCustomerRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.StoreId == Guid.Empty)
            {
                return Failed<StorefrontCartSessionDto>(ServiceResponseType.ValidationError, "Store is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return Failed<StorefrontCartSessionDto>(ServiceResponseType.ValidationError, "Cart token is required.");
            }

            if (string.IsNullOrWhiteSpace(request.AppUserId))
            {
                return Failed<StorefrontCartSessionDto>(ServiceResponseType.ValidationError, "Customer identity was not found.");
            }

            return await this.EnrichCartResponseAsync(
                await this.sessionService.AttachOrMergeCurrentCustomerAsync(request, cancellationToken),
                request.StoreId,
                cancellationToken);
        }

        public async Task<ServiceResponse<StorefrontCartSessionDto>> GetAsync(
            Guid storeId,
            string token,
            CancellationToken cancellationToken = default)
        {
            return await this.EnrichCartResponseAsync(
                await this.sessionService.ResolveAsync(storeId, token, cancellationToken),
                storeId,
                cancellationToken);
        }

        public async Task<ServiceResponse<StorefrontCartSessionDto>> AddLineAsync(
            StorefrontCartAddLineRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.Quantity < 1)
            {
                return Failed<StorefrontCartSessionDto>(ServiceResponseType.ValidationError, "Quantity must be at least 1.");
            }

            if (request.Quantity > this.cartOptions.EffectiveMaxQuantityPerLine)
            {
                return Failed<StorefrontCartSessionDto>(
                    ServiceResponseType.ValidationError,
                    $"Quantity must be {this.cartOptions.EffectiveMaxQuantityPerLine} or fewer.");
            }

            var personalization = this.ValidatePersonalization(request.PersonalizationHash, request.PersonalizationJson, request.FulfillmentProviderKey);
            if (!personalization.Success)
            {
                return Failed<StorefrontCartSessionDto>(ServiceResponseType.ValidationError, personalization.Message);
            }

            var selection = await this.productSelectionResolver.ResolveAsync(
                new ProductSelectionRequest(
                    request.StoreId,
                    request.ProductId,
                    request.ProductVariantId,
                    request.SelectedAttributes,
                    Quantity: request.Quantity,
                    CurrencyCode: request.CurrencyCode,
                    Mode: ProductSelectionMode.Cart),
                cancellationToken);
            if (!selection.Success)
            {
                return Failed<StorefrontCartSessionDto>(selection.ResponseType, selection.Message);
            }

            var cartLimit = await this.ValidateCartLineLimitAsync(
                request,
                selection.SelectedAttributesJson,
                cancellationToken);
            if (!cartLimit.Success)
            {
                return Failed<StorefrontCartSessionDto>(cartLimit.ResponseType, cartLimit.Message);
            }

            return await this.EnrichCartResponseAsync(
                await this.sessionService.AddOrUpdateLineAsync(
                    new StorefrontCartLineMutationRequest(
                        request.StoreId,
                        request.Token,
                        selection.ProductId,
                        selection.ProductVariantId,
                        selection.SelectedAttributesJson,
                        NormalizeNullable(request.PersonalizationHash),
                        NormalizeNullable(request.PersonalizationJson),
                        request.ArtworkAssetId,
                        request.ArtworkVersion,
                        NormalizeNullable(request.FulfillmentProviderKey),
                        request.Quantity,
                        selection.UnitPrice,
                        selection.CurrencyCode,
                        selection.BaseUnitPrice,
                        selection.BaseCurrencyCode,
                        selection.ExchangeRate,
                        selection.ExchangeRateProviderKey,
                        selection.ExchangeRateSource,
                        selection.ExchangeRateEffectiveAtUtc,
                        selection.ExchangeRateExpiresAtUtc),
                    cancellationToken),
                request.StoreId,
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

            if (request.Quantity > this.cartOptions.EffectiveMaxQuantityPerLine)
            {
                return Failed<StorefrontCartSessionDto>(
                    ServiceResponseType.ValidationError,
                    $"Quantity must be {this.cartOptions.EffectiveMaxQuantityPerLine} or fewer.");
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

            var selection = await this.productSelectionResolver.ResolveAsync(
                new ProductSelectionRequest(
                    request.StoreId,
                    line.ProductId,
                    line.ProductVariantId,
                    SelectedAttributesJson: line.SelectedAttributesJson,
                    Quantity: request.Quantity,
                    CurrencyCode: line.CurrencyCodeSnapshot,
                    Mode: ProductSelectionMode.Cart),
                cancellationToken);
            if (!selection.Success)
            {
                return Failed<StorefrontCartSessionDto>(selection.ResponseType, selection.Message);
            }

            return await this.EnrichCartResponseAsync(
                await this.sessionService.UpdateLineQuantityAsync(
                    request.StoreId,
                    request.Token,
                    request.LineId,
                    request.Quantity,
                    cancellationToken),
                request.StoreId,
                cancellationToken);
        }

        public async Task<ServiceResponse<StorefrontCartSessionDto>> RemoveLineAsync(
            Guid storeId,
            string token,
            Guid lineId,
            CancellationToken cancellationToken = default)
        {
            return await this.EnrichCartResponseAsync(
                await this.sessionService.RemoveLineAsync(storeId, token, lineId, cancellationToken),
                storeId,
                cancellationToken);
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

            return await this.EnrichCartResponseAsync(current, storeId, cancellationToken);
        }

        private async Task<CartLimitValidation> ValidateCartLineLimitAsync(
            StorefrontCartAddLineRequest request,
            string? selectedAttributesJson,
            CancellationToken cancellationToken)
        {
            var cart = await this.sessionService.ResolveAsync(request.StoreId, request.Token, cancellationToken);
            if (!cart.Success || cart.Payload is null)
            {
                return CartLimitValidation.Succeeded();
            }

            var lineKey = BuildLineKey(
                request.ProductId,
                request.ProductVariantId,
                selectedAttributesJson,
                request.PersonalizationHash,
                request.ArtworkAssetId,
                request.ArtworkVersion,
                request.FulfillmentProviderKey);
            if (cart.Payload.Lines.Any(line => string.Equals(line.LineKey, lineKey, StringComparison.Ordinal)))
            {
                return CartLimitValidation.Succeeded();
            }

            return cart.Payload.Lines.Count >= this.cartOptions.EffectiveMaxLines
                ? CartLimitValidation.Failed(
                    ServiceResponseType.Conflict,
                    $"Cart can contain at most {this.cartOptions.EffectiveMaxLines} lines.")
                : CartLimitValidation.Succeeded();
        }

        private async Task<ServiceResponse<StorefrontCartSessionDto>> EnrichCartResponseAsync(
            ServiceResponse<StorefrontCartSessionDto> response,
            Guid storeId,
            CancellationToken cancellationToken)
        {
            if (!response.Success || response.Payload is null)
            {
                return response;
            }

            return Succeeded(
                response.Message ?? "Cart session resolved.",
                await this.EnrichCartAsync(response.Payload, storeId, cancellationToken));
        }

        private async Task<StorefrontCartSessionDto> EnrichCartAsync(
            StorefrontCartSessionDto cart,
            Guid storeId,
            CancellationToken cancellationToken)
        {
            var baseCurrencyCode = await this.storeCurrencyResolver.ResolveDefaultCurrencyCodeAsync(storeId, cancellationToken);
            var currencyResolution = await this.ResolveCartSnapshotCurrencyAsync(
                storeId,
                cart.Lines,
                baseCurrencyCode,
                cancellationToken);
            var currencyCode = currencyResolution.CurrencyCode;
            var cartWarnings = new List<StorefrontCartWarningDto>();
            if (currencyResolution.Issue is not null)
            {
                cartWarnings.Add(ToCartWarning(currencyResolution.Issue));
            }

            var enrichedLines = new List<StorefrontCartLineDto>(cart.Lines.Count);
            decimal subtotal = 0m;
            var checkoutAllowed = true;
            foreach (var line in cart.Lines)
            {
                var enrichedLine = await this.EnrichLineAsync(storeId, line, currencyCode, cancellationToken);
                enrichedLines.Add(enrichedLine);
                subtotal += enrichedLine.LineTotal ?? 0m;
                if (!enrichedLine.Purchasable || (enrichedLine.Warnings?.Count ?? 0) > 0)
                {
                    checkoutAllowed = false;
                }
            }

            subtotal = this.moneyRoundingService.RoundOrderTotal(subtotal, currencyCode);
            var grandTotal = subtotal;

            return cart with
            {
                Lines = enrichedLines,
                CurrencyCode = currencyCode,
                SummaryCount = enrichedLines.Sum(line => Math.Max(0, line.Quantity)),
                Subtotal = subtotal,
                DiscountTotal = 0m,
                ShippingEstimate = 0m,
                TaxEstimate = 0m,
                GrandTotal = grandTotal,
                CheckoutAllowed = checkoutAllowed && cartWarnings.Count == 0,
                Warnings = cartWarnings,
                Adjustments =
                [
                    new StorefrontCartAdjustmentDto("subtotal", "Subtotal", subtotal, currencyCode),
                    new StorefrontCartAdjustmentDto("discount", "Discount", 0m, currencyCode),
                    new StorefrontCartAdjustmentDto("shipping", "Shipping estimate", 0m, currencyCode),
                    new StorefrontCartAdjustmentDto("tax", "Tax estimate", 0m, currencyCode),
                    new StorefrontCartAdjustmentDto("total", "Total", grandTotal, currencyCode),
                ],
            };
        }

        private async Task<StorefrontCartLineDto> EnrichLineAsync(
            Guid storeId,
            StorefrontCartLineDto line,
            string currencyCode,
            CancellationToken cancellationToken)
        {
            var warnings = new List<StorefrontCartWarningDto>();
            var selectedAttributes = DeserializeSelectedAttributes(line.SelectedAttributesJson);
            var product = await this.productReadRepository.GetPublishedProductDetailsByIdAsync(line.ProductId);
            if (product is null || !IsStorefrontAvailable(product, storeId))
            {
                warnings.Add(new StorefrontCartWarningDto(
                    "cart.line.product_unavailable",
                    "Product is no longer available.",
                    line.Id,
                    line.ProductId));

                var unavailableUnitPrice = line.UnitPriceSnapshot.HasValue
                    ? this.moneyRoundingService.RoundUnitPrice(line.UnitPriceSnapshot.Value, currencyCode)
                    : 0m;
                var unavailableLineTotal = this.moneyRoundingService.RoundLineTotal(unavailableUnitPrice * line.Quantity, currencyCode);
                return line with
                {
                    DisplayName = "Unavailable product",
                    SelectedAttributes = selectedAttributes,
                    UnitPrice = unavailableUnitPrice,
                    LineSubtotal = unavailableLineTotal,
                    LineTotal = unavailableLineTotal,
                    QuantityMinimum = 1,
                    QuantityStep = 1,
                    Purchasable = false,
                    Warnings = warnings,
                };
            }

            var variant = line.ProductVariantId.HasValue
                ? product.Variants.FirstOrDefault(candidate => candidate.Id == line.ProductVariantId.Value)
                : null;
            if (line.ProductVariantId.HasValue && variant is null)
            {
                warnings.Add(new StorefrontCartWarningDto(
                    "cart.line.variant_unavailable",
                    "Selected product variant is no longer available.",
                    line.Id,
                    line.ProductId));
            }
            else if (variant?.IsActive == false)
            {
                warnings.Add(new StorefrontCartWarningDto(
                    "cart.line.variant_inactive",
                    "Selected product variant is no longer active.",
                    line.Id,
                    line.ProductId));
            }

            var minQuantity = Math.Max(1, product.MinOrderQuantity);
            var quantityStep = Math.Max(1, product.QuantityStep);
            var availableQuantity = product.ManageStock ? variant?.Stock ?? product.Quantity : (int?)null;
            var maxQuantity = product.MaxOrderQuantity;
            if (availableQuantity.HasValue)
            {
                maxQuantity = maxQuantity.HasValue
                    ? Math.Min(maxQuantity.Value, availableQuantity.Value)
                    : availableQuantity.Value;
            }
            var effectiveMaxQuantity = maxQuantity.HasValue
                ? Math.Min(maxQuantity.Value, this.cartOptions.EffectiveMaxQuantityPerLine)
                : this.cartOptions.EffectiveMaxQuantityPerLine;

            if (product.PurchasingDisabled)
            {
                warnings.Add(new StorefrontCartWarningDto(
                    ProductPurchaseBlockReasons.PurchaseDisabled,
                    string.IsNullOrWhiteSpace(product.PurchasingDisabledReason)
                        ? "Product cannot be purchased right now."
                        : product.PurchasingDisabledReason.Trim(),
                    line.Id,
                    line.ProductId));
            }

            if (line.Quantity < minQuantity)
            {
                warnings.Add(new StorefrontCartWarningDto(
                    ProductPurchaseBlockReasons.BelowMinQuantity,
                    $"Minimum quantity is {minQuantity}.",
                    line.Id,
                    line.ProductId));
            }

            if (maxQuantity.HasValue && line.Quantity > maxQuantity.Value)
            {
                warnings.Add(new StorefrontCartWarningDto(
                    availableQuantity.HasValue ? ProductPurchaseBlockReasons.NotEnoughStock : ProductPurchaseBlockReasons.AboveMaxQuantity,
                    $"Maximum quantity is {maxQuantity.Value}.",
                    line.Id,
                    line.ProductId));
            }

            if (line.Quantity > this.cartOptions.EffectiveMaxQuantityPerLine)
            {
                warnings.Add(new StorefrontCartWarningDto(
                    ProductPurchaseBlockReasons.AboveMaxQuantity,
                    $"Maximum quantity is {this.cartOptions.EffectiveMaxQuantityPerLine}.",
                    line.Id,
                    line.ProductId));
            }

            if (line.Quantity >= minQuantity && ((line.Quantity - minQuantity) % quantityStep) != 0)
            {
                warnings.Add(new StorefrontCartWarningDto(
                    ProductPurchaseBlockReasons.InvalidQuantityStep,
                    $"Quantity must increase by {quantityStep}.",
                    line.Id,
                    line.ProductId));
            }

            var unitPrice = line.UnitPriceSnapshot
                ?? variant?.Price
                ?? product.Price;
            unitPrice = this.moneyRoundingService.RoundUnitPrice(unitPrice, currencyCode);
            var lineTotal = this.moneyRoundingService.RoundLineTotal(unitPrice * line.Quantity, currencyCode);
            var displayName = ResolveLineDisplayName(product, variant);
            var displayAttributes = ResolveLineSelectedAttributes(selectedAttributes, variant);

            return line with
            {
                DisplayName = displayName,
                ProductSlug = product.Slug,
                ProductUrl = string.IsNullOrWhiteSpace(product.Slug) ? null : $"/products/{product.Slug.Trim()}",
                ImageUrl = product.Image,
                SelectedAttributes = displayAttributes,
                UnitPrice = unitPrice,
                LineSubtotal = lineTotal,
                LineTotal = lineTotal,
                QuantityMinimum = minQuantity,
                QuantityMaximum = effectiveMaxQuantity,
                QuantityStep = quantityStep,
                AllowedQuantities = null,
                Purchasable = warnings.Count == 0,
                Warnings = warnings,
            };
        }

        private static StorefrontCartWarningDto ToCartWarning(StorefrontCartValidationIssueDto issue)
        {
            return new StorefrontCartWarningDto(issue.Code, issue.Message, issue.LineId, issue.ProductId);
        }

        private static string ResolveLineDisplayName(Product product, ProductVariant? variant)
        {
            return string.IsNullOrWhiteSpace(product.Name) ? "Product" : product.Name.Trim();
        }

        private static IReadOnlyList<SelectedAttributeDto> ResolveLineSelectedAttributes(
            IReadOnlyList<SelectedAttributeDto> selectedAttributes,
            ProductVariant? variant)
        {
            if (selectedAttributes.Count > 0 || variant is null)
            {
                return selectedAttributes;
            }

            var variantAttributes = ProductVariantAttributeNormalizer.Deserialize(variant.AttributesJson);
            if (variantAttributes.Count == 0 && !string.IsNullOrWhiteSpace(variant.DisplayName))
            {
                return [new SelectedAttributeDto("Variant", variant.DisplayName.Trim())];
            }

            return variantAttributes
                .Select(attribute => new SelectedAttributeDto(attribute.Name, attribute.Value))
                .ToArray();
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
                var selection = await this.productSelectionResolver.ResolveAsync(
                    new ProductSelectionRequest(
                        storeId,
                        line.ProductId,
                        line.ProductVariantId,
                        SelectedAttributesJson: line.SelectedAttributesJson,
                        Quantity: line.Quantity,
                        CurrencyCode: currencyCode,
                        Mode: ProductSelectionMode.Cart),
                    cancellationToken);
                if (!selection.Success)
                {
                    issues.Add(new StorefrontCartValidationIssueDto(
                        line.Id,
                        line.ProductId,
                        SelectionIssueCode(selection),
                        selection.Message));
                    continue;
                }

                var unitPrice = line.UnitPriceSnapshot.HasValue
                    ? this.moneyRoundingService.RoundUnitPrice(line.UnitPriceSnapshot.Value, currencyCode)
                    : selection.UnitPrice;
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

        public async Task<ServiceResponse<StorefrontCartSessionDto>> RecalculateAsync(
            StorefrontCartRecalculateRequest request,
            CancellationToken cancellationToken = default)
        {
            var cart = await this.sessionService.ResolveAsync(request.StoreId, request.Token, cancellationToken);
            if (!cart.Success || cart.Payload is null)
            {
                return cart;
            }

            if (request.ExpectedVersion.HasValue && request.ExpectedVersion.Value != cart.Payload.Version)
            {
                return Failed<StorefrontCartSessionDto>(
                    ServiceResponseType.Conflict,
                    "Cart has changed. Refresh the cart and try again.");
            }

            var updates = new List<StorefrontCartLineSnapshotUpdate>();
            foreach (var line in cart.Payload.Lines)
            {
                var selection = await this.productSelectionResolver.ResolveAsync(
                    new ProductSelectionRequest(
                        request.StoreId,
                        line.ProductId,
                        line.ProductVariantId,
                        SelectedAttributesJson: line.SelectedAttributesJson,
                        Quantity: line.Quantity,
                        CurrencyCode: line.CurrencyCodeSnapshot,
                        Mode: ProductSelectionMode.Cart),
                    cancellationToken);
                if (!selection.Success)
                {
                    continue;
                }

                updates.Add(new StorefrontCartLineSnapshotUpdate(
                    line.Id,
                    selection.UnitPrice,
                    selection.CurrencyCode,
                    selection.BaseUnitPrice,
                    selection.BaseCurrencyCode,
                    selection.ExchangeRate,
                    selection.ExchangeRateProviderKey,
                    selection.ExchangeRateSource,
                    selection.ExchangeRateEffectiveAtUtc,
                    selection.ExchangeRateExpiresAtUtc));
            }

            if (updates.Count == 0)
            {
                return await this.EnrichCartResponseAsync(cart, request.StoreId, cancellationToken);
            }

            return await this.EnrichCartResponseAsync(
                await this.sessionService.UpdateLineSnapshotsAsync(
                    request.StoreId,
                    request.Token,
                    updates,
                    cancellationToken),
                request.StoreId,
                cancellationToken);
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

        private PersonalizationValidation ValidatePersonalization(
            string? personalizationHash,
            string? personalizationJson,
            string? fulfillmentProviderKey)
        {
            if (NormalizeNullable(personalizationHash) is { Length: var hashLength }
                && hashLength > this.cartOptions.EffectiveMaxPersonalizationHashLength)
            {
                return PersonalizationValidation.Failed(
                    $"Personalization hash must be {this.cartOptions.EffectiveMaxPersonalizationHashLength} characters or fewer.");
            }

            if (NormalizeNullable(personalizationJson) is { Length: var payloadLength }
                && payloadLength > this.cartOptions.EffectiveMaxPersonalizationJsonLength)
            {
                return PersonalizationValidation.Failed(
                    $"Personalization payload must be {this.cartOptions.EffectiveMaxPersonalizationJsonLength} characters or fewer.");
            }

            if (NormalizeNullable(fulfillmentProviderKey) is { Length: var providerLength }
                && providerLength > this.cartOptions.EffectiveMaxFulfillmentProviderKeyLength)
            {
                return PersonalizationValidation.Failed(
                    $"Fulfillment provider key must be {this.cartOptions.EffectiveMaxFulfillmentProviderKeyLength} characters or fewer.");
            }

            return PersonalizationValidation.Succeeded();
        }

        private static string BuildLineKey(
            Guid productId,
            Guid? productVariantId,
            string? selectedAttributesJson,
            string? personalizationHash,
            Guid? artworkAssetId,
            int? artworkVersion,
            string? fulfillmentProviderKey)
        {
            var material = string.Join(
                "|",
                productId.ToString("N"),
                productVariantId?.ToString("N") ?? string.Empty,
                NormalizeNullable(selectedAttributesJson) ?? string.Empty,
                NormalizeNullable(personalizationHash) ?? string.Empty,
                artworkAssetId?.ToString("N") ?? string.Empty,
                artworkVersion?.ToString() ?? string.Empty,
                NormalizeNullable(fulfillmentProviderKey)?.ToLowerInvariant() ?? string.Empty);

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
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

        private static string SelectionIssueCode(ProductSelectionResult selection)
        {
            return selection.PurchaseBlockReasons.Count > 0
                ? selection.PurchaseBlockReasons[0]
                : ResponseTypeToCode(selection.ResponseType);
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

        private sealed record CartLimitValidation(bool Success, ServiceResponseType ResponseType, string Message)
        {
            public static CartLimitValidation Succeeded()
            {
                return new CartLimitValidation(true, ServiceResponseType.Success, string.Empty);
            }

            public static CartLimitValidation Failed(ServiceResponseType responseType, string message)
            {
                return new CartLimitValidation(false, responseType, message);
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
