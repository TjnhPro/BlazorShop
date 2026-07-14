namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Security.Cryptography;
    using System.Text;

    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class PaymentAttemptService : IPaymentAttemptService
    {
        private static readonly IReadOnlyDictionary<string, ISet<string>> AllowedTransitions =
            new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                [PaymentAttemptStates.Created] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    PaymentAttemptStates.RequiresAction,
                    PaymentAttemptStates.Authorized,
                    PaymentAttemptStates.Captured,
                    PaymentAttemptStates.Failed,
                    PaymentAttemptStates.Cancelled,
                    PaymentAttemptStates.Expired,
                },
                [PaymentAttemptStates.RequiresAction] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    PaymentAttemptStates.Authorized,
                    PaymentAttemptStates.Captured,
                    PaymentAttemptStates.Failed,
                    PaymentAttemptStates.Cancelled,
                    PaymentAttemptStates.Expired,
                },
                [PaymentAttemptStates.Authorized] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    PaymentAttemptStates.Captured,
                    PaymentAttemptStates.Failed,
                    PaymentAttemptStates.Cancelled,
                    PaymentAttemptStates.Expired,
                },
                [PaymentAttemptStates.Captured] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                [PaymentAttemptStates.Failed] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                [PaymentAttemptStates.Cancelled] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                [PaymentAttemptStates.Expired] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            };

        private readonly CommerceNodeDbContext context;

        public PaymentAttemptService(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<ServiceResponse<PaymentAttemptDto>> GetAsync(
            Guid storeId,
            Guid paymentAttemptId,
            CancellationToken cancellationToken = default)
        {
            if (storeId == Guid.Empty)
            {
                return Failed<PaymentAttemptDto>(ServiceResponseType.ValidationError, "Store is required.");
            }

            if (paymentAttemptId == Guid.Empty)
            {
                return Failed<PaymentAttemptDto>(ServiceResponseType.ValidationError, "Payment attempt is required.");
            }

            var attempt = await this.context.PaymentAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    candidate => candidate.StoreId == storeId
                        && candidate.PublicId == paymentAttemptId,
                    cancellationToken);

            return attempt is null
                ? Failed<PaymentAttemptDto>(ServiceResponseType.NotFound, "Payment attempt was not found.")
                : Succeeded("Payment attempt loaded.", ToDto(attempt));
        }

        public async Task<ServiceResponse<PaymentAttemptDto>> CreateAsync(
            CreatePaymentAttemptRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = ValidateCreateRequest(request);
            if (validation is not null)
            {
                return Failed<PaymentAttemptDto>(ServiceResponseType.ValidationError, validation);
            }

            var idempotencyKey = NormalizeRequired(request.IdempotencyKey)!;
            var existing = await this.FindAttemptByIdempotencyKeyAsync(request.StoreId, idempotencyKey, cancellationToken);
            if (existing is not null)
            {
                return Succeeded("Payment attempt already exists.", existing);
            }

            var now = DateTimeOffset.UtcNow;
            var attempt = new PaymentAttempt
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = request.StoreId,
                CheckoutSessionId = request.CheckoutSessionId,
                OrderId = request.OrderId,
                PaymentMethodKey = NormalizeKey(request.PaymentMethodKey)!,
                ProviderKey = NormalizeKey(request.ProviderKey)!,
                State = PaymentAttemptStates.Created,
                Amount = request.Amount,
                CurrencyCode = NormalizeCurrency(request.CurrencyCode)!,
                IdempotencyKey = idempotencyKey,
                MetadataJson = NormalizeNullable(request.MetadataJson),
                ExpiresAtUtc = request.ExpiresAtUtc ?? now.AddMinutes(30),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            this.context.PaymentAttempts.Add(attempt);
            try
            {
                await this.context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                var duplicate = await this.FindAttemptByIdempotencyKeyAsync(request.StoreId, idempotencyKey, cancellationToken);
                if (duplicate is not null)
                {
                    return Succeeded("Payment attempt already exists.", duplicate);
                }

                throw;
            }

            return Succeeded("Payment attempt created.", ToDto(attempt));
        }

        public async Task<ServiceResponse<PaymentAttemptDto>> TransitionAsync(
            TransitionPaymentAttemptRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.StoreId == Guid.Empty)
            {
                return Failed<PaymentAttemptDto>(ServiceResponseType.ValidationError, "Store is required.");
            }

            if (request.PaymentAttemptId == Guid.Empty)
            {
                return Failed<PaymentAttemptDto>(ServiceResponseType.ValidationError, "Payment attempt is required.");
            }

            var newState = NormalizeKey(request.NewState);
            if (newState is null || !AllowedTransitions.ContainsKey(newState))
            {
                return Failed<PaymentAttemptDto>(ServiceResponseType.ValidationError, "Payment attempt state is invalid.");
            }

            var attempt = await this.context.PaymentAttempts
                .FirstOrDefaultAsync(
                    candidate => candidate.StoreId == request.StoreId
                        && candidate.PublicId == request.PaymentAttemptId,
                    cancellationToken);
            if (attempt is null)
            {
                return Failed<PaymentAttemptDto>(ServiceResponseType.NotFound, "Payment attempt was not found.");
            }

            if (string.Equals(attempt.State, newState, StringComparison.OrdinalIgnoreCase))
            {
                return Succeeded("Payment attempt already has the requested state.", ToDto(attempt));
            }

            if (!AllowedTransitions.TryGetValue(attempt.State, out var nextStates) || !nextStates.Contains(newState))
            {
                return Failed<PaymentAttemptDto>(ServiceResponseType.Conflict, "Payment attempt state transition is not allowed.");
            }

            attempt.State = newState;
            attempt.ProviderReference = NormalizeNullable(request.ProviderReference) ?? attempt.ProviderReference;
            attempt.ProviderSessionId = NormalizeNullable(request.ProviderSessionId) ?? attempt.ProviderSessionId;
            attempt.NextActionType = NormalizeNullable(request.NextActionType);
            attempt.NextActionUrl = NormalizeNullable(request.NextActionUrl);
            attempt.FailureCode = NormalizeNullable(request.FailureCode);
            attempt.FailureMessage = NormalizeNullable(request.FailureMessage);
            attempt.MetadataJson = NormalizeNullable(request.MetadataJson) ?? attempt.MetadataJson;
            attempt.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken);
            return Succeeded("Payment attempt updated.", ToDto(attempt));
        }

        public async Task<ServiceResponse<PaymentProviderEventDto>> RecordProviderEventAsync(
            RecordPaymentProviderEventRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = ValidateProviderEventRequest(request);
            if (validation is not null)
            {
                return Failed<PaymentProviderEventDto>(ServiceResponseType.ValidationError, validation);
            }

            var providerKey = NormalizeKey(request.ProviderKey)!;
            var eventId = NormalizeNullable(request.EventId);
            Guid? paymentAttemptInternalId = null;
            if (request.PaymentAttemptId.HasValue)
            {
                var attempt = await this.context.PaymentAttempts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        candidate => candidate.StoreId == request.StoreId
                            && candidate.PublicId == request.PaymentAttemptId.Value,
                        cancellationToken);
                if (attempt is null)
                {
                    return Failed<PaymentProviderEventDto>(ServiceResponseType.NotFound, "Payment attempt was not found.");
                }

                paymentAttemptInternalId = attempt.Id;
            }

            if (eventId is not null)
            {
                var existing = await this.context.PaymentProviderEvents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        candidate => candidate.ProviderKey == providerKey
                            && candidate.EventId == eventId,
                        cancellationToken);
                if (existing is not null)
                {
                    return Succeeded("Payment provider event already recorded.", ToDto(existing, isDuplicate: true));
                }
            }

            var paymentEvent = new PaymentProviderEvent
            {
                Id = Guid.NewGuid(),
                StoreId = request.StoreId,
                PaymentAttemptId = paymentAttemptInternalId,
                ProviderKey = providerKey,
                EventId = eventId,
                EventType = NormalizeRequired(request.EventType)!,
                PayloadHash = ComputeSha256(request.PayloadJson),
                PayloadJson = request.PayloadJson,
                ProcessedAtUtc = request.ProcessedAtUtc,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };

            this.context.PaymentProviderEvents.Add(paymentEvent);
            try
            {
                await this.context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                if (eventId is not null)
                {
                    var duplicate = await this.context.PaymentProviderEvents
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            candidate => candidate.ProviderKey == providerKey
                                && candidate.EventId == eventId,
                            cancellationToken);
                    if (duplicate is not null)
                    {
                        return Succeeded("Payment provider event already recorded.", ToDto(duplicate, isDuplicate: true));
                    }
                }

                throw;
            }

            return Succeeded("Payment provider event recorded.", ToDto(paymentEvent, isDuplicate: false));
        }

        private async Task<PaymentAttemptDto?> FindAttemptByIdempotencyKeyAsync(
            Guid storeId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            var attempt = await this.context.PaymentAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    candidate => candidate.StoreId == storeId
                        && candidate.IdempotencyKey == idempotencyKey,
                    cancellationToken);

            return attempt is null ? null : ToDto(attempt);
        }

        private static string? ValidateCreateRequest(CreatePaymentAttemptRequest request)
        {
            if (request.StoreId == Guid.Empty)
            {
                return "Store is required.";
            }

            if (request.CheckoutSessionId == Guid.Empty)
            {
                return "Checkout session is required.";
            }

            if (NormalizeKey(request.PaymentMethodKey) is null)
            {
                return "Payment method is required.";
            }

            if (NormalizeKey(request.ProviderKey) is null)
            {
                return "Payment provider is required.";
            }

            if (request.Amount <= 0m)
            {
                return "Payment amount must be greater than zero.";
            }

            if (NormalizeCurrency(request.CurrencyCode) is null)
            {
                return "Currency code is required.";
            }

            if (NormalizeRequired(request.IdempotencyKey) is null)
            {
                return "Idempotency key is required.";
            }

            return null;
        }

        private static string? ValidateProviderEventRequest(RecordPaymentProviderEventRequest request)
        {
            if (request.StoreId == Guid.Empty)
            {
                return "Store is required.";
            }

            if (NormalizeKey(request.ProviderKey) is null)
            {
                return "Payment provider is required.";
            }

            if (NormalizeRequired(request.EventType) is null)
            {
                return "Provider event type is required.";
            }

            if (NormalizeNullable(request.PayloadJson) is null)
            {
                return "Provider event payload is required.";
            }

            return null;
        }

        private static string? NormalizeRequired(string? value)
        {
            var normalized = NormalizeNullable(value);
            return normalized is { Length: <= 128 } ? normalized : null;
        }

        private static string? NormalizeKey(string? value)
        {
            var normalized = NormalizeNullable(value);
            return normalized is { Length: <= 64 } ? normalized.ToLowerInvariant() : null;
        }

        private static string? NormalizeCurrency(string? value)
        {
            var normalized = NormalizeNullable(value);
            return normalized is { Length: 3 } ? normalized.ToUpperInvariant() : null;
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string ComputeSha256(string value)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static PaymentAttemptDto ToDto(PaymentAttempt attempt)
        {
            return new PaymentAttemptDto(
                attempt.PublicId,
                attempt.StoreId,
                attempt.CheckoutSessionId,
                attempt.OrderId,
                attempt.PaymentMethodKey,
                attempt.ProviderKey,
                attempt.State,
                attempt.Amount,
                attempt.CurrencyCode,
                attempt.IdempotencyKey,
                attempt.ProviderReference,
                attempt.ProviderSessionId,
                attempt.NextActionType,
                attempt.NextActionUrl,
                attempt.FailureCode,
                attempt.FailureMessage,
                attempt.ExpiresAtUtc,
                attempt.CreatedAtUtc,
                attempt.UpdatedAtUtc);
        }

        private static PaymentProviderEventDto ToDto(PaymentProviderEvent paymentEvent, bool isDuplicate)
        {
            return new PaymentProviderEventDto(
                paymentEvent.Id,
                paymentEvent.StoreId,
                paymentEvent.PaymentAttemptId,
                paymentEvent.ProviderKey,
                paymentEvent.EventId,
                paymentEvent.EventType,
                paymentEvent.PayloadHash,
                isDuplicate,
                paymentEvent.ProcessedAtUtc,
                paymentEvent.CreatedAtUtc);
        }

        private static ServiceResponse<TPayload> Succeeded<TPayload>(string message, TPayload payload)
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
}
