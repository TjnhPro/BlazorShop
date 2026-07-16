namespace BlazorShop.Application.CommerceNode.Carts
{
    using BlazorShop.Application.DTOs;

    public interface IStorefrontCartService
    {
        Task<ServiceResponse<StorefrontCartResult>> CreateOrResumeAsync(
            StorefrontCartCreateOrResumeRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCartSessionDto>> GetAsync(
            Guid storeId,
            string token,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCartSessionDto>> AddLineAsync(
            StorefrontCartAddLineRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCartSessionDto>> UpdateLineAsync(
            StorefrontCartUpdateLineRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCartSessionDto>> RemoveLineAsync(
            Guid storeId,
            string token,
            Guid lineId,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCartSessionDto>> ClearAsync(
            Guid storeId,
            string token,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCartValidationResult>> ValidateAsync(
            Guid storeId,
            string token,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCartSessionDto>> RecalculateAsync(
            StorefrontCartRecalculateRequest request,
            CancellationToken cancellationToken = default);
    }
}
