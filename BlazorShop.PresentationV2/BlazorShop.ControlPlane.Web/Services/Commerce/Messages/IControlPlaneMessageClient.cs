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

        public interface IControlPlaneMessageClient
    {
        Task<ControlPlaneClientResult<StoreEmailSettingsResponse>> GetEmailSettingsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreEmailSettingsResponse>> UpdateEmailSettingsAsync(
            Guid storePublicId,
            UpdateStoreEmailSettingsRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreEmailSettingsResponse>> RotateEmailPasswordAsync(
            Guid storePublicId,
            RotateStoreEmailPasswordRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<StoreEmailSettingsResponse>> ClearEmailPasswordAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<SendStoreEmailTestResponse>> SendEmailTestAsync(
            Guid storePublicId,
            SendStoreEmailTestRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<IReadOnlyList<MessageTemplateAdminSummary>>> ListMessageTemplatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<MessageTemplateAdminDetail>> GetMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<MessageTemplateAdminDetail>> UpdateMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            UpdateMessageTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<MessageTemplateAdminDetail>> ResetMessageTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<MessageTemplatePreviewResponse>> PreviewMessageTemplateAsync(
            Guid storePublicId,
            PreviewMessageTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<QueuedMessageAdminListResponse>> ListQueuedMessagesAsync(
            Guid storePublicId,
            string? status = null,
            string? templateSystemName = null,
            int skip = 0,
            int take = 25,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<QueuedMessageAdminDetail>> GetQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<QueuedMessageAdminDetail>> RetryQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<QueuedMessageAdminDetail>> CancelQueuedMessageAsync(
            Guid storePublicId,
            Guid queuedMessagePublicId,
            CancellationToken cancellationToken = default);
    }
}

