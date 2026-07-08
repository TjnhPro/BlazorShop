namespace BlazorShop.ControlPlane.API.Controllers
{
    using System.Security.Claims;

    using BlazorShop.Application.ControlPlane.Audit;
    using BlazorShop.Application.ControlPlane.Credentials;
    using BlazorShop.Application.ControlPlane.Security;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/control-plane/nodes/{nodePublicId:guid}/credentials")]
    [Authorize(Policy = ControlPlanePolicyNames.CredentialsRotate)]
    public sealed class ControlPlaneCredentialsController : ControllerBase
    {
        private readonly IControlPlaneCredentialService credentialService;
        private readonly IControlPlaneProfileService profileService;
        private readonly IControlPlaneAuditService auditService;

        public ControlPlaneCredentialsController(
            IControlPlaneCredentialService credentialService,
            IControlPlaneProfileService profileService,
            IControlPlaneAuditService auditService)
        {
            this.credentialService = credentialService;
            this.profileService = profileService;
            this.auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> List(Guid nodePublicId, CancellationToken cancellationToken)
        {
            var result = await this.credentialService.ListAsync(nodePublicId, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            Guid nodePublicId,
            CreateControlPlaneCredentialRequest? request,
            CancellationToken cancellationToken)
        {
            var actor = await this.ResolveActorProfileAsync(cancellationToken);
            var result = await this.credentialService.CreateAsync(nodePublicId, actor.AdminUserId, cancellationToken);

            if (result.Success && result.Payload is not null)
            {
                await this.WriteCredentialAuditAsync("credentials.create", "success", nodePublicId, result.Payload.Credential.KeyId, actor, cancellationToken);
                await this.WriteCredentialAuditAsync("credentials.reveal", "success", nodePublicId, result.Payload.Credential.KeyId, actor, cancellationToken);
            }
            else
            {
                await this.WriteCredentialAuditAsync("credentials.create", "failure", nodePublicId, null, actor, cancellationToken);
            }

            return ToActionResult(result);
        }

        [HttpPost("{keyId}/revoke")]
        public async Task<IActionResult> Revoke(Guid nodePublicId, string keyId, CancellationToken cancellationToken)
        {
            var actor = await this.ResolveActorProfileAsync(cancellationToken);
            var result = await this.credentialService.RevokeAsync(nodePublicId, keyId, actor.AdminUserId, cancellationToken);

            await this.WriteCredentialAuditAsync(
                "credentials.revoke",
                result.Success ? "success" : "failure",
                nodePublicId,
                keyId,
                actor,
                cancellationToken);

            return ToActionResult(result);
        }

        [HttpPost("{keyId}/rotate")]
        public async Task<IActionResult> Rotate(Guid nodePublicId, string keyId, CancellationToken cancellationToken)
        {
            var actor = await this.ResolveActorProfileAsync(cancellationToken);
            var result = await this.credentialService.RotateAsync(nodePublicId, keyId, actor.AdminUserId, cancellationToken);

            if (result.Success && result.Payload is not null)
            {
                await this.WriteCredentialAuditAsync("credentials.rotate", "success", nodePublicId, keyId, actor, cancellationToken);
                await this.WriteCredentialAuditAsync("credentials.reveal", "success", nodePublicId, result.Payload.Credential.KeyId, actor, cancellationToken);
            }
            else
            {
                await this.WriteCredentialAuditAsync("credentials.rotate", "failure", nodePublicId, keyId, actor, cancellationToken);
            }

            return ToActionResult(result);
        }

        private IActionResult ToActionResult<TPayload>(ControlPlaneCredentialOperationResult<TPayload> result)
        {
            if (result.Success)
            {
                return Ok(result.Payload);
            }

            var body = new { message = result.Message };
            return result.Failure switch
            {
                ControlPlaneCredentialOperationFailure.NotFound => NotFound(body),
                ControlPlaneCredentialOperationFailure.Conflict => Conflict(body),
                ControlPlaneCredentialOperationFailure.Validation => BadRequest(body),
                _ => BadRequest(body)
            };
        }

        private async Task<ControlPlaneProfileResult> ResolveActorProfileAsync(CancellationToken cancellationToken)
        {
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("nameid") ?? User.FindFirstValue("sub");
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            var displayName = User.FindFirstValue("FullName") ?? User.FindFirstValue("name") ?? email;

            if (string.IsNullOrWhiteSpace(identityUserId) || string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Authenticated Control Plane token is missing identity claims.");
            }

            return await this.profileService.EnsureProfileAsync(identityUserId, email, displayName ?? email, cancellationToken);
        }

        private async Task WriteCredentialAuditAsync(
            string action,
            string result,
            Guid nodePublicId,
            string? keyId,
            ControlPlaneProfileResult actor,
            CancellationToken cancellationToken)
        {
            var metadataJson = string.IsNullOrWhiteSpace(keyId)
                ? $$"""{"nodePublicId":"{{nodePublicId}}"}"""
                : $$"""{"nodePublicId":"{{nodePublicId}}","keyId":"{{keyId}}"}""";

            await this.auditService.WriteAsync(
                new ControlPlaneAuditEntry(
                    Action: action,
                    EntityType: "credential",
                    Result: result,
                    ActorIdentityUserId: actor.IdentityUserId,
                    ActorEmail: actor.Email,
                    ActorAdminUserId: actor.AdminUserId,
                    EntityPublicId: keyId,
                    MetadataJson: metadataJson,
                    IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent: Request.Headers.UserAgent.ToString()),
                cancellationToken);
        }
    }
}
