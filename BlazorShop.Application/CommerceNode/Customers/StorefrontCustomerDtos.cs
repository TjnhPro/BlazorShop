namespace BlazorShop.Application.CommerceNode.Customers
{
    public sealed record StorefrontCustomerResolutionRequest(
        Guid StoreId,
        string Email,
        string? FullName = null,
        string? Phone = null,
        string? AppUserId = null,
        string? FirstName = null,
        string? LastName = null,
        string? Company = null,
        string? PreferredLanguage = null,
        string? PreferredCurrencyCode = null);

    public sealed record StorefrontCustomerProfile(
        Guid Id,
        Guid StoreId,
        string? AppUserId,
        string Email,
        string NormalizedEmail,
        string FullName,
        string? FirstName,
        string? LastName,
        string? Company,
        string? Phone,
        string? PreferredLanguage,
        string? PreferredCurrencyCode,
        bool IsActive,
        DateTimeOffset? LastActivityAtUtc,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? LastCheckoutAt);
}
