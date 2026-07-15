# Media Core

Status: in progress
Date: 2026-07-15
Scope: Media storage, image processing policy, delivery hardening, and assignment expansion for active V2 Commerce Node and Storefront V2.

## Implementation Status

Updated: 2026-07-15

- Phase 0 complete: baseline inventory captured for ProductMedia, CommerceMediaAsset, Storefront V2 media proxy, Control Plane gateway routes, CommerceNode compose/imgproxy/Nginx config, current query limits, file size limits, and public URL shapes. No runtime behavior changed.
- Phase 1 complete: shared media file/signature policy, transform normalization, and URL presets added in Application; product media and generic asset controllers/services now reuse the shared policy without route or DB changes.
- Phase 2 complete: `IMediaStorageProvider` and local filesystem provider added; product media import/original render and generic asset upload/replace/delete/original render use provider-backed path resolution while preserving existing storage layout.
- Phase 3 complete: product and generic asset URL builders support named presets and configured-public-base absolute URLs; product media cache now distinguishes versioned immutable URLs from unversioned short-cache URLs; media responses include `nosniff`; Storefront V2 media proxy copies `X-Content-Type-Options`. Placeholder image asset selection is deferred because the current codebase has no semantic placeholder/no-image asset to reference safely.
- Phase 4 complete: product media service now invalidates catalog cache after order changes and every delete; deleting primary media excludes the deleted row when selecting the next primary so `Product.Image` is reassigned or cleared correctly; service tests guard primary sync, fallback/clear, invalidation, and alt text preservation.

## Goal

Build a stable Media Core around the media work that already exists instead of replacing it.

The project already has two active V2 media surfaces:

- `ProductMedia` for product gallery, primary product image, async import, and `Product.Image` compatibility.
- `CommerceMediaAsset` for generic page/content assets, admin upload, metadata, public asset URLs, and imgproxy transforms.

This phase should consolidate shared policy, harden delivery, and add missing assignment flows without breaking existing storefront/catalog behavior.

## Current Code Facts

- Product media is stored in `BlazorShop.Domain/Entities/CommerceNode/ProductMedia.cs`.
- Generic media assets are stored in `BlazorShop.Domain/Entities/CommerceNode/CommerceMediaAsset.cs`.
- Product media import uses `commerce_task` task type `product.media.import` and the existing `CommerceTaskWorker`.
- Public product media URL shape already exists:

```text
/media/products/{mediaPublicId}?w=600&h=600&fit=contain&format=webp&v=1
```

- Public generic asset URL shape already exists:

```text
/media/assets/{assetPublicId}/{canonicalFileName}
```

- Storefront V2 proxies public media routes and sends the configured store key to Commerce Node.
- `Product.Image` remains the Storefront V2 compatibility field and is synced to the primary `ProductMedia` URL.
- `Category.Image` currently exists as a string compatibility field, but category image assignment does not yet have a media-backed model.
- `ProductVariant` currently has no image field.
- No active Manufacturer domain was found in the current V2 code path.
- Commerce Node runtime already includes imgproxy in `compose.commercenode.yml`.
- Existing media storage is local filesystem under Commerce Node runtime paths.

## Boundaries

Owner:

- `BlazorShop.CommerceNode.API`
- `BlazorShop.Infrastructure/Data/CommerceNode`
- `BlazorShop.Application/CommerceNode`

Admin caller:

```text
BlazorShop.ControlPlane.Web
  -> BlazorShop.ControlPlane.API
      -> BlazorShop.CommerceNode.API
```

Public caller:

```text
BlazorShop.Storefront.V2
  -> /media/products/{mediaPublicId}
  -> /media/assets/{assetPublicId}/{canonicalFileName}
```

Database owner:

- `CommerceNodeDbContext`

Forbidden:

- No new V2 media feature in `AppDbContext`.
- No Control Plane database table for node-local media.
- No direct `ControlPlane.Web -> CommerceNode.API` call.
- No legacy `api/internal/*`, `api/admin/*`, `api/public/*`, or legacy presentation extension.
- No route-breaking change to `/media/products/*` or `/media/assets/*`.

