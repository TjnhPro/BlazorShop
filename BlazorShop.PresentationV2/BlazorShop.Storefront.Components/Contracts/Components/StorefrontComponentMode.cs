namespace BlazorShop.Storefront.Components.Contracts.Components;

public enum StorefrontComponentMode
{
    /// <summary>Primary useful output can be rendered from server-prepared state.</summary>
    Ssr = 1,

    /// <summary>Server-produced or prerendered output gains client-side WebAssembly interactivity after hydration.</summary>
    Hybrid = 2,

    /// <summary>Browser-side interactive root that must be included in a downloadable WebAssembly graph.</summary>
    WasmHost = 3,
}
