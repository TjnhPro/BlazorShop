namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Globalization;

    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Products;

    public sealed class ControlPlaneVariationTemplateGateway : ControlPlaneCommerceGatewayBase, IControlPlaneVariationTemplateGateway
    {
        public ControlPlaneVariationTemplateGateway(ICommerceNodeAdminGatewayTransport transport)
            : base(transport)
        {
        }

        public Task<ApplicationResult<VariationTemplateListResponse>> ListVariationTemplatesAsync(
            Guid storePublicId,
            VariationTemplateListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<VariationTemplateListResponse>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/variation-templates" + BuildPageQuery(query.PageNumber, query.PageSize),
                null,
                cancellationToken);
        }

        public Task<ApplicationResult<VariationTemplateDetailDto>> GetVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ApplicationResult<VariationTemplateDetailDto>> CreateVariationTemplateAsync(
            Guid storePublicId,
            CreateVariationTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/variation-templates",
                request,
                cancellationToken);
        }

        public Task<ApplicationResult<VariationTemplateDetailDto>> UpdateVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            UpdateVariationTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ApplicationResult<VariationTemplateDetailDto>> CreateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CreateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}/options",
                request,
                cancellationToken);
        }

        public Task<ApplicationResult<VariationTemplateDetailDto>> UpdateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            UpdateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}/options/{optionPublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ApplicationResult<VariationTemplateDetailDto>> CreateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            CreateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}/options/{optionPublicId:D}/values",
                request,
                cancellationToken);
        }

        public Task<ApplicationResult<VariationTemplateDetailDto>> UpdateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            Guid valuePublicId,
            UpdateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}/options/{optionPublicId:D}/values/{valuePublicId:D}",
                request,
                cancellationToken);
        }
    }
}
