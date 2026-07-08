namespace BlazorShop.ControlPlane.API.Controllers
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;

    using BlazorShop.Application.ControlPlane.Audit;
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Application.Services.Contracts.Authentication;
    using BlazorShop.ControlPlane.API.Responses;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/control-plane/auth")]
    public sealed class ControlPlaneAuthController : ControllerBase
    {
        private const string RefreshTokenCookieName = "__Host-blazorshop-controlplane-refresh";

        private readonly IAuthenticationService authenticationService;
        private readonly IControlPlaneProfileService profileService;
        private readonly IControlPlaneAuditService auditService;

        public ControlPlaneAuthController(
            IAuthenticationService authenticationService,
            IControlPlaneProfileService profileService,
            IControlPlaneAuditService auditService)
        {
            this.authenticationService = authenticationService;
            this.profileService = profileService;
            this.auditService = auditService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Login(LoginUser user, CancellationToken cancellationToken)
        {
            var result = await this.authenticationService.LoginUser(user, GetClientIpAddress(), GetUserAgent());

            if (!result.Success)
            {
                await this.WriteAuditAsync("auth.login", "failure", actorEmail: user.Email, cancellationToken: cancellationToken);
                return ControlPlaneApiResponseWriter.Failure<LoginResponse>(
                    StatusCodes.Status400BadRequest,
                    SanitizeLoginResponse(result).Message);
            }

            if (string.IsNullOrWhiteSpace(result.Token) || string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                await this.WriteAuditAsync("auth.login", "failure", actorEmail: user.Email, cancellationToken: cancellationToken);
                return ControlPlaneApiResponseWriter.Failure<LoginResponse>(
                    StatusCodes.Status500InternalServerError,
                    "Error occurred in login.");
            }

            var profile = await this.EnsureProfileFromTokenAsync(result.Token, cancellationToken);
            if (!IsActiveProfile(profile))
            {
                await this.authenticationService.Logout(result.RefreshToken, GetClientIpAddress());
                DeleteRefreshTokenCookie();
                await this.WriteAuditAsync("auth.login", "failure", profile.AdminUserId, profile.IdentityUserId, profile.Email, cancellationToken);
                return ControlPlaneApiResponseWriter.Failure<LoginResponse>(
                    StatusCodes.Status400BadRequest,
                    "Invalid credentials.");
            }

            AppendRefreshTokenCookie(result.RefreshToken);

            await this.WriteAuditAsync(
                "auth.login",
                "success",
                profile.AdminUserId,
                profile.IdentityUserId,
                profile.Email,
                cancellationToken);

            return ControlPlaneApiResponseWriter.Success(
                StatusCodes.Status200OK,
                SanitizeLoginResponse(result),
                string.IsNullOrWhiteSpace(result.Message) ? "Signed in." : result.Message);
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request, CancellationToken cancellationToken)
        {
            var refreshToken = ResolveRefreshToken(request?.RefreshToken);

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                DeleteRefreshTokenCookie();
                return ControlPlaneApiResponseWriter.Failure<LoginResponse>(
                    StatusCodes.Status200OK,
                    "No active session.");
            }

            var result = await this.authenticationService.ReviveToken(refreshToken, GetClientIpAddress(), GetUserAgent());

            if (!result.Success || string.IsNullOrWhiteSpace(result.Token) || string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                DeleteRefreshTokenCookie();
                await this.WriteAuditAsync("auth.refresh", "failure", cancellationToken: cancellationToken);
                return ControlPlaneApiResponseWriter.Failure<LoginResponse>(
                    StatusCodes.Status400BadRequest,
                    SanitizeLoginResponse(result).Message);
            }

            var profile = await this.EnsureProfileFromTokenAsync(result.Token, cancellationToken);
            if (!IsActiveProfile(profile))
            {
                await this.authenticationService.Logout(result.RefreshToken, GetClientIpAddress());
                DeleteRefreshTokenCookie();
                await this.WriteAuditAsync("auth.refresh", "failure", profile.AdminUserId, profile.IdentityUserId, profile.Email, cancellationToken);
                return ControlPlaneApiResponseWriter.Failure<LoginResponse>(
                    StatusCodes.Status400BadRequest,
                    "Session is no longer active.");
            }

            AppendRefreshTokenCookie(result.RefreshToken);

            await this.WriteAuditAsync(
                "auth.refresh",
                "success",
                profile.AdminUserId,
                profile.IdentityUserId,
                profile.Email,
                cancellationToken);

            return ControlPlaneApiResponseWriter.Success(
                StatusCodes.Status200OK,
                SanitizeLoginResponse(result),
                string.IsNullOrWhiteSpace(result.Message) ? "Session refreshed." : result.Message);
        }

        [HttpPost("logout")]
        [Authorize]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest? request, CancellationToken cancellationToken)
        {
            var refreshToken = ResolveRefreshToken(request?.RefreshToken);
            var result = await this.authenticationService.Logout(refreshToken ?? string.Empty, GetClientIpAddress());

            DeleteRefreshTokenCookie();
            await this.WriteAuditAsync("auth.logout", "success", cancellationToken: cancellationToken);

            return ControlPlaneApiResponseWriter.Success(
                StatusCodes.Status200OK,
                result,
                string.IsNullOrWhiteSpace(result.Message) ? "Signed out." : result.Message);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me(CancellationToken cancellationToken)
        {
            var profile = await this.EnsureProfileFromClaimsAsync(User.Claims, cancellationToken);
            if (!IsActiveProfile(profile))
            {
                return ControlPlaneApiResponseWriter.Failure<ControlPlaneProfileResponse>(
                    StatusCodes.Status403Forbidden,
                    "Session is no longer active.");
            }

            return ControlPlaneApiResponseWriter.Success(
                StatusCodes.Status200OK,
                new ControlPlaneProfileResponse(profile.AdminUserId, profile.Email, profile.DisplayName),
                "Control Plane profile loaded.");
        }

        private async Task<ControlPlaneProfileResult> EnsureProfileFromTokenAsync(
            string token,
            CancellationToken cancellationToken)
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return await this.EnsureProfileFromClaimsAsync(jwt.Claims, cancellationToken);
        }

        private async Task<ControlPlaneProfileResult> EnsureProfileFromClaimsAsync(
            IEnumerable<Claim> claims,
            CancellationToken cancellationToken)
        {
            var claimList = claims.ToArray();
            var identityUserId = FindClaimValue(claimList, ClaimTypes.NameIdentifier, "nameid", "sub");
            var email = FindClaimValue(claimList, ClaimTypes.Email, "email");
            var displayName = FindClaimValue(claimList, "FullName", "name") ?? email;

            if (string.IsNullOrWhiteSpace(identityUserId) || string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Authenticated Control Plane token is missing identity claims.");
            }

            return await this.profileService.EnsureProfileAsync(identityUserId, email, displayName ?? email, cancellationToken);
        }

        private static bool IsActiveProfile(ControlPlaneProfileResult profile)
        {
            return string.Equals(profile.Status, "active", StringComparison.Ordinal);
        }

        private async Task WriteAuditAsync(
            string action,
            string result,
            long? actorAdminUserId = null,
            string? actorIdentityUserId = null,
            string? actorEmail = null,
            CancellationToken cancellationToken = default)
        {
            actorIdentityUserId ??= User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("nameid") ?? User.FindFirstValue("sub");
            actorEmail ??= User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");

            await this.auditService.WriteAsync(
                new ControlPlaneAuditEntry(
                    Action: action,
                    EntityType: "control_plane_session",
                    Result: result,
                    ActorIdentityUserId: actorIdentityUserId,
                    ActorEmail: actorEmail,
                    ActorAdminUserId: actorAdminUserId,
                    IpAddress: GetClientIpAddress(),
                    UserAgent: GetUserAgent()),
                cancellationToken);
        }

        private void AppendRefreshTokenCookie(string refreshToken)
        {
            Response.Cookies.Append(RefreshTokenCookieName, refreshToken, CreateRefreshTokenCookieOptions());
        }

        private void DeleteRefreshTokenCookie()
        {
            Response.Cookies.Delete(RefreshTokenCookieName, CreateRefreshTokenCookieOptions());
        }

        private CookieOptions CreateRefreshTokenCookieOptions()
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                Path = "/",
                MaxAge = TimeSpan.FromDays(14)
            };
        }

        private string? ResolveRefreshToken(string? requestRefreshToken)
        {
            if (!string.IsNullOrWhiteSpace(requestRefreshToken))
            {
                return requestRefreshToken;
            }

            return Request.Cookies.TryGetValue(RefreshTokenCookieName, out var cookieRefreshToken)
                ? cookieRefreshToken
                : null;
        }

        private string? GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        private string? GetUserAgent()
        {
            return Request.Headers.UserAgent.ToString();
        }

        private static string? FindClaimValue(IEnumerable<Claim> claims, params string[] claimTypes)
        {
            return claims.FirstOrDefault(claim => claimTypes.Contains(claim.Type, StringComparer.Ordinal))?.Value;
        }

        private static LoginResponse SanitizeLoginResponse(LoginResponse response)
        {
            return response with { RefreshToken = string.Empty };
        }
    }

    public sealed record RefreshTokenRequest(string? RefreshToken = null);

    public sealed record ControlPlaneProfileResponse(long AdminUserId, string Email, string DisplayName);
}
