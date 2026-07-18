namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Globalization;

    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.ControlPlane.CommerceGateway;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Domain.Contracts;
    public abstract class ControlPlaneCommerceGatewayBase
    {
        private readonly ICommerceNodeAdminGatewayTransport transport;

        protected ControlPlaneCommerceGatewayBase(ICommerceNodeAdminGatewayTransport transport)
        {
            this.transport = transport;
        }

        protected async Task<ControlPlaneCommerceCatalogResult<TPayload>> SendAsync<TPayload>(
            Guid storePublicId,
            HttpMethod method,
            string path,
            object? body,
            CancellationToken cancellationToken)
        {
            return ToCatalogResult(await this.transport.SendAsync<TPayload>(
                storePublicId,
                method,
                path,
                body,
                cancellationToken));
        }

        protected async Task<ControlPlaneCommerceMediaResult> SendMediaAsync(
            Guid storePublicId,
            string path,
            CancellationToken cancellationToken)
        {
            var result = await this.transport.SendMediaAsync(storePublicId, path, cancellationToken);
            return new ControlPlaneCommerceMediaResult(
                result.Success,
                result.Message,
                result.Content,
                result.ContentType,
                ToCatalogFailure(result.Failure),
                result.HttpStatusCode);
        }

        protected async Task<ControlPlaneCommerceCatalogResult<TPayload>> SendMultipartAsync<TPayload>(
            Guid storePublicId,
            string path,
            ProductImportUploadRequest upload,
            CancellationToken cancellationToken)
        {
            return ToCatalogResult(await this.transport.SendProductImportMultipartAsync<TPayload>(
                storePublicId,
                path,
                upload,
                cancellationToken));
        }

        protected async Task<ControlPlaneCommerceCatalogResult<TPayload>> SendMediaAssetMultipartAsync<TPayload>(
            Guid storePublicId,
            string path,
            CommerceMediaAssetUploadRequest upload,
            CancellationToken cancellationToken)
        {
            return ToCatalogResult(await this.transport.SendMediaAssetMultipartAsync<TPayload>(
                storePublicId,
                path,
                upload,
                cancellationToken));
        }

        protected async Task<ControlPlaneCommerceCatalogResult<string>> ResolveStoreKeyAsync(
            Guid storePublicId,
            CancellationToken cancellationToken)
        {
            return ToCatalogResult(await this.transport.ResolveStoreKeyAsync(storePublicId, cancellationToken));
        }

        protected static ControlPlaneCommerceCatalogResult<TPayload> ToCatalogResult<TPayload>(
            CommerceNodeAdminGatewayResult<TPayload> result)
        {
            return new ControlPlaneCommerceCatalogResult<TPayload>(
                result.Success,
                result.Message,
                result.Payload,
                ToCatalogFailure(result.Failure),
                result.HttpStatusCode);
        }

        protected static ControlPlaneCommerceCatalogFailure? ToCatalogFailure(CommerceNodeAdminGatewayFailure? failure)
        {
            return failure switch
            {
                CommerceNodeAdminGatewayFailure.Validation => ControlPlaneCommerceCatalogFailure.Validation,
                CommerceNodeAdminGatewayFailure.NotFound => ControlPlaneCommerceCatalogFailure.NotFound,
                CommerceNodeAdminGatewayFailure.RemoteFailure => ControlPlaneCommerceCatalogFailure.RemoteFailure,
                _ => null,
            };
        }

        protected static string BuildProductQuery(ProductCatalogQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", query.GetNormalizedPageNumber().ToString(CultureInfo.InvariantCulture)),
                new("pageSize", query.GetNormalizedPageSize().ToString(CultureInfo.InvariantCulture)),
                new("sortBy", query.SortBy.ToString()),
            };

            AddIfPresent(values, "searchTerm", query.SearchTerm);
            AddIfPresent(values, "categoryId", query.CategoryId?.ToString("D"));
            AddIfPresent(values, "minPrice", query.MinPrice?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "maxPrice", query.MaxPrice?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "inStock", query.InStock?.ToString());
            AddIfPresent(values, "isPublished", query.IsPublished?.ToString());

            return ToQueryString(values);
        }

        protected static string BuildSeoSlugHistoryQuery(StoreSeoSlugHistoryQuery query)
        {
            var values = new List<KeyValuePair<string, string>>();
            AddIfPresent(values, "entityType", query.EntityType);
            if (query.EntityId != Guid.Empty)
            {
                values.Add(new KeyValuePair<string, string>("entityId", query.EntityId.ToString("D")));
            }

            AddIfPresent(values, "languageCode", query.LanguageCode);
            return ToQueryString(values);
        }

        protected static string BuildInventoryQuery(AdminInventoryQueryDto query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
                new("lowStockOnly", query.LowStockOnly.ToString()),
                new("outOfStockOnly", query.OutOfStockOnly.ToString()),
                new("lowStockThreshold", Math.Max(0, query.LowStockThreshold).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "searchTerm", query.SearchTerm);
            return ToQueryString(values);
        }

        protected static string BuildOrderQuery(AdminOrderQueryDto query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "searchTerm", query.SearchTerm);
            AddIfPresent(values, "status", query.Status);
            AddIfPresent(values, "shippingStatus", query.ShippingStatus);
            AddIfPresent(values, "fromUtc", query.FromUtc?.ToString("O", CultureInfo.InvariantCulture));
            AddIfPresent(values, "toUtc", query.ToUtc?.ToString("O", CultureInfo.InvariantCulture));
            return ToQueryString(values);
        }

        protected static string BuildQueuedMessageQuery(string? status, string? templateSystemName, int skip, int take)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("skip", Math.Max(0, skip).ToString(CultureInfo.InvariantCulture)),
                new("take", Math.Clamp(take, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "status", status);
            AddIfPresent(values, "templateSystemName", templateSystemName);
            return ToQueryString(values);
        }

        protected static string BuildMediaPreviewQuery(ProductMediaPreviewQuery query)
        {
            var values = new List<KeyValuePair<string, string>>();
            AddIfPresent(values, "w", query.Width?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "h", query.Height?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "fit", query.Fit);
            AddIfPresent(values, "format", query.Format);
            AddIfPresent(values, "v", query.Version?.ToString(CultureInfo.InvariantCulture));
            return ToQueryString(values);
        }

        protected static string BuildMediaAssetPreviewQuery(MediaAssetPreviewQuery query)
        {
            var values = new List<KeyValuePair<string, string>>();
            AddIfPresent(values, "w", query.Width?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "h", query.Height?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "fit", query.Fit);
            AddIfPresent(values, "format", query.Format);
            AddIfPresent(values, "v", query.Version?.ToString(CultureInfo.InvariantCulture));
            return ToQueryString(values);
        }

        protected static string BuildMediaAssetListQuery(CommerceMediaAssetListQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "search", query.Search);
            AddIfPresent(values, "usageType", query.UsageType);
            return ToQueryString(values);
        }

        protected static string BuildProductImportQuery(ProductImportJobListQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "status", query.Status);
            return ToQueryString(values);
        }

        protected static string BuildPageQuery(int pageNumber, int pageSize)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, pageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(pageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            return ToQueryString(values);
        }

        protected static string BuildProductImportRowsQuery(ProductImportRowsQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "status", query.Status);
            return ToQueryString(values);
        }

        protected static string BuildStorefrontPageQuery(StorefrontPageListQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "search", query.Search);
            AddIfPresent(values, "status", query.Status);
            return ToQueryString(values);
        }

        protected static void AddIfPresent(List<KeyValuePair<string, string>> values, string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(new KeyValuePair<string, string>(key, value.Trim()));
            }
        }

        protected static string ToQueryString(IReadOnlyCollection<KeyValuePair<string, string>> values)
        {
            if (values.Count == 0)
            {
                return string.Empty;
            }

            return "?" + string.Join(
                "&",
                values.Select(value =>
                    Uri.EscapeDataString(value.Key) + "=" + Uri.EscapeDataString(value.Value)));
        }

        protected static ControlPlaneCommerceCatalogResult<TPayload> Failure<TPayload>(
            string message,
            ControlPlaneCommerceCatalogFailure failure,
            int? httpStatusCode = null)
        {
            return new ControlPlaneCommerceCatalogResult<TPayload>(
                false,
                message,
                Failure: failure,
                HttpStatusCode: httpStatusCode);
        }

    }
}

