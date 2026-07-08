namespace BlazorShop.Web.Shared.Authentication
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;

    using BlazorShop.Web.Shared.Helper.Contracts;

    using Microsoft.AspNetCore.Components.Authorization;

    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ITokenService tokenService;
        private readonly ClaimsPrincipal anonymous = new(new ClaimsIdentity());

        public CustomAuthStateProvider(ITokenService tokenService)
        {
            this.tokenService = tokenService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var jwt = await this.tokenService.GetJwtTokenAsync(Constant.TokenStorage.Key);

            if (string.IsNullOrWhiteSpace(jwt))
            {
                return new AuthenticationState(this.anonymous);
            }

            var claims = TryGetValidClaims(jwt);

            if (claims == null)
            {
                await this.tokenService.RemoveJwtTokenAsync(Constant.TokenStorage.Key);
                return new AuthenticationState(this.anonymous);
            }

            var claimPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwtAuth"));
            return new AuthenticationState(claimPrincipal);
        }

        public void NotifyAuthenticationState()
        {
            this.NotifyAuthenticationStateChanged(this.GetAuthenticationStateAsync());
        }

        private static IReadOnlyList<Claim>? TryGetValidClaims(string jwt)
        {
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(jwt))
            {
                return null;
            }

            var token = handler.ReadJwtToken(jwt);
            var claims = token.Claims.ToList();

            if (claims.Count == 0)
            {
                return null;
            }

            var expirationClaim = claims
                .FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Exp || claim.Type == "exp")?
                .Value;

            if (!long.TryParse(expirationClaim, out var expirationSeconds))
            {
                return null;
            }

            var expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expirationSeconds);
            return expiresAtUtc <= DateTimeOffset.UtcNow ? null : claims;
        }
    }
}
