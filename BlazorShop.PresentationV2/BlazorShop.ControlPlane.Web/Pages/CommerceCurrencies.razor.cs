namespace BlazorShop.ControlPlane.Web.Pages
{
    using System.Globalization;
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.ControlPlane.Web.Services.Common;
    using BlazorShop.ControlPlane.Web.Services.Nodes;
    using BlazorShop.ControlPlane.Web.Services.Stores;
    using BlazorShop.ControlPlane.Web.Services.Users;
    using BlazorShop.Domain.Contracts;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    public partial class CommerceCurrencies
    {
        private static readonly string[] RoundingModes =
        [
            "halfAwayFromZero",
            "halfToEven",
            "floor",
            "ceiling",
            "truncate",
        ];

        private readonly List<StoreSummary> stores = [];
        private readonly List<CurrencyForm> currencyForms = [];
        private readonly List<StoreCurrencyExchangeRateDto> exchangeRates = [];
        private readonly List<StoreCurrencyExchangeRateProviderDto> providers = [];

        private Guid? selectedStorePublicId;
        private bool isLoading;
        private bool isSavingCurrency;
        private bool isSavingRate;
        private bool isProviderActionRunning;
        private string? errorMessage;
        private string? successMessage;
        private string? manualTargetCurrencyCode;
        private decimal manualRate = 1m;
        private string? manualSource = "manual";
        private bool manualRateEnabled = true;
        private string? selectedProviderKey;
        private string? selectedProviderTargetCurrencyCode;
        private CommerceTaskSummary? lastQueuedTask;

        private bool HasStore => selectedStorePublicId.HasValue && selectedStorePublicId.Value != Guid.Empty;

        private string BaseCurrencyLabel => currencyForms.FirstOrDefault(currency => currency.IsBaseCurrency)?.CurrencyCode ?? "-";

        private IReadOnlyList<string> TargetCurrencyCodes => currencyForms
            .Where(currency => currency.IsEnabled && !currency.IsBaseCurrency)
            .OrderBy(currency => currency.DisplayOrder)
            .ThenBy(currency => currency.CurrencyCode, StringComparer.Ordinal)
            .Select(currency => currency.CurrencyCode)
            .ToArray();

        protected override async Task OnInitializedAsync()
        {
            var response = await StoreClient.ListAsync(pageSize: 100);
            stores.AddRange(response.Items);
            selectedStorePublicId = stores.FirstOrDefault()?.PublicId;
            await LoadCurrencyStateAsync();
        }

        private async Task LoadCurrencyStateAsync()
        {
            if (!HasStore)
            {
                return;
            }

            isLoading = true;
            errorMessage = null;
            successMessage = null;
            lastQueuedTask = null;
            try
            {
                var currenciesResult = await CurrencyClient.ListCurrenciesAsync(selectedStorePublicId!.Value);
                if (!currenciesResult.Success || currenciesResult.Data is null)
                {
                    errorMessage = currenciesResult.Message;
                    return;
                }

                currencyForms.Clear();
                currencyForms.AddRange(currenciesResult.Data
                    .OrderBy(currency => currency.DisplayOrder)
                    .ThenBy(currency => currency.CurrencyCode, StringComparer.Ordinal)
                    .Select(CurrencyForm.FromDto));

                var ratesResult = await CurrencyClient.ListExchangeRatesAsync(selectedStorePublicId.Value);
                if (!ratesResult.Success || ratesResult.Data is null)
                {
                    errorMessage = ratesResult.Message;
                    return;
                }

                exchangeRates.Clear();
                exchangeRates.AddRange(ratesResult.Data
                    .OrderBy(rate => rate.TargetCurrencyCode, StringComparer.Ordinal)
                    .ThenBy(rate => rate.ProviderKey, StringComparer.Ordinal));

                var providersResult = await CurrencyClient.ListExchangeRateProvidersAsync(selectedStorePublicId.Value);
                if (!providersResult.Success || providersResult.Data is null)
                {
                    errorMessage = providersResult.Message;
                    return;
                }

                providers.Clear();
                providers.AddRange(providersResult.Data.OrderBy(provider => provider.ProviderKey, StringComparer.Ordinal));
                selectedProviderKey = providers.FirstOrDefault(provider => !string.Equals(provider.ProviderKey, "manual", StringComparison.OrdinalIgnoreCase))?.ProviderKey;
                manualTargetCurrencyCode ??= TargetCurrencyCodes.FirstOrDefault();
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task SaveCurrencyAsync(CurrencyForm currency)
        {
            if (!HasStore)
            {
                return;
            }

            isSavingCurrency = true;
            errorMessage = null;
            successMessage = null;
            try
            {
                var result = await CurrencyClient.UpdateCurrencyAsync(
                    selectedStorePublicId!.Value,
                    currency.CurrencyCode,
                    currency.ToRequest());

                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                ReplaceCurrency(result.Data);
                successMessage = result.Message;
            }
            finally
            {
                isSavingCurrency = false;
            }
        }

        private async Task SaveManualRateAsync()
        {
            if (!HasStore || string.IsNullOrWhiteSpace(manualTargetCurrencyCode))
            {
                errorMessage = "Select a target currency.";
                return;
            }

            isSavingRate = true;
            errorMessage = null;
            successMessage = null;
            try
            {
                var result = await CurrencyClient.UpsertExchangeRateAsync(
                    selectedStorePublicId!.Value,
                    manualTargetCurrencyCode,
                    new UpsertStoreCurrencyExchangeRateRequest(manualRate, manualSource, IsEnabled: manualRateEnabled));

                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                ReplaceRate(result.Data);
                successMessage = result.Message;
            }
            finally
            {
                isSavingRate = false;
            }
        }

        private async Task DisableRateAsync(string targetCurrencyCode)
        {
            if (!HasStore)
            {
                return;
            }

            isSavingRate = true;
            errorMessage = null;
            successMessage = null;
            try
            {
                var result = await CurrencyClient.DisableExchangeRateAsync(selectedStorePublicId!.Value, targetCurrencyCode);
                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                ReplaceRate(result.Data);
                successMessage = result.Message;
            }
            finally
            {
                isSavingRate = false;
            }
        }

        private async Task FetchRatesAsync()
        {
            if (!TryBuildProviderRequest(out var request))
            {
                return;
            }

            isProviderActionRunning = true;
            errorMessage = null;
            successMessage = null;
            try
            {
                var result = await CurrencyClient.FetchExchangeRatesAsync(selectedStorePublicId!.Value, request);
                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                foreach (var rate in result.Data.Rates)
                {
                    ReplaceRate(rate);
                }

                successMessage = result.Message;
            }
            finally
            {
                isProviderActionRunning = false;
            }
        }

        private async Task QueueRateUpdateAsync()
        {
            if (!TryBuildProviderRequest(out var fetchRequest))
            {
                return;
            }

            isProviderActionRunning = true;
            errorMessage = null;
            successMessage = null;
            lastQueuedTask = null;
            try
            {
                var result = await CurrencyClient.QueueExchangeRateUpdateAsync(
                    selectedStorePublicId!.Value,
                    new QueueStoreCurrencyExchangeRateUpdateRequest(
                        fetchRequest.ProviderKey,
                        fetchRequest.TargetCurrencyCodes,
                        fetchRequest.IsEnabled));

                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                lastQueuedTask = result.Data;
                successMessage = result.Message;
            }
            finally
            {
                isProviderActionRunning = false;
            }
        }

        private bool TryBuildProviderRequest(out FetchStoreCurrencyExchangeRatesRequest request)
        {
            request = new FetchStoreCurrencyExchangeRatesRequest(string.Empty);
            if (!HasStore)
            {
                errorMessage = "Select a store.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(selectedProviderKey))
            {
                errorMessage = "Select an exchange-rate provider.";
                return false;
            }

            IReadOnlyList<string>? targets = string.IsNullOrWhiteSpace(selectedProviderTargetCurrencyCode)
                ? null
                : [selectedProviderTargetCurrencyCode];
            request = new FetchStoreCurrencyExchangeRatesRequest(selectedProviderKey, targets);
            return true;
        }

        private void ReplaceCurrency(StoreCurrencyDto dto)
        {
            var index = currencyForms.FindIndex(item => item.CurrencyCode == dto.CurrencyCode);
            var form = CurrencyForm.FromDto(dto);
            if (index >= 0)
            {
                currencyForms[index] = form;
            }
            else
            {
                currencyForms.Add(form);
            }
        }

        private void ReplaceRate(StoreCurrencyExchangeRateDto dto)
        {
            var index = exchangeRates.FindIndex(item =>
                item.BaseCurrencyCode == dto.BaseCurrencyCode &&
                item.TargetCurrencyCode == dto.TargetCurrencyCode &&
                item.ProviderKey == dto.ProviderKey);

            if (index >= 0)
            {
                exchangeRates[index] = dto;
            }
            else
            {
                exchangeRates.Add(dto);
            }

            exchangeRates.Sort((left, right) =>
            {
                var target = string.CompareOrdinal(left.TargetCurrencyCode, right.TargetCurrencyCode);
                return target != 0 ? target : string.CompareOrdinal(left.ProviderKey, right.ProviderKey);
            });
        }

        private sealed class CurrencyForm
        {
            public string CurrencyCode { get; init; } = string.Empty;

            public bool IsEnabled { get; set; }

            public bool IsBaseCurrency { get; init; }

            public bool IsDefaultDisplayCurrency { get; set; }

            public int DisplayOrder { get; set; }

            public string? CultureName { get; set; }

            public string? Symbol { get; set; }

            public int DecimalDigits { get; set; }

            public string UnitPriceRoundingMode { get; set; } = "halfAwayFromZero";

            public decimal UnitPriceRoundingIncrement { get; set; } = 0.01m;

            public string LineTotalRoundingMode { get; set; } = "halfAwayFromZero";

            public decimal LineTotalRoundingIncrement { get; set; } = 0.01m;

            public string OrderTotalRoundingMode { get; set; } = "halfAwayFromZero";

            public decimal OrderTotalRoundingIncrement { get; set; } = 0.01m;

            public UpdateStoreCurrencyRequest ToRequest()
            {
                return new UpdateStoreCurrencyRequest(
                    IsEnabled,
                    IsDefaultDisplayCurrency,
                    DisplayOrder,
                    CultureName,
                    Symbol,
                    DecimalDigits,
                    UnitPriceRoundingMode,
                    UnitPriceRoundingIncrement,
                    LineTotalRoundingMode,
                    LineTotalRoundingIncrement,
                    OrderTotalRoundingMode,
                    OrderTotalRoundingIncrement);
            }

            public static CurrencyForm FromDto(StoreCurrencyDto currency)
            {
                return new CurrencyForm
                {
                    CurrencyCode = currency.CurrencyCode,
                    IsEnabled = currency.IsEnabled,
                    IsBaseCurrency = currency.IsBaseCurrency,
                    IsDefaultDisplayCurrency = currency.IsDefaultDisplayCurrency,
                    DisplayOrder = currency.DisplayOrder,
                    CultureName = currency.CultureName,
                    Symbol = currency.Symbol,
                    DecimalDigits = currency.DecimalDigits,
                    UnitPriceRoundingMode = currency.UnitPriceRoundingMode,
                    UnitPriceRoundingIncrement = currency.UnitPriceRoundingIncrement,
                    LineTotalRoundingMode = currency.LineTotalRoundingMode,
                    LineTotalRoundingIncrement = currency.LineTotalRoundingIncrement,
                    OrderTotalRoundingMode = currency.OrderTotalRoundingMode,
                    OrderTotalRoundingIncrement = currency.OrderTotalRoundingIncrement,
                };
            }
        }
    }
}
