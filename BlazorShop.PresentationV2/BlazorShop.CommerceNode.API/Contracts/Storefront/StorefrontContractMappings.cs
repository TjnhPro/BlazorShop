namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.IdentityModel.Tokens.Jwt;

    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
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
            };
        }

        public static LoginUser ToApplicationRequest(this StorefrontLoginRequest request)
        {
            return new LoginUser
            {
                Email = request.Email,
                Password = request.Password,
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
                SearchTerm = query.SearchTerm,
                MinPrice = query.MinPrice,
                MaxPrice = query.MaxPrice,
                InStock = query.InStock,
                SortBy = ToApplicationSortBy(query.SortBy),
                CreatedAfterUtc = query.CreatedAfterUtc,
            };
        }

        public static StorefrontCheckoutRequest ToStorefrontContract(
            this Application.DTOs.Payment.StorefrontCheckoutRequest request)
        {
            return new StorefrontCheckoutRequest
            {
                CustomerEmail = request.CustomerEmail,
                CustomerName = request.CustomerName,
                PaymentMethodKey = request.PaymentMethodKey,
                Carts = request.Carts.Select(item => item.ToStorefrontContract()).ToArray(),
                ShippingAddress = request.ShippingAddress.ToStorefrontContract(),
            };
        }

        public static Application.DTOs.Payment.StorefrontCheckoutRequest ToApplicationRequest(
            this StorefrontCheckoutRequest request)
        {
            return new Application.DTOs.Payment.StorefrontCheckoutRequest
            {
                CustomerEmail = request.CustomerEmail,
                CustomerName = request.CustomerName,
                PaymentMethodKey = request.PaymentMethodKey,
                Carts = request.Carts.Select(item => item.ToProcessCart()).ToArray(),
                ShippingAddress = request.ShippingAddress.ToApplicationRequest(),
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
            return new StorefrontPlaceOrderResponse(
                result.CheckoutSessionId,
                result.OrderId,
                result.Reference,
                result.OrderStatus,
                result.PaymentStatus,
                result.PaymentMethodKey,
                result.TotalAmount,
                result.CurrencyCode,
                result.IdempotencyKey,
                result.CreatedOn);
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
                cart.Lines.Select(line => line.ToStorefrontContract()).ToArray());
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
                line.CurrencyCodeSnapshot);
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
                page.Products.Select(product => product.ToStorefrontContract()).ToArray());
        }

        public static StorefrontCatalogProductResponse ToStorefrontContract(this GetCatalogProduct product)
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
                product.CreatedOn,
                product.UpdatedAt,
                product.DisplayOrder,
                product.InStock,
                product.PublishedOn,
                product.CategoryId,
                product.CategoryName,
                product.CategorySlug,
                product.HasVariants,
                product.ProductType,
                product.VariationTemplateId);
        }

        public static StorefrontProductResponse ToStorefrontContract(this GetProduct product)
        {
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
                product.Image,
                product.Quantity,
                product.DisplayOrder,
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
                product.Variants.Select(variant => variant.ToStorefrontContract()).ToArray());
        }

        public static StorefrontProductVariantResponse ToStorefrontContract(this GetProductVariant variant)
        {
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
                variant.Color,
                variant.IsDefault);
        }

        public static StorefrontProductVariantAttributeResponse ToStorefrontContract(
            this ProductVariantAttributeDto attribute)
        {
            return new StorefrontProductVariantAttributeResponse(attribute.Name, attribute.Value);
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
                paymentMethod.Description);
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
}
