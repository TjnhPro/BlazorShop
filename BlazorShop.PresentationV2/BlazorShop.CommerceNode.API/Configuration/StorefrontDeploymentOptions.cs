namespace BlazorShop.CommerceNode.API.Configuration
{
    public sealed class StorefrontDeploymentOptions
    {
        public const string SectionName = "StorefrontDeployment";

        public string DockerExecutable { get; set; } = "docker";

        public string ContainerNamePrefix { get; set; } = "blazorshop-storefront";

        public string EnvDirectory { get; set; } = "runtime/storefront-env";

        public string? NetworkName { get; set; }

        public int ContainerPort { get; set; } = 8080;

        public string HealthPath { get; set; } = "/";

        public int HealthTimeoutSeconds { get; set; } = 5;

        public int HealthProbeAttempts { get; set; } = 10;

        public int HealthProbeDelaySeconds { get; set; } = 2;

        public bool UseDockerExecHealthProbe { get; set; }

        public string HealthProbeContainerName { get; set; } = "blazorshop-commercenode-nginx";

        public string? StorefrontApiBaseUrl { get; set; }

        public string? ClientAppBaseUrl { get; set; }

        public List<string> AllowedImages { get; set; } = new()
        {
            "blazorshop-storefront-v2:latest",
        };
    }
}
