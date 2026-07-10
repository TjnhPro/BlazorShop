# BlazorShop CommerceNode Product Media Todo

Status: draft
Created: 2026-07-10
Scope: Product media pipeline for CommerceNode, ControlPlane catalog UI, StorefrontV2 image delivery, imgproxy, and review/fix QA gates.

## Goal

Build a Product Media foundation for V2 catalog without turning the MVP into a full media library.

The first target is product images:

- ControlPlane user can attach one or more source image URLs to a product.
- CommerceNode stores the original image in node-owned storage.
- CommerceNode processes media asynchronously through the existing CommerceTaskWorker pattern.
- StorefrontV2 serves product images through a stable public URL.
- imgproxy performs resize/format conversion on demand.
- Each store sees only its own media through its own Storefront domain.

This is an extension of CommerceNode catalog. It is not a rewrite of catalog, StorefrontV2, or ControlPlane.

## Locked Decisions

- Product media is a real extension and gets its own `product_media` table.
- Do not add media tables to ControlPlane DB.
- Do not use `AppDbContext`.
- Use `CommerceNodeDbContext` and CommerceNode migrations only.
- Keep `Product.Image` for backward compatibility with current StorefrontV2 and catalog DTOs.
- Update `Product.Image` to the primary ProductMedia public URL after media import succeeds.
- Store the original file in CommerceNode storage before imgproxy serves it.
- Use imgproxy for resize, webp conversion, and lightweight image processing.
- Do not pre-generate fixed image sizes in MVP.
- Public media id is a GUID `ProductMedia.PublicId`.
- Public URL shape is:
  - `/media/products/{mediaId}?w=600&h=600&fit=contain&format=webp&v=1`
- If only `w` or only `h` is supplied, resize proportionally by the supplied max dimension.
- If no `w` or `h` is supplied, default to max `1000`.
- Maximum accepted `w` and `h` is `2000`.
- Supported `fit` values in MVP: `contain`, `cover`, `max`.
- Default `fit` is `contain`.
- Supported `format` values in MVP: `webp`, `jpg`, `png`.
- Default `format` is `webp`.
- `v` maps to `ProductMedia.Version`; increment it when the original image changes.
- Do not use signed public URLs for Storefront product images in MVP.
- Google indexing must be possible through normal public product/media URLs.
- Store A media must not resolve from Store B domain.
- `BlazorShop.ControlPlane.Web` never calls `BlazorShop.CommerceNode.API` directly.
- `BlazorShop.ControlPlane.Web` only calls `BlazorShop.ControlPlane.API`.
- `BlazorShop.ControlPlane.API` calls `BlazorShop.CommerceNode.API` with node credentials and store scope.
- Keep using existing API response envelope patterns.
- Reuse existing CommerceTaskWorker and task handler pattern before adding a separate product worker process.
- A separate product worker service can be extracted later if media/product import grows.

## Non Goals

- No full global media library.
- No cross-store media sharing.
- No category/store/blog media in this round.
- No product variant-specific media in this round.
- No CSV/XLS product import in this round.
- No manual drag-and-drop media manager in this round.
- No signed public media URLs in this round.
- No S3/MinIO requirement in MVP.
- No AI image processing.
- No refactor of legacy `BlazorShop.Presentation`.
- No ControlPlane direct database access to CommerceNode data.

## Current Code Facts

- CommerceNode catalog admin APIs live under `api/commerce/admin/*`.
- Storefront private APIs live under `api/internal/*`.
- ControlPlane catalog page exists at `BlazorShop.ControlPlane.Web/Pages/Catalog.razor`.
- ControlPlane catalog client calls only ControlPlane API routes:
  - `api/control-plane/stores/{storePublicId}/catalog/*`
