namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using Xunit;

    public sealed class NginxRuntimeConfigTests
    {
        [Fact]
        public void DefaultDenyConfig_ReturnsForbiddenForUnmatchedHosts()
        {
            var configPath = Path.Combine(
                FindRepositoryRoot(),
                "BlazorShop.PresentationV2",
                "BlazorShop.CommerceNode.API",
                "runtime",
                "nginx",
                "conf.d",
                "00-default-deny.conf");

            Assert.True(File.Exists(configPath), $"Expected Nginx default deny config at {configPath}.");

            var config = File.ReadAllText(configPath);

            Assert.Contains("listen 80 default_server;", config, StringComparison.Ordinal);
            Assert.Contains("server_name _;", config, StringComparison.Ordinal);
            Assert.Contains("return 403;", config, StringComparison.Ordinal);
        }

        [Fact]
        public void GeneratedStoreProxyConfig_ClearsUnsafeStoreHostHeader()
        {
            var serviceSource = File.ReadAllText(Path.Combine(
                FindRepositoryRoot(),
                "BlazorShop.PresentationV2",
                "BlazorShop.CommerceNode.API",
                "Deployment",
                "NginxDeploymentService.cs"));

            Assert.Contains("proxy_set_header Host $host;", serviceSource, StringComparison.Ordinal);
            Assert.DoesNotContain("proxy_set_header X-Store-Host $", serviceSource, StringComparison.Ordinal);
            Assert.Equal(3, serviceSource.Split("proxy_set_header X-Store-Host \\\"\\\";", StringSplitOptions.None).Length - 1);
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "BlazorShop.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new InvalidOperationException("Could not locate repository root from test output directory.");
        }
    }
}
