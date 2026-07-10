# BlazorShop CommerceNode Product Import Task Todo

Status: draft
Created: 2026-07-10
Scope: CSV-only product import, async CommerceTask processing, import history, row-level error report, media task enqueue, ControlPlane upload UI/API proxy.

## Goal

Add CSV product import for CommerceNode catalog as an async task workflow.

The target is:

- ControlPlane admin uploads a CSV file for one store.
- ControlPlane API proxies the upload to the correct CommerceNode.
- CommerceNode stores the source file.
- CommerceNode creates a `ProductImportJob`.
- CommerceNode enqueues a `product.import` CommerceTask.
- CommerceTaskWorker processes rows asynchronously.
- Valid rows create/update products.
- Invalid rows are recorded with row/column errors.
- Product images are delegated to existing `product.media.import` tasks.

## Dependency

This plan depends on:

- `BlazorShop.CommerceNode.VariationTemplateFoundation.todo.md`

Reason:

- Product import CSV uses `product_type`.
- Product import CSV uses `variation_template_slug`.
- `CustomVariations` product type needs `Product.VariationTemplateId`.

Do not implement Product Import Task before Variation Template Foundation is merged.

## Locked Decisions

- CSV only in MVP.
- No Excel in MVP.
- Upload UI lives in ControlPlane.
- ControlPlane Web calls only ControlPlane API.
- ControlPlane API proxies upload/status requests to CommerceNode API.
- CommerceNode owns file storage, parsing, validation, product writes, and task processing.
- Use existing `CommerceTaskWorker`.
- Add a `product.import` task handler.
- Do not add a separate product worker service in MVP.
- Import supports mode:
  - `create_only`
  - `upsert`
- Product identity is `StoreId + Sku`.
- Partial success per row.
- Do not rollback the whole file when some rows fail.
- No dry-run in MVP.
- `category_slug` must already exist when supplied.
- Do not auto-create categories from CSV.
- `variation_template_slug` must already exist and be active when required.
- Do not auto-create variation templates from CSV.
- `blank = no change` for existing products in upsert mode.
- `__clear__` clears nullable fields where explicitly allowed.
- Same `StoreId + mode + fileHash` cannot be imported again.
- If the same file hash is submitted again, return the existing job instead of enqueueing another task.
- If a job has errors, user fixes CSV and uploads a new file containing only failed SKUs/rows.
- Store row-level errors with column names in `ErrorJson`.
- `image_urls` is a `|` separated list.
- Product import does not download images directly.
- After a product row succeeds, enqueue one `product.media.import` task for that product if `image_urls` has values.
- Media task is per product, not one large media task for the full CSV file.
- Product import row media status only tracks `None` or `Queued` in MVP.
- Media success/failure is checked through ProductMedia/task detail, not copied back to import row in MVP.
- Do not use `AppDbContext`.
- Use `CommerceNodeDbContext` and CommerceNode migrations only.

## Non Goals

- No Excel parsing.
- No dry-run preview.
- No category auto-create.
- No variation template auto-create.
- No variant inventory row generation.
- No `ProductVariant` import in MVP.
- No media download inside product import task.
- No product delete/archive from import.
- No product media deletion from import.
- No product import rollback across the full file.
- No cross-store import.
- No direct ControlPlane database write into CommerceNode data.
- No legacy `BlazorShop.Presentation` changes.

## Current Code Facts

- CommerceNode already has `CommerceTask`, `CommerceTaskWorker`, `ICommerceTaskHandler`, and `ICommerceTaskService`.
- Product media import already uses task type `product.media.import`.
- Product media import API accepts image URLs and queues media work.
- Product has `Sku`, `Name`, `Slug`, `CategoryId`, `Price`, `ComparePrice`, `Quantity`, `ShortDescription`, `FullDescription`, `Description`, `Image`, `StoreId`, and `IsPublished`.
- Product category lookup should be store-scoped.
- Product import should preserve the ControlPlane rule:
  - `BlazorShop.ControlPlane.Web` never calls CommerceNode API directly.
  - `BlazorShop.ControlPlane.API` calls CommerceNode API with node credentials and store scope.

## CSV Format

Header:

