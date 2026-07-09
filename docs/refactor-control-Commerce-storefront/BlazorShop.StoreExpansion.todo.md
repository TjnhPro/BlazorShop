# BlazorShop Store Expansion Todo

Status: draft
Created: 2026-07-09
Scope: Expand BlazorShop Store business depth by learning from Smartstore Store concepts without copying Smartstore code.

Related plan: `BlazorShop.CommerceNode.TaskOrchestration.todo.md` owns async task persistence, worker execution, Docker/Nginx deployment, retry, cancel, and rollback. This file owns Store business/domain/runtime behavior.

## Goal

Build a real Store business foundation for BlazorShop V2.

Smartstore `Store` is the reference for business depth: name, domain/host, URL, SSL, media icons, CDN, currency, display order, current-store resolution, store-closed behavior, store-scoped data, cache, and lifecycle cleanup.

BlazorShop currently has only:

- Control Plane `StoreRegistry`: platform registry for node assignment and domains.
- Commerce Node `AdminSettings`: global store name/support/currency/maintenance settings.
- Product/category/order/cart tables without store scope.

This plan expands Store as a Commerce Node business concept while keeping Control Plane as the platform registry.

## Non Goals

- Do not copy Smartstore code.
- Do not introduce Smartstore dependencies.
- Do not refactor legacy `BlazorShop.Presentation`.
- Do not move Control Plane registry tables into Commerce Node.
- Do not use `AppDbContext`.
- Do not implement full shared-products-across-stores mapping in the first slice unless explicitly approved.
- Do not make Storefront public APIs directly callable from the internet.
- Do not design the generic CommerceNode task queue/worker in this file.
- Do not own Docker/Nginx deployment mechanics in this file.

## Smartstore Store Concepts To Learn From

Reference folder:

```text
Smartstore/src/Smartstore.Core/Platform/Stores
```

Useful business concepts:

| Smartstore concept | Meaning | BlazorShop adoption |
| --- | --- | --- |
| `Store.Name` | Display/business name | Adopt in `CommerceStore`. |
| `Store.Url` | Canonical base URL | Adopt as `BaseUrl`. |
| `Store.Hosts` | Comma-separated host aliases for current-store resolution | Adopt as normalized domain rows instead of comma string. |
| `SslEnabled`, `SslPort` | HTTPS handling | Adopt minimal `ForceHttps`/`SslEnabled`; defer port unless needed. |
| Logo/icon media ids | Store branding assets | Adopt nullable media/url fields; full media library integration can be later. |
| `ContentDeliveryNetwork` | CDN host | Adopt as optional `CdnHost`. |
| `DefaultCurrencyId` | Store currency | Adopt as `DefaultCurrencyCode` string first; normalize later. |
| `DisplayOrder` | Admin/store ordering | Adopt. |
| `HtmlBodyId` | Store-specific HTML body id/theme hook | Adopt optional field. |
| `IStoreContext.CurrentStore` | Runtime current store | Adopt as `ICommerceStoreContext`. |
| Host -> store cache | Fast lookup by request host | Adopt cached store/domain resolver. |
| `IStoreRestricted` + `StoreMapping` | Entity-level store visibility | Defer generic mapping; start with direct `StoreId` for MVP. |
| Store closed filter | Maintenance/closed behavior | Adopt store-specific maintenance middleware/result. |
| Store hooks | Prevent invalid deletes and cleanup dependent data | Adopt lifecycle rules around archive/disable, not hard delete. |
| Store permissions | Read/update/create/delete/readstats | Reuse Control Plane/Commerce Admin permission style. |

## Current BlazorShop Store Model

### Control Plane

`StoreRegistry` is platform metadata:

- `PublicId`
- `NodeId`
- `StoreKey`
- `Name`
- `Status`
- `MetadataJson`
- `Domains`

`StoreDomainRegistry` is platform domain metadata:

- domain
- normalized domain
- status: pending/verified/disabled

This is useful for platform/node assignment, but it is not enough for Commerce Node runtime store behavior.

