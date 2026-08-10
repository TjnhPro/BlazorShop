namespace BlazorShop.Storefront.Components.Contracts.Diagnostics;

public sealed record StorefrontHybridRuntimeProbeLabels(
    string Heading,
    string PrerenderStateLabel,
    string InteractiveStateLabel,
    string ValueLabel,
    string ActionLabel);
