namespace BlazorShop.Storefront.Components.WasmHost.System;

using BlazorShop.Storefront.Components.Contracts.Components;

public static class StorefrontHybridRuntimeProbeDescriptor
{
    public static StorefrontComponentDescriptor Descriptor { get; } = new(
        "hybrid-runtime-probe",
        StorefrontComponentMode.Hybrid,
        StorefrontComponentCategory.System,
        typeof(StorefrontHybridRuntimeProbe));
}
