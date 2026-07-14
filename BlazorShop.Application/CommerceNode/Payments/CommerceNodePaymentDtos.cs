namespace BlazorShop.Application.CommerceNode.Payments
{
    using BlazorShop.Application.DTOs;

    public sealed record PaymentAttemptDto(
        Guid Id,
        Guid StoreId,
        Guid CheckoutSessionId,
        Guid? OrderId,
        string PaymentMethodKey,
        string ProviderKey,
        string State,
        decimal Amount,
        string CurrencyCode,
        string IdempotencyKey,
        string? ProviderReference,
        string? ProviderSessionId,
        string? NextActionType,
        string? NextActionUrl,
        string? FailureCode,
        string? FailureMessage,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

    public sealed record CreatePaymentAttemptRequest(
        Guid StoreId,
        Guid CheckoutSessionId,
        Guid? OrderId,
        string PaymentMethodKey,
        string ProviderKey,
        decimal Amount,
        string CurrencyCode,
        string IdempotencyKey,
        string? MetadataJson = null,
        DateTimeOffset? ExpiresAtUtc = null);

    public sealed record TransitionPaymentAttemptRequest(
        Guid StoreId,
        Guid PaymentAttemptId,
        string NewState,
        string? ProviderReference = null,
        string? ProviderSessionId = null,
        string? NextActionType = null,
        string? NextActionUrl = null,
        string? FailureCode = null,
        string? FailureMessage = null,
        string? MetadataJson = null);

    public sealed record PaymentProviderEventDto(
        Guid Id,
        Guid StoreId,
        Guid? PaymentAttemptId,
        string ProviderKey,
        string? EventId,
        string EventType,
        string PayloadHash,
        bool IsDuplicate,
        DateTimeOffset? ProcessedAtUtc,
        DateTimeOffset CreatedAtUtc);

    public sealed record RecordPaymentProviderEventRequest(
        Guid StoreId,
        Guid? PaymentAttemptId,
        string ProviderKey,
        string? EventId,
        string EventType,
        string PayloadJson,
        DateTimeOffset? ProcessedAtUtc = null);

    public interface IPaymentAttemptService
    {
        Task<ServiceResponse<PaymentAttemptDto>> GetAsync(
            Guid storeId,
            Guid paymentAttemptId,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<PaymentAttemptDto>> CreateAsync(
            CreatePaymentAttemptRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<PaymentAttemptDto>> TransitionAsync(
            TransitionPaymentAttemptRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<PaymentProviderEventDto>> RecordProviderEventAsync(
            RecordPaymentProviderEventRequest request,
            CancellationToken cancellationToken = default);
    }

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
