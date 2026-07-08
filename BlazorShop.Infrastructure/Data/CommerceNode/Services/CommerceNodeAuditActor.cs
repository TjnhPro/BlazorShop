namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    public sealed record CommerceNodeAuditActor(
        string? ActorUserId,
        string? ActorEmail,
        string? ActionId,
        string? IpAddress,
        string? UserAgent);
}
