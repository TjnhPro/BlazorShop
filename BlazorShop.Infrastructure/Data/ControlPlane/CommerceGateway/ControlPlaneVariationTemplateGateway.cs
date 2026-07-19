namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Globalization;

    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Products;

    public sealed class ControlPlaneVariationTemplateGateway : ControlPlaneCommerceGatewayBase, IControlPlaneVariationTemplateGateway
    {
        public ControlPlaneVariationTemplateGateway(ICommerceNodeAdminGatewayTransport transport)
            : base(transport)
        {
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
    }
}
