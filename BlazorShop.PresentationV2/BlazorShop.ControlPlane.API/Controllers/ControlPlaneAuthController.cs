namespace BlazorShop.ControlPlane.API.Controllers
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;

    using BlazorShop.Application.ControlPlane.Audit;
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Application.Services.Contracts.Authentication;

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
                return BadRequest(SanitizeLoginResponse(result));
            }

            if (string.IsNullOrWhiteSpace(result.Token) || string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                await this.WriteAuditAsync("auth.login", "failure", actorEmail: user.Email, cancellationToken: cancellationToken);
                return StatusCode(StatusCodes.Status500InternalServerError, new LoginResponse { Message = "Error occurred in login." });
            }

            var profile = await this.EnsureProfileFromTokenAsync(result.Token, cancellationToken);
            AppendRefreshTokenCookie(result.RefreshToken);

            await this.WriteAuditAsync(
                "auth.login",
                "success",
                profile.AdminUserId,
                profile.IdentityUserId,
                profile.Email,
                cancellationToken);

            return Ok(SanitizeLoginResponse(result));
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
                await this.WriteAuditAsync("auth.refresh", "failure", cancellationToken: cancellationToken);
                return BadRequest(new LoginResponse { Message = "Invalid token." });
            }

            var result = await this.authenticationService.ReviveToken(refreshToken, GetClientIpAddress(), GetUserAgent());

            if (!result.Success || string.IsNullOrWhiteSpace(result.Token) || string.IsNullOrWhiteSpace(result.RefreshToken))
            {
                DeleteRefreshTokenCookie();
                await this.WriteAuditAsync("auth.refresh", "failure", cancellationToken: cancellationToken);
                return BadRequest(SanitizeLoginResponse(result));
            }

            var profile = await this.EnsureProfileFromTokenAsync(result.Token, cancellationToken);
            AppendRefreshTokenCookie(result.RefreshToken);

            await this.WriteAuditAsync(
                "auth.refresh",
                "success",
                profile.AdminUserId,
                profile.IdentityUserId,
                profile.Email,
                cancellationToken);

            return Ok(SanitizeLoginResponse(result));
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

            return Ok(result);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ControlPlaneProfileResponse>> Me(CancellationToken cancellationToken)
        {
            var profile = await this.EnsureProfileFromClaimsAsync(User.Claims, cancellationToken);
            return Ok(new ControlPlaneProfileResponse(profile.AdminUserId, profile.Email, profile.DisplayName));
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
