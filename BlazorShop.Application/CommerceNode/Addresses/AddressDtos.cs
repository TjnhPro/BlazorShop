namespace BlazorShop.Application.CommerceNode.Addresses
{
    using BlazorShop.Application.DTOs;

    public sealed record CustomerAddressCreateRequest(
        string FirstName,
        string LastName,
        string? Company,
        string Address1,
        string? Address2,
        string City,
        string PostalCode,
        string CountryCode,
        string? StateProvinceCode,
        string? StateProvinceName,
        string? Phone,
        string? Email,
        bool IsDefaultShipping,
        bool IsDefaultBilling);

    public sealed record CustomerAddressUpdateRequest(
        Guid AddressPublicId,
        string FirstName,
        string LastName,
        string? Company,
        string Address1,
        string? Address2,
        string City,
        string PostalCode,
        string CountryCode,
        string? StateProvinceCode,
        string? StateProvinceName,
        string? Phone,
        string? Email,
        bool IsDefaultShipping,
        bool IsDefaultBilling);

    public sealed record CustomerAddressDto(
        Guid PublicId,
        string FirstName,
        string LastName,
        string? Company,
        string Address1,
        string? Address2,
        string City,
        string PostalCode,
        string CountryCode,
        string? StateProvinceCode,
        string? StateProvinceName,
        string? Phone,
        string? Email,
        bool IsDefaultShipping,
        bool IsDefaultBilling,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

    public sealed record StorefrontCustomerAddressContext(
        Guid StoreId,
        string AppUserId,
        string? Email,
        string? FullName);

    public sealed record AddressValidationIssue(
        string Code,
        string Message,
        string Field);

    public sealed record AddressValidationResult(
        CustomerAddressCreateRequest Address,
        IReadOnlyList<AddressValidationIssue> Issues)
    {
        public bool IsValid => this.Issues.Count == 0;
    }

    public interface IAddressValidationService
    {
        AddressValidationResult ValidateAndNormalize(CustomerAddressCreateRequest request);

        AddressValidationResult ValidateAndNormalize(CustomerAddressUpdateRequest request);
    }

    public interface IStorefrontCustomerAddressService
    {
        Task<ServiceResponse<IReadOnlyList<CustomerAddressDto>>> ListAsync(
            StorefrontCustomerAddressContext context,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<CustomerAddressDto>> CreateAsync(
            StorefrontCustomerAddressContext context,
            CustomerAddressCreateRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<CustomerAddressDto>> UpdateAsync(
            StorefrontCustomerAddressContext context,
            Guid addressPublicId,
            CustomerAddressCreateRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse> DeleteAsync(
            StorefrontCustomerAddressContext context,
            Guid addressPublicId,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<CustomerAddressDto>> SetDefaultShippingAsync(
            StorefrontCustomerAddressContext context,
            Guid addressPublicId,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<CustomerAddressDto>> SetDefaultBillingAsync(
            StorefrontCustomerAddressContext context,
            Guid addressPublicId,
            CancellationToken cancellationToken = default);
    }
}
