namespace BlazorShop.CommerceNode.API.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.Security.Claims;

    using BlazorShop.Application.CommerceNode.Addresses;
    using BlazorShop.Application.CommerceNode.Captcha;
    using ApplicationStorefrontCheckoutResult = BlazorShop.Application.DTOs.Payment.StorefrontCheckoutResult;
    using ApplicationStorefrontCheckoutPreviewResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutPreviewResult;
    using ApplicationStorefrontCheckoutReviewResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutReviewResult;
    using ApplicationStorefrontCheckoutSessionRequest = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutSessionRequest;
    using ApplicationStorefrontCheckoutSessionResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutSessionResult;
    using ApplicationStorefrontCheckoutStartRequest = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutStartRequest;
    using ApplicationStorefrontPlaceOrderResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontPlaceOrderResult;
    using IStorefrontCheckoutService = BlazorShop.Application.CommerceNode.Checkout.IStorefrontCheckoutService;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Consent;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Customers;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Orders;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Settings;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Category;
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
    using BlazorShop.Domain.Contracts;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.Extensions.Options;

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/auth")]
    public sealed class StorefrontScopedAuthController : StorefrontApiControllerBase
    {
        private readonly IAuthenticationService authenticationService;
        private readonly ICaptchaVerifier captchaVerifier;
        private readonly IStoreSecurityPrivacySettingsService securityPrivacySettingsService;
        private readonly CommerceNodeRuntimeOptions runtimeOptions;

        public StorefrontScopedAuthController(
            IAuthenticationService authenticationService,
            ICaptchaVerifier captchaVerifier,
            IStoreSecurityPrivacySettingsService securityPrivacySettingsService,
            IOptions<CommerceNodeRuntimeOptions> runtimeOptions)
        {
            this.authenticationService = authenticationService;
            this.captchaVerifier = captchaVerifier;
            this.securityPrivacySettingsService = securityPrivacySettingsService;
            this.runtimeOptions = runtimeOptions.Value;
        }

        [HttpPost("register")]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.AuthStrict)]
        public async Task<IActionResult> Register([FromBody] StorefrontRegisterRequest user, CancellationToken cancellationToken)
        {
            var registration = await this.GetRegistrationPolicyAsync(cancellationToken);
            if (!registration.RegistrationAllowed)
            {
                return this.Error(StatusCodes.Status403Forbidden, "auth.registration_disabled", registration.Message);
            }

            var captchaFailure = await this.ValidateCaptchaAsync(CaptchaTargetNames.Registration, user.CaptchaToken, cancellationToken);
            if (captchaFailure is not null)
            {
                return captchaFailure;
            }

            var result = await this.authenticationService.CreateUser(user.ToApplicationRequest());
            return this.FromServiceResponse(
                result,
                payload => new StorefrontRegistrationResponse(
                    result.Id ?? (payload is Guid userId ? userId : Guid.Empty)));
        }

        [HttpGet("registration-policy")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> GetRegistrationPolicy(CancellationToken cancellationToken)
        {
            var policy = await this.GetRegistrationPolicyAsync(cancellationToken);
            return this.Ok(CommerceNodeApiResponse<StorefrontRegistrationPolicyResponse>.Succeeded(
                policy,
                "Registration policy returned."));
        }

        [HttpPost("login")]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.AuthStrict)]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Login([FromBody] StorefrontLoginRequest user, CancellationToken cancellationToken)
        {
            var captchaFailure = await this.ValidateCaptchaAsync(CaptchaTargetNames.Login, user.CaptchaToken, cancellationToken);
            if (captchaFailure is not null)
            {
                return captchaFailure;
            }

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
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.AuthStrict)]
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
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.AuthStrict)]
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
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.AuthStrict)]
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

        [HttpPost("forgot-password")]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.AuthStrict)]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> ForgotPassword([FromBody] StorefrontForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            var captchaFailure = await this.ValidateCaptchaAsync(
                CaptchaTargetNames.PasswordRecovery,
                request.CaptchaToken,
                cancellationToken);
            if (captchaFailure is not null)
            {
                return captchaFailure;
            }

            var result = await this.authenticationService.ForgotPassword(request.Email);
            return this.FromServiceResponse(result);
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.AuthStrict)]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> ResetPassword([FromBody] StorefrontResetPasswordRequest request)
        {
            var result = await this.authenticationService.ResetPassword(request.ToApplicationRequest());
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
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.AuthStrict)]
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

        private async Task<IActionResult?> ValidateCaptchaAsync(string target, string? token, CancellationToken cancellationToken)
        {
            var runtimeSettings = await this.securityPrivacySettingsService.ResolveCurrentAsync(cancellationToken);
            if (!IsCaptchaEnabled(runtimeSettings.Captcha, target))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return this.Error(StatusCodes.Status400BadRequest, "captcha.required", "Security verification is required.");
            }

            var result = await this.captchaVerifier.VerifyAsync(
                new CaptchaVerificationRequest(target, token, this.GetClientIpAddress(), this.GetUserAgent()),
                cancellationToken);
            return result.Success
                ? null
                : this.Error(StatusCodes.Status400BadRequest, "captcha.failed", "Security verification failed.");
        }

        private static bool IsCaptchaEnabled(CaptchaOptions options, string target)
        {
            if (!options.Enabled)
            {
                return false;
            }

            return target switch
            {
                CaptchaTargetNames.Login => options.Targets.Login,
                CaptchaTargetNames.Registration => options.Targets.Registration,
                CaptchaTargetNames.PasswordRecovery => options.Targets.PasswordRecovery,
                _ => false,
            };
        }

        private async Task<StorefrontRegistrationPolicyResponse> GetRegistrationPolicyAsync(CancellationToken cancellationToken)
        {
            var runtimeSettings = await this.securityPrivacySettingsService.ResolveCurrentAsync(cancellationToken);
            var message = runtimeSettings.Registration.RegistrationAllowed
                ? "Customer registration is available."
                : "Customer registration is disabled.";
            return new StorefrontRegistrationPolicyResponse(
                runtimeSettings.Registration.Mode,
                runtimeSettings.Registration.RegistrationAllowed,
                message);
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

}
