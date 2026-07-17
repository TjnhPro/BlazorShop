namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class CommerceCustomerAddress
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public Guid CustomerId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Company { get; set; }

        public string Address1 { get; set; } = string.Empty;

        public string? Address2 { get; set; }

        public string City { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public string CountryCode { get; set; } = string.Empty;

        public string? StateProvinceCode { get; set; }

        public string? StateProvinceName { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public bool IsDefaultShipping { get; set; }

        public bool IsDefaultBilling { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? DeletedAtUtc { get; set; }

        public CommerceStore? Store { get; set; }

        public CommerceCustomer? Customer { get; set; }
    }
}
