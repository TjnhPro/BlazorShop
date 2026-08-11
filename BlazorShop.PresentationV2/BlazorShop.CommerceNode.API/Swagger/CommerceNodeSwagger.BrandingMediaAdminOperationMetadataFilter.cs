namespace BlazorShop.CommerceNode.API.Swagger
{
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.OpenApi;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public static partial class CommerceNodeSwaggerExtensions
    {
        private sealed class CommerceBrandingMediaAdminOperationMetadataFilter : IOperationFilter
        {
            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor action
                    || !string.Equals(action.ControllerName, "CommerceMediaAssets", StringComparison.Ordinal)
                    || !string.Equals(action.ActionName, "UploadBranding", StringComparison.Ordinal))
                {
                    return;
                }

                operation.OperationId = "CommerceMediaAssets_UploadBranding";
                operation.Summary = "Upload a normalized branding logo or favicon.";
                operation.Responses ??= [];
                operation.Responses["200"] = CommerceNodeSwaggerResponseHelpers.CreateJsonResponse(
                    context,
                    typeof(CommerceNodeApiResponse<CommerceBrandingAssetResponse>),
                    "Success.");
                foreach (var statusCode in new[] { StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError })
                {
                    operation.Responses[statusCode.ToString()] = CommerceNodeSwaggerResponseHelpers.CreateJsonResponse(
                        context,
                        typeof(CommerceNodeApiResponse<CommerceBrandingAssetResponse>),
                        "Error.");
                }
            }
        }
    }
}
