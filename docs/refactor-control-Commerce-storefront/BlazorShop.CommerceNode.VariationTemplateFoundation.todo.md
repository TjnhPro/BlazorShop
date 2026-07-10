# BlazorShop CommerceNode Variation Template Foundation Todo

Status: draft
Created: 2026-07-10
Scope: CommerceNode product type expansion, reusable variation templates, StorefrontV2 product detail options, and selected attribute snapshot support.

## Goal

Add a lightweight variation template foundation for POD-style products without breaking the existing `ProductVariant` path.

The target is:

- products can remain simple;
- products can still use the existing variant inventory model;
- products can use a new reusable variation template model where options/values are display choices only;
- StorefrontV2 receives active `name/value` options for custom variation products;
- cart/order stores selected attributes as a simple snapshot.

This phase must be completed before Product Import Task because product import will reference `variation_template_slug`.

## Locked Decisions

- Do not remove `ProductVariant`.
- Do not rewrite existing variant inventory code in this phase.
- Add a product type discriminator.
- MVP product types:
  - `Simple`
  - `VariantInventory`
  - `CustomVariations`
- `Simple` keeps current product-level price/quantity behavior.
- `VariantInventory` keeps the current `ProductVariant` behavior with per-variant SKU/stock/price.
- `CustomVariations` uses `Product.VariationTemplateId`.
- `CustomVariations` does not create or copy `ProductVariant` rows.
- `CustomVariations` uses product-level SKU, price, and quantity.
- Variation Template is an independent store-scoped reference entity.
- Updating template option/value display text should update future product display because products reference the template.
- Do not hard delete templates/options/values while referenced.
- Allow hard delete only when there is no product reference and no dependent reference.
- Storefront receives only active option/value `name` and `value`.
- Storefront does not need option/value IDs, slugs, or keys in MVP.
- Cart/order selected attributes are stored as array items with `name` and `value`.
- Backend validates selected attribute shape and limits, but does not validate membership against template.
- Maximum selected attributes per cart/order item: 5.
- Do not use `AppDbContext`.
- Use `CommerceNodeDbContext` and CommerceNode migrations only.
- `BlazorShop.ControlPlane.Web` never calls `BlazorShop.CommerceNode.API` directly.
- If ControlPlane UI is added, it must call ControlPlane API, and ControlPlane API proxies to CommerceNode API.

## Non Goals

- No removal of `ProductVariant`.
- No full variant inventory refactor.
- No per-combination stock for `CustomVariations`.
- No per-combination price for `CustomVariations`.
- No generated Cartesian `ProductVariant` rows.
- No validation that selected attributes belong to a template.
- No template versioning in MVP.
- No template clone workflow.
- No bulk variation import in this phase.
- No product import in this phase.
- No legacy `BlazorShop.Presentation` changes.

## Current Code Facts

- `Product` already has `Sku`, `Name`, `Slug`, `Description`, `ShortDescription`, `FullDescription`, `Price`, `ComparePrice`, `Quantity`, `CategoryId`, `StoreId`, `IsPublished`, and `Variants`.
- `ProductVariant` already has `ProductId`, `Sku`, `AttributesJson`, `AttributeSignature`, `DisplayName`, `SizeValue`, `Color`, `Price`, `Stock`, and `IsDefault`.
- `OrderLine` already has `ProductVariantId` and `VariantAttributesJson`.
- Product admin routes are under `api/commerce/admin/products`.
- Storefront catalog routes are under `api/internal/catalog/*`.
- Existing catalog/search/product media docs require ControlPlane Web to call ControlPlane API only.

## Target Architecture

```text
ControlPlane.Web
  -> ControlPlane.API variation-template endpoints
    -> CommerceNode.API api/commerce/admin/variation-templates/*
      -> Application contracts/DTOs
      -> CommerceNode services
      -> CommerceNodeDbContext

StorefrontV2
  -> api/internal/catalog/products/{id or slug}
      -> product detail includes active variationTemplate name/value data
```

