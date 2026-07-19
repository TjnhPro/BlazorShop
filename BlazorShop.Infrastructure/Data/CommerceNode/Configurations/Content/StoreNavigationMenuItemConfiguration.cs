namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Content
{
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class StoreNavigationMenuItemConfiguration : IEntityTypeConfiguration<StoreNavigationMenuItem>
    {
        public void Configure(EntityTypeBuilder<StoreNavigationMenuItem> entity)
        {
            entity.ToTable("store_navigation_menu_item");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.PublicId).HasColumnName("public_id");
            entity.Property(item => item.StoreId).HasColumnName("store_id");
            entity.Property(item => item.MenuId).HasColumnName("menu_id");
            entity.Property(item => item.ParentItemId).HasColumnName("parent_item_id");
            entity.Property(item => item.Label).HasColumnName("label").HasMaxLength(200).IsRequired();
            entity.Property(item => item.TargetType).HasColumnName("target_type").HasMaxLength(50).IsRequired();
            entity.Property(item => item.TargetKey).HasColumnName("target_key").HasMaxLength(120);
            entity.Property(item => item.TargetEntityPublicId).HasColumnName("target_entity_public_id");
            entity.Property(item => item.Url).HasColumnName("url").HasMaxLength(2048);
            entity.Property(item => item.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
            entity.Property(item => item.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
            entity.Property(item => item.OpensInNewTab).HasColumnName("opens_in_new_tab").HasDefaultValue(false);
            entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(item => item.ArchivedAt).HasColumnName("archived_at").HasColumnType("timestamp with time zone");

            entity.HasIndex(item => item.PublicId).IsUnique();
            entity.HasIndex(item => new { item.StoreId, item.MenuId, item.ParentItemId, item.DisplayOrder });
            entity.HasIndex(item => new { item.StoreId, item.TargetType, item.TargetEntityPublicId });
            entity.HasIndex(item => new { item.MenuId, item.IsEnabled, item.ArchivedAt });

            entity.HasOne(item => item.Store)
                .WithMany()
                .HasForeignKey(item => item.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.Menu)
                .WithMany(menu => menu.Items)
                .HasForeignKey(item => item.MenuId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.ParentItem)
                .WithMany(item => item.Children)
                .HasForeignKey(item => item.ParentItemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(
                "store_navigation_menu_item",
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_store_navigation_menu_item_target_type",
                        $"target_type in ({CommerceNodeSql.In(StoreNavigationTargetTypes.All)})");
                    table.HasCheckConstraint("ck_store_navigation_menu_item_display_order", "display_order >= 0");
                    table.HasCheckConstraint(
                        "ck_store_navigation_menu_item_external_url",
                        "target_type <> 'external_url' OR url LIKE 'https://%'");
                    table.HasCheckConstraint(
                        "ck_store_navigation_menu_item_group_shape",
                        "target_type <> 'group' OR (target_key IS NULL AND target_entity_public_id IS NULL AND url IS NULL)");
                });
        }
    }
}
