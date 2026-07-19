namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Orders;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.Services;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class StorefrontCustomerOrderService : IStorefrontCustomerOrderService
    {
        private const int DefaultPageSize = 10;
        private const int MaxPageSize = 100;

        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly OrderReadModelAssembler orderReadModelAssembler;

        public StorefrontCustomerOrderService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            OrderReadModelAssembler orderReadModelAssembler)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.orderReadModelAssembler = orderReadModelAssembler;
        }

        public async Task<ServiceResponse<PagedResult<GetOrder>>> ListAsync(
            StorefrontCustomerOrderQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var scope = await this.ResolveScopeAsync(query.AppUserId, cancellationToken);
            if (!scope.Success)
            {
                return new ServiceResponse<PagedResult<GetOrder>>(scope.Success, scope.Message)
                {
                    ResponseType = scope.ResponseType,
                };
            }

            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize <= 0 ? DefaultPageSize : query.PageSize, 1, MaxPageSize);
            var ordersQuery = this.CreateOwnedOrderQuery(scope.Payload!.StoreId, scope.Payload.Customer, query.AppUserId);
            var totalCount = await ordersQuery.CountAsync(cancellationToken);
            var orders = await ordersQuery
                .OrderByDescending(order => order.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(order => order.Lines)
                .ToListAsync(cancellationToken);

            return new ServiceResponse<PagedResult<GetOrder>>(true, "Current customer orders loaded.")
            {
                Payload = new PagedResult<GetOrder>
                {
                    Items = await this.MapOrdersAsync(orders, cancellationToken),
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                },
                ResponseType = ServiceResponseType.Success,
            };
        }

        public Task<ServiceResponse<GetOrder>> GetAsync(
            StorefrontCustomerOrderLookupRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.GetOwnedOrderAsync(request, receiptMode: false, cancellationToken);
        }

        public Task<ServiceResponse<GetOrder>> GetReceiptAsync(
            StorefrontCustomerOrderLookupRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.GetOwnedOrderAsync(request, receiptMode: true, cancellationToken);
        }

        private async Task<ServiceResponse<GetOrder>> GetOwnedOrderAsync(
            StorefrontCustomerOrderLookupRequest request,
            bool receiptMode,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var reference = NormalizeNullable(request.OrderReference);
            if (reference is null)
            {
                return Failed<GetOrder>("Order reference is required.", ServiceResponseType.ValidationError);
            }

            var scope = await this.ResolveScopeAsync(request.AppUserId, cancellationToken);
            if (!scope.Success)
            {
                return new ServiceResponse<GetOrder>(scope.Success, scope.Message)
                {
                    ResponseType = scope.ResponseType,
                };
            }

            var order = await this.CreateOwnedOrderQuery(scope.Payload!.StoreId, scope.Payload.Customer, request.AppUserId)
                .Where(item => item.Reference == reference)
                .Include(item => item.Lines)
                .FirstOrDefaultAsync(cancellationToken);

            if (order is null)
            {
                return Failed<GetOrder>("Order was not found.", ServiceResponseType.NotFound);
            }

            return new ServiceResponse<GetOrder>(true, receiptMode ? "Customer order receipt loaded." : "Customer order loaded.")
            {
                Payload = (await this.MapOrdersAsync([order], cancellationToken)).Single(),
                ResponseType = ServiceResponseType.Success,
            };
        }

        private IQueryable<Order> CreateOwnedOrderQuery(Guid storeId, CommerceCustomer customer, string appUserId)
        {
            var normalizedEmail = NormalizeEmail(customer.Email);
            var normalizedAppUserId = NormalizeNullable(appUserId);
            return this.context.Orders
                .AsNoTracking()
                .Where(order => order.StoreId == storeId)
                .Where(order =>
                    order.CustomerId == customer.Id
                    || (order.CustomerId == null
                        && normalizedAppUserId != null
                        && order.UserId == normalizedAppUserId
                        && (order.CustomerEmail == null
                            || order.CustomerEmail.Trim() == string.Empty
                            || order.CustomerEmail.Trim().ToUpper() == normalizedEmail)));
        }

        private async Task<ServiceResponse<CustomerOrderScope>> ResolveScopeAsync(
            string appUserId,
            CancellationToken cancellationToken)
        {
            var normalizedAppUserId = NormalizeNullable(appUserId);
            if (normalizedAppUserId is null)
            {
                return Failed<CustomerOrderScope>("Customer identity was not found.", ServiceResponseType.ValidationError);
            }

            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return Failed<CustomerOrderScope>("Store was not found.", ServiceResponseType.NotFound);
            }

            var customer = await this.context.CommerceCustomers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.StoreId == storeResult.Payload
                        && item.AppUserId == normalizedAppUserId
                        && item.IsActive,
                    cancellationToken);
            if (customer is null)
            {
                return Failed<CustomerOrderScope>("Customer account was not found.", ServiceResponseType.NotFound);
            }

            return new ServiceResponse<CustomerOrderScope>(true, "Customer order scope resolved.")
            {
                Payload = new CustomerOrderScope(storeResult.Payload, customer),
                ResponseType = ServiceResponseType.Success,
            };
        }

        private async Task<IReadOnlyList<GetOrder>> MapOrdersAsync(
            IReadOnlyCollection<Order> orders,
            CancellationToken cancellationToken)
        {
            return await this.orderReadModelAssembler.BuildAsync(
                orders,
                OrderReadModelOptions.Customer(),
                cancellationToken);
        }

        private static ServiceResponse<T> Failed<T>(string message, ServiceResponseType responseType)
        {
            return new ServiceResponse<T>(false, message)
            {
                ResponseType = responseType,
            };
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string NormalizeEmail(string? email)
        {
            return string.IsNullOrWhiteSpace(email)
                ? string.Empty
                : email.Trim().ToUpperInvariant();
        }

        private sealed record CustomerOrderScope(Guid StoreId, CommerceCustomer Customer);
    }
}
