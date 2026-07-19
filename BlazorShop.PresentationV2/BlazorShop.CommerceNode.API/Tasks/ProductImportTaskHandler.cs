namespace BlazorShop.CommerceNode.API.Tasks
{
    using System.Globalization;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class ProductImportTaskHandler : ICommerceTaskHandler
    {
        private const int MaxRows = 1000;
        private const int MaxImageUrls = 10;
        private const int MaxImageUrlLength = 1024;
        private const string ClearToken = "__clear__";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly CommerceNodeDbContext context;
        private readonly IProductImportCsvParser parser;
        private readonly IProductMediaService productMediaService;
        private readonly ISlugService slugService;

        public ProductImportTaskHandler(
            CommerceNodeDbContext context,
            IProductImportCsvParser parser,
            IProductMediaService productMediaService,
            ISlugService slugService)
        {
            this.context = context;
            this.parser = parser;
            this.productMediaService = productMediaService;
            this.slugService = slugService;
        }

        public string TaskType => ProductImportTaskTypes.Import;

        public async Task<CommerceTaskHandlerResult> ExecuteAsync(
            CommerceTaskHandlerContext handlerContext,
            CancellationToken cancellationToken)
        {
            ProductImportTaskPayload payload;
            try
            {
                payload = JsonSerializer.Deserialize<ProductImportTaskPayload>(handlerContext.PayloadJson, SerializerOptions)
                          ?? throw new JsonException("Empty payload.");
            }
            catch (JsonException)
            {
                return CommerceTaskHandlerResult.Failed("Product import payload is invalid.", "invalid_payload", retryable: false);
            }

            var job = await this.context.ProductImportJobs
                .Include(item => item.Rows)
                .FirstOrDefaultAsync(item => item.PublicId == payload.JobPublicId && item.StoreId == payload.StoreId, cancellationToken);

            if (job is null)
            {
                return CommerceTaskHandlerResult.Failed("Product import job was not found.", "job_not_found", retryable: false);
            }

            var now = DateTime.UtcNow;
            job.Status = ProductImportJobStatuses.Running;
            job.StartedAt ??= now;
            job.ErrorMessage = null;
            job.ErrorJson = null;
            job.UpdatedAt = now;
            if (job.Rows.Count > 0)
            {
                this.context.ProductImportRows.RemoveRange(job.Rows);
            }

            await this.context.SaveChangesAsync(cancellationToken);

            if (!File.Exists(payload.StoredFilePath))
            {
                await this.FailJobAsync(job, "Stored CSV file was not found.", cancellationToken);
                return CommerceTaskHandlerResult.Failed("Stored CSV file was not found.", "file_not_found", retryable: false);
            }

            await using var file = File.OpenRead(payload.StoredFilePath);
            var parsed = await this.parser.ParseAsync(file, MaxRows, cancellationToken);
            if (parsed.Errors.Count > 0)
            {
                await this.FailJobAsync(job, string.Join(" ", parsed.Errors.Select(error => error.Message)), cancellationToken);
                return CommerceTaskHandlerResult.Failed("Product import CSV could not be parsed.", "csv_invalid", retryable: false);
            }

            var counters = new Counters();
            foreach (var row in parsed.Rows)
            {
                if (await handlerContext.IsCancellationRequestedAsync(cancellationToken))
                {
                    return CommerceTaskHandlerResult.Failed("Product import was cancelled.", "cancelled", retryable: false);
                }

                var result = await this.ProcessRowAsync(job, payload.Mode, row, cancellationToken);
                counters.Add(result);
            }

            job.TotalRows = parsed.Rows.Count;
            job.CreatedCount = counters.Created;
            job.UpdatedCount = counters.Updated;
            job.FailedCount = counters.Failed;
            job.SkippedCount = counters.Skipped;
            job.MediaQueuedCount = counters.MediaQueued;
            job.Status = counters.Failed > 0 ? ProductImportJobStatuses.CompletedWithErrors : ProductImportJobStatuses.Completed;
            job.ErrorMessage = null;
            job.ErrorJson = null;
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = job.CompletedAt.Value;
            await this.context.SaveChangesAsync(cancellationToken);

            var resultJson = JsonSerializer.Serialize(
                new ProductImportTaskResult(job.PublicId, job.TotalRows, job.CreatedCount, job.UpdatedCount, job.FailedCount, job.SkippedCount, job.MediaQueuedCount),
                SerializerOptions);

            return CommerceTaskHandlerResult.Succeeded("Product import completed.", resultJson);
        }

        private async Task<RowResult> ProcessRowAsync(
            ProductImportJob job,
            string mode,
            ProductImportParsedRow parsedRow,
            CancellationToken cancellationToken)
        {
            var values = parsedRow.Values;
            var errors = new List<ProductImportError>();
            var sku = Get(values, "sku");
            if (string.IsNullOrWhiteSpace(sku))
            {
                errors.Add(new ProductImportError("sku", "SKU is required."));
            }

            var existing = string.IsNullOrWhiteSpace(sku)
                ? null
                : await this.context.Products.FirstOrDefaultAsync(
                    product => product.StoreId == job.StoreId
                        && product.Sku == sku
                        && product.ArchivedAt == null,
                    cancellationToken);

            var isCreate = existing is null;
            if (mode == ProductImportModes.CreateOnly && existing is not null)
            {
                errors.Add(new ProductImportError("sku", "SKU already exists for this store."));
            }

            var rowValues = await this.ResolveValuesAsync(job.StoreId, values, isCreate, existing, errors, cancellationToken);
            if (errors.Count > 0 || rowValues is null)
            {
                await this.AddRowAsync(job, parsedRow, sku, ProductImportRowStatuses.Failed, ProductImportRowActions.Failed, null, ProductImportMediaStatuses.None, null, errors, cancellationToken);
                return RowResult.ForFailed();
            }

            var now = DateTime.UtcNow;
            var product = existing ?? new Product
            {
                Id = Guid.NewGuid(),
                StoreId = job.StoreId,
                CreatedOn = now,
            };

            ApplyValues(product, rowValues, isCreate, now);
            if (isCreate)
            {
                this.context.Products.Add(product);
            }

            await this.context.SaveChangesAsync(cancellationToken);

            Guid? mediaTaskPublicId = null;
            var mediaStatus = ProductImportMediaStatuses.None;
            if (rowValues.ImageUrls.Count > 0)
            {
                var mediaResult = await this.productMediaService.ImportForStoreAsync(
                    job.StoreId,
                    product.Id,
                    new ImportProductMediaRequest(rowValues.ImageUrls.Select((url, index) => new ImportProductMediaItem(url, index, index == 0, product.Name)).ToArray()),
                    job.CreatedBy,
                    job.PublicId.ToString("D"),
                    cancellationToken);

                if (mediaResult.Success && mediaResult.Value is not null)
                {
                    mediaTaskPublicId = mediaResult.Value.TaskPublicId;
                    mediaStatus = ProductImportMediaStatuses.Queued;
                }
                else
                {
                    await this.AddRowAsync(
                        job,
                        parsedRow,
                        sku,
                        ProductImportRowStatuses.Failed,
                        ProductImportRowActions.Failed,
                        product.Id,
                        ProductImportMediaStatuses.None,
                        null,
                        [new ProductImportError("image_urls", mediaResult.Message ?? "Product media import could not be queued.")],
                        cancellationToken);

                    return RowResult.ForFailed();
                }
            }

            await this.AddRowAsync(
                job,
                parsedRow,
                sku,
                ProductImportRowStatuses.Succeeded,
                isCreate ? ProductImportRowActions.Created : ProductImportRowActions.Updated,
                product.Id,
                mediaStatus,
                mediaTaskPublicId,
                [],
                cancellationToken);

            return isCreate
                ? RowResult.ForCreated(mediaStatus == ProductImportMediaStatuses.Queued)
                : RowResult.ForUpdated(mediaStatus == ProductImportMediaStatuses.Queued);
        }

        private async Task<ResolvedRowValues?> ResolveValuesAsync(
            Guid storeId,
            IReadOnlyDictionary<string, string?> values,
            bool isCreate,
            Product? existing,
            List<ProductImportError> errors,
            CancellationToken cancellationToken)
        {
            var name = ResolveText(values, "name", existing?.Name, isCreate, errors, requiredOnCreate: true);
            var description = ResolveText(values, "description", existing?.Description, isCreate, errors, requiredOnCreate: true);
            var shortDescription = ResolveNullableText(values, "short_description", existing?.ShortDescription, isCreate);
            var gtin = ResolveNullableText(values, "gtin", existing?.Gtin, isCreate);
            var barcode = ResolveNullableText(values, "barcode", existing?.Barcode, isCreate);
            var manufacturerPartNumber = ResolveNullableText(values, "manufacturer_part_number", existing?.ManufacturerPartNumber, isCreate);
            var condition = ResolveNullableText(values, "condition", existing?.Condition, isCreate)?.ToLowerInvariant();
            var weight = ResolveNullableDecimal(values, "weight", existing?.Weight, errors);
            var length = ResolveNullableDecimal(values, "length", existing?.Length, errors);
            var width = ResolveNullableDecimal(values, "width", existing?.Width, errors);
            var height = ResolveNullableDecimal(values, "height", existing?.Height, errors);
            var productType = ResolveProductType(values, existing?.ProductType, isCreate, errors);
            var price = ResolveDecimal(values, "price", existing?.Price, isCreate, errors, requiredOnCreate: true, mustBePositive: true);
            var comparePrice = ResolveNullableDecimal(values, "compare_price", existing?.ComparePrice, errors);
            var quantity = ResolveInt(values, "quantity", existing?.Quantity, isCreate, errors);
            var isPublished = ResolveBool(values, "is_published", existing?.IsPublished, isCreate, errors);
            var availableStartUtc = ResolveNullableDateTimeUtc(values, "available_start_utc", existing?.AvailableStartUtc, errors);
            var availableEndUtc = ResolveNullableDateTimeUtc(values, "available_end_utc", existing?.AvailableEndUtc, errors);
            var categoryId = await ResolveCategoryIdAsync(storeId, values, existing?.CategoryId, isCreate, errors, cancellationToken);
            var variationTemplateId = await ResolveVariationTemplateIdAsync(storeId, values, existing?.VariationTemplateId, productType, isCreate, errors, cancellationToken);
            var imageUrls = ResolveImageUrls(values, errors);
            var slug = await ResolveSlugAsync(storeId, values, name, existing?.Slug, existing?.Id, isCreate, errors, cancellationToken);

            if (availableStartUtc.HasValue && availableEndUtc.HasValue && availableEndUtc.Value <= availableStartUtc.Value)
            {
                errors.Add(new ProductImportError("available_end_utc", "available_end_utc must be after available_start_utc."));
            }

            ValidateIdentityFields(gtin, barcode, manufacturerPartNumber, condition, errors);

            if (errors.Count > 0 || name is null || description is null || productType is null || !price.HasValue || !quantity.HasValue || !isPublished.HasValue)
            {
                return null;
            }

            return new ResolvedRowValues(
                Get(values, "sku")!.Trim(),
                name,
                description,
                shortDescription,
                slug,
                categoryId,
                productType,
                variationTemplateId,
                price.Value,
                comparePrice,
                gtin,
                barcode,
                manufacturerPartNumber,
                condition,
                weight,
                length,
                width,
                height,
                quantity.Value,
                isPublished.Value,
                availableStartUtc,
                availableEndUtc,
                imageUrls);
        }

        private async Task<Guid?> ResolveCategoryIdAsync(Guid storeId, IReadOnlyDictionary<string, string?> values, Guid? currentCategoryId, bool isCreate, List<ProductImportError> errors, CancellationToken cancellationToken)
        {
            var raw = Get(values, "category_slug");
            if (string.IsNullOrWhiteSpace(raw))
            {
                return isCreate ? null : currentCategoryId;
            }

            if (IsClear(raw))
            {
                return null;
            }

            var slug = this.slugService.NormalizeSlug(raw);
            var category = await this.context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.StoreId == storeId && item.Slug == slug && item.ArchivedAt == null, cancellationToken);
            if (category is null)
            {
                errors.Add(new ProductImportError("category_slug", "Category slug was not found for this store."));
                return currentCategoryId;
            }

            return category.Id;
        }

        private async Task<Guid?> ResolveVariationTemplateIdAsync(Guid storeId, IReadOnlyDictionary<string, string?> values, Guid? currentTemplateId, string? productType, bool isCreate, List<ProductImportError> errors, CancellationToken cancellationToken)
        {
            var raw = Get(values, "variation_template_slug");
            if (string.IsNullOrWhiteSpace(raw))
            {
                if (string.Equals(productType, ProductTypes.CustomVariations, StringComparison.OrdinalIgnoreCase))
                {
                    if (isCreate || !currentTemplateId.HasValue)
                    {
                        errors.Add(new ProductImportError("variation_template_slug", "Variation template slug is required for CustomVariations products."));
                    }

                    return currentTemplateId;
                }

                return string.Equals(productType, ProductTypes.CustomVariations, StringComparison.OrdinalIgnoreCase) ? currentTemplateId : null;
            }

            if (IsClear(raw))
            {
                return string.Equals(productType, ProductTypes.CustomVariations, StringComparison.OrdinalIgnoreCase)
                    ? currentTemplateId
                    : null;
            }

            var slug = this.slugService.NormalizeSlug(raw);
            var template = await this.context.VariationTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.StoreId == storeId && item.Slug == slug && item.IsActive, cancellationToken);
            if (template is null)
            {
                errors.Add(new ProductImportError("variation_template_slug", "Variation template slug was not found or is inactive."));
                return currentTemplateId;
            }

            return template.Id;
        }

        private async Task<string?> ResolveSlugAsync(Guid storeId, IReadOnlyDictionary<string, string?> values, string? name, string? currentSlug, Guid? currentProductId, bool isCreate, List<ProductImportError> errors, CancellationToken cancellationToken)
        {
            var raw = Get(values, "slug");
            if (string.IsNullOrWhiteSpace(raw))
            {
                if (!isCreate)
                {
                    return currentSlug;
                }

                raw = name;
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                errors.Add(new ProductImportError("slug", "Slug is required when name is missing."));
                return currentSlug;
            }

            if (IsClear(raw))
            {
                errors.Add(new ProductImportError("slug", "Slug cannot be cleared."));
                return currentSlug;
            }

            var slug = this.slugService.NormalizeSlug(raw);
            if (string.IsNullOrWhiteSpace(slug))
            {
                errors.Add(new ProductImportError("slug", "Slug is invalid."));
                return currentSlug;
            }

            var exists = await this.context.Products.AnyAsync(
                product => product.StoreId == storeId
                    && product.Slug == slug
                    && product.ArchivedAt == null
                    && (!currentProductId.HasValue || product.Id != currentProductId.Value),
                cancellationToken);
            if (exists)
            {
                errors.Add(new ProductImportError("slug", "Slug already exists for this store."));
                return currentSlug;
            }

            return slug;
        }

        private static void ApplyValues(Product product, ResolvedRowValues values, bool isCreate, DateTime now)
        {
            product.Name = values.Name;
            product.Description = values.Description;
            product.ShortDescription = values.ShortDescription;
            product.FullDescription = values.Description;
            product.Slug = values.Slug;
            product.CategoryId = values.CategoryId;
            product.ProductType = values.ProductType;
            product.VariationTemplateId = string.Equals(values.ProductType, ProductTypes.CustomVariations, StringComparison.OrdinalIgnoreCase)
                ? values.VariationTemplateId
                : null;
            product.Price = values.Price;
            product.ComparePrice = values.ComparePrice;
            product.Gtin = values.Gtin;
            product.Barcode = values.Barcode;
            product.ManufacturerPartNumber = values.ManufacturerPartNumber;
            product.Condition = values.Condition;
            product.Weight = values.Weight;
            product.Length = values.Length;
            product.Width = values.Width;
            product.Height = values.Height;
            product.Quantity = values.Quantity;
            product.IsPublished = values.IsPublished;
            product.PublishedOn = values.IsPublished ? product.PublishedOn ?? now : null;
            product.AvailableStartUtc = values.AvailableStartUtc;
            product.AvailableEndUtc = values.AvailableEndUtc;
            product.UpdatedAt = now;
            if (isCreate)
            {
                product.Sku = values.Sku;
            }
        }

        private static string? ResolveText(IReadOnlyDictionary<string, string?> values, string column, string? current, bool isCreate, List<ProductImportError> errors, bool requiredOnCreate)
        {
            var raw = Get(values, column);
            if (string.IsNullOrWhiteSpace(raw))
            {
                if (isCreate && requiredOnCreate)
                {
                    errors.Add(new ProductImportError(column, $"{column} is required."));
                }

                return isCreate ? null : current;
            }

            return raw.Trim();
        }

        private static string? ResolveNullableText(IReadOnlyDictionary<string, string?> values, string column, string? current, bool isCreate)
        {
            var raw = Get(values, column);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return isCreate ? null : current;
            }

            return IsClear(raw) ? null : raw.Trim();
        }

        private static string? ResolveProductType(IReadOnlyDictionary<string, string?> values, string? current, bool isCreate, List<ProductImportError> errors)
        {
            var raw = Get(values, "product_type");
            var productType = string.IsNullOrWhiteSpace(raw)
                ? isCreate ? ProductTypes.Simple : current ?? ProductTypes.Simple
                : ProductTypes.All.FirstOrDefault(type => string.Equals(type, raw.Trim(), StringComparison.OrdinalIgnoreCase));
            if (productType is null)
            {
                errors.Add(new ProductImportError("product_type", "Product type is invalid."));
            }

            return productType;
        }

        private static decimal? ResolveDecimal(IReadOnlyDictionary<string, string?> values, string column, decimal? current, bool isCreate, List<ProductImportError> errors, bool requiredOnCreate, bool mustBePositive)
        {
            var raw = Get(values, column);
            if (string.IsNullOrWhiteSpace(raw))
            {
                if (isCreate && requiredOnCreate)
                {
                    errors.Add(new ProductImportError(column, $"{column} is required."));
                }

                return isCreate ? null : current;
            }

            if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || (mustBePositive && value <= 0))
            {
                errors.Add(new ProductImportError(column, $"{column} is invalid."));
                return current;
            }

            return value;
        }

        private static decimal? ResolveNullableDecimal(IReadOnlyDictionary<string, string?> values, string column, decimal? current, List<ProductImportError> errors)
        {
            var raw = Get(values, column);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return current;
            }

            if (IsClear(raw))
            {
                return null;
            }

            if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || value < 0)
            {
                errors.Add(new ProductImportError(column, $"{column} is invalid."));
                return current;
            }

            return value;
        }

        private static void ValidateIdentityFields(
            string? gtin,
            string? barcode,
            string? manufacturerPartNumber,
            string? condition,
            List<ProductImportError> errors)
        {
            AddLengthError(gtin, "gtin", ProductIdentityConstraints.GtinMaxLength, errors);
            AddLengthError(barcode, "barcode", ProductIdentityConstraints.BarcodeMaxLength, errors);
            AddLengthError(manufacturerPartNumber, "manufacturer_part_number", ProductIdentityConstraints.ManufacturerPartNumberMaxLength, errors);
            AddLengthError(condition, "condition", ProductIdentityConstraints.ConditionMaxLength, errors);

            if (!string.IsNullOrWhiteSpace(condition)
                && !ProductIdentityConstraints.Conditions.Contains(condition, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add(new ProductImportError("condition", "condition is invalid."));
            }
        }

        private static void AddLengthError(string? value, string column, int maxLength, List<ProductImportError> errors)
        {
            if (value?.Length > maxLength)
            {
                errors.Add(new ProductImportError(column, $"{column} must be {maxLength} characters or fewer."));
            }
        }

        private static int? ResolveInt(IReadOnlyDictionary<string, string?> values, string column, int? current, bool isCreate, List<ProductImportError> errors)
        {
            var raw = Get(values, column);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return isCreate ? 0 : current;
            }

            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < 0)
            {
                errors.Add(new ProductImportError(column, $"{column} is invalid."));
                return current;
            }

            return value;
        }

        private static bool? ResolveBool(IReadOnlyDictionary<string, string?> values, string column, bool? current, bool isCreate, List<ProductImportError> errors)
        {
            var raw = Get(values, column);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return isCreate ? false : current;
            }

            var normalized = raw.Trim().ToLowerInvariant();
            return normalized switch
            {
                "true" or "1" or "yes" => true,
                "false" or "0" or "no" => false,
                _ => AddBoolError(column, errors, current),
            };
        }

        private static bool? AddBoolError(string column, List<ProductImportError> errors, bool? current)
        {
            errors.Add(new ProductImportError(column, $"{column} is invalid."));
            return current;
        }

        private static DateTime? ResolveNullableDateTimeUtc(
            IReadOnlyDictionary<string, string?> values,
            string column,
            DateTime? current,
            List<ProductImportError> errors)
        {
            var raw = Get(values, column);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return current;
            }

            if (IsClear(raw))
            {
                return null;
            }

            if (!DateTimeOffset.TryParse(
                    raw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var value))
            {
                errors.Add(new ProductImportError(column, $"{column} is invalid."));
                return current;
            }

            return value.UtcDateTime;
        }

        private static IReadOnlyList<string> ResolveImageUrls(IReadOnlyDictionary<string, string?> values, List<ProductImportError> errors)
        {
            var raw = Get(values, "image_urls");
            if (string.IsNullOrWhiteSpace(raw))
            {
                return [];
            }

            if (IsClear(raw))
            {
                return [];
            }

            var urls = raw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (urls.Length > MaxImageUrls)
            {
                errors.Add(new ProductImportError("image_urls", $"At most {MaxImageUrls} image URLs are allowed."));
                return [];
            }

            foreach (var url in urls)
            {
                if (url.Length > MaxImageUrlLength || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
                {
                    errors.Add(new ProductImportError("image_urls", "Image URLs must be absolute HTTP or HTTPS URLs."));
                    return [];
                }
            }

            return urls;
        }

        private async Task AddRowAsync(ProductImportJob job, ProductImportParsedRow parsedRow, string? sku, string status, string action, Guid? productId, string mediaStatus, Guid? mediaTaskPublicId, IReadOnlyList<ProductImportError> errors, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            this.context.ProductImportRows.Add(new ProductImportRow
            {
                JobId = job.Id,
                RowNumber = parsedRow.RowNumber,
                Sku = string.IsNullOrWhiteSpace(sku) ? null : sku.Trim(),
                Status = status,
                Action = action,
                ProductId = productId,
                MediaStatus = mediaStatus,
                MediaTaskPublicId = mediaTaskPublicId,
                ErrorMessage = errors.Count == 0 ? null : string.Join(" ", errors.Select(error => error.Message)),
                ErrorJson = errors.Count == 0 ? null : JsonSerializer.Serialize(errors, SerializerOptions),
                RawDataJson = JsonSerializer.Serialize(parsedRow.Values, SerializerOptions),
                CreatedAt = now,
                UpdatedAt = now,
            });
            await this.context.SaveChangesAsync(cancellationToken);
        }

        private async Task FailJobAsync(ProductImportJob job, string message, CancellationToken cancellationToken)
        {
            job.Status = ProductImportJobStatuses.Failed;
            job.ErrorMessage = message;
            job.ErrorJson = JsonSerializer.Serialize(
                new[] { new ProductImportError("file", message) },
                SerializerOptions);
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = job.CompletedAt.Value;
            job.FailedCount = Math.Max(1, job.FailedCount);
            await this.context.SaveChangesAsync(cancellationToken);
        }

        private static string? Get(IReadOnlyDictionary<string, string?> values, string key)
        {
            return values.TryGetValue(key, out var value) ? value : null;
        }

        private static bool IsClear(string value) => string.Equals(value.Trim(), ClearToken, StringComparison.OrdinalIgnoreCase);

        private sealed record ResolvedRowValues(
            string Sku,
            string Name,
            string Description,
            string? ShortDescription,
            string? Slug,
            Guid? CategoryId,
            string ProductType,
            Guid? VariationTemplateId,
            decimal Price,
            decimal? ComparePrice,
            string? Gtin,
            string? Barcode,
            string? ManufacturerPartNumber,
            string? Condition,
            decimal? Weight,
            decimal? Length,
            decimal? Width,
            decimal? Height,
            int Quantity,
            bool IsPublished,
            DateTime? AvailableStartUtc,
            DateTime? AvailableEndUtc,
            IReadOnlyList<string> ImageUrls);

        private sealed record RowResult(int Created, int Updated, int Failed, int Skipped, int MediaQueued)
        {
            public static RowResult ForCreated(bool mediaQueued) => new(1, 0, 0, 0, mediaQueued ? 1 : 0);

            public static RowResult ForUpdated(bool mediaQueued) => new(0, 1, 0, 0, mediaQueued ? 1 : 0);

            public static RowResult ForFailed() => new(0, 0, 1, 0, 0);
        }

        private sealed class Counters
        {
            public int Created { get; private set; }

            public int Updated { get; private set; }

            public int Failed { get; private set; }

            public int Skipped { get; private set; }

            public int MediaQueued { get; private set; }

            public void Add(RowResult result)
            {
                this.Created += result.Created;
                this.Updated += result.Updated;
                this.Failed += result.Failed;
                this.Skipped += result.Skipped;
                this.MediaQueued += result.MediaQueued;
            }
        }
    }
}
