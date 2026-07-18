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

        public sealed class ControlPlaneMessageClient : ControlPlaneCommerceClientBase, IControlPlaneMessageClient
    {
        public ControlPlaneMessageClient(IControlPlaneApiClient apiClient)
            : base(apiClient)
        {
        }
        public Task<ControlPlaneClientResult<StoreEmailSettingsResponse>> GetEmailSettingsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<StoreEmailSettingsResponse>(
                CommerceRoute(storePublicId, "email-settings"),
                "Unable to load store email settings.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreEmailSettingsResponse>> UpdateEmailSettingsAsync(
            Guid storePublicId,
            UpdateStoreEmailSettingsRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<UpdateStoreEmailSettingsRequest, StoreEmailSettingsResponse>(
                CommerceRoute(storePublicId, "email-settings"),
                request,
                "Unable to update store email settings.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreEmailSettingsResponse>> RotateEmailPasswordAsync(
            Guid storePublicId,
            RotateStoreEmailPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<RotateStoreEmailPasswordRequest, StoreEmailSettingsResponse>(
                CommerceRoute(storePublicId, "email-settings/password/rotate"),
                request,
                "Unable to rotate store SMTP password.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreEmailSettingsResponse>> ClearEmailPasswordAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<StoreEmailSettingsResponse>(
                CommerceRoute(storePublicId, "email-settings/password/clear"),
                "Unable to clear store SMTP password.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<SendStoreEmailTestResponse>> SendEmailTestAsync(
            Guid storePublicId,
            SendStoreEmailTestRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<SendStoreEmailTestRequest, SendStoreEmailTestResponse>(
                CommerceRoute(storePublicId, "email-settings/test-send"),
                request,
                "Unable to send store SMTP test email.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<MessageTemplateAdminSummary>>> ListMessageTemplatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<IReadOnlyList<MessageTemplateAdminSummary>>(
                CommerceRoute(storePublicId, "message-templates"),
                "Unable to load message templates.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<MessageTemplateAdminDetail>> GetMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<MessageTemplateAdminDetail>(
                CommerceRoute(storePublicId, $"message-templates/{templatePublicId:D}"),
                "Unable to load message template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<MessageTemplateAdminDetail>> UpdateMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            UpdateMessageTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<UpdateMessageTemplateRequest, MessageTemplateAdminDetail>(
                CommerceRoute(storePublicId, $"message-templates/{templatePublicId:D}"),
                request,
                "Unable to update message template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<MessageTemplateAdminDetail>> ResetMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<MessageTemplateAdminDetail>(
                CommerceRoute(storePublicId, $"message-templates/{templatePublicId:D}/reset"),
                "Unable to reset message template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<MessageTemplatePreviewResponse>> PreviewMessageTemplateAsync(
            Guid storePublicId,
            PreviewMessageTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<PreviewMessageTemplateRequest, MessageTemplatePreviewResponse>(
                CommerceRoute(storePublicId, "message-templates/preview"),
                request,
                "Unable to preview message template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<QueuedMessageAdminListResponse>> ListQueuedMessagesAsync(
            Guid storePublicId,
            string? status = null,
            string? templateSystemName = null,
            int skip = 0,
            int take = 25,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<QueuedMessageAdminListResponse>(
                CommerceRoute(storePublicId, "queued-messages" + BuildQueuedMessageQuery(status, templateSystemName, skip, take)),
                "Unable to load queued messages.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<QueuedMessageAdminDetail>> GetQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<QueuedMessageAdminDetail>(
                CommerceRoute(storePublicId, $"queued-messages/{queuedMessagePublicId:D}"),
                "Unable to load queued message.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<QueuedMessageAdminDetail>> RetryQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<QueuedMessageAdminDetail>(
                CommerceRoute(storePublicId, $"queued-messages/{queuedMessagePublicId:D}/retry"),
                "Unable to retry queued message.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<QueuedMessageAdminDetail>> CancelQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<QueuedMessageAdminDetail>(
                CommerceRoute(storePublicId, $"queued-messages/{queuedMessagePublicId:D}/cancel"),
                "Unable to cancel queued message.",
                cancellationToken);
        }
    }
}