Product model paths:

```text
Simple
  Product price/quantity only

VariantInventory
  Product + ProductVariant rows
  Existing variant inventory path remains valid

CustomVariations
  Product + VariationTemplateId
  Product price/quantity only
  Selected attributes are stored as text snapshot in cart/order
```

## Database Design

### Existing table: `Products`

Add columns:

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `ProductType` | `varchar(64)` | no | Default `Simple`. |
| `VariationTemplateId` | `uuid` | yes | Required by app validation when `ProductType=CustomVariations`. |

Indexes:

- `IX_Products_StoreId_ProductType`
- `IX_Products_VariationTemplateId`

Recommended validation:

- App/service validation should enforce allowed product types.
- App/service validation should enforce `VariationTemplateId` when product type is `CustomVariations`.
- Do not add a PostgreSQL enum.

### New table: `VariationTemplates`

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `Id` | `uuid` | no | Primary key. |
| `PublicId` | `uuid` | no | Public admin API id. |
| `StoreId` | `uuid` | no | Store scope. |
| `Name` | `varchar(160)` | no | Display/admin name. |
| `Slug` | `varchar(160)` | no | Unique per store, used by CSV import. |
| `IsActive` | `boolean` | no | Inactive templates cannot be assigned by import/create. |
| `CreatedAt` | `timestamp with time zone` | no | Default current timestamp. |
| `UpdatedAt` | `timestamp with time zone` | no | Updated by service. |

Indexes:

- unique `(StoreId, Slug)`
- unique `PublicId`
- `(StoreId, IsActive)`

### New table: `VariationTemplateOptions`

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `Id` | `uuid` | no | Primary key. |
| `PublicId` | `uuid` | no | Public admin API id. |
| `TemplateId` | `uuid` | no | FK to `VariationTemplates`. |
| `Name` | `varchar(100)` | no | Example: `Color`, `Size`. |
| `SortOrder` | `integer` | no | UI order. |
| `IsActive` | `boolean` | no | Inactive option hidden from Storefront. |
| `CreatedAt` | `timestamp with time zone` | no | Default current timestamp. |
| `UpdatedAt` | `timestamp with time zone` | no | Updated by service. |

Indexes:

- unique `(TemplateId, Name)`
- unique `PublicId`
- `(TemplateId, SortOrder)`

### New table: `VariationTemplateValues`

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `Id` | `uuid` | no | Primary key. |
| `PublicId` | `uuid` | no | Public admin API id. |
| `OptionId` | `uuid` | no | FK to `VariationTemplateOptions`. |
| `Value` | `varchar(200)` | no | Example: `Red`, `XL`. |
| `SortOrder` | `integer` | no | UI order. |
| `IsActive` | `boolean` | no | Inactive value hidden from Storefront. |
| `CreatedAt` | `timestamp with time zone` | no | Default current timestamp. |
| `UpdatedAt` | `timestamp with time zone` | no | Updated by service. |

Indexes:

- unique `(OptionId, Value)`
- unique `PublicId`
- `(OptionId, SortOrder)`

## Application Contracts and DTOs

Add DTOs under an Application CommerceNode catalog/admin namespace.

Recommended DTO groups:

- `VariationTemplateListItem`
- `VariationTemplateDetail`
- `VariationTemplateOptionDto`
- `VariationTemplateValueDto`
- `CreateVariationTemplateRequest`
- `UpdateVariationTemplateRequest`
- `CreateVariationTemplateOptionRequest`
- `UpdateVariationTemplateOptionRequest`
- `CreateVariationTemplateValueRequest`
- `UpdateVariationTemplateValueRequest`
- `ProductVariationTemplateDto` for Storefront product detail
- `SelectedAttributeDto` with `Name`, `Value`

Product DTO changes:

- Admin product DTOs include `ProductType`, `VariationTemplateId/PublicId`, `VariationTemplateSlug`.
- Storefront product detail DTO includes:
  - `ProductType`
  - `VariationTemplate` only when product type is `CustomVariations` and template is active.

