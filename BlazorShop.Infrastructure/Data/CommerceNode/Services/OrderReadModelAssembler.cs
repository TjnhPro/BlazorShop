namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Domain.Entities.Payment;

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

            return orders.Count == 0
                ? Task.FromResult<IReadOnlyList<GetOrder>>([])
                : throw new NotSupportedException("Order read model projection is introduced in the next assembler phase.");
        }
    }
}
