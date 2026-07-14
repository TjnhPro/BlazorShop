namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
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

        public static CreateOrderItem ToCreateOrderItem(this StorefrontOrderItemRequest request, string userId)
        {
            return new CreateOrderItem
            {
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                SelectedAttributes = request.SelectedAttributes,
                Quantity = request.Quantity,
                UserId = userId,
            };
        }

        public static StorefrontAuthResponse ToStorefrontContract(this LoginResponse response)
        {
            return new StorefrontAuthResponse(response.Success, response.Message, response.Token);
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
    }
}
