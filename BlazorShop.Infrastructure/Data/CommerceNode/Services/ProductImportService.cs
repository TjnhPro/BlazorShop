namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Security.Cryptography;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class ProductImportService : IProductImportService
    {
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;
        private const int DefaultTake = 50;
        private const int MaxTake = 200;
        private const string SchemaVersion = "v1";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly ICommerceTaskService taskService;

        public ProductImportService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            ICommerceTaskService taskService)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.taskService = taskService;
        }

        public async Task<ServiceResponse<ProductImportUploadResponse>> UploadAsync(
            ProductImportUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var storeId = await this.ResolveStoreIdAsync();
            if (!storeId.HasValue)
            {
                return Failure<ProductImportUploadResponse>("Store context was not resolved.", ServiceResponseType.NotFound);
            }

            var mode = NormalizeMode(request.Mode);
            if (mode is null)
            {
                return Failure<ProductImportUploadResponse>("Product import mode is invalid.", ServiceResponseType.ValidationError);
            }

            if (string.IsNullOrWhiteSpace(request.FileName) || !request.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return Failure<ProductImportUploadResponse>("A CSV file is required.", ServiceResponseType.ValidationError);
            }

            if (request.FileSizeBytes <= 0 || request.FileSizeBytes > MaxFileSizeBytes)
            {
                return Failure<ProductImportUploadResponse>("CSV file must be greater than 0 bytes and no larger than 5 MB.", ServiceResponseType.ValidationError);
            }

            await using var memory = new MemoryStream();
            await request.Content.CopyToAsync(memory, cancellationToken);
            if (memory.Length <= 0 || memory.Length > MaxFileSizeBytes)
            {
                return Failure<ProductImportUploadResponse>("CSV file must be greater than 0 bytes and no larger than 5 MB.", ServiceResponseType.ValidationError);
            }

            var hash = Convert.ToHexString(SHA256.HashData(memory.ToArray())).ToLowerInvariant();
            var existing = await this.context.ProductImportJobs
                .AsNoTracking()
                .FirstOrDefaultAsync(job => job.StoreId == storeId && job.Mode == mode && job.FileHash == hash, cancellationToken);

            if (existing is not null)
            {
                return Success(new ProductImportUploadResponse(MapJob(existing)), "Product import already exists for this file.");
            }

            var now = DateTime.UtcNow;
            var job = new ProductImportJob
            {
                StoreId = storeId.Value,
                Mode = mode,
                Status = ProductImportJobStatuses.Queued,
                FileName = Path.GetFileName(request.FileName),
                FileHash = hash,
                FileSizeBytes = memory.Length,
                CreatedBy = NormalizeOptional(request.CreatedBy),
                CreatedAt = now,
                UpdatedAt = now,
            };

            job.StoredFilePath = BuildStoredFilePath(job.PublicId);
            Directory.CreateDirectory(Path.GetDirectoryName(job.StoredFilePath)!);
            memory.Position = 0;
            await File.WriteAllBytesAsync(job.StoredFilePath, memory.ToArray(), cancellationToken);

            this.context.ProductImportJobs.Add(job);
            await this.context.SaveChangesAsync(cancellationToken);

            var payload = JsonSerializer.Serialize(
                new ProductImportTaskPayload(SchemaVersion, job.PublicId, job.StoreId, job.Mode, job.StoredFilePath),
                SerializerOptions);
            var enqueue = await this.taskService.EnqueueAsync(
                new EnqueueCommerceTaskRequest(
                    ProductImportTaskTypes.Import,
                    IdempotencyKey: $"product-import:{job.StoreId:D}:{job.Mode}:{job.FileHash}",
                    PayloadSchemaVersion: SchemaVersion,
                    PayloadJson: payload,
                    LockKey: $"product-import:{job.StoreId:D}",
                    CreatedBy: job.CreatedBy,
                    CorrelationId: job.PublicId.ToString("D")),
                cancellationToken);

            if (!enqueue.Success || enqueue.Payload is null)
            {
                job.Status = ProductImportJobStatuses.Failed;
                job.UpdatedAt = DateTime.UtcNow;
                await this.context.SaveChangesAsync(cancellationToken);
                return Failure<ProductImportUploadResponse>(enqueue.Message ?? "Product import task could not be queued.", ServiceResponseType.Failure);
            }

            job.TaskPublicId = enqueue.Payload.PublicId;
            job.UpdatedAt = DateTime.UtcNow;
            await this.context.SaveChangesAsync(cancellationToken);

            return Success(new ProductImportUploadResponse(MapJob(job)), "Product import queued.");
        }

        public async Task<ServiceResponse<ProductImportJobListResponse>> ListAsync(
            ProductImportJobListQuery query,
            CancellationToken cancellationToken = default)
        {
            var storeId = await this.ResolveStoreIdAsync();
            if (!storeId.HasValue)
            {
                return Failure<ProductImportJobListResponse>("Store context was not resolved.", ServiceResponseType.NotFound);
            }

            var skip = Math.Max(0, query.Skip);
            var take = Math.Clamp(query.Take <= 0 ? DefaultTake : query.Take, 1, MaxTake);
            var jobs = this.context.ProductImportJobs.AsNoTracking().Where(job => job.StoreId == storeId);
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                jobs = jobs.Where(job => job.Status == query.Status.Trim());
            }

            var total = await jobs.CountAsync(cancellationToken);
            var items = await jobs
                .OrderByDescending(job => job.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(job => MapJob(job))
                .ToListAsync(cancellationToken);

            return Success(new ProductImportJobListResponse(items, total, skip, take), "Product imports retrieved.");
        }

        public async Task<ServiceResponse<ProductImportJobDetailDto>> GetByPublicIdAsync(
            Guid jobPublicId,
            CancellationToken cancellationToken = default)
        {
            var job = await this.LoadJobAsync(jobPublicId, cancellationToken);
            if (job is null)
            {
                return Failure<ProductImportJobDetailDto>("Product import job was not found.", ServiceResponseType.NotFound);
            }

            return Success(new ProductImportJobDetailDto(MapJob(job), job.Rows.OrderBy(row => row.RowNumber).Take(100).Select(MapRow).ToArray()), "Product import job retrieved.");
        }

        public async Task<ServiceResponse<ProductImportRowsResponse>> ListRowsAsync(
            Guid jobPublicId,
            ProductImportRowsQuery query,
            CancellationToken cancellationToken = default)
        {
            var job = await this.LoadJobAsync(jobPublicId, cancellationToken);
            if (job is null)
            {
                return Failure<ProductImportRowsResponse>("Product import job was not found.", ServiceResponseType.NotFound);
            }

            var skip = Math.Max(0, query.Skip);
            var take = Math.Clamp(query.Take <= 0 ? 100 : query.Take, 1, MaxTake);
            var rows = this.context.ProductImportRows.AsNoTracking().Where(row => row.JobId == job.Id);
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                rows = rows.Where(row => row.Status == query.Status.Trim());
            }

            var total = await rows.CountAsync(cancellationToken);
            var items = await rows
                .OrderBy(row => row.RowNumber)
                .Skip(skip)
                .Take(take)
                .Select(row => MapRow(row))
                .ToListAsync(cancellationToken);

            return Success(new ProductImportRowsResponse(items, total, skip, take), "Product import rows retrieved.");
        }

        private async Task<ProductImportJob?> LoadJobAsync(Guid publicId, CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync();
            if (!storeId.HasValue || publicId == Guid.Empty)
            {
                return null;
            }

            return await this.context.ProductImportJobs
                .AsNoTracking()
                .Include(job => job.Rows.OrderBy(row => row.RowNumber))
                .FirstOrDefaultAsync(job => job.StoreId == storeId && job.PublicId == publicId, cancellationToken);
        }

        private async Task<Guid?> ResolveStoreIdAsync()
        {
            var result = await this.storeContext.GetCurrentStoreIdAsync();
            return result.Success && result.Payload != Guid.Empty ? result.Payload : null;
        }

        private static string BuildStoredFilePath(Guid jobPublicId)
        {
            return Path.Combine(AppContext.BaseDirectory, "commerce-node-imports", "products", jobPublicId.ToString("D"), "source.csv");
        }

        private static string? NormalizeMode(string? mode)
        {
            if (string.IsNullOrWhiteSpace(mode))
            {
                return ProductImportModes.CreateOnly;
            }

            var trimmed = mode.Trim();
            return ProductImportModes.All.FirstOrDefault(item => string.Equals(item, trimmed, StringComparison.OrdinalIgnoreCase));
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static ProductImportJobDto MapJob(ProductImportJob job)
        {
            return new ProductImportJobDto(
                job.PublicId,
                job.StoreId,
                job.TaskPublicId,
                job.Mode,
                job.Status,
                job.FileName,
                job.FileHash,
                job.FileSizeBytes,
                job.TotalRows,
                job.CreatedCount,
                job.UpdatedCount,
                job.FailedCount,
                job.SkippedCount,
                job.MediaQueuedCount,
                job.CreatedAt,
                job.StartedAt,
                job.CompletedAt,
                job.UpdatedAt);
        }

        private static ProductImportRowDto MapRow(ProductImportRow row)
        {
            return new ProductImportRowDto(
                row.RowNumber,
                row.Sku,
                row.Status,
                row.Action,
                row.ProductId,
                row.MediaStatus,
                row.MediaTaskPublicId,
                row.ErrorMessage,
                row.ErrorJson,
                row.RawDataJson,
                row.CreatedAt,
                row.UpdatedAt);
        }

        private static ServiceResponse<TPayload> Success<TPayload>(TPayload payload, string message)
        {
            return new ServiceResponse<TPayload>(true, message)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<TPayload> Failure<TPayload>(string message, ServiceResponseType responseType)
        {
            return new ServiceResponse<TPayload>(false, message)
            {
                ResponseType = responseType,
            };
        }
    }
}