```csv
sku,name,slug,category_slug,product_type,variation_template_slug,price,compare_price,quantity,is_published,short_description,description,image_urls
```

Example:

```csv
sku,name,slug,category_slug,product_type,variation_template_slug,price,compare_price,quantity,is_published,short_description,description,image_urls
TSHIRT-001,Classic Tee,classic-tee,t-shirts,CustomVariations,tshirt,19.99,29.99,100,true,Soft cotton tee,Classic print-on-demand shirt,https://cdn.example.com/a.jpg|https://cdn.example.com/b.jpg
```

## CSV Column Rules

| Column | Create rule | Update rule | Notes |
| --- | --- | --- | --- |
| `sku` | Required | Required | Identity key with `StoreId`. |
| `name` | Required | Blank = no change | Trim, max length follows product rules. |
| `slug` | Blank = auto-generate from name | Blank = no change | `__clear__` not allowed. |
| `category_slug` | Optional | Blank = no change | If supplied, must exist. `__clear__` clears category on update. |
| `product_type` | Blank = `Simple` | Blank = no change | Allowed: `Simple`, `VariantInventory`, `CustomVariations`. |
| `variation_template_slug` | Required when `product_type=CustomVariations` | Blank = no change | Must exist and be active. `__clear__` allowed only when product type is not `CustomVariations`. |
| `price` | Required, > 0 | Blank = no change | Decimal. |
| `compare_price` | Optional | Blank = no change | `__clear__` clears. |
| `quantity` | Optional, default 0 | Blank = no change | Integer >= 0. |
| `is_published` | Optional, default false | Blank = no change | Boolean parser should accept `true/false`, `1/0`, `yes/no`. |
| `short_description` | Optional | Blank = no change | `__clear__` clears. |
| `description` | Required | Blank = no change | Missing on create is row error. |
| `image_urls` | Optional | Blank = no change | Split by `|`, max 10 URLs. |

General update semantics:

- In `upsert`, if SKU exists, update only columns with values.
- Blank cell means "do not change" for existing products.
- `__clear__` clears nullable fields where explicitly allowed.
- `__clear__` is not a delete/archive operation.

## Limits

- Max file size: 5 MB.
- Max rows: 1,000.
- Max image URLs per product row: 10.
- Max media tasks queued by one import: 1,000.
- Max image URL length: 1,024.
- CSV must include a header row.
- Unknown columns should be ignored or reported as warnings; required known columns must be validated.

## Database Design

### New table: `ProductImportJobs`

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `Id` | `uuid` | no | Primary key. |
| `PublicId` | `uuid` | no | Public API id. |
| `StoreId` | `uuid` | no | Store scope. |
| `TaskPublicId` | `uuid` | yes | Set after task is queued. |
| `Mode` | `varchar(32)` | no | `create_only` or `upsert`. |
| `Status` | `varchar(64)` | no | Job status. |
| `FileName` | `varchar(260)` | no | Original upload file name. |
| `StoredFilePath` | `text` | no | Node-local stored file path. |
| `FileHash` | `varchar(128)` | no | SHA-256 hash. |
| `FileSizeBytes` | `bigint` | no | Upload size. |
| `TotalRows` | `integer` | no | Total parsed data rows. |
| `CreatedCount` | `integer` | no | Created product count. |
| `UpdatedCount` | `integer` | no | Updated product count. |
| `FailedCount` | `integer` | no | Failed row count. |
| `SkippedCount` | `integer` | no | Skipped row count. |
| `MediaQueuedCount` | `integer` | no | Rows with queued media import tasks. |
| `CreatedBy` | `varchar(256)` | yes | Audit actor. |
| `CreatedAt` | `timestamp with time zone` | no | Default current timestamp. |
| `StartedAt` | `timestamp with time zone` | yes | Set by task handler. |
| `CompletedAt` | `timestamp with time zone` | yes | Set by task handler. |
| `UpdatedAt` | `timestamp with time zone` | no | Updated by service/handler. |

Indexes:

- unique `PublicId`
- unique `(StoreId, Mode, FileHash)`
- `(StoreId, Status, CreatedAt)`
- `TaskPublicId`