- ControlPlane API proxies catalog calls through `ControlPlaneCommerceCatalogService`.
- CommerceNode already has `CommerceTaskWorker` and task handlers.
- Existing task type pattern example: `store.create_and_deploy`.
- Existing upload endpoint exists at `api/commerce/admin/media/images`.
- Existing upload endpoint stores files under `/uploads` and returns a URL.
- `Product.Image` is currently the single image URL used by StorefrontV2.
- StorefrontV2 product card/detail/cart and structured data read `Product.Image`.
- `compose.commercenode.yml` already has PostgreSQL, Nginx, shared network, and Nginx cache volume.
- The current design must preserve StorefrontV2 compatibility by keeping a primary image string available.

## Target Architecture

```text
ControlPlane.Web
  -> ControlPlane.API
      -> CommerceNode API: api/commerce/admin/products/{productId}/media/*
          -> CommerceNodeDbContext.product_media
          -> commerce_task(type = product.media.import)
          -> CommerceTaskWorker
              -> ProductMediaImportTaskHandler
                  -> download source image
                  -> validate image
                  -> store original in CommerceNode storage
                  -> update product_media status/metadata
                  -> update Product.Image when primary media is ready

StorefrontV2
  -> renders Product.Image
  -> /media/products/{mediaId}?w=...&h=...&fit=...&format=...&v=...
      -> Nginx cache
      -> CommerceNode media resolver
      -> imgproxy
      -> original file from CommerceNode storage
```

ControlPlane owns authorization, node credential usage, and audit for admin actions.
CommerceNode owns product data, media data, media storage, task execution, and Storefront image delivery.

## Database Design

### New table: `product_media`

Purpose: store product image metadata, processing status, and the original file location.

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `id` | `uuid` | no | Primary key. Internal DB id. |
| `public_id` | `uuid` | no | Public media id used in URLs. |
| `store_id` | `uuid` | no | Store scope. Must match product store. |
| `product_id` | `uuid` | no | FK to `Products.Id`. |
| `original_source_url` | `text` | yes | Original URL submitted by admin/import. |
| `original_storage_path` | `text` | yes | Node-local path/key for stored original. |
| `content_hash` | `text` | yes | SHA-256 or equivalent hash of stored original. |
| `file_name` | `text` | yes | Normalized file name for debugging. |
| `mime_type` | `text` | yes | Validated MIME type. |
| `width` | `integer` | yes | Original width after validation. |
| `height` | `integer` | yes | Original height after validation. |
| `file_size_bytes` | `bigint` | yes | Stored original size. |
| `sort_order` | `integer` | no | Gallery order. Default `0`. |
| `is_primary` | `boolean` | no | Primary product image. Default `false`. |
| `alt_text` | `text` | yes | Optional image alt text. |
| `status` | `text` | no | `pending`, `downloading`, `stored`, `failed`, `deleted`. |
| `error_message` | `text` | yes | Safe processing error. |
| `version` | `integer` | no | Cache-busting version. Default `1`. |
| `created_at` | `timestamp with time zone` | no | Created timestamp. |
| `updated_at` | `timestamp with time zone` | no | Updated timestamp. |
| `processed_at` | `timestamp with time zone` | yes | Successful processing timestamp. |
| `deleted_at` | `timestamp with time zone` | yes | Soft delete timestamp. |

### Entity placement

- Add `ProductMedia` under `BlazorShop.Domain.Entities.CommerceNode`.
- Add `ProductMediaStatuses` under the same namespace.
- Add `DbSet<ProductMedia>` to `CommerceNodeDbContext`.
- Do not add this entity to `AppDbContext`.
- Do not add this entity to ControlPlane context.

### Relationships

- `ProductMedia.ProductId` references `Products.Id`.
- Hard delete of a product may cascade to media rows.
- Normal product archive should not delete media rows.
- `ProductMedia.StoreId` is required and must match the product `StoreId`.
- Enforce store/product match in service validation because existing `Product.StoreId` is nullable legacy-compatible state.
- If a reliable FK to `commerce_store(id)` is already possible in the current model snapshot, add it. If it causes migration risk, keep the required `store_id` column and service-level validation in MVP.

### Indexes

- unique `public_id`
- index `(store_id, product_id, sort_order)`
- index `(store_id, product_id, status)`
- index `(store_id, product_id, is_primary)` filtered by `deleted_at is null and is_primary = true`
- index `(store_id, content_hash)` where `content_hash is not null`
- index `status`
- index `deleted_at`

