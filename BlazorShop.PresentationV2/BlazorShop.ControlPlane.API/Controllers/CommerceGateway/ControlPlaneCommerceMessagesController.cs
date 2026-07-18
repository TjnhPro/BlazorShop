namespace BlazorShop.ControlPlane.API.Controllers
{
    using System.Globalization;
    using System.Text;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.ControlPlane.API.Responses;
    using BlazorShop.Domain.Contracts;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    [ApiController]
    [Route("api/control-plane/stores/{storePublicId:guid}/catalog")]
    [Authorize(Policy = ControlPlanePolicyNames.StoresRead)]
    public sealed class ControlPlaneCommerceMessagesController : ControlPlaneCommerceGatewayControllerBase
    {
        private readonly BlazorShop.Application.ControlPlane.CommerceGateway.Messages.IControlPlaneMessageGateway gateway;

        public ControlPlaneCommerceMessagesController(BlazorShop.Application.ControlPlane.CommerceGateway.Messages.IControlPlaneMessageGateway gateway)
        {
            this.gateway = gateway;
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/email-settings")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsRead)]
        public async Task<IActionResult> GetEmailSettings(Guid storePublicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.GetEmailSettingsAsync(storePublicId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/email-settings")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsWrite)]
        public async Task<IActionResult> UpdateEmailSettings(
            Guid storePublicId,
            [FromBody] UpdateStoreEmailSettingsRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateEmailSettingsAsync(storePublicId, request, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/email-settings/password/rotate")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsWrite)]
        public async Task<IActionResult> RotateEmailPassword(
            Guid storePublicId,
            [FromBody] RotateStoreEmailPasswordRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.RotateEmailPasswordAsync(storePublicId, request, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/email-settings/password/clear")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsWrite)]
        public async Task<IActionResult> ClearEmailPassword(Guid storePublicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ClearEmailPasswordAsync(storePublicId, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/email-settings/test-send")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsWrite)]
        public async Task<IActionResult> SendEmailTest(
            Guid storePublicId,
            [FromBody] SendStoreEmailTestRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.SendEmailTestAsync(storePublicId, request, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/message-templates")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsRead)]
        public async Task<IActionResult> ListMessageTemplates(Guid storePublicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ListMessageTemplatesAsync(storePublicId, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/message-templates/{templatePublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsRead)]
        public async Task<IActionResult> GetMessageTemplate(Guid storePublicId, Guid templatePublicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.GetMessageTemplateAsync(storePublicId, templatePublicId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/message-templates/{templatePublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsWrite)]
        public async Task<IActionResult> UpdateMessageTemplate(
            Guid storePublicId,
            Guid templatePublicId,
            [FromBody] UpdateMessageTemplateRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateMessageTemplateAsync(storePublicId, templatePublicId, request, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/message-templates/{templatePublicId:guid}/reset")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsWrite)]
        public async Task<IActionResult> ResetMessageTemplate(Guid storePublicId, Guid templatePublicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ResetMessageTemplateAsync(storePublicId, templatePublicId, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/message-templates/preview")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsRead)]
        public async Task<IActionResult> PreviewMessageTemplate(
            Guid storePublicId,
            [FromBody] PreviewMessageTemplateRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.PreviewMessageTemplateAsync(storePublicId, request, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/queued-messages")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsRead)]
        public async Task<IActionResult> ListQueuedMessages(
            Guid storePublicId,
            [FromQuery] string? status,
            [FromQuery] string? templateSystemName,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 25,
            CancellationToken cancellationToken = default)
        {
            return ToActionResult(await this.gateway.ListQueuedMessagesAsync(storePublicId, status, templateSystemName, skip, take, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/queued-messages/{queuedMessagePublicId:guid}")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsRead)]
        public async Task<IActionResult> GetQueuedMessage(Guid storePublicId, Guid queuedMessagePublicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.GetQueuedMessageAsync(storePublicId, queuedMessagePublicId, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/queued-messages/{queuedMessagePublicId:guid}/retry")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsWrite)]
        public async Task<IActionResult> RetryQueuedMessage(Guid storePublicId, Guid queuedMessagePublicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.RetryQueuedMessageAsync(storePublicId, queuedMessagePublicId, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/queued-messages/{queuedMessagePublicId:guid}/cancel")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsWrite)]
        public async Task<IActionResult> CancelQueuedMessage(Guid storePublicId, Guid queuedMessagePublicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.CancelQueuedMessageAsync(storePublicId, queuedMessagePublicId, cancellationToken));
        }
    }
}
