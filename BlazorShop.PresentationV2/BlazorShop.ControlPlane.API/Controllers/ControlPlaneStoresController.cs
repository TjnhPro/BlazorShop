namespace BlazorShop.ControlPlane.API.Controllers
{
    using System.Security.Claims;

    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.ControlPlane.Audit;
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.Application.ControlPlane.Stores;
    using BlazorShop.ControlPlane.API.Responses;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/control-plane/stores")]
    [Authorize(Policy = ControlPlanePolicyNames.StoresRead)]
    public sealed class ControlPlaneStoresController : ControllerBase
    {
        private readonly IControlPlaneStoreService storeService;
        private readonly IControlPlaneStoreDeploymentService deploymentService;
        private readonly IControlPlaneAuditService auditService;

        public ControlPlaneStoresController(
            IControlPlaneStoreService storeService,
            IControlPlaneStoreDeploymentService deploymentService,
            IControlPlaneAuditService auditService)
        {
            this.storeService = storeService;
            this.deploymentService = deploymentService;
            this.auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] Guid? nodePublicId,
            CancellationToken cancellationToken)
        {
            return ControlPlaneApiResponseWriter.Success(
                StatusCodes.Status200OK,
                await this.storeService.ListAsync(
                    new ControlPlaneStoreListQuery(search, status, nodePublicId),
                    cancellationToken),
                "Stores loaded.");
        }

        [HttpGet("{publicId:guid}")]
        public async Task<IActionResult> Get(Guid publicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.storeService.GetByPublicIdAsync(publicId, cancellationToken));
        }

        [HttpPost]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> Create(CreateControlPlaneStoreRequest request, CancellationToken cancellationToken)
        {
            var result = await this.storeService.CreateAsync(request, cancellationToken);
            await this.WriteStoreAuditAsync("stores.create", result, result.Payload, cancellationToken);

            return result.Success && result.Payload is not null
                ? CreatedAtAction(
                    nameof(Get),
                    new { publicId = result.Payload.PublicId },
                    ControlPlaneApiResponse<ControlPlaneStoreDetail>.Succeeded(result.Payload, "Store created."))
                : ToActionResult(result);
        }

        [HttpPut("{publicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> Update(Guid publicId, UpdateControlPlaneStoreRequest request, CancellationToken cancellationToken)
        {
            var result = await this.storeService.UpdateAsync(publicId, request, cancellationToken);
            await this.WriteStoreAuditAsync("stores.update", result, result.Payload, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/archive")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> Archive(Guid publicId, CancellationToken cancellationToken)
        {
            var result = await this.storeService.ArchiveAsync(publicId, cancellationToken);
            await this.WriteStoreAuditAsync("stores.archive", result, result.Payload, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/domains")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> AddDomain(Guid publicId, CreateControlPlaneStoreDomainRequest request, CancellationToken cancellationToken)
        {
            var result = await this.storeService.AddDomainAsync(publicId, request, cancellationToken);
            await this.WriteStoreAuditAsync("stores.domain.create", result, result.Payload, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/domains/{domainId:long}/verify")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> VerifyDomain(Guid publicId, long domainId, CancellationToken cancellationToken)
        {
            var result = await this.storeService.VerifyDomainAsync(publicId, domainId, cancellationToken);
            await this.WriteStoreAuditAsync("stores.domain.verify", result, result.Payload, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/domains/{domainId:long}/disable")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> DisableDomain(Guid publicId, long domainId, CancellationToken cancellationToken)
        {
            var result = await this.storeService.DisableDomainAsync(publicId, domainId, cancellationToken);
            await this.WriteStoreAuditAsync("stores.domain.disable", result, result.Payload, cancellationToken);
            return ToActionResult(result);
        }

        [HttpPost("{publicId:guid}/deployment-tasks")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> ProvisionStore(
            Guid publicId,
            DeployControlPlaneStoreRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.deploymentService.ProvisionAsync(publicId, request, cancellationToken);
            await this.WriteDeploymentAuditAsync("stores.deployment.submit", publicId, result.Success, result.Message, cancellationToken);
            return ToDeploymentActionResult(result);
        }

        [HttpGet("{publicId:guid}/deployment-tasks/{taskPublicId:guid}")]
        public async Task<IActionResult> GetDeploymentTask(
            Guid publicId,
            Guid taskPublicId,
            CancellationToken cancellationToken)
        {
            var result = await this.deploymentService.GetTaskAsync(publicId, taskPublicId, cancellationToken);
            return ToDeploymentActionResult(result);
        }

        [HttpPost("{publicId:guid}/deployment-tasks/{taskPublicId:guid}/cancel")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> CancelDeploymentTask(
            Guid publicId,
            Guid taskPublicId,
            CancelCommerceTaskRequest? request,
            CancellationToken cancellationToken)
        {
            var result = await this.deploymentService.CancelTaskAsync(
                publicId,
                taskPublicId,
                request ?? new CancelCommerceTaskRequest(),
                cancellationToken);
            await this.WriteDeploymentAuditAsync("stores.deployment.cancel", publicId, result.Success, result.Message, cancellationToken);
            return ToDeploymentActionResult(result);
        }

        [HttpPost("{publicId:guid}/deployment-tasks/{taskPublicId:guid}/retry")]
        [Authorize(Policy = ControlPlanePolicyNames.StoresWrite)]
        public async Task<IActionResult> RetryDeploymentTask(
            Guid publicId,
            Guid taskPublicId,
            RetryCommerceTaskRequest? request,
            CancellationToken cancellationToken)
        {
            var result = await this.deploymentService.RetryTaskAsync(
                publicId,
                taskPublicId,
                request ?? new RetryCommerceTaskRequest(),
                cancellationToken);
            await this.WriteDeploymentAuditAsync("stores.deployment.retry", publicId, result.Success, result.Message, cancellationToken);
            return ToDeploymentActionResult(result);
        }

        private IActionResult ToActionResult(ControlPlaneStoreOperationResult<ControlPlaneStoreDetail> result)
        {
            if (result.Success)
            {
                return ControlPlaneApiResponseWriter.Success(
                    StatusCodes.Status200OK,
                    result.Payload,
                    string.IsNullOrWhiteSpace(result.Message) ? "Store request completed." : result.Message);
            }

            return result.Failure switch
            {
                ControlPlaneStoreOperationFailure.NotFound => ControlPlaneApiResponseWriter.Failure<ControlPlaneStoreDetail>(StatusCodes.Status404NotFound, result.Message),
                ControlPlaneStoreOperationFailure.Conflict => ControlPlaneApiResponseWriter.Failure<ControlPlaneStoreDetail>(StatusCodes.Status409Conflict, result.Message),
                ControlPlaneStoreOperationFailure.Validation => ControlPlaneApiResponseWriter.Failure<ControlPlaneStoreDetail>(StatusCodes.Status400BadRequest, result.Message),
                _ => ControlPlaneApiResponseWriter.Failure<ControlPlaneStoreDetail>(StatusCodes.Status400BadRequest, result.Message)
            };
        }

        private static IActionResult ToDeploymentActionResult<TPayload>(
            ControlPlaneStoreDeploymentOperationResult<TPayload> result)
        {
            if (result.Success)
            {
                return ControlPlaneApiResponseWriter.Success(
                    StatusCodes.Status200OK,
                    result.Payload,
                    string.IsNullOrWhiteSpace(result.Message) ? "Deployment task request completed." : result.Message);
            }

            return result.Failure switch
            {
                ControlPlaneStoreDeploymentOperationFailure.NotFound => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status404NotFound, result.Message, result.Payload),
                ControlPlaneStoreDeploymentOperationFailure.Conflict => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status409Conflict, result.Message, result.Payload),
                ControlPlaneStoreDeploymentOperationFailure.Validation => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status400BadRequest, result.Message, result.Payload),
                ControlPlaneStoreDeploymentOperationFailure.RemoteFailure => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status502BadGateway, result.Message, result.Payload),
                _ => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status400BadRequest, result.Message, result.Payload)
            };
        }

        private async Task WriteStoreAuditAsync(
            string action,
            ControlPlaneStoreOperationResult<ControlPlaneStoreDetail> result,
            ControlPlaneStoreDetail? store,
            CancellationToken cancellationToken)
        {
            await this.auditService.WriteAsync(
                new ControlPlaneAuditEntry(
                    Action: action,
                    EntityType: "store_registry",
                    Result: result.Success ? "success" : "failure",
                    ActorIdentityUserId: User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("nameid") ?? User.FindFirstValue("sub"),
                    ActorEmail: User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email"),
                    EntityPublicId: store?.PublicId.ToString(),
                    MetadataJson: store is null ? null : $$"""{"storeKey":"{{store.StoreKey}}","nodePublicId":"{{store.NodePublicId}}"}""",
                    IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent: Request.Headers.UserAgent.ToString()),
                cancellationToken);
        }

        private async Task WriteDeploymentAuditAsync(
            string action,
            Guid storePublicId,
            bool success,
            string? message,
            CancellationToken cancellationToken)
        {
            await this.auditService.WriteAsync(
                new ControlPlaneAuditEntry(
                    Action: action,
                    EntityType: "store_deployment_task",
                    Result: success ? "success" : "failure",
                    ActorIdentityUserId: User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("nameid") ?? User.FindFirstValue("sub"),
                    ActorEmail: User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email"),
                    EntityPublicId: storePublicId.ToString(),
                    MetadataJson: string.IsNullOrWhiteSpace(message) ? null : $$"""{"message":"{{message.Replace("\"", "\\\"", StringComparison.Ordinal)}}"}""",
                    IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent: Request.Headers.UserAgent.ToString()),
                cancellationToken);
        }
    }
}
