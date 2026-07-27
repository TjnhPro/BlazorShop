namespace BlazorShop.Storefront.Starter.Features
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using BlazorShop.Storefront.Services;

    public sealed record StarterFeatureManifest(
        IReadOnlyDictionary<string, StarterFeatureDefinition> Features)
    {
        public static readonly IReadOnlySet<string> AllowedFeatureKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "customerAccounts",
            "registration",
            "cart",
            "checkout",
            "payments",
            "newsletter",
            "recommendations",
            "contactForm",
        };

        public static readonly IReadOnlySet<string> AllowedPlacements = new HashSet<string>(StringComparer.Ordinal)
        {
            "home",
            "productDetail",
            "category",
            "cart",
            "checkout",
            "account",
        };

        public static StarterFeatureManifest Load(string path)
        {
            using var stream = File.OpenRead(path);
            var manifest = JsonSerializer.Deserialize(
                stream,
                StarterFeatureManifestJsonContext.Default.StarterFeatureManifest)
                ?? throw new InvalidOperationException($"Feature manifest '{path}' could not be read.");

            manifest.Validate();
            return manifest;
        }

        public void Validate()
        {
            foreach (var (key, definition) in this.Features)
            {
                if (!AllowedFeatureKeys.Contains(key))
                {
                    throw new InvalidOperationException($"Unsupported Starter feature key '{key}'.");
                }

                foreach (var placement in definition.Placements)
                {
                    if (!AllowedPlacements.Contains(placement))
                    {
                        throw new InvalidOperationException($"Unsupported Starter feature placement '{placement}' for '{key}'.");
                    }
                }
            }
        }
    }

    public sealed record StarterFeatureDefinition(
        bool Installed,
        bool Required,
        IReadOnlyList<string> Placements);

    public sealed record StarterFeatureActivation(
        string Key,
        bool Installed,
        bool Required,
        bool BackendSupported,
        bool StoreEnabled,
        bool PresentationPlaced,
        string? Reason)
    {
        public bool Visible => this.Installed && this.BackendSupported && this.StoreEnabled && this.PresentationPlaced;
    }

    public sealed class StarterFeatureActivationService
    {
        private readonly StarterFeatureManifest manifest;

        public StarterFeatureActivationService(StarterFeatureManifest manifest)
        {
            this.manifest = manifest;
        }

        public StarterFeatureActivation Evaluate(
            string key,
            string placement,
            IReadOnlyDictionary<string, StorefrontCapability> backendCapabilities)
        {
            if (!this.manifest.Features.TryGetValue(key, out var definition))
            {
                return new StarterFeatureActivation(key, false, false, false, false, false, "not_installed");
            }

            var backendSupported = backendCapabilities.TryGetValue(key, out var capability) && capability.Supported;
            var storeEnabled = backendCapabilities.TryGetValue(key, out capability) && capability.Supported && capability.Enabled;
            var placed = definition.Placements.Contains(placement, StringComparer.Ordinal);

            return new StarterFeatureActivation(
                key,
                definition.Installed,
                definition.Required,
                backendSupported,
                storeEnabled,
                placed,
                storeEnabled && placed ? null : GetReason(backendCapabilities, key));
        }

        private static string? GetReason(
            IReadOnlyDictionary<string, StorefrontCapability> backendCapabilities,
            string key)
        {
            return backendCapabilities.TryGetValue(key, out var capability)
                ? capability.Reason
                : "not_installed";
        }
    }

    [JsonSerializable(typeof(StarterFeatureManifest))]
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    internal sealed partial class StarterFeatureManifestJsonContext : JsonSerializerContext;
}
