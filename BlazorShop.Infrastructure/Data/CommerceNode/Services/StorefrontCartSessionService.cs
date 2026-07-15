namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Security.Cryptography;
    using System.Text;

    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class StorefrontCartSessionService : IStorefrontCartSessionService
    {
        private const int TokenBytes = 32;

        private readonly CommerceNodeDbContext context;

        public StorefrontCartSessionService(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<ServiceResponse<StorefrontCartSessionCreated>> CreateAsync(
            StorefrontCartSessionCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.StoreId == Guid.Empty)
            {
                return Failed<StorefrontCartSessionCreated>(ServiceResponseType.ValidationError, "Store is required.");
            }

            var now = DateTimeOffset.UtcNow;
            var expiresAt = request.ExpiresAtUtc ?? now.AddDays(30);
            if (expiresAt <= now)
            {
                return Failed<StorefrontCartSessionCreated>(ServiceResponseType.ValidationError, "Cart expiration must be in the future.");
            }

            var token = GenerateToken();
            var session = new CartSession
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = request.StoreId,
                TokenHash = HashToken(token),
                CustomerId = request.CustomerId,
                AppUserId = NormalizeNullable(request.AppUserId),
                State = CartSessionStates.Active,
                Version = 1,
                LastActivityAtUtc = now,
                ExpiresAtUtc = expiresAt,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            this.context.CartSessions.Add(session);
            await this.context.SaveChangesAsync(cancellationToken);

            return new ServiceResponse<StorefrontCartSessionCreated>(true, "Cart session created.", session.Id)
            {
                Payload = new StorefrontCartSessionCreated(
                    session.Id,
                    session.PublicId,
                    session.StoreId,
                    token,
                    session.State,
                    session.Version,
                    session.ExpiresAtUtc,
                    session.CreatedAtUtc),
                ResponseType = ServiceResponseType.Success,
            };
        }

        public async Task<ServiceResponse<StorefrontCartSessionDto>> ResolveAsync(
            Guid storeId,
            string token,
            CancellationToken cancellationToken = default)
        {
            var sessionResult = await this.LoadActiveSessionAsync(storeId, token, cancellationToken);
            return sessionResult.Success
                ? Succeeded("Cart session resolved.", Map(sessionResult.Session!))
                : Failed<StorefrontCartSessionDto>(sessionResult.ResponseType, sessionResult.Message);
        }

        public async Task<ServiceResponse<StorefrontCartSessionDto>> AddOrUpdateLineAsync(
            StorefrontCartLineMutationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.ProductId == Guid.Empty)
            {
                return Failed<StorefrontCartSessionDto>(ServiceResponseType.ValidationError, "Product is required.");
            }

            if (request.Quantity < 1)
            {
                return Failed<StorefrontCartSessionDto>(ServiceResponseType.ValidationError, "Quantity must be at least 1.");
            }

            var sessionResult = await this.LoadActiveSessionAsync(request.StoreId, request.Token, cancellationToken);
            if (!sessionResult.Success)
            {
                return Failed<StorefrontCartSessionDto>(sessionResult.ResponseType, sessionResult.Message);
            }

            var session = sessionResult.Session!;
            var lineKey = BuildLineKey(request);
            var line = session.Lines.FirstOrDefault(candidate => candidate.LineKey == lineKey);
            var now = DateTimeOffset.UtcNow;

            if (line is null)
            {
                line = new CartLine
                {
                    Id = Guid.NewGuid(),
                    CartSessionId = session.Id,
                    ProductId = request.ProductId,
                    ProductVariantId = request.ProductVariantId,
                    LineKey = lineKey,
                    SelectedAttributesJson = NormalizeNullable(request.SelectedAttributesJson),
                    PersonalizationHash = NormalizeNullable(request.PersonalizationHash),
                    PersonalizationJson = NormalizeNullable(request.PersonalizationJson),
                    ArtworkAssetId = request.ArtworkAssetId,
                    ArtworkVersion = request.ArtworkVersion,
                    FulfillmentProviderKey = NormalizeNullable(request.FulfillmentProviderKey),
                    Quantity = request.Quantity,
                    UnitPriceSnapshot = request.UnitPriceSnapshot,
                    CurrencyCodeSnapshot = NormalizeCurrencyCode(request.CurrencyCodeSnapshot),
                    BaseUnitPriceSnapshot = request.BaseUnitPriceSnapshot,
                    BaseCurrencyCodeSnapshot = NormalizeCurrencyCode(request.BaseCurrencyCodeSnapshot),
                    ExchangeRateSnapshot = request.ExchangeRateSnapshot,
                    ExchangeRateProviderKey = NormalizeNullable(request.ExchangeRateProviderKey),
                    ExchangeRateSource = NormalizeNullable(request.ExchangeRateSource),
                    ExchangeRateEffectiveAtUtc = request.ExchangeRateEffectiveAtUtc,
                    ExchangeRateExpiresAtUtc = request.ExchangeRateExpiresAtUtc,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                };

                this.context.CartLines.Add(line);
            }
            else
            {
                line.Quantity += request.Quantity;
                line.UnitPriceSnapshot = request.UnitPriceSnapshot ?? line.UnitPriceSnapshot;
                line.CurrencyCodeSnapshot = NormalizeCurrencyCode(request.CurrencyCodeSnapshot) ?? line.CurrencyCodeSnapshot;
                line.BaseUnitPriceSnapshot = request.BaseUnitPriceSnapshot ?? line.BaseUnitPriceSnapshot;
                line.BaseCurrencyCodeSnapshot = NormalizeCurrencyCode(request.BaseCurrencyCodeSnapshot) ?? line.BaseCurrencyCodeSnapshot;
                line.ExchangeRateSnapshot = request.ExchangeRateSnapshot ?? line.ExchangeRateSnapshot;
                line.ExchangeRateProviderKey = NormalizeNullable(request.ExchangeRateProviderKey) ?? line.ExchangeRateProviderKey;
                line.ExchangeRateSource = NormalizeNullable(request.ExchangeRateSource) ?? line.ExchangeRateSource;
                line.ExchangeRateEffectiveAtUtc = request.ExchangeRateEffectiveAtUtc ?? line.ExchangeRateEffectiveAtUtc;
                line.ExchangeRateExpiresAtUtc = request.ExchangeRateExpiresAtUtc ?? line.ExchangeRateExpiresAtUtc;
                line.UpdatedAtUtc = now;
            }

            Touch(session, now);
            await this.context.SaveChangesAsync(cancellationToken);
            return Succeeded("Cart line saved.", Map(session));
        }

        public async Task<ServiceResponse<StorefrontCartSessionDto>> UpdateLineQuantityAsync(
            Guid storeId,
            string token,
            Guid lineId,
            int quantity,
            CancellationToken cancellationToken = default)
        {
            if (quantity < 1)
            {
                return Failed<StorefrontCartSessionDto>(ServiceResponseType.ValidationError, "Quantity must be at least 1.");
            }

            var sessionResult = await this.LoadActiveSessionAsync(storeId, token, cancellationToken);
            if (!sessionResult.Success)
            {
                return Failed<StorefrontCartSessionDto>(sessionResult.ResponseType, sessionResult.Message);
            }

            var session = sessionResult.Session!;
            var line = session.Lines.FirstOrDefault(candidate => candidate.Id == lineId);
            if (line is null)
            {
                return Failed<StorefrontCartSessionDto>(ServiceResponseType.NotFound, "Cart line was not found.");
            }

            var now = DateTimeOffset.UtcNow;
            line.Quantity = quantity;
            line.UpdatedAtUtc = now;
            Touch(session, now);
            await this.context.SaveChangesAsync(cancellationToken);
            return Succeeded("Cart line updated.", Map(session));
        }

        public async Task<ServiceResponse<StorefrontCartSessionDto>> RemoveLineAsync(
            Guid storeId,
            string token,
            Guid lineId,
            CancellationToken cancellationToken = default)
        {
            var sessionResult = await this.LoadActiveSessionAsync(storeId, token, cancellationToken);
            if (!sessionResult.Success)
            {
                return Failed<StorefrontCartSessionDto>(sessionResult.ResponseType, sessionResult.Message);
            }

            var session = sessionResult.Session!;
            var line = session.Lines.FirstOrDefault(candidate => candidate.Id == lineId);
            if (line is null)
            {
                return Failed<StorefrontCartSessionDto>(ServiceResponseType.NotFound, "Cart line was not found.");
            }

            this.context.CartLines.Remove(line);
            Touch(session, DateTimeOffset.UtcNow);
            await this.context.SaveChangesAsync(cancellationToken);
            return Succeeded("Cart line removed.", Map(session));
        }

        private async Task<CartSessionLoadResult> LoadActiveSessionAsync(
            Guid storeId,
            string token,
            CancellationToken cancellationToken)
        {
            if (storeId == Guid.Empty || string.IsNullOrWhiteSpace(token))
            {
                return CartSessionLoadResult.Failed(ServiceResponseType.ValidationError, "Cart token is required.");
            }

            var tokenHash = HashToken(token);
            var session = await this.context.CartSessions
                .Include(cart => cart.Lines)
                .FirstOrDefaultAsync(
                    cart => cart.StoreId == storeId && cart.TokenHash == tokenHash,
                    cancellationToken);
            if (session is null)
            {
                return CartSessionLoadResult.Failed(ServiceResponseType.NotFound, "Cart session was not found.");
            }

            if (!string.Equals(session.State, CartSessionStates.Active, StringComparison.Ordinal))
            {
                return CartSessionLoadResult.Failed(ServiceResponseType.Conflict, "Cart session is not active.");
            }

            if (session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                session.State = CartSessionStates.Expired;
                session.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await this.context.SaveChangesAsync(cancellationToken);
                return CartSessionLoadResult.Failed(ServiceResponseType.Conflict, "Cart session has expired.");
            }

            return CartSessionLoadResult.Succeeded(session);
        }

        private static void Touch(CartSession session, DateTimeOffset now)
        {
            session.Version++;
            session.LastActivityAtUtc = now;
            session.UpdatedAtUtc = now;
        }

        private static StorefrontCartSessionDto Map(CartSession session)
        {
            return new StorefrontCartSessionDto(
                session.Id,
                session.PublicId,
                session.StoreId,
                session.CustomerId,
                session.AppUserId,
                session.State,
                session.Version,
                session.LastActivityAtUtc,
                session.ExpiresAtUtc,
                session.Lines
                    .OrderBy(line => line.CreatedAtUtc)
                    .ThenBy(line => line.Id)
                    .Select(MapLine)
                    .ToArray());
        }

        private static StorefrontCartLineDto MapLine(CartLine line)
        {
            return new StorefrontCartLineDto(
                line.Id,
                line.ProductId,
                line.ProductVariantId,
                line.LineKey,
                line.SelectedAttributesJson,
                line.PersonalizationHash,
                line.PersonalizationJson,
                line.ArtworkAssetId,
                line.ArtworkVersion,
                line.FulfillmentProviderKey,
                line.Quantity,
                line.UnitPriceSnapshot,
                line.CurrencyCodeSnapshot,
                line.BaseUnitPriceSnapshot,
                line.BaseCurrencyCodeSnapshot,
                line.ExchangeRateSnapshot,
                line.ExchangeRateProviderKey,
                line.ExchangeRateSource,
                line.ExchangeRateEffectiveAtUtc,
                line.ExchangeRateExpiresAtUtc,
                line.CreatedAtUtc,
                line.UpdatedAtUtc);
        }

        private static string BuildLineKey(StorefrontCartLineMutationRequest request)
        {
            var material = string.Join(
                "|",
                request.ProductId.ToString("N"),
                request.ProductVariantId?.ToString("N") ?? string.Empty,
                NormalizeNullable(request.SelectedAttributesJson) ?? string.Empty,
                NormalizeNullable(request.PersonalizationHash) ?? string.Empty,
                request.ArtworkAssetId?.ToString("N") ?? string.Empty,
                request.ArtworkVersion?.ToString() ?? string.Empty,
                NormalizeNullable(request.FulfillmentProviderKey)?.ToLowerInvariant() ?? string.Empty);

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
        }

        private static string GenerateToken()
        {
            return ToBase64Url(RandomNumberGenerator.GetBytes(TokenBytes));
        }

        private static string HashToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
        }

        private static string ToBase64Url(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string? NormalizeCurrencyCode(string? value)
        {
            var normalized = NormalizeNullable(value);
            return normalized?.ToUpperInvariant();
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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

        private sealed record CartSessionLoadResult(
            bool Success,
            CartSession? Session,
            ServiceResponseType ResponseType,
            string Message)
        {
            public static CartSessionLoadResult Succeeded(CartSession session)
            {
                return new CartSessionLoadResult(true, session, ServiceResponseType.Success, "Cart session resolved.");
            }

            public static CartSessionLoadResult Failed(ServiceResponseType responseType, string message)
            {
                return new CartSessionLoadResult(false, null, responseType, message);
            }
        }
    }
}
