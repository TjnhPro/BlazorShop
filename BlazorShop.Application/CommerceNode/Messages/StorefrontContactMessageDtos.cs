namespace BlazorShop.Application.CommerceNode.Messages
{
    using BlazorShop.Application.DTOs;

    public sealed record StorefrontContactMessageRequest(
        string Name,
        string Email,
        string Subject,
        string Message);

    public sealed record StorefrontContactMessageResult(
        bool Accepted,
        string Message);

    public interface IStorefrontContactMessageService
    {
        Task<ServiceResponse<StorefrontContactMessageResult>> SendAsync(
            StorefrontContactMessageRequest request,
            CancellationToken cancellationToken = default);
    }
}