### Constraints

- `status in ('pending', 'downloading', 'stored', 'failed', 'deleted')`
- `sort_order >= 0`
- `version >= 1`
- `width is null or width > 0`
- `height is null or height > 0`
- `file_size_bytes is null or file_size_bytes > 0`

### Product compatibility rule

`Products.Image` remains the compatibility field used by current DTOs and StorefrontV2.

When a ProductMedia row becomes primary and `status = stored`, set:

```text
Product.Image = /media/products/{public_id}?w=1000&fit=contain&format=webp&v={version}
```

If the primary media is deleted:

- choose the next stored media by `sort_order` as primary; or
- set `Product.Image = null` if no stored media remains.

## Storage Design

### MVP storage

Use CommerceNode local storage mounted as a Docker volume.

Recommended runtime path:

```text
BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/runtime/media
```

Recommended container path:

```text
/var/blazorshop/media
```

Recommended original storage layout:

```text
/var/blazorshop/media/stores/{storeId}/products/{productId}/{mediaPublicId}/original.{ext}
```

Rules:

- Never derive final paths from untrusted file names.
- Use `storeId`, `productId`, and `mediaPublicId` only after DB validation.
- Keep submitted file name only as metadata.
- Store only validated image content.
- Use a temp download path first, then atomic move into the final media path.

### Future storage

MinIO/S3-compatible storage can be added later behind a storage abstraction.
Do not require MinIO in this MVP because local Docker volume is enough to validate the workflow.

## imgproxy and Nginx Design

### Compose additions

Extend `compose.commercenode.yml` later with:

- `commercenode-imgproxy`
- shared `commercenode_media_data` volume
- internal-only imgproxy port
- existing `blazorshop-commercenode` network

Nginx remains the public edge for Storefront and media.

### Public media resolver

Add public Storefront-safe route:

```http
GET /media/products/{mediaPublicId}?w=600&h=600&fit=cover&format=webp&v=1
```

Resolver responsibilities:

- Resolve store from request host using existing store/domain resolution pattern.
- Load `ProductMedia` by `public_id`.
- Require matching `store_id`.
- Require `status = stored`.
- Require `deleted_at is null`.
- Validate query params.
- Build imgproxy request from the stored original path.
- Return or proxy the optimized image response.

MVP implementation may proxy imgproxy through CommerceNode API to keep DB validation simple.
Nginx should cache the final media response by host + URI + query string.

Later optimization can move to internal redirects or Nginx-level acceleration after the behavior is verified.

### Query parameter rules

| Parameter | Default | Rule |
| --- | --- | --- |
| `w` | `1000` when both dimensions are missing | Optional integer, `1..2000`. |
| `h` | none | Optional integer, `1..2000`. |
| `fit` | `contain` | Allow `contain`, `cover`, `max`. |
| `format` | `webp` | Allow `webp`, `jpg`, `png`. |
| `v` | media current version | Must match or be ignored only for cache key. |

If only `w` is present, resize by width and preserve aspect ratio.
If only `h` is present, resize by height and preserve aspect ratio.
If both are missing, use default max width/size `1000`.
Never return the original unbounded file through public URLs.

### Cache headers

For stored media responses:

```text
Cache-Control: public, max-age=31536000, immutable
ETag: "{mediaPublicId}:{w}:{h}:{fit}:{format}:{version}"
```

When replacing original media, increment `version` to change the URL and break cache.

## Task Design

### New task type

```text
product.media.import
```

Use existing `commerce_task` and `CommerceTaskWorker`.

Recommended task payload:

```json
{
  "schemaVersion": "v1",
  "storeId": "00000000-0000-0000-0000-000000000000",
  "productId": "00000000-0000-0000-0000-000000000000",
  "items": [
    {
      "mediaPublicId": "00000000-0000-0000-0000-000000000000",
      "sourceUrl": "https://example.com/image.jpg",
      "sortOrder": 0,
      "isPrimary": true,
      "altText": "Product image"
    }
  ],
  "requestedBy": "control-plane",
  "correlationId": "optional"
}
```

