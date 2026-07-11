namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class ProductMediaService : IProductMediaService
    {
        private const int MaxImportItems = 25;
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly CommerceNodeDbContext context;
        private readonly ICatalogQueryCache catalogQueryCache;
        private readonly ICommerceStoreContext storeContext;
        private readonly ICommerceTaskService taskService;
        private readonly IProductMediaUrlBuilder urlBuilder;

        public ProductMediaService(
            CommerceNodeDbContext context,
            ICatalogQueryCache catalogQueryCache,
            ICommerceStoreContext storeContext,
            ICommerceTaskService taskService,
            IProductMediaUrlBuilder urlBuilder)
        {
            this.context = context;
            this.catalogQueryCache = catalogQueryCache;
            this.storeContext = storeContext;
            this.taskService = taskService;
            this.urlBuilder = urlBuilder;
        }

        public async Task<ProductMediaOperationResult<ProductMediaListResponse>> ListAsync(
            Guid productId,
            ProductMediaListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var scope = await this.ResolveProductScopeAsync(productId, asTracking: false, cancellationToken);
            if (!scope.Success)
            {
                return Failed<ProductMediaListResponse>(scope.Failure!.Value, scope.Message);
            }

            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize <= 0 ? 25 : query.PageSize, 1, 100);
            var mediaQuery = this.context.ProductMedia
                .AsNoTracking()
                .Where(media => media.StoreId == scope.StoreId && media.ProductId == productId && media.DeletedAt == null);
            var totalCount = await mediaQuery.CountAsync(cancellationToken);
            var mediaRows = await mediaQuery
                .OrderBy(media => media.SortOrder)
                .ThenBy(media => media.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            var items = mediaRows.Select(this.Map).ToList();

            return Succeeded(
                "Product media retrieved.",
                new ProductMediaListResponse(
                    items,
                    totalCount,
                    pageNumber,
                    pageSize,
                    totalCount <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)));
        }

        public async Task<ProductMediaOperationResult<ImportProductMediaResponse>> ImportAsync(
            Guid productId,
            ImportProductMediaRequest request,
            string? createdBy = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            var scope = await this.ResolveProductScopeAsync(productId, asTracking: false, cancellationToken);
            if (!scope.Success)
            {
                return Failed<ImportProductMediaResponse>(scope.Failure!.Value, scope.Message);
            }

            return await this.ImportScopedAsync(
                scope.StoreId,
                productId,
                request,
                createdBy,
                correlationId,
                cancellationToken);
        }

        public async Task<ProductMediaOperationResult<ImportProductMediaResponse>> ImportForStoreAsync(
            Guid storeId,
            Guid productId,
            ImportProductMediaRequest request,
            string? createdBy = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            if (storeId == Guid.Empty)
            {
                return Failed<ImportProductMediaResponse>(ProductMediaOperationFailure.Validation, "Store id is required.");
            }

            var productExists = await this.context.Products
                .AsNoTracking()
                .AnyAsync(
                    entity => entity.Id == productId && entity.StoreId == storeId && entity.ArchivedAt == null,
                    cancellationToken);
            if (!productExists)
            {
                return Failed<ImportProductMediaResponse>(ProductMediaOperationFailure.NotFound, "Product was not found for the current store.");
            }

            return await this.ImportScopedAsync(
                storeId,
                productId,
                request,
                createdBy,
                correlationId,
                cancellationToken);
        }

        private async Task<ProductMediaOperationResult<ImportProductMediaResponse>> ImportScopedAsync(
            Guid storeId,
            Guid productId,
            ImportProductMediaRequest request,
            string? createdBy,
            string? correlationId,
            CancellationToken cancellationToken)
        {
            var validationError = ValidateImportRequest(request);
            if (validationError is not null)
            {
                return Failed<ImportProductMediaResponse>(ProductMediaOperationFailure.Validation, validationError);
            }

            var now = DateTimeOffset.UtcNow;
            var normalizedItems = request.Items
                .Select((item, index) => NormalizeImportItem(item, index))
                .ToList();
            var idempotencyKey = BuildIdempotencyKey(
                storeId,
                productId,
                normalizedItems.Select(item => item.SourceUrl));
            var existingTaskResponse = await this.TryBuildExistingImportResponseAsync(
                idempotencyKey,
                cancellationToken);
            if (existingTaskResponse is not null)
            {
                return Succeeded("Product media import task already exists.", existingTaskResponse);
            }

            var mediaRows = normalizedItems
                .Select(item => new ProductMedia
                {
                    Id = Guid.NewGuid(),
                    PublicId = Guid.NewGuid(),
                    StoreId = storeId,
                    ProductId = productId,
                    OriginalSourceUrl = item.SourceUrl,
                    SortOrder = item.SortOrder,
                    AltText = item.AltText,
                    Status = ProductMediaStatuses.Pending,
                    Version = 1,
                    CreatedAt = now,
                    UpdatedAt = now,
                })
                .ToList();

            this.context.ProductMedia.AddRange(mediaRows);
            await this.context.SaveChangesAsync(cancellationToken);

            var payloadItems = mediaRows
                .Zip(normalizedItems)
                .Select(pair => new ProductMediaImportTaskItem(
                    pair.First.PublicId,
                    pair.Second.SourceUrl,
                    pair.Second.SortOrder,
                    pair.Second.IsPrimary,
                    pair.Second.AltText))
                .ToList();

            var payload = new ProductMediaImportTaskPayload(
                "v1",
                storeId,
                productId,
                payloadItems,
                createdBy,
                correlationId);

            var taskResult = await this.taskService.EnqueueAsync(
                new EnqueueCommerceTaskRequest(
                    ProductMediaTaskTypes.Import,
                    idempotencyKey,
                    "v1",
                    JsonSerializer.Serialize(payload, SerializerOptions),
                    $"product:{productId:D}:media",
                    MaxAttempts: 3,
                    CreatedBy: createdBy,
                    CorrelationId: correlationId),
                cancellationToken);

            if (!taskResult.Success || taskResult.Payload is null)
            {
                return Failed<ImportProductMediaResponse>(
                    ProductMediaOperationFailure.Conflict,
                    taskResult.Message ?? "Product media import task could not be queued.");
            }

            var response = new ImportProductMediaResponse(
                taskResult.Payload.PublicId,
                mediaRows.Select(this.Map).ToList());

            return Succeeded("Product media import queued.", response);
        }

        public async Task<ProductMediaOperationResult<ProductMediaDto>> SetPrimaryAsync(
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            var scope = await this.ResolveProductScopeAsync(productId, asTracking: true, cancellationToken);
            if (!scope.Success)
            {
                return Failed<ProductMediaDto>(scope.Failure!.Value, scope.Message);
            }

            var media = await this.context.ProductMedia
                .FirstOrDefaultAsync(
                    entity =>
                        entity.StoreId == scope.StoreId &&
                        entity.ProductId == productId &&
                        entity.PublicId == mediaPublicId &&
                        entity.DeletedAt == null,
                    cancellationToken);

            if (media is null)
            {
                return Failed<ProductMediaDto>(ProductMediaOperationFailure.NotFound, "Product media was not found.");
            }

            if (media.Status != ProductMediaStatuses.Stored)
            {
                return Failed<ProductMediaDto>(ProductMediaOperationFailure.Conflict, "Only stored media can be primary.");
            }

            await this.SetPrimaryMediaAsync(scope.Product!, media, DateTimeOffset.UtcNow, cancellationToken);
            await this.catalogQueryCache.InvalidateStoreCatalogAsync(scope.StoreId, cancellationToken);
            return Succeeded("Primary product media updated.", this.Map(media));
        }

        public async Task<ProductMediaOperationResult<ProductMediaListResponse>> UpdateOrderAsync(
            Guid productId,
            UpdateProductMediaOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.Items.Count == 0)
            {
                return Failed<ProductMediaListResponse>(ProductMediaOperationFailure.Validation, "At least one media order item is required.");
            }

            var scope = await this.ResolveProductScopeAsync(productId, asTracking: false, cancellationToken);
            if (!scope.Success)
            {
                return Failed<ProductMediaListResponse>(scope.Failure!.Value, scope.Message);
            }

            var mediaRows = await this.context.ProductMedia
                .Where(media => media.StoreId == scope.StoreId && media.ProductId == productId && media.DeletedAt == null)
                .ToListAsync(cancellationToken);
            var rowsByPublicId = mediaRows.ToDictionary(media => media.PublicId);
            var now = DateTimeOffset.UtcNow;

            foreach (var item in request.Items)
            {
                if (!rowsByPublicId.TryGetValue(item.MediaPublicId, out var media))
                {
                    return Failed<ProductMediaListResponse>(ProductMediaOperationFailure.NotFound, "One or more media items were not found.");
                }

                media.SortOrder = Math.Max(0, item.SortOrder);
                media.UpdatedAt = now;
            }

            await this.context.SaveChangesAsync(cancellationToken);
            return await this.ListAsync(productId, new ProductMediaListQuery(PageSize: 100), cancellationToken);
        }

        public async Task<ProductMediaOperationResult<ProductMediaListResponse>> DeleteAsync(
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            var scope = await this.ResolveProductScopeAsync(productId, asTracking: true, cancellationToken);
            if (!scope.Success)
            {
                return Failed<ProductMediaListResponse>(scope.Failure!.Value, scope.Message);
            }

            var media = await this.context.ProductMedia
                .FirstOrDefaultAsync(
                    entity =>
                        entity.StoreId == scope.StoreId &&
                        entity.ProductId == productId &&
                        entity.PublicId == mediaPublicId &&
                        entity.DeletedAt == null,
                    cancellationToken);

            if (media is null)
            {
                return Failed<ProductMediaListResponse>(ProductMediaOperationFailure.NotFound, "Product media was not found.");
            }

            var now = DateTimeOffset.UtcNow;
            var wasPrimary = media.IsPrimary;
            media.Status = ProductMediaStatuses.Deleted;
            media.DeletedAt = now;
            media.IsPrimary = false;
            media.UpdatedAt = now;

            if (wasPrimary)
            {
                await this.AssignNextPrimaryAsync(scope.Product!, scope.StoreId, productId, now, cancellationToken);
            }

            await this.context.SaveChangesAsync(cancellationToken);
            if (wasPrimary)
            {
                await this.catalogQueryCache.InvalidateStoreCatalogAsync(scope.StoreId, cancellationToken);
            }

            return await this.ListAsync(productId, new ProductMediaListQuery(PageSize: 100), cancellationToken);
        }

        public async Task<ProductMediaOperationResult<ImportProductMediaResponse>> RetryAsync(
            Guid productId,
            Guid mediaPublicId,
            string? createdBy = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            var scope = await this.ResolveProductScopeAsync(productId, asTracking: false, cancellationToken);
            if (!scope.Success)
            {
                return Failed<ImportProductMediaResponse>(scope.Failure!.Value, scope.Message);
            }

            var media = await this.context.ProductMedia
                .FirstOrDefaultAsync(
                    entity =>
                        entity.StoreId == scope.StoreId &&
                        entity.ProductId == productId &&
                        entity.PublicId == mediaPublicId &&
                        entity.DeletedAt == null,
                    cancellationToken);

            if (media is null)
            {
                return Failed<ImportProductMediaResponse>(ProductMediaOperationFailure.NotFound, "Product media was not found.");
            }

            if (string.IsNullOrWhiteSpace(media.OriginalSourceUrl))
            {
                return Failed<ImportProductMediaResponse>(ProductMediaOperationFailure.Conflict, "Product media has no source URL to retry.");
            }

            media.Status = ProductMediaStatuses.Pending;
            media.ErrorMessage = null;
            media.UpdatedAt = DateTimeOffset.UtcNow;
            await this.context.SaveChangesAsync(cancellationToken);

            var taskItem = new ProductMediaImportTaskItem(
                media.PublicId,
                media.OriginalSourceUrl,
                media.SortOrder,
                media.IsPrimary,
                media.AltText);
            var payload = new ProductMediaImportTaskPayload(
                "v1",
                scope.StoreId,
                productId,
                [taskItem],
                createdBy,
                correlationId);

            var taskResult = await this.taskService.EnqueueAsync(
                new EnqueueCommerceTaskRequest(
                    ProductMediaTaskTypes.Import,
                    BuildIdempotencyKey(scope.StoreId, productId, [media.OriginalSourceUrl, media.PublicId.ToString("D")]),
                    "v1",
                    JsonSerializer.Serialize(payload, SerializerOptions),
                    $"product:{productId:D}:media",
                    MaxAttempts: 3,
                    CreatedBy: createdBy,
                    CorrelationId: correlationId),
                cancellationToken);

            if (!taskResult.Success || taskResult.Payload is null)
            {
                return Failed<ImportProductMediaResponse>(
                    ProductMediaOperationFailure.Conflict,
                    taskResult.Message ?? "Product media retry task could not be queued.");
            }

            return Succeeded(
                "Product media retry queued.",
                new ImportProductMediaResponse(taskResult.Payload.PublicId, [this.Map(media)]));
        }

        private async Task<ProductScopeResult> ResolveProductScopeAsync(
            Guid productId,
            bool asTracking,
            CancellationToken cancellationToken)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success || storeResult.Payload == Guid.Empty)
            {
                return ProductScopeResult.Failed(ProductMediaOperationFailure.Validation, storeResult.Message ?? "Current store could not be resolved.");
            }

            var storeId = storeResult.Payload;
            var products = asTracking ? this.context.Products : this.context.Products.AsNoTracking();
            var product = await products.FirstOrDefaultAsync(
                entity => entity.Id == productId && entity.StoreId == storeId && entity.ArchivedAt == null,
                cancellationToken);

            return product is null
                ? ProductScopeResult.Failed(ProductMediaOperationFailure.NotFound, "Product was not found for the current store.")
                : ProductScopeResult.Succeeded(storeId, product);
        }

        private async Task SetPrimaryMediaAsync(
            Product product,
            ProductMedia media,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            var existingPrimaryRows = await this.context.ProductMedia
                .Where(entity =>
                    entity.StoreId == media.StoreId &&
                    entity.ProductId == media.ProductId &&
                    entity.DeletedAt == null &&
                    entity.IsPrimary)
                .ToListAsync(cancellationToken);

            foreach (var existingPrimary in existingPrimaryRows)
            {
                existingPrimary.IsPrimary = false;
                existingPrimary.UpdatedAt = now;
            }

            media.IsPrimary = true;
            media.UpdatedAt = now;
            product.Image = this.urlBuilder.BuildProductMediaUrl(media.PublicId, media.Version);
            product.UpdatedAt = now.UtcDateTime;
            await this.context.SaveChangesAsync(cancellationToken);
        }

        private async Task AssignNextPrimaryAsync(
            Product product,
            Guid storeId,
            Guid productId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            var nextPrimary = await this.context.ProductMedia
                .Where(media =>
                    media.StoreId == storeId &&
                    media.ProductId == productId &&
                    media.Status == ProductMediaStatuses.Stored &&
                    media.DeletedAt == null)
                .OrderBy(media => media.SortOrder)
                .ThenBy(media => media.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextPrimary is null)
            {
                product.Image = null;
                product.UpdatedAt = now.UtcDateTime;
                return;
            }

            nextPrimary.IsPrimary = true;
            nextPrimary.UpdatedAt = now;
            product.Image = this.urlBuilder.BuildProductMediaUrl(nextPrimary.PublicId, nextPrimary.Version);
            product.UpdatedAt = now.UtcDateTime;
        }

        private async Task<ImportProductMediaResponse?> TryBuildExistingImportResponseAsync(
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            var existingTask = await this.context.CommerceTasks
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    task => task.IdempotencyKey == idempotencyKey &&
                            task.TaskType == ProductMediaTaskTypes.Import &&
                            task.Status != CommerceTaskStatuses.Failed &&
                            task.Status != CommerceTaskStatuses.Dead &&
                            task.Status != CommerceTaskStatuses.Cancelled,
                    cancellationToken);

            if (existingTask is null)
            {
                return null;
            }

            ProductMediaImportTaskPayload? existingPayload;
            try
            {
                existingPayload = JsonSerializer.Deserialize<ProductMediaImportTaskPayload>(
                    existingTask.PayloadJson,
                    SerializerOptions);
            }
            catch (JsonException)
            {
                return null;
            }

            if (existingPayload is null || existingPayload.Items.Count == 0)
            {
                return null;
            }

            var mediaPublicIds = existingPayload.Items.Select(item => item.MediaPublicId).ToList();
            var mediaRows = await this.context.ProductMedia
                .AsNoTracking()
                .Where(media => mediaPublicIds.Contains(media.PublicId) && media.DeletedAt == null)
                .OrderBy(media => media.SortOrder)
                .ThenBy(media => media.CreatedAt)
                .ToListAsync(cancellationToken);

            return new ImportProductMediaResponse(existingTask.PublicId, mediaRows.Select(this.Map).ToList());
        }

        private ProductMediaDto Map(ProductMedia media)
        {
            var publicUrl = media.Status == ProductMediaStatuses.Stored
                ? this.urlBuilder.BuildProductMediaUrl(media.PublicId, media.Version)
                : null;

            return new ProductMediaDto(
                media.PublicId,
                media.StoreId,
                media.ProductId,
                media.OriginalSourceUrl,
                publicUrl,
                media.Status,
                media.ErrorMessage,
                media.SortOrder,
                media.IsPrimary,
                media.AltText,
                media.Version,
                media.Width,
                media.Height,
                media.FileSizeBytes,
                media.CreatedAt,
                media.UpdatedAt,
                media.ProcessedAt);
        }

        private static string? ValidateImportRequest(ImportProductMediaRequest request)
        {
            if (request.Items.Count == 0)
            {
                return "At least one media source URL is required.";
            }

            if (request.Items.Count > MaxImportItems)
            {
                return $"At most {MaxImportItems} media items can be imported at once.";
            }

            foreach (var item in request.Items)
            {
                if (string.IsNullOrWhiteSpace(item.SourceUrl))
                {
                    return "Media source URL is required.";
                }

                if (!Uri.TryCreate(item.SourceUrl.Trim(), UriKind.Absolute, out var uri) ||
                    uri.Scheme is not ("http" or "https"))
                {
                    return "Media source URL must be an absolute HTTP or HTTPS URL.";
                }
            }

            return null;
        }

        private static ProductMediaImportTaskItem NormalizeImportItem(ImportProductMediaItem item, int index)
        {
            return new ProductMediaImportTaskItem(
                Guid.Empty,
                item.SourceUrl!.Trim(),
                Math.Max(0, item.SortOrder == 0 ? index : item.SortOrder),
                item.IsPrimary,
                NormalizeOptional(item.AltText));
        }

        private static string BuildIdempotencyKey(Guid storeId, Guid productId, IEnumerable<string> values)
        {
            var material = $"{storeId:D}|{productId:D}|{string.Join("|", values.Select(value => value.Trim().ToLowerInvariant()))}";
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
            return $"product-media-import:{storeId:D}:{productId:D}:{hash}";
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static ProductMediaOperationResult<TPayload> Succeeded<TPayload>(string message, TPayload payload)
        {
            return new ProductMediaOperationResult<TPayload>(true, message, payload);
        }

        private static ProductMediaOperationResult<TPayload> Failed<TPayload>(
            ProductMediaOperationFailure failure,
            string? message)
        {
            return new ProductMediaOperationResult<TPayload>(
                false,
                string.IsNullOrWhiteSpace(message) ? "Product media request could not be completed." : message,
                Failure: failure);
        }

        private sealed record ProductScopeResult(
            bool Success,
            Guid StoreId,
            Product? Product,
            ProductMediaOperationFailure? Failure = null,
            string? Message = null)
        {
            public static ProductScopeResult Succeeded(Guid storeId, Product product)
            {
                return new ProductScopeResult(true, storeId, product);
            }

            public static ProductScopeResult Failed(ProductMediaOperationFailure failure, string message)
            {
                return new ProductScopeResult(false, Guid.Empty, null, failure, message);
            }
        }
    }
}
