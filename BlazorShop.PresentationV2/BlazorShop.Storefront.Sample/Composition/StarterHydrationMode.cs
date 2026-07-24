namespace BlazorShop.Storefront.Sample.Composition
{
    public enum StarterHydrationMode
    {
        InitialSnapshot,
        BrowserFetch,
        RefreshAfterHydration,
    }

    public sealed record StarterRenderContract(
        string RenderOwner,
        StarterHydrationMode HydrationMode,
        bool HasInitialSnapshot)
    {
        public bool ShouldFetchOnFirstLoad => this.HydrationMode != StarterHydrationMode.InitialSnapshot || !this.HasInitialSnapshot;
    }
}