Recommended idempotency key:

```text
product-media-import:{storeId}:{productId}:{hash-of-source-url-list}
```

Recommended lock key:

```text
product:{productId}:media
```

### Handler behavior

Add `ProductMediaImportTaskHandler : ICommerceTaskHandler`.

Handler steps:

1. Validate store exists and product belongs to store.
2. Validate media rows exist and belong to the product/store.
3. For each pending item:
   - set status `downloading`;
   - download source URL through an `HttpClient`;
   - block private IP, loopback, localhost, file paths, and unsupported schemes;
   - enforce max download size;
   - validate MIME type and image signature;
   - store original under CommerceNode media storage;
   - compute content hash;
   - read width/height metadata;
   - set status `stored`;
   - clear error message;
   - set `processed_at`.
4. Set exactly one primary image when at least one item is stored.
5. Update `Product.Image` to primary media public URL.
6. Write per-item results to `commerce_task.result_json`.

### Failure behavior

- Item-level failures should mark the media row as `failed` with a safe message.
- Task should succeed with result details if the handler completed and recorded per-item failures.
- Task should fail/retry only for infrastructure failures that prevent processing as a whole.
- Do not roll back the product when media import fails.
- Do not delete successfully stored originals because a later item failed.

## API Design

### CommerceNode Admin API

Add controller under `BlazorShop.CommerceNode.API`:

```text
CommerceProductMediaController
Route: api/commerce/admin/products/{productId}/media
```

Endpoints:

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `api/commerce/admin/products/{productId}/media` | List product media. |
| `POST` | `api/commerce/admin/products/{productId}/media/import` | Create media rows and enqueue `product.media.import`. |
| `PUT` | `api/commerce/admin/products/{productId}/media/order` | Update sort order. |
| `POST` | `api/commerce/admin/products/{productId}/media/{mediaPublicId}/primary` | Set primary media. |
| `DELETE` | `api/commerce/admin/products/{productId}/media/{mediaPublicId}` | Soft delete media. |
| `POST` | `api/commerce/admin/products/{productId}/media/{mediaPublicId}/retry` | Retry failed media by enqueueing a task. |

Recommended request/response DTOs:

```csharp
public sealed record ImportProductMediaRequest(
    IReadOnlyList<ImportProductMediaItem> Items);

public sealed record ImportProductMediaItem(
    string SourceUrl,
    int SortOrder = 0,
    bool IsPrimary = false,
    string? AltText = null);

public sealed record ProductMediaDto(
    Guid PublicId,
    Guid StoreId,
    Guid ProductId,
    string? OriginalSourceUrl,
    string? PublicUrl,
    string Status,
    string? ErrorMessage,
    int SortOrder,
    bool IsPrimary,
    string? AltText,
    int Version,
    int? Width,
    int? Height,
    long? FileSizeBytes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ProcessedAt);

public sealed record ImportProductMediaResponse(
    Guid TaskPublicId,
    IReadOnlyList<ProductMediaDto> Items);
```

### CommerceNode public media route

Add public route outside admin API:

```text
GET /media/products/{mediaPublicId}
```

This route is public but store-scoped by host/domain.
It must not use ControlPlane credentials.
It must not allow cross-store media reads.

### ControlPlane API gateway

Add gateway endpoints under:

```text
api/control-plane/stores/{storePublicId}/catalog/products/{productId}/media
```

ControlPlane API responsibilities:

- Check ControlPlane user permission.
- Resolve `storePublicId` to CommerceNode and store scope.
- Forward to CommerceNode using node key/secret/IP security.
- Preserve API response envelope.
- Return CommerceNode message to Web for display.
- Audit media import, delete, primary change, order change, and retry actions.

### ControlPlane Web client

Extend `IControlPlaneCatalogClient` and `ControlPlaneCatalogClient` with media methods.

ControlPlane Web must still call only:

