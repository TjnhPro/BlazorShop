namespace BlazorShop.Storefront.Runtime
{
    public sealed record StorefrontRuntimeCapability(
        bool Supported,
        bool Enabled,
        string? Reason);

    public interface IStorefrontCapabilityReader
    {
        bool IsSupported(IReadOnlyDictionary<string, StorefrontRuntimeCapability> capabilities, string key);

        bool IsEnabled(IReadOnlyDictionary<string, StorefrontRuntimeCapability> capabilities, string key);

        string? GetReason(IReadOnlyDictionary<string, StorefrontRuntimeCapability> capabilities, string key);
    }

    public sealed class StorefrontCapabilityReader : IStorefrontCapabilityReader
    {
        public bool IsSupported(IReadOnlyDictionary<string, StorefrontRuntimeCapability> capabilities, string key)
        {
            ArgumentNullException.ThrowIfNull(capabilities);
            return capabilities.TryGetValue(key, out var capability) && capability.Supported;
        }

        public bool IsEnabled(IReadOnlyDictionary<string, StorefrontRuntimeCapability> capabilities, string key)
        {
            ArgumentNullException.ThrowIfNull(capabilities);
            return capabilities.TryGetValue(key, out var capability) && capability.Supported && capability.Enabled;
        }

        public string? GetReason(IReadOnlyDictionary<string, StorefrontRuntimeCapability> capabilities, string key)
        {
            ArgumentNullException.ThrowIfNull(capabilities);
            return capabilities.TryGetValue(key, out var capability) ? capability.Reason : "not_installed";
        }
    }
}
