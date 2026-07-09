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

        public string HealthPath { get; set; } = "/healthz";

        public int HealthTimeoutSeconds { get; set; } = 5;

        public List<string> AllowedImages { get; set; } = new()
        {
            "blazorshop-storefront-v2:latest",
        };
    }
}
