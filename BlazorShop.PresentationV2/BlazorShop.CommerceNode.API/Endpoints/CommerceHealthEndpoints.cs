namespace BlazorShop.CommerceNode.API.Endpoints
{
    using System.Reflection;

    using BlazorShop.CommerceNode.API.Configuration;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.Extensions.Options;

    public static class CommerceHealthEndpoints
    {
        public static IEndpointRouteBuilder MapCommerceHealthEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/commerce")
                .WithTags("Commerce Health");

            group.MapGet(
                    "/healthz",
                    (IOptions<CommerceNodeOptions> options, IWebHostEnvironment environment) =>
                    {
                        var data = new CommerceHealthResponse(
                            options.Value.NodeKey,
                            "healthy",
                            DateTimeOffset.UtcNow,
                            ResolveVersion(),
                            environment.EnvironmentName);

                        return CommerceNodeApiResponseWriter.Success(
                            StatusCodes.Status200OK,
                            data,
                            "Commerce Node is healthy.");
                    })
                .WithName("CommerceNodeHealthz")
                .WithSummary("Checks whether the Commerce Node API is reachable and credential validation succeeded.")
                .Produces<CommerceNodeApiResponse<CommerceHealthResponse>>(StatusCodes.Status200OK)
                .Produces<CommerceNodeApiResponse<object>>(StatusCodes.Status401Unauthorized)
                .Produces<CommerceNodeApiResponse<object>>(StatusCodes.Status403Forbidden);

            return endpoints;
        }

        private static string ResolveVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                   ?? "unknown";
        }

        private sealed record CommerceHealthResponse(
            string NodeKey,
            string Status,
            DateTimeOffset CheckedAt,
            string Version,
            string Environment);
    }
}
