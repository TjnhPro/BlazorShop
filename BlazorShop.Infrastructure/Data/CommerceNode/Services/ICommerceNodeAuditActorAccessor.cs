namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    public interface ICommerceNodeAuditActorAccessor
    {
        CommerceNodeAuditActor GetCurrentActor();
    }
}