### Commerce Node

Commerce Node currently has `AdminSettings`:

- `StoreName`
- `StoreSupportEmail`
- `StoreSupportPhone`
- `DefaultCurrency`
- `DefaultCulture`
- `MaintenanceModeEnabled`
- `MaintenanceMessage`

This is global node config, not a multi-store business model.

Catalog/order/cart entities currently have no `StoreId`.

## Target Architecture

Keep two Store concepts separate:

```text
ControlPlane StoreRegistry
  = platform registry, node assignment, operator view, domain ownership, audit

CommerceNode CommerceStore
  = runtime business store config, current-store resolution, storefront behavior
```

Runtime direction:

```text
Storefront V2
  -> resolves public host
  -> sends host/store key to Commerce Node internal API
  -> Commerce Node resolves CurrentStore
  -> Commerce Node filters catalog/order/settings by CurrentStore
```

Control Plane direction:

```text
ControlPlane Web
  -> ControlPlane API
  -> StoreRegistry CRUD
  -> CommerceNode API for store task submit/query/cancel/retry
```

ControlPlane does not store CommerceNode task state. Task state belongs to CommerceNode and is queried through `api/commerce/*` when needed.

## Locked Deployment Decisions

- Store deployment tasks live in the CommerceNode database.
- ControlPlane treats CommerceNode as an API service, not as a shared database.
- ControlPlane can submit a task, query task status, cancel a wrong task, or retry a failed task through CommerceNode API.
- Storefront V2 is deployed as one Docker container per store.
- Nginx belongs to the CommerceNode service cluster and reverse proxies public domains to Storefront containers.
- Redis is deferred for MVP; PostgreSQL is enough for task persistence.
- Deployment rollback is required and is owned by `BlazorShop.CommerceNode.TaskOrchestration.todo.md`.

## Dependency On CommerceNode Task Orchestration

StoreExpansion depends on `BlazorShop.CommerceNode.TaskOrchestration.todo.md` for async deployment work.

StoreExpansion owns:

- `CommerceStore` business model.
- store domain model.
- current store resolution.
- store-scoped catalog/order/customer behavior.
- validation rules for Store business payloads.

TaskOrchestration owns:

- `commerce_task`.
- task worker.
- task status and step history.
- Docker container creation.
- Nginx config generation/reload.
- rollback.

The Store create/deploy flow should be:

```text
ControlPlane create StoreRegistry
  -> ControlPlane calls CommerceNode API
  -> CommerceNode enqueues store.create_and_deploy
  -> CommerceNode worker creates CommerceStore/domain
  -> CommerceNode worker deploys StorefrontV2 container
  -> CommerceNode worker updates deployment/task status
```

Do not implement deployment directly inside Store CRUD services.

## Database Design

### Commerce Node DB: `commerce_store`

Purpose: local Commerce Node runtime store entity.

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `id` | `uuid` | no | Primary key. |
| `store_key` | `text` | no | Stable key, unique per Commerce Node. |
| `name` | `varchar(400)` | no | Business/display name. |
| `status` | `text` | no | `active`, `disabled`, `archived`. |
| `base_url` | `text` | yes | Canonical storefront base URL. |
| `force_https` | `boolean` | no | MVP replacement for Smartstore SSL behavior. |
| `ssl_enabled` | `boolean` | no | Optional compatibility flag for future. |
| `ssl_port` | `integer` | yes | Defer UI unless needed. |
| `display_order` | `integer` | no | Admin ordering. |
| `html_body_id` | `text` | yes | Theme/layout hook. |
| `cdn_host` | `text` | yes | CDN/static host. |
| `logo_url` | `text` | yes | Use URL/string first; media library can replace later. |
| `favicon_url` | `text` | yes | Store icon. |
| `png_icon_url` | `text` | yes | PWA/icon support. |
| `apple_touch_icon_url` | `text` | yes | PWA/icon support. |
| `ms_tile_image_url` | `text` | yes | Windows tile support. |
| `ms_tile_color` | `text` | yes | Windows tile color. |
| `default_currency_code` | `varchar(3)` | no | Start as ISO code string, e.g. `EUR`. |
| `default_culture` | `varchar(20)` | no | e.g. `en-US`. |
| `support_email` | `varchar(256)` | yes | Store support contact. |
| `support_phone` | `varchar(64)` | yes | Store support contact. |
| `maintenance_mode_enabled` | `boolean` | no | Store closed/maintenance. |
| `maintenance_message` | `text` | yes | Safe customer-facing message. |
| `metadata_json` | `jsonb` | yes | Escape hatch for later fields. |
| `created_at` | `timestamp with time zone` | no | Default current timestamp. |
| `updated_at` | `timestamp with time zone` | no | Updated by service. |
| `archived_at` | `timestamp with time zone` | yes | Soft delete only. |

