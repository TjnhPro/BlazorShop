namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Customers
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class CommerceCustomerConfiguration : IEntityTypeConfiguration<CommerceCustomer>
    {
        public void Configure(EntityTypeBuilder<CommerceCustomer> entity)
        {
            entity.ToTable("commerce_customers");
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.Id).HasColumnName("id");
            entity.Property(customer => customer.StoreId).HasColumnName("store_id");
            entity.Property(customer => customer.AppUserId).HasColumnName("app_user_id").HasMaxLength(450);
            entity.Property(customer => customer.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
            entity.Property(customer => customer.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(256).IsRequired();
            entity.Property(customer => customer.FullName).HasColumnName("full_name").HasMaxLength(256).IsRequired();
            entity.Property(customer => customer.FirstName).HasColumnName("first_name").HasMaxLength(120);
            entity.Property(customer => customer.LastName).HasColumnName("last_name").HasMaxLength(120);
            entity.Property(customer => customer.Company).HasColumnName("company").HasMaxLength(200);
            entity.Property(customer => customer.Phone).HasColumnName("phone").HasMaxLength(64);
            entity.Property(customer => customer.PreferredLanguage).HasColumnName("preferred_language").HasMaxLength(16);
            entity.Property(customer => customer.PreferredCurrencyCode).HasColumnName("preferred_currency_code").HasMaxLength(3);
            entity.Property(customer => customer.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(customer => customer.LastActivityAtUtc).HasColumnName("last_activity_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(customer => customer.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(customer => customer.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(customer => customer.LastCheckoutAt).HasColumnName("last_checkout_at").HasColumnType("timestamp with time zone");

            entity.HasIndex(customer => new { customer.StoreId, customer.NormalizedEmail }).IsUnique();
            entity.HasIndex(customer => customer.AppUserId).HasFilter("app_user_id IS NOT NULL");

            entity.HasOne(customer => customer.Store)
                .WithMany()
                .HasForeignKey(customer => customer.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(customer => customer.AppUser)
                .WithMany()
                .HasForeignKey(customer => customer.AppUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