## Selected Scope

### Included

- Shared image/media validation policy.
- Shared transform and URL preset policy.
- Storage provider abstraction with current local filesystem implementation.
- Delivery hardening for cache, placeholder, absolute URL, and storefront-safe alt text.
- Product media hardening without rewriting `ProductMedia`.
- Category media assignment using existing `CommerceMediaAsset` and `Category.Image` compatibility sync.
- Clear separation between catalog media, content/page assets, branding/theme assets, and future variant media.
- QA checklist updates for Commerce Node, Control Plane, Storefront V2, and task orchestration where relevant.

### Deferred

- Full S3/MinIO implementation.
- Video/audio/document processing.
- Folder/tag UI.
- Full reference tracking.
- Full orphan cleanup worker.
- Manufacturer image/logo because active Manufacturer model is not established.
- Variant/attribute-value thumbnail mapping until product variant UX/model needs it.
- Replacing `ProductMedia` with `CommerceMediaAsset`.
- Pre-generating image derivatives.
- Signed public storefront media URLs.

## Target Architecture

```text
ControlPlane.Web
  -> ControlPlane.API
      -> CommerceNode API api/commerce/admin/media/*
      -> CommerceNode API api/commerce/admin/products/{productId}/media/*
      -> CommerceNode API api/commerce/admin/categories/{categoryId}/media/*

CommerceNode API
  -> CommerceNodeDbContext
      -> product_media
      -> commerce_media_assets
      -> category_media_assignments
      -> commerce_task
  -> IMediaStorageProvider
      -> LocalMediaStorageProvider
      -> future S3MediaStorageProvider
  -> MediaPolicy/MediaUrlPolicy
  -> imgproxy

Storefront V2
  -> Product.Image
  -> Category.Image
  -> Page body /media/assets/* links
  -> /media/products/*
  -> /media/assets/*
```

## Data Model Direction

### Keep Existing

Keep `ProductMedia` as the product gallery model.

Reasons:

- It owns product-specific behavior: product ID, primary image, gallery order, import task status, retry, delete, and `Product.Image` sync.
- It already matches Storefront V2 compatibility needs.
- Replacing it would create unnecessary migration and UI risk.

Keep `CommerceMediaAsset` as generic reusable asset model.

Reasons:

- It already supports upload, metadata, public asset URL, canonical filename, transform, and page/content usage.
- It should remain the source for category image, page embedded media, branding, and theme-like assets unless a future use case needs a more specific table.

### Add Carefully

Add a Commerce Node-only category assignment model instead of directly changing the shared `Category` entity first:

```text
category_media_assignment
  id
  store_id
  category_id
  media_asset_id
  alt_text
  sort_order
  is_primary
  created_at
  updated_at
```

Compatibility rule:

- When category primary media changes, sync `Category.Image` to a stable `/media/assets/{assetPublicId}/{canonicalFileName}?preset=category-thumbnail...` style URL or to the resolved preset URL format used by Media Core.
- Storefront and existing category DTOs continue to read `Category.Image`.

Avoid adding `ImageAssetId` directly to `Category` in the first slice because `Category` is shared with legacy code. A separate Commerce Node assignment table reduces migration blast radius.

## Shared Policy Direction

Add shared application-level policy types under `BlazorShop.Application/CommerceNode/Media` or a nearby existing media namespace:

```text
MediaFilePolicy
MediaFileType
MediaTransformPolicy
MediaUrlPreset
MediaUrlPresetNames
MediaStorageObject
IMediaStorageProvider
```

Do not introduce a broad module framework. Keep these as small contracts/helpers reused by existing services and controllers.

## URL Presets

Define named presets so UI and API callers stop hardcoding query rules:

