namespace BlazorShop.CommerceNode.API.Controllers
{
    using System.Security.Claims;

    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Discovery;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Application.Options;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Services.Contracts.Authentication;
    using BlazorShop.Application.Services.Contracts.Payment;
    using BlazorShop.CommerceNode.API.Configuration;
    using BlazorShop.CommerceNode.API.Responses;
    using BlazorShop.Domain.Contracts;

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
        public async Task<IActionResult> Register(CreateUser user)
        {
            var result = await this.authenticationService.CreateUser(user);
            return this.FromServiceResponse(result);
        }

        [HttpPost("login")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Login(LoginUser user)
        {
            var result = await this.authenticationService.LoginUser(user, this.GetClientIpAddress(), this.GetUserAgent());
            if (!result.Success)
            {
                return this.BadRequest(CommerceNodeApiResponse<LoginResponse>.Failed(
                    NormalizeLoginMessage(result.Message),
                    SanitizeLoginResponse(result)));
            }

            if (string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                return this.StatusCode(
                    StatusCodes.Status500InternalServerError,
                    CommerceNodeApiResponse<LoginResponse>.Failed("Error occurred in login.", SanitizeLoginResponse(result)));
            }

            this.AppendRefreshTokenCookie(result.RefreshToken);
            return this.Ok(CommerceNodeApiResponse<LoginResponse>.Succeeded(
                SanitizeLoginResponse(result),
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
                return this.BadRequest(CommerceNodeApiResponse<LoginResponse>.Failed(
                    "Invalid token.",
                    new LoginResponse(Message: "Invalid token.")));
            }

            var result = await this.authenticationService.ReviveToken(
                refreshToken,
                this.GetClientIpAddress(),
                this.GetUserAgent());

            if (!result.Success)
            {
                this.DeleteRefreshTokenCookie();
                return this.BadRequest(CommerceNodeApiResponse<LoginResponse>.Failed(
                    NormalizeLoginMessage(result.Message),
                    SanitizeLoginResponse(result)));
            }

            if (string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                this.DeleteRefreshTokenCookie();
                return this.StatusCode(
                    StatusCodes.Status500InternalServerError,
                    CommerceNodeApiResponse<LoginResponse>.Failed("Error occurred in login.", SanitizeLoginResponse(result)));
            }

            this.AppendRefreshTokenCookie(result.RefreshToken);
            return this.Ok(CommerceNodeApiResponse<LoginResponse>.Succeeded(
                SanitizeLoginResponse(result),
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
        public async Task<IActionResult> ChangePassword([FromBody] ChangePassword dto)
        {
            var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized(CommerceNodeApiResponse<object>.Failed("Customer identity was not found."));
            }

            var result = await this.authenticationService.ChangePassword(dto, userId);
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
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfile dto)
        {
            var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized(CommerceNodeApiResponse<object>.Failed("Customer identity was not found."));
            }

            var result = await this.authenticationService.UpdateProfile(userId, dto);
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

        public StorefrontScopedCatalogController(IPublicCatalogService publicCatalogService)
        {
            this.publicCatalogService = publicCatalogService;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await this.publicCatalogService.GetPublishedCategoriesAsync();
            return this.Success(categories, "Published categories loaded.");
        }

        [HttpGet("categories/tree")]
        public async Task<IActionResult> GetCategoryTree()
        {
            var categories = await this.publicCatalogService.GetPublishedCategoryTreeAsync();
            return this.Success(categories, "Published category tree loaded.");
        }

        [HttpGet("categories/{id:guid}")]
        public async Task<IActionResult> GetCategoryById(Guid id)
        {
            var category = await this.publicCatalogService.GetPublishedCategoryByIdAsync(id);
            return category is null
                ? this.Failure<GetCategory>(ServiceResponseType.NotFound, "Published category was not found.")
                : this.Success(category, "Published category loaded.");
        }

        [HttpGet("categories/slug/{slug}")]
        public async Task<IActionResult> GetCategoryBySlug(string slug)
        {
            var categoryPage = await this.publicCatalogService.GetPublishedCategoryPageBySlugAsync(slug);
            return categoryPage is null
                ? this.Failure<GetCategoryPage>(ServiceResponseType.NotFound, "Published category was not found.")
                : this.Success(categoryPage, "Published category page loaded.");
        }

        [HttpGet("categories/{categoryId:guid}/products")]
        public async Task<IActionResult> GetProductsByCategory(Guid categoryId)
        {
            var category = await this.publicCatalogService.GetPublishedCategoryByIdAsync(categoryId);
            if (category is null)
            {
                return this.Failure<IReadOnlyList<GetCatalogProduct>>(
                    ServiceResponseType.NotFound,
                    "Published category was not found.");
            }

            var products = await this.publicCatalogService.GetPublishedProductsByCategoryAsync(categoryId);
            return this.Success(products, "Published category products loaded.");
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts([FromQuery] ProductCatalogQuery query)
        {
            var products = await this.publicCatalogService.GetPublishedCatalogPageAsync(query);
            return this.Success(products, "Published products loaded.");
        }

        [HttpGet("products/{id:guid}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var product = await this.publicCatalogService.GetPublishedProductByIdAsync(id);
            return product is null
                ? this.Failure<GetProduct>(ServiceResponseType.NotFound, "Published product was not found.")
                : this.Success(product, "Published product loaded.");
        }

        [HttpGet("products/slug/{slug}")]
        public async Task<IActionResult> GetProductBySlug(string slug)
        {
            var product = await this.publicCatalogService.GetPublishedProductBySlugAsync(slug);
            return product is null
                ? this.Failure<GetProduct>(ServiceResponseType.NotFound, "Published product was not found.")
                : this.Success(product, "Published product loaded.");
        }

        [HttpGet("sitemap")]
        public async Task<IActionResult> GetSitemap()
        {
            var sitemap = await this.publicCatalogService.GetPublishedSitemapAsync();
            return this.Success<GetPublicCatalogSitemap>(sitemap, "Published catalog sitemap loaded.");
        }
    }

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/cart")]
    [Authorize]
    public sealed class StorefrontScopedCartController : StorefrontApiControllerBase
    {
        private readonly ICartService cartService;

        public StorefrontScopedCartController(ICartService cartService)
        {
            this.cartService = cartService;
        }

        [HttpPost("checkout")]
        [AllowAnonymous]
        public async Task<IActionResult> Checkout(StorefrontCheckoutRequest checkout)
        {
            var userId = this.GetCurrentCustomerId();
            var result = await this.cartService.CheckoutAsync(checkout, userId);
            return this.FromServiceResponse(result);
        }

        [HttpPost("save-checkout")]
        public async Task<IActionResult> SaveCheckout(IEnumerable<CreateOrderItem> orderItems)
        {
            var userId = this.GetCurrentCustomerId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return this.Unauthorized(CommerceNodeApiResponse<object>.Failed("Customer identity was not found."));
            }

            var result = await this.cartService.SaveCheckoutHistoryAsync(userId, orderItems);
            return this.FromServiceResponse(result);
        }

        private string? GetCurrentCustomerId()
        {
            return this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
        public async Task<IActionResult> Subscribe([FromBody] SubscribeEmailRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Email))
            {
                return this.Failure<object>(ServiceResponseType.ValidationError, "Email is required.");
            }

            var result = await this.newsletterService.SubscribeAsync(request.Email);
            return this.FromServiceResponse(result);
        }

        public sealed record SubscribeEmailRequest(string Email);
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
        public async Task<IActionResult> ConfirmOrder(IEnumerable<ProcessCart> carts, [FromQuery] string? status = null)
        {
            var userId = this.GetCurrentCustomerId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return this.Unauthorized(CommerceNodeApiResponse<object>.Failed("Customer identity was not found."));
            }

            var result = await this.cartService.ConfirmOrderAsync(carts, userId, status);
            return this.FromServiceResponse(result);
        }

        [HttpGet("current-user")]
        public async Task<IActionResult> GetCurrentUserOrders()
        {
            var userId = this.GetCurrentCustomerId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return this.Unauthorized(CommerceNodeApiResponse<object>.Failed("Customer identity was not found."));
            }

            var orders = (await this.orderQueryService.GetOrdersForUserAsync(userId)).ToArray();
            return this.Success<IEnumerable<GetOrder>>(orders, "Current customer orders loaded.");
        }

        [HttpGet("current-user/items")]
        public async Task<IActionResult> GetCurrentUserOrderItems()
        {
            var userId = this.GetCurrentCustomerId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return this.Unauthorized(CommerceNodeApiResponse<object>.Failed("Customer identity was not found."));
            }

            var orderItems = (await this.cartService.GetCheckoutHistoryByUserId(userId)).ToArray();
            return orderItems.Length == 0
                ? this.Failure<IEnumerable<GetOrderItem>>(
                    ServiceResponseType.NotFound,
                    "No orders found for the current customer.",
                    orderItems)
                : this.Success<IEnumerable<GetOrderItem>>(orderItems, "Current customer order items loaded.");
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

        public StorefrontScopedPagesController(IStorefrontPageService storefrontPageService)
        {
            this.storefrontPageService = storefrontPageService;
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
        {
            var result = await this.storefrontPageService.GetPublishedBySlugAsync(slug, cancellationToken);
            return this.FromServiceResponse(result);
        }
    }

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/payments")]
    public sealed class StorefrontScopedPaymentsController : StorefrontApiControllerBase
    {
        private readonly ClientAppOptions clientAppOptions;
        private readonly IPaymentMethodService paymentMethodService;
        private readonly IPayPalPaymentService payPalPaymentService;

        public StorefrontScopedPaymentsController(
            IPaymentMethodService paymentMethodService,
            IPayPalPaymentService payPalPaymentService,
            IOptions<ClientAppOptions> clientAppOptions)
        {
            this.paymentMethodService = paymentMethodService;
            this.payPalPaymentService = payPalPaymentService;
            this.clientAppOptions = clientAppOptions.Value;
        }

        [HttpGet("methods")]
        public async Task<IActionResult> GetPaymentMethods()
        {
            var paymentMethods = (await this.paymentMethodService.GetPaymentMethodsAsync()).ToArray();
            return paymentMethods.Length == 0
                ? this.Failure<IEnumerable<GetPaymentMethod>>(
                    ServiceResponseType.NotFound,
                    "No payment methods are currently available.",
                    paymentMethods)
                : this.Success<IEnumerable<GetPaymentMethod>>(paymentMethods, "Payment methods loaded.");
        }

        [HttpGet("paypal/capture")]
        public async Task<IActionResult> CapturePayPal([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return this.Failure<object>(ServiceResponseType.ValidationError, "Missing PayPal token.");
            }

            var captured = await this.payPalPaymentService.CaptureAsync(token);
            return captured
                ? this.Redirect(this.BuildClientUrl("payment-success"))
                : this.Redirect(this.BuildClientUrl("payment-cancel"));
        }

        private string BuildClientUrl(string path)
        {
            if (string.IsNullOrWhiteSpace(this.clientAppOptions.BaseUrl))
            {
                return $"/{path.TrimStart('/')}";
            }

            return $"{this.clientAppOptions.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
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
                    return this.Failure<IEnumerable<GetProductRecommendation>>(
                        ServiceResponseType.ValidationError,
                        "Invalid product ID.");
                }

                var recommendations = (await this.recommendationService.GetRecommendationsForProductAsync(productId)).ToArray();
                if (recommendations.Length == 0)
                {
                    return this.Failure<IEnumerable<GetProductRecommendation>>(
                        ServiceResponseType.NotFound,
                        "No recommendations found for this product.",
                        recommendations);
                }

                return this.Success<IEnumerable<GetProductRecommendation>>(
                    recommendations,
                    "Product recommendations loaded.");
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error occurred while fetching recommendations for product {ProductId}.", productId);
                return this.Failure<IEnumerable<GetProductRecommendation>>(
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
        private readonly ISeoSettingsService seoSettingsService;

        public StorefrontScopedSeoController(
            ISeoRedirectResolutionService seoRedirectResolutionService,
            ISeoSettingsService seoSettingsService)
        {
            this.seoRedirectResolutionService = seoRedirectResolutionService;
            this.seoSettingsService = seoSettingsService;
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await this.seoSettingsService.GetCurrentAsync();
            return this.Success(settings, "SEO settings loaded.");
        }

        [HttpGet("redirects/resolve")]
        public async Task<IActionResult> ResolveRedirect([FromQuery] string path)
        {
            var redirect = await this.seoRedirectResolutionService.ResolvePublicPathAsync(path);
            return redirect is null
                ? this.Failure<SeoRedirectResolutionDto>(ServiceResponseType.NotFound, "SEO redirect was not found.")
                : this.Success(redirect, "SEO redirect resolved.");
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
            return ToActionResult(result);
        }

        [HttpGet("maintenance")]
        public async Task<IActionResult> Maintenance(CancellationToken cancellationToken)
        {
            var result = await this.storeContext.GetCurrentStoreAsync(cancellationToken);
            if (!result.Success || result.Payload is null)
            {
                return ToActionResult(result);
            }

            var maintenance = new
            {
                result.Payload.PublicId,
                result.Payload.StoreKey,
                result.Payload.Name,
                result.Payload.MaintenanceModeEnabled,
                result.Payload.MaintenanceMessage,
            };

            return this.Ok(CommerceNodeApiResponse<object>.Succeeded(maintenance, "Store maintenance state retrieved."));
        }

        private static IActionResult ToActionResult<TPayload>(CommerceStoreOperationResult<TPayload> result)
        {
            var response = result.Success
                ? CommerceNodeApiResponse<TPayload>.Succeeded(result.Payload, NormalizeMessage(result.Message))
                : CommerceNodeApiResponse<TPayload>.Failed(NormalizeMessage(result.Message), result.Payload);

            return new ObjectResult(response)
            {
                StatusCode = result.Success ? StatusCodes.Status200OK : ToStatusCode(result.Failure),
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

        private static string NormalizeMessage(string? message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? "The current store could not be resolved."
                : message;
        }
    }
}
