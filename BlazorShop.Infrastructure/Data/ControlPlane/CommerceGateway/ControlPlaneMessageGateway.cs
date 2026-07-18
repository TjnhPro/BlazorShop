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
    public sealed class ControlPlaneMessageGateway : ControlPlaneCommerceGatewayBase, BlazorShop.Application.ControlPlane.CommerceGateway.Messages.IControlPlaneMessageGateway
    {
        public ControlPlaneMessageGateway(ICommerceNodeAdminGatewayTransport transport)
            : base(transport)
        {
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreEmailSettingsResponse>> GetEmailSettingsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreEmailSettingsResponse>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/email-settings",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreEmailSettingsResponse>> UpdateEmailSettingsAsync(
            Guid storePublicId,
            UpdateStoreEmailSettingsRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreEmailSettingsResponse>(
                storePublicId,
                HttpMethod.Put,
                "api/commerce/admin/email-settings",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreEmailSettingsResponse>> RotateEmailPasswordAsync(
            Guid storePublicId,
            RotateStoreEmailPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreEmailSettingsResponse>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/email-settings/password/rotate",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreEmailSettingsResponse>> ClearEmailPasswordAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreEmailSettingsResponse>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/email-settings/password/clear",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<SendStoreEmailTestResponse>> SendEmailTestAsync(
            Guid storePublicId,
            SendStoreEmailTestRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<SendStoreEmailTestResponse>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/email-settings/test-send",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<MessageTemplateAdminSummary>>> ListMessageTemplatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<MessageTemplateAdminSummary>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/message-templates",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<MessageTemplateAdminDetail>> GetMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<MessageTemplateAdminDetail>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/message-templates/{templatePublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<MessageTemplateAdminDetail>> UpdateMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            UpdateMessageTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<MessageTemplateAdminDetail>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/message-templates/{templatePublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<MessageTemplateAdminDetail>> ResetMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<MessageTemplateAdminDetail>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/message-templates/{templatePublicId:D}/reset",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<MessageTemplatePreviewResponse>> PreviewMessageTemplateAsync(
            Guid storePublicId,
            PreviewMessageTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<MessageTemplatePreviewResponse>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/message-templates/preview",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<QueuedMessageAdminListResponse>> ListQueuedMessagesAsync(
            Guid storePublicId,
            string? status = null,
            string? templateSystemName = null,
            int skip = 0,
            int take = 25,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<QueuedMessageAdminListResponse>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/queued-messages" + BuildQueuedMessageQuery(status, templateSystemName, skip, take),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<QueuedMessageAdminDetail>> GetQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<QueuedMessageAdminDetail>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/queued-messages/{queuedMessagePublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<QueuedMessageAdminDetail>> RetryQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<QueuedMessageAdminDetail>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/queued-messages/{queuedMessagePublicId:D}/retry",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<QueuedMessageAdminDetail>> CancelQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<QueuedMessageAdminDetail>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/queued-messages/{queuedMessagePublicId:D}/cancel",
                null,
                cancellationToken);
        }
    }
}

