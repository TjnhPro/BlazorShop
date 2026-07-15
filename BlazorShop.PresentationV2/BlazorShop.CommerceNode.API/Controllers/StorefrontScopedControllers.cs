namespace BlazorShop.CommerceNode.API.Controllers
{
    using System.Security.Claims;

    using ApplicationStorefrontCheckoutResult = BlazorShop.Application.DTOs.Payment.StorefrontCheckoutResult;
    using ApplicationStorefrontCheckoutPreviewResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutPreviewResult;
    using ApplicationStorefrontPlaceOrderResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontPlaceOrderResult;
    using IStorefrontCheckoutService = BlazorShop.Application.CommerceNode.Checkout.IStorefrontCheckoutService;

    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.Settings;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Discovery;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Application.Options;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Services.Contracts.Authentication;
    using BlazorShop.Application.Services.Contracts.Payment;
    using BlazorShop.CommerceNode.API.Configuration;
    using BlazorShop.CommerceNode.API.Contracts.Storefront;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/auth")]
    public sealed class StorefrontScopedAuthController : StorefrontApiControllerBase
    {
        private readonly IAuthenticationService authenticationService;
        private readonly CommerceNodeRuntimeOptions runtimeOptions;

        public StorefrontScopedAuthController(
            IAuthenticationService authenticationService,
            IOptions<CommerceNodeRuntimeOptions> runtimeOptions)
        {
            this.authenticationService = authenticationService;
            this.runtimeOptions = runtimeOptions.Value;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] StorefrontRegisterRequest user)
        {
            var result = await this.authenticationService.CreateUser(user.ToApplicationRequest());
            return this.FromServiceResponse(
                result,
                payload => new StorefrontRegistrationResponse(
                    result.Id ?? (payload is Guid userId ? userId : Guid.Empty)));
        }

        [HttpPost("login")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Login([FromBody] StorefrontLoginRequest user)
        {
            var result = await this.authenticationService.LoginUser(
                user.ToApplicationRequest(),
                this.GetClientIpAddress(),
                this.GetUserAgent());
            if (!result.Success)
            {
                return this.Error(
                    StatusCodes.Status400BadRequest,
                    "validation_error",
                    NormalizeLoginMessage(result.Message));
            }

            if (string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                return this.StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new CommerceNodeApiErrorResponse(
                        false,
                        "internal_error",
                        "Error occurred in login.",
                        this.HttpContext.TraceIdentifier));
            }

            this.AppendRefreshTokenCookie(result.RefreshToken);
            return this.Ok(CommerceNodeApiResponse<StorefrontTokenResponse>.Succeeded(
                SanitizeLoginResponse(result).ToStorefrontTokenContract(),
                NormalizeLoginMessage(result.Message)));
        }

        [HttpPost("refresh-token")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RefreshToken()
        {
            if (!this.Request.Cookies.TryGetValue(this.GetRefreshTokenCookieName(), out var refreshToken)
                || string.IsNullOrWhiteSpace(refreshToken))
            {
                this.DeleteRefreshTokenCookie();
                return this.Error(StatusCodes.Status401Unauthorized, "auth.refresh_cookie_missing", "Refresh token cookie was not found.");
            }

            var result = await this.authenticationService.ReviveToken(
                refreshToken,
                this.GetClientIpAddress(),
                this.GetUserAgent());

            if (!result.Success)
            {
                this.DeleteRefreshTokenCookie();
                return this.Error(
                    StatusCodes.Status401Unauthorized,
                    "auth.invalid_refresh_token",
                    NormalizeLoginMessage(result.Message));
            }

            if (string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                this.DeleteRefreshTokenCookie();
                return this.StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new CommerceNodeApiErrorResponse(
                        false,
                        "internal_error",
                        "Error occurred in login.",
                        this.HttpContext.TraceIdentifier));
            }

            this.AppendRefreshTokenCookie(result.RefreshToken);
            return this.Ok(CommerceNodeApiResponse<StorefrontTokenResponse>.Succeeded(
                SanitizeLoginResponse(result).ToStorefrontTokenContract(),
                NormalizeLoginMessage(result.Message)));
        }

        [HttpPost("logout")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Logout()
        {
            this.Request.Cookies.TryGetValue(this.GetRefreshTokenCookieName(), out var refreshToken);
            var result = await this.authenticationService.Logout(refreshToken ?? string.Empty, this.GetClientIpAddress());

            this.DeleteRefreshTokenCookie();
            return this.FromServiceResponse(result);
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] StorefrontChangePasswordRequest dto)
        {
            var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Error(StatusCodes.Status401Unauthorized, "unauthorized", "Customer identity was not found.");
            }

            var result = await this.authenticationService.ChangePassword(dto.ToApplicationRequest(), userId);
            return this.FromServiceResponse(result);
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var result = await this.authenticationService.ConfirmEmail(userId, token);
            return this.FromServiceResponse(result);
        }

        [HttpPost("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] StorefrontUpdateProfileRequest dto)
        {
            var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Error(StatusCodes.Status401Unauthorized, "unauthorized", "Customer identity was not found.");
            }

            var result = await this.authenticationService.UpdateProfile(userId, dto.ToApplicationRequest());
            return this.FromServiceResponse(result);
        }

        private void AppendRefreshTokenCookie(string refreshToken)
        {
            this.Response.Cookies.Append(this.GetRefreshTokenCookieName(), refreshToken, this.CreateRefreshTokenCookieOptions());
        }

        private void DeleteRefreshTokenCookie()
        {
            this.Response.Cookies.Delete(this.GetRefreshTokenCookieName(), this.CreateRefreshTokenCookieOptions());
        }

        private CookieOptions CreateRefreshTokenCookieOptions()
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = this.GetRefreshTokenCookieSameSiteMode(),
                IsEssential = true,
                Path = "/",
                MaxAge = TimeSpan.FromDays(this.GetRefreshTokenLifetimeDays()),
            };
        }

        private string GetRefreshTokenCookieName()
        {
            return string.IsNullOrWhiteSpace(this.runtimeOptions.Security.RefreshTokenCookieName)
                ? "__Host-blazorshop-refresh"
                : this.runtimeOptions.Security.RefreshTokenCookieName;
        }

        private SameSiteMode GetRefreshTokenCookieSameSiteMode()
        {
            return Enum.TryParse<SameSiteMode>(
                this.runtimeOptions.Security.RefreshTokenCookieSameSite,
                ignoreCase: true,
                out var sameSiteMode)
                ? sameSiteMode
                : SameSiteMode.Strict;
        }

        private int GetRefreshTokenLifetimeDays()
        {
            return this.runtimeOptions.Security.RefreshTokenLifetimeDays > 0
                ? this.runtimeOptions.Security.RefreshTokenLifetimeDays
                : 14;
        }

        private string? GetClientIpAddress()
        {
            return this.HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        private string? GetUserAgent()
        {
            return this.Request.Headers.UserAgent.ToString();
        }

        private static LoginResponse SanitizeLoginResponse(LoginResponse response)
        {
            return response with { RefreshToken = string.Empty };
        }

        private static string NormalizeLoginMessage(string? message)
        {
            return string.IsNullOrWhiteSpace(message) ? "Authentication request completed." : message;
        }
    }

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/catalog")]
    public sealed class StorefrontScopedCatalogController : StorefrontApiControllerBase
    {
        private readonly IPublicCatalogService publicCatalogService;
        private readonly ICommerceStoreContext storeContext;
        private readonly IStorefrontWorkingCurrencyResolver workingCurrencyResolver;
        private readonly IMoneyConversionService moneyConversionService;

        public StorefrontScopedCatalogController(
            IPublicCatalogService publicCatalogService,
            ICommerceStoreContext storeContext,
            IStorefrontWorkingCurrencyResolver workingCurrencyResolver,
            IMoneyConversionService moneyConversionService)
        {
            this.publicCatalogService = publicCatalogService;
            this.storeContext = storeContext;
            this.workingCurrencyResolver = workingCurrencyResolver;
            this.moneyConversionService = moneyConversionService;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await this.publicCatalogService.GetPublishedCategoriesAsync();
            return this.Success(
                categories.Select(category => category.ToStorefrontContract()).ToArray(),
                "Published categories loaded.");
        }

        [HttpGet("categories/tree")]
        public async Task<IActionResult> GetCategoryTree()
        {
            var categories = await this.publicCatalogService.GetPublishedCategoryTreeAsync();
            return this.Success(
                categories.Select(category => category.ToStorefrontContract()).ToArray(),
                "Published category tree loaded.");
        }

        [HttpGet("categories/{id:guid}")]
        public async Task<IActionResult> GetCategoryById(Guid id)
        {
            var category = await this.publicCatalogService.GetPublishedCategoryByIdAsync(id);
            return category is null
                ? this.Failure<StorefrontCategoryResponse>(ServiceResponseType.NotFound, "Published category was not found.")
                : this.Success(category.ToStorefrontContract(), "Published category loaded.");
        }

        [HttpGet("categories/slug/{slug}")]
        public async Task<IActionResult> GetCategoryBySlug(
            string slug,
            [FromQuery] string? currencyCode,
            CancellationToken cancellationToken)
        {
            var categoryPage = await this.publicCatalogService.GetPublishedCategoryPageBySlugAsync(slug);
            var displayCurrency = await this.ResolveDisplayCurrencyAsync(currencyCode, cancellationToken);
            var mappedProducts = categoryPage is null
                ? []
                : await Task.WhenAll(categoryPage.Products.Select(product => this.ToDisplayCatalogProductContractAsync(product, displayCurrency, cancellationToken)));
            return categoryPage is null
                ? this.Failure<StorefrontCategoryPageResponse>(ServiceResponseType.NotFound, "Published category was not found.")
                : this.Success(
                    new StorefrontCategoryPageResponse(categoryPage.Category.ToStorefrontContract(), mappedProducts),
                    "Published category page loaded.");
        }

        [HttpGet("categories/{categoryId:guid}/products")]
        public async Task<IActionResult> GetProductsByCategory(
            Guid categoryId,
            [FromQuery] string? currencyCode,
            CancellationToken cancellationToken)
        {
            var category = await this.publicCatalogService.GetPublishedCategoryByIdAsync(categoryId);
            if (category is null)
            {
                return this.Failure<IReadOnlyList<StorefrontCatalogProductResponse>>(
                    ServiceResponseType.NotFound,
                    "Published category was not found.");
            }

            var products = await this.publicCatalogService.GetPublishedProductsByCategoryAsync(categoryId);
            var displayCurrency = await this.ResolveDisplayCurrencyAsync(currencyCode, cancellationToken);
            var mappedProducts = await Task.WhenAll(products.Select(product => this.ToDisplayCatalogProductContractAsync(product, displayCurrency, cancellationToken)));
            return this.Success(
                mappedProducts,
                "Published category products loaded.");
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts(
            [FromQuery] StorefrontProductCatalogQuery query,
            CancellationToken cancellationToken)
        {
            var products = await this.publicCatalogService.GetPublishedCatalogPageAsync(query.ToApplicationQuery());
            var displayCurrency = await this.ResolveDisplayCurrencyAsync(query.CurrencyCode, cancellationToken);
            var mappedProducts = await Task.WhenAll(products.Items.Select(product => this.ToDisplayCatalogProductContractAsync(product, displayCurrency, cancellationToken)));
            return this.Success(
                new StorefrontPagedResponse<StorefrontCatalogProductResponse>(
                    mappedProducts,
                    products.PageNumber,
                    products.PageSize,
                    products.TotalCount,
                    products.TotalPages),
                "Published products loaded.");
        }

        [HttpGet("products/{id:guid}")]
        public async Task<IActionResult> GetProductById(
            Guid id,
            [FromQuery] string? currencyCode,
            CancellationToken cancellationToken)
        {
            var product = await this.publicCatalogService.GetPublishedProductByIdAsync(id);
            var displayCurrency = await this.ResolveDisplayCurrencyAsync(currencyCode, cancellationToken);
            return product is null
                ? this.Failure<StorefrontProductResponse>(ServiceResponseType.NotFound, "Published product was not found.")
                : this.Success(await this.ToDisplayProductContractAsync(product, displayCurrency, cancellationToken), "Published product loaded.");
        }

        [HttpGet("products/slug/{slug}")]
        public async Task<IActionResult> GetProductBySlug(
            string slug,
            [FromQuery] string? currencyCode,
            CancellationToken cancellationToken)
        {
            var product = await this.publicCatalogService.GetPublishedProductBySlugAsync(slug);
            var displayCurrency = await this.ResolveDisplayCurrencyAsync(currencyCode, cancellationToken);
            return product is null
                ? this.Failure<StorefrontProductResponse>(ServiceResponseType.NotFound, "Published product was not found.")
                : this.Success(await this.ToDisplayProductContractAsync(product, displayCurrency, cancellationToken), "Published product loaded.");
        }

        [HttpGet("sitemap")]
        public async Task<IActionResult> GetSitemap()
        {
            var sitemap = await this.publicCatalogService.GetPublishedSitemapAsync();
            return this.Success<GetPublicCatalogSitemap>(sitemap, "Published catalog sitemap loaded.");
        }

        private async Task<StorefrontDisplayCurrency?> ResolveDisplayCurrencyAsync(
            string? requestedCurrencyCode,
            CancellationToken cancellationToken)
        {
            var storeIdResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeIdResult.Success)
            {
                return null;
            }

            var resolution = await this.workingCurrencyResolver.ResolveAsync(
                storeIdResult.Payload,
                requestedCurrencyCode,
                cancellationToken);

            return new StorefrontDisplayCurrency(
                storeIdResult.Payload,
                resolution.CurrencyCode,
                resolution.BaseCurrencyCode);
        }

        private async Task<StorefrontCatalogProductResponse> ToDisplayCatalogProductContractAsync(
            BlazorShop.Application.DTOs.Product.GetCatalogProduct product,
            StorefrontDisplayCurrency? displayCurrency,
            CancellationToken cancellationToken)
        {
            var displayMoney = displayCurrency is null
                ? null
                : await this.ResolveDisplayMoneyAsync(product.Price, product.ComparePrice, displayCurrency, cancellationToken);

            return product.ToStorefrontContract(displayMoney);
        }

        private async Task<StorefrontProductResponse> ToDisplayProductContractAsync(
            BlazorShop.Application.DTOs.Product.GetProduct product,
            StorefrontDisplayCurrency? displayCurrency,
            CancellationToken cancellationToken)
        {
            var displayMoney = displayCurrency is null
                ? null
                : await this.ResolveDisplayMoneyAsync(product.Price, product.ComparePrice, displayCurrency, cancellationToken);

            if (displayCurrency is null)
            {
                return product.ToStorefrontContract(displayMoney);
            }

            var variantDisplayMoney = new Dictionary<Guid, StorefrontDisplayMoney>();
            foreach (var variant in product.Variants)
            {
                var effectivePrice = variant.EffectivePrice > 0
                    ? variant.EffectivePrice
                    : variant.Price ?? product.Price;
                variantDisplayMoney[variant.Id] = await this.ResolveDisplayMoneyAsync(
                    effectivePrice,
                    comparePrice: null,
                    displayCurrency,
                    cancellationToken);
            }

            return product.ToStorefrontContract(
                displayMoney,
                variant => variant.ToStorefrontContract(
                    variantDisplayMoney.TryGetValue(variant.Id, out var variantMoney)
                        ? variantMoney
                        : null));
        }

        private async Task<StorefrontDisplayMoney> ResolveDisplayMoneyAsync(
            decimal price,
            decimal? comparePrice,
            StorefrontDisplayCurrency displayCurrency,
            CancellationToken cancellationToken)
        {
            if (string.Equals(displayCurrency.CurrencyCode, displayCurrency.BaseCurrencyCode, StringComparison.Ordinal))
            {
                return new StorefrontDisplayMoney(price, comparePrice, displayCurrency.BaseCurrencyCode);
            }

            var priceResult = await this.moneyConversionService.ConvertFromBaseAsync(
                displayCurrency.StoreId,
                price,
                displayCurrency.CurrencyCode,
                cancellationToken);
            if (!priceResult.Success || priceResult.Payload is null)
            {
                return new StorefrontDisplayMoney(price, comparePrice, displayCurrency.BaseCurrencyCode);
            }

            decimal? convertedComparePrice = null;
            if (comparePrice.HasValue)
            {
                var compareResult = await this.moneyConversionService.ConvertFromBaseAsync(
                    displayCurrency.StoreId,
                    comparePrice.Value,
                    displayCurrency.CurrencyCode,
                    cancellationToken);
                convertedComparePrice = compareResult.Success && compareResult.Payload is not null
                    ? compareResult.Payload.ConvertedAmount
                    : comparePrice;
            }

            return new StorefrontDisplayMoney(
                priceResult.Payload.ConvertedAmount,
                convertedComparePrice,
                priceResult.Payload.TargetCurrencyCode);
        }

        private sealed record StorefrontDisplayCurrency(
            Guid StoreId,
            string CurrencyCode,
            string BaseCurrencyCode);
    }

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/cart")]
    [Authorize]
    public sealed class StorefrontScopedCartController : StorefrontApiControllerBase
    {
        private const string CartTokenHeaderName = "X-Cart-Token";

        private readonly ICartService cartService;
        private readonly IStorefrontCartService storefrontCartService;
        private readonly ICommerceStoreContext storeContext;

        public StorefrontScopedCartController(
            ICartService cartService,
            IStorefrontCartService storefrontCartService,
            ICommerceStoreContext storeContext)
        {
            this.cartService = cartService;
            this.storefrontCartService = storefrontCartService;
            this.storeContext = storeContext;
        }

        [HttpPost("session")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateSession(
            [FromBody] StorefrontCreateCartSessionRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.storefrontCartService.CreateOrResumeAsync(
                new StorefrontCartCreateOrResumeRequest(storeId.Value, request.CartToken),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload?.ToSessionContract(request.CartToken));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get(
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.storefrontCartService.GetAsync(storeId.Value, cartToken, cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload?.ToStorefrontContract());
        }

        [HttpPost("lines")]
        [AllowAnonymous]
        public async Task<IActionResult> AddLine(
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            [FromBody] StorefrontCartLineCreateRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.storefrontCartService.AddLineAsync(
                request.ToApplicationRequest(storeId.Value, cartToken),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload?.ToStorefrontContract());
        }

        [HttpPut("lines/{lineId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateLine(
            Guid lineId,
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            [FromBody] StorefrontCartLineUpdateRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.storefrontCartService.UpdateLineAsync(
                request.ToApplicationRequest(storeId.Value, cartToken, lineId),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload?.ToStorefrontContract());
        }

        [HttpDelete("lines/{lineId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveLine(
            Guid lineId,
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.storefrontCartService.RemoveLineAsync(
                storeId.Value,
                cartToken,
                lineId,
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload?.ToStorefrontContract());
        }

        [HttpDelete]
        [AllowAnonymous]
        public async Task<IActionResult> Clear(
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.storefrontCartService.ClearAsync(
                storeId.Value,
                cartToken,
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload?.ToStorefrontContract());
        }

        [HttpPost("validate")]
        [AllowAnonymous]
        public async Task<IActionResult> Validate(
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            [FromBody] StorefrontCartValidateRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.storefrontCartService.ValidateAsync(storeId.Value, cartToken, cancellationToken);
            if (result.Success
                && result.Payload is not null
                && request.ExpectedVersion.HasValue
                && request.ExpectedVersion.Value != result.Payload.Version)
            {
                return this.Error(StatusCodes.Status409Conflict, "cart.version_stale", "Cart version is stale.");
            }

            return this.FromServiceResponse(
                result,
                payload => payload?.ToStorefrontContract());
        }

        [HttpPost("save-checkout")]
        public async Task<IActionResult> SaveCheckout([FromBody] IReadOnlyList<StorefrontOrderItemRequest> orderItems)
        {
            var userId = this.GetCurrentCustomerId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return this.Error(StatusCodes.Status401Unauthorized, "unauthorized", "Customer identity was not found.");
            }

            var result = await this.cartService.SaveCheckoutHistoryAsync(
                userId,
                orderItems.Select(orderItem => orderItem.ToCreateOrderItem()).ToArray());
            return this.FromServiceResponse(result);
        }

        private string? GetCurrentCustomerId()
        {
            return this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private async Task<Guid?> ResolveStoreIdAsync(CancellationToken cancellationToken)
        {
            var result = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            return result.Success ? result.Payload : null;
        }
    }

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/currency")]
    public sealed class StorefrontScopedCurrencyController : StorefrontApiControllerBase
    {
        private const string WorkingCurrencyCookieName = "blazorshop-working-currency";

        private readonly ICommerceStoreContext storeContext;
        private readonly IStorefrontWorkingCurrencyResolver workingCurrencyResolver;

        public StorefrontScopedCurrencyController(
            ICommerceStoreContext storeContext,
            IStorefrontWorkingCurrencyResolver workingCurrencyResolver)
        {
            this.storeContext = storeContext;
            this.workingCurrencyResolver = workingCurrencyResolver;
        }

        [HttpPost("preference")]
        [AllowAnonymous]
        public async Task<IActionResult> SetPreference(
            [FromBody] StorefrontCurrencyPreferenceRequest request,
            CancellationToken cancellationToken)
        {
            var storeIdResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeIdResult.Success)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var resolution = await this.workingCurrencyResolver.ResolveAsync(
                storeIdResult.Payload,
                request.CurrencyCode,
                cancellationToken);

            if (resolution.RequestedCurrencySupported && resolution.CheckoutCurrencyEnabled)
            {
                this.Response.Cookies.Append(
                    WorkingCurrencyCookieName,
                    resolution.CurrencyCode,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = this.Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        Path = "/",
                        MaxAge = TimeSpan.FromDays(30),
                    });
            }
            else
            {
                this.Response.Cookies.Delete(WorkingCurrencyCookieName, new CookieOptions { Path = "/" });
            }

            return this.Success(resolution.ToStorefrontContract(), "Currency preference resolved.");
        }
    }

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/checkout")]
    [Authorize]
    public sealed class StorefrontScopedCheckoutController : StorefrontApiControllerBase
    {
        private const string CartTokenHeaderName = "X-Cart-Token";

        private readonly IStorefrontCheckoutService checkoutService;
        private readonly ICommerceStoreContext storeContext;

        public StorefrontScopedCheckoutController(
            IStorefrontCheckoutService checkoutService,
            ICommerceStoreContext storeContext)
        {
            this.checkoutService = checkoutService;
            this.storeContext = storeContext;
        }

        [HttpPost("preview")]
        [AllowAnonymous]
        public async Task<IActionResult> Preview(
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            [FromBody] StorefrontCheckoutPreviewRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.checkoutService.PreviewAsync(
                request.ToApplicationRequest(storeId.Value, cartToken),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload is ApplicationStorefrontCheckoutPreviewResult preview
                    ? preview.ToStorefrontContract()
                    : null);
        }

        [HttpPost("place-order")]
        [AllowAnonymous]
        public async Task<IActionResult> PlaceOrder(
            [FromBody] StorefrontPlaceOrderRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.checkoutService.PlaceOrderAsync(
                request.ToApplicationRequest(storeId.Value),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload is ApplicationStorefrontPlaceOrderResult order
                    ? order.ToStorefrontContract()
                    : null);
        }

        private async Task<Guid?> ResolveStoreIdAsync(CancellationToken cancellationToken)
        {
            var result = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            return result.Success ? result.Payload : null;
        }
    }

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/newsletter")]
    public sealed class StorefrontScopedNewsletterController : StorefrontApiControllerBase
    {
        private readonly INewsletterService newsletterService;

        public StorefrontScopedNewsletterController(INewsletterService newsletterService)
        {
            this.newsletterService = newsletterService;
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] StorefrontNewsletterSubscribeRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Email))
            {
                return this.Failure<object>(ServiceResponseType.ValidationError, "Email is required.");
            }

            var result = await this.newsletterService.SubscribeAsync(request.Email);
            return this.FromServiceResponse(result);
        }

    }

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/orders")]
    [Authorize]
    public sealed class StorefrontScopedOrdersController : StorefrontApiControllerBase
    {
        private readonly ICartService cartService;
        private readonly IOrderQueryService orderQueryService;

        public StorefrontScopedOrdersController(
            ICartService cartService,
            IOrderQueryService orderQueryService)
        {
            this.cartService = cartService;
            this.orderQueryService = orderQueryService;
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmOrder([FromBody] IReadOnlyList<StorefrontCartItemRequest> carts)
        {
            var userId = this.GetCurrentCustomerId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return this.Error(StatusCodes.Status401Unauthorized, "unauthorized", "Customer identity was not found.");
            }

            var result = await this.cartService.ConfirmOrderAsync(
                carts.Select(cart => cart.ToProcessCart()).ToArray(),
                userId);
            return this.FromServiceResponse(
                result,
                payload => payload is ApplicationStorefrontCheckoutResult checkoutResult
                    ? checkoutResult.ToStorefrontContract()
                    : null);
        }

        [HttpGet("current-user")]
        public async Task<IActionResult> GetCurrentUserOrders()
        {
            var userId = this.GetCurrentCustomerId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return this.Error(StatusCodes.Status401Unauthorized, "unauthorized", "Customer identity was not found.");
            }

            var orders = (await this.orderQueryService.GetOrdersForUserAsync(userId)).ToArray();
            return this.Success(
                orders.Select(order => order.ToStorefrontContract()).ToArray(),
                "Current customer orders loaded.");
        }

        [HttpGet("current-user/items")]
        public async Task<IActionResult> GetCurrentUserOrderItems()
        {
            var userId = this.GetCurrentCustomerId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return this.Error(StatusCodes.Status401Unauthorized, "unauthorized", "Customer identity was not found.");
            }

            var orderItems = (await this.cartService.GetCheckoutHistoryByUserId(userId)).ToArray();
            return orderItems.Length == 0
                ? this.Failure<IReadOnlyList<StorefrontOrderItemHistoryResponse>>(
                    ServiceResponseType.NotFound,
                    "No orders found for the current customer.",
                    [])
                : this.Success(
                    orderItems.Select(item => item.ToStorefrontContract()).ToArray(),
                    "Current customer order items loaded.");
        }

        private string? GetCurrentCustomerId()
        {
            return this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/pages")]
    public sealed class StorefrontScopedPagesController : StorefrontApiControllerBase
    {
        private readonly IStorefrontPageService storefrontPageService;
        private readonly IStorefrontPageTemplateService templateService;

        public StorefrontScopedPagesController(
            IStorefrontPageService storefrontPageService,
            IStorefrontPageTemplateService templateService)
        {
            this.storefrontPageService = storefrontPageService;
            this.templateService = templateService;
        }

        [HttpGet("navigation")]
        public async Task<IActionResult> ListNavigation(CancellationToken cancellationToken)
        {
            var result = await this.templateService.ListNavigationLinksAsync(cancellationToken);
            return this.FromServiceResponse(result);
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
        {
            var result = await this.storefrontPageService.GetPublishedBySlugAsync(slug, cancellationToken);
            return this.FromServiceResponse(result);
        }
    }

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/configuration")]
    public sealed class StorefrontScopedConfigurationController : StorefrontApiControllerBase
    {
        private readonly ICommerceStoreContext storeContext;
        private readonly IPaymentMethodService paymentMethodService;
        private readonly IStoreCurrencyService currencyService;
        private readonly IStoreSeoSettingsService seoSettingsService;
        private readonly IStoreFeatureStateService featureStateService;
        private readonly IStorefrontPublicConfigurationCache publicConfigurationCache;

        public StorefrontScopedConfigurationController(
            ICommerceStoreContext storeContext,
            IPaymentMethodService paymentMethodService,
            IStoreCurrencyService currencyService,
            IStoreSeoSettingsService seoSettingsService,
            IStoreFeatureStateService featureStateService,
            IStorefrontPublicConfigurationCache publicConfigurationCache)
        {
            this.storeContext = storeContext;
            this.paymentMethodService = paymentMethodService;
            this.currencyService = currencyService;
            this.seoSettingsService = seoSettingsService;
            this.featureStateService = featureStateService;
            this.publicConfigurationCache = publicConfigurationCache;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var storeResult = await this.storeContext.GetCurrentStoreAsync(cancellationToken);
            if (!storeResult.Success || storeResult.Payload is null)
            {
                return this.ToActionResult(storeResult);
            }

            if (this.publicConfigurationCache.TryGet<StorefrontPublicConfigurationResponse>(
                storeResult.Payload.StoreKey,
                out var cachedConfiguration) && cachedConfiguration is not null)
            {
                return this.Success(cachedConfiguration, "Storefront configuration loaded.");
            }

            var storeIdResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeIdResult.Success)
            {
                return this.ToActionResult(storeIdResult);
            }

            var paymentMethods = (await this.paymentMethodService.GetPaymentMethodsAsync())
                .Select(method => method.ToStorefrontContract())
                .ToArray();
            var supportedCurrencyCodes = await this.currencyService.ResolveSupportedCurrencyCodesAsync(
                storeIdResult.Payload,
                cancellationToken);
            var seoDefaults = await this.seoSettingsService.ResolveAsync(cancellationToken);
            var featureStates = await this.featureStateService.ResolveAsync(storeIdResult.Payload, cancellationToken);
            var configuration = storeResult.Payload.ToPublicConfigurationContract(
                paymentMethods,
                seoDefaults,
                featureStates,
                supportedCurrencyCodes);

            this.publicConfigurationCache.Set(storeResult.Payload.StoreKey, configuration);

            return this.Success(
                configuration,
                "Storefront configuration loaded.");
        }

        private IActionResult ToActionResult<TPayload>(CommerceStoreOperationResult<TPayload> result)
        {
            if (!result.Success)
            {
                return this.StatusCode(
                    ToStatusCode(result.Failure),
                    new CommerceNodeApiErrorResponse(
                        false,
                        ToErrorCode(result.Failure),
                        NormalizeMessage(result.Message),
                        this.HttpContext.TraceIdentifier));
            }

            return this.StatusCode(
                StatusCodes.Status500InternalServerError,
                new CommerceNodeApiErrorResponse(
                    false,
                    "store.unavailable",
                    "Storefront store could not be resolved.",
                    this.HttpContext.TraceIdentifier));
        }

        private static int ToStatusCode(CommerceStoreOperationFailure? failure)
        {
            return failure switch
            {
                CommerceStoreOperationFailure.Validation => StatusCodes.Status400BadRequest,
                CommerceStoreOperationFailure.NotFound => StatusCodes.Status404NotFound,
                CommerceStoreOperationFailure.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError,
            };
        }

        private static string ToErrorCode(CommerceStoreOperationFailure? failure)
        {
            return failure switch
            {
                CommerceStoreOperationFailure.Validation => "store.validation_error",
                CommerceStoreOperationFailure.NotFound => "store.not_found",
                CommerceStoreOperationFailure.Conflict => "store.conflict",
                _ => "store.unavailable",
            };
        }

        private static string NormalizeMessage(string? message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? "Storefront store could not be resolved."
                : message;
        }
    }

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/payments")]
    public sealed class StorefrontScopedPaymentsController : StorefrontApiControllerBase
    {
        private readonly ClientAppOptions clientAppOptions;
        private readonly ICommerceStoreContext storeContext;
        private readonly IPaymentAttemptService paymentAttemptService;
        private readonly IPaymentMethodService paymentMethodService;
        private readonly IPayPalPaymentService payPalPaymentService;

        public StorefrontScopedPaymentsController(
            ICommerceStoreContext storeContext,
            IPaymentAttemptService paymentAttemptService,
            IPaymentMethodService paymentMethodService,
            IPayPalPaymentService payPalPaymentService,
            IOptions<ClientAppOptions> clientAppOptions)
        {
            this.storeContext = storeContext;
            this.paymentAttemptService = paymentAttemptService;
            this.paymentMethodService = paymentMethodService;
            this.payPalPaymentService = payPalPaymentService;
            this.clientAppOptions = clientAppOptions.Value;
        }

        [HttpGet("methods")]
        public async Task<IActionResult> GetPaymentMethods()
        {
            var paymentMethods = (await this.paymentMethodService.GetPaymentMethodsAsync()).ToArray();
            return paymentMethods.Length == 0
                ? this.Failure<IReadOnlyList<StorefrontPaymentMethodResponse>>(
                    ServiceResponseType.NotFound,
                    "No payment methods are currently available.",
                    [])
                : this.Success(
                    paymentMethods.Select(method => method.ToStorefrontContract()).ToArray(),
                    "Payment methods loaded.");
        }

        [HttpGet("attempts/{attemptId:guid}")]
        public async Task<IActionResult> GetAttempt(Guid attemptId, CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.paymentAttemptService.GetAsync(storeId.Value, attemptId, cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload is PaymentAttemptDto attempt
                    ? attempt.ToStorefrontContract()
                    : null);
        }

        [HttpPost("provider-callback/{providerKey}")]
        public async Task<IActionResult> HandleProviderCallback(
            string providerKey,
            [FromBody] StorefrontPaymentCallbackRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var eventResult = await this.paymentAttemptService.RecordProviderEventAsync(
                new RecordPaymentProviderEventRequest(
                    storeId.Value,
                    request.PaymentAttemptId,
                    providerKey,
                    request.ProviderEventId,
                    request.EventType,
                    request.PayloadJson,
                    ProcessedAtUtc: DateTimeOffset.UtcNow),
                cancellationToken);
            if (!eventResult.Success || eventResult.Payload is null)
            {
                return this.FromServiceResponse(eventResult);
            }

            if (request.PaymentAttemptId.HasValue && !string.IsNullOrWhiteSpace(request.State))
            {
                var transition = await this.paymentAttemptService.TransitionAsync(
                    new TransitionPaymentAttemptRequest(
                        storeId.Value,
                        request.PaymentAttemptId.Value,
                        request.State,
                        request.ProviderReference,
                        request.ProviderSessionId,
                        FailureCode: request.FailureCode,
                        FailureMessage: request.FailureMessage,
                        MetadataJson: request.PayloadJson),
                    cancellationToken);
                if (!transition.Success)
                {
                    return this.FromServiceResponse(transition);
                }
            }

            return this.Success(
                new StorefrontPaymentWebhookAcceptedResponse(
                    providerKey,
                    request.ProviderEventId,
                    eventResult.Payload.IsDuplicate,
                    eventResult.Payload.PayloadHash,
                    eventResult.Payload.CreatedAtUtc),
                "Payment provider callback accepted.");
        }

        [HttpPost("webhooks/{providerKey}")]
        public async Task<IActionResult> HandleWebhook(
            string providerKey,
            [FromHeader(Name = "X-Provider-Signature")] string? providerSignature,
            [FromBody] StorefrontPaymentWebhookRequest request,
            CancellationToken cancellationToken)
        {
            _ = providerSignature;
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var eventResult = await this.paymentAttemptService.RecordProviderEventAsync(
                new RecordPaymentProviderEventRequest(
                    storeId.Value,
                    request.PaymentAttemptId,
                    providerKey,
                    request.EventId,
                    request.EventType,
                    request.PayloadJson,
                    ProcessedAtUtc: DateTimeOffset.UtcNow),
                cancellationToken);
            if (!eventResult.Success || eventResult.Payload is null)
            {
                return this.FromServiceResponse(eventResult);
            }

            if (request.PaymentAttemptId.HasValue && !string.IsNullOrWhiteSpace(request.State))
            {
                var transition = await this.paymentAttemptService.TransitionAsync(
                    new TransitionPaymentAttemptRequest(
                        storeId.Value,
                        request.PaymentAttemptId.Value,
                        request.State,
                        MetadataJson: request.PayloadJson),
                    cancellationToken);
                if (!transition.Success)
                {
                    return this.FromServiceResponse(transition);
                }
            }

            return this.Success(
                new StorefrontPaymentWebhookAcceptedResponse(
                    providerKey,
                    request.EventId,
                    eventResult.Payload.IsDuplicate,
                    eventResult.Payload.PayloadHash,
                    eventResult.Payload.CreatedAtUtc),
                "Payment webhook accepted.");
        }

        [HttpPost("paypal/capture")]
        public async Task<IActionResult> CapturePayPal([FromBody] StorefrontPayPalCaptureRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Token))
            {
                return this.Error(
                    StatusCodes.Status400BadRequest,
                    "payment.paypal_token_missing",
                    "Missing PayPal token.");
            }

            var captured = await this.payPalPaymentService.CaptureAsync(request.Token);
            if (!captured)
            {
                return this.Error(
                    StatusCodes.Status409Conflict,
                    "payment.paypal_capture_failed",
                    "PayPal payment capture failed.");
            }

            return this.Success(
                new StorefrontPayPalCaptureResponse(
                    true,
                    this.BuildClientUrl("payment-success"),
                    "PayPal payment captured."),
                "PayPal payment captured.");
        }

        private string BuildClientUrl(string path)
        {
            if (string.IsNullOrWhiteSpace(this.clientAppOptions.BaseUrl))
            {
                return $"/{path.TrimStart('/')}";
            }

            return $"{this.clientAppOptions.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        }

        private async Task<Guid?> ResolveStoreIdAsync(CancellationToken cancellationToken)
        {
            var result = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            return result.Success ? result.Payload : null;
        }
    }

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/recommendations")]
    public sealed class StorefrontScopedRecommendationsController : StorefrontApiControllerBase
    {
        private readonly IProductRecommendationService recommendationService;
        private readonly ILogger<StorefrontScopedRecommendationsController> logger;

        public StorefrontScopedRecommendationsController(
            IProductRecommendationService recommendationService,
            ILogger<StorefrontScopedRecommendationsController> logger)
        {
            this.recommendationService = recommendationService;
            this.logger = logger;
        }

        [HttpGet("products/{productId:guid}")]
        public async Task<IActionResult> GetRecommendations(Guid productId)
        {
            try
            {
                if (productId == Guid.Empty)
                {
                    return this.Failure<IReadOnlyList<StorefrontProductRecommendationResponse>>(
                        ServiceResponseType.ValidationError,
                        "Invalid product ID.");
                }

                var recommendations = (await this.recommendationService.GetRecommendationsForProductAsync(productId)).ToArray();
                if (recommendations.Length == 0)
                {
                    return this.Failure<IReadOnlyList<StorefrontProductRecommendationResponse>>(
                        ServiceResponseType.NotFound,
                        "No recommendations found for this product.",
                        []);
                }

                return this.Success(
                    recommendations.Select(recommendation => recommendation.ToStorefrontContract()).ToArray(),
                    "Product recommendations loaded.");
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error occurred while fetching recommendations for product {ProductId}.", productId);
                return this.Failure<IReadOnlyList<StorefrontProductRecommendationResponse>>(
                    ServiceResponseType.Failure,
                    "An error occurred while processing product recommendations.");
            }
        }
    }

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/seo")]
    public sealed class StorefrontScopedSeoController : StorefrontApiControllerBase
    {
        private readonly ISeoRedirectResolutionService seoRedirectResolutionService;
        private readonly ISeoUrlResolver seoUrlResolver;
        private readonly IStoreSeoSettingsService seoSettingsService;

        public StorefrontScopedSeoController(
            ISeoRedirectResolutionService seoRedirectResolutionService,
            ISeoUrlResolver seoUrlResolver,
            IStoreSeoSettingsService seoSettingsService)
        {
            this.seoRedirectResolutionService = seoRedirectResolutionService;
            this.seoUrlResolver = seoUrlResolver;
            this.seoSettingsService = seoSettingsService;
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await this.seoSettingsService.ResolveAsync();
            return this.Success(settings, "SEO settings loaded.");
        }

        [HttpGet("redirects/resolve")]
        public async Task<IActionResult> ResolveRedirect([FromQuery] string path, CancellationToken cancellationToken)
        {
            var redirect = await this.seoRedirectResolutionService.ResolvePublicPathAsync(path);
            redirect ??= ToRedirectResolution(await this.seoUrlResolver.ResolvePublicPathAsync(path, cancellationToken));

            return redirect is null
                ? this.Failure<SeoRedirectResolutionDto>(ServiceResponseType.NotFound, "SEO redirect was not found.")
                : this.Success(redirect, "SEO redirect resolved.");
        }

        private static SeoRedirectResolutionDto? ToRedirectResolution(SeoUrlResolutionDto resolution)
        {
            return resolution.RequiresRedirect && !string.IsNullOrWhiteSpace(resolution.CanonicalPath)
                ? new SeoRedirectResolutionDto
                {
                    NewPath = resolution.CanonicalPath,
                    StatusCode = resolution.HttpStatusCode,
                }
                : null;
        }
    }

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/store")]
    public sealed class StorefrontScopedStoreController : StorefrontApiControllerBase
    {
        private readonly ICommerceStoreContext storeContext;

        public StorefrontScopedStoreController(ICommerceStoreContext storeContext)
        {
            this.storeContext = storeContext;
        }

        [HttpGet("current")]
        public async Task<IActionResult> Current(CancellationToken cancellationToken)
        {
            var result = await this.storeContext.GetCurrentStoreAsync(cancellationToken);
            if (!result.Success || result.Payload is null)
            {
                return this.ToActionResult(result);
            }

            return this.Ok(CommerceNodeApiResponse<StorefrontCurrentStoreResponse>.Succeeded(
                result.Payload.ToStorefrontContract(),
                "Current store resolved."));
        }

        [HttpGet("maintenance")]
        public async Task<IActionResult> Maintenance(CancellationToken cancellationToken)
        {
            var result = await this.storeContext.GetCurrentStoreAsync(cancellationToken);
            if (!result.Success || result.Payload is null)
            {
                return this.ToActionResult(result);
            }

            return this.Ok(CommerceNodeApiResponse<StorefrontMaintenanceResponse>.Succeeded(
                result.Payload.ToStorefrontMaintenanceContract(),
                "Store maintenance state retrieved."));
        }

        private IActionResult ToActionResult<TPayload>(CommerceStoreOperationResult<TPayload> result)
        {
            if (!result.Success)
            {
                return this.StatusCode(
                    ToStatusCode(result.Failure),
                    new CommerceNodeApiErrorResponse(
                        false,
                        ToErrorCode(result.Failure),
                        NormalizeMessage(result.Message),
                        this.HttpContext.TraceIdentifier));
            }

            return new ObjectResult(CommerceNodeApiResponse<TPayload>.Succeeded(result.Payload, NormalizeMessage(result.Message)))
            {
                StatusCode = StatusCodes.Status200OK,
            };
        }

        private static int ToStatusCode(CommerceStoreOperationFailure? failure)
        {
            return failure switch
            {
                CommerceStoreOperationFailure.Validation => StatusCodes.Status400BadRequest,
                CommerceStoreOperationFailure.NotFound => StatusCodes.Status404NotFound,
                CommerceStoreOperationFailure.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError,
            };
        }

        private static string ToErrorCode(CommerceStoreOperationFailure? failure)
        {
            return failure switch
            {
                CommerceStoreOperationFailure.Validation => "validation_error",
                CommerceStoreOperationFailure.NotFound => "not_found",
                CommerceStoreOperationFailure.Conflict => "conflict",
                _ => "internal_error",
            };
        }

        private static string NormalizeMessage(string? message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? "The current store could not be resolved."
                : message;
        }
    }
}
