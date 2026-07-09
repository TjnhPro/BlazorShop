namespace BlazorShop.Infrastructure.Data.CommerceNode.Repositories
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Net;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;

    using BlazorShop.Domain.Contracts.Authentication;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IdentityModel.Tokens;

    public sealed class CommerceNodeAppTokenManager : IAppTokenManager
    {
        private const int RefreshTokenLifetimeDaysDefault = 14;

        private readonly IConfiguration config;
        private readonly CommerceNodeDbContext context;

        public CommerceNodeAppTokenManager(CommerceNodeDbContext context, IConfiguration config)
        {
            this.context = context;
            this.config = config;
        }

        public string GetReFreshToken()
        {
            const int byteSize = 64;
            var randomBytes = new byte[byteSize];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return WebUtility.UrlEncode(Convert.ToBase64String(randomBytes));
        }

        public RefreshToken CreateRefreshToken(
            string userId,
            string refreshToken,
            string? createdByIp = null,
            string? userAgent = null)
        {
            var createdAtUtc = DateTime.UtcNow;

            return new RefreshToken
            {
                UserId = userId,
                TokenHash = HashRefreshToken(refreshToken),
                CreatedAtUtc = createdAtUtc,
                ExpiresAtUtc = createdAtUtc.AddDays(this.GetRefreshTokenLifetimeDays()),
                CreatedByIp = createdByIp,
                UserAgent = userAgent,
            };
        }

        public List<Claim> GetUserClaimsFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken is not null ? jwtToken.Claims.ToList() : [];
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            var refreshTokenHash = HashRefreshToken(refreshToken);
            return await this.context.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == refreshTokenHash);
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
        {
            var storedRefreshToken = await this.GetRefreshTokenAsync(refreshToken);
            return storedRefreshToken is not null && this.IsRefreshTokenActive(storedRefreshToken);
        }

        public bool IsRefreshTokenActive(RefreshToken refreshToken)
        {
            return refreshToken.RevokedAtUtc is null && refreshToken.ExpiresAtUtc > DateTime.UtcNow;
        }

        public async Task<int> AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            this.context.RefreshTokens.Add(refreshToken);
            return await this.context.SaveChangesAsync();
        }

        public async Task<int> RevokeRefreshTokenAsync(
            string refreshToken,
            string? revokedByIp = null,
            string? replacedByRefreshToken = null)
        {
            var storedRefreshToken = await this.GetRefreshTokenAsync(refreshToken);
            if (storedRefreshToken is null)
            {
                return 0;
            }

            var hasChanges = false;

            if (storedRefreshToken.RevokedAtUtc is null)
            {
                storedRefreshToken.RevokedAtUtc = DateTime.UtcNow;
                storedRefreshToken.RevokedByIp = revokedByIp;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(replacedByRefreshToken)
                && string.IsNullOrWhiteSpace(storedRefreshToken.ReplacedByTokenHash))
            {
                storedRefreshToken.ReplacedByTokenHash = HashRefreshToken(replacedByRefreshToken);
                hasChanges = true;
            }

            return hasChanges ? await this.context.SaveChangesAsync() : 0;
        }

        public async Task<int> RevokeRefreshTokenFamilyAsync(string refreshToken, string? revokedByIp = null)
        {
            var storedRefreshToken = await this.GetRefreshTokenAsync(refreshToken);
            if (storedRefreshToken is null || string.IsNullOrWhiteSpace(storedRefreshToken.ReplacedByTokenHash))
            {
                return 0;
            }

            var revokedCount = 0;
            var nextTokenHash = storedRefreshToken.ReplacedByTokenHash;

            while (!string.IsNullOrWhiteSpace(nextTokenHash))
            {
                var descendantRefreshToken = await this.context.RefreshTokens
                    .FirstOrDefaultAsync(token => token.TokenHash == nextTokenHash);

                if (descendantRefreshToken is null)
                {
                    break;
                }

                if (descendantRefreshToken.RevokedAtUtc is null)
                {
                    descendantRefreshToken.RevokedAtUtc = DateTime.UtcNow;
                    descendantRefreshToken.RevokedByIp = revokedByIp;
                    revokedCount++;
                }

                nextTokenHash = descendantRefreshToken.ReplacedByTokenHash;
            }

            return revokedCount > 0 ? await this.context.SaveChangesAsync() : 0;
        }

        public string GenerateAccessToken(List<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.config["JWT:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddHours(2);
            var token = new JwtSecurityToken(
                this.config["JWT:Issuer"],
                this.config["JWT:Audience"],
                claims,
                expires: expiration,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private int GetRefreshTokenLifetimeDays()
        {
            return int.TryParse(this.config["Runtime:Security:RefreshTokenLifetimeDays"], out var configuredDays)
                   && configuredDays > 0
                ? configuredDays
                : RefreshTokenLifetimeDaysDefault;
        }

        private static string HashRefreshToken(string refreshToken)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        }
    }
}
