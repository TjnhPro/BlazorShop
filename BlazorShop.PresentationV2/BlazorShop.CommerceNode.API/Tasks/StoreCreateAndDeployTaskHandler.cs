namespace BlazorShop.CommerceNode.API.Tasks
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.CommerceNode.API.Deployment;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class StoreCreateAndDeployTaskHandler : ICommerceTaskHandler
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly CommerceNodeDbContext context;
        private readonly IStorefrontDockerDeploymentService dockerDeployment;
        private readonly INginxDeploymentService nginxDeployment;

        public StoreCreateAndDeployTaskHandler(
            CommerceNodeDbContext context,
            IStorefrontDockerDeploymentService dockerDeployment,
            INginxDeploymentService nginxDeployment)
        {
            this.context = context;
            this.dockerDeployment = dockerDeployment;
            this.nginxDeployment = nginxDeployment;
        }

        public string TaskType => "store.create_and_deploy";

        public async Task<CommerceTaskHandlerResult> ExecuteAsync(
            CommerceTaskHandlerContext context,
            CancellationToken cancellationToken)
        {
            StoreCreateAndDeployPayload payload;
            try
            {
                payload = JsonSerializer.Deserialize<StoreCreateAndDeployPayload>(
                    context.PayloadJson,
                    SerializerOptions) ?? new StoreCreateAndDeployPayload();
            }
            catch (JsonException)
            {
                return CommerceTaskHandlerResult.Failed(
                    "Task payload is not valid JSON.",
                    "invalid_payload_json");
            }

            var validationError = ValidatePayload(payload);
            if (validationError is not null)
            {
                return CommerceTaskHandlerResult.Failed(validationError, "invalid_store_deploy_payload");
            }

            var now = DateTimeOffset.UtcNow;
            var normalizedDomain = NormalizeDomain(payload.PrimaryDomain!);

            var store = await this.context.CommerceStores
                .Include(entity => entity.Domains)
                .FirstOrDefaultAsync(
                    entity => entity.StoreKey == payload.StoreKey ||
                              (payload.ControlPlaneStorePublicId != null &&
                               entity.ControlPlaneStorePublicId == payload.ControlPlaneStorePublicId),
                    cancellationToken);

            if (store is null)
            {
                store = new CommerceStore
                {
                    Id = Guid.NewGuid(),
                    PublicId = Guid.NewGuid(),
                    CreatedAt = now,
                };
                this.context.CommerceStores.Add(store);
            }

            store.ControlPlaneStorePublicId = payload.ControlPlaneStorePublicId;
            store.StoreKey = payload.StoreKey!.Trim();
            store.Name = payload.Name!.Trim();
            store.Status = CommerceStoreStatuses.Disabled;
            store.BaseUrl = payload.BaseUrl?.Trim();
            store.DefaultCurrencyCode = payload.DefaultCurrencyCode!.Trim().ToUpperInvariant();
            store.DefaultCulture = payload.DefaultCulture!.Trim();
            store.UpdatedAt = now;

            foreach (var existingPrimaryDomain in store.Domains.Where(domain => domain.IsPrimary && domain.NormalizedDomain != normalizedDomain))
            {
                existingPrimaryDomain.IsPrimary = false;
                existingPrimaryDomain.UpdatedAt = now;
            }

            var domain = store.Domains.FirstOrDefault(entity => entity.NormalizedDomain == normalizedDomain);
            if (domain is null)
            {
                domain = new CommerceStoreDomain
                {
                    Id = Guid.NewGuid(),
                    Store = store,
                    CreatedAt = now,
                };
                store.Domains.Add(domain);
            }

            domain.Domain = payload.PrimaryDomain!.Trim();
            domain.NormalizedDomain = normalizedDomain;
            domain.IsPrimary = true;
            domain.Status = CommerceStoreDomainStatuses.Pending;
            domain.DisabledAt = null;
            domain.UpdatedAt = now;

            await this.context.SaveChangesAsync(cancellationToken);

            var deployment = await this.context.StoreDeployments
                .FirstOrDefaultAsync(entity => entity.StoreId == store.Id, cancellationToken);

            if (deployment is null)
            {
                deployment = new StoreDeployment
                {
                    Id = Guid.NewGuid(),
                    StoreId = store.Id,
                    CreatedAt = now,
                };
                this.context.StoreDeployments.Add(deployment);
            }

            deployment.TaskId = context.TaskId;
            deployment.StorefrontImage = payload.StorefrontImage!.Trim();
            deployment.Status = StoreDeploymentStatuses.Provisioning;
            deployment.UpdatedAt = now;
            await this.context.SaveChangesAsync(cancellationToken);

            StorefrontContainerPlan? containerPlan = null;
            NginxStoreProxyPlan? nginxPlan = null;

            try
            {
                var deploymentRequest = new StorefrontDeploymentRequest(
                    store.Id,
                    store.StoreKey,
                    payload.StorefrontImage!.Trim(),
                    payload.NetworkName,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["STORE_PUBLIC_ID"] = store.PublicId.ToString("D"),
                        ["STORE_BASE_URL"] = store.BaseUrl ?? string.Empty,
                        ["STORE_PRIMARY_DOMAIN"] = normalizedDomain,
                        ["DEFAULT_CURRENCY_CODE"] = store.DefaultCurrencyCode,
                        ["DEFAULT_CULTURE"] = store.DefaultCulture,
                    });

                containerPlan = this.dockerDeployment.CreatePlan(deploymentRequest);
                var envFilePath = await this.dockerDeployment.RenderEnvironmentFileAsync(
                    containerPlan,
                    deploymentRequest,
                    cancellationToken);

                deployment.ContainerName = containerPlan.ContainerName;
                deployment.NetworkName = containerPlan.NetworkName;
                deployment.InternalUrl = containerPlan.InternalUrl;
                deployment.PublicUrl = store.BaseUrl;
                deployment.EnvFilePath = envFilePath;
                deployment.UpdatedAt = DateTimeOffset.UtcNow;
                await this.context.SaveChangesAsync(cancellationToken);

                var createContainer = await this.dockerDeployment.CreateOrUpdateContainerAsync(containerPlan, cancellationToken);
                if (!createContainer.Success)
                {
                    return await this.FailDeploymentAsync(
                        store,
                        deployment,
                        containerPlan,
                        null,
                        "Docker container could not be created.",
                        "docker_create_failed",
                        cancellationToken);
                }

                var startContainer = await this.dockerDeployment.StartContainerAsync(containerPlan.ContainerName, cancellationToken);
                if (!startContainer.Success)
                {
                    return await this.FailDeploymentAsync(
                        store,
                        deployment,
                        containerPlan,
                        null,
                        "Docker container could not be started.",
                        "docker_start_failed",
                        cancellationToken);
                }

                nginxPlan = this.nginxDeployment.CreatePlan(
                    new NginxStoreProxyRequest(store.StoreKey, normalizedDomain, containerPlan.InternalUrl));
                var nginxConfigPath = await this.nginxDeployment.RenderConfigAsync(nginxPlan, cancellationToken);
                deployment.NginxServerName = nginxPlan.ServerName;
                deployment.NginxConfigPath = nginxConfigPath;
                deployment.UpdatedAt = DateTimeOffset.UtcNow;
                await this.context.SaveChangesAsync(cancellationToken);

                var validateNginx = await this.nginxDeployment.ValidateConfigAsync(cancellationToken);
                if (!validateNginx.Success)
                {
                    return await this.FailDeploymentAsync(
                        store,
                        deployment,
                        containerPlan,
                        nginxPlan,
                        "Nginx config validation failed.",
                        "nginx_validate_failed",
                        cancellationToken);
                }

                var reloadNginx = await this.nginxDeployment.ReloadAsync(cancellationToken);
                if (!reloadNginx.Success)
                {
                    return await this.FailDeploymentAsync(
                        store,
                        deployment,
                        containerPlan,
                        nginxPlan,
                        "Nginx reload failed.",
                        "nginx_reload_failed",
                        cancellationToken);
                }

                var health = await this.dockerDeployment.ProbeHealthAsync(containerPlan, cancellationToken);
                deployment.LastHealthStatus = health.Healthy ? "healthy" : "unhealthy";
                deployment.LastHealthAt = DateTimeOffset.UtcNow;
                if (!health.Healthy)
                {
                    return await this.FailDeploymentAsync(
                        store,
                        deployment,
                        containerPlan,
                        nginxPlan,
                        $"Storefront health check failed: {health.Message}",
                        "storefront_health_failed",
                        cancellationToken);
                }

                store.Status = CommerceStoreStatuses.Active;
                store.UpdatedAt = DateTimeOffset.UtcNow;
                domain.Status = CommerceStoreDomainStatuses.Verified;
                domain.VerifiedAt = DateTimeOffset.UtcNow;
                domain.UpdatedAt = DateTimeOffset.UtcNow;
                deployment.Status = StoreDeploymentStatuses.Active;
                deployment.DeployedAt = DateTimeOffset.UtcNow;
                deployment.UpdatedAt = DateTimeOffset.UtcNow;

                await this.context.SaveChangesAsync(cancellationToken);

                var resultJson = JsonSerializer.Serialize(
                    new
                    {
                        store.PublicId,
                        store.StoreKey,
                        deployment.ContainerName,
                        deployment.InternalUrl,
                        deployment.PublicUrl,
                        deployment.NginxServerName,
                    },
                    SerializerOptions);

                return CommerceTaskHandlerResult.Succeeded("Store created and deployed.", resultJson);
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException)
            {
                return await this.FailDeploymentAsync(
                    store,
                    deployment,
                    containerPlan,
                    nginxPlan,
                    ex.Message,
                    "store_deploy_failed",
                    cancellationToken);
            }
        }

        private async Task<CommerceTaskHandlerResult> FailDeploymentAsync(
            CommerceStore store,
            StoreDeployment deployment,
            StorefrontContainerPlan? containerPlan,
            NginxStoreProxyPlan? nginxPlan,
            string message,
            string errorCode,
            CancellationToken cancellationToken)
        {
            if (nginxPlan is not null)
            {
                await this.nginxDeployment.RemoveConfigAsync(nginxPlan, cancellationToken);
                await this.nginxDeployment.ReloadAsync(cancellationToken);
            }

            if (containerPlan is not null)
            {
                await this.dockerDeployment.StopContainerAsync(containerPlan.ContainerName, cancellationToken);
                await this.dockerDeployment.RemoveContainerAsync(containerPlan.ContainerName, cancellationToken);
            }

            store.Status = CommerceStoreStatuses.Disabled;
            store.UpdatedAt = DateTimeOffset.UtcNow;
            deployment.Status = StoreDeploymentStatuses.Failed;
            deployment.LastHealthStatus ??= "failed";
            deployment.UpdatedAt = DateTimeOffset.UtcNow;
            await this.context.SaveChangesAsync(cancellationToken);

            return CommerceTaskHandlerResult.Failed(message, errorCode);
        }

        private static string? ValidatePayload(StoreCreateAndDeployPayload payload)
        {
            if (!string.Equals(payload.SchemaVersion, "v1", StringComparison.OrdinalIgnoreCase))
            {
                return "Payload schemaVersion must be v1.";
            }

            if (string.IsNullOrWhiteSpace(payload.StoreKey))
            {
                return "Store key is required.";
            }

            if (string.IsNullOrWhiteSpace(payload.Name))
            {
                return "Store name is required.";
            }

            if (string.IsNullOrWhiteSpace(payload.PrimaryDomain))
            {
                return "Primary domain is required.";
            }

            if (string.IsNullOrWhiteSpace(payload.DefaultCurrencyCode) || payload.DefaultCurrencyCode.Trim().Length != 3)
            {
                return "Default currency code must be a 3-letter code.";
            }

            if (string.IsNullOrWhiteSpace(payload.DefaultCulture))
            {
                return "Default culture is required.";
            }

            if (string.IsNullOrWhiteSpace(payload.StorefrontImage))
            {
                return "Storefront image is required.";
            }

            return null;
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

        private sealed class StoreCreateAndDeployPayload
        {
            public string SchemaVersion { get; set; } = "v1";

            public Guid? ControlPlaneStorePublicId { get; set; }

            public string? StoreKey { get; set; }

            public string? Name { get; set; }

            public string? PrimaryDomain { get; set; }

            public string? BaseUrl { get; set; }

            public string? DefaultCurrencyCode { get; set; }

            public string? DefaultCulture { get; set; }

            public string? StorefrontImage { get; set; }

            public string? NetworkName { get; set; }
        }
    }
}