### New table: `ProductImportRows`

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `Id` | `uuid` | no | Primary key. |
| `JobId` | `uuid` | no | FK to `ProductImportJobs`. |
| `RowNumber` | `integer` | no | CSV row number, including header offset. |
| `Sku` | `varchar(64)` | yes | Parsed SKU. |
| `Status` | `varchar(64)` | no | Row status. |
| `Action` | `varchar(64)` | no | Created, Updated, Skipped, Failed. |
| `ProductId` | `uuid` | yes | Product affected when successful. |
| `MediaStatus` | `varchar(64)` | no | `None` or `Queued` in MVP. |
| `MediaTaskPublicId` | `uuid` | yes | One media task per product row. |
| `ErrorMessage` | `text` | yes | Short summary. |
| `ErrorJson` | `jsonb` | yes | Column-level errors. |
| `RawDataJson` | `jsonb` | yes | Raw parsed row for audit/debug. |
| `CreatedAt` | `timestamp with time zone` | no | Default current timestamp. |
| `UpdatedAt` | `timestamp with time zone` | no | Updated by handler. |

Indexes:

- unique `(JobId, RowNumber)`
- `(JobId, Status)`
- `(JobId, Sku)`
- `(ProductId)`
- `(MediaTaskPublicId)`

Example `ErrorJson`:

```json
[
  { "column": "name", "message": "Name is required." },
  { "column": "description", "message": "Description is required." },
  { "column": "image_urls", "message": "Image URL must be HTTP or HTTPS." }
]
```

## Status Model

### ProductImportJob.Status

- `Queued`
- `Running`
- `Completed`
- `CompletedWithErrors`
- `Failed`

Rules:

- Worker system failure: job `Failed`, CommerceTask `Failed` or `Dead`.
- Worker completes with zero row errors: job `Completed`, CommerceTask `Succeeded`.
- Worker completes with row errors: job `CompletedWithErrors`, CommerceTask `Succeeded`.
- Duplicate file hash returns existing job.

### ProductImportRow.Status

- `Pending`
- `Succeeded`
- `Failed`
- `Skipped`

### ProductImportRow.Action

- `Created`
- `Updated`
- `Skipped`
- `Failed`

### ProductImportRow.MediaStatus

- `None`
- `Queued`

## Application Contracts and DTOs

Add Product Import DTOs under a CommerceNode catalog/import namespace.

Recommended DTOs:

- `ProductImportMode`
- `ProductImportJobStatus`
- `ProductImportRowStatus`
- `ProductImportRowAction`
- `ProductImportMediaStatus`
- `ProductImportJobDto`
- `ProductImportJobDetailDto`
- `ProductImportRowDto`
- `ProductImportListQuery`
- `ProductImportRowsQuery`
- `ProductImportUploadResponse`
- `ProductImportErrorDto`

Service contracts:

- `IProductImportService`
  - upload/start import;
  - list jobs;
  - get job detail;
  - list row results.
- `IProductImportCsvParser`
  - parse CSV headers and rows;
  - return structured row values/errors.

Task constants:

- `ProductImportTaskTypes.Import = "product.import"`

Task payload:

```json
{
  "schemaVersion": "v1",
  "jobPublicId": "...",
  "storeId": "...",
  "mode": "upsert",
  "storedFilePath": "..."
}
```

Task result:

```json
{
  "jobPublicId": "...",
  "totalRows": 100,
  "created": 60,
  "updated": 20,
  "failed": 20,
  "skipped": 0,
  "mediaQueued": 50
}
```

## CommerceNode API Design

Routes:

| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/api/commerce/admin/products/import` | Upload CSV and queue import. |
| `GET` | `/api/commerce/admin/products/imports` | List import jobs. |
| `GET` | `/api/commerce/admin/products/imports/{jobPublicId}` | Get import job detail. |
| `GET` | `/api/commerce/admin/products/imports/{jobPublicId}/rows` | List row results/errors. |

Upload request:

- multipart form file: `file`
- form/query field: `mode=create_only|upsert`

Response:

- Return `jobPublicId`, `taskPublicId`, status, and counts.
- If duplicate file hash exists, return existing job with message like `Product import already exists for this file.`

Security:

- Use Commerce admin security.
- Store scope must come from existing store context/header/domain mechanism.
- Do not trust store id from form body.

## ControlPlane API and UI Plan

ControlPlane API routes:

| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/api/control-plane/stores/{storePublicId}/products/import` | Proxy upload to CommerceNode. |
| `GET` | `/api/control-plane/stores/{storePublicId}/products/imports` | Proxy job list. |
| `GET` | `/api/control-plane/stores/{storePublicId}/products/imports/{jobPublicId}` | Proxy job detail. |
| `GET` | `/api/control-plane/stores/{storePublicId}/products/imports/{jobPublicId}/rows` | Proxy row result list. |

ControlPlane Web UI:

- Store-scoped product import page/panel.
- Upload CSV file.
- Select mode:
  - `create_only`
  - `upsert`
- Show CSV column template.
- Show limits.
- Submit import.
- Show job status/counts.
- Show row errors with:
  - row number;
  - SKU;
  - status/action;
  - error columns;
  - error message.
- Show media queued count.

Do not expose CommerceNode URL, node key, or node secret to browser.

## Product Import Processing Algorithm

1. Upload endpoint validates file extension, content type where possible, size, and mode.
2. Store file under node-owned storage path:
   - `/commerce-node/imports/products/{jobPublicId}/source.csv`
3. Compute SHA-256 file hash.
4. Check duplicate `(StoreId, Mode, FileHash)`.
5. If duplicate exists, return existing job.
6. Create `ProductImportJob` with status `Queued`.
7. Enqueue `product.import` task with idempotency key:
   - `product-import:{storeId}:{mode}:{fileHash}`
8. Worker marks job `Running`.
9. Worker parses CSV.
10. Worker validates row-level fields.
11. Worker applies `create_only` or `upsert`.
12. Worker writes one `ProductImportRow` per data row.
13. Worker queues product media import task per successful row with `image_urls`.
14. Worker updates counts and job status.
15. Worker returns summary in `CommerceTask.ResultJson`.

## Row Validation Rules

Validation should return column-level errors.

Required on create:

- `sku`
- `name`
- `description`
- `price`

Additional validation:

- `price > 0`
- `compare_price` must be empty, `__clear__`, or `>= 0`
- `quantity >= 0`
- `product_type` must be one of `Simple`, `VariantInventory`, `CustomVariations`
- `category_slug` must exist when supplied
- `variation_template_slug` required for `CustomVariations`
- `variation_template_slug` must exist and be active when supplied
- image URL count <= 10
- image URLs must be HTTP/HTTPS absolute URLs
- image URL length <= 1,024

Create-only behavior:

- If SKU exists in store, row fails.

Upsert behavior:

- If SKU exists, update only provided values.
- If SKU does not exist, create with create-required validations.

## Media Task Enqueue

For each successful product row with `image_urls`:

- Split by `|`.
- Normalize/trim URLs.
- Create one `product.media.import` task for that product.
- Use existing ProductMedia import request/payload shape where possible.
- Use idempotency key:
  - `product-import-media:{storeId}:{jobPublicId}:{rowNumber}:{sku}`
- Set row `MediaStatus=Queued`.
- Set row `MediaTaskPublicId`.

If image URL syntax validation fails before product write:

- row fails with `image_urls` column error.

If media task later fails:

- Product import row remains media queued.
- User checks media task/media page for image failures.
- No callback into ProductImportRows in MVP.

## Phase 1 - Product Import Schema

Tasks:

- Add `ProductImportJob` entity.
- Add `ProductImportRow` entity.
- Add `DbSet` entries to `CommerceNodeDbContext`.
- Configure indexes, jsonb fields, lengths, and relationships.
- Add CommerceNode migration.

Acceptance:

- Migration applies on clean CommerceNode DB.
- Unique `(StoreId, Mode, FileHash)` exists.
- `ErrorJson` and `RawDataJson` use `jsonb`.

## Phase 2 - DTOs, Parser, and Service Contract

Tasks:

- Add import DTOs/status constants.
- Add `IProductImportService`.
- Add `IProductImportCsvParser`.
- Implement CSV parser with header validation and row parsing.
- Add import storage options:
  - root path;
  - max file size;
  - max rows.

