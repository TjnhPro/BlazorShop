namespace BlazorShop.Storefront.Services
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Web.SharedV2;
    using ProcessCart = BlazorShop.Web.SharedV2.Models.Payment.ProcessCart;

    public sealed class StorefrontCartTokenService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IStorefrontCartClient apiClient;
        private readonly IHostEnvironment environment;

        public StorefrontCartTokenService(IStorefrontCartClient apiClient, IHostEnvironment environment)
        {
            this.apiClient = apiClient;
            this.environment = environment;
        }

        public async Task<StorefrontCartResolution> ResolveAsync(
            HttpContext? httpContext,
            bool createWhenMissing = false,
            bool importLegacyCart = true,
            CancellationToken cancellationToken = default)
        {
            if (httpContext is null)
            {
                return StorefrontCartResolution.Empty();
            }

            var token = httpContext.Request.Cookies[StorefrontCookieNames.CartToken];
            var legacyCookie = httpContext.Request.Cookies[StorefrontCookieNames.Cart];
            var legacyItems = importLegacyCart ? ReadLegacyCart(legacyCookie) : [];
            var shouldImportLegacy = string.IsNullOrWhiteSpace(token) && legacyItems.Count > 0;

            if (string.IsNullOrWhiteSpace(token))
            {
                if (!createWhenMissing && !shouldImportLegacy)
                {
                    if (!string.IsNullOrWhiteSpace(legacyCookie) && legacyItems.Count == 0)
                    {
                        DeleteLegacyCartCookie(httpContext);
                    }

                    return StorefrontCartResolution.Empty();
                }

                var sessionResult = await this.apiClient.CreateOrResumeCartSessionAsync(null, cancellationToken);
                if (!sessionResult.Success || sessionResult.Data is null)
                {
                    return StorefrontCartResolution.Failed(sessionResult.Message);
                }

                token = sessionResult.Data.CartToken;
                WriteCartTokenCookie(httpContext, token, sessionResult.Data.ExpiresAtUtc);
            }
            else if (!string.IsNullOrWhiteSpace(legacyCookie))
            {
                DeleteLegacyCartCookie(httpContext);
            }

            if (shouldImportLegacy)
            {
                var importResult = await ImportLegacyCartAsync(token, legacyItems, cancellationToken);
                if (importResult.Success)
                {
                    DeleteLegacyCartCookie(httpContext);
                }
            }

            var cartResult = await this.apiClient.GetCartAsync(token, cancellationToken);
            if (!cartResult.Success)
            {
                return StorefrontCartResolution.Failed(cartResult.Message, token);
            }

            if (cartResult.Data is not null)
            {
                WriteCartTokenCookie(httpContext, token, cartResult.Data.ExpiresAtUtc);
            }

            return StorefrontCartResolution.Succeeded(token, cartResult.Data);
        }

        public async Task<StorefrontCartMutationResult> AddLineAsync(
            HttpContext httpContext,
            StorefrontCartLineCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            var resolution = await this.ResolveAsync(httpContext, createWhenMissing: true, cancellationToken: cancellationToken);
            if (!resolution.Success || string.IsNullOrWhiteSpace(resolution.CartToken))
            {
                return StorefrontCartMutationResult.Failed(resolution.Message);
            }

            var result = await this.apiClient.AddCartLineAsync(resolution.CartToken, request, cancellationToken);
            return this.ApplyMutationResult(httpContext, resolution.CartToken, result);
        }

        public async Task<StorefrontCartMutationResult> UpdateLineAsync(
            HttpContext httpContext,
            Guid lineId,
            int quantity,
            CancellationToken cancellationToken = default)
        {
            var token = httpContext.Request.Cookies[StorefrontCookieNames.CartToken];
            if (string.IsNullOrWhiteSpace(token))
            {
                return StorefrontCartMutationResult.Failed("Your cart is empty.");
            }

            var result = await this.apiClient.UpdateCartLineAsync(
                token,
                lineId,
                new StorefrontCartLineUpdateRequest { Quantity = quantity },
                cancellationToken);
            return this.ApplyMutationResult(httpContext, token, result);
        }

        public async Task<StorefrontCartMutationResult> RemoveLineAsync(
            HttpContext httpContext,
            Guid lineId,
            CancellationToken cancellationToken = default)
        {
            var token = httpContext.Request.Cookies[StorefrontCookieNames.CartToken];
            if (string.IsNullOrWhiteSpace(token))
            {
                return StorefrontCartMutationResult.Failed("Your cart is empty.");
            }

            var result = await this.apiClient.RemoveCartLineAsync(token, lineId, cancellationToken);
            return this.ApplyMutationResult(httpContext, token, result);
        }

        public async Task<StorefrontCartMutationResult> ClearAsync(
            HttpContext httpContext,
            CancellationToken cancellationToken = default)
        {
            var token = httpContext.Request.Cookies[StorefrontCookieNames.CartToken];
            DeleteLegacyCartCookie(httpContext);

            if (string.IsNullOrWhiteSpace(token))
            {
                DeleteCartTokenCookie(httpContext);
                return StorefrontCartMutationResult.Succeeded(null);
            }

            var result = await this.apiClient.ClearCartAsync(token, cancellationToken);
            if (result.Success)
            {
                DeleteCartTokenCookie(httpContext);
                return StorefrontCartMutationResult.Succeeded(result.Data);
            }

            return StorefrontCartMutationResult.Failed(result.Message);
        }

        public async Task<StorefrontCartMutationResult> MergeCurrentCustomerAsync(
            HttpContext httpContext,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            var token = httpContext.Request.Cookies[StorefrontCookieNames.CartToken];
            if (string.IsNullOrWhiteSpace(token))
            {
                return StorefrontCartMutationResult.Succeeded(null);
            }

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return StorefrontCartMutationResult.Failed("Customer identity was not found.");
            }

            var result = await this.apiClient.MergeCurrentCustomerCartAsync(token, accessToken, cancellationToken);
            return this.ApplyMutationResult(httpContext, token, result);
        }

        private StorefrontCartMutationResult ApplyMutationResult(
            HttpContext httpContext,
            string cartToken,
            StorefrontSubmitResult<StorefrontCartResponse> result)
        {
            if (!result.Success)
            {
                return StorefrontCartMutationResult.Failed(result.Message);
            }

            if (result.Data is not null)
            {
                WriteCartTokenCookie(httpContext, cartToken, result.Data.ExpiresAtUtc);
            }

            DeleteLegacyCartCookie(httpContext);
            return StorefrontCartMutationResult.Succeeded(result.Data);
        }

        private async Task<StorefrontCartImportResult> ImportLegacyCartAsync(
            string cartToken,
            IReadOnlyList<ProcessCart> legacyItems,
            CancellationToken cancellationToken)
        {
            foreach (var item in legacyItems)
            {
                var result = await this.apiClient.AddCartLineAsync(
                    cartToken,
                    new StorefrontCartLineCreateRequest
                    {
                        ProductId = item.ProductId,
                        ProductVariantId = item.ProductVariantId ?? item.VariantId,
                        Quantity = Math.Max(1, item.Quantity),
                        SelectedAttributes = item.SelectedAttributes?
                            .Select(attribute => new SelectedAttributeDto(attribute.Name, attribute.Value))
                            .ToArray(),
                    },
                    cancellationToken);

                if (!result.Success)
                {
                    return new StorefrontCartImportResult(false);
                }
            }

            return new StorefrontCartImportResult(true);
        }

        private static List<ProcessCart> ReadLegacyCart(string? rawCart)
        {
            if (string.IsNullOrWhiteSpace(rawCart))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<List<ProcessCart>>(rawCart, JsonOptions)
                    ?.Where(item => item.ProductId != Guid.Empty && item.Quantity > 0)
                    .ToList()
                    ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private void WriteCartTokenCookie(HttpContext httpContext, string cartToken, DateTimeOffset expiresAtUtc)
        {
            if (string.IsNullOrWhiteSpace(cartToken))
            {
                return;
            }

            var now = DateTimeOffset.UtcNow;
            httpContext.Response.Cookies.Append(
                StorefrontCookieNames.CartToken,
                cartToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !this.environment.IsDevelopment(),
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    Expires = expiresAtUtc,
                    MaxAge = expiresAtUtc > now ? expiresAtUtc - now : TimeSpan.Zero,
                });
        }

        private static void DeleteLegacyCartCookie(HttpContext httpContext)
        {
            httpContext.Response.Cookies.Delete(StorefrontCookieNames.Cart, new CookieOptions { Path = "/" });
        }

        private static void DeleteCartTokenCookie(HttpContext httpContext)
        {
            httpContext.Response.Cookies.Delete(
                StorefrontCookieNames.CartToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                });
        }

        private sealed record StorefrontCartImportResult(bool Success);
    }

    public sealed record StorefrontCartResolution(bool Success, string Message, string? CartToken, StorefrontCartResponse? Cart)
    {
        public static StorefrontCartResolution Empty()
        {
            return new(true, string.Empty, null, null);
        }

        public static StorefrontCartResolution Succeeded(string cartToken, StorefrontCartResponse? cart)
        {
            return new(true, string.Empty, cartToken, cart);
        }

        public static StorefrontCartResolution Failed(string message, string? cartToken = null)
        {
            return new(false, string.IsNullOrWhiteSpace(message) ? "Unable to load cart right now." : message, cartToken, null);
        }
    }

    public sealed record StorefrontCartMutationResult(bool Success, string Message, StorefrontCartResponse? Cart)
    {
        public static StorefrontCartMutationResult Succeeded(StorefrontCartResponse? cart)
        {
            return new(true, string.Empty, cart);
        }

        public static StorefrontCartMutationResult Failed(string message)
        {
            return new(false, string.IsNullOrWhiteSpace(message) ? "Unable to update cart right now." : message, null);
        }
    }
}
