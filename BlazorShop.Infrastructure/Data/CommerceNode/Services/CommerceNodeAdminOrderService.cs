namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Contracts.Payment;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeAdminOrderService : IAdminOrderService
    {
        private readonly CommerceNodeDbContext context;
        private readonly IOrderTrackingService trackingService;
        private readonly IAdminAuditService auditService;
        private readonly ICommerceStoreContext storeContext;

        public CommerceNodeAdminOrderService(
            CommerceNodeDbContext context,
            IOrderTrackingService trackingService,
            IAdminAuditService auditService,
            ICommerceStoreContext storeContext)
        {
            this.context = context;
            this.trackingService = trackingService;
            this.auditService = auditService;
            this.storeContext = storeContext;
        }

        public async Task<PagedResult<GetOrder>> GetAsync(AdminOrderQueryDto query)
        {
            ArgumentNullException.ThrowIfNull(query);

            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var orders = await this.GetCurrentStoreOrdersAsync();

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var search = query.SearchTerm.Trim().ToLowerInvariant();
                orders = orders.Where(order =>
                    order.Reference.ToLower().Contains(search) ||
                    order.UserId.ToLower().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = query.Status.Trim();
                orders = orders.Where(order => order.OrderStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(query.ShippingStatus))
            {
                if (ShippingStatusNormalizer.TryNormalize(query.ShippingStatus, out var shippingStatus))
                {
                    var aliases = ShippingStatusNormalizer.GetLookupAliases(shippingStatus);
                    orders = orders.Where(order => aliases.Contains(order.ShippingStatus.ToLower()));
                }
                else
                {
                    orders = orders.Where(order => false);
                }
            }

            if (query.FromUtc.HasValue)
            {
                orders = orders.Where(order => order.CreatedOn >= EnsureUtc(query.FromUtc.Value));
            }

            if (query.ToUtc.HasValue)
            {
                orders = orders.Where(order => order.CreatedOn <= EnsureUtc(query.ToUtc.Value));
            }

            var total = await orders.CountAsync();
            var page = await orders
                .OrderByDescending(order => order.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<GetOrder>
            {
                Items = await this.MapOrdersAsync(page),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = total,
            };
        }

        public async Task<ServiceResponse<GetOrder>> GetByIdAsync(Guid id)
        {
            var order = await this.GetOrderEntityAsync(id);
            return order is null
                ? Failure("Order not found.", ServiceResponseType.NotFound)
                : Success((await this.MapOrdersAsync(new[] { order })).Single(), "Order retrieved successfully.");
        }

        public async Task<ServiceResponse<GetOrder>> UpdateTrackingAsync(Guid id, UpdateTrackingRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (id == Guid.Empty)
            {
                return Failure("Order id is required.", ServiceResponseType.ValidationError);
            }

            var updated = await this.trackingService.UpdateTrackingAsync(
                id,
                request.Carrier?.Trim() ?? string.Empty,
                request.TrackingNumber?.Trim() ?? string.Empty,
                request.TrackingUrl?.Trim() ?? string.Empty);

            if (!updated)
            {
                return Failure("Order not found.", ServiceResponseType.NotFound);
            }

            var order = await this.GetOrderEntityAsync(id);
            await this.LogAsync("Order.TrackingUpdated", id, "Order tracking updated.", request);
            return Success((await this.MapOrdersAsync(new[] { order! })).Single(), "Order tracking updated successfully.");
        }

        public async Task<ServiceResponse<GetOrder>> UpdateShippingStatusAsync(Guid id, UpdateShippingStatusRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (id == Guid.Empty)
            {
                return Failure("Order id is required.", ServiceResponseType.ValidationError);
            }

            if (!ShippingStatusNormalizer.TryNormalize(request.ShippingStatus, out var shippingStatus))
            {
                return Failure("Shipping status is invalid.", ServiceResponseType.ValidationError);
            }

            var updated = await this.trackingService.UpdateShippingStatusAsync(
                id,
                shippingStatus,
                request.ShippedOn,
                request.DeliveredOn);

            if (!updated)
            {
                return Failure("Order not found.", ServiceResponseType.NotFound);
            }

            var order = await this.GetOrderEntityAsync(id);
            await this.LogAsync("Order.ShippingStatusUpdated", id, "Order shipping status updated.", request);
            return Success((await this.MapOrdersAsync(new[] { order! })).Single(), "Order shipping status updated successfully.");
        }

        public async Task<ServiceResponse<GetOrder>> UpdateAdminNoteAsync(Guid id, UpdateOrderAdminNoteRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (id == Guid.Empty)
            {
                return Failure("Order id is required.", ServiceResponseType.ValidationError);
            }

            if (request.AdminNote?.Length > 2000)
            {
                return Failure("Admin note must be 2,000 characters or fewer.", ServiceResponseType.ValidationError);
            }

            var orders = await this.GetCurrentStoreOrdersAsync(asTracking: true);
            var order = await orders.Include(item => item.Lines).FirstOrDefaultAsync(item => item.Id == id);
            if (order is null)
            {
                return Failure("Order not found.", ServiceResponseType.NotFound);
            }

            order.AdminNote = string.IsNullOrWhiteSpace(request.AdminNote) ? null : request.AdminNote.Trim();
            OrderLifecycleTransitionHelper.RecordAdminNoteUpdated(this.context, order);
            await this.context.SaveChangesAsync();
            await this.LogAsync("Order.AdminNoteUpdated", id, "Order admin note updated.", new { HasNote = !string.IsNullOrWhiteSpace(order.AdminNote) });

            return Success((await this.MapOrdersAsync(new[] { order })).Single(), "Order admin note updated successfully.");
        }

        public async Task<ServiceResponse<GetOrder>> CompleteAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return Failure("Order id is required.", ServiceResponseType.ValidationError);
            }

            var orders = await this.GetCurrentStoreOrdersAsync(asTracking: true);
            var order = await orders.Include(item => item.Lines).FirstOrDefaultAsync(item => item.Id == id);
            if (order is null)
            {
                return Failure("Order not found.", ServiceResponseType.NotFound);
            }

            if (string.Equals(order.OrderStatus, OrderStatuses.Complete, StringComparison.OrdinalIgnoreCase))
            {
                return Success((await this.MapOrdersAsync(new[] { order })).Single(), "Order is already complete.");
            }

            if (!string.Equals(order.PaymentStatus, PaymentStatuses.Paid, StringComparison.OrdinalIgnoreCase))
            {
                return Failure("Only paid orders can be completed.", ServiceResponseType.ValidationError);
            }

            if (!ShippingStatusNormalizer.IsCompleteAllowed(order.ShippingStatus))
            {
                return Failure("Order must be shipped, delivered, or shipping-not-required before completion.", ServiceResponseType.ValidationError);
            }

            OrderLifecycleTransitionHelper.MarkCompleted(this.context, order, DateTime.UtcNow, source: "admin");
            await this.context.SaveChangesAsync();
            await this.LogAsync("Order.Completed", id, "Order marked complete.", new
            {
                order.Reference,
                order.PaymentStatus,
                order.ShippingStatus,
            });

            return Success((await this.MapOrdersAsync(new[] { order })).Single(), "Order marked complete successfully.");
        }

        public async Task<ServiceResponse<GetOrder>> CancelAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return Failure("Order id is required.", ServiceResponseType.ValidationError);
            }

            var orders = await this.GetCurrentStoreOrdersAsync(asTracking: true);
            var order = await orders.Include(item => item.Lines).FirstOrDefaultAsync(item => item.Id == id);
            if (order is null)
            {
                return Failure("Order not found.", ServiceResponseType.NotFound);
            }

            if (string.Equals(order.OrderStatus, OrderStatuses.Complete, StringComparison.OrdinalIgnoreCase))
            {
                return Failure("Completed orders cannot be cancelled.", ServiceResponseType.ValidationError);
            }

            if (string.Equals(order.OrderStatus, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            {
                return Success((await this.MapOrdersAsync(new[] { order })).Single(), "Order is already cancelled.");
            }

            OrderLifecycleTransitionHelper.MarkCancelled(this.context, order, DateTime.UtcNow, source: "admin");
            await this.context.SaveChangesAsync();
            await this.LogAsync("Order.Cancelled", id, "Order cancelled.", new
            {
                order.Reference,
                order.PaymentStatus,
                order.ShippingStatus,
            });

            return Success((await this.MapOrdersAsync(new[] { order })).Single(), "Order cancelled successfully.");
        }

        private async Task<Order?> GetOrderEntityAsync(Guid id)
        {
            return id == Guid.Empty
                ? null
                : await (await this.GetCurrentStoreOrdersAsync()).Include(order => order.Lines).FirstOrDefaultAsync(order => order.Id == id);
        }

        private async Task<IQueryable<Order>> GetCurrentStoreOrdersAsync(bool asTracking = false)
        {
            IQueryable<Order> orders = this.context.Orders;
            if (!asTracking)
            {
                orders = orders.AsNoTracking();
            }

            var result = await this.storeContext.GetCurrentStoreIdAsync();
            return result.Success
                ? orders.Include(order => order.Lines).Where(order => order.StoreId == result.Payload)
                : orders.Where(order => false);
        }

        private async Task<IReadOnlyList<GetOrder>> MapOrdersAsync(IReadOnlyCollection<Order> orders)
        {
            var productIds = orders.SelectMany(order => order.Lines).Select(line => line.ProductId).Distinct().ToArray();
            var productNames = await this.context.Products
                .AsNoTracking()
                .Where(product => productIds.Contains(product.Id))
                .Select(product => new { product.Id, product.Name })
                .ToDictionaryAsync(product => product.Id, product => product.Name ?? string.Empty);

            var orderIds = orders.Select(order => order.Id).ToArray();
            var historyEntries = await this.context.OrderHistoryEntries
                .AsNoTracking()
                .Where(entry => orderIds.Contains(entry.OrderId))
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
                .ToListAsync();

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
                        PaymentAttemptPublicId = attempt.PublicId,
                        ProviderKey = attempt.ProviderKey,
                        PaymentStatus = attempt.State,
                        PaymentMethodKey = attempt.PaymentMethodKey,
                        AttemptState = attempt.State,
                        Amount = attempt.Amount,
                        CurrencyCode = attempt.CurrencyCode,
                        UpdatedAtUtc = attempt.UpdatedAtUtc,
                    },
                })
                .ToListAsync();

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
                UserId = order.UserId,
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
                AdminNote = order.AdminNote,
                HistoryEntries = historyEntriesByOrder.TryGetValue(order.Id, out var history) ? history : [],
                Lines = order.Lines.Select(line => new GetOrderLine
                {
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    ProductName = line.ProductName ?? (productNames.TryGetValue(line.ProductId, out var productName) ? productName : string.Empty),
                    Sku = line.Sku,
                    Image = line.Image,
                    ProductVariantId = line.ProductVariantId,
                    VariantAttributes = ProductVariantAttributeNormalizer.Deserialize(line.VariantAttributesJson),
                }),
            }).ToArray();
        }

        private async Task LogAsync(string action, Guid orderId, string summary, object metadata)
        {
            await this.auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = action,
                EntityType = "Order",
                EntityId = orderId.ToString(),
                Summary = summary,
                MetadataJson = JsonSerializer.Serialize(metadata),
            });
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

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };
        }

        private static ServiceResponse<GetOrder> Success(GetOrder payload, string message)
        {
            return new ServiceResponse<GetOrder>(true, message, payload.Id)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<GetOrder> Failure(string message, ServiceResponseType responseType)
        {
            return new ServiceResponse<GetOrder>(false, message)
            {
                ResponseType = responseType,
            };
        }
    }
}
