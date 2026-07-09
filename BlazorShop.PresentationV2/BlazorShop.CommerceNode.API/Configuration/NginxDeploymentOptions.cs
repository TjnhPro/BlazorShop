namespace BlazorShop.CommerceNode.API.Configuration
{
    public sealed class NginxDeploymentOptions
    {
        public const string SectionName = "NginxDeployment";

        public string NginxExecutable { get; set; } = "nginx";

        public string ConfigDirectory { get; set; } = "runtime/nginx/conf.d";

        public string ConfigFilePrefix { get; set; } = "blazorshop-store";

        public int ProxyConnectTimeoutSeconds { get; set; } = 5;

        public int ProxyReadTimeoutSeconds { get; set; } = 30;
    }
}
