namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeAdminShipmentService : IAdminShipmentService
    {
        private readonly CommerceNodeDbContext context;
        private readonly IAdminAuditService auditService;
        private readonly ICommerceStoreContext storeContext;

        public CommerceNodeAdminShipmentService(
            CommerceNodeDbContext context,
            IAdminAuditService auditService,
            ICommerceStoreContext storeContext)
        {
            this.context = context;
            this.auditService = auditService;
            this.storeContext = storeContext;
        }

        public async Task<ServiceResponse<GetShipment>> GetShipmentAsync(Guid orderId)
        {
            if (orderId == Guid.Empty)
            {
                return Failure("Order id is required.", ServiceResponseType.ValidationError);
            }

            var storeId = await this.ResolveCurrentStoreIdAsync();
            if (!storeId.HasValue)
            {
                return Failure("Order not found.", ServiceResponseType.NotFound);
            }

            var orderExists = await this.context.Orders
                .AsNoTracking()
                .AnyAsync(order => order.Id == orderId && order.StoreId == storeId);

            if (!orderExists)
            {
                return Failure("Order not found.", ServiceResponseType.NotFound);
            }

            var shipment = await this.context.Shipments
                .AsNoTracking()
                .Include(item => item.Order)
                .Include(item => item.Items)
                .Include(item => item.TrackingEvents)
                .FirstOrDefaultAsync(item => item.OrderId == orderId && item.StoreId == storeId);

            return shipment is null
                ? Failure("Shipment not found.", ServiceResponseType.NotFound)
                : Success(MapShipment(shipment), "Shipment retrieved successfully.");
        }

        public async Task<ServiceResponse<GetShipment>> UpsertShipmentAsync(Guid orderId, UpsertShipmentRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (orderId == Guid.Empty)
            {
                return Failure("Order id is required.", ServiceResponseType.ValidationError);
            }

            var validationMessage = Validate(request);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                return Failure(validationMessage, ServiceResponseType.ValidationError);
            }

            var storeId = await this.ResolveCurrentStoreIdAsync();
            if (!storeId.HasValue)
            {
                return Failure("Order not found.", ServiceResponseType.NotFound);
            }

            var order = await this.context.Orders
                .Include(item => item.Lines)
                .FirstOrDefaultAsync(item => item.Id == orderId && item.StoreId == storeId);

            if (order is null)
            {
                return Failure("Order not found.", ServiceResponseType.NotFound);
            }

            validationMessage = ValidateShipmentItems(request.Items, order.Lines);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                return Failure(validationMessage, ServiceResponseType.ValidationError);
            }

            var now = DateTime.UtcNow;
            var shipDate = EnsureUtc(request.ShipDate);
            var shipment = await this.context.Shipments
                .Include(item => item.Items)
                .FirstOrDefaultAsync(item => item.OrderId == orderId && item.StoreId == storeId);

            var isNewShipment = shipment is null;
            var trackingChanged = false;

            if (shipment is null)
            {
                shipment = new Shipment
                {
                    StoreId = storeId.Value,
                    OrderId = order.Id,
                    CreatedAt = now,
                };
                this.context.Shipments.Add(shipment);
            }
            else
            {
                trackingChanged = !string.Equals(shipment.CarrierName, request.CarrierName.Trim(), StringComparison.Ordinal)
                    || !string.Equals(shipment.CarrierService, NormalizeOptional(request.CarrierService), StringComparison.Ordinal)
                    || !string.Equals(shipment.TrackingNumber, request.TrackingNumber.Trim(), StringComparison.Ordinal)
                    || !string.Equals(shipment.TrackingUrl, NormalizeOptional(request.TrackingUrl), StringComparison.Ordinal)
                    || shipment.ShipDate != shipDate;
            }

            shipment.ShipDate = shipDate;
            shipment.CarrierName = request.CarrierName.Trim();
            shipment.CarrierService = NormalizeOptional(request.CarrierService);
            shipment.TrackingNumber = request.TrackingNumber.Trim();
            shipment.TrackingUrl = NormalizeOptional(request.TrackingUrl);
            shipment.Note = NormalizeOptional(request.Note);
            shipment.UpdatedAt = now;

            if (request.Items is not null)
            {
                shipment.Items.Clear();
                foreach (var item in request.Items)
                {
                    shipment.Items.Add(new ShipmentItem
                    {
                        ShipmentId = shipment.Id,
                        OrderLineId = item.OrderLineId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }
            }

            if (isNewShipment)
            {
                this.context.ShipmentTrackingEvents.Add(new ShipmentTrackingEvent
                {
                    ShipmentId = shipment.Id,
                    StoreId = storeId.Value,
                    OrderId = order.Id,
                    Status = "shipped",
                    Message = "Shipment created.",
                    OccurredAtUtc = shipDate,
                    Source = "manual_admin",
                    CreatedAt = now,
                });
            }
            else if (trackingChanged)
            {
                this.context.ShipmentTrackingEvents.Add(new ShipmentTrackingEvent
                {
                    ShipmentId = shipment.Id,
                    StoreId = storeId.Value,
                    OrderId = order.Id,
                    Status = "tracking_updated",
                    Message = "Shipment tracking updated.",
                    OccurredAtUtc = now,
                    Source = "manual_admin",
                    CreatedAt = now,
                });
            }

            order.ShippingStatus = "Shipped";
            order.ShippedOn = shipDate;
            order.ShippingCarrier = shipment.CarrierName;
            order.TrackingNumber = shipment.TrackingNumber;
            order.TrackingUrl = shipment.TrackingUrl;
            order.LastTrackingUpdate = now;

            await this.context.SaveChangesAsync();
            var trackingEventCount = await this.context.ShipmentTrackingEvents
                .AsNoTracking()
                .CountAsync(item => item.ShipmentId == shipment.Id);

            await this.LogAsync("Order.ShipmentUpserted", order.Id, "Order shipment upserted.", new
            {
                shipment.Id,
                shipment.CarrierName,
                shipment.CarrierService,
                shipment.TrackingNumber,
                shipment.TrackingUrl,
                shipment.ShipDate,
                HasNote = !string.IsNullOrWhiteSpace(shipment.Note),
                ItemCount = shipment.Items.Count,
                TrackingEventCount = trackingEventCount,
            });

            var savedShipment = await this.context.Shipments
                .AsNoTracking()
                .Include(item => item.Order)
                .Include(item => item.Items)
                .Include(item => item.TrackingEvents)
                .FirstAsync(item => item.Id == shipment.Id);

            return Success(MapShipment(savedShipment), "Shipment saved successfully.");
        }

        private async Task<Guid?> ResolveCurrentStoreIdAsync()
        {
            var result = await this.storeContext.GetCurrentStoreIdAsync();
            return result.Success ? result.Payload : null;
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

        private static string? Validate(UpsertShipmentRequest request)
        {
            if (request.ShipDate == default)
            {
                return "Ship date is required.";
            }

            if (string.IsNullOrWhiteSpace(request.CarrierName))
            {
                return "Carrier name is required.";
            }

            if (request.CarrierName.Trim().Length > 128)
            {
                return "Carrier name must be 128 characters or fewer.";
            }

            if (request.CarrierService?.Trim().Length > 128)
            {
                return "Carrier service must be 128 characters or fewer.";
            }

            if (string.IsNullOrWhiteSpace(request.TrackingNumber))
            {
                return "Tracking number is required.";
            }

            if (request.TrackingNumber.Trim().Length > 160)
            {
                return "Tracking number must be 160 characters or fewer.";
            }

            if (request.TrackingUrl?.Trim().Length > 1024)
            {
                return "Tracking URL must be 1,024 characters or fewer.";
            }

            if (request.Note?.Trim().Length > 1000)
            {
                return "Shipment note must be 1,000 characters or fewer.";
            }

            return null;
        }

        private static string? ValidateShipmentItems(
            IReadOnlyList<UpsertShipmentItemRequest>? requestItems,
            IEnumerable<OrderLine> orderLines)
        {
            if (requestItems is null || requestItems.Count == 0)
            {
                return null;
            }

            var orderLineLookup = orderLines.ToDictionary(item => item.Id);
            var totalsByLine = new Dictionary<Guid, int>();

            foreach (var item in requestItems)
            {
                if (item.OrderLineId == Guid.Empty)
                {
                    return "Shipment item order line id is required.";
                }

                if (item.ProductId == Guid.Empty)
                {
                    return "Shipment item product id is required.";
                }

                if (item.Quantity <= 0)
                {
                    return "Shipment item quantity must be greater than zero.";
                }

                if (!orderLineLookup.TryGetValue(item.OrderLineId, out var orderLine))
                {
                    return "Shipment item order line does not belong to this order.";
                }

                if (orderLine.ProductId != item.ProductId)
                {
                    return "Shipment item product id must match the order line.";
                }

                if (totalsByLine.ContainsKey(item.OrderLineId))
                {
                    return "Shipment item order line can only appear once.";
                }

                totalsByLine[item.OrderLineId] = item.Quantity;
                if (item.Quantity > orderLine.Quantity)
                {
                    return "Shipment item quantity cannot exceed ordered quantity.";
                }
            }

            return null;
        }

        private static GetShipment MapShipment(Shipment shipment)
        {
            return new GetShipment
            {
                Id = shipment.Id,
                StoreId = shipment.StoreId,
                OrderId = shipment.OrderId,
                ShipDate = shipment.ShipDate,
                CarrierName = shipment.CarrierName,
                CarrierService = shipment.CarrierService,
                TrackingNumber = shipment.TrackingNumber,
                TrackingUrl = shipment.TrackingUrl,
                Note = shipment.Note,
                ShippingStatus = shipment.Order?.ShippingStatus ?? string.Empty,
                DeliveredOn = shipment.Order?.DeliveredOn,
                ShippingMethod = shipment.Order is null
                    ? null
                    : new GetShipmentShippingMethod
                    {
                        Key = shipment.Order.ShippingMethodKey,
                        ProviderSystemName = shipment.Order.ShippingProviderSystemName,
                        MethodCode = shipment.Order.ShippingMethodCode,
                        MethodName = shipment.Order.ShippingMethodName,
                        ShippingTotal = shipment.Order.ShippingTotal,
                        CurrencyCode = shipment.Order.ShippingCurrencyCode,
                        DeliveryEstimateText = shipment.Order.ShippingDeliveryEstimateText,
                    },
                CreatedAt = shipment.CreatedAt,
                UpdatedAt = shipment.UpdatedAt,
                Items = shipment.Items
                    .OrderBy(item => item.CreatedAt)
                    .Select(item => new GetShipmentItem
                    {
                        Id = item.Id,
                        OrderLineId = item.OrderLineId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                    })
                    .ToList(),
                TrackingEvents = shipment.TrackingEvents
                    .OrderBy(item => item.OccurredAtUtc)
                    .Select(item => new GetShipmentTrackingEvent
                    {
                        Id = item.Id,
                        Status = item.Status,
                        Message = item.Message,
                        OccurredAtUtc = item.OccurredAtUtc,
                        Location = item.Location,
                        Source = item.Source,
                    })
                    .ToList(),
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

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static ServiceResponse<GetShipment> Success(GetShipment payload, string message)
        {
            return new ServiceResponse<GetShipment>(true, message, payload.Id)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<GetShipment> Failure(string message, ServiceResponseType responseType)
        {
            return new ServiceResponse<GetShipment>(false, message)
            {
                ResponseType = responseType,
            };
        }
    }
}
