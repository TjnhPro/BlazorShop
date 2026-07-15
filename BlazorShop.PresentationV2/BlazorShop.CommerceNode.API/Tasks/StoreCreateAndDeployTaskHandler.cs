namespace BlazorShop.CommerceNode.API.Tasks
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.CommerceNode.API.Configuration;
    using BlazorShop.CommerceNode.API.Deployment;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;

    public sealed class StoreCreateAndDeployTaskHandler : ICommerceTaskHandler
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly CommerceNodeDbContext context;
        private readonly IStorefrontDockerDeploymentService dockerDeployment;
        private readonly INginxDeploymentService nginxDeployment;
        private readonly StorefrontDeploymentOptions deploymentOptions;

        public StoreCreateAndDeployTaskHandler(
            CommerceNodeDbContext context,
            IStorefrontDockerDeploymentService dockerDeployment,
            INginxDeploymentService nginxDeployment,
            IOptions<StorefrontDeploymentOptions> deploymentOptions)
        {
            this.context = context;
            this.dockerDeployment = dockerDeployment;
            this.nginxDeployment = nginxDeployment;
            this.deploymentOptions = deploymentOptions.Value;
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
            var imageResolution = await this.ResolveStorefrontImageAsync(payload.StorefrontImage, cancellationToken);
            if (!imageResolution.Success)
            {
                return CommerceTaskHandlerResult.Failed(
                    imageResolution.Message,
                    "storefront_image_not_allowed");
            }

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
            store.Status = CommerceStoreStatuses.Provisioning;
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
            deployment.StorefrontImage = imageResolution.Image!;
            deployment.Status = StoreDeploymentStatuses.Provisioning;
            deployment.UpdatedAt = now;
            await this.context.SaveChangesAsync(cancellationToken);

            StorefrontContainerPlan? containerPlan = null;
            NginxStoreProxyPlan? nginxPlan = null;

            try
            {
                var publicBaseUrl = string.IsNullOrWhiteSpace(store.BaseUrl)
                    ? $"https://{normalizedDomain}"
                    : store.BaseUrl.Trim();
                var apiBaseUrl = ResolveOptionalValue(payload.StorefrontApiBaseUrl, this.deploymentOptions.StorefrontApiBaseUrl);
                var clientAppBaseUrl = ResolveOptionalValue(payload.ClientAppBaseUrl, this.deploymentOptions.ClientAppBaseUrl) ?? publicBaseUrl;
                var publicUrlBaseUrl = ResolveOptionalValue(payload.PublicUrlBaseUrl, publicBaseUrl) ?? publicBaseUrl;
                var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["STORE_PUBLIC_ID"] = store.PublicId.ToString("D"),
                    ["STORE_BASE_URL"] = publicBaseUrl,
                    ["STORE_PRIMARY_DOMAIN"] = normalizedDomain,
                    ["DEFAULT_CURRENCY_CODE"] = store.DefaultCurrencyCode,
                    ["DEFAULT_CULTURE"] = store.DefaultCulture,
                    ["ClientApp__BaseUrl"] = clientAppBaseUrl,
                    ["PublicUrl__BaseUrl"] = publicUrlBaseUrl,
                };

                if (!string.IsNullOrWhiteSpace(apiBaseUrl))
                {
                    environment["Api__BaseUrl"] = apiBaseUrl;
                }

                var deploymentRequest = new StorefrontDeploymentRequest(
                    store.Id,
                    store.PublicId,
                    context.PublicId,
                    store.StoreKey,
                    imageResolution.Image!,
                    payload.NetworkName,
                    environment);

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
                        BuildCommandFailureMessage("Docker container could not be created.", createContainer),
                        "docker_create_failed",
                        cancellationToken);
                }

                var startContainer = await this.dockerDeployment.StartContainerAsync(containerPlan, cancellationToken);
                if (!startContainer.Success)
                {
                    return await this.FailDeploymentAsync(
                        store,
                        deployment,
                        containerPlan,
                        null,
                        BuildCommandFailureMessage("Docker container could not be started.", startContainer),
                        "docker_start_failed",
                        cancellationToken);
                }

                var startupHealth = await this.WaitForHealthyStorefrontAsync(containerPlan, cancellationToken);
                deployment.LastHealthStatus = startupHealth.Healthy ? "healthy" : "unhealthy";
                deployment.LastHealthAt = DateTimeOffset.UtcNow;
                deployment.UpdatedAt = DateTimeOffset.UtcNow;
                await this.context.SaveChangesAsync(cancellationToken);
                if (!startupHealth.Healthy)
                {
                    return await this.FailDeploymentAsync(
                        store,
                        deployment,
                        containerPlan,
                        null,
                        $"Storefront health check failed: {startupHealth.Message}",
                        "storefront_health_failed",
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
                        BuildNginxFailureMessage("Nginx config validation failed.", validateNginx),
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
                        BuildNginxFailureMessage("Nginx reload failed.", reloadNginx),
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
                await this.dockerDeployment.StopContainerAsync(containerPlan, cancellationToken);
                await this.dockerDeployment.RemoveContainerAsync(containerPlan, cancellationToken);
            }

            store.Status = CommerceStoreStatuses.Disabled;
            store.UpdatedAt = DateTimeOffset.UtcNow;
            deployment.Status = StoreDeploymentStatuses.Failed;
            deployment.LastHealthStatus ??= "failed";
            deployment.UpdatedAt = DateTimeOffset.UtcNow;
            await this.context.SaveChangesAsync(cancellationToken);

            return CommerceTaskHandlerResult.Failed(message, errorCode);
        }

        private async Task<StorefrontHealthProbeResult> WaitForHealthyStorefrontAsync(
            StorefrontContainerPlan containerPlan,
            CancellationToken cancellationToken)
        {
            var attempts = Math.Max(1, this.deploymentOptions.HealthProbeAttempts);
            var delay = TimeSpan.FromSeconds(Math.Max(1, this.deploymentOptions.HealthProbeDelaySeconds));
            StorefrontHealthProbeResult? lastResult = null;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                lastResult = await this.dockerDeployment.ProbeHealthAsync(containerPlan, cancellationToken);
                if (lastResult.Healthy || attempt == attempts)
                {
                    return lastResult;
                }

                await Task.Delay(delay, cancellationToken);
            }

            return lastResult ?? new StorefrontHealthProbeResult(false, null, "Storefront health check did not run.");
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

            return null;
        }

        private async Task<StorefrontImageResolution> ResolveStorefrontImageAsync(
            string? imageSelector,
            CancellationToken cancellationToken)
        {
            var enabledImages = await this.context.StorefrontDeploymentImages
                .AsNoTracking()
                .Where(image => image.IsEnabled)
                .OrderByDescending(image => image.IsDefault)
                .ThenBy(image => image.Key)
                .ToListAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(imageSelector))
            {
                var selector = imageSelector.Trim();
                var configuredImage = enabledImages.FirstOrDefault(
                    image =>
                        string.Equals(image.Key, selector, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(image.Image, selector, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(image.Version, selector, StringComparison.OrdinalIgnoreCase));

                if (configuredImage is not null)
                {
                    return StorefrontImageResolution.Succeeded(configuredImage.Image);
                }

                if (this.deploymentOptions.AllowedImages.Contains(selector, StringComparer.OrdinalIgnoreCase))
                {
                    return StorefrontImageResolution.Succeeded(selector);
                }

                return StorefrontImageResolution.Failed("Storefront image is not configured for this Commerce Node.");
            }

            var defaultImage = enabledImages.FirstOrDefault(image => image.IsDefault) ?? enabledImages.FirstOrDefault();
            if (defaultImage is not null)
            {
                return StorefrontImageResolution.Succeeded(defaultImage.Image);
            }

            var fallbackImage = this.deploymentOptions.AllowedImages.FirstOrDefault();
            return string.IsNullOrWhiteSpace(fallbackImage)
                ? StorefrontImageResolution.Failed("No Storefront image is configured for this Commerce Node.")
                : StorefrontImageResolution.Succeeded(fallbackImage);
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

        private static string? ResolveOptionalValue(params string?[] values)
        {
            return values
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
                ?.Trim();
        }

        private static string BuildCommandFailureMessage(
            string message,
            StorefrontDeploymentCommandResult result)
        {
            return AppendCommandOutput($"{message} {result.Message}", result.StandardError, result.StandardOutput);
        }

        private static string BuildNginxFailureMessage(
            string message,
            NginxDeploymentCommandResult result)
        {
            return AppendCommandOutput(message, result.StandardError, result.StandardOutput);
        }

        private static string AppendCommandOutput(
            string message,
            string? standardError,
            string? standardOutput)
        {
            var details = string.Join(
                " ",
                new[] { standardError, standardOutput }
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!.Trim()));

            return string.IsNullOrWhiteSpace(details)
                ? message
                : $"{message} {details}";
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

            public string? StorefrontApiBaseUrl { get; set; }

            public string? ClientAppBaseUrl { get; set; }

            public string? PublicUrlBaseUrl { get; set; }
        }

        private sealed record StorefrontImageResolution(bool Success, string? Image, string Message)
        {
            public static StorefrontImageResolution Succeeded(string image)
            {
                return new StorefrontImageResolution(true, image, "Storefront image resolved.");
            }

            public static StorefrontImageResolution Failed(string message)
            {
                return new StorefrontImageResolution(false, null, message);
            }
        }
    }
}
