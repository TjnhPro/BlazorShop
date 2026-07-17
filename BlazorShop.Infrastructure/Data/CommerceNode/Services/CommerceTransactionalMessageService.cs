namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Globalization;

    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.Options;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;

    public sealed class CommerceTransactionalMessageService : ICommerceTransactionalMessageService
    {
        private readonly CommerceNodeDbContext context;
        private readonly IMessageQueueService messageQueueService;
        private readonly ClientAppOptions clientAppOptions;

        public CommerceTransactionalMessageService(
            CommerceNodeDbContext context,
            IMessageQueueService messageQueueService,
            IOptions<ClientAppOptions> clientAppOptions)
        {
            this.context = context;
            this.messageQueueService = messageQueueService;
            this.clientAppOptions = clientAppOptions.Value;
        }

        public Task<QueuedMessageResult> QueueOrderPlacedAsync(
            Guid storeId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.QueueOrderMessageAsync(
                storeId,
                orderId,
                TransactionalMessageTemplateSystemNames.OrderPlaced,
                $"order.placed:{orderId:N}",
                cancellationToken);
        }

        public async Task<QueuedMessageResult> QueuePaymentStatusChangedAsync(
            Guid storeId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            var order = await this.LoadOrderAsync(storeId, orderId, cancellationToken);
            if (order is null)
            {
                return new QueuedMessageResult(false, ErrorCode: "order.not_found", Message: "Order was not found.");
            }

            return await this.QueueOrderMessageAsync(
                order,
                TransactionalMessageTemplateSystemNames.OrderPaymentStatusChanged,
                $"order.payment_status_changed:{order.Id:N}:{NormalizeKey(order.PaymentStatus)}",
                cancellationToken);
        }

        public async Task<QueuedMessageResult> QueueFulfillmentStatusChangedAsync(
            Guid storeId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            var order = await this.LoadOrderAsync(storeId, orderId, cancellationToken);
            if (order is null)
            {
                return new QueuedMessageResult(false, ErrorCode: "order.not_found", Message: "Order was not found.");
            }

            return await this.QueueOrderMessageAsync(
                order,
                TransactionalMessageTemplateSystemNames.OrderFulfillmentStatusChanged,
                $"order.fulfillment_status_changed:{order.Id:N}:{NormalizeKey(order.ShippingStatus)}",
                cancellationToken);
        }

        private async Task<QueuedMessageResult> QueueOrderMessageAsync(
            Guid storeId,
            Guid orderId,
            string templateSystemName,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            var order = await this.LoadOrderAsync(storeId, orderId, cancellationToken);
            if (order is null)
            {
                return new QueuedMessageResult(false, ErrorCode: "order.not_found", Message: "Order was not found.");
            }

            return await this.QueueOrderMessageAsync(order, templateSystemName, idempotencyKey, cancellationToken);
        }

        private async Task<QueuedMessageResult> QueueOrderMessageAsync(
            Order order,
            string templateSystemName,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            var customerEmail = NormalizeOptional(order.CustomerEmail);
            if (customerEmail is null)
            {
                return new QueuedMessageResult(true, Message: "Order has no customer email; message skipped.");
            }

            var store = await this.LoadStoreAsync(order.StoreId, cancellationToken);
            var tokens = CreateOrderTokens(order, store);
            return await this.messageQueueService.QueueAsync(
                new QueueTransactionalMessageRequest(
                    order.StoreId ?? Guid.Empty,
                    templateSystemName,
                    customerEmail,
                    NormalizeOptional(order.CustomerName),
                    NormalizeOptional(store?.DefaultCulture),
                    tokens,
                    idempotencyKey,
                    order.Reference,
                    "order",
                    order.Id.ToString("D")),
                cancellationToken);
        }

        private async Task<Order?> LoadOrderAsync(
            Guid storeId,
            Guid orderId,
            CancellationToken cancellationToken)
        {
            if (storeId == Guid.Empty || orderId == Guid.Empty)
            {
                return null;
            }

            return await this.context.Orders
                .FirstOrDefaultAsync(
                    order => order.Id == orderId && order.StoreId == storeId,
                    cancellationToken);
        }

        private async Task<CommerceStore?> LoadStoreAsync(Guid? storeId, CancellationToken cancellationToken)
        {
            if (!storeId.HasValue)
            {
                return null;
            }

            return await this.context.CommerceStores
                .AsNoTracking()
                .FirstOrDefaultAsync(store => store.Id == storeId.Value, cancellationToken);
        }

        private IReadOnlyDictionary<string, string?> CreateOrderTokens(Order order, CommerceStore? store)
        {
            var storeName = NormalizeOptional(order.StoreNameSnapshot)
                ?? NormalizeOptional(store?.Name)
                ?? "Store";
            var storeUrl = NormalizeOptional(order.StoreBaseUrlSnapshot)
                ?? NormalizeOptional(store?.BaseUrl)
                ?? this.clientAppOptions.BaseUrl;
            var detailUrl = BuildUrl(storeUrl, $"account/orders/{Uri.EscapeDataString(order.Reference)}");
            var receiptUrl = BuildUrl(storeUrl, $"account/orders/{Uri.EscapeDataString(order.Reference)}/receipt");
            var (firstName, lastName) = SplitName(order.CustomerName);

            return new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Store.Name"] = storeName,
                ["Store.Url"] = storeUrl,
                ["Store.SupportEmail"] = NormalizeOptional(store?.SupportEmail ?? order.StoreCompanyEmailSnapshot),
                ["Store.SupportPhone"] = NormalizeOptional(store?.SupportPhone ?? order.StoreCompanyPhoneSnapshot),
                ["Customer.Email"] = NormalizeOptional(order.CustomerEmail),
                ["Customer.FullName"] = NormalizeOptional(order.CustomerName) ?? NormalizeOptional(order.CustomerEmail),
                ["Customer.FirstName"] = firstName,
                ["Customer.LastName"] = lastName,
                ["Order.Reference"] = order.Reference,
                ["Order.CreatedAt"] = order.CreatedOn.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                ["Order.Total"] = order.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture),
                ["Order.Currency"] = order.CurrencyCode,
                ["Order.Status"] = order.OrderStatus,
                ["Order.PaymentStatus"] = order.PaymentStatus,
                ["Order.ShippingStatus"] = order.ShippingStatus,
                ["Order.DetailUrl"] = detailUrl,
                ["Order.ReceiptUrl"] = receiptUrl,
                ["Shipment.Carrier"] = NormalizeOptional(order.ShippingCarrier),
                ["Shipment.TrackingNumber"] = NormalizeOptional(order.TrackingNumber),
                ["Shipment.TrackingUrl"] = NormalizeOptional(order.TrackingUrl),
            };
        }

        private static string BuildUrl(string baseUrl, string path)
        {
            return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        }

        private static (string? FirstName, string? LastName) SplitName(string? fullName)
        {
            var normalized = NormalizeOptional(fullName);
            if (normalized is null)
            {
                return (null, null);
            }

            var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length switch
            {
                0 => (null, null),
                1 => (parts[0], null),
                _ => (parts[0], string.Join(" ", parts.Skip(1))),
            };
        }

        private static string NormalizeKey(string? value)
        {
            return NormalizeOptional(value)?.ToLowerInvariant() ?? "unknown";
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
