namespace BlazorShop.Domain.Contracts
{
    public sealed record ProductFilterMetadataReadModel(
        decimal? MinPrice,
        decimal? MaxPrice);
}
