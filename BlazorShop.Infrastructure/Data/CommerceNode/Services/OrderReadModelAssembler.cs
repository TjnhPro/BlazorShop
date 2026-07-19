namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class OrderReadModelAssembler
    {
        private readonly CommerceNodeDbContext context;

        public OrderReadModelAssembler(CommerceNodeDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.context = context;
        }

        public Task<IReadOnlyList<GetOrder>> BuildAsync(
            IReadOnlyCollection<Order> orders,
            OrderReadModelOptions options,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(orders);
            ArgumentNullException.ThrowIfNull(options);

            if (orders.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<GetOrder>>([]);
            }

            return this.BuildNonEmptyAsync(orders, options, cancellationToken);
        }

        private async Task<IReadOnlyList<GetOrder>> BuildNonEmptyAsync(
            IReadOnlyCollection<Order> orders,
            OrderReadModelOptions options,
            CancellationToken cancellationToken)
        {
            _ = await this.LoadChildDataAsync(orders, options, cancellationToken);
            throw new NotSupportedException("Order read model projection is introduced in the next assembler phase.");
        }

        private async Task<OrderReadModelChildData> LoadChildDataAsync(
            IReadOnlyCollection<Order> orders,
            OrderReadModelOptions options,
            CancellationToken cancellationToken)
        {
            var orderIds = orders.Select(order => order.Id).ToArray();
            return new OrderReadModelChildData(
                await this.LoadProductNamesAsync(orders, options, cancellationToken),
                await this.LoadPaymentSummariesAsync(orderIds, options, cancellationToken),
                await this.LoadHistoryEntriesAsync(orderIds, options, cancellationToken),
                await this.LoadTrackingEventsAsync(orderIds, options, cancellationToken));
        }

        private async Task<IReadOnlyDictionary<Guid, string>> LoadProductNamesAsync(
            IReadOnlyCollection<Order> orders,
            OrderReadModelOptions options,
            CancellationToken cancellationToken)
        {
            if (!options.UseProductNameFallback)
            {
                return new Dictionary<Guid, string>();
            }

            var productIds = orders
                .SelectMany(order => order.Lines)
                .Select(line => line.ProductId)
                .Distinct()
                .ToArray();
            if (productIds.Length == 0)
            {
                return new Dictionary<Guid, string>();
            }

            return await this.context.Products
                .AsNoTracking()
                .Where(product => productIds.Contains(product.Id))
                .Select(product => new { product.Id, product.Name })
                .ToDictionaryAsync(product => product.Id, product => product.Name ?? string.Empty, cancellationToken);
        }

        private async Task<IReadOnlyDictionary<Guid, GetOrderPaymentSummary>> LoadPaymentSummariesAsync(
            IReadOnlyCollection<Guid> orderIds,
            OrderReadModelOptions options,
            CancellationToken cancellationToken)
        {
            var paymentAttempts = await this.context.PaymentAttempts
                .AsNoTracking()
                .Where(attempt => attempt.OrderId.HasValue && orderIds.Contains(attempt.OrderId.Value))
                .OrderByDescending(attempt => attempt.UpdatedAtUtc)
                .Select(attempt => new
                {
                    OrderId = attempt.OrderId!.Value,
                    Summary = new GetOrderPaymentSummary
                    {
                        PaymentAttemptPublicId = options.IncludePaymentAttemptPublicReference ? attempt.PublicId : null,
                        ProviderKey = options.IncludePaymentAttemptPublicReference ? attempt.ProviderKey : null,
                        PaymentStatus = attempt.State,
                        PaymentMethodKey = attempt.PaymentMethodKey,
                        AttemptState = attempt.State,
                        Amount = attempt.Amount,
                        CurrencyCode = attempt.CurrencyCode,
                        UpdatedAtUtc = attempt.UpdatedAtUtc,
                    },
                })
                .ToListAsync(cancellationToken);

            return paymentAttempts
                .GroupBy(item => item.OrderId)
                .ToDictionary(group => group.Key, group => group.First().Summary);
        }

        private async Task<IReadOnlyDictionary<Guid, GetOrderHistoryEntry[]>> LoadHistoryEntriesAsync(
            IReadOnlyCollection<Guid> orderIds,
            OrderReadModelOptions options,
            CancellationToken cancellationToken)
        {
            var historyEntries = await this.context.OrderHistoryEntries
                .AsNoTracking()
                .Where(entry => orderIds.Contains(entry.OrderId) && (options.IncludeAllHistory || entry.VisibleToCustomer))
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

            return historyEntries
                .GroupBy(item => item.OrderId)
                .ToDictionary(group => group.Key, group => group.Select(item => item.Entry).ToArray());
        }

        private async Task<IReadOnlyDictionary<Guid, GetShipmentTrackingEvent[]>> LoadTrackingEventsAsync(
            IReadOnlyCollection<Guid> orderIds,
            OrderReadModelOptions options,
            CancellationToken cancellationToken)
        {
            if (!options.IncludeTrackingEvents)
            {
                return new Dictionary<Guid, GetShipmentTrackingEvent[]>();
            }

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

            return trackingEvents
                .GroupBy(item => item.OrderId)
                .ToDictionary(group => group.Key, group => group.Select(item => item.Event).ToArray());
        }

        private sealed record OrderReadModelChildData(
            IReadOnlyDictionary<Guid, string> ProductNames,
            IReadOnlyDictionary<Guid, GetOrderPaymentSummary> PaymentSummaries,
            IReadOnlyDictionary<Guid, GetOrderHistoryEntry[]> HistoryEntries,
            IReadOnlyDictionary<Guid, GetShipmentTrackingEvent[]> TrackingEvents);
    }
}