Acceptance:

- CSV parser handles quoted fields.
- CSV parser reports missing required headers.
- CSV parser limits row count.

## Phase 3 - CommerceNode Upload API

Tasks:

- Add `CommerceProductImportsController`.
- Implement multipart upload.
- Validate file size and extension.
- Store source file.
- Compute file hash.
- Return existing job for duplicate hash.
- Create job and enqueue `product.import` task.

Acceptance:

- Upload returns API response envelope.
- Duplicate same file returns same job, no new task.
- Invalid mode/file returns validation failure.

## Phase 4 - Product Import Task Handler

Tasks:

- Add `ProductImportTaskHandler`.
- Register as `ICommerceTaskHandler`.
- Load job by public id.
- Mark job running.
- Parse stored CSV.
- Process each row.
- Create/update product.
- Store row result and column errors.
- Enqueue media tasks.
- Update job counts/status.
- Return task summary.

Acceptance:

- Partial success works.
- Row errors do not rollback successful rows.
- Task succeeds when worker completes with row errors.
- Task fails only for system-level/import-level failures.

## Phase 5 - Product Field Integration

Tasks:

- Implement SKU lookup by `StoreId + Sku`.
- Implement category slug lookup by current store.
- Implement variation template slug lookup by current store.
- Implement product type application.
- Implement blank/no-change and `__clear__` semantics.
- Keep `ProductVariant` path untouched.

Acceptance:

- `create_only` rejects duplicate SKU.
- `upsert` updates only supplied columns.
- `__clear__` clears allowed nullable fields.
- `CustomVariations` requires active template.
- `VariantInventory` does not create variant rows.

## Phase 6 - Media Task Integration

Tasks:

- Map `image_urls` into existing product media import request/payload.
- Enqueue one media task per successful product row.
- Set row media status/task id.
- Do not wait for media task completion.

Acceptance:

- Product import completes even when media task is still pending.
- Row shows `MediaStatus=Queued`.
- Media task can be inspected through existing task/media tooling.

## Phase 7 - ControlPlane Proxy and UI

Tasks:

- Add ControlPlane API proxy upload/list/detail/rows routes.
- Add ControlPlane Web import UI.
- Show template CSV header.
- Show limits.
- Show mode selector.
- Show job status/counts.
- Show row error table.

Acceptance:

- ControlPlane Web calls only ControlPlane API.
- ControlPlane API calls CommerceNode API.
- Node credentials never reach browser.

## Phase 8 - QA Checklist

Add cases to `QA-CommerceNode.todo.md`:

- Upload valid CSV in `create_only` mode.
- Upload valid CSV in `upsert` mode.
- Duplicate same file hash returns existing job.
- Same file cannot be imported again.
- Missing required header returns validation failure.
- Missing SKU row returns `sku` column error.
- Missing name on create returns `name` column error.
- Missing description on create returns `description` column error.
- Missing price on create returns `price` column error.
- Duplicate SKU in `create_only` returns row error.
- `upsert` blank cells do not overwrite existing values.
- `__clear__` clears allowed nullable fields.
- Missing `category_slug` leaves category unchanged on update.
- Unknown `category_slug` returns row error.
- `CustomVariations` without `variation_template_slug` returns row error.
- Unknown/inactive `variation_template_slug` returns row error.
- Valid `variation_template_slug` sets product template reference.
- `VariantInventory` import does not create `ProductVariant` rows.
- `image_urls` with more than 10 URLs returns row error.
- Valid `image_urls` queues one media task per product row.
- Product import completes with `CompletedWithErrors` when some rows fail.
- CommerceTask result contains summary counts.
- ProductImportRows contain `ErrorJson` with column names.
- ControlPlane upload UI uses ControlPlane API only.

## Deferred

- Excel import.
- Dry-run preview.
- Import template download with sample data.
- Retry failed rows from same job.
- Media callback into ProductImportRows.
- Product import rollback.
- Product archive/delete through CSV.
- Category auto-create.
- Variation template auto-create.
- Variant inventory CSV import.
- Product SEO bulk import.
- Import performance batching beyond 1,000 rows.
