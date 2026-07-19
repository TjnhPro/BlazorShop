namespace BlazorShop.CommerceNode.API.Swagger
{
    using System.Reflection;
    using System.Text.Json.Nodes;

    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Discovery;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.CommerceNode.API.Contracts.Storefront;
    using BlazorShop.CommerceNode.API.Middleware;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.OpenApi;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public static partial class CommerceNodeSwaggerExtensions
    {
        private sealed class CommerceTransactionalMessageAdminOperationMetadataFilter : IOperationFilter
        {
            private static readonly IReadOnlyDictionary<(string Controller, string Action), CommerceTransactionalMessageOperationMetadata> Metadata =
                new Dictionary<(string Controller, string Action), CommerceTransactionalMessageOperationMetadata>
                {
                    [("CommerceStoreEmailSettings", "Get")] = new(
                        "CommerceStoreEmailSettings_Get",
                        "Get store email SMTP settings.",
                        typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("CommerceStoreEmailSettings", "Update")] = new(
                        "CommerceStoreEmailSettings_Update",
                        "Update store email SMTP settings.",
                        typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("CommerceStoreEmailSettings", "RotatePassword")] = new(
                        "CommerceStoreEmailSettings_RotatePassword",
                        "Rotate the store SMTP password.",
                        typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("CommerceStoreEmailSettings", "ClearPassword")] = new(
                        "CommerceStoreEmailSettings_ClearPassword",
                        "Clear the store SMTP password.",
                        typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("CommerceStoreEmailSettings", "SendTest")] = new(
                        "CommerceStoreEmailSettings_SendTest",
                        "Send a store SMTP test email.",
                        typeof(CommerceNodeApiResponse<SendStoreEmailTestResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("CommerceMessageTemplates", "List")] = new(
                        "CommerceMessageTemplates_List",
                        "List transactional message templates.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<MessageTemplateAdminSummary>>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError]),
                    [("CommerceMessageTemplates", "Get")] = new(
                        "CommerceMessageTemplates_Get",
                        "Get a transactional message template.",
                        typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("CommerceMessageTemplates", "Update")] = new(
                        "CommerceMessageTemplates_Update",
                        "Update a store transactional message template override.",
                        typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("CommerceMessageTemplates", "Reset")] = new(
                        "CommerceMessageTemplates_Reset",
                        "Reset a store transactional message template override.",
                        typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("CommerceMessageTemplates", "Preview")] = new(
                        "CommerceMessageTemplates_Preview",
                        "Preview a transactional message template.",
                        typeof(CommerceNodeApiResponse<MessageTemplatePreviewResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("CommerceQueuedMessages", "List")] = new(
                        "CommerceQueuedMessages_List",
                        "List queued transactional messages.",
                        typeof(CommerceNodeApiResponse<QueuedMessageAdminListResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError]),
                    [("CommerceQueuedMessages", "Get")] = new(
                        "CommerceQueuedMessages_Get",
                        "Get a queued transactional message.",
                        typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("CommerceQueuedMessages", "Retry")] = new(
                        "CommerceQueuedMessages_Retry",
                        "Retry a queued transactional message.",
                        typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("CommerceQueuedMessages", "Cancel")] = new(
                        "CommerceQueuedMessages_Cancel",
                        "Cancel a queued transactional message.",
                        typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                };

            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor actionDescriptor
                    || !Metadata.TryGetValue((actionDescriptor.ControllerName, actionDescriptor.ActionName), out var metadata))
                {
                    return;
                }

                operation.OperationId = metadata.OperationId;
                operation.Summary = metadata.Summary;

                if (operation.RequestBody is OpenApiRequestBody requestBody)
                {
                    requestBody.Required = true;
                }

                operation.Responses ??= new OpenApiResponses();
                operation.Responses["200"] = CommerceNodeSwaggerResponseHelpers.CreateJsonResponse(context, metadata.ResponseType, "Success.");
                foreach (var statusCode in metadata.ErrorStatusCodes)
                {
                    operation.Responses[statusCode.ToString()] = CommerceNodeSwaggerResponseHelpers.CreateJsonResponse(context, metadata.ResponseType, "Error.");
                }
            }

        private sealed record CommerceTransactionalMessageOperationMetadata(
                string OperationId,
                string Summary,
                Type ResponseType,
                int[] ErrorStatusCodes);
        }
    }
}
