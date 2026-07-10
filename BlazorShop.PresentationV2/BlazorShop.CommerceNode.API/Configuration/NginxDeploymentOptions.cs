namespace BlazorShop.CommerceNode.API.Configuration
{
    public sealed class NginxDeploymentOptions
    {
        public const string SectionName = "NginxDeployment";

        public string NginxExecutable { get; set; } = "nginx";

        public bool UseDockerExec { get; set; }

        public string DockerExecutable { get; set; } = "docker";

        public string ContainerName { get; set; } = "blazorshop-commercenode-nginx";

        public string ConfigDirectory { get; set; } = "runtime/nginx/conf.d";

        public string ConfigFilePrefix { get; set; } = "blazorshop-store";

        public string MediaUpstreamUrl { get; set; } = "http://host.docker.internal:5180";

        public int ProxyConnectTimeoutSeconds { get; set; } = 5;

        public int ProxyReadTimeoutSeconds { get; set; } = 30;
    }
}
