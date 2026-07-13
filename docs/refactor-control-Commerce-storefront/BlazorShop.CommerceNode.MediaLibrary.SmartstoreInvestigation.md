# Commerce Node Media Library Smartstore Investigation

Status: investigation
Date: 2026-07-13
Scope: Smartstore media library research for a future BlazorShop Commerce Node Media Library expansion.

## Investigation Hypothesis

BlazorShop should not replace the current ProductMedia MVP. The right expansion path is to extract a reusable, store-scoped Commerce Media asset layer under `CommerceNodeDbContext`, then adapt ProductMedia to reference that asset layer over time.

Reason: Smartstore treats product pictures as one consumer of a generic media library. BlazorShop currently stores product-specific media rows directly in `product_media`, but it already has secure download, async import, public URL rendering, imgproxy integration, primary image selection, and catalog cache invalidation.

## Smartstore Evidence

Smartstore media is a generic asset system:

- `MediaFile` stores metadata: folder, name, alt, title, extension, MIME type, media type, size, pixel size, width, height, metadata JSON, admin comment, transient flag, soft-delete flag, hidden flag, storage pointer, tags, tracks, and product mappings.
- `MediaFolder` gives assets a tree, slug, metadata, file count, and parent/child structure.
- `MediaAlbum` extends folder with system album behavior and URL path inclusion.
- `MediaTag` supports many-to-many tagging.
- `MediaTrack` records references from entities to media by album, entity name, entity id, and property. This is the main safety mechanism behind orphan detection and delete protection.
- `MediaStorage` exists for database-backed binary storage. Smartstore also has a file-system provider through `IMediaStorageProvider`.
- `IMediaService` is the central API for count, search, get by path/id/name, uniqueness checks, save, delete, copy, move, replace, reprocess, batch save, folder operations, conversion, and URL generation.
- Web API exposes first-class `MediaFiles` and `MediaFolders` endpoints for search, count, uniqueness, save, move, copy, delete, and folder operations.
- `ProductMediaFile` is only a product-to-media mapping with display order. Product main image maintenance happens in hooks, not by embedding all asset details in product media rows.
- `TransientMediaClearTask` deletes old transient uploads that were never assigned.
- `MediaSettings` includes upload limits, media type extension lists, thumbnail sizes, image processing defaults, response cache policy, and optional URL versioning.

Key source references:

- `Smartstore/dev-docs/framework/content/media-system-and-imaging.md`
- `Smartstore/src/Smartstore.Core/Content/Media/Domain/MediaFile.cs`
- `Smartstore/src/Smartstore.Core/Content/Media/Domain/MediaFolder.cs`
- `Smartstore/src/Smartstore.Core/Content/Media/Domain/MediaTrack.cs`
- `Smartstore/src/Smartstore.Core/Content/Media/IMediaService.cs`
- `Smartstore/src/Smartstore.Modules/Smartstore.WebApi/Controllers/Content/MediaFilesController.cs`
- `Smartstore/src/Smartstore.Modules/Smartstore.WebApi/Controllers/Content/MediaFoldersController.cs`
- `Smartstore/src/Smartstore.Core/Catalog/Products/Domain/ProductMediaFile.cs`
- `Smartstore/src/Smartstore.Core/Catalog/Products/Hooks/ProductMediaFileHook.cs`

## BlazorShop Current State

Current ProductMedia is narrower but already production-shaped for product images:

- `ProductMedia` owns product-specific metadata directly: store id, product id, source URL, storage path, hash, file name, MIME type, width, height, size, sort order, primary flag, alt text, status, error, version, timestamps, and soft delete.
- `ProductMediaService` supports list, import, retry, reorder, set primary, delete, idempotent task enqueue, store scope validation, and catalog cache invalidation.
- `ProductMediaImportTaskHandler` processes `product.media.import` under the existing `CommerceTaskWorker`.
- `ProductMediaDownloader` validates public HTTP/HTTPS URLs, blocks private/local hosts, validates image signatures, stores originals under `runtime/media`, captures hash and dimensions, and enforces size/time limits.
- `ProductMediaController` serves `/media/products/{mediaPublicId}` with store scope, bounded dimensions, fit/format validation, immutable cache headers, and optional imgproxy rendering.
- `CommerceMediaController` has a separate admin image upload endpoint under `api/commerce/admin/media/images`, but it writes to `uploads` and is not integrated with `product_media` or reusable asset metadata.

Key source references:

- `BlazorShop.Domain/Entities/CommerceNode/ProductMedia.cs`
- `BlazorShop.Application/CommerceNode/ProductMedia/ProductMediaDtos.cs`
- `BlazorShop.Application/CommerceNode/ProductMedia/IProductMediaService.cs`
- `BlazorShop.Infrastructure/Data/CommerceNode/Services/ProductMediaService.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Tasks/ProductMediaImportTaskHandler.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/ProductMedia/ProductMediaDownloader.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/ProductMediaController.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/CommerceProductMediaController.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/CommerceMediaController.cs`
- `BlazorShop.Infrastructure/Data/CommerceNode/CommerceNodeDbContext.cs`

## Design Implications For BlazorShop

Do:

