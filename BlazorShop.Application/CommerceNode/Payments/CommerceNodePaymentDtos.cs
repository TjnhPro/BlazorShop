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

    public static class PaymentProviderActionTypes
    {
        public const string None = "none";
        public const string Redirect = "redirect";
        public const string ClientSecret = "client_secret";
        public const string OfflineInstructions = "offline_instructions";
    }

    public sealed record PaymentProviderOperationRequest(
        Guid StoreId,
        Guid CheckoutSessionId,
        Guid PaymentAttemptId,
        string PaymentMethodKey,
        string ProviderKey,
        decimal Amount,
        string CurrencyCode,
        string IdempotencyKey,
        IReadOnlyList<PaymentProviderSessionLine>? Lines = null,
        string? ProviderReference = null,
        string? ProviderSessionId = null,
        string? PayloadJson = null,
        string? ProviderSignature = null);

    public sealed record PaymentProviderOperationResult(
        string ActionType,
        string? ActionUrl,
        string? ProviderSessionId,
        string? ProviderReference,
        string? SafeFailureCode,
        string? SafeFailureMessage,
        string? MetadataJson,
        string? RecommendedState,
        string? IgnoredReason)
    {
        public static ServiceResponse<PaymentProviderOperationResult> Succeeded(
            string message,
            string actionType = PaymentProviderActionTypes.None,
            string? actionUrl = null,
            string? providerSessionId = null,
            string? providerReference = null,
            string? metadataJson = null,
            string? recommendedState = null,
            string? ignoredReason = null)
        {
            return new ServiceResponse<PaymentProviderOperationResult>(true, message)
            {
                Payload = new PaymentProviderOperationResult(
                    actionType,
                    actionUrl,
                    providerSessionId,
                    providerReference,
                    SafeFailureCode: null,
                    SafeFailureMessage: null,
                    metadataJson,
                    recommendedState,
                    ignoredReason),
                ResponseType = ServiceResponseType.Success,
            };
        }

        public static ServiceResponse<PaymentProviderOperationResult> Failed(
            ServiceResponseType responseType,
            string message,
            string safeFailureCode)
        {
            return new ServiceResponse<PaymentProviderOperationResult>(false, message)
            {
                Payload = new PaymentProviderOperationResult(
                    PaymentProviderActionTypes.None,
                    ActionUrl: null,
                    ProviderSessionId: null,
                    ProviderReference: null,
                    safeFailureCode,
                    message,
                    MetadataJson: null,
                    RecommendedState: null,
                    IgnoredReason: null),
                ResponseType = responseType,
            };
        }

        public static ServiceResponse<PaymentProviderOperationResult> Unsupported(string operationName)
        {
            return Failed(
                ServiceResponseType.ValidationError,
                $"Payment operation '{operationName}' is not supported.",
                "payment.operation_not_supported");
        }
    }

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

    public interface IPaymentWebhookSignatureVerifier
    {
        Task<ServiceResponse<object?>> VerifyAsync(
            string providerKey,
            string payloadJson,
            string? providerSignature,
            CancellationToken cancellationToken = default);
    }

    public sealed record RecordPaymentProviderEventRequest(
        Guid StoreId,
        Guid? PaymentAttemptId,
        string ProviderKey,
        string? EventId,
        string EventType,
        string PayloadJson,
        string? ProviderReference = null,
        string? ProviderSessionId = null,
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

        Task<ServiceResponse<PaymentProviderOperationResult>> ValidateInputAsync(
            PaymentProviderOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PaymentProviderOperationResult.Succeeded("Payment input accepted."));
        }

        Task<ServiceResponse<PaymentProviderOperationResult>> CreatePaymentSessionAsync(
            CreatePaymentProviderSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PaymentProviderOperationResult.Unsupported("create_payment_session"));
        }

        Task<ServiceResponse<PaymentProviderSessionResult>> CreateHostedSessionAsync(
            CreatePaymentProviderSessionRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<PaymentProviderOperationResult>> HandleReturnAsync(
            PaymentProviderOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PaymentProviderOperationResult.Unsupported("handle_return"));
        }

        Task<ServiceResponse<PaymentProviderOperationResult>> HandleCancelAsync(
            PaymentProviderOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PaymentProviderOperationResult.Unsupported("handle_cancel"));
        }

        Task<ServiceResponse<PaymentProviderOperationResult>> HandleWebhookAsync(
            PaymentProviderOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PaymentProviderOperationResult.Unsupported("handle_webhook"));
        }

        Task<ServiceResponse<PaymentProviderOperationResult>> AuthorizeAsync(
            PaymentProviderOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PaymentProviderOperationResult.Unsupported("authorize"));
        }

        Task<ServiceResponse<PaymentProviderOperationResult>> CaptureAsync(
            PaymentProviderOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PaymentProviderOperationResult.Unsupported("capture"));
        }

        Task<ServiceResponse<PaymentProviderOperationResult>> VoidAsync(
            PaymentProviderOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PaymentProviderOperationResult.Unsupported("void"));
        }

        Task<ServiceResponse<PaymentProviderOperationResult>> RefundAsync(
            PaymentProviderOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PaymentProviderOperationResult.Unsupported("refund"));
        }
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