| Preset | Intended use | Width | Height | Fit | Format |
| --- | --- | ---: | ---: | --- | --- |
| `product-card` | Product listing/card | 600 | 600 | `contain` | `webp` |
| `product-detail` | Product detail primary image | 1000 | 1000 | `contain` | `webp` |
| `cart-line` | Cart/mini-cart thumbnail | 160 | 160 | `cover` | `webp` |
| `category-card` | Category tile/list thumbnail | 600 | 400 | `cover` | `webp` |
| `content-banner` | Page/content banner | 1920 | 600 | `cover` | `webp` |
| `content-card` | Page/content card image | 800 | 600 | `cover` | `webp` |
| `brand-logo` | Store branding/logo | 320 | null | `inside` or supported equivalent | `png` or original |

Implementation note:

- Product media currently supports `contain`, `cover`, and `max`.
- Generic assets currently support `cover`, `contain`, and `inside`.
- Phase 1 should reconcile this naming without breaking existing query params. Existing values must remain accepted.

## Phase 0 - Baseline And Safety Lock

Goal: confirm the exact current behavior and prevent accidental rewrite.

Tasks:

- [x] Re-read `ProductMedia`, `ProductMediaService`, `ProductMediaUrlBuilder`, `ProductMediaDownloader`, and `ProductMediaController`.
- [x] Re-read `CommerceMediaAsset`, `CommerceMediaAssetService`, `CommerceMediaAssetsController`, and `CommerceMediaAssetPublicController`.
- [x] Re-read Storefront V2 media proxy route in `BlazorShop.Storefront.V2/Program.cs`.
- [x] Re-read `compose.commercenode.yml` and Nginx media cache config.
- [x] Confirm current query limits for product media and generic assets.
- [x] Confirm current file size limits for upload and remote import.
- [x] Confirm current Control Plane gateway routes for product media and generic media assets.
- [x] Snapshot current public URL examples for products and assets.

Exit gate:

- [x] Existing product media import still works. Baseline QA already covers valid public image URL import, task storage, retry, primary sync, and private/local URL rejection.
- [x] Existing media asset upload still works. Baseline QA already covers PNG upload/list/preview/replace/delete through Control Plane.
- [x] Existing `/media/products/*` route is documented.
- [x] Existing `/media/assets/*` route is documented.
- [x] No code change yet except this plan and optional QA checklist notes.

Phase 0 baseline facts:

- Product media public route: `/media/products/{mediaPublicId}` plus query `w`, `h`, `fit`, `format`, `v`.
- Product media allowed output formats: `webp`, `jpg`, `png`.
- Product media accepted fit values: `contain`, `cover`, `max`.
- Product media dimension default/max: default width `1000`, max dimension `2000`.
- Product media storage root: `ProductMediaStorage:RootPath`, default `runtime/media`.
- Product media download/import max size: `ProductMediaStorage:MaxDownloadBytes`, default `10MB`.
- Product media source validation blocks non-HTTP(S), localhost, private, and local IP hosts.
- Product media cache currently emits `Cache-Control: public, max-age=31536000, immutable` and `ETag` for every successful public response.
- Generic asset public route: `/media/assets/{assetPublicId}/{canonicalFileName}` plus aliases `w`/`width`, `h`/`height`, `fit`, `format`, `v`.
- Generic asset allowed output formats: `original`, `webp`, `jpg`, `png`.
- Generic asset accepted fit values: `contain`, `cover`, `inside`.
- Generic asset dimension max: `4096`, max output pixels `16,000,000`.
- Generic asset upload max size: `CommerceMediaStorage:MaxUploadBytes`, default `10MB`.
- Generic asset supported upload extensions: `.jpg`, `.jpeg`, `.png`, `.webp`, `.gif`, `.ico`.
- Generic asset signature validation exists inside `CommerceMediaAssetService`; product media download uses `ImageFileSignatureValidator`.
- Generic asset cache currently uses immutable long cache only when `v` is present, otherwise `public, max-age=3600`.
- Storefront V2 proxies `/media/products/*` and `/media/assets/*` to Commerce Node and forwards configured `X-Store-Key`.
- Storefront V2 currently copies `Cache-Control`, `ETag`, and `Last-Modified` from Commerce Node media responses.
- `compose.commercenode.yml` runs imgproxy at local port `8089` with `IMGPROXY_LOCAL_FILESYSTEM_ROOT=/var/blazorshop/media`.
- Commerce Node Nginx keeps a default catch-all server returning `403`.

