namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class StoreDeployment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public CommerceStore? Store { get; set; }

        public Guid? TaskId { get; set; }

        public CommerceTask? Task { get; set; }

        public string StorefrontImage { get; set; } = string.Empty;

        public string ContainerName { get; set; } = string.Empty;

        public string? NetworkName { get; set; }

        public string? PublicUrl { get; set; }

        public string? InternalUrl { get; set; }

        public string? NginxServerName { get; set; }

        public string? NginxConfigPath { get; set; }

        public string? EnvFilePath { get; set; }

        public string Status { get; set; } = StoreDeploymentStatuses.Provisioning;

        public string? LastHealthStatus { get; set; }

        public DateTimeOffset? LastHealthAt { get; set; }

        public DateTimeOffset? DeployedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
