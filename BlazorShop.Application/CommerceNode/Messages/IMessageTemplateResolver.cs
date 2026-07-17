namespace BlazorShop.Application.CommerceNode.Messages
{
    public interface IMessageTemplateResolver
    {
        Task<MessageTemplateResolutionResult> ResolveAsync(
            MessageTemplateResolutionRequest request,
            CancellationToken cancellationToken = default);
    }
}