```text
api/control-plane/stores/{storePublicId}/catalog/...
```

Do not put CommerceNode base URLs, node key, or node secret in Web client code.

## UI Design

### Catalog product form

Replace the simple `Image URL` input with a media section, but keep a compatibility path during migration.

MVP UI:

- Product form still shows current primary image URL for visibility.
- Add `Media source URLs` textarea.
- One URL per line.
- Add optional `Alt text` for the first/primary image if simple enough.
- Submit product first, then import media for the saved product.
- Show task id/status after media import starts.

### Product detail media panel

When a product is selected:

- Load media list.
- Show thumbnail using public media URL with `w=96&h=96&fit=cover&format=webp`.
- Show status badge: pending, downloading, stored, failed, deleted.
- Show primary marker.
- Show sort order.
- Show error message for failed rows.
- Actions:
  - set primary;
  - retry failed;
  - soft delete;
  - move up/down or edit sort order.

### User feedback

Use existing API response pattern:

- UI reads `Success`.
- UI displays `Message`.
- UI uses `Data` only when `Success = true`.
- Validation and task enqueue errors must come from API, not duplicated in UI.

### StorefrontV2

No large UI refactor is required.

StorefrontV2 continues to render `Product.Image`.
After media import succeeds, `Product.Image` points to `/media/products/{mediaPublicId}?...`.

If needed, add a small helper later to normalize relative media URLs against the current Storefront host.

## Phase Plan

### Phase 0 - Review Baseline

- [ ] Re-read existing catalog product DTOs, product services, and ControlPlane catalog gateway.
- [ ] Re-read `CommerceTaskWorker` and `StoreCreateAndDeployTaskHandler` before implementing media task handler.
- [ ] Re-read current Nginx and compose setup.
- [ ] Confirm max source image size for MVP.
- [ ] Confirm local storage root env/config name.
- [ ] Confirm whether `ProductBase.Image` remains required during the first implementation slice.

Review/fix gate:

- [ ] Confirm plan still preserves `ControlPlane.Web -> ControlPlane.API -> CommerceNode.API`.
- [ ] Confirm no direct ControlPlane Web call to CommerceNode is introduced.

### Phase 1 - Database Schema

- [x] Add `ProductMedia` entity.
- [x] Add `ProductMediaStatuses`.
- [x] Add `DbSet<ProductMedia>` to `CommerceNodeDbContext`.
- [x] Configure `product_media` table with snake_case columns.
- [x] Add indexes and constraints listed above.
- [x] Add CommerceNode EF migration.
- [x] Verify migration applies against clean CommerceNode PostgreSQL on port `5434`.

Review/fix gate:

- [x] Check migration does not touch ControlPlane DB.
- [x] Check migration does not touch `AppDbContext`.
- [x] Check unique primary-media behavior is enforced by service or filtered index.

### Phase 2 - Application Contracts and Services

- [ ] Add ProductMedia DTOs in Application layer.
- [ ] Add `IProductMediaService`.
- [ ] Implement service in Infrastructure CommerceNode layer.
- [ ] Add methods for list, create pending import rows, set primary, update order, soft delete, retry failed.
- [ ] Add media public URL builder.
- [ ] Update primary media logic to sync `Product.Image`.

Review/fix gate:

- [ ] Ensure service validates store/product match.
- [ ] Ensure all queries are store-scoped.
- [ ] Ensure deleted media is excluded by default.

### Phase 3 - Storage and Download Pipeline

- [ ] Add ProductMedia storage options.
- [ ] Add local file storage implementation for original images.
- [ ] Add safe image downloader.
- [ ] Block unsupported schemes.
- [ ] Block private IPs, loopback, localhost, and link-local targets.
- [ ] Enforce max download size.
- [ ] Reuse or extend existing image signature validation.
- [ ] Capture MIME type, width, height, file size, and content hash.

Review/fix gate:

- [ ] Review SSRF defenses.
- [ ] Review path traversal defenses.
- [ ] Review memory usage for large downloads.
- [ ] Review temp file cleanup after failures.

