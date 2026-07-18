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

        public sealed class ControlPlaneProductImportClient : ControlPlaneCommerceClientBase, IControlPlaneProductImportClient
    {
        public ControlPlaneProductImportClient(IControlPlaneApiClient apiClient)
            : base(apiClient)
        {
        }
        public Task<ControlPlaneFileResult> DownloadProductImportTemplateAsync(
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateFileAsync(
                "api/controlplane/commerce/product-imports/template",
                "Unable to download product import template.",
                cancellationToken);
        }

        public async Task<ControlPlaneClientResult<ProductImportUploadResponse>> UploadProductImportAsync(
            Guid storePublicId,
            Stream content,
            string fileName,
            string? mode,
            CancellationToken cancellationToken = default)
        {
            using var form = new MultipartFormDataContent();
            using var fileContent = new StreamContent(content);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            form.Add(fileContent, "file", string.IsNullOrWhiteSpace(fileName) ? "products.csv" : fileName);
            form.Add(new StringContent(string.IsNullOrWhiteSpace(mode) ? ProductImportModes.CreateOnly : mode), "mode");

            return await this.ApiClient.PostPrivateMultipartAsync<ProductImportUploadResponse>(
                CommerceRoute(storePublicId, "product-imports"),
                form,
                "Unable to upload product import.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductImportJobListResponse>> ListProductImportsAsync(
            Guid storePublicId,
            ProductImportJobListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<ProductImportJobListResponse>(
                CommerceRoute(storePublicId, "product-imports") + BuildProductImportQuery(query),
                "Unable to load product import jobs.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductImportJobDetailDto>> GetProductImportAsync(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<ProductImportJobDetailDto>(
                CommerceRoute(storePublicId, $"product-imports/{jobPublicId:D}"),
                "Unable to load product import job.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductImportRowsResponse>> ListProductImportRowsAsync(
            Guid storePublicId,
            Guid jobPublicId,
            ProductImportRowsQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<ProductImportRowsResponse>(
                CommerceRoute(storePublicId, $"product-imports/{jobPublicId:D}/rows") + BuildProductImportRowsQuery(query),
                "Unable to load product import rows.",
                cancellationToken);
        }

        public Task<ControlPlaneFileResult> DownloadProductImportErrorsAsync(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateFileAsync(
                CommerceRoute(storePublicId, $"product-imports/{jobPublicId:D}/errors.csv"),
                "Unable to download product import errors.",
                cancellationToken);
        }
    }
}

