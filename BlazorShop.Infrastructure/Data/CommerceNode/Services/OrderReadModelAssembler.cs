namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.Services;
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
            var childData = await this.LoadChildDataAsync(orders, options, cancellationToken);
            return orders.Select(order => this.MapOrder(order, childData, options)).ToArray();
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

        private GetOrder MapOrder(
            Order order,
            OrderReadModelChildData childData,
            OrderReadModelOptions options)
        {
            return new GetOrder
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
                    childData.PaymentSummaries.TryGetValue(order.Id, out var paymentSummary) ? paymentSummary : null),
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
                UserId = options.IncludeUserId ? order.UserId : null,
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
                AdminNote = options.IncludeAdminNote ? order.AdminNote : null,
                TrackingEvents = childData.TrackingEvents.TryGetValue(order.Id, out var trackingEvents) ? trackingEvents : [],
                HistoryEntries = childData.HistoryEntries.TryGetValue(order.Id, out var history) ? history : [],
                Lines = order.Lines.Select(line => MapLine(line, childData.ProductNames, options)).ToArray(),
            };
        }

        private static GetOrderLine MapLine(
            OrderLine line,
            IReadOnlyDictionary<Guid, string> productNames,
            OrderReadModelOptions options)
        {
            return new GetOrderLine
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                CurrencyCode = options.IncludeLineMoneyDetails ? line.CurrencyCode : null,
                BaseUnitPrice = options.IncludeLineMoneyDetails ? line.BaseUnitPrice : null,
                ConvertedUnitPrice = options.IncludeLineMoneyDetails ? line.ConvertedUnitPrice : null,
                PersistedLineTotal = options.IncludeLineMoneyDetails ? line.LineTotal : null,
                BaseLineTotal = options.IncludeLineMoneyDetails ? line.BaseLineTotal : null,
                ProductName = ResolveProductName(line, productNames, options),
                Sku = line.Sku,
                Image = line.Image,
                ProductVariantId = line.ProductVariantId,
                VariantAttributes = ProductVariantAttributeNormalizer.Deserialize(line.VariantAttributesJson),
            };
        }

        private static string? ResolveProductName(
            OrderLine line,
            IReadOnlyDictionary<Guid, string> productNames,
            OrderReadModelOptions options)
        {
            if (line.ProductName is not null)
            {
                return line.ProductName;
            }

            if (!options.UseProductNameFallback)
            {
                return null;
            }

            return productNames.TryGetValue(line.ProductId, out var productName) ? productName : string.Empty;
        }

        private static GetOrderPaymentSummary CreatePaymentSummary(Order order, GetOrderPaymentSummary? paymentAttempt)
        {
            return new GetOrderPaymentSummary
            {
                PaymentAttemptPublicId = paymentAttempt?.PaymentAttemptPublicId,
                ProviderKey = paymentAttempt?.ProviderKey,
                PaymentStatus = order.PaymentStatus,
                PaymentMethodKey = order.PaymentMethodKey,
                AttemptState = paymentAttempt?.AttemptState,
                Amount = paymentAttempt?.Amount,
                CurrencyCode = paymentAttempt?.CurrencyCode,
                PaymentAt = order.PaymentAt,
                UpdatedAtUtc = paymentAttempt?.UpdatedAtUtc,
            };
        }

        private sealed record OrderReadModelChildData(
            IReadOnlyDictionary<Guid, string> ProductNames,
            IReadOnlyDictionary<Guid, GetOrderPaymentSummary> PaymentSummaries,
            IReadOnlyDictionary<Guid, GetOrderHistoryEntry[]> HistoryEntries,
            IReadOnlyDictionary<Guid, GetShipmentTrackingEvent[]> TrackingEvents);
    }
}
