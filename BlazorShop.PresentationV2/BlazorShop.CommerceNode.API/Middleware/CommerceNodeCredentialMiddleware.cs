namespace BlazorShop.CommerceNode.API.Middleware
{
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;

    using BlazorShop.CommerceNode.API.Configuration;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.Extensions.Options;

    public sealed class CommerceNodeCredentialMiddleware
    {
        public const string NodeKeyHeaderName = "X-Node-Key";

        public const string NodeSecretHeaderName = "X-Node-Secret";

        private readonly RequestDelegate next;

        public CommerceNodeCredentialMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IOptions<CommerceNodeOptions> options,
            ILogger<CommerceNodeCredentialMiddleware> logger)
        {
            var commerceNodeOptions = options.Value;
            if (!IsControlPlaneIpAllowed(context.Connection.RemoteIpAddress, commerceNodeOptions.AllowedControlPlaneIps))
            {
                logger.LogWarning(
                    "Rejected Commerce Node request from disallowed IP {RemoteIpAddress} for {Path}.",
                    context.Connection.RemoteIpAddress,
                    context.Request.Path);

                await CommerceNodeApiResponseWriter.WriteFailureAsync<object>(
                    context,
                    StatusCodes.Status403Forbidden,
                    "Control Plane IP is not allowed.");
                return;
            }

            var requestNodeKey = context.Request.Headers[NodeKeyHeaderName].ToString();
            var requestNodeSecret = context.Request.Headers[NodeSecretHeaderName].ToString();

            if (!SecureEquals(requestNodeKey, commerceNodeOptions.NodeKey)
                || !SecureEquals(requestNodeSecret, commerceNodeOptions.NodeSecret))
            {
                logger.LogWarning(
                    "Rejected Commerce Node request with invalid credentials for node key {ConfiguredNodeKey} from {RemoteIpAddress}.",
                    commerceNodeOptions.NodeKey,
                    context.Connection.RemoteIpAddress);

                await CommerceNodeApiResponseWriter.WriteFailureAsync<object>(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "Invalid Commerce Node credential.");
                return;
            }

            await this.next(context);
        }

        private static bool IsControlPlaneIpAllowed(IPAddress? remoteIpAddress, IReadOnlyCollection<string> allowedControlPlaneIps)
        {
            if (allowedControlPlaneIps.Count == 0)
            {
                return true;
            }

            if (remoteIpAddress is null)
            {
                return false;
            }

            var normalizedRemoteIpAddress = NormalizeIpAddress(remoteIpAddress);

            foreach (var allowedControlPlaneIp in allowedControlPlaneIps)
            {
                if (!IPAddress.TryParse(allowedControlPlaneIp, out var parsedAllowedIp))
                {
                    continue;
                }

                if (NormalizeIpAddress(parsedAllowedIp).Equals(normalizedRemoteIpAddress))
                {
                    return true;
                }
            }

            return false;
        }

        private static IPAddress NormalizeIpAddress(IPAddress ipAddress)
        {
            return ipAddress.IsIPv4MappedToIPv6 ? ipAddress.MapToIPv4() : ipAddress;
        }

        private static bool SecureEquals(string? left, string? right)
        {
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
            {
                return false;
            }

            var leftBytes = Encoding.UTF8.GetBytes(left);
            var rightBytes = Encoding.UTF8.GetBytes(right);

            return leftBytes.Length == rightBytes.Length
                   && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
    }
}
