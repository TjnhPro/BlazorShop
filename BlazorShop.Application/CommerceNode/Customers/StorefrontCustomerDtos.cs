namespace BlazorShop.Application.CommerceNode.Customers
{
    public sealed record StorefrontCustomerResolutionRequest(
        Guid StoreId,
        string Email,
        string? FullName = null,
        string? Phone = null,
        string? AppUserId = null);

    public sealed record StorefrontCustomerProfile(
        Guid Id,
        Guid StoreId,
        string? AppUserId,
        string Email,
        string NormalizedEmail,
        string FullName,
        string? Phone,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? LastCheckoutAt);
}
