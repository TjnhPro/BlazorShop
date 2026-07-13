namespace BlazorShop.Application.CommerceNode.Payments
{
    using BlazorShop.Application.DTOs;

    public sealed record StorePaymentMethodDto(
        Guid Id,
        string PaymentMethodKey,
        string DisplayName,
        string? Description,
        bool Enabled,
        int DisplayOrder,
        string? SettingsJson,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed record UpdateStorePaymentMethodRequest(
        bool Enabled,
        string DisplayName,
        string? Description,
        int DisplayOrder,
        string? SettingsJson);

    public interface IStorePaymentMethodAdminService
    {
        Task<IReadOnlyList<StorePaymentMethodDto>> GetAsync(CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorePaymentMethodDto>> UpdateAsync(
            string paymentMethodKey,
            UpdateStorePaymentMethodRequest request,
            CancellationToken cancellationToken = default);
    }

    public sealed record PaymentHandlerContext(
        Guid StoreId,
        Guid OrderId,
        string PaymentMethodKey,
        decimal Amount,
        string CurrencyCode,
        string? MetadataJson);

    public sealed record PaymentHandlerResult(
        bool Success,
        string Message,
        string PaymentStatus,
        DateTime? PaymentAt = null,
        string? ProviderReference = null,
        string? MetadataJson = null);

    public interface IPaymentHandler
    {
        string PaymentMethodKey { get; }

        Task<PaymentHandlerResult> ProcessAsync(
            PaymentHandlerContext context,
            CancellationToken cancellationToken = default);
    }

    public interface IPaymentHandlerResolver
    {
        IPaymentHandler Resolve(string paymentMethodKey);
    }
}
