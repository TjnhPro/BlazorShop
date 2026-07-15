namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Currencies;

    using Microsoft.EntityFrameworkCore;

    public sealed class StoreCurrencyResolver : IStoreCurrencyResolver
    {
        private const string FallbackCurrencyCode = "USD";

        private readonly CommerceNodeDbContext context;

        public StoreCurrencyResolver(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<string> ResolveDefaultCurrencyCodeAsync(
            Guid storeId,
            CancellationToken cancellationToken = default)
        {
            if (storeId == Guid.Empty)
            {
                return FallbackCurrencyCode;
            }

            var currencyCode = await this.context.CommerceStores
                .AsNoTracking()
                .Where(store => store.Id == storeId)
                .Select(store => store.DefaultCurrencyCode)
                .FirstOrDefaultAsync(cancellationToken);

            return Normalize(currencyCode) ?? FallbackCurrencyCode;
        }

        private static string? Normalize(string? value)
        {
            var normalized = value?.Trim().ToUpperInvariant();
            return normalized is { Length: 3 } ? normalized : null;
        }
    }
}