Indexes:

- unique `store_key` where `archived_at is null`
- index `status`
- index `display_order`

Check constraints:

- `status in ('active', 'disabled', 'archived')`
- `default_currency_code` length 3

### Commerce Node DB: `commerce_store_domain`

Purpose: host/domain lookup for runtime current store resolution.

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `id` | `uuid` | no | Primary key. |
| `store_id` | `uuid` | no | FK to `commerce_store`. |
| `domain` | `text` | no | Original input. |
| `normalized_domain` | `text` | no | Lowercase host only, no scheme/path. |
| `is_primary` | `boolean` | no | Only one primary active domain per store. |
| `status` | `text` | no | `pending`, `verified`, `disabled`. |
| `created_at` | `timestamp with time zone` | no | Default current timestamp. |
| `updated_at` | `timestamp with time zone` | no | Updated by service. |
| `verified_at` | `timestamp with time zone` | yes | Verification time. |
| `disabled_at` | `timestamp with time zone` | yes | Soft disable. |

Indexes:

- unique `normalized_domain` where `disabled_at is null`
- index `store_id`
- partial unique `(store_id, is_primary)` where `is_primary = true and disabled_at is null`

Check constraints:

- `status in ('pending', 'verified', 'disabled')`

### Future DB: `commerce_store_setting`

Defer until Store has more plugin/config pressure.

Use when many store-scoped settings appear:

| Column | Type | Notes |
| --- | --- | --- |
| `id` | `uuid` | Primary key. |
| `store_id` | `uuid` | Store scope. |
| `setting_key` | `text` | Stable setting key. |
| `value_json` | `jsonb` | Typed setting payload. |
| `updated_at` | `timestamp with time zone` | Audit timestamp. |

### Future DB: Store Scope For Catalog

For MVP multi-store, prefer direct `StoreId` first:

- `Category.StoreId`
- `Product.StoreId`
- `Order.StoreId`
- `OrderItem.StoreId` or cart/order session equivalent
- `NewsletterSubscriber.StoreId`
- `SeoSettings.StoreId`
- `SeoRedirect.StoreId`
- `AppUserStore` customer membership if needed

Smartstore-style generic `StoreMapping` should be a later choice only if BlazorShop needs shared products/categories across multiple stores.

## Application Layer Design

Add a Commerce Store slice under `BlazorShop.Application`:

```text
BlazorShop.Application/CommerceNode/Stores/
  CommerceStoreDtos.cs
  ICommerceStoreService.cs
  ICommerceStoreContext.cs
  ICommerceStoreDomainResolver.cs
```

Recommended DTOs:

- `CommerceStoreListQuery`
- `CreateCommerceStoreRequest`
- `UpdateCommerceStoreRequest`
- `CommerceStoreSummary`
- `CommerceStoreDetail`
- `CreateCommerceStoreDomainRequest`
- `UpdateCommerceStoreDomainRequest`
- `CommerceCurrentStore`
- `CommerceStoreOperationResult<T>`

Service responsibilities:

- Validate store key/name/domain/base URL/currency/culture.
- Create/update/archive stores.
- Add/verify/disable domains.
- Enforce one active primary domain.
- Normalize domains consistently.
- Load current store by:
  - explicit store key
  - forwarded host
  - fallback default store
