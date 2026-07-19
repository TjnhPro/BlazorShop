namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Content
{
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class StoreNavigationMenuConfiguration : IEntityTypeConfiguration<StoreNavigationMenu>
    {
        public void Configure(EntityTypeBuilder<StoreNavigationMenu> entity)
        {
            entity.ToTable("store_navigation_menu");
            entity.HasKey(menu => menu.Id);
            entity.Property(menu => menu.Id).HasColumnName("id");
            entity.Property(menu => menu.PublicId).HasColumnName("public_id");
            entity.Property(menu => menu.StoreId).HasColumnName("store_id");
            entity.Property(menu => menu.SystemName).HasColumnName("system_name").HasMaxLength(80).IsRequired();
            entity.Property(menu => menu.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
            entity.Property(menu => menu.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
            entity.Property(menu => menu.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(menu => menu.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(menu => menu.ArchivedAt).HasColumnName("archived_at").HasColumnType("timestamp with time zone");

            entity.HasIndex(menu => menu.PublicId).IsUnique();
            entity.HasIndex(menu => new { menu.StoreId, menu.SystemName })
                .IsUnique()
                .HasFilter("archived_at IS NULL");
            entity.HasIndex(menu => new { menu.StoreId, menu.IsEnabled });

            entity.HasOne(menu => menu.Store)
                .WithMany()
                .HasForeignKey(menu => menu.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(
                "store_navigation_menu",
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_store_navigation_menu_system_name",
                        $"system_name in ({CommerceNodeSql.In(StoreNavigationMenuNames.All)})");
                });
        }
    }
}