## Phase 1 - Shared Media Policy

Goal: centralize validation and transform policy without changing behavior.

Tasks:

- [x] Add shared media type constants for current supported image types.
- [x] Add shared MIME/signature validation helper or service.
- [x] Reuse existing `ImageFileSignatureValidator` behavior instead of duplicating validation logic.
- [x] Define shared max dimension defaults and per-surface limits:
  - product media public max dimension currently `2000`.
  - generic asset public max dimension currently `4096`.
- [x] Define shared transform value normalization:
  - preserve existing `contain`, `cover`, `max`, and `inside` behavior.
  - map equivalent values internally where possible.
- [x] Define `MediaUrlPreset` names and default query options.
- [x] Add unit tests for policy normalization and invalid values.

Likely files:

- `BlazorShop.Application/CommerceNode/Media/*`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Validation/ImageFileSignatureValidator.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/ProductMediaController.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/CommerceMediaAssetPublicController.cs`

Review gate:

- [x] No DB migration.
- [x] Existing query values still accepted. Covered by `MediaTransformPolicyTests` for product `contain|cover|max` and generic asset `contain|cover|inside`.
- [x] Invalid media values return the same or better safe errors. Existing error strings are preserved by shared policy results.
- [x] Product and asset controllers still enforce store scope before rendering. Store scope checks remain before media lookup in both public controllers.

Phase 1 verification:

- `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~MediaFilePolicyTests|FullyQualifiedName~MediaTransformPolicyTests"` passed 20/20.
- `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~ImageFileSignatureValidatorTests|FullyQualifiedName~ProductMedia"` passed 3/3 matched existing signature tests.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.

## Phase 2 - Storage Provider Abstraction

Goal: make local filesystem storage swappable later without adding S3 now.

Tasks:

- [x] Add `IMediaStorageProvider` contract for storing, replacing, deleting, opening, and resolving local processing paths.
- [x] Add `LocalMediaStorageProvider` wrapping current filesystem path behavior.
- [x] Move path normalization and trusted ID-based path generation behind the provider.
- [x] Preserve current storage layout:
  - product media under `runtime/media/stores/.../products/...`
  - generic assets under `runtime/media/assets/stores/...`
- [x] Keep imgproxy local filesystem path behavior working.
- [x] Add tests for path traversal prevention and storage key normalization.
- [x] Do not add S3 package or cloud credentials in this phase.

Likely files:

- `BlazorShop.Application/CommerceNode/Media/*`
- `BlazorShop.Infrastructure/Data/CommerceNode/Services/CommerceMediaAssetService.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/ProductMedia/ProductMediaDownloader.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Configuration/ProductMediaStorageOptions.cs`
- `BlazorShop.Application/CommerceNode/Media/CommerceMediaStorageOptions.cs`

Review gate:

- [x] Existing files remain readable. Original public controllers now resolve physical files through `IMediaStorageProvider`.
- [x] Existing product media import writes to the same effective location. Provider builds `stores/{storeId}/products/{productId}/{mediaPublicId}/original{extension}`.
- [x] Existing generic asset upload writes to the same effective location. Provider builds `stores/{storeId}/{assetPublicId}/original{extension}` under the generic asset root.
- [x] No public URL change.
- [x] No S3-specific runtime dependency.

Phase 2 verification:

- `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~LocalMediaStorageProviderTests|FullyQualifiedName~MediaFilePolicyTests|FullyQualifiedName~MediaTransformPolicyTests"` passed 25/25.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.

## Phase 3 - Delivery Hardening

Goal: make media URLs predictable, cacheable, and safe across Storefront, SEO, email, and feeds.

Tasks:

- [x] Add shared URL preset builder for products and assets.
- [x] Keep `IProductMediaUrlBuilder.BuildProductMediaUrl` backward compatible.
- [x] Add generic asset URL builder if not already present in service form.
- [x] Add absolute URL generation support using Storefront public URL rules:
  - prefer configured `PublicUrl:BaseUrl`.
  - fallback only through trusted forwarded headers where existing Storefront infrastructure permits.
