namespace BlazorShop.Application.CommerceNode.Stores
{
    public sealed record StoreExecutionContext(
        Guid StoreId,
        string StoreKey,
        string? Host,
        string Source,
        string Status,
        bool IsActive,
        CommerceCurrentStore CurrentStore);

    public static class StoreExecutionContextSources
    {
        public const string Unknown = "unknown";

        public const string StorefrontRoute = "storefront-route";

        public const string CommerceAdminQuery = "commerce-admin-query";

        public const string PublicMediaHost = "public-media-host";
    }

    public interface IStoreExecutionContextAccessor
    {
        StoreExecutionContext? Current { get; }

        void SetCurrent(StoreExecutionContext context);

        void Clear();
    }

    public sealed class StoreExecutionContextAccessor : IStoreExecutionContextAccessor
    {
        public StoreExecutionContext? Current { get; private set; }

        public void SetCurrent(StoreExecutionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.Current = context;
        }

        public void Clear()
        {
            this.Current = null;
        }
    }
}
