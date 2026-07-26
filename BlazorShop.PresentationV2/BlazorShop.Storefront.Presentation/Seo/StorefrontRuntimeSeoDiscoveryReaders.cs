namespace BlazorShop.Storefront.Services;

using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorShop.Storefront.Models;
using BlazorShop.Storefront.Runtime;
using BlazorShop.Storefront.Services.Contracts;

public sealed class StorefrontRuntimeSitemapReader : IStorefrontSitemapReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IStorefrontRuntimeCatalogFacade catalogFacade;

    public StorefrontRuntimeSitemapReader(IStorefrontRuntimeCatalogFacade catalogFacade)
    {
        this.catalogFacade = catalogFacade;
    }

    public async Task<StorefrontApiResult<GetPublicCatalogSitemap>> GetPublishedSitemapAsync(CancellationToken cancellationToken = default)
    {
        var result = await this.catalogFacade.GetPublishedSitemapAsync(cancellationToken).ConfigureAwait(false);
        if (result.Success && result.Value is not null)
        {
            return StorefrontApiResult<GetPublicCatalogSitemap>.Success(Project<GetPublicCatalogSitemap>(result.Value));
        }

        return MapFailure<GetPublicCatalogSitemap>(result.Error);
    }

    private static StorefrontApiResult<T> MapFailure<T>(StorefrontRuntimeError? error)
    {
        return error?.Status == StorefrontRuntimeStatusCodes.ServiceUnavailable
            ? StorefrontApiResult<T>.ServiceUnavailable()
            : StorefrontApiResult<T>.NotFound();
    }

    private static TTarget Project<TTarget>(object source)
    {
        return JsonSerializer.Deserialize<TTarget>(JsonSerializer.Serialize(source, JsonOptions), JsonOptions)
            ?? throw new InvalidOperationException($"Could not project generated Storefront DTO to {typeof(TTarget).Name}.");
    }
}

public sealed class StorefrontRuntimeSeoSettingsReader : IStorefrontSeoSettingsReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IStorefrontRuntimeSeoFacade seoFacade;

    public StorefrontRuntimeSeoSettingsReader(IStorefrontRuntimeSeoFacade seoFacade)
    {
        this.seoFacade = seoFacade;
    }

    public async Task<StorefrontApiResult<GetSeoSettings>> GetSeoSettingsAsync(CancellationToken cancellationToken = default)
    {
        var result = await this.seoFacade.GetSeoSettingsAsync(cancellationToken).ConfigureAwait(false);
        if (result.Success && result.Value is not null)
        {
            return StorefrontApiResult<GetSeoSettings>.Success(Project<GetSeoSettings>(result.Value));
        }

        return result.Error?.Status == StorefrontRuntimeStatusCodes.ServiceUnavailable
            ? StorefrontApiResult<GetSeoSettings>.ServiceUnavailable()
            : StorefrontApiResult<GetSeoSettings>.NotFound();
    }

    private static TTarget Project<TTarget>(object source)
    {
        return JsonSerializer.Deserialize<TTarget>(JsonSerializer.Serialize(source, JsonOptions), JsonOptions)
            ?? throw new InvalidOperationException($"Could not project generated Storefront DTO to {typeof(TTarget).Name}.");
    }
}
