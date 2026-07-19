namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Orders
{
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class OrderItemCheckoutConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> entity)
        {
            entity.HasIndex(orderItem => new { orderItem.StoreId, orderItem.UserId, orderItem.CreatedOn });
        }
    }
}
