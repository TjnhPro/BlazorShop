namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.ControlPlane.Stores;
    using BlazorShop.Domain.Entities.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    public sealed class ControlPlaneStoreDeploymentService : IControlPlaneStoreDeploymentService
    {
        private const string ControlApiEndpointKind = "control_api";
        private const string StoreCreateAndDeployTaskType = "store.create_and_deploy";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly ControlPlaneDbContext dbContext;
        private readonly ICommerceNodeTaskClient taskClient;

        public ControlPlaneStoreDeploymentService(
            ControlPlaneDbContext dbContext,
            ICommerceNodeTaskClient taskClient)
        {
            this.dbContext = dbContext;
            this.taskClient = taskClient;
        }

        public async Task<ApplicationResult<CommerceTaskSummary>> ProvisionAsync(
            Guid storePublicId,
            DeployControlPlaneStoreRequest request,
            CancellationToken cancellationToken = default)
        {
            var store = await this.LoadStoreAsync(storePublicId, cancellationToken);
            var validation = ValidateStoreForRemoteCall(store);
            if (validation is not null)
            {
                return validation.ToResult<CommerceTaskSummary>();
            }

            var payloadValidation = ValidateProvisionRequest(request);
            if (payloadValidation is not null)
            {
                return ValidationFailed<CommerceTaskSummary>(payloadValidation);
            }

            var primaryDomain = string.IsNullOrWhiteSpace(request.PrimaryDomain)
                ? SelectPrimaryDomain(store!)
                : request.PrimaryDomain.Trim();

            if (string.IsNullOrWhiteSpace(primaryDomain))
            {
                return ValidationFailed<CommerceTaskSummary>("Store must have a domain or PrimaryDomain must be provided.");
            }

            var baseUrl = string.IsNullOrWhiteSpace(request.BaseUrl)
                ? $"https://{NormalizeDomain(primaryDomain)}"
                : request.BaseUrl.Trim();

            var payloadJson = JsonSerializer.Serialize(
                new
                {
                    schemaVersion = "v1",
                    controlPlaneStorePublicId = store!.PublicId,
                    storeKey = store.StoreKey,
                    name = store.Name,
                    primaryDomain,
                    baseUrl,
                    defaultCurrencyCode = request.DefaultCurrencyCode.Trim().ToUpperInvariant(),
                    defaultCulture = request.DefaultCulture.Trim(),
                    storefrontImage = request.StorefrontImage.Trim(),
                    networkName = request.NetworkName,
                },
                SerializerOptions);

            var clientResult = await this.taskClient.EnqueueAsync(
                GetControlApiUrl(store.Node!),
                store.Node!.NodeKey,
                store.Node.NodeSecret!,
                new EnqueueCommerceTaskRequest(
                    StoreCreateAndDeployTaskType,
                    IdempotencyKey: $"store:{store.PublicId:D}:create-and-deploy",
                    PayloadSchemaVersion: "v1",
                    PayloadJson: payloadJson,
                    LockKey: $"store:{store.StoreKey}",
                    MaxAttempts: 1,
                    CreatedBy: "control-plane"),
                cancellationToken);

            var result = FromClientResult(clientResult);
            if (result.Success && result.Payload is not null)
            {
                await this.UpdateStoreDeploymentStatusAsync(store.Id, result.Payload.Status, cancellationToken);
            }

            return result;
        }

        public async Task<ApplicationResult<CommerceTaskDetail>> GetTaskAsync(
            Guid storePublicId,
            Guid taskPublicId,
            CancellationToken cancellationToken = default)
        {
            var store = await this.LoadStoreAsync(storePublicId, cancellationToken);
            var validation = ValidateStoreForRemoteCall(store);
            if (validation is not null)
            {
                return validation.ToResult<CommerceTaskDetail>();
            }

            var clientResult = await this.taskClient.GetAsync(
                GetControlApiUrl(store!.Node!),
                store.Node!.NodeKey,
                store.Node.NodeSecret!,
                taskPublicId,
                cancellationToken);

            var result = FromClientResult(clientResult);
            if (result.Success && result.Payload is not null)
            {
                await this.UpdateStoreDeploymentStatusAsync(store.Id, result.Payload.Status, cancellationToken);
            }

            return result;
        }

        public async Task<ApplicationResult<CommerceTaskDetail>> CancelTaskAsync(
            Guid storePublicId,
            Guid taskPublicId,
            CancelCommerceTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            var store = await this.LoadStoreAsync(storePublicId, cancellationToken);
            var validation = ValidateStoreForRemoteCall(store);
            if (validation is not null)
            {
                return validation.ToResult<CommerceTaskDetail>();
            }

            return FromClientResult(await this.taskClient.CancelAsync(
                GetControlApiUrl(store!.Node!),
                store.Node!.NodeKey,
                store.Node.NodeSecret!,
                taskPublicId,
                request,
                cancellationToken));
        }

        public async Task<ApplicationResult<CommerceTaskDetail>> RetryTaskAsync(
            Guid storePublicId,
            Guid taskPublicId,
            RetryCommerceTaskRequest request,
            CancellationToken cancellationToken = default)
        {
            var store = await this.LoadStoreAsync(storePublicId, cancellationToken);
            var validation = ValidateStoreForRemoteCall(store);
            if (validation is not null)
            {
                return validation.ToResult<CommerceTaskDetail>();
            }

            return FromClientResult(await this.taskClient.RetryAsync(
                GetControlApiUrl(store!.Node!),
                store.Node!.NodeKey,
                store.Node.NodeSecret!,
                taskPublicId,
                request,
                cancellationToken));
        }

        private async Task<StoreRegistry?> LoadStoreAsync(Guid publicId, CancellationToken cancellationToken)
        {
            return await this.dbContext.Stores
                .AsNoTracking()
                .Include(store => store.Node)
                    .ThenInclude(node => node!.Endpoints)
                .Include(store => store.Domains)
                .FirstOrDefaultAsync(store => store.PublicId == publicId, cancellationToken);
        }

        private async Task UpdateStoreDeploymentStatusAsync(long storeId, string taskStatus, CancellationToken cancellationToken)
        {
            var status = taskStatus switch
            {
                "succeeded" => ControlPlaneStoreStatuses.Active,
                "failed" => ControlPlaneStoreStatuses.Disabled,
                "cancelled" => ControlPlaneStoreStatuses.Disabled,
                _ => ControlPlaneStoreStatuses.Provisioning
            };

            var store = await this.dbContext.Stores.FirstOrDefaultAsync(store => store.Id == storeId, cancellationToken);
            if (store is null || store.Status == ControlPlaneStoreStatuses.Archived || store.Status == status)
            {
                return;
            }

            store.Status = status;
            store.UpdatedAt = DateTimeOffset.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private static StoreValidationFailure? ValidateStoreForRemoteCall(StoreRegistry? store)
        {
            if (store is null)
            {
                return new StoreValidationFailure(ApplicationErrorKind.NotFound, "Store was not found.");
            }

            if (store.Status == ControlPlaneStoreStatuses.Archived)
            {
                return new StoreValidationFailure(ApplicationErrorKind.Validation, "Archived stores cannot be deployed.");
            }

            if (store.Node is null || store.Node.Status == "disabled")
            {
                return new StoreValidationFailure(ApplicationErrorKind.Validation, "Store node is missing or disabled.");
            }

            if (string.IsNullOrWhiteSpace(store.Node.NodeSecret))
            {
                return new StoreValidationFailure(ApplicationErrorKind.Validation, "Store node does not have a node secret configured.");
            }

            if (string.IsNullOrWhiteSpace(GetControlApiUrl(store.Node)))
            {
                return new StoreValidationFailure(ApplicationErrorKind.Validation, "Store node does not have an active Control API endpoint.");
            }

            return null;
        }

        private static string? ValidateProvisionRequest(DeployControlPlaneStoreRequest request)
        {
            if (request is null)
            {
                return "Request body is required.";
            }

            if (string.IsNullOrWhiteSpace(request.StorefrontImage))
            {
                return "Storefront image is required.";
            }

            if (string.IsNullOrWhiteSpace(request.DefaultCurrencyCode) || request.DefaultCurrencyCode.Trim().Length != 3)
            {
                return "Default currency code must be a 3-letter code.";
            }

            if (string.IsNullOrWhiteSpace(request.DefaultCulture))
            {
                return "Default culture is required.";
            }

            return null;
        }

        private static string? SelectPrimaryDomain(StoreRegistry store)
        {
            return store.Domains
                .Where(domain => domain.DisabledAt is null)
                .OrderByDescending(domain => domain.Status == "verified")
                .ThenBy(domain => domain.Id)
                .Select(domain => domain.NormalizedDomain)
                .FirstOrDefault();
        }

        private static string GetControlApiUrl(CommerceNode node)
        {
            return node.Endpoints.FirstOrDefault(endpoint =>
                endpoint.Kind == ControlApiEndpointKind &&
                endpoint.IsPrimary &&
                endpoint.DisabledAt is null)?.Url ?? string.Empty;
        }

        private static string NormalizeDomain(string domain)
        {
            var normalized = domain.Trim().ToLowerInvariant();
            if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            {
                normalized = uri.Host;
            }

            return normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? normalized;
        }

        private static ApplicationResult<TPayload> FromClientResult<TPayload>(
            CommerceNodeTaskClientResult<TPayload> clientResult)
        {
            if (clientResult.Success)
            {
                return new ApplicationResult<TPayload>(
                    true,
                    clientResult.Message,
                    clientResult.Payload);
            }

            return new ApplicationResult<TPayload>(
                false,
                clientResult.Message,
                clientResult.Payload,
                ApplicationErrorKind.RemoteFailure);
        }

        private static ApplicationResult<TPayload> ValidationFailed<TPayload>(string message)
        {
            return new ApplicationResult<TPayload>(
                false,
                message,
                Failure: ApplicationErrorKind.Validation);
        }

        private sealed record StoreValidationFailure(
            ApplicationErrorKind Failure,
            string Message)
        {
            public ApplicationResult<TPayload> ToResult<TPayload>()
            {
                return new ApplicationResult<TPayload>(
                    false,
                    this.Message,
                    Failure: this.Failure);
            }
        }
    }
}
