namespace BlazorShop.Storefront.Presentation.Services;

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorShop.Storefront.Presentation.Models;
using BlazorShop.Storefront.Runtime;
using BlazorShop.Storefront.Presentation.Contracts;
using GeneratedClients = BlazorShop.Storefront.Client;

public sealed class GeneratedStorefrontCustomerClient : IStorefrontCustomerClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IHttpClientFactory httpClientFactory;
    private readonly IStorefrontRuntimeContext runtimeContext;

    public GeneratedStorefrontCustomerClient(
        IHttpClientFactory httpClientFactory,
        IStorefrontRuntimeContext runtimeContext)
    {
        this.httpClientFactory = httpClientFactory;
        this.runtimeContext = runtimeContext;
    }

    public Task<StorefrontSubmitResult<StorefrontCustomerProfileResponse>> GetCustomerProfileAsync(
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var client = new GeneratedClients.StorefrontCustomerProfileClient(this.CreateAuthorizedHttpClient(bearerToken));
        return ExecuteAsync<GeneratedClients.StorefrontCustomerProfileResponseCommerceNodeApiResponse, GeneratedClients.StorefrontCustomerProfileResponse, StorefrontCustomerProfileResponse>(
            storeKey => client.GetAsync(storeKey, cancellationToken),
            envelope => envelope.Success,
            envelope => envelope.Data,
            envelope => envelope.Message,
            "Unable to load customer profile right now.");
    }

    public Task<StorefrontSubmitResult<StorefrontCustomerProfileResponse>> UpdateCustomerProfileAsync(
        string bearerToken,
        StorefrontCustomerProfileUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = new GeneratedClients.StorefrontCustomerProfileClient(this.CreateAuthorizedHttpClient(bearerToken));
        return ExecuteAsync<GeneratedClients.StorefrontCustomerProfileResponseCommerceNodeApiResponse, GeneratedClients.StorefrontCustomerProfileResponse, StorefrontCustomerProfileResponse>(
            storeKey => client.UpdateAsync(storeKey, Project<GeneratedClients.StorefrontCustomerProfileUpdateRequest>(request), cancellationToken),
            envelope => envelope.Success,
            envelope => envelope.Data,
            envelope => envelope.Message,
            "Unable to update customer profile right now.");
    }

    public Task<StorefrontSubmitResult<IReadOnlyList<StorefrontCustomerAddressResponse>>> GetCustomerAddressesAsync(
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var client = new GeneratedClients.StorefrontCustomerAddressesClient(this.CreateAuthorizedHttpClient(bearerToken));
        return ExecuteAsync<GeneratedClients.StorefrontCustomerAddressResponseIReadOnlyListCommerceNodeApiResponse, ICollection<GeneratedClients.StorefrontCustomerAddressResponse>, IReadOnlyList<StorefrontCustomerAddressResponse>>(
            storeKey => client.ListAsync(storeKey, cancellationToken),
            envelope => envelope.Success,
            envelope => envelope.Data,
            envelope => envelope.Message,
            "Unable to load saved addresses right now.");
    }

    public Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> CreateCustomerAddressAsync(
        string bearerToken,
        StorefrontCustomerAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = new GeneratedClients.StorefrontCustomerAddressesClient(this.CreateAuthorizedHttpClient(bearerToken));
        return ExecuteAsync<GeneratedClients.StorefrontCustomerAddressResponseCommerceNodeApiResponse, GeneratedClients.StorefrontCustomerAddressResponse, StorefrontCustomerAddressResponse>(
            storeKey => client.CreateAsync(storeKey, Project<GeneratedClients.StorefrontCustomerAddressRequest>(request), cancellationToken),
            envelope => envelope.Success,
            envelope => envelope.Data,
            envelope => envelope.Message,
            "Unable to save this address right now.");
    }

    public Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> UpdateCustomerAddressAsync(
        string bearerToken,
        Guid addressId,
        StorefrontCustomerAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        if (addressId == Guid.Empty)
        {
            return Task.FromResult(StorefrontSubmitResult<StorefrontCustomerAddressResponse>.Failed("Address is required.", 400));
        }

        var client = new GeneratedClients.StorefrontCustomerAddressesClient(this.CreateAuthorizedHttpClient(bearerToken));
        return ExecuteAsync<GeneratedClients.StorefrontCustomerAddressResponseCommerceNodeApiResponse, GeneratedClients.StorefrontCustomerAddressResponse, StorefrontCustomerAddressResponse>(
            storeKey => client.UpdateAsync(addressId, storeKey, Project<GeneratedClients.StorefrontCustomerAddressRequest>(request), cancellationToken),
            envelope => envelope.Success,
            envelope => envelope.Data,
            envelope => envelope.Message,
            "Unable to update this address right now.");
    }

    public Task<StorefrontSubmitResult<object>> DeleteCustomerAddressAsync(
        string bearerToken,
        Guid addressId,
        CancellationToken cancellationToken = default)
    {
        if (addressId == Guid.Empty)
        {
            return Task.FromResult(StorefrontSubmitResult<object>.Failed("Address is required.", 400));
        }

        var client = new GeneratedClients.StorefrontCustomerAddressesClient(this.CreateAuthorizedHttpClient(bearerToken));
        return ExecuteCommandAsync(
            storeKey => client.DeleteAsync(addressId, storeKey, cancellationToken),
            "Unable to delete this address right now.");
    }

    public Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> SetDefaultShippingAddressAsync(
        string bearerToken,
        Guid addressId,
        CancellationToken cancellationToken = default)
    {
        var client = new GeneratedClients.StorefrontCustomerAddressesClient(this.CreateAuthorizedHttpClient(bearerToken));
        return ExecuteAsync<GeneratedClients.StorefrontCustomerAddressResponseCommerceNodeApiResponse, GeneratedClients.StorefrontCustomerAddressResponse, StorefrontCustomerAddressResponse>(
            storeKey => client.SetDefaultShippingAsync(addressId, storeKey, cancellationToken),
            envelope => envelope.Success,
            envelope => envelope.Data,
            envelope => envelope.Message,
            "Unable to update this address right now.");
    }

    public Task<StorefrontSubmitResult<StorefrontCustomerAddressResponse>> SetDefaultBillingAddressAsync(
        string bearerToken,
        Guid addressId,
        CancellationToken cancellationToken = default)
    {
        var client = new GeneratedClients.StorefrontCustomerAddressesClient(this.CreateAuthorizedHttpClient(bearerToken));
        return ExecuteAsync<GeneratedClients.StorefrontCustomerAddressResponseCommerceNodeApiResponse, GeneratedClients.StorefrontCustomerAddressResponse, StorefrontCustomerAddressResponse>(
            storeKey => client.SetDefaultBillingAsync(addressId, storeKey, cancellationToken),
            envelope => envelope.Success,
            envelope => envelope.Data,
            envelope => envelope.Message,
            "Unable to update this address right now.");
    }

    public Task<StorefrontSubmitResult<PagedResult<StorefrontCustomerOrderListItemResponse>>> GetCustomerOrdersAsync(
        string bearerToken,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var client = new GeneratedClients.StorefrontOrdersClient(this.CreateAuthorizedHttpClient(bearerToken));
        return ExecuteAsync<GeneratedClients.StorefrontCustomerOrderListItemResponseStorefrontPagedResponseCommerceNodeApiResponse, GeneratedClients.StorefrontCustomerOrderListItemResponseStorefrontPagedResponse, PagedResult<StorefrontCustomerOrderListItemResponse>>(
            storeKey => client.ListCurrentUserOrdersAsync(Math.Max(1, pageNumber), Math.Clamp(pageSize <= 0 ? 10 : pageSize, 1, 100), storeKey, cancellationToken),
            envelope => envelope.Success,
            envelope => envelope.Data,
            envelope => envelope.Message,
            "Unable to load orders right now.");
    }

    public Task<StorefrontSubmitResult<StorefrontCustomerOrderDetailResponse>> GetCustomerOrderAsync(
        string bearerToken,
        string orderReference,
        CancellationToken cancellationToken = default)
    {
        var reference = NormalizeOrderReference(orderReference);
        if (reference is null)
        {
            return Task.FromResult(StorefrontSubmitResult<StorefrontCustomerOrderDetailResponse>.Failed("Order reference is required.", 400));
        }

        var client = new GeneratedClients.StorefrontOrdersClient(this.CreateAuthorizedHttpClient(bearerToken));
        return ExecuteAsync<GeneratedClients.StorefrontCustomerOrderDetailResponseCommerceNodeApiResponse, GeneratedClients.StorefrontCustomerOrderDetailResponse, StorefrontCustomerOrderDetailResponse>(
            storeKey => client.GetCurrentUserOrderAsync(reference, storeKey, cancellationToken),
            envelope => envelope.Success,
            envelope => envelope.Data,
            envelope => envelope.Message,
            "Unable to load this order right now.");
    }

    public Task<StorefrontSubmitResult<StorefrontCustomerOrderDetailResponse>> GetCustomerOrderReceiptAsync(
        string bearerToken,
        string orderReference,
        CancellationToken cancellationToken = default)
    {
        var reference = NormalizeOrderReference(orderReference);
        if (reference is null)
        {
            return Task.FromResult(StorefrontSubmitResult<StorefrontCustomerOrderDetailResponse>.Failed("Order reference is required.", 400));
        }

        var client = new GeneratedClients.StorefrontOrdersClient(this.CreateAuthorizedHttpClient(bearerToken));
        return ExecuteAsync<GeneratedClients.StorefrontCustomerOrderDetailResponseCommerceNodeApiResponse, GeneratedClients.StorefrontCustomerOrderDetailResponse, StorefrontCustomerOrderDetailResponse>(
            storeKey => client.GetCurrentUserOrderReceiptAsync(reference, storeKey, cancellationToken),
            envelope => envelope.Success,
            envelope => envelope.Data,
            envelope => envelope.Message,
            "Unable to load this receipt right now.");
    }

    private HttpClient CreateAuthorizedHttpClient(string bearerToken)
    {
        var httpClient = this.httpClientFactory.CreateClient(StorefrontRuntimeServiceCollectionExtensions.GeneratedClientHttpClientName);
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken.Trim());
        }

        return httpClient;
    }

    private async Task<StorefrontSubmitResult<TLocal>> ExecuteAsync<TEnvelope, TGenerated, TLocal>(
        Func<string, Task<TEnvelope>> execute,
        Func<TEnvelope, bool?> successSelector,
        Func<TEnvelope, TGenerated?> dataSelector,
        Func<TEnvelope, string?> messageSelector,
        string fallbackMessage)
    {
        try
        {
            var envelope = await execute(this.runtimeContext.RequireStoreKey()).ConfigureAwait(false);
            if (successSelector(envelope) == true && dataSelector(envelope) is { } data)
            {
                return StorefrontSubmitResult<TLocal>.Succeeded(Project<TLocal>(data), messageSelector(envelope));
            }

            return StorefrontSubmitResult<TLocal>.Failed(messageSelector(envelope) ?? fallbackMessage);
        }
        catch (Exception exception)
        {
            var error = StorefrontRuntimeErrorMapper.FromException(exception);
            return StorefrontSubmitResult<TLocal>.Failed(error.Message, error.Status);
        }
    }

    private async Task<StorefrontSubmitResult<object>> ExecuteCommandAsync(
        Func<string, Task<GeneratedClients.CommerceNodeApiResponse>> execute,
        string fallbackMessage)
    {
        try
        {
            var envelope = await execute(this.runtimeContext.RequireStoreKey()).ConfigureAwait(false);
            return envelope.Success == true
                ? StorefrontSubmitResult<object>.Succeeded(null, envelope.Message)
                : StorefrontSubmitResult<object>.Failed(envelope.Message ?? fallbackMessage);
        }
        catch (Exception exception)
        {
            var error = StorefrontRuntimeErrorMapper.FromException(exception);
            return StorefrontSubmitResult<object>.Failed(error.Message, error.Status);
        }
    }

    private static TTarget Project<TTarget>(object source)
    {
        return JsonSerializer.Deserialize<TTarget>(JsonSerializer.Serialize(source, JsonOptions), JsonOptions)
            ?? throw new InvalidOperationException($"Could not project generated Storefront DTO to {typeof(TTarget).Name}.");
    }

    private static string? NormalizeOrderReference(string? orderReference)
    {
        return string.IsNullOrWhiteSpace(orderReference) ? null : orderReference.Trim();
    }
}