- [~] Add placeholder/default image policy for missing product/category/page media. Deferred: no semantic placeholder/no-image asset exists in the current repo; adding a fake URL or reusing the banner image would create broken or misleading storefront behavior.
- [x] Align cache headers:
  - versioned URL: long cache and immutable.
  - unversioned URL: shorter cache.
  - `X-Content-Type-Options: nosniff`.
- [x] Ensure `ETag` generation includes id, transform, and version.
- [x] Ensure admin preview endpoints do not leak another store's media.
- [x] Ensure Storefront V2 copies relevant response headers from Commerce Node media responses.

Likely files:

- `BlazorShop.Infrastructure/Data/CommerceNode/Services/ProductMediaUrlBuilder.cs`
- `BlazorShop.Infrastructure/Data/CommerceNode/Services/CommerceMediaAssetService.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/ProductMediaController.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/CommerceMediaAssetPublicController.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs`
- Storefront SEO/structured data services if absolute image URLs are consumed there.

Review gate:

- [x] `/media/products/*` route shape is unchanged and CommerceNode API builds.
- [x] `/media/assets/*` route shape is unchanged and CommerceNode API builds.
- [x] Versioned media URL cache busting remains intact.
- [x] Store A cannot access Store B media through public/admin media lookup because existing store-scoped query guards remain in place.
- [x] Public routes do not expose storage paths.

Phase 3 verification:

