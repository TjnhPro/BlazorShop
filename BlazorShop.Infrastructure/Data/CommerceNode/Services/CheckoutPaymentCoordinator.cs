namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class CheckoutPaymentCoordinator
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private const string DefaultCurrencyCode = "USD";

        private readonly CommerceNodeDbContext context;
        private readonly IPaymentProviderCapabilityRegistry paymentProviderCapabilityRegistry;
        private readonly IStorefrontPaymentProviderResolver paymentProviderResolver;

        public CheckoutPaymentCoordinator(
            CommerceNodeDbContext context,
            IPaymentProviderCapabilityRegistry paymentProviderCapabilityRegistry,
            IStorefrontPaymentProviderResolver paymentProviderResolver)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(paymentProviderCapabilityRegistry);
            ArgumentNullException.ThrowIfNull(paymentProviderResolver);

            this.context = context;
            this.paymentProviderCapabilityRegistry = paymentProviderCapabilityRegistry;
            this.paymentProviderResolver = paymentProviderResolver;
        }

        internal async Task<IReadOnlyList<StorefrontCheckoutPaymentMethodOption>> ResolvePaymentMethodOptionsAsync(
            CheckoutSession session,
            string selectedKey,
            CancellationToken cancellationToken)
        {
            var currencyCode = NormalizeCurrency(session.CurrencyCode) ?? DefaultCurrencyCode;
            var countryCode = NormalizeCountry(session.ShippingCountryCode);
            var orderTotal = session.GrandTotal;
            var methods = await this.context.StorePaymentMethods
                .AsNoTracking()
                .Where(method => method.StoreId == session.StoreId && method.Enabled)
                .OrderBy(method => method.DisplayOrder)
                .ThenBy(method => method.DisplayName)
                .ToArrayAsync(cancellationToken);

            return methods
                .Where(method =>
                    SupportsValue(ParseCodes(method.SupportedCurrencyCodesJson), currencyCode)
                    && SupportsValue(ParseCodes(method.SupportedCountryCodesJson), countryCode)
                    && (!method.MinOrderTotal.HasValue || orderTotal >= method.MinOrderTotal.Value)
                    && (!method.MaxOrderTotal.HasValue || orderTotal <= method.MaxOrderTotal.Value))
                .Select(method => new
                {
                    Method = method,
                    Key = NormalizeKey(method.PaymentMethodKey),
                })
                .Where(candidate => candidate.Key is not null)
                .Select(candidate => new StorefrontCheckoutPaymentMethodOption(
                    candidate.Key!,
                    candidate.Method.DisplayName,
                    candidate.Method.Description,
                    candidate.Method.ShortDisplayText,
                    candidate.Method.IconUrl,
                    candidate.Key!,
                    this.ResolvePaymentNextActionKind(candidate.Key!),
                    string.Equals(candidate.Key, selectedKey, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }

        internal StorefrontCheckoutPaymentMethodOption? CreateSelectedPaymentMethod(string? paymentMethodKey)
        {
            var key = NormalizeKey(paymentMethodKey);
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return new StorefrontCheckoutPaymentMethodOption(
                key,
                key,
                null,
                null,
                null,
                key,
                this.ResolvePaymentNextActionKind(key),
                Selected: true);
        }

        internal ServiceResponse<PaymentProviderCapabilityDto> ResolveActiveCapability(string? paymentMethodKey)
        {
            var key = NormalizeKey(paymentMethodKey);
            if (key is null)
            {
                return Failed<PaymentProviderCapabilityDto>(
                    ServiceResponseType.Conflict,
                    "Payment provider is not available for order placement.");
            }

            var capabilityResult = this.paymentProviderCapabilityRegistry.Get(key);
            if (!capabilityResult.Success || capabilityResult.Payload is null)
            {
                return Failed<PaymentProviderCapabilityDto>(
                    ServiceResponseType.Conflict,
                    "Payment provider is not available for order placement.");
            }

            var capability = capabilityResult.Payload;
            if (!capability.Installed || !capability.Active)
            {
                return Failed<PaymentProviderCapabilityDto>(
                    ServiceResponseType.Conflict,
                    "Payment provider is not available for order placement.");
            }

            return Succeeded("Payment provider capability loaded.", capability);
        }

        internal async Task<bool> IsPaymentMethodAvailableAsync(
            Guid storeId,
            string paymentMethodKey,
            string? currencyCode,
            string? countryCode,
            decimal orderTotal,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(paymentMethodKey))
            {
                return false;
            }

            var method = await this.context.StorePaymentMethods
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    method => method.StoreId == storeId
                        && method.PaymentMethodKey == paymentMethodKey,
                    cancellationToken);

            if (method is null || !method.Enabled)
            {
                return false;
            }

            var currencyCodes = ParseCodes(method.SupportedCurrencyCodesJson);
            var normalizedCurrency = NormalizeCurrency(currencyCode);
            if (currencyCodes.Count > 0
                && (normalizedCurrency is null || !currencyCodes.Contains(normalizedCurrency, StringComparer.Ordinal)))
            {
                return false;
            }

            var countryCodes = ParseCodes(method.SupportedCountryCodesJson);
            var normalizedCountry = NormalizeCountry(countryCode);
            if (countryCodes.Count > 0
                && (normalizedCountry is null || !countryCodes.Contains(normalizedCountry, StringComparer.Ordinal)))
            {
                return false;
            }

            if (method.MinOrderTotal.HasValue && orderTotal < method.MinOrderTotal.Value)
            {
                return false;
            }

            if (method.MaxOrderTotal.HasValue && orderTotal > method.MaxOrderTotal.Value)
            {
                return false;
            }

            return true;
        }

        internal PaymentAttempt CreatePaymentAttempt(CheckoutPaymentAttemptDraft draft)
        {
            var now = DateTimeOffset.UtcNow;
            return new PaymentAttempt
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = draft.StoreId,
                CheckoutSessionId = draft.CheckoutSessionId,
                PaymentMethodKey = NormalizeKey(draft.PaymentMethodKey) ?? draft.PaymentMethodKey,
                ProviderKey = NormalizeKey(draft.ProviderKey) ?? draft.ProviderKey,
                State = PaymentAttemptStates.Created,
                Amount = draft.Amount,
                CurrencyCode = NormalizeCurrency(draft.CurrencyCode) ?? DefaultCurrencyCode,
                BaseCurrencyCode = NormalizeCurrency(draft.RateSnapshot.BaseCurrencyCode),
                BaseAmount = draft.RateSnapshot.BaseTotalAmount,
                ExchangeRate = draft.RateSnapshot.ExchangeRate,
                ExchangeRateProviderKey = NormalizeKey(draft.RateSnapshot.ExchangeRateProviderKey),
                ExchangeRateSource = NormalizeNullable(draft.RateSnapshot.ExchangeRateSource),
                ExchangeRateEffectiveAtUtc = draft.RateSnapshot.ExchangeRateEffectiveAtUtc,
                ExchangeRateExpiresAtUtc = draft.RateSnapshot.ExchangeRateExpiresAtUtc,
                IdempotencyKey = draft.IdempotencyKey,
                ExpiresAtUtc = now.AddMinutes(30),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
        }

        internal async Task<ServiceResponse<PaymentProviderOperationResult>> CreateProviderPaymentSessionAsync(
            CreatePaymentProviderSessionRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var provider = this.paymentProviderResolver.Resolve(request.ProviderKey);
                return await provider.CreatePaymentSessionAsync(request, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return new ServiceResponse<PaymentProviderOperationResult>(false, ex.Message)
                {
                    ResponseType = ServiceResponseType.Conflict,
                };
            }
        }

        internal async Task<ServiceResponse<StorefrontPlaceOrderResult>> CreateOnlinePaymentSessionAsync(
            Guid storeId,
            CheckoutSession session,
            CartSession cart,
            IReadOnlyList<CheckoutPaymentLineSnapshot> lines,
            decimal totalAmount,
            string currencyCode,
            CheckoutPaymentRateSnapshot rateSnapshot,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            var existingAttempt = await this.context.PaymentAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    attempt => attempt.StoreId == storeId
                        && attempt.IdempotencyKey == idempotencyKey,
                    cancellationToken);
            if (existingAttempt is not null)
            {
                return Succeeded("Payment session already exists.", ToOnlinePlaceOrderResult(session, existingAttempt, idempotencyKey));
            }

            var paymentAttempt = this.CreatePaymentAttempt(new CheckoutPaymentAttemptDraft(
                storeId,
                session.Id,
                session.PaymentMethodKey,
                session.PaymentMethodKey,
                totalAmount,
                currencyCode,
                idempotencyKey,
                rateSnapshot));

            this.context.PaymentAttempts.Add(paymentAttempt);
            this.AppendPaymentAudit(
                paymentAttempt,
                oldState: null,
                PaymentAttemptStates.Created,
                "payment_attempt.created",
                "Payment attempt created.",
                providerMetadataJson: null);
            try
            {
                await this.context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                var duplicate = await this.context.PaymentAttempts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        attempt => attempt.StoreId == storeId
                            && attempt.IdempotencyKey == idempotencyKey,
                        cancellationToken);
                if (duplicate is not null)
                {
                    return Succeeded("Payment session already exists.", ToOnlinePlaceOrderResult(session, duplicate, idempotencyKey));
                }

                throw;
            }

            var providerResult = await this.CreateProviderPaymentSessionAsync(
                new CreatePaymentProviderSessionRequest(
                    storeId,
                    session.PublicId,
                    paymentAttempt.PublicId,
                    paymentAttempt.PaymentMethodKey,
                    paymentAttempt.ProviderKey,
                    totalAmount,
                    currencyCode,
                    idempotencyKey,
                    lines.Select(line => new PaymentProviderSessionLine(
                        line.ProductId,
                        line.Name,
                        line.Quantity,
                        line.UnitPrice)).ToArray()),
                cancellationToken);

            if (!providerResult.Success || providerResult.Payload is null)
            {
                this.MarkAttemptFailed(paymentAttempt, providerResult, DateTimeOffset.UtcNow);
                await this.context.SaveChangesAsync(cancellationToken);
                return Failed<StorefrontPlaceOrderResult>(
                    providerResult.ResponseType is ServiceResponseType.Success ? ServiceResponseType.Conflict : providerResult.ResponseType,
                    paymentAttempt.FailureMessage ?? "Payment provider session could not be created.");
            }

            var providerSession = providerResult.Payload;
            paymentAttempt.State = NormalizeNullable(providerSession.RecommendedState) ?? PaymentAttemptStates.RequiresAction;
            paymentAttempt.ProviderSessionId = providerSession.ProviderSessionId;
            paymentAttempt.ProviderReference = providerSession.ProviderReference;
            paymentAttempt.NextActionType = providerSession.ActionType;
            paymentAttempt.NextActionUrl = providerSession.ActionUrl;
            paymentAttempt.MetadataJson = providerSession.MetadataJson;
            paymentAttempt.UpdatedAtUtc = DateTimeOffset.UtcNow;
            this.AppendPaymentAudit(
                paymentAttempt,
                PaymentAttemptStates.Created,
                paymentAttempt.State,
                $"payment_attempt.{paymentAttempt.State}",
                "Payment attempt requires provider action.",
                providerSession.MetadataJson);

            CheckoutSessionStateRules.Touch(session, CheckoutSessionStates.OrderPending, CheckoutSteps.PlaceOrder, DateTimeOffset.UtcNow);
            session.IdempotencyKey = idempotencyKey;
            ApplyRateSnapshot(session, rateSnapshot);
            cart.LastActivityAtUtc = DateTimeOffset.UtcNow;
            cart.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken);
            return Succeeded("Payment session created.", ToOnlinePlaceOrderResult(session, paymentAttempt, idempotencyKey));
        }

        internal void MarkAttemptFailed(
            PaymentAttempt paymentAttempt,
            ServiceResponse<PaymentProviderOperationResult> providerResult,
            DateTimeOffset now)
        {
            paymentAttempt.State = PaymentAttemptStates.Failed;
            paymentAttempt.FailureCode = providerResult.Payload?.SafeFailureCode ?? "provider_session_failed";
            paymentAttempt.FailureMessage = providerResult.Payload?.SafeFailureMessage
                ?? providerResult.Message
                ?? "Payment provider session could not be created.";
            paymentAttempt.UpdatedAtUtc = now;
            this.AppendPaymentAudit(
                paymentAttempt,
                PaymentAttemptStates.Created,
                PaymentAttemptStates.Failed,
                "payment_attempt.failed",
                paymentAttempt.FailureMessage,
                providerResult.Payload?.MetadataJson);
        }

        internal void AppendPaymentAudit(
            PaymentAttempt attempt,
            string? oldState,
            string newState,
            string eventType,
            string message,
            string? providerMetadataJson)
        {
            var normalizedMessage = NormalizeNullable(message) ?? "Payment attempt updated.";
            this.context.PaymentAttemptAuditLogs.Add(new PaymentAttemptAuditLog
            {
                Id = Guid.NewGuid(),
                StoreId = attempt.StoreId,
                OrderId = attempt.OrderId,
                PaymentAttemptId = attempt.Id,
                ProviderKey = attempt.ProviderKey,
                EventType = NormalizeNullable(eventType) is { Length: <= 128 } normalizedEventType
                    ? normalizedEventType
                    : "payment_attempt.updated",
                OldState = NormalizeKey(oldState),
                NewState = NormalizeKey(newState) ?? PaymentAttemptStates.Created,
                Message = normalizedMessage.Length > 512 ? normalizedMessage[..512] : normalizedMessage,
                MetadataJson = BuildPaymentAuditMetadataJson(attempt, providerMetadataJson),
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
        }

        internal static bool IsAsyncPaymentMethod(string methodType)
        {
            return string.Equals(methodType, PaymentProviderMethodTypes.Redirect, StringComparison.OrdinalIgnoreCase);
        }

        internal static StorefrontPlaceOrderResult ToOnlinePlaceOrderResult(
            CheckoutSession session,
            PaymentAttempt paymentAttempt,
            string idempotencyKey)
        {
            return new StorefrontPlaceOrderResult(
                session.PublicId,
                paymentAttempt.PublicId,
                paymentAttempt.OrderId,
                null,
                null,
                PaymentStatuses.Pending,
                paymentAttempt.PaymentMethodKey,
                paymentAttempt.Amount,
                NormalizeCurrency(paymentAttempt.CurrencyCode) ?? DefaultCurrencyCode,
                idempotencyKey,
                paymentAttempt.CreatedAtUtc.UtcDateTime,
                GuestAccessToken: null,
                NextActionType: paymentAttempt.NextActionType,
                NextActionUrl: paymentAttempt.NextActionUrl);
        }

        private string ResolvePaymentNextActionKind(string paymentMethodKey)
        {
            var capability = this.paymentProviderCapabilityRegistry.Get(paymentMethodKey);
            if (!capability.Success || capability.Payload is null)
            {
                return PaymentProviderActionTypes.None;
            }

            return IsAsyncPaymentMethod(capability.Payload.MethodType)
                ? PaymentProviderActionTypes.Redirect
                : PaymentProviderActionTypes.None;
        }

        private static void ApplyRateSnapshot(CheckoutSession session, CheckoutPaymentRateSnapshot rateSnapshot)
        {
            session.BaseCurrencyCode = rateSnapshot.BaseCurrencyCode;
            session.BaseSubtotal = rateSnapshot.BaseTotalAmount;
            session.BaseGrandTotal = rateSnapshot.BaseTotalAmount;
            session.ExchangeRate = rateSnapshot.ExchangeRate;
            session.ExchangeRateProviderKey = rateSnapshot.ExchangeRateProviderKey;
            session.ExchangeRateSource = rateSnapshot.ExchangeRateSource;
            session.ExchangeRateEffectiveAtUtc = rateSnapshot.ExchangeRateEffectiveAtUtc;
            session.ExchangeRateExpiresAtUtc = rateSnapshot.ExchangeRateExpiresAtUtc;
        }

        private static bool SupportsValue(IReadOnlyList<string> supportedValues, string? value)
        {
            return supportedValues.Count == 0
                || (value is not null && supportedValues.Contains(value, StringComparer.OrdinalIgnoreCase));
        }

        private static string BuildPaymentAuditMetadataJson(PaymentAttempt attempt, string? providerMetadataJson)
        {
            return JsonSerializer.Serialize(new
            {
                providerReference = NormalizeNullable(attempt.ProviderReference),
                providerSessionId = NormalizeNullable(attempt.ProviderSessionId),
                failureCode = NormalizeNullable(attempt.FailureCode),
                hasProviderMetadata = !string.IsNullOrWhiteSpace(providerMetadataJson),
            }, JsonOptions);
        }

        private static IReadOnlyList<string> ParseCodes(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<IReadOnlyList<string>>(json)
                    ?.Select(NormalizeCountry)
                    .Where(code => code is not null)
                    .Select(code => code!)
                    .ToArray() ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static string? NormalizeNullable(string? value)
        {
            var normalized = value?.Trim();
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private static string? NormalizeKey(string? value)
        {
            var normalized = NormalizeNullable(value);
            return normalized is null ? null : normalized.ToLowerInvariant();
        }

        private static string? NormalizeCurrency(string? value)
        {
            var normalized = NormalizeNullable(value);
            return normalized is null ? null : normalized.ToUpperInvariant();
        }

        private static string? NormalizeCountry(string? value)
        {
            var normalized = NormalizeNullable(value);
            return normalized is null ? null : normalized.ToUpperInvariant();
        }

        private static ServiceResponse<TPayload> Succeeded<TPayload>(
            string message,
            TPayload payload)
        {
            return new ServiceResponse<TPayload>(true, message)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<TPayload> Failed<TPayload>(
            ServiceResponseType responseType,
            string message)
        {
            return new ServiceResponse<TPayload>(false, message)
            {
                ResponseType = responseType,
            };
        }
    }

    internal sealed record CheckoutPaymentAttemptDraft(
        Guid StoreId,
        Guid CheckoutSessionId,
        string PaymentMethodKey,
        string ProviderKey,
        decimal Amount,
        string CurrencyCode,
        string IdempotencyKey,
        CheckoutPaymentRateSnapshot RateSnapshot);

    internal sealed record CheckoutPaymentLineSnapshot(
        Guid ProductId,
        string Name,
        int Quantity,
        decimal UnitPrice);

    internal sealed record CheckoutPaymentRateSnapshot(
        string? BaseCurrencyCode,
        decimal? BaseTotalAmount,
        decimal? ExchangeRate,
        string? ExchangeRateProviderKey,
        string? ExchangeRateSource,
        DateTimeOffset? ExchangeRateEffectiveAtUtc,
        DateTimeOffset? ExchangeRateExpiresAtUtc);
}
