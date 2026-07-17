namespace BlazorShop.Application.CommerceNode.Messages
{
    public interface IMessageTokenRenderer
    {
        MessageTokenRenderResult Render(MessageTokenRenderRequest request);
    }
}