- `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~MediaUrlBuilderTests|FullyQualifiedName~MediaDeliveryHardeningTests|FullyQualifiedName~LocalMediaStorageProviderTests|FullyQualifiedName~MediaFilePolicyTests|FullyQualifiedName~MediaTransformPolicyTests"` passed 35/35.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore` passed.

## Phase 4 - Product Media Hardening

Goal: improve product media behavior while preserving existing product gallery and `Product.Image` compatibility.

Tasks:

- [x] Update product media service/controller to use shared policy from Phase 1.
- [x] Update downloader/storage path use to go through storage provider from Phase 2.
- [x] Add URL preset helper for product card, product detail, and cart thumbnails.
- [x] Ensure primary media sync still writes `Product.Image`.
- [x] Ensure deleting primary media still chooses next stored media or clears `Product.Image`.
- [x] Ensure catalog cache invalidation still runs when primary/order/delete changes.
- [x] Expose or preserve `AltText` in admin DTOs.
- [x] Evaluate whether Storefront product response needs primary image alt text without breaking existing DTOs. Decision: keep current Storefront public contract unchanged in Phase 4; it still exposes `Image`, `PrimaryMediaPublicId`, and `HasPrimaryMedia`, while admin product media DTO preserves `AltText`.
- [x] Do not change product import task type or worker model.

Likely files:

- `BlazorShop.Application/CommerceNode/ProductMedia/*`
- `BlazorShop.Infrastructure/Data/CommerceNode/Services/ProductMediaService.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Tasks/ProductMediaImportTaskHandler.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/CommerceProductMediaController.cs`
- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/*`
- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Pages/Catalog.razor`

Review gate:

- [x] No replacement of `ProductMedia`.
- [x] No replacement of `Product.Image`.
- [x] Import/retry/delete/primary/order behavior remains available.
- [x] Existing Control Plane Web still calls Control Plane API only.
- [x] Storefront product listing/detail/cart still render product image through existing `Product.Image` compatibility.

Phase 4 verification:

- `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductMediaServiceTests|FullyQualifiedName~MediaUrlBuilderTests|FullyQualifiedName~MediaDeliveryHardeningTests|FullyQualifiedName~MediaFilePolicyTests|FullyQualifiedName~MediaTransformPolicyTests|FullyQualifiedName~LocalMediaStorageProviderTests"` passed 40/40.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.

## Phase 5 - Category Media Assignment

Goal: add media-backed category image assignment without breaking `Category.Image`.

Design:

- Use `CommerceMediaAsset` as the source image.
- Add a Commerce Node-only assignment table instead of directly changing shared `Category` in the first slice.
- Sync `Category.Image` to the chosen asset URL for existing DTO/storefront compatibility.

Tasks:

- [ ] Add `CategoryMediaAssignment` entity under Commerce Node domain namespace or another Commerce Node-only model location.
- [ ] Add `DbSet` and EF configuration to `CommerceNodeDbContext`.
- [ ] Add Commerce Node migration only.
- [ ] Add application DTOs and service methods:
  - get category media assignment.
  - set category primary media asset.
  - clear category media assignment.
  - update category media alt text if needed.
- [ ] Validate category belongs to current store.
- [ ] Validate asset belongs to current store.
- [ ] Sync `Category.Image` on set/clear.
- [ ] Invalidate category/catalog cache after changes.
- [ ] Add Commerce Node admin endpoint under the existing Commerce Admin route style.
- [ ] Add Control Plane API gateway endpoint.
- [ ] Add Control Plane Web UI control only if it fits current category manager UX; otherwise expose API first and defer UI polish.

Suggested route shape:

```text
GET    api/commerce/admin/categories/{categoryId}/media
PUT    api/commerce/admin/categories/{categoryId}/media/primary
DELETE api/commerce/admin/categories/{categoryId}/media/primary
```

Control Plane route should stay under:

```text
api/control-plane/stores/{storePublicId}/catalog/categories/{categoryId}/media
```

Review gate:

- [ ] Store scope checked for both category and asset.
- [ ] `Category.Image` remains populated for old DTOs.
- [ ] Existing category APIs do not break.
- [ ] Migration touches `CommerceNodeDbContext` only.
- [ ] No legacy `AppDbContext` migration.

## Phase 6 - Content, Branding, And Theme Asset Classification

Goal: clarify asset usage without forcing a full folder/reference-tracking system.

Tasks:

- [ ] Add optional usage classification for generic assets only if it materially helps UI and assignment:
  - `content`
  - `branding`
  - `theme`
  - `category`
- [ ] Prefer a nullable/defaulted field on `CommerceMediaAsset` or a small side table only if migration risk is lower.
- [ ] Keep existing generic asset upload/list behavior compatible.
- [ ] Add filters in API/UI only after the data field exists.
- [ ] Use `CommerceMediaAsset` for page embedded media and branding assets.
- [ ] Do not mix theme/runtime deployment files with product gallery media.
- [ ] Do not add page editor picker in this phase unless a separate page-editor phase approves it.

Review gate:

- [ ] Existing media library still lists old assets.
- [ ] Old assets default to `content` or equivalent.
- [ ] Existing copied page `<img>` snippets still work.
- [ ] No product media rows are migrated into generic assets.

## Phase 7 - Orphan And Delete Policy

Goal: define predictable delete/retain behavior without building a heavy media lifecycle system.

Current behavior:

- `ProductMedia` has soft-delete behavior through status/deleted timestamp.
- `CommerceMediaAsset` currently behaves closer to hard-delete.

Tasks:

- [ ] Document current delete behavior in admin UI and QA.
- [ ] Keep product media soft delete.
- [ ] Keep generic asset hard delete unless reference tracking is added.
- [ ] Add pre-delete warning for generic assets that page links may break.
- [ ] If category assignment exists, block or warn before deleting an assigned asset.
- [ ] Add optional lightweight usage check:
  - category assignments.
  - store branding fields if they point to media assets.
  - page body string scan is optional and should be treated as advisory only.
- [ ] Do not add background orphan cleanup worker yet.

Review gate:

- [ ] Product media delete behavior unchanged.
- [ ] Generic media delete remains explicit and understandable.
- [ ] Assigned category asset cannot silently break category image without warning or cleanup.

## Phase 8 - Optional Object Storage Adapter

Goal: prepare for S3-compatible storage only after local provider abstraction is stable.

Entry criteria:

- Phase 2 local provider is merged and verified.
- The project has a real deployment requirement for S3/MinIO or equivalent.
- Required config names and secret ownership are agreed.

Tasks:

- [ ] Add provider options:
  - provider type: `local` or `s3`.
  - bucket/container.
  - region/endpoint.
  - public URL mode.
  - credentials through runtime secrets only.
- [ ] Implement `S3MediaStorageProvider` behind `IMediaStorageProvider`.
- [ ] Keep public storefront URL routed through BlazorShop/Nginx, not raw S3 URLs, unless a future CDN decision explicitly changes that.
- [ ] Ensure imgproxy can read source objects safely, either through configured object storage support or a controlled internal fetch path.
- [ ] Add integration tests behind opt-in config.
- [ ] Document local dev still defaults to local filesystem.

Review gate:

- [ ] No credentials in client code or repository.
- [ ] No public raw bucket URL unless explicitly approved.
- [ ] Local filesystem provider remains default.
- [ ] Existing media URLs remain stable.

## Phase 9 - Variant And Attribute Media Future Phase

Goal: defer variant media until the variant UX/model needs it.

Rationale:

- `ProductVariant` currently has SKU, attributes, price, stock, color, and default flag, but no image field.
- Adding variant media now would require Storefront variant selection UI and cart/checkout image rules.

Future tasks when approved:

- [ ] Define whether variant image is one primary image or a gallery.
- [ ] Add `ProductVariantMediaAssignment` or equivalent Commerce Node model.
- [ ] Decide fallback order:
  - variant image.
  - product primary image.
  - placeholder.
- [ ] Update Storefront variant selection and cart image behavior.
- [ ] Update Control Plane product variant editor.

This is intentionally not part of the first Media Core implementation.

## Phase 10 - QA And Documentation

Goal: prove each phase works and record the behavior for future agents.

Tasks:

- [ ] Update `QA-CommerceNode.todo.md` with media policy, storage, public rendering, category assignment, and delete/usage cases.
- [ ] Update `QA-ControlPlane.todo.md` for admin gateway/UI cases.
- [ ] Update `QA-StorefrontV2.todo.md` for product/category/page image rendering.
- [ ] Update `QA-CommerceNode-TaskOrchestration.todo.md` if product media import behavior changes.
- [ ] Update architecture docs only when a hard runtime rule changes.
- [ ] Run focused API tests for changed Commerce Node endpoints.
- [ ] Run contract tests for new/changed V2 APIs.
- [ ] Run browser QA when Control Plane or Storefront UI changes.
- [ ] Verify `docker compose -f compose.commercenode.yml up -d` media dependencies when delivery changes touch imgproxy/Nginx.

Exit gate:

- [ ] Product media import still works.
- [ ] Generic media asset upload/list/render still works.
- [ ] Product listing/detail/cart images still render.
- [ ] Category image renders when assigned.
- [ ] Missing media uses placeholder instead of broken UI where implemented.
- [ ] Store A media does not resolve from Store B.
- [ ] Public media cache headers are correct.
- [ ] Swagger/OpenAPI standards are satisfied for every changed API.

## API Contract Checklist

Every new or changed API must include:

- [ ] Stable `operationId`.
- [ ] Short summary.
- [ ] Explicit request DTO.
- [ ] Explicit response DTO.
- [ ] Standard error responses.
- [ ] Required body metadata for body-reading endpoints.
- [ ] Security metadata.
- [ ] Validation metadata.
- [ ] No domain entities in public schemas.
- [ ] No side-effecting `GET`.
- [ ] Contract tests and snapshots where existing project pattern supports them.

## QA Checklist Seeds

### Commerce Node

- [ ] Product media public route accepts existing query values.
- [ ] Product media public route rejects invalid dimensions.
- [ ] Generic asset public route accepts existing query values.
- [ ] Generic asset public route rejects invalid dimensions.
- [ ] Product media import validates MIME/signature through shared policy.
- [ ] Generic asset upload validates MIME/signature through shared policy.
- [ ] Store A product media cannot render from Store B context.
- [ ] Store A generic asset cannot render from Store B context.
- [ ] Category assignment requires category and asset to belong to current store.
- [ ] Category assignment syncs `Category.Image`.
- [ ] Clearing category assignment clears or restores `Category.Image` according to selected rule.
- [ ] Deleting assigned generic asset is blocked or warns and cleans assignment.
- [ ] Versioned media URLs return long cache headers.
- [ ] Unversioned media URLs return shorter cache headers.

### Control Plane

- [ ] Media library upload/list/update/delete still routes through Control Plane API.
- [ ] Product media actions still route through Control Plane API.
- [ ] Category media assignment route goes through Control Plane API.
- [ ] Browser network calls do not expose Commerce Node credentials.
- [ ] Error message from Commerce Node is surfaced through response envelope.

### Storefront V2

- [ ] Product card image renders.
- [ ] Product detail image renders.
- [ ] Cart line image renders.
- [ ] Category image renders after assignment.
- [ ] Missing product/category image uses placeholder where implemented.
- [ ] Page body copied `/media/assets/*` snippet still renders.
- [ ] Absolute image URLs for SEO/email/feed use configured public base URL when available.

## Risk Register

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Replacing `ProductMedia` breaks primary image/import behavior | High | Do not replace it; wrap shared helpers around it |
| Changing public media route breaks storefront and SEO | High | Preserve `/media/products/*` and `/media/assets/*` |
| Shared policy accidentally rejects existing images | Medium | Add compatibility tests before switching callers |
| Storage abstraction changes file paths | High | Local provider must preserve effective existing layout |
| Category media touches shared legacy `Category` entity too broadly | Medium | Prefer separate Commerce Node assignment table first |
| Deleting generic asset breaks page HTML links | Medium | Add warnings and later lightweight usage checks |
| Store scope bug leaks media across stores | Critical | Query by current store plus public id for every render/mutation |
| S3 added too early increases config/security surface | Medium | Defer object storage adapter until local provider is stable |

## Implementation Order

Recommended commit order:

1. Plan and QA checklist updates.
2. Shared media policy and tests.
3. Local storage provider abstraction and tests.
4. Delivery preset/cache/placeholder hardening.
5. Product media adoption of shared policy/provider.
6. Category media assignment schema/service/API.
7. Control Plane gateway/UI for category media if approved.
8. Content/branding/theme asset classification if still needed.
9. Delete/orphan usage guard.
10. Optional object storage adapter only after approval.

## Decision Audit Trail

| # | Decision | Classification | Rationale | Rejected |
| --- | --- | --- | --- | --- |
| 1 | Keep `ProductMedia` as product gallery model | Architecture | Current code already owns primary image, gallery order, async import, and `Product.Image` sync | Replacing with generic media table |
| 2 | Keep `CommerceMediaAsset` as generic/page/content asset model | Architecture | Current code already supports upload, metadata, public asset URL, and imgproxy transforms | Building a third generic asset table |
| 3 | Add shared media policy before feature expansion | Engineering | Reduces duplication and avoids inconsistent MIME/transform behavior | Adding category/variant media first |
| 4 | Add storage abstraction with local provider first | Engineering | Enables future S3 without forcing deployment complexity now | Immediate S3/MinIO implementation |
| 5 | Preserve public URL shapes | Compatibility | Storefront, SEO, cache, and copied page snippets depend on stable routes | Route rename or raw object storage URLs |
| 6 | Add category media through a Commerce Node assignment table | Compatibility | Avoids broad shared `Category` entity impact while keeping `Category.Image` compatible | Direct first-slice mutation of shared Category model |
| 7 | Defer variant media | Scope | `ProductVariant` has no image model and would require storefront variant UX rules | Adding variant image mapping now |
| 8 | Defer manufacturer media | Scope | Active Manufacturer domain was not found in current V2 path | Adding logo fields without owning domain |
| 9 | Do not add full orphan worker now | Scope | Current product media has soft delete and generic asset hard delete; usage tracking is not mature yet | Smartstore-style full media lifecycle |
