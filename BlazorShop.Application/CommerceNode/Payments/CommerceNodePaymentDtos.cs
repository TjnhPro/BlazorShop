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
        string? BaseCurrencyCode,
        decimal? BaseAmount,
        decimal? ExchangeRate,
        string? ExchangeRateProviderKey,
        string? ExchangeRateSource,
        DateTimeOffset? ExchangeRateEffectiveAtUtc,
        DateTimeOffset? ExchangeRateExpiresAtUtc,
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
        string? BaseCurrencyCode = null,
        decimal? BaseAmount = null,
        decimal? ExchangeRate = null,
        string? ExchangeRateProviderKey = null,
        string? ExchangeRateSource = null,
        DateTimeOffset? ExchangeRateEffectiveAtUtc = null,
        DateTimeOffset? ExchangeRateExpiresAtUtc = null,
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

    public sealed record PaymentProviderSessionLine(
        Guid ProductId,
        string Name,
        int Quantity,
        decimal UnitAmount);

    public sealed record CreatePaymentProviderSessionRequest(
        Guid StoreId,
        Guid CheckoutSessionId,
        Guid PaymentAttemptId,
        string PaymentMethodKey,
        string ProviderKey,
        decimal Amount,
        string CurrencyCode,
        string IdempotencyKey,
        IReadOnlyList<PaymentProviderSessionLine> Lines);

    public sealed record PaymentProviderSessionResult(
        string ProviderSessionId,
        string? ProviderReference,
        string NextActionType,
        string NextActionUrl,
        string? MetadataJson);

    public static class PaymentProviderMethodTypes
    {
        public const string Offline = "offline";
        public const string Redirect = "redirect";
        public const string Immediate = "immediate";
    }

    public sealed record PaymentProviderCapabilityDto(
        string SystemName,
        bool Installed,
        bool Active,
        string DisplayName,
        string? Description,
        string? IconUrl,
        int DefaultDisplayOrder,
        IReadOnlyList<Guid> SupportedStoreIds,
        IReadOnlyList<string> SupportedCurrencyCodes,
        IReadOnlyList<string> SupportedCountryCodes,
        decimal? MinOrderTotal,
        decimal? MaxOrderTotal,
        string MethodType,
        bool RecurringCapable,
        bool SupportsAuthorize,
        bool SupportsCapture,
        bool SupportsVoid,
        bool SupportsRefund,
        bool SupportsPartialRefund,
        bool RequiresWebhookSignature);

    public interface IPaymentProviderCapabilityRegistry
    {
        IReadOnlyList<PaymentProviderCapabilityDto> List();

        ServiceResponse<PaymentProviderCapabilityDto> Get(string systemName);
    }

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

    public interface IStorefrontPaymentProvider
    {
        string ProviderKey { get; }

        Task<ServiceResponse<PaymentProviderSessionResult>> CreateHostedSessionAsync(
            CreatePaymentProviderSessionRequest request,
            CancellationToken cancellationToken = default);
    }

    public interface IStorefrontPaymentProviderResolver
    {
        IStorefrontPaymentProvider Resolve(string providerKey);
    }

    public sealed record StorePaymentMethodDto(
        Guid Id,
        string PaymentMethodKey,
        string DisplayName,
        string? Description,
        bool Enabled,
        int DisplayOrder,
        string? ShortDisplayText,
        string? IconUrl,
        IReadOnlyList<string> SupportedCurrencyCodes,
        IReadOnlyList<string> SupportedCountryCodes,
        decimal? MinOrderTotal,
        decimal? MaxOrderTotal,
        StorePaymentMethodSettingsStatusDto Settings,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed record StorePaymentMethodSettingsStatusDto(
        bool Configured);

    public sealed record UpdateStorePaymentMethodRequest(
        bool Enabled,
        string DisplayName,
        string? Description,
        int DisplayOrder,
        string? SettingsJson,
        bool ClearSettings = false,
        string? ShortDisplayText = null,
        string? IconUrl = null,
        IReadOnlyList<string>? SupportedCurrencyCodes = null,
        IReadOnlyList<string>? SupportedCountryCodes = null,
        decimal? MinOrderTotal = null,
        decimal? MaxOrderTotal = null);

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
