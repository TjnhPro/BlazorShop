namespace BlazorShop.Application.ControlPlane.CommerceGateway.Content
{
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Domain.Contracts;
    public interface IControlPlaneContentGateway
    {
        
                Task<ApplicationResult<StorefrontPageListResponse>> ListStorefrontPagesAsync(
                    Guid storePublicId,
                    StorefrontPageListQuery query,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<IReadOnlyList<StorefrontPageTemplateDefinitionDto>>> ListStorefrontPageTemplatesAsync(
                    Guid storePublicId,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<IReadOnlyList<StorefrontPageTemplateStatusDto>>> GetStorefrontPageTemplateStatusAsync(
                    Guid storePublicId,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<StorefrontPageDetailDto>> GetStorefrontPageAsync(
                    Guid storePublicId,
                    Guid pagePublicId,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<StorefrontPageDetailDto>> CreateStorefrontPageAsync(
                    Guid storePublicId,
                    CreateStorefrontPageRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<StorefrontPageDetailDto>> UpdateStorefrontPageAsync(
                    Guid storePublicId,
                    Guid pagePublicId,
                    UpdateStorefrontPageRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<StorefrontPageDetailDto>> ArchiveStorefrontPageAsync(
                    Guid storePublicId,
                    Guid pagePublicId,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<StorefrontPageDetailDto>> CreateStorefrontPageDraftFromTemplateAsync(
                    Guid storePublicId,
                    string pageKey,
                    CreatePageFromTemplateRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<StorefrontPageDetailDto>> MapStorefrontPageTemplateAsync(
                    Guid storePublicId,
                    Guid pagePublicId,
                    MapPageTemplateRequest request,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<StorefrontPageDetailDto>> ClearStorefrontPageTemplateAsync(
                    Guid storePublicId,
                    Guid pagePublicId,
                    CancellationToken cancellationToken = default);

        
                Task<ApplicationResult<StorefrontPageDetailDto>> UpdateStorefrontPageNavigationAsync(
                    Guid storePublicId,
                    Guid pagePublicId,
                    UpdatePageNavigationRequest request,
                    CancellationToken cancellationToken = default);
    }
}

