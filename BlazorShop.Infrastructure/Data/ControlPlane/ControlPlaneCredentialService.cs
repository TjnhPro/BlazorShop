namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Security.Cryptography;
    using System.Text;

    using BlazorShop.Application.ControlPlane.Credentials;
    using BlazorShop.Domain.Entities.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    public sealed class ControlPlaneCredentialService : IControlPlaneCredentialService
    {
        private const string ActiveStatus = "active";
        private const string RevokedStatus = "revoked";
        private const string RotatedStatus = "rotated";
        private const string HashAlgorithm = "sha256";
        private const string SecretPrefix = "bs_cp_";

        private readonly ControlPlaneDbContext dbContext;

        public ControlPlaneCredentialService(ControlPlaneDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<ControlPlaneCredentialOperationResult<ControlPlaneCredentialListResponse>> ListAsync(
            Guid nodePublicId,
            CancellationToken cancellationToken = default)
        {
            var node = await this.dbContext.Nodes
                .AsNoTracking()
                .Include(candidate => candidate.Credentials)
                .FirstOrDefaultAsync(candidate => candidate.PublicId == nodePublicId, cancellationToken);

            if (node is null)
            {
                return NotFound<ControlPlaneCredentialListResponse>("Node was not found.");
            }

            var credentials = node.Credentials
                .OrderByDescending(credential => credential.CreatedAt)
                .Select(MapSummary)
                .ToArray();

            return Succeeded(new ControlPlaneCredentialListResponse(credentials));
        }

        public async Task<ControlPlaneCredentialOperationResult<ControlPlaneCredentialSecretResult>> CreateAsync(
            Guid nodePublicId,
            long? actorAdminUserId = null,
            CancellationToken cancellationToken = default)
        {
            var node = await this.dbContext.Nodes.FirstOrDefaultAsync(candidate => candidate.PublicId == nodePublicId, cancellationToken);
            if (node is null)
            {
                return NotFound<ControlPlaneCredentialSecretResult>("Node was not found.");
            }

            if (node.Status == "disabled")
            {
                return ValidationFailed<ControlPlaneCredentialSecretResult>("Credentials cannot be created for disabled nodes.");
            }

            var credential = CreateCredential(node.Id, actorAdminUserId, out var rawSecret);
            this.dbContext.NodeCredentials.Add(credential);
            await this.dbContext.SaveChangesAsync(cancellationToken);

            return Succeeded(new ControlPlaneCredentialSecretResult(MapSummary(credential), rawSecret));
        }

        public async Task<ControlPlaneCredentialOperationResult<ControlPlaneCredentialSummary>> RevokeAsync(
            Guid nodePublicId,
            string keyId,
            long? actorAdminUserId = null,
            CancellationToken cancellationToken = default)
        {
            var credential = await this.LoadCredentialAsync(nodePublicId, keyId, cancellationToken);
            if (credential is null)
            {
                return NotFound<ControlPlaneCredentialSummary>("Credential was not found.");
            }

            if (credential.Status == RevokedStatus)
            {
                return Succeeded(MapSummary(credential));
            }

            var now = DateTimeOffset.UtcNow;
            credential.Status = RevokedStatus;
            credential.RevokedAt = now;
            credential.RevokedByAdminUserId = actorAdminUserId;
            await this.dbContext.SaveChangesAsync(cancellationToken);

            return Succeeded(MapSummary(credential));
        }

        public async Task<ControlPlaneCredentialOperationResult<ControlPlaneCredentialSecretResult>> RotateAsync(
            Guid nodePublicId,
            string keyId,
            long? actorAdminUserId = null,
            CancellationToken cancellationToken = default)
        {
            var credential = await this.LoadCredentialAsync(nodePublicId, keyId, cancellationToken);
            if (credential is null)
            {
                return NotFound<ControlPlaneCredentialSecretResult>("Credential was not found.");
            }

            if (credential.Node?.Status == "disabled")
            {
                return ValidationFailed<ControlPlaneCredentialSecretResult>("Credentials cannot be rotated for disabled nodes.");
            }

            if (credential.Status != ActiveStatus)
            {
                return Conflict<ControlPlaneCredentialSecretResult>("Only active credentials can be rotated.");
            }

            var now = DateTimeOffset.UtcNow;
            credential.Status = RotatedStatus;
            credential.RevokedAt = now;
            credential.RevokedByAdminUserId = actorAdminUserId;

            var replacement = CreateCredential(credential.NodeId, actorAdminUserId, out var rawSecret);
            this.dbContext.NodeCredentials.Add(replacement);
            await this.dbContext.SaveChangesAsync(cancellationToken);

            return Succeeded(new ControlPlaneCredentialSecretResult(MapSummary(replacement), rawSecret));
        }

        public async Task<bool> VerifyAsync(
            string keyId,
            string rawSecret,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(keyId) || string.IsNullOrWhiteSpace(rawSecret))
            {
                return false;
            }

            var credential = await this.dbContext.NodeCredentials
                .AsNoTracking()
                .Include(candidate => candidate.Node)
                .FirstOrDefaultAsync(
                    candidate => candidate.KeyId == keyId.Trim()
                                 && candidate.Status == ActiveStatus
                                 && candidate.RevokedAt == null,
                    cancellationToken);

            if (credential is null || credential.Node?.Status == "disabled")
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(credential.SecretHash),
                Encoding.UTF8.GetBytes(HashSecret(rawSecret.Trim())));
        }

        private async Task<CommerceNodeCredential?> LoadCredentialAsync(
            Guid nodePublicId,
            string keyId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(keyId))
            {
                return null;
            }

            return await this.dbContext.NodeCredentials
                .Include(credential => credential.Node)
                .FirstOrDefaultAsync(
                    credential => credential.Node != null
                                  && credential.Node.PublicId == nodePublicId
                                  && credential.KeyId == keyId.Trim(),
                    cancellationToken);
        }

        private static CommerceNodeCredential CreateCredential(long nodeId, long? actorAdminUserId, out string rawSecret)
        {
            var now = DateTimeOffset.UtcNow;
            rawSecret = $"{SecretPrefix}{ToBase64Url(RandomNumberGenerator.GetBytes(32))}";

            return new CommerceNodeCredential
            {
                NodeId = nodeId,
                KeyId = $"cpk_{ToBase64Url(RandomNumberGenerator.GetBytes(12))}",
                SecretHash = HashSecret(rawSecret),
                HashAlgorithm = HashAlgorithm,
                Status = ActiveStatus,
                CreatedAt = now,
                RevealedAt = now,
                CreatedByAdminUserId = actorAdminUserId
            };
        }

        private static ControlPlaneCredentialSummary MapSummary(CommerceNodeCredential credential)
        {
            return new ControlPlaneCredentialSummary(
                credential.KeyId,
                credential.Status,
                credential.HashAlgorithm,
                credential.CreatedAt,
                credential.RevealedAt,
                credential.RevokedAt);
        }

        private static string HashSecret(string rawSecret)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawSecret))).ToLowerInvariant();
        }

        private static string ToBase64Url(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static ControlPlaneCredentialOperationResult<TPayload> Succeeded<TPayload>(TPayload payload)
        {
            return new ControlPlaneCredentialOperationResult<TPayload>(true, Payload: payload);
        }

        private static ControlPlaneCredentialOperationResult<TPayload> ValidationFailed<TPayload>(string message)
        {
            return new ControlPlaneCredentialOperationResult<TPayload>(false, message, Failure: ControlPlaneCredentialOperationFailure.Validation);
        }

        private static ControlPlaneCredentialOperationResult<TPayload> Conflict<TPayload>(string message)
        {
            return new ControlPlaneCredentialOperationResult<TPayload>(false, message, Failure: ControlPlaneCredentialOperationFailure.Conflict);
        }

        private static ControlPlaneCredentialOperationResult<TPayload> NotFound<TPayload>(string message)
        {
            return new ControlPlaneCredentialOperationResult<TPayload>(false, message, Failure: ControlPlaneCredentialOperationFailure.NotFound);
        }
    }
}
