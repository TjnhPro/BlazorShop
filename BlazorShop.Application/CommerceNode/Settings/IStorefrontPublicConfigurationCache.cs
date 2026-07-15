namespace BlazorShop.Application.CommerceNode.Settings
{
    public interface IStorefrontPublicConfigurationCache
    {
        bool TryGet<TValue>(string storeKey, out TValue? value);

        void Set<TValue>(string storeKey, TValue value);

        void Invalidate(string storeKey);

        Task InvalidateAsync(Guid storeId, CancellationToken cancellationToken = default);
    }
}