## Service Design

Add `IVariationTemplateService` for Commerce admin.

Responsibilities:

- List templates scoped by current store.
- Create template.
- Get template detail.
- Update template.
- Delete template only when no product references it.
- Create/update option.
- Disable option through update.
- Create/update value.
- Disable value through update.
- Ensure names are trimmed.
- Ensure slug is generated/normalized consistently with existing slug service.
- Enforce unique slug per store.
- Enforce no hard delete if referenced.

Product service changes:

- Accept and validate `ProductType`.
- Accept and validate `VariationTemplateId` or `VariationTemplateSlug` depending on API shape.
- For `CustomVariations`, require active template.
- For `Simple`, clear `VariationTemplateId` unless explicitly preserving on update is required.
- For `VariantInventory`, keep existing `ProductVariant` behavior.

Storefront catalog service changes:

- Product detail query loads active variation template data when product type is `CustomVariations`.
- Storefront response includes only active options/values ordered by `SortOrder`.

Cart/order changes:

- Add request support for `selectedAttributes` array.
- Validate:
  - max 5 attributes;
  - `name` required, max 100;
  - `value` required, max 200;
  - trim text;
  - no template membership validation.
- Store snapshot in `OrderLine.VariantAttributesJson`.
- `ProductVariantId` remains null for `CustomVariations`.

## CommerceNode API Design

