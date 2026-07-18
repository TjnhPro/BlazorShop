namespace BlazorShop.ControlPlane.Web.Services.Commerce
{
    using System.Globalization;
    using System.Net.Http.Headers;

    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.ControlPlane.Web.Services.Common;
    using BlazorShop.Domain.Contracts;

        public abstract class ControlPlaneCommerceClientBase
    {
        protected ControlPlaneCommerceClientBase(IControlPlaneApiClient apiClient)
        {
            this.ApiClient = apiClient;
        }
        protected IControlPlaneApiClient ApiClient { get; }
        protected static string BuildProductQuery(ProductCatalogQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", query.GetNormalizedPageNumber().ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("pageSize", query.GetNormalizedPageSize().ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("sortBy", query.SortBy.ToString()),
            };

            AddIfPresent(values, "searchTerm", query.SearchTerm);
            AddIfPresent(values, "categoryId", query.CategoryId?.ToString("D"));
            AddIfPresent(values, "minPrice", query.MinPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture));
            AddIfPresent(values, "maxPrice", query.MaxPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture));
            AddIfPresent(values, "inStock", query.InStock?.ToString());
            AddIfPresent(values, "isPublished", query.IsPublished?.ToString());
            return ToQueryString(values);
        }

        protected static string BuildInventoryQuery(AdminInventoryQueryDto query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("lowStockOnly", query.LowStockOnly.ToString()),
                new("outOfStockOnly", query.OutOfStockOnly.ToString()),
                new("lowStockThreshold", Math.Max(0, query.LowStockThreshold).ToString(System.Globalization.CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "searchTerm", query.SearchTerm);
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

        protected static string BuildMediaAssetPreviewQuery(string canonicalFileName, MediaAssetPreviewQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("fileName", canonicalFileName),
            };

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
            return ToQueryString(values);
        }

        protected static MultipartFormDataContent BuildMediaAssetForm(Stream content, string fileName, string? contentType)
        {
            var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(content);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
            form.Add(fileContent, "file", string.IsNullOrWhiteSpace(fileName) ? "media-asset" : fileName);
            return form;
        }

        protected static string CommerceRoute(Guid storePublicId, string path)
        {
            return $"api/controlplane/commerce/stores/{storePublicId:D}/{path.TrimStart('/')}";
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
            return values.Count == 0
                ? string.Empty
                : "?" + string.Join(
                    "&",
                    values.Select(value => Uri.EscapeDataString(value.Key) + "=" + Uri.EscapeDataString(value.Value)));
        }
    }
}

