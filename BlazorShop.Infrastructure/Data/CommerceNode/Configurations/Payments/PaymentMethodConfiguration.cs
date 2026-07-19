namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Payments
{
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
    {
        public void Configure(EntityTypeBuilder<PaymentMethod> entity)
        {
            entity.Property(method => method.Key)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(method => method.Name)
                .HasMaxLength(160)
                .IsRequired();

            entity.Property(method => method.Description)
                .HasMaxLength(500);

            entity.HasIndex(method => method.Key).IsUnique();

            entity.ToTable(
                table => table.HasCheckConstraint(
                    "ck_payment_methods_key",
                    "\"Key\" in ('cod', 'stripe', 'paypal')"));

            entity.HasData(
                new PaymentMethod
                {
                    Id = Guid.Parse("3604fc1d-cd6a-46ad-ace4-9b5f8e03f43b"),
                    Key = PaymentMethodKeys.Stripe,
                    Name = "Stripe",
                    Description = "Card payments through Stripe.",
                    IsEnabledByDefault = false,
                    SortOrder = 20,
                },
                new PaymentMethod
                {
                    Id = Guid.Parse("6f2c2a7e-9f9b-4a0d-9f7f-2a1b3c4d5e6f"),
                    Key = PaymentMethodKeys.Cod,
                    Name = "Cash on Delivery",
                    Description = "Test checkout payment method for MVP.",
                    IsEnabledByDefault = true,
                    SortOrder = 10,
                },
                new PaymentMethod
                {
                    Id = Guid.Parse("b2e5c1d4-7a9f-4d2c-8f1e-3a4b5c6d7e8f"),
                    Key = PaymentMethodKeys.PayPal,
                    Name = "PayPal",
                    Description = "PayPal payment skeleton.",
                    IsEnabledByDefault = false,
                    SortOrder = 30,
                });
        }
    }
}
