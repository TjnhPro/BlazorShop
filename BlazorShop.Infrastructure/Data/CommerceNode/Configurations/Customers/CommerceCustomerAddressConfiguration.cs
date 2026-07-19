namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Customers
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class CommerceCustomerAddressConfiguration : IEntityTypeConfiguration<CommerceCustomerAddress>
    {
        public void Configure(EntityTypeBuilder<CommerceCustomerAddress> entity)
        {
            entity.ToTable("commerce_customer_addresses");
            entity.HasKey(address => address.Id);
            entity.Property(address => address.Id).HasColumnName("id");
            entity.Property(address => address.PublicId).HasColumnName("public_id");
            entity.Property(address => address.StoreId).HasColumnName("store_id");
            entity.Property(address => address.CustomerId).HasColumnName("customer_id");
            entity.Property(address => address.FirstName).HasColumnName("first_name").HasMaxLength(120).IsRequired();
            entity.Property(address => address.LastName).HasColumnName("last_name").HasMaxLength(120).IsRequired();
            entity.Property(address => address.Company).HasColumnName("company").HasMaxLength(160);
            entity.Property(address => address.Address1).HasColumnName("address1").HasMaxLength(240).IsRequired();
            entity.Property(address => address.Address2).HasColumnName("address2").HasMaxLength(240);
            entity.Property(address => address.City).HasColumnName("city").HasMaxLength(120).IsRequired();
            entity.Property(address => address.PostalCode).HasColumnName("postal_code").HasMaxLength(32).IsRequired();
            entity.Property(address => address.CountryCode).HasColumnName("country_code").HasMaxLength(2).IsRequired();
            entity.Property(address => address.StateProvinceCode).HasColumnName("state_province_code").HasMaxLength(64);
            entity.Property(address => address.StateProvinceName).HasColumnName("state_province_name").HasMaxLength(120);
            entity.Property(address => address.Phone).HasColumnName("phone").HasMaxLength(32);
            entity.Property(address => address.Email).HasColumnName("email").HasMaxLength(256);
            entity.Property(address => address.IsDefaultShipping).HasColumnName("is_default_shipping").HasDefaultValue(false);
            entity.Property(address => address.IsDefaultBilling).HasColumnName("is_default_billing").HasDefaultValue(false);
            entity.Property(address => address.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(address => address.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(address => address.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamp with time zone");

            entity.HasIndex(address => address.PublicId).IsUnique();
            entity.HasIndex(address => new { address.StoreId, address.CustomerId, address.DeletedAtUtc });
            entity.HasIndex(address => new { address.StoreId, address.CustomerId, address.CountryCode });
            entity.HasIndex(address => new { address.StoreId, address.CustomerId, address.IsDefaultShipping })
                .IsUnique()
                .HasFilter("is_default_shipping = true AND deleted_at_utc IS NULL");
            entity.HasIndex(address => new { address.StoreId, address.CustomerId, address.IsDefaultBilling })
                .IsUnique()
                .HasFilter("is_default_billing = true AND deleted_at_utc IS NULL");

            entity.HasOne(address => address.Store)
                .WithMany()
                .HasForeignKey(address => address.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(address => address.Customer)
                .WithMany(customer => customer.Addresses)
                .HasForeignKey(address => address.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