Admin routes:

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/commerce/admin/variation-templates` | List templates. |
| `POST` | `/api/commerce/admin/variation-templates` | Create template. |
| `GET` | `/api/commerce/admin/variation-templates/{id}` | Get detail. |
| `PUT` | `/api/commerce/admin/variation-templates/{id}` | Update template. |
| `DELETE` | `/api/commerce/admin/variation-templates/{id}` | Delete only if unreferenced. |
| `POST` | `/api/commerce/admin/variation-templates/{id}/options` | Add option. |
| `PUT` | `/api/commerce/admin/variation-templates/{id}/options/{optionId}` | Update/disable option. |
| `POST` | `/api/commerce/admin/variation-templates/{id}/options/{optionId}/values` | Add value. |
| `PUT` | `/api/commerce/admin/variation-templates/{id}/options/{optionId}/values/{valueId}` | Update/disable value. |

Product routes:

- Existing product create/update endpoints should accept product type fields.
- Existing product detail endpoints should return product type fields.

Storefront routes:

- Existing product detail routes should return `variationTemplate` for `CustomVariations`.
- Do not add a separate Storefront variation endpoint in MVP unless product detail becomes too heavy.

## ControlPlane API and UI Plan

ControlPlane Web must not call CommerceNode API directly.

If UI is implemented in this phase:

- Add ControlPlane API proxy routes for variation templates.
- Add ControlPlane Web page or catalog tab:
  - list templates;
  - create template;
  - edit name/slug/active state;
  - manage options;
  - manage values;
  - copy `variation_template_slug` for CSV import.

UI is intentionally admin-focused and compact. It is not a marketing page.

## Storefront UI Plan

For product detail:

- If product type is `CustomVariations`, render template options as selectors.
- Use active `name/value` only.
- Submit selected attributes as:

```json
[
  { "name": "Color", "value": "Red" },
  { "name": "Size", "value": "XL" }
]
```

Do not show IDs/slugs to Storefront UI.

## Phase 1 - Schema Foundation

Tasks:

- Add variation template entities.
- Add `Product.ProductType`.
- Add `Product.VariationTemplateId`.
- Add `DbSet` entries to `CommerceNodeDbContext`.
- Configure indexes, max lengths, relationships, and delete behavior.
- Add CommerceNode migration.

Acceptance:

- Migration creates template tables.
- Migration adds product type/template reference.
- Existing products default to `Simple`.
- Existing `ProductVariant` schema remains unchanged.

## Phase 2 - Application Contracts and Service

Tasks:

- Add DTOs.
- Add `IVariationTemplateService`.
- Implement CommerceNode service.
- Enforce store scope.
- Enforce slug uniqueness.
- Enforce no hard delete while referenced.
- Add audit log actions:
  - `VariationTemplate.Created`
  - `VariationTemplate.Updated`
  - `VariationTemplate.Deleted`
  - `VariationTemplate.OptionUpdated`
  - `VariationTemplate.ValueUpdated`

Acceptance:

- Service returns existing response envelope style.
- Invalid duplicate slug returns validation/conflict.
- Referenced template delete returns conflict.

## Phase 3 - CommerceNode Admin API

Tasks:

- Add `CommerceVariationTemplatesController`.
- Add list/create/detail/update/delete routes.
- Add option/value create/update routes.
- Keep routes under `api/commerce/admin/variation-templates`.

Acceptance:

- Routes use Commerce admin auth/security.
- Routes are store-scoped.
- Routes use current API response pattern.

## Phase 4 - Product Integration

Tasks:

- Add product type constants.
- Update product create/update DTOs and services.
- Validate `CustomVariations` requires active `VariationTemplateId`.
- Keep `VariantInventory` path compatible with existing `ProductVariant` rows.
- Update product admin/detail DTO mapping.

Acceptance:

- Existing product flows still work as `Simple` or `VariantInventory`.
- `CustomVariations` product cannot be saved without a valid active template.
- No `ProductVariant` rows are generated for `CustomVariations`.

## Phase 5 - Storefront Product Detail

Tasks:

- Update Storefront product detail DTO.
- Load active template options/values for `CustomVariations`.
- Return only active `name/value`, sorted by option/value sort order.
- Do not expose option/value IDs/slugs in Storefront response.

Acceptance:

- Simple product detail is unchanged except for optional product type field.
- Custom variation product detail returns variation options.
- Inactive template/options/values are not returned to Storefront.

## Phase 6 - Cart and Order Snapshot

Tasks:

- Add `selectedAttributes` to cart/add and checkout paths as needed.
- Validate shape and limits:
  - max 5 attributes;
  - name required, max 100;
  - value required, max 200.
- Do not validate membership against template.
- Store snapshot in `VariantAttributesJson`.
- Keep `ProductVariantId` null for `CustomVariations`.

Acceptance:

- Custom variation selection persists to order line snapshot.
- Existing `VariantInventory` order lines still support `ProductVariantId`.
- Existing Simple product cart/order flow still works.

## Phase 7 - ControlPlane UI/API Proxy

Tasks:

- Add ControlPlane API proxy endpoints if admin UI is implemented in this round.
- Add ControlPlane UI for template CRUD and option/value editing.
- Add copy slug affordance for CSV import.

Acceptance:

- ControlPlane Web calls only ControlPlane API.
- Node credentials are not exposed to browser.
- UI can create a template usable by Product Import Task.

## Phase 8 - QA Checklist

Add cases to `QA-CommerceNode.todo.md`:

- Migration applies on clean CommerceNode database.
- Existing products default to `Simple`.
- Create variation template.
- Duplicate slug in same store is rejected.
- Same slug in another store is allowed.
- Add/update option.
- Add/update value.
- Disable option hides it from Storefront product detail.
- Disable value hides it from Storefront product detail.
- Delete unreferenced template succeeds.
- Delete referenced template fails.
- Create `CustomVariations` product without template fails.
- Create `CustomVariations` product with active template succeeds.
- Storefront product detail returns active `name/value`.
- Cart/order accepts max 5 selected attributes.
- Cart/order rejects more than 5 selected attributes.
- Cart/order stores selected attributes in `VariantAttributesJson`.
- Existing `ProductVariant` endpoints still work.
- Existing checkout/order flow for `VariantInventory` still works.

## Deferred

- Template versioning.
- Template cloning.
- Option/value hard delete UX.
- Per-combination price.
- Per-combination stock.
- Generated variant rows.
- Product import.
- Variation template import/export.
