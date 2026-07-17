namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    public sealed class DefaultOrderStockAdjustmentHook : IOrderStockAdjustmentHook
    {
        public Task<OrderStockAdjustmentResult> ApplyAsync(
            OrderStockAdjustmentRequest request,
            CancellationToken cancellationToken = default)
        {
            foreach (var line in request.Lines)
            {
                if (!string.IsNullOrWhiteSpace(line.CartLine.FulfillmentProviderKey))
                {
                    continue;
                }

                if (!line.Product.ManageStock)
                {
                    continue;
                }

                if (line.Variant is not null)
                {
                    line.Variant.Stock -= line.CartLine.Quantity;
                    continue;
                }

                line.Product.Quantity -= line.CartLine.Quantity;
            }

            return Task.FromResult(OrderStockAdjustmentResult.Succeeded());
        }
    }
}
