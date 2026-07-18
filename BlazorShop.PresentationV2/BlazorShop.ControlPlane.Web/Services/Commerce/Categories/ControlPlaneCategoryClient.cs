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

        public sealed class ControlPlaneCategoryClient : ControlPlaneCommerceClientBase, IControlPlaneCategoryClient
    {
        public ControlPlaneCategoryClient(IControlPlaneApiClient apiClient)
            : base(apiClient)
        {
        }
        public Task<ControlPlaneClientResult<PagedResult<GetCategory>>> ListCategoriesAsync(
            Guid storePublicId,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<PagedResult<GetCategory>>(
                CommerceRoute(storePublicId, "categories") + BuildPageQuery(pageNumber, pageSize),
                "Unable to load categories.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<GetCategoryTreeNode>>> GetCategoryTreeAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<IReadOnlyList<GetCategoryTreeNode>>(
                CommerceRoute(storePublicId, "categories/tree"),
                "Unable to load category tree.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> CreateCategoryAsync(
            Guid storePublicId,
            CreateCategory request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<CreateCategory, object>(
                CommerceRoute(storePublicId, "categories"),
                request,
                "Unable to create category.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> UpdateCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            UpdateCategory request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<UpdateCategory, object>(
                CommerceRoute(storePublicId, $"categories/{categoryId:D}"),
                request,
                "Unable to update category.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> ArchiveCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.DeletePrivateAsync<object>(
                CommerceRoute(storePublicId, $"categories/{categoryId:D}"),
                "Unable to archive category.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<CategorySeoDto>> GetCategorySeoAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<CategorySeoDto>(
                CommerceRoute(storePublicId, $"categories/{categoryId:D}/seo"),
                "Unable to load category SEO.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<CategorySeoDto>> UpdateCategorySeoAsync(
            Guid storePublicId,
            Guid categoryId,
            UpdateCategorySeoDto request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<UpdateCategorySeoDto, CategorySeoDto>(
                CommerceRoute(storePublicId, $"categories/{categoryId:D}/seo"),
                request,
                "Unable to update category SEO.",
                cancellationToken);
        }
    }
}

