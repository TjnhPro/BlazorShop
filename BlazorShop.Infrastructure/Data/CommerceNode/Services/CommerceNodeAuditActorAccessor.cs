namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using Microsoft.AspNetCore.Http;

    public sealed class CommerceNodeAuditActorAccessor : ICommerceNodeAuditActorAccessor
    {
        public const string ActorIdHeaderName = "X-Control-Plane-Actor-Id";

        public const string ActorEmailHeaderName = "X-Control-Plane-Actor-Email";

        public const string ActionIdHeaderName = "X-Control-Plane-Action-Id";

        private readonly IHttpContextAccessor httpContextAccessor;

        public CommerceNodeAuditActorAccessor(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public CommerceNodeAuditActor GetCurrentActor()
        {
            var httpContext = this.httpContextAccessor.HttpContext;
            return new CommerceNodeAuditActor(
                Normalize(httpContext?.Request.Headers[ActorIdHeaderName].ToString()),
                Normalize(httpContext?.Request.Headers[ActorEmailHeaderName].ToString()),
                Normalize(httpContext?.Request.Headers[ActionIdHeaderName].ToString()),
                httpContext?.Connection.RemoteIpAddress?.ToString(),
                Normalize(httpContext?.Request.Headers.UserAgent.ToString()));
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
