namespace BlazorShop.CommerceNode.API.Swagger
{
    using Microsoft.OpenApi;
    using Swashbuckle.AspNetCore.SwaggerGen;

    internal static class CommerceNodeSwaggerResponseHelpers
    {
        public static OpenApiResponse CreateJsonResponse(
            OperationFilterContext context,
            Type responseType,
            string description)
        {
            return new OpenApiResponse
            {
                Description = description,
                Content = CreateJsonContent(context, responseType),
            };
        }

        public static Dictionary<string, OpenApiMediaType> CreateJsonContent(
            OperationFilterContext context,
            Type responseType)
        {
            return new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new()
                {
                    Schema = context.SchemaGenerator.GenerateSchema(responseType, context.SchemaRepository),
                },
            };
        }
    }
}
