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
    public sealed class ControlPlaneCategoryGateway : ControlPlaneCommerceGatewayBase, BlazorShop.Application.ControlPlane.CommerceGateway.Categories.IControlPlaneCategoryGateway
    {
        public ControlPlaneCategoryGateway(ICommerceNodeAdminGatewayTransport transport)
            : base(transport)
        {
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
    }
}

