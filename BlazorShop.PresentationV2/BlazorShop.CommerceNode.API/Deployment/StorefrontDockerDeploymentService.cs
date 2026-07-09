namespace BlazorShop.CommerceNode.API.Deployment
{
    using System.Diagnostics;
    using System.Globalization;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;

    using BlazorShop.CommerceNode.API.Configuration;

    using Microsoft.Extensions.Options;

    public sealed partial class StorefrontDockerDeploymentService : IStorefrontDockerDeploymentService
    {
        private readonly StorefrontDeploymentOptions options;
        private readonly IWebHostEnvironment environment;
        private readonly IHttpClientFactory httpClientFactory;

        public StorefrontDockerDeploymentService(
            IOptions<StorefrontDeploymentOptions> options,
            IWebHostEnvironment environment,
            IHttpClientFactory httpClientFactory)
        {
            this.options = options.Value;
            this.environment = environment;
            this.httpClientFactory = httpClientFactory;
        }

        public StorefrontContainerPlan CreatePlan(StorefrontDeploymentRequest request)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.StoreKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.StorefrontImage);

            if (!this.options.AllowedImages.Contains(request.StorefrontImage, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Storefront image is not allowed.");
            }

            var normalizedStoreKey = NormalizeStoreKey(request.StoreKey);
            var containerName = $"{NormalizeStoreKey(this.options.ContainerNamePrefix)}-{normalizedStoreKey}";
            var envDirectory = ResolveConfiguredPath(this.options.EnvDirectory);
            var envFilePath = Path.Combine(envDirectory, $"{normalizedStoreKey}.env");
            var networkName = string.IsNullOrWhiteSpace(request.NetworkName)
                ? this.options.NetworkName
                : request.NetworkName.Trim();
            var internalUrl = $"http://{containerName}:{Math.Max(1, this.options.ContainerPort).ToString(CultureInfo.InvariantCulture)}";

            return new StorefrontContainerPlan(
                request.StoreKey,
                containerName,
                request.StorefrontImage,
                networkName,
                Math.Max(1, this.options.ContainerPort),
                envFilePath,
                internalUrl);
        }

        public async Task<string> RenderEnvironmentFileAsync(
            StorefrontContainerPlan plan,
            StorefrontDeploymentRequest request,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(plan.EnvFilePath)!);

            var environmentValues = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["STORE_ID"] = request.StoreId.ToString("D"),
                ["STORE_KEY"] = request.StoreKey,
            };

            foreach (var item in request.Environment)
            {
                if (!IsValidEnvironmentName(item.Key))
                {
                    throw new InvalidOperationException($"Environment variable '{item.Key}' is not valid.");
                }

                environmentValues[item.Key] = item.Value;
            }

            var builder = new StringBuilder();
            foreach (var item in environmentValues)
            {
                builder.Append(item.Key)
                    .Append('=')
                    .AppendLine(EscapeEnvironmentValue(item.Value));
            }

            await File.WriteAllTextAsync(plan.EnvFilePath, builder.ToString(), Encoding.UTF8, cancellationToken);
            return plan.EnvFilePath;
        }

        public async Task<StorefrontDeploymentCommandResult> CreateOrUpdateContainerAsync(
            StorefrontContainerPlan plan,
            CancellationToken cancellationToken = default)
        {
            var inspect = await this.RunDockerAsync(
                new[] { "container", "inspect", plan.ContainerName },
                cancellationToken,
                allowFailure: true);

            if (inspect.Success)
            {
                var remove = await this.RemoveContainerAsync(plan.ContainerName, cancellationToken);
                if (!remove.Success)
                {
                    return remove;
                }
            }

            var args = new List<string>
            {
                "create",
                "--name",
                plan.ContainerName,
                "--env-file",
                plan.EnvFilePath,
                "--label",
                $"blazorshop.store_key={plan.StoreKey}",
            };

            if (!string.IsNullOrWhiteSpace(plan.NetworkName))
            {
                args.Add("--network");
                args.Add(plan.NetworkName);
            }

            args.Add(plan.StorefrontImage);

            return await this.RunDockerAsync(args, cancellationToken);
        }

        public Task<StorefrontDeploymentCommandResult> StartContainerAsync(
            string containerName,
            CancellationToken cancellationToken = default)
        {
            return this.RunDockerAsync(new[] { "start", containerName }, cancellationToken);
        }

        public Task<StorefrontDeploymentCommandResult> StopContainerAsync(
            string containerName,
            CancellationToken cancellationToken = default)
        {
            return this.RunDockerAsync(new[] { "stop", containerName }, cancellationToken, allowFailure: true);
        }

        public Task<StorefrontDeploymentCommandResult> RemoveContainerAsync(
            string containerName,
            CancellationToken cancellationToken = default)
        {
            return this.RunDockerAsync(new[] { "rm", "-f", containerName }, cancellationToken, allowFailure: true);
        }

        public async Task<StorefrontHealthProbeResult> ProbeHealthAsync(
            StorefrontContainerPlan plan,
            CancellationToken cancellationToken = default)
        {
            var client = this.httpClientFactory.CreateClient(nameof(StorefrontDockerDeploymentService));
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, this.options.HealthTimeoutSeconds));

            var path = this.options.HealthPath.StartsWith("/", StringComparison.Ordinal)
                ? this.options.HealthPath
                : $"/{this.options.HealthPath}";
            var uri = new Uri($"{plan.InternalUrl.TrimEnd('/')}{path}");

            try
            {
                using var response = await client.GetAsync(uri, cancellationToken);
                return new StorefrontHealthProbeResult(
                    response.StatusCode == HttpStatusCode.OK,
                    (int)response.StatusCode,
                    response.IsSuccessStatusCode ? "Storefront health check passed." : "Storefront health check failed.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return new StorefrontHealthProbeResult(false, null, ex.Message);
            }
        }

        private async Task<StorefrontDeploymentCommandResult> RunDockerAsync(
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken,
            bool allowFailure = false)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = string.IsNullOrWhiteSpace(this.options.DockerExecutable)
                    ? "docker"
                    : this.options.DockerExecutable,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

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
            var success = process.ExitCode == 0;

            return new StorefrontDeploymentCommandResult(
                success || allowFailure,
                success ? "Docker command completed." : "Docker command failed.",
                standardOutput,
                standardError,
                process.ExitCode);
        }

        private string ResolveConfiguredPath(string configuredPath)
        {
            var path = string.IsNullOrWhiteSpace(configuredPath)
                ? "runtime/storefront-env"
                : configuredPath.Trim();

            return Path.IsPathRooted(path)
                ? path
                : Path.Combine(this.environment.ContentRootPath, path);
        }

        private static string NormalizeStoreKey(string value)
        {
            var normalized = StoreKeyUnsafeCharactersRegex().Replace(value.Trim().ToLowerInvariant(), "-");
            normalized = RepeatedDashRegex().Replace(normalized, "-").Trim('-');
            return string.IsNullOrWhiteSpace(normalized) ? "store" : normalized;
        }

        private static bool IsValidEnvironmentName(string value)
        {
            return EnvironmentNameRegex().IsMatch(value);
        }

        private static string EscapeEnvironmentValue(string value)
        {
            return value.ReplaceLineEndings(" ").Replace("\"", "\\\"", StringComparison.Ordinal);
        }

        [GeneratedRegex("[^a-z0-9_.-]+", RegexOptions.Compiled)]
        private static partial Regex StoreKeyUnsafeCharactersRegex();

        [GeneratedRegex("-+", RegexOptions.Compiled)]
        private static partial Regex RepeatedDashRegex();

        [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled)]
        private static partial Regex EnvironmentNameRegex();
    }
}
