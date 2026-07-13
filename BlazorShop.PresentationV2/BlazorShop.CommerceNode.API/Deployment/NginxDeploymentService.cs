namespace BlazorShop.CommerceNode.API.Deployment
{
    using System.Diagnostics;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;

    using BlazorShop.CommerceNode.API.Configuration;

    using Microsoft.Extensions.Options;

    public sealed partial class NginxDeploymentService : INginxDeploymentService
    {
        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

        private readonly NginxDeploymentOptions options;
        private readonly IWebHostEnvironment environment;

        public NginxDeploymentService(
            IOptions<NginxDeploymentOptions> options,
            IWebHostEnvironment environment)
        {
            this.options = options.Value;
            this.environment = environment;
        }

        public NginxStoreProxyPlan CreatePlan(NginxStoreProxyRequest request)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.StoreKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.ServerName);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.UpstreamUrl);

            if (!Uri.TryCreate(request.UpstreamUrl, UriKind.Absolute, out var upstreamUri) ||
                upstreamUri.Scheme is not ("http" or "https"))
            {
                throw new InvalidOperationException("UpstreamUrl must be an absolute HTTP URL.");
            }

            var normalizedStoreKey = NormalizeName(request.StoreKey);
            var configDirectory = ResolveConfiguredPath(this.options.ConfigDirectory);
            var configFileName = $"{NormalizeName(this.options.ConfigFilePrefix)}-{normalizedStoreKey}.conf";
            var serverName = NormalizeServerName(request.ServerName);

            return new NginxStoreProxyPlan(
                request.StoreKey,
                serverName,
                request.UpstreamUrl.TrimEnd('/'),
                Path.Combine(configDirectory, configFileName));
        }

        public async Task<string> RenderConfigAsync(
            NginxStoreProxyPlan plan,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(plan.ConfigPath)!);

            var config = new StringBuilder()
                .AppendLine("server {")
                .Append("    server_name ").Append(plan.ServerName).AppendLine(";")
                .AppendLine()
                .AppendLine("    location /media/products/ {")
                .Append("        proxy_pass ").Append(this.ResolveMediaUpstreamUrl()).AppendLine(";")
                .AppendLine("        proxy_cache blazorshop_product_media;")
                .AppendLine("        proxy_cache_key \"$host$request_uri\";")
                .AppendLine("        proxy_cache_valid 200 301 302 30d;")
                .AppendLine("        proxy_cache_lock on;")
                .AppendLine("        proxy_set_header Host $host;")
                .AppendLine("        proxy_set_header X-Real-IP $remote_addr;")
                .AppendLine("        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;")
                .AppendLine("        proxy_set_header X-Forwarded-Proto $scheme;")
                .AppendLine("        add_header X-BlazorShop-Media-Cache $upstream_cache_status always;")
                .Append("        proxy_connect_timeout ").Append(Math.Max(1, this.options.ProxyConnectTimeoutSeconds)).AppendLine("s;")
                .Append("        proxy_read_timeout ").Append(Math.Max(1, this.options.ProxyReadTimeoutSeconds)).AppendLine("s;")
                .AppendLine("    }")
                .AppendLine()
                .AppendLine("    location /media/assets/ {")
                .Append("        proxy_pass ").Append(this.ResolveMediaUpstreamUrl()).AppendLine(";")
                .AppendLine("        proxy_cache blazorshop_product_media;")
                .AppendLine("        proxy_cache_key \"$host$request_uri\";")
                .AppendLine("        proxy_cache_valid 200 301 302 30d;")
                .AppendLine("        proxy_cache_lock on;")
                .AppendLine("        proxy_set_header Host $host;")
                .AppendLine("        proxy_set_header X-Real-IP $remote_addr;")
                .AppendLine("        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;")
                .AppendLine("        proxy_set_header X-Forwarded-Proto $scheme;")
                .AppendLine("        add_header X-BlazorShop-Media-Cache $upstream_cache_status always;")
                .Append("        proxy_connect_timeout ").Append(Math.Max(1, this.options.ProxyConnectTimeoutSeconds)).AppendLine("s;")
                .Append("        proxy_read_timeout ").Append(Math.Max(1, this.options.ProxyReadTimeoutSeconds)).AppendLine("s;")
                .AppendLine("    }")
                .AppendLine()
                .AppendLine("    location / {")
                .Append("        proxy_pass ").Append(plan.UpstreamUrl).AppendLine(";")
                .AppendLine("        proxy_set_header Host $host;")
                .AppendLine("        proxy_set_header X-Real-IP $remote_addr;")
                .AppendLine("        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;")
                .AppendLine("        proxy_set_header X-Forwarded-Proto $scheme;")
                .Append("        proxy_connect_timeout ").Append(Math.Max(1, this.options.ProxyConnectTimeoutSeconds)).AppendLine("s;")
                .Append("        proxy_read_timeout ").Append(Math.Max(1, this.options.ProxyReadTimeoutSeconds)).AppendLine("s;")
                .AppendLine("    }")
                .AppendLine("}")
                .ToString();

            await File.WriteAllTextAsync(plan.ConfigPath, config, Utf8WithoutBom, cancellationToken);
            return plan.ConfigPath;
        }

        public Task<NginxDeploymentCommandResult> ValidateConfigAsync(
            CancellationToken cancellationToken = default)
        {
            return this.RunNginxAsync(new[] { "-t" }, cancellationToken);
        }

        public Task<NginxDeploymentCommandResult> ReloadAsync(
            CancellationToken cancellationToken = default)
        {
            return this.RunNginxAsync(new[] { "-s", "reload" }, cancellationToken);
        }

        public Task RemoveConfigAsync(
            NginxStoreProxyPlan plan,
            CancellationToken cancellationToken = default)
        {
            if (File.Exists(plan.ConfigPath))
            {
                File.Delete(plan.ConfigPath);
            }

            return Task.CompletedTask;
        }

        private async Task<NginxDeploymentCommandResult> RunNginxAsync(
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = this.ResolveExecutable(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            if (this.options.UseDockerExec)
            {
                startInfo.ArgumentList.Add("exec");
                startInfo.ArgumentList.Add(this.ResolveContainerName());
                startInfo.ArgumentList.Add(string.IsNullOrWhiteSpace(this.options.NginxExecutable)
                    ? "nginx"
                    : this.options.NginxExecutable);
            }

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var standardOutput = await standardOutputTask;
            var standardError = await standardErrorTask;

            return new NginxDeploymentCommandResult(
                process.ExitCode == 0,
                process.ExitCode == 0 ? "Nginx command completed." : "Nginx command failed.",
                standardOutput,
                standardError,
                process.ExitCode);
        }

        private string ResolveExecutable()
        {
            if (this.options.UseDockerExec)
            {
                return string.IsNullOrWhiteSpace(this.options.DockerExecutable)
                    ? "docker"
                    : this.options.DockerExecutable;
            }

            return string.IsNullOrWhiteSpace(this.options.NginxExecutable)
                ? "nginx"
                : this.options.NginxExecutable;
        }

        private string ResolveContainerName()
        {
            return string.IsNullOrWhiteSpace(this.options.ContainerName)
                ? "blazorshop-commercenode-nginx"
                : this.options.ContainerName.Trim();
        }

        private string ResolveMediaUpstreamUrl()
        {
            return string.IsNullOrWhiteSpace(this.options.MediaUpstreamUrl)
                ? "http://host.docker.internal:5180"
                : this.options.MediaUpstreamUrl.TrimEnd('/');
        }

        private string ResolveConfiguredPath(string configuredPath)
        {
            var path = string.IsNullOrWhiteSpace(configuredPath)
                ? "runtime/nginx/conf.d"
                : configuredPath.Trim();

            return Path.IsPathRooted(path)
                ? path
                : Path.Combine(this.environment.ContentRootPath, path);
        }

        private static string NormalizeName(string value)
        {
            var normalized = UnsafeNameCharactersRegex().Replace(value.Trim().ToLowerInvariant(), "-");
            normalized = RepeatedDashRegex().Replace(normalized, "-").Trim('-');
            return string.IsNullOrWhiteSpace(normalized) ? "store" : normalized;
        }

        private static string NormalizeServerName(string value)
        {
            var host = value.Trim().ToLowerInvariant();

            if (Uri.TryCreate(host, UriKind.Absolute, out var uri))
            {
                host = uri.Host;
            }

            host = host.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? host;
            host = WebUtility.UrlDecode(host);

            if (!ServerNameRegex().IsMatch(host))
            {
                throw new InvalidOperationException("ServerName is not a valid host name.");
            }

            return host;
        }

        [GeneratedRegex("[^a-z0-9_.-]+", RegexOptions.Compiled)]
        private static partial Regex UnsafeNameCharactersRegex();

        [GeneratedRegex("-+", RegexOptions.Compiled)]
        private static partial Regex RepeatedDashRegex();

        [GeneratedRegex("^[a-z0-9][a-z0-9.-]*[a-z0-9]$", RegexOptions.Compiled)]
        private static partial Regex ServerNameRegex();
    }
}
