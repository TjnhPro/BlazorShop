namespace BlazorShop.Storefront.Components.Contracts.Diagnostics;

public sealed record StorefrontHybridRuntimeProbeClasses(
    string? Root = null,
    string? Heading = null,
    string? State = null,
    string? ValueGroup = null,
    string? ValueLabel = null,
    string? Value = null,
    string? Action = null);
