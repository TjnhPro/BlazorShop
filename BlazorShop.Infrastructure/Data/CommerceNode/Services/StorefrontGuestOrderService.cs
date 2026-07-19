namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Security.Cryptography;
    using System.Text;

    using BlazorShop.Application.CommerceNode.Orders;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.Services;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class StorefrontGuestOrderService : IStorefrontGuestOrderService
    {
        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly OrderReadModelAssembler orderReadModelAssembler;

        public StorefrontGuestOrderService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            OrderReadModelAssembler orderReadModelAssembler)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.orderReadModelAssembler = orderReadModelAssembler;
        }

        public async Task<ServiceResponse<GetOrder>> GetAsync(
            StorefrontGuestOrderLookupRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var reference = NormalizeNullable(request.Reference);
            var accessToken = NormalizeNullable(request.AccessToken);
            if (reference is null || accessToken is null)
            {
                return Failed("Order reference and access token are required.", ServiceResponseType.ValidationError);
            }

            var storeResult = await this.storeContext.GetCurrentStoreIdAsync();
            if (!storeResult.Success)
            {
                return Failed("Order was not found.", ServiceResponseType.NotFound);
            }

            var tokenHash = ComputeSha256(accessToken);
            var now = DateTimeOffset.UtcNow;
            var order = await this.context.Orders
                .AsNoTracking()
                .Include(item => item.Lines)
                .FirstOrDefaultAsync(
                    item => item.StoreId == storeResult.Payload
                        && item.Reference == reference
                        && item.GuestAccessTokenHash == tokenHash
                        && (!item.GuestAccessTokenExpiresAtUtc.HasValue || item.GuestAccessTokenExpiresAtUtc > now),
                    cancellationToken);
            if (order is null)
            {
                return Failed("Order was not found.", ServiceResponseType.NotFound);
            }

            return new ServiceResponse<GetOrder>(true, "Guest order loaded.", order.Id)
            {
                Payload = (await this.orderReadModelAssembler.BuildAsync(
                    [order],
                    OrderReadModelOptions.Guest(),
                    cancellationToken)).Single(),
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<GetOrder> Failed(string message, ServiceResponseType responseType)
        {
            return new ServiceResponse<GetOrder>(false, message)
            {
                ResponseType = responseType,
            };
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string ComputeSha256(string value)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
