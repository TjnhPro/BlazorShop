namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.IdentityModel.Tokens.Jwt;

    using BlazorShop.Application.CommerceNode.Addresses;
    using BlazorShop.Application.CommerceNode.Captcha;
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Consent;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Domain.Contracts;

    public static class StorefrontContractMappings
    {
        public static CreateUser ToApplicationRequest(this StorefrontRegisterRequest request)
        {
            return new CreateUser
            {
                FullName = request.FullName,
                Email = request.Email,
                Password = request.Password,
                ConfirmPassword = request.ConfirmPassword,
                CaptchaToken = request.CaptchaToken,
            };
        }

        public static LoginUser ToApplicationRequest(this StorefrontLoginRequest request)
        {
            return new LoginUser
            {
                Email = request.Email,
                Password = request.Password,
                CaptchaToken = request.CaptchaToken,
            };
        }

        public static ChangePassword ToApplicationRequest(this StorefrontChangePasswordRequest request)
        {
            return new ChangePassword
            {
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword,
                ConfirmPassword = request.ConfirmPassword,
            };
        }

        public static UpdateProfile ToApplicationRequest(this StorefrontUpdateProfileRequest request)
        {
            return new UpdateProfile
            {
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
            };
        }

        public static ProductCatalogQuery ToApplicationQuery(this StorefrontProductCatalogQuery query)
        {
            return new ProductCatalogQuery
            {
                PageNumber = query.PageNumber,
                PageSize = Math.Clamp(query.PageSize, 1, StorefrontContractValidation.MaxPageSize),
                CategoryId = query.CategoryId,
                CategorySlug = query.CategorySlug,
                IncludeSubcategories = query.IncludeSubcategories,
                SearchTerm = query.SearchTerm,
                MinPrice = query.MinPrice,
                MaxPrice = query.MaxPrice,
                InStock = query.InStock,
                SortBy = ToApplicationSortBy(query.SortBy),
                CreatedAfterUtc = query.CreatedAfterUtc,
            };
        }

        public static CheckoutShippingAddress ToApplicationRequest(this StorefrontCheckoutShippingAddress request)
        {
            return new CheckoutShippingAddress
            {
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                Address1 = request.Address1,
                Address2 = request.Address2,
                City = request.City,
                State = request.State,
                PostalCode = request.PostalCode,
                CountryCode = request.CountryCode,
            };
        }

        public static StorefrontCheckoutShippingAddress ToStorefrontContract(this CheckoutShippingAddress address)
        {
            return new StorefrontCheckoutShippingAddress
            {
                FullName = address.FullName,
                Email = address.Email,
                Phone = address.Phone,
                Address1 = address.Address1,
                Address2 = address.Address2,
                City = address.City,
                State = address.State,
                PostalCode = address.PostalCode,
                CountryCode = address.CountryCode,
            };
        }

        public static StorefrontAddressCountryResponse ToStorefrontContract(this AddressCountryDto country)
        {
            return new StorefrontAddressCountryResponse(
                country.Code,
                country.Name,
                country.PostalCodeRequired,
                country.StateProvinceRequired);
        }

        public static StorefrontAddressStateProvinceResponse ToStorefrontContract(this AddressStateProvinceDto state)
        {
            return new StorefrontAddressStateProvinceResponse(state.Code, state.Name);
        }

        public static StorefrontAddressFieldConfigurationResponse ToStorefrontContract(this AddressFieldConfigurationDto configuration)
        {
            return new StorefrontAddressFieldConfigurationResponse(
                configuration.CompanyEnabled,
                configuration.PhoneEnabled,
                configuration.PhoneRequired,
                configuration.PostalCodeRequired,
                configuration.BillingAddressEnabled,
                configuration.UseShippingAddressAsBillingDefault,
                configuration.FirstNameMaxLength,
                configuration.LastNameMaxLength,
                configuration.CompanyMaxLength,
                configuration.AddressLineMaxLength,
                configuration.CityMaxLength,
                configuration.PostalCodeMaxLength,
                configuration.StateProvinceCodeMaxLength,
                configuration.StateProvinceNameMaxLength,
                configuration.PhoneMaxLength,
                configuration.EmailMaxLength,
                configuration.StateProvinceRequiredCountryCodes);
        }

        public static BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutPreviewRequest ToApplicationRequest(
            this Contracts.Storefront.StorefrontCheckoutPreviewRequest request,
            Guid storeId,
            string cartToken)
        {
            return new BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutPreviewRequest(
                storeId,
                cartToken,
                request.ExpectedCartVersion,
                request.CustomerEmail,
                request.CustomerName,
                request.PaymentMethodKey,
                request.ShippingAddress.ToPreviewShippingAddress());
        }

        public static StorefrontCheckoutShippingAddressDto ToPreviewShippingAddress(this StorefrontCheckoutShippingAddress request)
        {
            return new StorefrontCheckoutShippingAddressDto(
                request.FullName,
                request.Email,
                request.Phone,
                request.Address1,
                request.Address2,
                request.City,
                request.State,
                request.PostalCode,
                request.CountryCode);
        }

        public static StorefrontCheckoutPreviewResponse ToStorefrontContract(this StorefrontCheckoutPreviewResult result)
        {
            return new StorefrontCheckoutPreviewResponse(
                result.CheckoutSessionId,
                result.CartId,
                result.CartVersion,
                result.State,
                result.IsValid,
                result.NextAction,
                result.CustomerEmail,
                result.CustomerName,
                result.PaymentMethodKey,
                result.Subtotal,
                result.ShippingTotal,
                result.TaxTotal,
                result.DiscountTotal,
                result.GrandTotal,
                result.CurrencyCode,
                result.ExpiresAtUtc,
                result.Lines.Select(line => new StorefrontCheckoutLineSummaryResponse(
                    line.LineId,
                    line.ProductId,
                    line.ProductVariantId,
                    line.Quantity,
                    line.UnitPrice,
                    line.LineTotal,
                    line.CurrencyCode)).ToArray(),
                result.Issues.Select(issue => new StorefrontCheckoutValidationIssueResponse(
                    issue.Code,
                    issue.Message,
                    issue.Field,
                    issue.LineId,
                    issue.ProductId)).ToArray());
        }

        public static BlazorShop.Application.CommerceNode.Checkout.StorefrontPlaceOrderRequest ToApplicationRequest(
            this Contracts.Storefront.StorefrontPlaceOrderRequest request,
            Guid storeId)
        {
            return new BlazorShop.Application.CommerceNode.Checkout.StorefrontPlaceOrderRequest(
                storeId,
                request.CheckoutSessionId,
                request.ExpectedCartVersion,
                request.IdempotencyKey);
        }

        public static StorefrontPlaceOrderResponse ToStorefrontContract(this StorefrontPlaceOrderResult result)
        {
            var nextAction = string.IsNullOrWhiteSpace(result.NextActionType)
                ? null
                : new StorefrontPaymentNextActionResponse(result.NextActionType, result.NextActionUrl);

            return new StorefrontPlaceOrderResponse(
                result.CheckoutSessionId,
                result.PaymentAttemptId,
                result.OrderId,
                result.Reference,
                result.OrderStatus,
                result.PaymentStatus,
                result.PaymentMethodKey,
                result.TotalAmount,
                result.CurrencyCode,
                result.IdempotencyKey,
                result.CreatedOn,
                nextAction);
        }

        public static StorefrontPaymentAttemptResponse ToStorefrontContract(this PaymentAttemptDto result)
        {
            var nextAction = string.IsNullOrWhiteSpace(result.NextActionType)
                ? null
                : new StorefrontPaymentNextActionResponse(result.NextActionType, result.NextActionUrl);

            return new StorefrontPaymentAttemptResponse(
                result.Id,
                result.CheckoutSessionId,
                result.OrderId,
                result.PaymentMethodKey,
                result.ProviderKey,
                result.State,
                result.Amount,
                result.CurrencyCode,
                result.ProviderReference,
                result.ProviderSessionId,
                nextAction,
                result.FailureCode,
                result.FailureMessage,
                result.ExpiresAtUtc,
                result.CreatedAtUtc,
                result.UpdatedAtUtc);
        }

        public static ProcessCart ToProcessCart(this StorefrontCartItemRequest request)
        {
            return new ProcessCart
            {
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                SelectedAttributes = request.SelectedAttributes,
                Quantity = request.Quantity,
            };
        }

        public static StorefrontCartItemRequest ToStorefrontContract(this ProcessCart request)
        {
            return new StorefrontCartItemRequest
            {
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                SelectedAttributes = request.SelectedAttributes,
                Quantity = request.Quantity,
            };
        }

        public static StorefrontCartAddLineRequest ToApplicationRequest(
            this StorefrontCartLineCreateRequest request,
            Guid storeId,
            string cartToken)
        {
            return new StorefrontCartAddLineRequest(
                storeId,
                cartToken,
                request.ProductId,
                request.ProductVariantId,
                request.SelectedAttributes,
                request.PersonalizationHash,
                request.PersonalizationJson,
                request.ArtworkAssetId,
                request.ArtworkVersion,
                request.FulfillmentProviderKey,
                request.Quantity,
                request.CurrencyCode);
        }

        public static StorefrontCurrencyPreferenceResponse ToStorefrontContract(
            this StorefrontWorkingCurrencyResolution resolution)
        {
            return new StorefrontCurrencyPreferenceResponse(
                resolution.CurrencyCode,
                resolution.BaseCurrencyCode,
                resolution.RequestedCurrencyCode,
                resolution.RequestedCurrencySupported,
                resolution.CheckoutCurrencyEnabled,
                resolution.Reason);
        }

        public static StorefrontCartUpdateLineRequest ToApplicationRequest(
            this StorefrontCartLineUpdateRequest request,
            Guid storeId,
            string cartToken,
            Guid lineId)
        {
            return new StorefrontCartUpdateLineRequest(
                storeId,
                cartToken,
                lineId,
                request.Quantity);
        }

        public static StorefrontCartSessionResponse ToSessionContract(this StorefrontCartResult result, string? fallbackCartToken = null)
        {
            return new StorefrontCartSessionResponse(
                result.Cart.PublicId,
                result.Token ?? fallbackCartToken ?? string.Empty,
                result.Cart.State,
                result.Cart.Version,
                result.Cart.ExpiresAtUtc);
        }

        public static StorefrontCartResponse ToStorefrontContract(this StorefrontCartSessionDto cart)
        {
            return new StorefrontCartResponse(
                cart.PublicId,
                cart.State,
                cart.Version,
                cart.LastActivityAtUtc,
                cart.ExpiresAtUtc,
                cart.Lines.Select(line => line.ToStorefrontContract()).ToArray(),
                cart.CurrencyCode,
                cart.SummaryCount,
                cart.Subtotal,
                cart.DiscountTotal,
                cart.ShippingEstimate,
                cart.TaxEstimate,
                cart.GrandTotal,
                cart.CheckoutAllowed,
                cart.Warnings?.Select(warning => warning.ToStorefrontContract()).ToArray() ?? [],
                cart.Adjustments?.Select(adjustment => adjustment.ToStorefrontContract()).ToArray() ?? []);
        }

        public static StorefrontCartLineResponse ToStorefrontContract(this StorefrontCartLineDto line)
        {
            return new StorefrontCartLineResponse(
                line.Id,
                line.ProductId,
                line.ProductVariantId,
                line.SelectedAttributesJson,
                line.PersonalizationHash,
                line.PersonalizationJson,
                line.ArtworkAssetId,
                line.ArtworkVersion,
                line.FulfillmentProviderKey,
                line.Quantity,
                line.UnitPriceSnapshot,
                line.CurrencyCodeSnapshot,
                line.DisplayName,
                line.ProductSlug,
                line.ProductUrl,
                line.ImageUrl,
                line.SelectedAttributes?
                    .Select(attribute => new StorefrontCartSelectedAttributeResponse(attribute.Name, attribute.Value))
                    .ToArray() ?? [],
                line.UnitPrice,
                line.LineSubtotal,
                line.LineTotal,
                line.QuantityMinimum,
                line.QuantityMaximum,
                line.QuantityStep,
                line.AllowedQuantities,
                line.Purchasable,
                line.Warnings?.Select(warning => warning.ToStorefrontContract()).ToArray() ?? []);
        }

        public static StorefrontCartWarningResponse ToStorefrontContract(this StorefrontCartWarningDto warning)
        {
            return new StorefrontCartWarningResponse(
                warning.Code,
                warning.Message,
                warning.LineId,
                warning.ProductId);
        }

        public static StorefrontCartAdjustmentResponse ToStorefrontContract(this StorefrontCartAdjustmentDto adjustment)
        {
            return new StorefrontCartAdjustmentResponse(
                adjustment.Code,
                adjustment.Label,
                adjustment.Amount,
                adjustment.CurrencyCode);
        }

        public static StorefrontCartValidationResponse ToStorefrontContract(this StorefrontCartValidationResult validation)
        {
            return new StorefrontCartValidationResponse(
                validation.CartPublicId,
                validation.Version,
                validation.IsValid,
                validation.TotalAmount,
                validation.CurrencyCode,
                validation.Issues.Select(issue => issue.ToStorefrontContract()).ToArray());
        }

        public static StorefrontCartValidationIssueResponse ToStorefrontContract(this StorefrontCartValidationIssueDto issue)
        {
            return new StorefrontCartValidationIssueResponse(
                issue.LineId,
                issue.ProductId,
                issue.Code,
                issue.Message);
        }

        public static CreateOrderItem ToCreateOrderItem(this StorefrontOrderItemRequest request)
        {
            return new CreateOrderItem
            {
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                SelectedAttributes = request.SelectedAttributes,
                Quantity = request.Quantity,
            };
        }

        public static StorefrontTokenResponse ToStorefrontTokenContract(this LoginResponse response)
        {
            return new StorefrontTokenResponse(response.Token, ResolveAccessTokenExpiration(response.Token));
        }

        public static StorefrontCategoryResponse ToStorefrontContract(this GetCategory category)
        {
            return new StorefrontCategoryResponse(
                category.Id,
                category.ParentCategoryId,
                category.Name,
                category.Description,
                category.Slug,
                category.Image,
                category.DisplayOrder,
                category.UpdatedAt,
                category.MetaTitle,
                category.MetaDescription,
                category.CanonicalUrl,
                category.OgTitle,
                category.OgDescription,
                category.OgImage,
                category.SeoContent,
                category.RobotsIndex,
                category.RobotsFollow);
        }

        public static StorefrontCategoryTreeNodeResponse ToStorefrontContract(this GetCategoryTreeNode category)
        {
            return new StorefrontCategoryTreeNodeResponse(
                category.Id,
                category.ParentCategoryId,
                category.Name,
                category.Slug,
                category.Image,
                category.DisplayOrder,
                category.Children.Select(child => child.ToStorefrontContract()).ToArray());
        }

        public static StorefrontCategoryPageResponse ToStorefrontContract(this GetCategoryPage page)
        {
            return new StorefrontCategoryPageResponse(
                page.Category.ToStorefrontContract(),
                page.Breadcrumbs.Select(crumb => crumb.ToStorefrontContract()).ToArray(),
                page.Products.Select(product => product.ToStorefrontContract()).ToArray(),
                page.DirectProductCount,
                page.DescendantProductCount);
        }

        public static StorefrontCategoryPageResponse ToStorefrontContract(
            this GetCategoryPage page,
            Func<GetCatalogProduct, StorefrontCatalogProductResponse> mapProduct)
        {
            return new StorefrontCategoryPageResponse(
                page.Category.ToStorefrontContract(),
                page.Breadcrumbs.Select(crumb => crumb.ToStorefrontContract()).ToArray(),
                page.Products.Select(mapProduct).ToArray(),
                page.DirectProductCount,
                page.DescendantProductCount);
        }

        public static StorefrontCategoryBreadcrumbItemResponse ToStorefrontContract(this GetCategoryBreadcrumbItem crumb)
        {
            return new StorefrontCategoryBreadcrumbItemResponse(
                crumb.Id,
                crumb.Name,
                crumb.Slug);
        }

        public static StorefrontCatalogProductResponse ToStorefrontContract(
            this GetCatalogProduct product,
            StorefrontDisplayMoney? displayMoney = null)
        {
            return new StorefrontCatalogProductResponse(
                product.Id,
                product.Slug,
                product.Name,
                product.Description,
                product.Sku,
                product.ShortDescription,
                product.Price,
                product.ComparePrice,
                product.Image,
                product.PrimaryMediaPublicId,
                product.HasPrimaryMedia,
                product.Quantity,
                product.CreatedOn,
                product.UpdatedAt,
                product.DisplayOrder,
                product.InStock,
                IsCatalogProductPurchasable(product),
                ResolveCatalogProductPurchaseBlockReasons(product),
                ResolveCatalogProductStockStatus(product),
                ResolveCatalogProductAvailableQuantity(product),
                product.MinOrderQuantity,
                product.MaxOrderQuantity,
                product.QuantityStep,
                product.ManageStock,
                product.ShippingRequired,
                product.FreeShipping,
                product.DeliveryEstimateText,
                product.PublishedOn,
                product.CategoryId,
                product.CategoryName,
                product.CategorySlug,
                product.HasVariants,
                product.ProductType,
                product.VariationTemplateId,
                displayMoney?.Price,
                displayMoney?.ComparePrice,
                displayMoney?.CurrencyCode);
        }

        public static StorefrontProductResponse ToStorefrontContract(
            this GetProduct product,
            StorefrontDisplayMoney? displayMoney = null,
            Func<GetProductVariant, GetProduct, StorefrontProductVariantResponse>? mapVariant = null)
        {
            var purchaseBlockReasons = ResolveProductPurchaseBlockReasons(product);
            return new StorefrontProductResponse(
                product.Id,
                product.Slug,
                product.Name,
                product.Description,
                product.Sku,
                product.ShortDescription,
                product.FullDescription,
                product.Price,
                product.ComparePrice,
                product.Weight,
                product.Length,
                product.Width,
                product.Height,
                product.Image,
                product.Quantity,
                purchaseBlockReasons.Count == 0,
                purchaseBlockReasons,
                ResolveProductStockStatus(product),
                ResolveProductAvailableQuantity(product),
                product.MinOrderQuantity,
                product.MaxOrderQuantity,
                product.QuantityStep,
                product.ManageStock,
                product.ShippingRequired,
                product.FreeShipping,
                product.DeliveryEstimateText,
                product.DisplayOrder,
                IsProductInStock(product),
                product.PublishedOn,
                product.ProductType,
                product.VariationTemplateId,
                product.CategoryId,
                product.MetaTitle,
                product.MetaDescription,
                product.CanonicalUrl,
                product.OgTitle,
                product.OgDescription,
                product.OgImage,
                product.SeoContent,
                product.RobotsIndex,
                product.RobotsFollow,
                product.Category?.ToStorefrontContract(),
                product.VariationTemplate,
                product.CreatedOn,
                product.UpdatedAt,
                product.Variants.Select(variant => mapVariant is null
                    ? variant.ToStorefrontContract(product: product)
                    : mapVariant(variant, product)).ToArray(),
                displayMoney?.Price,
                displayMoney?.ComparePrice,
                displayMoney?.CurrencyCode);
        }

        public static StorefrontProductVariantResponse ToStorefrontContract(
            this GetProductVariant variant,
            StorefrontDisplayMoney? displayMoney = null,
            GetProduct? product = null)
        {
            var purchaseBlockReasons = ResolveVariantPurchaseBlockReasons(variant, product);
            return new StorefrontProductVariantResponse(
                variant.Id,
                variant.ProductId,
                variant.Sku,
                variant.Attributes.Select(attribute => attribute.ToStorefrontContract()).ToArray(),
                variant.AttributeSignature,
                variant.DisplayName,
                variant.SizeScale,
                variant.SizeValue,
                variant.Price,
                variant.EffectivePrice,
                variant.Stock,
                variant.IsActive,
                purchaseBlockReasons.Count == 0,
                purchaseBlockReasons,
                ResolveVariantStockStatus(variant, product),
                product?.ManageStock == false ? null : variant.Stock,
                variant.Color,
                variant.IsDefault,
                displayMoney?.Price,
                displayMoney?.CurrencyCode);
        }

        public static StorefrontProductVariantAttributeResponse ToStorefrontContract(
            this ProductVariantAttributeDto attribute)
        {
            return new StorefrontProductVariantAttributeResponse(attribute.Name, attribute.Value);
        }

        public static ProductSelectionRequest ToApplicationRequest(
            this StorefrontProductSelectionPreviewRequest request,
            Guid storeId,
            Guid productId)
        {
            return new ProductSelectionRequest(
                storeId,
                productId,
                request.ProductVariantId,
                request.SelectedAttributes,
                SelectedAttributesJson: null,
                request.Quantity,
                request.CurrencyCode,
                ProductSelectionMode.Preview);
        }

        public static StorefrontProductSelectionPreviewResponse ToStorefrontContract(
            this ProductSelectionResult result)
        {
            return new StorefrontProductSelectionPreviewResponse(
                result.ProductId,
                result.ProductVariantId,
                result.IsValid,
                result.IsAvailable,
                result.CanAddToCart,
                result.ValidationMessages,
                result.SelectedAttributes
                    .Select(attribute => new StorefrontProductVariantAttributeResponse(attribute.Name, attribute.Value))
                    .ToArray(),
                result.AttributeSignature,
                result.Sku,
                result.DisplayName,
                result.UnitPrice,
                result.ComparePrice,
                result.CurrencyCode,
                result.StockQuantity,
                result.MinQuantity,
                result.MaxQuantity,
                result.Product?.Image);
        }

        private static IReadOnlyList<string> ResolveCatalogProductPurchaseBlockReasons(GetCatalogProduct product)
        {
            var reasons = new List<string>();
            AddCommonPurchaseReasons(
                reasons,
                product.PurchasingDisabled,
                product.ManageStock,
                product.HasVariants,
                product.InStock,
                product.Quantity);
            return reasons;
        }

        private static bool IsCatalogProductPurchasable(GetCatalogProduct product)
        {
            return ResolveCatalogProductPurchaseBlockReasons(product).Count == 0;
        }

        private static string ResolveCatalogProductStockStatus(GetCatalogProduct product)
        {
            if (!product.ManageStock)
            {
                return ProductStockStatuses.NotManaged;
            }

            if (product.HasVariants)
            {
                return ProductStockStatuses.VariantRequired;
            }

            return product.InStock ? ProductStockStatuses.InStock : ProductStockStatuses.OutOfStock;
        }

        private static int? ResolveCatalogProductAvailableQuantity(GetCatalogProduct product)
        {
            return product.ManageStock && !product.HasVariants ? product.Quantity : null;
        }

        private static IReadOnlyList<string> ResolveProductPurchaseBlockReasons(GetProduct product)
        {
            var reasons = new List<string>();
            AddCommonPurchaseReasons(
                reasons,
                product.PurchasingDisabled,
                product.ManageStock,
                product.Variants.Any(),
                product.Variants.Any(variant => variant.IsActive && variant.Stock > 0),
                product.Quantity);
            return reasons;
        }

        private static bool IsProductInStock(GetProduct product)
        {
            return product.Quantity > 0 || product.Variants.Any(variant => variant.Stock > 0);
        }

        private static string ResolveProductStockStatus(GetProduct product)
        {
            if (!product.ManageStock)
            {
                return ProductStockStatuses.NotManaged;
            }

            if (product.Variants.Any())
            {
                return ProductStockStatuses.VariantRequired;
            }

            return product.Quantity > 0 ? ProductStockStatuses.InStock : ProductStockStatuses.OutOfStock;
        }

        private static int? ResolveProductAvailableQuantity(GetProduct product)
        {
            return product.ManageStock && !product.Variants.Any() ? product.Quantity : null;
        }

        private static IReadOnlyList<string> ResolveVariantPurchaseBlockReasons(GetProductVariant variant, GetProduct? product)
        {
            var reasons = new List<string>();
            if (product?.PurchasingDisabled == true)
            {
                reasons.Add(ProductPurchaseBlockReasons.PurchaseDisabled);
            }

            if (!variant.IsActive)
            {
                reasons.Add(ProductPurchaseBlockReasons.VariantInactive);
            }

            if (product?.ManageStock != false && variant.Stock <= 0)
            {
                reasons.Add(ProductPurchaseBlockReasons.OutOfStock);
            }

            return reasons;
        }

        private static string ResolveVariantStockStatus(GetProductVariant variant, GetProduct? product)
        {
            if (product?.ManageStock == false)
            {
                return ProductStockStatuses.NotManaged;
            }

            return variant.Stock > 0 ? ProductStockStatuses.InStock : ProductStockStatuses.OutOfStock;
        }

        private static void AddCommonPurchaseReasons(
            List<string> reasons,
            bool purchasingDisabled,
            bool manageStock,
            bool requiresVariant,
            bool inStock,
            int quantity)
        {
            if (purchasingDisabled)
            {
                reasons.Add(ProductPurchaseBlockReasons.PurchaseDisabled);
            }

            if (requiresVariant)
            {
                reasons.Add(ProductPurchaseBlockReasons.VariantRequired);
                return;
            }

            if (manageStock && (!inStock || quantity <= 0))
            {
                reasons.Add(ProductPurchaseBlockReasons.OutOfStock);
            }
        }

        public static StorefrontPagedResponse<TTarget> ToStorefrontContract<TSource, TTarget>(
            this PagedResult<TSource> page,
            Func<TSource, TTarget> map)
        {
            return new StorefrontPagedResponse<TTarget>(
                page.Items.Select(map).ToArray(),
                page.PageNumber,
                page.PageSize,
                page.TotalCount,
                page.TotalPages);
        }

        public static StorefrontCheckoutResultResponse ToStorefrontContract(this StorefrontCheckoutResult result)
        {
            return new StorefrontCheckoutResultResponse(
                result.OrderId,
                result.Reference,
                result.OrderStatus,
                result.PaymentStatus,
                result.PaymentMethodKey,
                result.CreatedOn);
        }

        public static StorefrontOrderResponse ToStorefrontContract(this GetOrder order)
        {
            return new StorefrontOrderResponse(
                order.Id,
                order.Reference,
                order.Status,
                order.OrderStatus,
                order.PaymentStatus,
                order.PaymentMethodKey,
                order.PaymentAt,
                order.CurrencyCode,
                order.TotalAmount,
                order.CreatedOn,
                order.ShippingStatus,
                order.ShippingCarrier,
                order.TrackingNumber,
                order.TrackingUrl,
                order.ShippedOn,
                order.DeliveredOn,
                order.CustomerName,
                order.CustomerEmail,
                new StorefrontShippingAddressResponse(
                    order.ShippingFullName,
                    order.ShippingEmail,
                    order.ShippingPhone,
                    order.ShippingAddress1,
                    order.ShippingAddress2,
                    order.ShippingCity,
                    order.ShippingState,
                    order.ShippingPostalCode,
                    order.ShippingCountryCode),
                order.CompletedAt,
                order.CancelledAt,
                order.Lines.Select(line => line.ToStorefrontContract()).ToArray());
        }

        public static StorefrontOrderLineResponse ToStorefrontContract(this GetOrderLine line)
        {
            return new StorefrontOrderLineResponse(
                line.ProductId,
                line.ProductName,
                line.Sku,
                line.Image,
                line.ProductVariantId,
                line.VariantAttributes.Select(attribute => attribute.ToStorefrontContract()).ToArray(),
                line.Quantity,
                line.UnitPrice,
                line.LineTotal);
        }

        public static StorefrontOrderItemHistoryResponse ToStorefrontContract(this GetOrderItem item)
        {
            return new StorefrontOrderItemHistoryResponse(
                item.ProductName,
                item.QuantityOrdered,
                item.CustomerName,
                item.CustomerEmail,
                item.AmountPayed,
                item.DatePurchased,
                item.TrackingNumber,
                item.TrackingUrl,
                item.ShippingStatus);
        }

        public static StorefrontPaymentMethodResponse ToStorefrontContract(this GetPaymentMethod paymentMethod)
        {
            return new StorefrontPaymentMethodResponse(
                paymentMethod.Id,
                paymentMethod.Key,
                paymentMethod.Name,
                paymentMethod.Description,
                paymentMethod.ShortDisplayText,
                paymentMethod.IconUrl,
                paymentMethod.SupportedCurrencyCodes,
                paymentMethod.SupportedCountryCodes);
        }

        public static StorefrontProductRecommendationResponse ToStorefrontContract(
            this GetProductRecommendation recommendation)
        {
            return new StorefrontProductRecommendationResponse(
                recommendation.Id,
                recommendation.Name,
                recommendation.Image,
                recommendation.Price,
                recommendation.CategoryName);
        }

        public static StorefrontMaintenanceResponse ToStorefrontMaintenanceContract(this CommerceCurrentStore store)
        {
            return new StorefrontMaintenanceResponse(
                store.PublicId,
                store.StoreKey,
                store.Name,
                store.MaintenanceModeEnabled,
                store.MaintenanceMessage);
        }

        public static StorefrontCurrentStoreResponse ToStorefrontContract(this CommerceCurrentStore store)
        {
            return new StorefrontCurrentStoreResponse(
                store.PublicId,
                store.StoreKey,
                store.Name,
                store.Status,
                store.BaseUrl,
                store.PrimaryDomain,
                store.ForceHttps,
                store.CdnHost,
                store.LogoUrl,
                store.CompanyName,
                store.CompanyEmail,
                store.CompanyPhone,
                store.CompanyAddress,
                store.FaviconUrl,
                store.PngIconUrl,
                store.AppleTouchIconUrl,
                store.MsTileImageUrl,
                store.MsTileColor,
                store.DefaultCurrencyCode,
                store.DefaultCulture,
                store.SupportEmail,
                store.SupportPhone,
                store.MaintenanceModeEnabled,
                store.MaintenanceMessage,
                store.HtmlBodyId);
        }

        public static StorefrontPublicConfigurationResponse ToPublicConfigurationContract(
            this CommerceCurrentStore store,
            IReadOnlyList<StorefrontPaymentMethodResponse> paymentMethods,
            SeoSettingsDto seoDefaults,
            StoreFeatureStateSnapshot featureStates,
            StorefrontConsentOptions consentOptions,
            CaptchaOptions captchaOptions,
            IReadOnlyList<string>? supportedCurrencyCodes = null)
        {
            var currencyCodes = NormalizeSupportedCurrencyCodes(store.DefaultCurrencyCode, supportedCurrencyCodes);
            return new StorefrontPublicConfigurationResponse(
                new StorefrontStoreIdentityResponse(
                    store.PublicId,
                    store.StoreKey,
                    store.Name,
                    store.Status,
                    store.BaseUrl,
                    store.PrimaryDomain,
                    store.ForceHttps),
                new StorefrontBrandingResponse(
                    store.CdnHost,
                    store.LogoUrl,
                    store.CompanyName,
                    store.CompanyEmail,
                    store.CompanyPhone,
                    store.CompanyAddress,
                    store.FaviconUrl,
                    store.PngIconUrl,
                    store.AppleTouchIconUrl,
                    store.MsTileImageUrl,
                    store.MsTileColor,
                    store.SupportEmail,
                    store.SupportPhone,
                    store.HtmlBodyId),
                new StorefrontLocaleOptionsResponse(
                    store.DefaultCulture,
                    [store.DefaultCulture]),
                new StorefrontCurrencyOptionsResponse(
                    store.DefaultCurrencyCode,
                    currencyCodes),
                ToStorefrontContract(consentOptions),
                ToStorefrontContract(captchaOptions),
                new StorefrontMaintenanceStateResponse(
                    store.MaintenanceModeEnabled,
                    store.MaintenanceMessage),
                new StorefrontFeatureFlagsResponse(
                    CustomerAccountsEnabled: featureStates.CustomerAccountsEnabled,
                    CartEnabled: true,
                    CheckoutEnabled: featureStates.CheckoutEnabled,
                    PaymentsEnabled: true,
                    NewsletterEnabled: featureStates.NewsletterEnabled,
                    RecommendationsEnabled: featureStates.RecommendationsEnabled),
                paymentMethods,
                new StorefrontSeoDefaultsResponse(
                    seoDefaults.SiteName,
                    seoDefaults.DefaultTitleSuffix,
                    seoDefaults.DefaultMetaDescription,
                    seoDefaults.DefaultOgImage,
                    seoDefaults.BaseCanonicalUrl,
                    seoDefaults.CompanyName,
                    seoDefaults.CompanyLogoUrl,
                    seoDefaults.CompanyPhone,
                    seoDefaults.CompanyEmail,
                    seoDefaults.CompanyAddress,
                    seoDefaults.FacebookUrl,
                    seoDefaults.InstagramUrl,
                    seoDefaults.XUrl));
        }

        public static StorefrontConsentConfigurationResponse ToStorefrontContract(StorefrontConsentOptions options)
        {
            var optionalDefault = options.OptionalCategoriesDefaultEnabled;
            return new StorefrontConsentConfigurationResponse(
                options.Enabled,
                options.BannerRequired,
                string.IsNullOrWhiteSpace(options.CurrentVersion) ? "default" : options.CurrentVersion,
                string.IsNullOrWhiteSpace(options.PolicyPagePath) ? "/pages/cookies" : options.PolicyPagePath,
                [
                    new StorefrontConsentCategoryResponse(StorefrontConsentCategoryNames.Essential, Required: true, DefaultEnabled: true),
                    new StorefrontConsentCategoryResponse(StorefrontConsentCategoryNames.Preferences, Required: false, DefaultEnabled: optionalDefault),
                    new StorefrontConsentCategoryResponse(StorefrontConsentCategoryNames.Analytics, Required: false, DefaultEnabled: optionalDefault),
                    new StorefrontConsentCategoryResponse(StorefrontConsentCategoryNames.Marketing, Required: false, DefaultEnabled: optionalDefault),
                ],
                Math.Clamp(options.VisitorCookieLifetimeDays, 1, 3650));
        }

        public static StorefrontConsentResponse ToStorefrontContract(this StorefrontConsentSnapshot snapshot)
        {
            return new StorefrontConsentResponse(
                snapshot.Enabled,
                snapshot.BannerRequired,
                snapshot.ConsentVersion,
                snapshot.ConsentKey,
                new StorefrontConsentCategorySelectionResponse(
                    snapshot.Categories.Essential,
                    snapshot.Categories.Preferences,
                    snapshot.Categories.Analytics,
                    snapshot.Categories.Marketing),
                snapshot.UpdatedAtUtc,
                snapshot.RevokedAtUtc,
                snapshot.ExpiresAtUtc);
        }

        public static StorefrontCaptchaConfigurationResponse ToStorefrontContract(CaptchaOptions options)
        {
            var enabledTargets = new List<string>();
            if (options.Enabled && options.Targets.Login)
            {
                enabledTargets.Add(CaptchaTargetNames.Login);
            }

            if (options.Enabled && options.Targets.Registration)
            {
                enabledTargets.Add(CaptchaTargetNames.Registration);
            }

            if (options.Enabled && options.Targets.Newsletter)
            {
                enabledTargets.Add(CaptchaTargetNames.Newsletter);
            }

            return new StorefrontCaptchaConfigurationResponse(
                options.Enabled,
                string.IsNullOrWhiteSpace(options.ProviderSystemName) ? "none" : options.ProviderSystemName,
                options.Enabled ? options.PublicSiteKey : null,
                enabledTargets,
                enabledTargets.ToDictionary(target => target, target => target, StringComparer.Ordinal));
        }

        private static IReadOnlyList<string> NormalizeSupportedCurrencyCodes(
            string defaultCurrencyCode,
            IReadOnlyList<string>? supportedCurrencyCodes)
        {
            var baseCurrencyCode = NormalizeCurrencyCode(defaultCurrencyCode) ?? "USD";
            return (supportedCurrencyCodes ?? [])
                .Prepend(baseCurrencyCode)
                .Select(code => NormalizeCurrencyCode(code) ?? baseCurrencyCode)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static string? NormalizeCurrencyCode(string? currencyCode)
        {
            var normalized = currencyCode?.Trim().ToUpperInvariant();
            return normalized is { Length: 3 } ? normalized : null;
        }

        private static ProductCatalogSortBy ToApplicationSortBy(string? sortBy)
        {
            return sortBy switch
            {
                StorefrontProductCatalogSortValues.Oldest => ProductCatalogSortBy.Oldest,
                StorefrontProductCatalogSortValues.PriceLowToHigh => ProductCatalogSortBy.PriceLowToHigh,
                StorefrontProductCatalogSortValues.PriceHighToLow => ProductCatalogSortBy.PriceHighToLow,
                StorefrontProductCatalogSortValues.NameAscending => ProductCatalogSortBy.NameAscending,
                StorefrontProductCatalogSortValues.NameDescending => ProductCatalogSortBy.NameDescending,
                StorefrontProductCatalogSortValues.DisplayOrder => ProductCatalogSortBy.DisplayOrder,
                StorefrontProductCatalogSortValues.Updated => ProductCatalogSortBy.Updated,
                _ => ProductCatalogSortBy.Newest,
            };
        }

        private static DateTime ResolveAccessTokenExpiration(string token)
        {
            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
                return jwt.ValidTo == DateTime.MinValue
                    ? DateTime.UtcNow.AddHours(2)
                    : jwt.ValidTo;
            }
            catch (ArgumentException)
            {
                return DateTime.UtcNow.AddHours(2);
            }
        }
    }

    public sealed record StorefrontDisplayMoney(
        decimal Price,
        decimal? ComparePrice,
        string CurrencyCode);
}
