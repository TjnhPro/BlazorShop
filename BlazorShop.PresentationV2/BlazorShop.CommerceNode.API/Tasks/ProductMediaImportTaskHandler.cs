namespace BlazorShop.CommerceNode.API.Tasks
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.CommerceNode.API.ProductMedia;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class ProductMediaImportTaskHandler : ICommerceTaskHandler
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly CommerceNodeDbContext context;
        private readonly IProductMediaDownloader downloader;
        private readonly IProductMediaUrlBuilder urlBuilder;

        public ProductMediaImportTaskHandler(
            CommerceNodeDbContext context,
            IProductMediaDownloader downloader,
            IProductMediaUrlBuilder urlBuilder)
        {
            this.context = context;
            this.downloader = downloader;
            this.urlBuilder = urlBuilder;
        }

        public string TaskType => ProductMediaTaskTypes.Import;

        public async Task<CommerceTaskHandlerResult> ExecuteAsync(
            CommerceTaskHandlerContext context,
            CancellationToken cancellationToken)
        {
            ProductMediaImportTaskPayload payload;
            try
            {
                payload = JsonSerializer.Deserialize<ProductMediaImportTaskPayload>(
                    context.PayloadJson,
                    SerializerOptions) ?? new ProductMediaImportTaskPayload("v1", Guid.Empty, Guid.Empty, []);
            }
            catch (JsonException)
            {
                return CommerceTaskHandlerResult.Failed("Task payload is not valid JSON.", "invalid_payload_json");
            }

            var validationError = ValidatePayload(payload);
            if (validationError is not null)
            {
                return CommerceTaskHandlerResult.Failed(validationError, "invalid_product_media_payload");
            }

            var product = await this.context.Products.FirstOrDefaultAsync(
                entity => entity.Id == payload.ProductId &&
                          entity.StoreId == payload.StoreId &&
                          entity.ArchivedAt == null,
                cancellationToken);
            if (product is null)
            {
                return CommerceTaskHandlerResult.Failed(
                    "Product was not found for product media import.",
                    "product_not_found");
            }

            var mediaPublicIds = payload.Items.Select(item => item.MediaPublicId).ToList();
            var mediaRows = await this.context.ProductMedia
                .Where(media =>
                    media.StoreId == payload.StoreId &&
                    media.ProductId == payload.ProductId &&
                    mediaPublicIds.Contains(media.PublicId) &&
                    media.DeletedAt == null)
                .ToListAsync(cancellationToken);
            var rowsByPublicId = mediaRows.ToDictionary(media => media.PublicId);
            var results = new List<ProductMediaImportItemResult>();

            foreach (var item in payload.Items)
            {
                if (await context.IsCancellationRequestedAsync(cancellationToken))
                {
                    return CommerceTaskHandlerResult.Failed(
                        "Product media import was cancelled.",
                        "task_cancelled",
                        resultJson: JsonSerializer.Serialize(new ProductMediaImportTaskResult(results), SerializerOptions));
                }

                if (!rowsByPublicId.TryGetValue(item.MediaPublicId, out var media))
                {
                    results.Add(new ProductMediaImportItemResult(item.MediaPublicId, false, "Media row was not found."));
                    continue;
                }

                var itemResult = await this.ProcessItemAsync(payload, item, media, cancellationToken);
                results.Add(itemResult);
            }

            await this.ApplyPrimaryMediaAsync(product, payload, rowsByPublicId, cancellationToken);

            var resultJson = JsonSerializer.Serialize(new ProductMediaImportTaskResult(results), SerializerOptions);
            return CommerceTaskHandlerResult.Succeeded("Product media import completed.", resultJson);
        }

        private async Task<ProductMediaImportItemResult> ProcessItemAsync(
            ProductMediaImportTaskPayload payload,
            ProductMediaImportTaskItem item,
            Domain.Entities.CommerceNode.ProductMedia media,
            CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            media.Status = ProductMediaStatuses.Downloading;
            media.ErrorMessage = null;
            media.UpdatedAt = now;
            await this.context.SaveChangesAsync(cancellationToken);

            var download = await this.downloader.DownloadOriginalAsync(
                payload.StoreId,
                payload.ProductId,
                item.MediaPublicId,
                item.SourceUrl,
                cancellationToken);

            now = DateTimeOffset.UtcNow;
            if (!download.Success)
            {
                media.Status = ProductMediaStatuses.Failed;
                media.ErrorMessage = download.Message;
                media.UpdatedAt = now;
                await this.context.SaveChangesAsync(cancellationToken);
                return new ProductMediaImportItemResult(item.MediaPublicId, false, download.Message);
            }

            media.OriginalStoragePath = download.StoragePath;
            media.ContentHash = download.ContentHash;
            media.FileName = download.FileName;
            media.MimeType = download.MimeType;
            media.Width = download.Width;
            media.Height = download.Height;
            media.FileSizeBytes = download.FileSizeBytes;
            media.SortOrder = item.SortOrder;
            media.AltText = item.AltText;
            media.Status = ProductMediaStatuses.Stored;
            media.ErrorMessage = null;
            media.ProcessedAt = now;
            media.UpdatedAt = now;
            await this.context.SaveChangesAsync(cancellationToken);

            return new ProductMediaImportItemResult(item.MediaPublicId, true, "Stored.");
        }

        private async Task ApplyPrimaryMediaAsync(
            Domain.Entities.Product product,
            ProductMediaImportTaskPayload payload,
            IReadOnlyDictionary<Guid, Domain.Entities.CommerceNode.ProductMedia> rowsByPublicId,
            CancellationToken cancellationToken)
        {
            var requestedPrimary = payload.Items
                .Where(item => item.IsPrimary)
                .Select(item => rowsByPublicId.TryGetValue(item.MediaPublicId, out var media) ? media : null)
                .FirstOrDefault(media => media?.Status == ProductMediaStatuses.Stored);

            var currentPrimaryExists = await this.context.ProductMedia.AnyAsync(
                media =>
                    media.StoreId == payload.StoreId &&
                    media.ProductId == payload.ProductId &&
                    media.Status == ProductMediaStatuses.Stored &&
                    media.DeletedAt == null &&
                    media.IsPrimary,
                cancellationToken);

            var primary = requestedPrimary;
            if (primary is null && !currentPrimaryExists)
            {
                primary = rowsByPublicId.Values
                    .Where(media => media.Status == ProductMediaStatuses.Stored && media.DeletedAt == null)
                    .OrderBy(media => media.SortOrder)
                    .ThenBy(media => media.CreatedAt)
                    .FirstOrDefault();
            }

            if (primary is null)
            {
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var existingPrimaryRows = await this.context.ProductMedia
                .Where(media =>
                    media.StoreId == payload.StoreId &&
                    media.ProductId == payload.ProductId &&
                    media.DeletedAt == null &&
                    media.IsPrimary)
                .ToListAsync(cancellationToken);

            foreach (var existingPrimary in existingPrimaryRows)
            {
                existingPrimary.IsPrimary = false;
                existingPrimary.UpdatedAt = now;
            }

            primary.IsPrimary = true;
            primary.UpdatedAt = now;
            product.Image = this.urlBuilder.BuildProductMediaUrl(primary.PublicId, primary.Version);
            product.UpdatedAt = now.UtcDateTime;
            await this.context.SaveChangesAsync(cancellationToken);
        }

        private static string? ValidatePayload(ProductMediaImportTaskPayload payload)
        {
            if (payload.StoreId == Guid.Empty)
            {
                return "Store id is required.";
            }

            if (payload.ProductId == Guid.Empty)
            {
                return "Product id is required.";
            }

            if (payload.Items.Count == 0)
            {
                return "At least one media item is required.";
            }

            if (payload.Items.Any(item => item.MediaPublicId == Guid.Empty || string.IsNullOrWhiteSpace(item.SourceUrl)))
            {
                return "Media item payload is invalid.";
            }

            return null;
        }

        private sealed record ProductMediaImportTaskResult(IReadOnlyList<ProductMediaImportItemResult> Items);

        private sealed record ProductMediaImportItemResult(Guid MediaPublicId, bool Success, string Message);
    }
}