### Phase 4 - Product Media Task Handler

- [ ] Add task type constant `product.media.import`.
- [ ] Add `ProductMediaImportTaskHandler`.
- [ ] Register handler in CommerceNode API DI.
- [ ] Build task payload parser.
- [ ] Process media items one by one with per-item status updates.
- [ ] Record task result JSON with successes and failures.
- [ ] Update `Product.Image` when primary media is stored.

Review/fix gate:

- [ ] Verify partial item failure does not roll back successful items.
- [ ] Verify retry behavior only retries infrastructure-level failures.
- [ ] Verify idempotency prevents duplicate media import tasks.

### Phase 5 - CommerceNode APIs

- [ ] Add `CommerceProductMediaController`.
- [ ] Add list/import/order/primary/delete/retry endpoints.
- [ ] Add public `GET /media/products/{mediaPublicId}` resolver.
- [ ] Validate resize query params.
- [ ] Return envelope responses for admin endpoints.
- [ ] Return correct image response/cache headers for public media endpoint.

Review/fix gate:

- [ ] Verify admin APIs require existing Commerce admin security.
- [ ] Verify public media route is host/store scoped.
- [ ] Verify Store A cannot read Store B media by guessing GUID.

### Phase 6 - Docker, Nginx, and imgproxy

- [ ] Extend `compose.commercenode.yml` with `commercenode-imgproxy`.
- [ ] Add persistent `commercenode_media_data` volume.
- [ ] Mount media volume into CommerceNode API and imgproxy as read-only where possible.
- [ ] Add Nginx cache route for `/media/products/*`.
- [ ] Keep imgproxy internal to CommerceNode network.
- [ ] Document local test URLs.

Review/fix gate:

- [ ] Verify Nginx cache key includes host and query string.
- [ ] Verify imgproxy cannot fetch arbitrary public URLs directly in MVP.
- [ ] Verify media files survive container restart.

### Phase 7 - ControlPlane API Gateway

- [ ] Extend `IControlPlaneCommerceCatalogService` with product media methods.
- [ ] Extend `ControlPlaneCommerceCatalogService` forwarding methods.
- [ ] Add ControlPlane API routes under `api/control-plane/stores/{storePublicId}/catalog/products/{productId}/media`.
- [ ] Reuse existing node endpoint resolution and credential injection.
- [ ] Add audit entries for media actions.
- [ ] Preserve response envelope and message propagation.

Review/fix gate:

- [ ] Verify ControlPlane Web never receives CommerceNode credentials.
- [ ] Verify permission checks match existing catalog admin pattern.
- [ ] Verify errors from CommerceNode are shown through ControlPlane response message.

### Phase 8 - ControlPlane Web UI

- [ ] Extend catalog client with media methods.
- [ ] Add media URLs textarea to product editor.
- [ ] Add media panel for selected product.
- [ ] Add thumbnail/status/primary/order/delete/retry controls.
- [ ] Display task id/status after import starts.
- [ ] Keep current product list stable while media is processing.

Review/fix gate:

- [ ] Verify UI only checks `Success` and displays `Message`.
- [ ] Verify no CommerceNode URL/secret appears in Web code.
- [ ] Verify text and controls fit the current Tailwind/FontAwesome style.

### Phase 9 - StorefrontV2 Compatibility

- [ ] Verify product card renders ProductMedia URL from `Product.Image`.
- [ ] Verify product detail renders ProductMedia URL.
- [ ] Verify cart line image renders ProductMedia URL.
- [ ] Verify structured data uses full absolute image URL when needed.
- [ ] Add helper only if relative `/media/products/*` URLs do not resolve correctly per Storefront host.

Review/fix gate:

- [ ] Verify StorefrontV2 does not call ControlPlane for media.
- [ ] Verify StorefrontV2 does not need node credentials for public images.
- [ ] Verify default image size does not return original full-size image.

### Phase 10 - QA and Documentation

