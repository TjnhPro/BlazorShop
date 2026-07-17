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

        public StorefrontCustomerOrderService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext)
        {
            this.context = context;
            this.storeContext = storeContext;
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
                        && (string.IsNullOrWhiteSpace(order.CustomerEmail)
                            || NormalizeEmail(order.CustomerEmail) == normalizedEmail)));
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
            if (orders.Count == 0)
            {
                return [];
            }

            var productIds = orders.SelectMany(order => order.Lines).Select(line => line.ProductId).Distinct().ToArray();
            var productNames = await this.context.Products
                .AsNoTracking()
                .Where(product => productIds.Contains(product.Id))
                .Select(product => new { product.Id, product.Name })
                .ToDictionaryAsync(product => product.Id, product => product.Name ?? string.Empty, cancellationToken);

            var orderIds = orders.Select(order => order.Id).ToArray();
            var trackingEvents = await this.context.ShipmentTrackingEvents
                .AsNoTracking()
                .Where(trackingEvent => orderIds.Contains(trackingEvent.OrderId))
                .OrderBy(trackingEvent => trackingEvent.OccurredAtUtc)
                .Select(trackingEvent => new
                {
                    trackingEvent.OrderId,
                    Event = new GetShipmentTrackingEvent
                    {
                        Id = trackingEvent.Id,
                        Status = trackingEvent.Status,
                        Message = trackingEvent.Message,
                        OccurredAtUtc = trackingEvent.OccurredAtUtc,
                        Location = trackingEvent.Location,
                        Source = trackingEvent.Source,
                    },
                })
                .ToListAsync(cancellationToken);

            var trackingEventsByOrder = trackingEvents
                .GroupBy(item => item.OrderId)
                .ToDictionary(group => group.Key, group => group.Select(item => item.Event).ToArray());

            var historyEntries = await this.context.OrderHistoryEntries
                .AsNoTracking()
                .Where(entry => orderIds.Contains(entry.OrderId) && entry.VisibleToCustomer)
                .OrderBy(entry => entry.CreatedAtUtc)
                .Select(entry => new
                {
                    entry.OrderId,
                    Entry = new GetOrderHistoryEntry
                    {
                        Id = entry.Id,
                        EventType = entry.EventType,
                        OldValue = entry.OldValue,
                        NewValue = entry.NewValue,
                        Message = entry.Message,
                        VisibleToCustomer = entry.VisibleToCustomer,
                        CreatedAtUtc = entry.CreatedAtUtc,
                        Source = entry.Source,
                    },
                })
                .ToListAsync(cancellationToken);

            var historyEntriesByOrder = historyEntries
                .GroupBy(item => item.OrderId)
                .ToDictionary(group => group.Key, group => group.Select(item => item.Entry).ToArray());

            var paymentAttempts = await this.context.PaymentAttempts
                .AsNoTracking()
                .Where(attempt => attempt.OrderId.HasValue && orderIds.Contains(attempt.OrderId.Value))
                .OrderByDescending(attempt => attempt.UpdatedAtUtc)
                .Select(attempt => new
                {
                    OrderId = attempt.OrderId!.Value,
                    Summary = new GetOrderPaymentSummary
                    {
                        PaymentStatus = attempt.State,
                        PaymentMethodKey = attempt.PaymentMethodKey,
                        AttemptState = attempt.State,
                        Amount = attempt.Amount,
                        CurrencyCode = attempt.CurrencyCode,
                        UpdatedAtUtc = attempt.UpdatedAtUtc,
                    },
                })
                .ToListAsync(cancellationToken);

            var paymentSummaryByOrder = paymentAttempts
                .GroupBy(item => item.OrderId)
                .ToDictionary(group => group.Key, group => group.First().Summary);

            return orders.Select(order => new GetOrder
            {
                Id = order.Id,
                Reference = order.Reference,
                Status = order.OrderStatus,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                PaymentMethodKey = order.PaymentMethodKey,
                PaymentAt = order.PaymentAt,
                PaymentSummary = CreatePaymentSummary(
                    order,
                    paymentSummaryByOrder.TryGetValue(order.Id, out var paymentSummary) ? paymentSummary : null),
                StoreSnapshot = OrderSnapshotProjection.ToStoreSnapshot(order),
                CurrencyCode = order.CurrencyCode,
                TotalAmount = order.TotalAmount,
                TotalBreakdown = OrderSnapshotProjection.ToTotalBreakdown(
                    order.SubtotalAmount,
                    order.ShippingTotalAmount,
                    order.TaxTotalAmount,
                    order.DiscountTotalAmount,
                    order.GrandTotalAmount),
                BaseCurrencyCode = order.BaseCurrencyCode,
                BaseTotalAmount = order.BaseTotalAmount,
                BaseTotalBreakdown = OrderSnapshotProjection.ToTotalBreakdown(
                    order.BaseSubtotalAmount,
                    order.BaseShippingTotalAmount,
                    order.BaseTaxTotalAmount,
                    order.BaseDiscountTotalAmount,
                    order.BaseGrandTotalAmount),
                ExchangeRate = order.ExchangeRate,
                ExchangeRateProviderKey = order.ExchangeRateProviderKey,
                ExchangeRateSource = order.ExchangeRateSource,
                ExchangeRateEffectiveAtUtc = order.ExchangeRateEffectiveAtUtc,
                ExchangeRateExpiresAtUtc = order.ExchangeRateExpiresAtUtc,
                CreatedOn = order.CreatedOn,
                ShippingStatus = order.ShippingStatus,
                ShippingCarrier = order.ShippingCarrier,
                TrackingNumber = order.TrackingNumber,
                TrackingUrl = order.TrackingUrl,
                ShippedOn = order.ShippedOn,
                DeliveredOn = order.DeliveredOn,
                CustomerName = order.CustomerName,
                CustomerEmail = order.CustomerEmail,
                BillingAddress = OrderSnapshotProjection.ToAddress(order.BillingAddressSnapshotJson),
                ShippingAddressSnapshot = OrderSnapshotProjection.ToShippingAddressSnapshot(order),
                ShippingFullName = order.ShippingFullName,
                ShippingEmail = order.ShippingEmail,
                ShippingPhone = order.ShippingPhone,
                ShippingAddress1 = order.ShippingAddress1,
                ShippingAddress2 = order.ShippingAddress2,
                ShippingCity = order.ShippingCity,
                ShippingState = order.ShippingState,
                ShippingPostalCode = order.ShippingPostalCode,
                ShippingCountryCode = order.ShippingCountryCode,
                ShippingMethod = OrderSnapshotProjection.ToShippingMethod(order),
                CompletedAt = order.CompletedAt,
                CancelledAt = order.CancelledAt,
                TrackingEvents = trackingEventsByOrder.TryGetValue(order.Id, out var events) ? events : [],
                HistoryEntries = historyEntriesByOrder.TryGetValue(order.Id, out var history) ? history : [],
                Lines = order.Lines.Select(line => new GetOrderLine
                {
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    CurrencyCode = line.CurrencyCode,
                    BaseUnitPrice = line.BaseUnitPrice,
                    ConvertedUnitPrice = line.ConvertedUnitPrice,
                    PersistedLineTotal = line.LineTotal,
                    BaseLineTotal = line.BaseLineTotal,
                    ProductName = line.ProductName ?? (productNames.TryGetValue(line.ProductId, out var productName) ? productName : string.Empty),
                    Sku = line.Sku,
                    Image = line.Image,
                    ProductVariantId = line.ProductVariantId,
                    VariantAttributes = ProductVariantAttributeNormalizer.Deserialize(line.VariantAttributesJson),
                }),
            }).ToArray();
        }

        private static GetOrderPaymentSummary CreatePaymentSummary(Order order, GetOrderPaymentSummary? paymentAttempt)
        {
            return new GetOrderPaymentSummary
            {
                PaymentStatus = order.PaymentStatus,
                PaymentMethodKey = order.PaymentMethodKey,
                AttemptState = paymentAttempt?.AttemptState,
                Amount = paymentAttempt?.Amount,
                CurrencyCode = paymentAttempt?.CurrencyCode,
                PaymentAt = order.PaymentAt,
                UpdatedAtUtc = paymentAttempt?.UpdatedAtUtc,
            };
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
