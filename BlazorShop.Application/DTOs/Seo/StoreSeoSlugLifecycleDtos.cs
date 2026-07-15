namespace BlazorShop.Application.DTOs.Seo
{
    public sealed class StoreSeoSlugGenerateRequest
    {
        public string? EntityType { get; set; }

        public string? SourceName { get; set; }

        public string? LanguageCode { get; set; }

        public Guid? ExcludedEntityId { get; set; }
    }

    public sealed class StoreSeoSlugValidateRequest
    {
        public string? EntityType { get; set; }

        public string? Slug { get; set; }

        public string? LanguageCode { get; set; }

        public Guid? ExcludedEntityId { get; set; }
    }

    public sealed class StoreSeoSlugHistoryQuery
    {
        public string? EntityType { get; set; }

        public Guid EntityId { get; set; }

        public string? LanguageCode { get; set; }
    }
}
