namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class StoreShippingSettings
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public CommerceStore? Store { get; set; }

        public string? OriginFullName { get; set; }

        public string? OriginCompany { get; set; }

        public string? OriginAddress1 { get; set; }

        public string? OriginAddress2 { get; set; }

        public string? OriginCity { get; set; }

        public string? OriginStateProvinceCode { get; set; }

        public string? OriginPostalCode { get; set; }

        public string? OriginCountryCode { get; set; }

        public string? EnabledCountryCodesJson { get; set; }

        public decimal? DefaultFlatRate { get; set; }

        public decimal? FreeShippingThreshold { get; set; }

        public string SurchargePolicy { get; set; } = "sum";

        public string? DefaultDeliveryEstimateText { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public string? UpdatedByUserId { get; set; }
    }
}
