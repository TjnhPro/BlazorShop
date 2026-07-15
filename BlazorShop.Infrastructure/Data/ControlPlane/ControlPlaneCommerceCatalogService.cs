namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Globalization;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
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
    using BlazorShop.Domain.Entities.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    public sealed class ControlPlaneCommerceCatalogService : IControlPlaneCommerceCatalogService
    {
        private const string ControlApiEndpointKind = "control_api";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly ControlPlaneDbContext dbContext;
        private readonly HttpClient httpClient;

        public ControlPlaneCommerceCatalogService(ControlPlaneDbContext dbContext, HttpClient httpClient)
        {
            this.dbContext = dbContext;
            this.httpClient = httpClient;
        }

        public Task<ControlPlaneCommerceCatalogResult<PagedResult<GetCatalogProduct>>> QueryProductsAsync(
            Guid storePublicId,
            ProductCatalogQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<PagedResult<GetCatalogProduct>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/products/query" + BuildProductQuery(query),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<GetProduct>> GetProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<GetProduct>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/{productId:D}",
                null,
                cancellationToken);
        }

        public async Task<ControlPlaneCommerceCatalogResult<CommerceStoreDetail>> GetRuntimeStoreAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            var store = await this.LoadStoreAsync(storePublicId, cancellationToken);
            var validation = ValidateStoreForRemoteCall(store);
            if (validation is not null)
            {
                return validation.ToResult<CommerceStoreDetail>();
            }

            var result = await this.SendAsync<CommerceStoreListResponse>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/stores",
                null,
                cancellationToken);

            if (!result.Success)
            {
                return new ControlPlaneCommerceCatalogResult<CommerceStoreDetail>(
                    false,
                    result.Message,
                    Failure: result.Failure,
                    HttpStatusCode: result.HttpStatusCode);
            }

            var runtimeStore = result.Payload?.Items.FirstOrDefault(item =>
                string.Equals(item.StoreKey, store!.StoreKey, StringComparison.OrdinalIgnoreCase));
            if (runtimeStore is null)
            {
                return Failure<CommerceStoreDetail>("Runtime store was not found.", ControlPlaneCommerceCatalogFailure.NotFound);
            }

            return await this.SendAsync<CommerceStoreDetail>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/stores/{runtimeStore.PublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceStoreDetail>> UpdateRuntimeStoreAsync(
            Guid storePublicId,
            Guid runtimeStorePublicId,
            UpdateCommerceStoreRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceStoreDetail>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/stores/{runtimeStorePublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceStoreDetail>> ActivateRuntimeStoreAsync(
            Guid storePublicId,
            Guid runtimeStorePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceStoreDetail>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/stores/{runtimeStorePublicId:D}/activate",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceStoreDetail>> DeactivateRuntimeStoreAsync(
            Guid storePublicId,
            Guid runtimeStorePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceStoreDetail>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/stores/{runtimeStorePublicId:D}/deactivate",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> CreateProductAsync(
            Guid storePublicId,
            CreateProduct request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/products",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> UpdateProductAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProduct request,
            CancellationToken cancellationToken = default)
        {
            request.Id = productId;
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/products/{productId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> ArchiveProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/products/{productId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CategorySeoDto>> GetCategorySeoAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CategorySeoDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/categories/{categoryId:D}/seo",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CategorySeoDto>> UpdateCategorySeoAsync(
            Guid storePublicId,
            Guid categoryId,
            UpdateCategorySeoDto request,
            CancellationToken cancellationToken = default)
        {
            request.CategoryId = categoryId;
            return this.SendAsync<CategorySeoDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/categories/{categoryId:D}/seo",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductSeoDto>> GetProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductSeoDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/{productId:D}/seo",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductSeoDto>> UpdateProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductSeoDto request,
            CancellationToken cancellationToken = default)
        {
            request.ProductId = productId;
            return this.SendAsync<ProductSeoDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/products/{productId:D}/seo",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreSeoSlugPolicyResult>> GenerateSeoSlugAsync(
            Guid storePublicId,
            StoreSeoSlugGenerateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreSeoSlugPolicyResult>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/seo/slugs/generate",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreSeoSlugPolicyResult>> ValidateSeoSlugAsync(
            Guid storePublicId,
            StoreSeoSlugValidateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreSeoSlugPolicyResult>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/seo/slugs/validate",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreSeoSlugHistoryDto>>> ListSeoSlugHistoryAsync(
            Guid storePublicId,
            StoreSeoSlugHistoryQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<StoreSeoSlugHistoryDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/seo/slugs/history" + BuildSeoSlugHistoryQuery(query),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductImportUploadResponse>> UploadProductImportAsync(
            Guid storePublicId,
            ProductImportUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendMultipartAsync<ProductImportUploadResponse>(
                storePublicId,
                "api/commerce/admin/products/import",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductImportJobListResponse>> ListProductImportsAsync(
            Guid storePublicId,
            ProductImportJobListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductImportJobListResponse>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/products/imports" + BuildProductImportQuery(query),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductImportJobDetailDto>> GetProductImportAsync(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductImportJobDetailDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/imports/{jobPublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductImportRowsResponse>> ListProductImportRowsAsync(
            Guid storePublicId,
            Guid jobPublicId,
            ProductImportRowsQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductImportRowsResponse>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/imports/{jobPublicId:D}/rows" + BuildProductImportRowsQuery(query),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<VariationTemplateListResponse>> ListVariationTemplatesAsync(
            Guid storePublicId,
            VariationTemplateListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<VariationTemplateListResponse>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/variation-templates" + BuildPageQuery(query.PageNumber, query.PageSize),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> GetVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> CreateVariationTemplateAsync(
            Guid storePublicId,
            CreateVariationTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/variation-templates",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> UpdateVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            UpdateVariationTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> CreateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CreateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}/options",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> UpdateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            UpdateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}/options/{optionPublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> CreateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            CreateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}/options/{optionPublicId:D}/values",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> UpdateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            Guid valuePublicId,
            UpdateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}/options/{optionPublicId:D}/values/{valuePublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageListResponse>> ListStorefrontPagesAsync(
            Guid storePublicId,
            StorefrontPageListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageListResponse>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/pages" + BuildStorefrontPageQuery(query),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StorefrontPageTemplateDefinitionDto>>> ListStorefrontPageTemplatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<StorefrontPageTemplateDefinitionDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/pages/templates",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StorefrontPageTemplateStatusDto>>> GetStorefrontPageTemplateStatusAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<StorefrontPageTemplateStatusDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/pages/template-status",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> GetStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageDetailDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/pages/{pagePublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> CreateStorefrontPageAsync(
            Guid storePublicId,
            CreateStorefrontPageRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageDetailDto>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/pages",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> UpdateStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            UpdateStorefrontPageRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/pages/{pagePublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> ArchiveStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageDetailDto>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/pages/{pagePublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> CreateStorefrontPageDraftFromTemplateAsync(
            Guid storePublicId,
            string pageKey,
            CreatePageFromTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageDetailDto>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/pages/templates/{Uri.EscapeDataString(pageKey)}/draft",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> MapStorefrontPageTemplateAsync(
            Guid storePublicId,
            Guid pagePublicId,
            MapPageTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/pages/{pagePublicId:D}/template",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> ClearStorefrontPageTemplateAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageDetailDto>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/pages/{pagePublicId:D}/template",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> UpdateStorefrontPageNavigationAsync(
            Guid storePublicId,
            Guid pagePublicId,
            UpdatePageNavigationRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/pages/{pagePublicId:D}/navigation",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreNavigationMenuSummaryDto>>> ListNavigationMenusAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<StoreNavigationMenuSummaryDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/navigation/menus",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreNavigationMenuDetailDto>> GetNavigationMenuAsync(
            Guid storePublicId,
            Guid menuPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreNavigationMenuDetailDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/navigation/menus/{menuPublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreNavigationMenuDetailDto>> CreateNavigationMenuAsync(
            Guid storePublicId,
            CreateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreNavigationMenuDetailDto>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/navigation/menus",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreNavigationMenuDetailDto>> UpdateNavigationMenuAsync(
            Guid storePublicId,
            Guid menuPublicId,
            UpdateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreNavigationMenuDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/navigation/menus/{menuPublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreNavigationMenuDetailDto>> CreateNavigationItemAsync(
            Guid storePublicId,
            Guid menuPublicId,
            CreateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreNavigationMenuDetailDto>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/navigation/menus/{menuPublicId:D}/items",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreNavigationMenuDetailDto>> UpdateNavigationItemAsync(
            Guid storePublicId,
            Guid itemPublicId,
            UpdateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreNavigationMenuDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/navigation/items/{itemPublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreNavigationMenuDetailDto>> ArchiveNavigationItemAsync(
            Guid storePublicId,
            Guid itemPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreNavigationMenuDetailDto>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/navigation/items/{itemPublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreNavigationMenuDetailDto>> UpdateNavigationItemOrderAsync(
            Guid storePublicId,
            Guid menuPublicId,
            UpdateStoreNavigationMenuItemOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreNavigationMenuDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/navigation/menus/{menuPublicId:D}/items/order",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreNavigationTargetOptionDto>>> ListNavigationSystemTargetsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<StoreNavigationTargetOptionDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/navigation/system-targets",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductMediaListResponse>> ListProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ProductMediaListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductMediaListResponse>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/{productId:D}/media" + BuildPageQuery(query.PageNumber, query.PageSize),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ImportProductMediaResponse>> ImportProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ImportProductMediaRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ImportProductMediaResponse>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/products/{productId:D}/media/import",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductMediaListResponse>> UpdateProductMediaOrderAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductMediaOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductMediaListResponse>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/products/{productId:D}/media/order",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductMediaDto>> SetPrimaryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductMediaDto>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/products/{productId:D}/media/{mediaPublicId:D}/primary",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductMediaListResponse>> DeleteProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductMediaListResponse>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/products/{productId:D}/media/{mediaPublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ImportProductMediaResponse>> RetryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ImportProductMediaResponse>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/products/{productId:D}/media/{mediaPublicId:D}/retry",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetListResponse>> ListMediaAssetsAsync(
            Guid storePublicId,
            CommerceMediaAssetListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceMediaAssetListResponse>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/media/assets" + BuildMediaAssetListQuery(query),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetDto>> GetMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceMediaAssetDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/media/assets/{assetPublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetDto>> UploadMediaAssetAsync(
            Guid storePublicId,
            CommerceMediaAssetUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendMediaAssetMultipartAsync<CommerceMediaAssetDto>(
                storePublicId,
                "api/commerce/admin/media/assets",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetDto>> UpdateMediaAssetMetadataAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CommerceMediaAssetMetadataRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceMediaAssetDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/media/assets/{assetPublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceMediaAssetDto>> ReplaceMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CommerceMediaAssetUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendMediaAssetMultipartAsync<CommerceMediaAssetDto>(
                storePublicId,
                $"api/commerce/admin/media/assets/{assetPublicId:D}/replace",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> DeleteMediaAssetAsync(
            Guid storePublicId,
            Guid assetPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/media/assets/{assetPublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<PagedResult<GetCategory>>> ListCategoriesAsync(
            Guid storePublicId,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<PagedResult<GetCategory>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/categories" + BuildPageQuery(pageNumber, pageSize),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<GetCategoryTreeNode>>> GetCategoryTreeAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<GetCategoryTreeNode>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/categories/tree",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> CreateCategoryAsync(
            Guid storePublicId,
            CreateCategory request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/categories",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> UpdateCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            UpdateCategory request,
            CancellationToken cancellationToken = default)
        {
            request.Id = categoryId;
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/categories/{categoryId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> ArchiveCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/categories/{categoryId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CategoryMediaAssignmentDto>> GetCategoryMediaAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CategoryMediaAssignmentDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/categories/{categoryId:D}/media",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CategoryMediaAssignmentDto>> SetCategoryPrimaryMediaAsync(
            Guid storePublicId,
            Guid categoryId,
            SetCategoryPrimaryMediaRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CategoryMediaAssignmentDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/categories/{categoryId:D}/media/primary",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CategoryMediaAssignmentDto>> ClearCategoryPrimaryMediaAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CategoryMediaAssignmentDto>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/categories/{categoryId:D}/media/primary",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<PagedResult<GetProductVariant>>> ListVariantsAsync(
            Guid storePublicId,
            Guid productId,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<PagedResult<GetProductVariant>>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/{productId:D}/variants" + BuildPageQuery(pageNumber, pageSize),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> CreateVariantAsync(
            Guid storePublicId,
            Guid productId,
            CreateProductVariant request,
            CancellationToken cancellationToken = default)
        {
            request.ProductId = productId;
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/products/{productId:D}/variants",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> UpdateVariantAsync(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            UpdateProductVariant request,
            CancellationToken cancellationToken = default)
        {
            request.ProductId = productId;
            request.Id = variantId;
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/products/{productId:D}/variants/{variantId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> DeleteVariantAsync(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/products/{productId:D}/variants/{variantId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<PagedResult<AdminInventoryItemDto>>> QueryInventoryAsync(
            Guid storePublicId,
            AdminInventoryQueryDto query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<PagedResult<AdminInventoryItemDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/inventory" + BuildInventoryQuery(query),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<AdminInventoryItemDto>> UpdateProductStockAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductStockDto request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<AdminInventoryItemDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/inventory/products/{productId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<AdminInventoryVariantDto>> UpdateVariantStockAsync(
            Guid storePublicId,
            Guid variantId,
            UpdateVariantStockDto request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<AdminInventoryVariantDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/inventory/variants/{variantId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<PagedResult<GetOrder>>> QueryOrdersAsync(
            Guid storePublicId,
            AdminOrderQueryDto query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<PagedResult<GetOrder>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/orders" + BuildOrderQuery(query),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<GetOrder>> GetOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<GetOrder>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/orders/{orderId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<GetOrder>> UpdateOrderAdminNoteAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateOrderAdminNoteRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<GetOrder>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/orders/{orderId:D}/admin-note",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<GetOrder>> UpdateOrderShippingStatusAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateShippingStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<GetOrder>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/orders/{orderId:D}/shipping-status",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<GetOrder>> CompleteOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<GetOrder>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/orders/{orderId:D}/complete",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<GetOrder>> CancelOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<GetOrder>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/orders/{orderId:D}/cancel",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StorePaymentMethodDto>>> ListPaymentMethodsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<StorePaymentMethodDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/payment-methods",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorePaymentMethodDto>> UpdatePaymentMethodAsync(
            Guid storePublicId,
            string paymentMethodKey,
            UpdateStorePaymentMethodRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorePaymentMethodDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/payment-methods/{Uri.EscapeDataString(paymentMethodKey)}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreCurrencyDto>>> ListCurrenciesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<StoreCurrencyDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/currencies",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreCurrencyDto>> UpdateCurrencyAsync(
            Guid storePublicId,
            string currencyCode,
            UpdateStoreCurrencyRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreCurrencyDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/currencies/{Uri.EscapeDataString(currencyCode)}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreCurrencyExchangeRateDto>>> ListExchangeRatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<StoreCurrencyExchangeRateDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/currencies/exchange-rates",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreCurrencyExchangeRateProviderDto>>> ListExchangeRateProvidersAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<StoreCurrencyExchangeRateProviderDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/currencies/exchange-rate-providers",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreCurrencyExchangeRateProviderFetchResult>> FetchExchangeRatesAsync(
            Guid storePublicId,
            FetchStoreCurrencyExchangeRatesRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreCurrencyExchangeRateProviderFetchResult>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/currencies/exchange-rates/fetch",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceTaskSummary>> QueueExchangeRateUpdateAsync(
            Guid storePublicId,
            QueueStoreCurrencyExchangeRateUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceTaskSummary>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/currencies/exchange-rates/update-tasks",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreCurrencyExchangeRateDto>> UpsertExchangeRateAsync(
            Guid storePublicId,
            string targetCurrencyCode,
            UpsertStoreCurrencyExchangeRateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreCurrencyExchangeRateDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/currencies/exchange-rates/{Uri.EscapeDataString(targetCurrencyCode)}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreCurrencyExchangeRateDto>> DisableExchangeRateAsync(
            Guid storePublicId,
            string targetCurrencyCode,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreCurrencyExchangeRateDto>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/currencies/exchange-rates/{Uri.EscapeDataString(targetCurrencyCode)}/disable",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<GetShipment>> GetShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<GetShipment>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/orders/{orderId:D}/shipment",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<GetShipment>> UpsertShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            UpsertShipmentRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<GetShipment>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/orders/{orderId:D}/shipment",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceMediaResult> GetProductMediaPreviewAsync(
            Guid storePublicId,
            Guid mediaPublicId,
            ProductMediaPreviewQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendMediaAsync(
                storePublicId,
                $"api/commerce/admin/media/products/{mediaPublicId:D}" + BuildMediaPreviewQuery(query),
                cancellationToken);
        }

        public Task<ControlPlaneCommerceMediaResult> GetMediaAssetPreviewAsync(
            Guid storePublicId,
            Guid assetPublicId,
            string canonicalFileName,
            MediaAssetPreviewQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendMediaAsync(
                storePublicId,
                $"api/commerce/admin/media/assets/{assetPublicId:D}/preview" + BuildMediaAssetPreviewQuery(query),
                cancellationToken);
        }

        private async Task<ControlPlaneCommerceCatalogResult<TPayload>> SendAsync<TPayload>(
            Guid storePublicId,
            HttpMethod method,
            string path,
            object? body,
            CancellationToken cancellationToken)
        {
            var store = await this.LoadStoreAsync(storePublicId, cancellationToken);
            var validation = ValidateStoreForRemoteCall(store);
            if (validation is not null)
            {
                return validation.ToResult<TPayload>();
            }

            try
            {
                using var request = new HttpRequestMessage(method, AppendPath(GetControlApiUrl(store!.Node!), AppendStoreKeyQuery(path, store.StoreKey)));
                request.Headers.TryAddWithoutValidation("X-Node-Key", store.Node!.NodeKey);
                request.Headers.TryAddWithoutValidation("X-Node-Secret", store.Node.NodeSecret);

                if (body is not null)
                {
                    request.Content = JsonContent.Create(body, options: SerializerOptions);
                }

                using var response = await this.httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    return Failure<TPayload>(
                        "Commerce Node returned an empty response.",
                        ControlPlaneCommerceCatalogFailure.RemoteFailure,
                        (int)response.StatusCode);
                }

                var envelope = JsonSerializer.Deserialize<CommerceNodeEnvelope<TPayload>>(responseBody, SerializerOptions);
                if (envelope is null)
                {
                    return Failure<TPayload>(
                        "Commerce Node returned a malformed response envelope.",
                        ControlPlaneCommerceCatalogFailure.RemoteFailure,
                        (int)response.StatusCode);
                }

                if (!response.IsSuccessStatusCode || !envelope.Success)
                {
                    return new ControlPlaneCommerceCatalogResult<TPayload>(
                        false,
                        string.IsNullOrWhiteSpace(envelope.Message) ? "Commerce Node catalog request failed." : envelope.Message,
                        envelope.Data,
                        ToFailure(response.StatusCode),
                        (int)response.StatusCode);
                }

                return new ControlPlaneCommerceCatalogResult<TPayload>(
                    true,
                    envelope.Message,
                    envelope.Data,
                    HttpStatusCode: (int)response.StatusCode);
            }
            catch (TaskCanceledException)
            {
                return Failure<TPayload>("Commerce Node catalog request timed out.", ControlPlaneCommerceCatalogFailure.RemoteFailure);
            }
            catch (HttpRequestException ex)
            {
                return Failure<TPayload>(ex.Message, ControlPlaneCommerceCatalogFailure.RemoteFailure);
            }
            catch (JsonException)
            {
                return Failure<TPayload>("Commerce Node returned malformed JSON.", ControlPlaneCommerceCatalogFailure.RemoteFailure);
            }
        }

        private async Task<ControlPlaneCommerceMediaResult> SendMediaAsync(
            Guid storePublicId,
            string path,
            CancellationToken cancellationToken)
        {
            var store = await this.LoadStoreAsync(storePublicId, cancellationToken);
            var validation = ValidateStoreForRemoteCall(store);
            if (validation is not null)
            {
                return validation.ToMediaResult();
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, AppendPath(GetControlApiUrl(store!.Node!), AppendStoreKeyQuery(path, store.StoreKey)));
                request.Headers.TryAddWithoutValidation("X-Node-Key", store.Node!.NodeKey);
                request.Headers.TryAddWithoutValidation("X-Node-Secret", store.Node.NodeSecret);

                using var response = await this.httpClient.SendAsync(request, cancellationToken);
                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return new ControlPlaneCommerceMediaResult(
                        false,
                        bytes.Length > 0 ? System.Text.Encoding.UTF8.GetString(bytes) : "Commerce Node media preview request failed.",
                        Failure: ToFailure(response.StatusCode),
                        HttpStatusCode: (int)response.StatusCode);
                }

                return new ControlPlaneCommerceMediaResult(
                    true,
                    "Product media preview loaded.",
                    bytes,
                    response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
                    HttpStatusCode: (int)response.StatusCode);
            }
            catch (TaskCanceledException)
            {
                return new ControlPlaneCommerceMediaResult(false, "Commerce Node media preview request timed out.", Failure: ControlPlaneCommerceCatalogFailure.RemoteFailure);
            }
            catch (HttpRequestException ex)
            {
                return new ControlPlaneCommerceMediaResult(false, ex.Message, Failure: ControlPlaneCommerceCatalogFailure.RemoteFailure);
            }
        }

        private async Task<ControlPlaneCommerceCatalogResult<TPayload>> SendMultipartAsync<TPayload>(
            Guid storePublicId,
            string path,
            ProductImportUploadRequest upload,
            CancellationToken cancellationToken)
        {
            var store = await this.LoadStoreAsync(storePublicId, cancellationToken);
            var validation = ValidateStoreForRemoteCall(store);
            if (validation is not null)
            {
                return validation.ToResult<TPayload>();
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, AppendPath(GetControlApiUrl(store!.Node!), AppendStoreKeyQuery(path, store.StoreKey)));
                request.Headers.TryAddWithoutValidation("X-Node-Key", store.Node!.NodeKey);
                request.Headers.TryAddWithoutValidation("X-Node-Secret", store.Node.NodeSecret);

                using var form = new MultipartFormDataContent();
                using var fileContent = new StreamContent(upload.Content);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
                form.Add(fileContent, "file", string.IsNullOrWhiteSpace(upload.FileName) ? "products.csv" : upload.FileName);
                form.Add(new StringContent(string.IsNullOrWhiteSpace(upload.Mode) ? ProductImportModes.CreateOnly : upload.Mode), "mode");
                request.Content = form;

                using var response = await this.httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    return Failure<TPayload>("Commerce Node returned an empty response.", ControlPlaneCommerceCatalogFailure.RemoteFailure, (int)response.StatusCode);
                }

                var envelope = JsonSerializer.Deserialize<CommerceNodeEnvelope<TPayload>>(responseBody, SerializerOptions);
                if (envelope is null)
                {
                    return Failure<TPayload>("Commerce Node returned a malformed response envelope.", ControlPlaneCommerceCatalogFailure.RemoteFailure, (int)response.StatusCode);
                }

                if (!response.IsSuccessStatusCode || !envelope.Success)
                {
                    return new ControlPlaneCommerceCatalogResult<TPayload>(
                        false,
                        string.IsNullOrWhiteSpace(envelope.Message) ? "Commerce Node catalog request failed." : envelope.Message,
                        envelope.Data,
                        ToFailure(response.StatusCode),
                        (int)response.StatusCode);
                }

                return new ControlPlaneCommerceCatalogResult<TPayload>(
                    true,
                    envelope.Message,
                    envelope.Data,
                    HttpStatusCode: (int)response.StatusCode);
            }
            catch (TaskCanceledException)
            {
                return Failure<TPayload>("Commerce Node catalog request timed out.", ControlPlaneCommerceCatalogFailure.RemoteFailure);
            }
            catch (HttpRequestException ex)
            {
                return Failure<TPayload>(ex.Message, ControlPlaneCommerceCatalogFailure.RemoteFailure);
            }
            catch (JsonException)
            {
                return Failure<TPayload>("Commerce Node returned malformed JSON.", ControlPlaneCommerceCatalogFailure.RemoteFailure);
            }
        }

        private async Task<ControlPlaneCommerceCatalogResult<TPayload>> SendMediaAssetMultipartAsync<TPayload>(
            Guid storePublicId,
            string path,
            CommerceMediaAssetUploadRequest upload,
            CancellationToken cancellationToken)
        {
            var store = await this.LoadStoreAsync(storePublicId, cancellationToken);
            var validation = ValidateStoreForRemoteCall(store);
            if (validation is not null)
            {
                return validation.ToResult<TPayload>();
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, AppendPath(GetControlApiUrl(store!.Node!), AppendStoreKeyQuery(path, store.StoreKey)));
                request.Headers.TryAddWithoutValidation("X-Node-Key", store.Node!.NodeKey);
                request.Headers.TryAddWithoutValidation("X-Node-Secret", store.Node.NodeSecret);

                using var form = new MultipartFormDataContent();
                using var fileContent = new StreamContent(upload.Content);
                if (!string.IsNullOrWhiteSpace(upload.ContentType))
                {
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(upload.ContentType);
                }

                form.Add(fileContent, "file", string.IsNullOrWhiteSpace(upload.FileName) ? "media-asset" : upload.FileName);
                request.Content = form;

                using var response = await this.httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    return Failure<TPayload>("Commerce Node returned an empty response.", ControlPlaneCommerceCatalogFailure.RemoteFailure, (int)response.StatusCode);
                }

                var envelope = JsonSerializer.Deserialize<CommerceNodeEnvelope<TPayload>>(responseBody, SerializerOptions);
                if (envelope is null)
                {
                    return Failure<TPayload>("Commerce Node returned a malformed response envelope.", ControlPlaneCommerceCatalogFailure.RemoteFailure, (int)response.StatusCode);
                }

                if (!response.IsSuccessStatusCode || !envelope.Success)
                {
                    return new ControlPlaneCommerceCatalogResult<TPayload>(
                        false,
                        string.IsNullOrWhiteSpace(envelope.Message) ? "Commerce Node catalog request failed." : envelope.Message,
                        envelope.Data,
                        ToFailure(response.StatusCode),
                        (int)response.StatusCode);
                }

                return new ControlPlaneCommerceCatalogResult<TPayload>(
                    true,
                    envelope.Message,
                    envelope.Data,
                    HttpStatusCode: (int)response.StatusCode);
            }
            catch (TaskCanceledException)
            {
                return Failure<TPayload>("Commerce Node catalog request timed out.", ControlPlaneCommerceCatalogFailure.RemoteFailure);
            }
            catch (HttpRequestException ex)
            {
                return Failure<TPayload>(ex.Message, ControlPlaneCommerceCatalogFailure.RemoteFailure);
            }
            catch (JsonException)
            {
                return Failure<TPayload>("Commerce Node returned malformed JSON.", ControlPlaneCommerceCatalogFailure.RemoteFailure);
            }
        }

        private async Task<StoreRegistry?> LoadStoreAsync(Guid publicId, CancellationToken cancellationToken)
        {
            return await this.dbContext.Stores
                .AsNoTracking()
                .Include(store => store.Node)
                    .ThenInclude(node => node!.Endpoints)
                .FirstOrDefaultAsync(store => store.PublicId == publicId, cancellationToken);
        }

        private static StoreValidationFailure? ValidateStoreForRemoteCall(StoreRegistry? store)
        {
            if (store is null)
            {
                return new StoreValidationFailure(ControlPlaneCommerceCatalogFailure.NotFound, "Store was not found.");
            }

            if (store.Status == "archived")
            {
                return new StoreValidationFailure(ControlPlaneCommerceCatalogFailure.Validation, "Archived stores cannot be managed.");
            }

            if (store.Node is null || store.Node.Status == "disabled")
            {
                return new StoreValidationFailure(ControlPlaneCommerceCatalogFailure.Validation, "Store node is missing or disabled.");
            }

            if (string.IsNullOrWhiteSpace(store.Node.NodeSecret))
            {
                return new StoreValidationFailure(ControlPlaneCommerceCatalogFailure.Validation, "Store node does not have a node secret configured.");
            }

            if (string.IsNullOrWhiteSpace(GetControlApiUrl(store.Node)))
            {
                return new StoreValidationFailure(ControlPlaneCommerceCatalogFailure.Validation, "Store node does not have an active Control API endpoint.");
            }

            return null;
        }

        private static string GetControlApiUrl(CommerceNode node)
        {
            return node.Endpoints.FirstOrDefault(endpoint =>
                endpoint.Kind == ControlApiEndpointKind &&
                endpoint.IsPrimary &&
                endpoint.DisabledAt is null)?.Url ?? string.Empty;
        }

        private static Uri AppendPath(string baseUrl, string path)
        {
            return new Uri(baseUrl.TrimEnd('/') + "/" + path.TrimStart('/'), UriKind.Absolute);
        }

        private static string AppendStoreKeyQuery(string path, string storeKey)
        {
            var separator = path.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            return path + separator + "storeKey=" + Uri.EscapeDataString(storeKey);
        }

        private static string BuildProductQuery(ProductCatalogQuery query)
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

        private static string BuildSeoSlugHistoryQuery(StoreSeoSlugHistoryQuery query)
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

        private static string BuildInventoryQuery(AdminInventoryQueryDto query)
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

        private static string BuildOrderQuery(AdminOrderQueryDto query)
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

        private static string BuildMediaPreviewQuery(ProductMediaPreviewQuery query)
        {
            var values = new List<KeyValuePair<string, string>>();
            AddIfPresent(values, "w", query.Width?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "h", query.Height?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "fit", query.Fit);
            AddIfPresent(values, "format", query.Format);
            AddIfPresent(values, "v", query.Version?.ToString(CultureInfo.InvariantCulture));
            return ToQueryString(values);
        }

        private static string BuildMediaAssetPreviewQuery(MediaAssetPreviewQuery query)
        {
            var values = new List<KeyValuePair<string, string>>();
            AddIfPresent(values, "w", query.Width?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "h", query.Height?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "fit", query.Fit);
            AddIfPresent(values, "format", query.Format);
            AddIfPresent(values, "v", query.Version?.ToString(CultureInfo.InvariantCulture));
            return ToQueryString(values);
        }

        private static string BuildMediaAssetListQuery(CommerceMediaAssetListQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "search", query.Search);
            return ToQueryString(values);
        }

        private static string BuildProductImportQuery(ProductImportJobListQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "status", query.Status);
            return ToQueryString(values);
        }

        private static string BuildPageQuery(int pageNumber, int pageSize)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, pageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(pageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            return ToQueryString(values);
        }

        private static string BuildProductImportRowsQuery(ProductImportRowsQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "status", query.Status);
            return ToQueryString(values);
        }

        private static string BuildStorefrontPageQuery(StorefrontPageListQuery query)
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

        private static void AddIfPresent(List<KeyValuePair<string, string>> values, string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(new KeyValuePair<string, string>(key, value.Trim()));
            }
        }

        private static string ToQueryString(IReadOnlyCollection<KeyValuePair<string, string>> values)
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

        private static ControlPlaneCommerceCatalogFailure ToFailure(System.Net.HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                System.Net.HttpStatusCode.NotFound => ControlPlaneCommerceCatalogFailure.NotFound,
                System.Net.HttpStatusCode.BadRequest => ControlPlaneCommerceCatalogFailure.Validation,
                System.Net.HttpStatusCode.Conflict => ControlPlaneCommerceCatalogFailure.Validation,
                _ => ControlPlaneCommerceCatalogFailure.RemoteFailure,
            };
        }

        private static ControlPlaneCommerceCatalogResult<TPayload> Failure<TPayload>(
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

        private sealed record CommerceNodeEnvelope<TPayload>(
            bool Success,
            string? Message,
            TPayload? Data);

        private sealed record StoreValidationFailure(ControlPlaneCommerceCatalogFailure Failure, string Message)
        {
            public ControlPlaneCommerceCatalogResult<TPayload> ToResult<TPayload>()
            {
                return new ControlPlaneCommerceCatalogResult<TPayload>(
                    false,
                    this.Message,
                    Failure: this.Failure);
            }

            public ControlPlaneCommerceMediaResult ToMediaResult()
            {
                return new ControlPlaneCommerceMediaResult(
                    false,
                    this.Message,
                    Failure: this.Failure);
            }
        }
    }
}
