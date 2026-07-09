namespace BlazorShop.ControlPlane.API.Middleware
{
    using Microsoft.Extensions.Primitives;

    public sealed class ControlPlaneCorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-ID";

        private readonly RequestDelegate next;
        private readonly ILogger<ControlPlaneCorrelationIdMiddleware> logger;

        public ControlPlaneCorrelationIdMiddleware(
            RequestDelegate next,
            ILogger<ControlPlaneCorrelationIdMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = ResolveCorrelationId(context.Request.Headers[HeaderName]);
            context.Items[HeaderName] = correlationId;
            context.Response.OnStarting(
                () =>
                {
                    context.Response.Headers[HeaderName] = correlationId;
                    return Task.CompletedTask;
                });

            using (this.logger.BeginScope(new Dictionary<string, object>
                   {
                       ["CorrelationId"] = correlationId
                   }))
            {
                await this.next(context);
            }
        }

        private static string ResolveCorrelationId(StringValues values)
        {
            var value = values.FirstOrDefault();
            return !string.IsNullOrWhiteSpace(value) && value.Length <= 128
                ? value
                : Guid.NewGuid().ToString("N");
        }
    }
}
