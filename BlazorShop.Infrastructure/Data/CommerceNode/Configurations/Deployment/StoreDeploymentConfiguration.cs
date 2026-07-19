namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Deployment
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class StoreDeploymentConfiguration : IEntityTypeConfiguration<StoreDeployment>
    {
        public void Configure(EntityTypeBuilder<StoreDeployment> entity)
        {
            entity.ToTable("store_deployment");
            entity.HasKey(deployment => deployment.Id);
            entity.Property(deployment => deployment.Id).HasColumnName("id");
            entity.Property(deployment => deployment.StoreId).HasColumnName("store_id");
            entity.Property(deployment => deployment.TaskId).HasColumnName("task_id");
            entity.Property(deployment => deployment.StorefrontImage).HasColumnName("storefront_image").IsRequired();
            entity.Property(deployment => deployment.ContainerName).HasColumnName("container_name").IsRequired();
            entity.Property(deployment => deployment.NetworkName).HasColumnName("network_name");
            entity.Property(deployment => deployment.PublicUrl).HasColumnName("public_url");
            entity.Property(deployment => deployment.InternalUrl).HasColumnName("internal_url");
            entity.Property(deployment => deployment.NginxServerName).HasColumnName("nginx_server_name");
            entity.Property(deployment => deployment.NginxConfigPath).HasColumnName("nginx_config_path");
            entity.Property(deployment => deployment.EnvFilePath).HasColumnName("env_file_path");
            entity.Property(deployment => deployment.Status).HasColumnName("status").IsRequired();
            entity.Property(deployment => deployment.LastHealthStatus).HasColumnName("last_health_status");
            entity.Property(deployment => deployment.LastHealthAt).HasColumnName("last_health_at").HasColumnType("timestamp with time zone");
            entity.Property(deployment => deployment.DeployedAt).HasColumnName("deployed_at").HasColumnType("timestamp with time zone");
            entity.Property(deployment => deployment.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(deployment => deployment.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(deployment => deployment.Task).WithMany().HasForeignKey(deployment => deployment.TaskId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(deployment => deployment.Store).WithOne().HasForeignKey<StoreDeployment>(deployment => deployment.StoreId).OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(deployment => deployment.StoreId).IsUnique();
            entity.HasIndex(deployment => deployment.ContainerName).IsUnique();
            entity.HasIndex(deployment => deployment.Status);

            entity.ToTable(table => table.HasCheckConstraint("ck_store_deployment_status", "status in ('provisioning', 'active', 'failed', 'disabled', 'removed')"));
        }
    }
}