- Keep Media Library inside Commerce Node and `CommerceNodeDbContext`.
- Keep public media store-scoped. No global `/media/{id}` endpoint for storefront.
- Keep existing `CommerceTaskWorker` and `commerce_task` for async import.
- Preserve `/media/products/{mediaPublicId}` for storefront compatibility.
- Use the existing imgproxy path and cache behavior.
- Reuse current ProductMedia security rules for remote imports.

Do not:

- Copy Smartstore implementation code or module structure.
- Add runtime references to Smartstore projects.
- Move media ownership to Control Plane.
- Make Control Plane Web call Commerce Node directly.
- Build storage provider abstraction in MVP unless there is an immediate non-filesystem backend requirement.
- Replace `Product.Image` compatibility behavior in the first phase.

## Proposed BlazorShop Model

Phase 1 should introduce a small generic asset table, not the full Smartstore system.

Candidate entities:

- `CommerceMediaAsset`
  - `Id`, `PublicId`, `StoreId`
  - `FolderId`
  - `OriginalSourceUrl`
  - `StoragePath`
  - `ContentHash`
  - `FileName`
  - `DisplayName`
  - `MimeType`
  - `MediaType`
  - `Extension`
  - `Width`, `Height`, `FileSizeBytes`
  - `AltText`, `TitleText`
  - `MetadataJson`
  - `Status`
  - `Version`
  - `CreatedAt`, `UpdatedAt`, `ProcessedAt`, `DeletedAt`

- `CommerceMediaFolder`
  - `Id`, `PublicId`, `StoreId`
  - `ParentId`
  - `Name`, `Slug`, `Path`
  - `SortOrder`
  - `CreatedAt`, `UpdatedAt`, `DeletedAt`

- `CommerceMediaTag`
  - `Id`, `StoreId`, `Name`, `Slug`

- `CommerceMediaAssetTag`
  - `AssetId`, `TagId`

- `CommerceMediaReference`
  - `Id`, `StoreId`, `AssetId`
  - `EntityType`
  - `EntityId`
  - `Role`
  - `CreatedAt`

Product media can then evolve from "asset record embedded in product mapping" to "product-to-asset mapping":

- Add nullable `MediaAssetId` to `ProductMedia`.
- New imports create `CommerceMediaAsset` first, then create/update `ProductMedia`.
- Existing rows can be backfilled gradually from current `product_media` values.
- `Product.Image` keeps using `/media/products/{mediaPublicId}` until Storefront V2 is ready for generic media URLs.

## MVP Feature Slice

Recommended first slice:

1. Add `CommerceMediaAsset` only, plus service/DTOs for upload/import/list/detail.
2. Store only images in MVP: jpeg, png, webp, gif.
3. Use store-scoped admin APIs under `api/commerce/admin/media/assets`.
4. Support search by file name/display name, MIME/media type, status, created date, and content hash.
5. Support upload from multipart and import from remote URL using the hardened ProductMedia downloader logic.
6. Add reuse action: attach an existing asset to a product as ProductMedia.
7. Keep folder/tag/reference tracking for phase 2 unless UI work requires them immediately.

Completeness: 7/10. This gives a real reusable media library without taking on Smartstore's full folder, tracking, cleanup, and storage provider surface.

## Later Phases

Phase 2:

- Add folders and move/copy semantics.
- Add tags.
- Add reference tracking so delete can warn or block when assets are attached to products, categories, storefront pages, or SEO content.
- Add orphan/transient cleanup through `commerce_task` or an existing scheduled/background path.

Phase 3:

- Add generic public rendering route for non-product asset roles if needed, still store-scoped.
- Add category/page/editor integration.
- Add duplicate detection by content hash and duplicate filename policy.
- Add admin usage view: "used by products/pages/categories".

Phase 4:

- Consider storage provider abstraction only when S3/Azure/database storage is required.
- Consider image reprocessing jobs and cache purge/version bump operations.

## Main Risks

- Introducing a generic library too early can duplicate ProductMedia and make product image behavior harder to reason about.
- Deleting or replacing an asset without reference tracking can break product/category/page rendering.
- A global public media URL would violate V2 store isolation.
- Multipart admin upload currently writes to `uploads` and is not attached to Commerce Media metadata; leaving it separate will create two media worlds.
- Remote import must preserve SSRF protections: no localhost/private IP, content-type allowlist, file signature validation, max bytes, timeout, safe error messages.

## QA Additions For The First Implementation Phase

Add Commerce Node QA cases:

- `POST /api/commerce/admin/media/assets/upload` stores valid image metadata.
- Upload rejects invalid signature, unsupported MIME type, and over-limit size.
- `POST /api/commerce/admin/media/assets/import` queues or stores a remote image with SSRF protection.
- `GET /api/commerce/admin/media/assets` is paged and store-scoped.
- Duplicate content hash is detectable and handled by the selected policy.
- Asset attach to product creates ProductMedia and preserves existing product media list behavior.
- Store A cannot list, attach, or render Store B assets.
- Product primary image and `/media/products/{mediaPublicId}` remain backward compatible.

Add Storefront V2 QA only when public rendering changes:

- Product listing/detail/cart still render `Product.Image`.
- Structured data image remains valid.
- Public media URL resolves only for the current store host/key.

## Decision

Use Smartstore as business reference, not architecture import. Build a BlazorShop-native, store-scoped Commerce Media Asset layer in narrow phases, with ProductMedia as the first consumer.
