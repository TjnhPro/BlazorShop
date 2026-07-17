namespace BlazorShop.Application.CommerceNode.Shipping
{
    using BlazorShop.Application.DTOs;

    public static class StoreShippingSurchargePolicies
    {
        public const string Sum = "sum";
        public const string Highest = "highest";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Sum,
            Highest,
        };
    }

    public sealed record StoreShippingSettingsDto(
        Guid PublicId,
        StoreShippingOriginDto Origin,
        IReadOnlyList<string> EnabledCountryCodes,
        decimal? DefaultFlatRate,
        decimal? FreeShippingThreshold,
        string SurchargePolicy,
        string? DefaultDeliveryEstimateText,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        string? UpdatedByUserId);

    public sealed record StoreShippingOriginDto(
        string? FullName,
        string? Company,
        string? Address1,
        string? Address2,
        string? City,
        string? StateProvinceCode,
        string? PostalCode,
        string? CountryCode);

    public sealed record UpdateStoreShippingSettingsRequest(
        StoreShippingOriginDto Origin,
        IReadOnlyList<string>? EnabledCountryCodes,
        decimal? DefaultFlatRate,
        decimal? FreeShippingThreshold,
        string SurchargePolicy,
        string? DefaultDeliveryEstimateText);

    public sealed record StoreShippingRuntimeSettings(
        StoreShippingOriginDto Origin,
        IReadOnlyList<string> EnabledCountryCodes,
        decimal? DefaultFlatRate,
        decimal? FreeShippingThreshold,
        string SurchargePolicy,
        string? DefaultDeliveryEstimateText);

    public interface IStoreShippingSettingsService
    {
        Task<ServiceResponse<StoreShippingSettingsDto>> GetAsync(
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreShippingSettingsDto>> UpdateAsync(
            UpdateStoreShippingSettingsRequest request,
            CancellationToken cancellationToken = default);

        Task<StoreShippingRuntimeSettings> ResolveAsync(
            Guid storeId,
            CancellationToken cancellationToken = default);

        Task<StoreShippingRuntimeSettings> ResolveCurrentAsync(
            CancellationToken cancellationToken = default);
    }
}
