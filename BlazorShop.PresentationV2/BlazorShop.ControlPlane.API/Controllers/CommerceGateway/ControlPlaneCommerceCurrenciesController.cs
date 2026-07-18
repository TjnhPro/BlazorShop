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
    public sealed class ControlPlaneCommerceCurrenciesController : ControlPlaneCommerceGatewayControllerBase
    {
        private readonly BlazorShop.Application.ControlPlane.CommerceGateway.Currencies.IControlPlaneCurrencyGateway gateway;

        public ControlPlaneCommerceCurrenciesController(BlazorShop.Application.ControlPlane.CommerceGateway.Currencies.IControlPlaneCurrencyGateway gateway)
        {
            this.gateway = gateway;
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/currencies")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsRead)]
        public async Task<IActionResult> ListCurrencies(Guid storePublicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ListCurrenciesAsync(storePublicId, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/currencies/{currencyCode}")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsWrite)]
        public async Task<IActionResult> UpdateCurrency(
            Guid storePublicId,
            string currencyCode,
            UpdateStoreCurrencyRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpdateCurrencyAsync(storePublicId, currencyCode, request, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/currencies/exchange-rates")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsRead)]
        public async Task<IActionResult> ListExchangeRates(Guid storePublicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ListExchangeRatesAsync(storePublicId, cancellationToken));
        }

        [HttpGet("~/api/controlplane/commerce/stores/{storePublicId:guid}/currencies/exchange-rate-providers")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceProvidersRead)]
        public async Task<IActionResult> ListExchangeRateProviders(Guid storePublicId, CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.ListExchangeRateProvidersAsync(storePublicId, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/currencies/exchange-rates/fetch")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceProvidersWrite)]
        public async Task<IActionResult> FetchExchangeRates(
            Guid storePublicId,
            FetchStoreCurrencyExchangeRatesRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.FetchExchangeRatesAsync(storePublicId, request, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/currencies/exchange-rates/update-tasks")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceProvidersWrite)]
        public async Task<IActionResult> QueueExchangeRateUpdate(
            Guid storePublicId,
            QueueStoreCurrencyExchangeRateUpdateRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.QueueExchangeRateUpdateAsync(storePublicId, request, cancellationToken));
        }

        [HttpPut("~/api/controlplane/commerce/stores/{storePublicId:guid}/currencies/exchange-rates/{targetCurrencyCode}")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsWrite)]
        public async Task<IActionResult> UpsertExchangeRate(
            Guid storePublicId,
            string targetCurrencyCode,
            UpsertStoreCurrencyExchangeRateRequest request,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.UpsertExchangeRateAsync(storePublicId, targetCurrencyCode, request, cancellationToken));
        }

        [HttpPost("~/api/controlplane/commerce/stores/{storePublicId:guid}/currencies/exchange-rates/{targetCurrencyCode}/disable")]
        [Authorize(Policy = ControlPlanePolicyNames.CommerceSettingsWrite)]
        public async Task<IActionResult> DisableExchangeRate(
            Guid storePublicId,
            string targetCurrencyCode,
            CancellationToken cancellationToken)
        {
            return ToActionResult(await this.gateway.DisableExchangeRateAsync(storePublicId, targetCurrencyCode, cancellationToken));
        }
    }
}