- Return safe messages through existing response pattern.

## Infrastructure Design

Add to Commerce Node infrastructure:

```text
BlazorShop.Infrastructure/Data/CommerceNode/Stores/
  CommerceStoreService.cs
  CommerceStoreContext.cs
  CommerceStoreDomainResolver.cs
  CommerceStoreCache.cs
```

Responsibilities:

- EF mapping for `CommerceStore` and `CommerceStoreDomain`.
- Cache active stores/domains.
- Invalidate cache on store/domain mutation.
- Resolve domain by normalized host.
- Return a default active store when only one store exists.
- Avoid hard delete; archive or disable.

Backfill migration:

- Create default store from current `AdminSettings`.
- Store key: `default`
- Name: existing `AdminSettings.StoreName` or `BlazorShop`
- Currency/culture from `AdminSettings`
- Maintenance fields from `AdminSettings`

## API Design

### Commerce Admin API

Prefix: `api/commerce/admin/stores`

Endpoints:

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/commerce/admin/stores` | List stores. |
| `GET` | `/api/commerce/admin/stores/{publicId}` | Store detail. |
| `POST` | `/api/commerce/admin/stores` | Create store. |
| `PUT` | `/api/commerce/admin/stores/{publicId}` | Update core config. |
| `POST` | `/api/commerce/admin/stores/{publicId}/archive` | Archive store. |
| `POST` | `/api/commerce/admin/stores/{publicId}/domains` | Add domain. |
| `POST` | `/api/commerce/admin/stores/{publicId}/domains/{domainId}/verify` | Verify domain. |
| `POST` | `/api/commerce/admin/stores/{publicId}/domains/{domainId}/disable` | Disable domain. |
| `POST` | `/api/commerce/admin/stores/{publicId}/domains/{domainId}/primary` | Set primary domain. |

Auth:

- Use existing Commerce Node `api/commerce/*` node key/secret/IP middleware.
- Return existing `success/message/data` response envelope.
- Keep HTTP status meaningful.

### Storefront Internal API

Prefix: `api/internal/store`

Endpoints:

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/internal/store/current` | Return current store config for Storefront. |
| `GET` | `/api/internal/store/maintenance` | Optional focused maintenance result. |

Current-store input:

- MVP: Storefront sends `X-Store-Key` from config.
- Next: Storefront sends `X-Forwarded-Host` or `X-Store-Host`.
- Later: full host resolver from request domain.

## UI Design

### Control Plane Web

Existing `Stores` page remains platform registry.

Enhance later:

- Show whether a StoreRegistry has a matching CommerceNode `CommerceStore`.
- Add action: `Open Commerce Store Config`.
- Add action: `Sync/create Commerce Store on node`.
- Show domain verification status from Control Plane and runtime domain status from Commerce Node separately.

### Commerce Admin UI

There is no dedicated Commerce Admin Web project yet. Until then:

- Control Plane can expose store config via proxy/action if needed.
- Or defer UI and test through API first.

Future Store UI sections:

- General: name, store key, status, display order.
- Domains: primary domain, aliases, status.
- URLs: base URL, force HTTPS, CDN host.
- Branding: logo/favicon/icon URLs.
- Localization: default currency, default culture.
- Support: support email, phone.
- Maintenance: enabled, message.
- Advanced: metadata JSON, HTML body id.

## Phase Plan

### Phase 1 - Store Business Baseline Plan

- [x] Compare Smartstore Store properties and behaviors.
- [x] Identify BlazorShop current store gaps.
- [x] Decide boundary:
  - ControlPlane `StoreRegistry` stays platform registry.
  - CommerceNode `CommerceStore` becomes runtime business store.
- [x] Review and approve this plan.

Stop gate:

- Plan is accepted before database changes.

### Phase 2 - Task Orchestration Dependency Gate

- [x] Review and approve `BlazorShop.CommerceNode.TaskOrchestration.todo.md`.
- [x] Implement minimum task schema: `commerce_task`, `commerce_task_step`, `store_deployment`.
- [x] Implement minimum task API: enqueue, detail, cancel.
- [x] Implement minimum worker and handler dispatch.
- [x] Implement `store.create_and_deploy` placeholder handler before wiring real Docker/Nginx work.
- [x] Confirm ControlPlane does not create any local task table.

Stop gate:

- Store deployment work does not start until CommerceNode task foundation exists.

### Phase 3 - Commerce Store Domain Model And Migration

- [x] Add `CommerceStore` entity.
- [x] Add `CommerceStoreDomain` entity.
- [x] Add `DbSet`s to `CommerceNodeDbContext`.
- [x] Configure EF mappings, indexes, constraints.
- [x] Create migration `CommerceNodeStoreExpansion`.
- [x] Backfill default store from `AdminSettings`.
- [x] Add model tests for indexes/constraints.

2026-07-09 verification: EF model build passed and `dotnet ef database update --context CommerceNodeDbContext` applied `CommerceNodeStoreExpansion` against the CommerceNode PostgreSQL dev database. Existing EF migration constraints cover uniqueness/check behavior for this phase.

Stop gate:

- Clean Commerce Node DB migrates from empty and existing dev DB.

### Phase 4 - Commerce Store Service

- [x] Add Application DTOs/contracts.
- [x] Add store validation:
  - store key format
  - name required/max length
  - base URL format
  - domain normalization
  - currency code format
  - culture format
  - support email format
- [x] Add create/update/archive store.
- [x] Add add/verify/disable/set-primary domain.
- [x] Add cache invalidation.
- [x] Add service tests.

2026-07-09 verification: service compiles against CommerceNode API and follows EF uniqueness/check constraints. API-level QA is added in Phase 6.

Stop gate:

- Store CRUD/domain behavior works without touching catalog/order code.

### Phase 5 - Current Store Resolution

- [x] Add `ICommerceStoreContext`.
- [x] Add `ICommerceStoreDomainResolver`.
- [x] Resolve by explicit `X-Store-Key`.
- [x] Resolve by forwarded host/header.
- [x] Fallback to the single active store only when exactly one active store exists.
- [x] Return clear error when no store can be resolved.
- [x] Add tests for:
  - valid store key
  - valid host
  - unknown host
  - multiple active stores without explicit key
  - archived/disabled store

2026-07-09 verification: resolver behavior is implemented in `CommerceStoreDomainResolver`; endpoint QA is added in Phase 7.

Stop gate:

- Commerce Node can identify current store deterministically for every Storefront request.

### Phase 6 - Commerce Admin Store API

- [ ] Add `CommerceStoresController`.
- [ ] Add list/detail/create/update/archive endpoints.
- [ ] Add domain endpoints.
- [ ] Apply existing Commerce Node admin response pattern.
- [ ] Apply node key/secret/IP auth.
- [ ] Add API tests.

Stop gate:

- Store can be managed through `api/commerce/admin/stores`.

### Phase 7 - Storefront Current Store API

- [ ] Add `StorefrontStoreController`.
- [ ] Add `GET api/internal/store/current`.
- [ ] Return public-safe store config:
  - name
  - base URL
  - primary domain
  - logo/icon URLs
  - currency/culture
  - maintenance state
- [ ] Add maintenance/noindex behavior for Storefront V2.
- [ ] Add Storefront V2 client support.
- [ ] Add smoke tests.

Stop gate:

- Storefront V2 can display current store identity from Commerce Node.

### Phase 8 - Replace Global AdminSettings Store Fields

- [ ] Keep `AdminSettings` for order/notification/system settings.
- [ ] Move store name/support/currency/culture/maintenance reads to `CommerceStore`.
- [ ] Keep compatibility mapping temporarily for existing settings API.
- [ ] Update `CommerceAdminSettingsController` to either:
  - redirect store settings to `CommerceStoresController`, or
  - expose only non-store settings.
- [ ] Add regression tests.

Stop gate:

- There is one runtime source of truth for store config: `CommerceStore`.

### Phase 9 - Store Scope Catalog MVP

- [ ] Add `StoreId` to Category.
- [ ] Add `StoreId` to Product.
- [ ] Add `StoreId` to SEO settings/redirects if needed.
- [ ] Backfill existing catalog to default store.
- [ ] Apply current-store filter to Storefront catalog APIs.
- [ ] Ensure admin APIs require explicit/current store.
- [ ] Add tests that product/category from store A cannot leak into store B.

Stop gate:

- Storefront catalog is store-isolated for basic Product/Category reads.

### Phase 10 - Orders, Cart, Customer Store Scope

- [ ] Add `StoreId` to orders.
- [ ] Add store context to checkout/order creation.
- [ ] Add customer-store membership if required.
- [ ] Add newsletter store scope.
- [ ] Add order history filter by current store.
- [ ] Add tests for cross-store order isolation.

Stop gate:

- Customer/cart/order data is store-isolated.

### Phase 11 - Control Plane Integration

- [ ] Extend Control Plane store detail to show runtime Commerce Store status.
- [ ] Add action to enqueue `store.create_and_deploy` on assigned node.
- [ ] Add action to query task status from CommerceNode API.
- [ ] Add action to request task cancel/retry from CommerceNode API.
- [ ] Add action to verify runtime domain.
- [ ] Record audit logs for sync actions.
- [ ] Do not make Control Plane query Commerce Node DB directly.
- [ ] Do not store CommerceNode task state in ControlPlane DB.

Stop gate:

- Control Plane remains platform registry, Commerce Node remains runtime owner.

## QA Checklist

- [ ] Clean Commerce Node DB migration.
- [ ] Existing dev DB migration with default store backfill.
- [ ] Store CRUD through API.
- [ ] Domain uniqueness.
- [ ] Primary domain uniqueness.
- [ ] Unknown host returns safe error.
- [ ] Single active store fallback works.
- [ ] Multiple active stores require explicit key/host.
- [ ] Maintenance mode returns safe Storefront state.
- [ ] Storefront V2 loads current store config.
- [ ] Product/category does not leak between stores after Phase 8.
- [ ] Order/cart does not leak between stores after Phase 9.

## Risks

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Mixing ControlPlane StoreRegistry with CommerceStore | Confusing ownership and future bugs | Keep separate names/tables/APIs. |
| Adding generic StoreMapping too early | Over-engineering MVP | Use direct `StoreId` first, defer mapping. |
| Moving AdminSettings too abruptly | Breaks migrated admin settings API | Compatibility phase before removing fields. |
| Host resolution ambiguity | Wrong store data served | Deterministic resolution rules and tests. |
| Multi-store data leaks | High severity | Add store-scope tests before enabling multi-store catalog/order. |

## Open Questions To Close

1. For MVP Storefront V2, should current store be resolved by `X-Store-Key` config first, or by public host first after Nginx is wired?
2. Should catalog multi-store use direct `StoreId` first, or do we need Smartstore-style many-to-many `StoreMapping` immediately?
3. Should logo/favicon fields be URL strings now, or integrate with media/file upload immediately?
4. Should `DefaultCurrencyCode` remain string for MVP, or should currency become a normalized table before store expansion?
5. Should `AdminSettings.StoreName/DefaultCurrency/Maintenance` be removed later or kept as node-level defaults?
6. What minimum Storefront container health contract is required before activating a store?

## Recommended Decisions

1. ControlPlane store creation should enqueue `store.create_and_deploy` through CommerceNode API when deployment is requested.
2. Use `X-Store-Key` for MVP Storefront V2, then host resolution.
3. Use direct `StoreId` for Product/Category/Order first.
4. Use URL strings for store branding assets first.
5. Keep currency as 3-letter string first.
6. Keep `AdminSettings` temporarily, then migrate store fields into `CommerceStore`.
7. Keep task status only in CommerceNode DB and query it from ControlPlane through CommerceNode API.
