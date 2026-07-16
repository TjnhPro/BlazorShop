namespace BlazorShop.Application.CommerceNode.SecurityPrivacy
{
    using System.Net;

    public static class PrivacyDataSanitizer
    {
        public const int DefaultMaxIpAddressLength = 128;

        public const int DefaultMaxUserAgentLength = 512;

        public static string? NormalizeIpAddress(string? ipAddress)
        {
            return NormalizeText(ipAddress, DefaultMaxIpAddressLength);
        }

        public static string? NormalizeUserAgent(string? userAgent)
        {
            return NormalizeText(userAgent, DefaultMaxUserAgentLength);
        }

        public static string? AnonymizeIpAddress(string? ipAddress)
        {
            var normalized = NormalizeIpAddress(ipAddress);
            if (normalized is null)
            {
                return null;
            }

            if (!IPAddress.TryParse(normalized, out var parsed))
            {
                return normalized;
            }

            var bytes = parsed.GetAddressBytes();
            if (parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                bytes[3] = 0;
                return new IPAddress(bytes).ToString();
            }

            if (parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                for (var index = 8; index < bytes.Length; index++)
                {
                    bytes[index] = 0;
                }

                return new IPAddress(bytes).ToString();
            }

            return normalized;
        }

        private static string? NormalizeText(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();
            return normalized.Length <= maxLength
                ? normalized
                : normalized[..maxLength];
        }
    }
}
