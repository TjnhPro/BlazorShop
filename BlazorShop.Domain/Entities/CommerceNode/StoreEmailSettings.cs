namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class StoreEmailSettings
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public bool Enabled { get; set; }

        public string? SmtpHost { get; set; }

        public int SmtpPort { get; set; } = 587;

        public bool UseSsl { get; set; } = true;

        public string? Username { get; set; }

        public string? ProtectedPassword { get; set; }

        public DateTimeOffset? PasswordUpdatedAtUtc { get; set; }

        public string? FromEmail { get; set; }

        public string? FromDisplayName { get; set; }

        public string? ReplyToEmail { get; set; }

        public string DeliveryMode { get; set; } = StoreEmailDeliveryModes.Smtp;

        public string? CaptureRedirectToEmail { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public string? UpdatedByUserId { get; set; }

        public CommerceStore? Store { get; set; }
    }

    public static class StoreEmailDeliveryModes
    {
        public const string Smtp = "smtp";

        public const string Capture = "capture";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Smtp,
            Capture,
        };
    }
}
