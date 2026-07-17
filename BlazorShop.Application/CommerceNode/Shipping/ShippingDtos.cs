namespace BlazorShop.Application.CommerceNode.Shipping
{
    using BlazorShop.Application.DTOs;

    public sealed record ShippingAddressSnapshot(
        string? FullName,
        string? Company,
        string Address1,
        string? Address2,
        string City,
        string? StateProvinceCode,
        string PostalCode,
        string CountryCode,
        string? Phone,
        string? Email);

    public sealed record ShippingPackageLine(
        Guid ProductId,
        Guid? ProductVariantId,
        int Quantity,
        bool ShippingRequired,
        bool FreeShipping,
        decimal? Weight = null,
        decimal? Length = null,
        decimal? Width = null,
        decimal? Height = null,
        decimal? Surcharge = null);

    public sealed record ShippingOptionDto(
        string Key,
        string ProviderSystemName,
        string MethodCode,
        string DisplayName,
        string? Description,
        decimal Rate,
        string CurrencyCode,
        string? DeliveryEstimateText,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Errors,
        string? RuleMatch);

    public sealed record ShippingOptionsRequest(
        Guid StoreId,
        Guid? CartId,
        Guid? CartPublicId,
        ShippingAddressSnapshot? Address,
        string CurrencyCode,
        decimal Subtotal,
        IReadOnlyList<ShippingPackageLine> PackageLines);

    public sealed record ShippingProviderResult(
        IReadOnlyList<ShippingOptionDto> Options,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Errors)
    {
        public static ShippingProviderResult Empty { get; } = new([], [], []);
    }

    public sealed record ShippingCalculationResult(
        bool ShippingRequired,
        IReadOnlyList<ShippingOptionDto> Options,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Errors);

    public sealed record ShippingTaxCalculationRequest(
        Guid StoreId,
        ShippingAddressSnapshot? Address,
        string CurrencyCode,
        decimal Subtotal,
        decimal ShippingTotal);

    public sealed record ShippingTaxCalculationResult(
        decimal TaxTotal,
        string ReasonCode,
        string Source);

    public interface IShippingProvider
    {
        string ProviderSystemName { get; }

        Task<ShippingProviderResult> GetOptionsAsync(
            ShippingOptionsRequest request,
            CancellationToken cancellationToken = default);
    }

    public interface IShippingProviderResolver
    {
        IReadOnlyList<string> ListProviderSystemNames();

        ServiceResponse<IShippingProvider> Resolve(string providerSystemName);
    }

    public interface IShippingCalculator
    {
        Task<ServiceResponse<ShippingCalculationResult>> GetOptionsAsync(
            ShippingOptionsRequest request,
            CancellationToken cancellationToken = default);
    }

    public interface IShippingTaxCalculator
    {
        Task<ShippingTaxCalculationResult> CalculateAsync(
            ShippingTaxCalculationRequest request,
            CancellationToken cancellationToken = default);
    }
}