- [ ] Add ProductMedia cases to `QA-CommerceNode.todo.md`.
- [ ] Add ControlPlane catalog media cases to `QA-ControlPlane.todo.md`.
- [ ] Add Storefront image rendering cases to `QA-StorefrontV2.todo.md`.
- [ ] Add architecture note under `docs/architecture` if implementation changes runtime topology.
- [ ] Update `AGENTS.md` if a new hard rule is introduced.

Review/fix gate:

- [ ] Run API QA against clean CommerceNode DB.
- [ ] Run ControlPlane UI QA with Playwright.
- [ ] Run StorefrontV2 UI QA with Playwright.
- [ ] Fix discovered bugs immediately in the same phase.
- [ ] Mark checklist items only after verification.

## QA Checklist Seeds

Add these cases when implementation starts:

### CommerceNode

- [ ] Import one valid image URL.
- [ ] Import multiple valid image URLs.
- [ ] Import invalid URL.
- [ ] Import unsupported image type.
- [ ] Import oversized image.
- [ ] Block localhost/private IP source URL.
- [ ] Verify pending/downloading/stored/failed statuses.
- [ ] Verify failed item records safe error message.
- [ ] Verify primary media updates `Product.Image`.
- [ ] Verify deleting primary chooses next media or clears image.
- [ ] Verify retry failed media.
- [ ] Verify public media URL returns webp by default.
- [ ] Verify `w` only preserves aspect ratio.
- [ ] Verify `h` only preserves aspect ratio.
- [ ] Verify max dimension rejects or clamps above `2000`.
- [ ] Verify no-dimension request defaults to `1000`.
- [ ] Verify `fit=cover`, `fit=contain`, `fit=max`.
- [ ] Verify version `v` changes cache key.
- [ ] Verify Store A cannot access Store B media.

### ControlPlane

- [ ] Product media import starts from Catalog page.
- [ ] Import task id/status is visible.
- [ ] API error message is shown from response message.
- [ ] Media list loads through ControlPlane API only.
- [ ] Set primary works.
- [ ] Delete media works.
- [ ] Retry failed media works.
- [ ] No direct CommerceNode URL or credential appears in browser network calls.

### StorefrontV2

- [ ] Product listing displays primary media image.
- [ ] Product detail displays primary media image.
- [ ] Cart displays product media image.
- [ ] Image URL resolves on the current Storefront host.
- [ ] Image URL does not leak media from another store domain.
- [ ] Structured data image remains valid.

## Review and Fix Checklist

Use this checklist after each implementation phase:

- [ ] Does this phase preserve Layered Architecture?
- [ ] Did it reuse existing service/repository/response/task patterns?
- [ ] Did it avoid touching legacy `BlazorShop.Presentation`?
- [ ] Did it avoid `AppDbContext`?
- [ ] Did it keep ControlPlane Web behind ControlPlane API?
- [ ] Are all CommerceNode queries store-scoped?
- [ ] Are error messages safe for UI display?
- [ ] Are task failures retryable only when retry makes sense?
- [ ] Are file paths generated from trusted IDs only?
- [ ] Are public media URLs safe for Google indexing?
- [ ] Are cache keys separated by host/store and query params?

## Open Questions Before Implementation

- What exact max source image size should MVP accept: 5MB, 10MB, or 20MB?
- Should `ProductBase.Image` stop being `[Required]` in the same media phase, or remain required until async product create is added?
- Should the first UI slice import media only after product save, or should product create itself become an async task now?
- Should the public media resolver proxy bytes through CommerceNode API in MVP, or use internal redirect after DB validation?
- Should category media be planned immediately after product media, or remain deferred until product media is stable?

## Suggested Implementation Order

Implement in small commits:

1. Database and entity.
2. ProductMedia service and URL builder.
3. Storage/downloader validation.
4. Task handler.
5. CommerceNode API and public resolver.
6. compose/Nginx/imgproxy.
7. ControlPlane API gateway.
8. ControlPlane Web UI.
9. StorefrontV2 verification fixes.
10. QA checklist updates and final bug fixes.

Do not start UI before CommerceNode API and media resolver are testable through direct API calls.
