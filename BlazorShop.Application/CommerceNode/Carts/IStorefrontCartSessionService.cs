namespace BlazorShop.Application.CommerceNode.Carts
{
    using BlazorShop.Application.DTOs;

    public interface IStorefrontCartSessionService
    {
        Task<ServiceResponse<StorefrontCartSessionCreated>> CreateAsync(
            StorefrontCartSessionCreateRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCartSessionDto>> ResolveAsync(
            Guid storeId,
            string token,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCartSessionDto>> AttachOrMergeCurrentCustomerAsync(
            StorefrontCartAttachCurrentCustomerRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCartSessionDto>> AddOrUpdateLineAsync(
            StorefrontCartLineMutationRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCartSessionDto>> UpdateLineQuantityAsync(
            Guid storeId,
            string token,
            Guid lineId,
            int quantity,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCartSessionDto>> UpdateLineSnapshotsAsync(
            Guid storeId,
            string token,
            IReadOnlyList<StorefrontCartLineSnapshotUpdate> updates,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontCartSessionDto>> RemoveLineAsync(
            Guid storeId,
            string token,
            Guid lineId,
            CancellationToken cancellationToken = default);
    }
}
