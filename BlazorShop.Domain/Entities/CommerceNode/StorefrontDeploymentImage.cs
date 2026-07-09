namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class StorefrontDeploymentImage
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Key { get; set; } = string.Empty;

        public string Image { get; set; } = string.Empty;

        public string? Version { get; set; }

        public bool IsDefault { get; set; }

        public bool IsEnabled { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
