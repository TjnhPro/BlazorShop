# BlazorShop Commerce Node Catalog Expansion Todo

Status: draft
Created: 2026-07-10
Scope source: `BlazorShop.CommerceNode.CatalogExpansion.proposal.csv`

## Goal

Expand Commerce Node catalog just enough for a usable ecommerce V2 foundation without making the codebase heavy.

This plan uses only rows marked `Select=yes` in `BlazorShop.CommerceNode.CatalogExpansion.proposal.csv`.

The target is not a full Smartstore clone. The target is a small, layered, store-scoped catalog foundation that improves:

- product admin data quality;
- category tree/menu behavior;
- variant combination support for apparel-style products;
- storefront product filtering;
- checkout stock correctness;
- order history stability through snapshots.

## Scope Rules

- Keep using Layered Architecture style.
- Keep `BlazorShop.Presentation` legacy untouched.
- Do not use `AppDbContext`.
- Use `CommerceNodeDbContext` and `CommerceNodeConnection`.
- Reuse existing Application services, DTOs, repository abstractions, API response envelope, audit service, and store context.
- Preserve existing routes where possible.
- Add new endpoints instead of changing response shape when existing consumers may break.
- Commit each completed phase separately when implementation starts.

## Non Goals

- No preorder/backorder.
- No import/export in this round.
- No bundle/grouped/downloadable/gift card product type.
- No full media library.
- No product image gallery in this round.
- No variant matrix generator in this round.
- No full Smartstore-style variant engine.
- No discount/coupon/tier-price engine.
- No tax/shipping calculation model.
- No full text search/search suggestion.
- No generic `StoreMapping`; keep direct `StoreId`.

## Current State

Commerce Node already has:

- `Product` with name, description, price, image, quantity, slug, SEO fields, publish fields, `StoreId`, category, variants.
- `Category` with name, slug, SEO fields, publish field, `StoreId`.
- `ProductVariant` with SKU, size scale/value, price override, stock, color, default flag.
- `Order` and `OrderLine` with basic total, status, store, shipping tracking, line product id, quantity, unit price.
- `CommerceNodeProductReadRepository` with store-scoped storefront catalog queries.
- `CommerceNodeCategoryRepository` with store-scoped published category queries.
- `CommerceNodeAdminInventoryService` with product/variant stock listing and stock updates.
- Admin routes under `api/commerce/admin/*`.
- Storefront private routes under `api/internal/*`.

Known gaps:

- Product has no SKU at product level.
- Product description is one field, so listing/detail content cannot be tuned separately.
- Category has no parent-child tree.
- Product variants are hard-coded around size/color fields instead of attribute combinations.
- Checkout does not validate/deduct stock enough for real ecommerce.
- `OrderLine` does not snapshot product/variant details, so historical orders can change when product data changes.
- Admin product list is currently `GetAll`, which will become heavy as catalog grows.

## Autoplan Decisions

| Decision | Result | Reason |
| --- | --- | --- |
| Expand catalog in one controlled round | Yes | The selected scope is small enough and mostly extends existing entities/services. |
| Add product/category fields directly | Yes | Current model is simple; a direct schema extension has lower cost than new subsystems. |
| Variant attributes storage | Use JSON attributes + normalized signature | Supports `Color=Red`, `Size=XL` without a full attribute engine. |
| Variant matrix generator | Defer | Useful later, but not needed for correctness. |
| Product media gallery | Defer | Main image/category image is enough for this round. |
| Import/export | Defer | Needs validation, idempotency, import history, and rollback. |
| Order snapshot | Include minimal snapshot now | Prevents order history from depending on mutable product rows. |
| Stock deduction | Include minimal stock deduction now | Required for ecommerce correctness. |

## Target Architecture

```text
ControlPlane
  -> api/commerce/admin/* on CommerceNode
      -> Application services
      -> CommerceNode repositories/services
      -> CommerceNodeDbContext

StorefrontV2
  -> api/internal/catalog/*
  -> api/internal/cart/*
  -> api/internal/orders/*
      -> store context resolves CommerceStore
      -> catalog/order queries filter by StoreId
```

CatalogExpansion stays inside Commerce Node. Control Plane should not own product/category/order data.

## Database Design

### Existing table: `Products`

Add columns:

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `Sku` | `varchar(64)` | yes | Product-level SKU for admin/search/order snapshot. |
| `ShortDescription` | `text` | yes | Listing/card description. |
| `FullDescription` | `text` | yes | Product detail description. Existing `Description` remains for compatibility. |
| `ComparePrice` | `decimal(18,2)` | yes | Simple sale/list price display, no discount engine. |
| `DisplayOrder` | `integer` | no | Default `0`. |
| `UpdatedAt` | `timestamp with time zone` | no | Set by service on create/update. |
| `ArchivedAt` | `timestamp with time zone` | yes | Soft delete marker. |

Indexes:

- index `StoreId`
- index `(StoreId, Sku)` where `StoreId is not null and Sku is not null and ArchivedAt is null`
- index `(StoreId, CategoryId, DisplayOrder, CreatedOn)`
- index `(StoreId, IsPublished, ArchivedAt)`
- keep existing unique `(StoreId, Slug)` but update filter to ignore archived rows if possible:
  - `StoreId is not null and Slug is not null and ArchivedAt is null`

Migration notes:

- Keep existing `Description`.
- Copy existing `Description` into `FullDescription` where `FullDescription` is null.
- Existing products get `DisplayOrder = 0`.
- Existing products get `UpdatedAt = CreatedOn` when possible.
- Do not make `Sku` required in the first migration; enforce through validation after UI/API is ready.

### Existing table: `Categories`

Add columns:

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `ParentCategoryId` | `uuid` | yes | FK to `Categories.Id`, same store only enforced in service. |
| `Image` | `text` | yes | Category image URL/string first. |
| `DisplayOrder` | `integer` | no | Default `0`. |
| `UpdatedAt` | `timestamp with time zone` | no | Set by service. |
| `ArchivedAt` | `timestamp with time zone` | yes | Soft delete marker. |

Indexes:

- index `StoreId`
- index `(StoreId, ParentCategoryId, DisplayOrder)`
- index `(StoreId, IsPublished, ArchivedAt)`
- keep existing unique `(StoreId, Slug)` but update filter to ignore archived rows if possible.

FK:

- `ParentCategoryId -> Categories.Id`
- delete behavior: `Restrict` or `SetNull`; do not cascade delete child categories.

Validation:

- Parent category must belong to current store.
- Category cannot be its own parent.
- Reject circular parent chains.
- Archive category should fail or require explicit handling if active products still reference it.

### Existing table: `ProductVariants`

Add columns:

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `AttributesJson` | `jsonb` | yes | Array of `{ "name": "Color", "value": "Red" }`. |
| `AttributeSignature` | `varchar(512)` | yes | Normalized sorted signature, e.g. `color=red|size=xl`. |
| `DisplayName` | `varchar(256)` | yes | Human display name, e.g. `Red / XL`. |

Keep existing columns for compatibility:

- `Sku`
- `SizeScale`
- `SizeValue`
- `Price`
- `Stock`
- `Color`
- `IsDefault`

Indexes:

- unique `(ProductId, AttributeSignature)` where `AttributeSignature is not null`
- unique `(ProductId)` where `IsDefault = true`
- keep current `(ProductId, SizeScale, SizeValue)` unique only if it does not conflict with migrated attributes; otherwise replace with the signature index in the same migration.

Migration notes:

- For existing variants, generate attributes from existing fields:
  - if `Color` has value: `{ "name": "Color", "value": Color }`
  - if `SizeValue` has value: `{ "name": "Size", "value": SizeValue }`
- Generate `AttributeSignature` from normalized attributes.
- Generate `DisplayName` from attribute values joined with ` / `.

Validation:

- Attributes must not be empty for new variants unless preserving an old variant.
- Attribute names and values must be trimmed.
- Attribute names are case-insensitive for uniqueness.
- Attribute order should not affect uniqueness.
- A product can have only one default variant.
- Variant must belong to a product in current store for admin operations.

### Existing table: `Orders`

Add columns:

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `CurrencyCode` | `varchar(3)` | yes | Snapshot from `CommerceStore.DefaultCurrencyCode`. |

Migration notes:

- Existing rows may use current store currency when resolvable; otherwise default to `USD`/existing config only if safe.
- Keep nullable initially to avoid breaking existing data.

### Existing table: `OrderLines`

Add columns:

| Column | Type | Nullable | Notes |
| --- | --- | --- | --- |
| `ProductName` | `text` | yes | Snapshot at purchase time. |
| `Sku` | `varchar(64)` | yes | Product or variant SKU snapshot. |
| `Image` | `text` | yes | Main product image snapshot. |
| `ProductVariantId` | `uuid` | yes | Selected variant id if any. |
| `VariantAttributesJson` | `jsonb` | yes | Snapshot of selected variant attributes. |

Migration notes:

- Do not backfill by joining products unless needed for development seed only.
- New orders must always populate snapshots.
- Admin order service should prefer snapshot fields and only fallback to product join for old rows.

## DTO And Contract Design

### Product DTOs

Extend existing DTOs without removing existing fields:

- `ProductBase`
  - `Sku`
  - `ShortDescription`
  - `FullDescription`
  - `ComparePrice`
  - `DisplayOrder`
- `GetCatalogProduct`
  - `Sku`
  - `ShortDescription`
  - `ComparePrice`
  - `DisplayOrder`
  - `UpdatedAt`
  - `InStock`
- `GetProduct`
  - all product foundation fields
  - variant attributes
  - effective variant price where useful

Compatibility rule:

- Existing `Description` remains accepted.
- If `FullDescription` is not provided, map from `Description`.
- If `ShortDescription` is not provided, derive from `Description` only for display, not by mutating data.

### Category DTOs

Extend existing DTOs:

- `ParentCategoryId`
- `Image`
- `DisplayOrder`
- `UpdatedAt`
- `Children` only for tree response DTOs.

Add tree DTO:

```csharp
public sealed class GetCategoryTreeNode
{
    public Guid Id { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Image { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; }
    public IReadOnlyList<GetCategoryTreeNode> Children { get; set; } = [];
}
```

### Variant DTOs

Add simple attribute DTO:

```csharp
public sealed class ProductVariantAttributeDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
```

Extend create/update/get variant DTOs:

- `IReadOnlyList<ProductVariantAttributeDto> Attributes`
- `string? AttributeSignature`
- `string? DisplayName`
- `decimal EffectivePrice`

Compatibility rule:

- Keep current `SizeScale`, `SizeValue`, `Color` during migration.
- New code should write `Attributes`.
- Storefront should read `Attributes` and `DisplayName`.

### Cart/Order DTOs

Extend cart item payload:

- `ProductVariantId`

Extend order line DTO:

- `ProductName`
- `Sku`
- `Image`
- `ProductVariantId`
- `VariantAttributes`
- keep `UnitPrice` and `LineTotal`.

## API Design

### Commerce Admin Products

Existing route remains:

- `GET /api/commerce/admin/products`
- `GET /api/commerce/admin/products/{id}`
- `POST /api/commerce/admin/products`
- `PUT /api/commerce/admin/products/{id}`
- `DELETE /api/commerce/admin/products/{id}`

Add paged query route to avoid breaking existing consumers:

- `GET /api/commerce/admin/products/query`

Query parameters:

- `pageNumber`
- `pageSize`
- `searchTerm`
- `categoryId`
- `minPrice`
- `maxPrice`
- `inStock`
- `sortBy`

Sort values:

- `newest`
- `oldest`
- `price_low_to_high`
- `price_high_to_low`
- `name_ascending`
- `name_descending`
- `display_order`
- `updated`

Delete behavior:

- `DELETE` should archive product by setting `ArchivedAt`, not hard delete.
- Existing behavior can be preserved behind service implementation; API response should still say product deleted/archived successfully.

### Commerce Admin Categories

Existing route remains:

- `GET /api/commerce/admin/categories`
- `GET /api/commerce/admin/categories/{id}`
- `POST /api/commerce/admin/categories`
- `PUT /api/commerce/admin/categories/{id}`
- `DELETE /api/commerce/admin/categories/{id}`

Add:

- `GET /api/commerce/admin/categories/tree`

Delete behavior:

- Archive category by setting `ArchivedAt`.
- Reject archive if active child categories or active products would become orphaned unless explicit handling is added.

### Commerce Admin Product Variants

Existing route remains:

- `GET /api/commerce/admin/products/{productId}/variants`
- `POST /api/commerce/admin/products/{productId}/variants`
- `PUT /api/commerce/admin/products/{productId}/variants/{variantId}`
- `DELETE /api/commerce/admin/products/{productId}/variants/{variantId}`

Payload includes:

```json
{
  "sku": "TSHIRT-RED-XL",
  "attributes": [
    { "name": "Color", "value": "Red" },
    { "name": "Size", "value": "XL" }
  ],
  "price": 19.99,
  "stock": 10,
  "isDefault": false
}
```

Rules:

- API computes `attributeSignature`.
- API computes `displayName`.
- API rejects duplicate attribute combinations.
- API rejects variant updates across stores.

### Storefront Catalog

Existing route remains:

- `GET /api/internal/catalog/categories`
- `GET /api/internal/catalog/categories/{id}`
- `GET /api/internal/catalog/categories/slug/{slug}`
- `GET /api/internal/catalog/categories/{categoryId}/products`
- `GET /api/internal/catalog/products`
- `GET /api/internal/catalog/products/{id}`
- `GET /api/internal/catalog/products/slug/{slug}`
- `GET /api/internal/catalog/sitemap`

Add:

- `GET /api/internal/catalog/categories/tree`

Extend product listing query:

- `minPrice`
- `maxPrice`
- `inStock`
- `sortBy=display_order`
- `sortBy=updated`

Product detail response:

- include variant attributes;
- include effective price;
- include default variant;
- do not expose archived products/categories.

Sitemap:

- use `UpdatedAt` when available;
- fallback to `PublishedOn`;
- fallback to `CreatedOn`.

### Storefront Cart And Orders

Existing routes remain:

- `POST /api/internal/cart/checkout`
- `POST /api/internal/cart/save-checkout`
- `POST /api/internal/orders/confirm`

Payload extension:

```json
{
  "productId": "uuid",
  "productVariantId": "uuid-or-null",
  "quantity": 1
}
```

Rules:

- Checkout validates product belongs to current store.
- If `productVariantId` is supplied, variant must belong to product.
- If product has variants and no variant is supplied, use default variant only if exactly one valid default exists.
- Reject checkout if requested quantity exceeds available product/variant stock.
- On confirmed order, deduct stock.
- Order line must snapshot product and variant data.

## UI Design

Use existing `BlazorShop.ControlPlane.Web` / Commerce admin style. Do not create a new design system.

### Product List

Add columns:

- Name
- SKU
- Category
- Price
- Compare Price
- Stock
- Published
- Updated At

Controls:

- search input;
- category filter;
- price range inputs;
- in-stock filter;
- sort select;
- pagination.

### Product Create/Edit

Keep one practical form, not many tabs in this round.

Fields:

- Name
- SKU
- Short Description
- Full Description
- Price
- Compare Price
- Main Image URL
- Category
- Display Order
- Published

### Variant Inline Editor

On product detail/edit page:

- list variants;
- edit SKU;
- edit attributes array as rows:
  - attribute name
  - attribute value
- edit price override;
- edit stock;
- set default variant.

No matrix generator in this round.

### Category Tree

Add admin category tree page:

- show parent/child tree;
- create category with optional parent;
- edit display order;
- edit image URL;
- publish/unpublish;
- archive.

### Inventory View

Reuse existing inventory service/page direction:

- show product stock;
- show variant stock;
- show low-stock/out-of-stock state;
- update product/variant stock;
- verify checkout stock behavior in QA.

## Phase Plan

### Phase 0 - Scope Lock

- [ ] Confirm `BlazorShop.CommerceNode.CatalogExpansion.proposal.csv` is the source of selected scope.
- [ ] Do not implement rows with `Select=no`.
- [ ] Confirm no changes are needed in legacy `BlazorShop.Presentation`.
- [ ] Confirm no import/export, discount, tax, shipping, media library, or variant matrix generator enters this round.

Stop gate:

- [ ] Plan scope matches only selected `yes` rows.

Commit:

```text
docs: add commerce node catalog expansion plan
```

### Phase 1 - Database And Domain Model

- [ ] Add product fields:
  - [ ] `Sku`
  - [ ] `ShortDescription`
  - [ ] `FullDescription`
  - [ ] `ComparePrice`
  - [ ] `DisplayOrder`
  - [ ] `UpdatedAt`
  - [ ] `ArchivedAt`
- [ ] Add category fields:
  - [ ] `ParentCategoryId`
  - [ ] `Image`
  - [ ] `DisplayOrder`
  - [ ] `UpdatedAt`
  - [ ] `ArchivedAt`
- [ ] Add variant fields:
  - [ ] `AttributesJson`
  - [ ] `AttributeSignature`
  - [ ] `DisplayName`
- [ ] Add order snapshot fields:
  - [ ] `Order.CurrencyCode`
  - [ ] `OrderLine.ProductName`
  - [ ] `OrderLine.Sku`
  - [ ] `OrderLine.Image`
  - [ ] `OrderLine.ProductVariantId`
  - [ ] `OrderLine.VariantAttributesJson`
- [ ] Update `CommerceNodeDbContext` mappings and indexes.
- [ ] Create Commerce Node migration.
- [ ] Keep migration backward-compatible with existing data.

Stop gate:

- [ ] Migration applies on a clean Commerce Node DB.
- [ ] Existing seed/dev data can still load.
- [ ] Existing routes compile without DTO changes yet.

Commit:

```text
feat(commerce-node): expand catalog domain schema
```

### Phase 2 - DTOs, Mapping, Validation Helpers

- [ ] Extend product DTOs with selected fields.
- [ ] Extend category DTOs with tree fields.
- [ ] Add `GetCategoryTreeNode`.
- [ ] Add `ProductVariantAttributeDto`.
- [ ] Extend variant DTOs with attributes, signature, display name, effective price.
- [ ] Extend cart/order DTOs with `ProductVariantId` and variant snapshots.
- [ ] Update AutoMapper mappings.
- [ ] Add helper to normalize variant attributes:
  - [ ] trim names/values;
  - [ ] reject empty names/values;
  - [ ] sort attributes by normalized name;
  - [ ] generate signature;
  - [ ] generate display name.
- [ ] Preserve compatibility with existing `SizeScale`, `SizeValue`, `Color`.

Stop gate:

- [ ] Existing Application tests compile.
- [ ] Old product/category/variant payloads still work where required.
- [ ] New variant attribute payloads validate consistently.

Commit:

```text
feat(commerce-node): add catalog expansion contracts
```

### Phase 3 - Product And Category Services

- [ ] Update `ProductService` create/update:
  - [ ] set `StoreId` from current store;
  - [ ] set `UpdatedAt`;
  - [ ] normalize description fields;
  - [ ] validate SKU uniqueness per store when SKU is provided;
  - [ ] archive instead of hard delete.
- [ ] Update `CategoryService` create/update:
  - [ ] set `StoreId` from current store;
  - [ ] set `UpdatedAt`;
  - [ ] validate parent category same store;
  - [ ] reject circular parent chain;
  - [ ] archive instead of hard delete.
- [ ] Add category tree query service/repository method.
- [ ] Update audit metadata to include SKU/category parent/display order where useful.

Stop gate:

- [ ] Product CRUD still returns response envelope.
- [ ] Category CRUD still returns response envelope.
- [ ] Archive behavior does not expose archived data in storefront queries.

Commit:

```text
feat(commerce-node): expand product and category services
```

### Phase 4 - Catalog Query And APIs

- [ ] Extend `ProductCatalogQuery`:
  - [ ] `MinPrice`
  - [ ] `MaxPrice`
  - [ ] `InStock`
  - [ ] sort by display order;
  - [ ] sort by updated at.
- [ ] Update `CommerceNodeProductReadRepository`:
  - [ ] search by product SKU;
  - [ ] apply price range filter;
  - [ ] apply in-stock filter;
  - [ ] exclude archived products;
  - [ ] exclude archived categories;
  - [ ] map new catalog fields.
- [ ] Update `CommerceNodeCategoryRepository`:
  - [ ] exclude archived categories;
  - [ ] return category tree;
  - [ ] order by display order then name.
- [ ] Add admin product paged query endpoint:
  - [ ] `GET /api/commerce/admin/products/query`
- [ ] Add admin category tree endpoint:
  - [ ] `GET /api/commerce/admin/categories/tree`
- [ ] Add storefront category tree endpoint:
  - [ ] `GET /api/internal/catalog/categories/tree`
- [ ] Extend storefront product listing query.
- [ ] Update sitemap last-modified logic.

Stop gate:

- [ ] Existing storefront catalog endpoints still work.
- [ ] New query filters are store-scoped.
- [ ] Store A cannot see Store B products/categories.

Commit:

```text
feat(commerce-node): add catalog query expansion
```

### Phase 5 - Variant Combination Light

- [ ] Update variant create/update service:
  - [ ] accept attributes array;
  - [ ] compute signature;
  - [ ] compute display name;
  - [ ] reject duplicate combination per product;
  - [ ] enforce one default variant per product;
  - [ ] validate product belongs to current store.
- [ ] Update variant read mapping:
  - [ ] return attributes;
  - [ ] return display name;
  - [ ] return effective price.
- [ ] Update inventory service:
  - [ ] display variant attributes/display name;
  - [ ] preserve SKU search;
  - [ ] validate variant belongs to current store before stock update.
- [ ] Update storefront product detail:
  - [ ] include attributes;
  - [ ] include default variant;
  - [ ] include effective price.

Stop gate:

- [ ] Product can have `Color=Red, Size=XL`.
- [ ] Product cannot have duplicate `Color=Red, Size=XL`.
- [ ] Product cannot have two default variants.
- [ ] Existing size/color variants migrated from old data still show.

Commit:

```text
feat(commerce-node): support lightweight variant combinations
```

### Phase 6 - Checkout Stock And Order Snapshot

- [ ] Extend cart process DTO with `ProductVariantId`.
- [ ] Update checkout/order creation:
  - [ ] validate product belongs to current store;
  - [ ] validate variant belongs to product and current store;
  - [ ] resolve default variant when safe;
  - [ ] reject missing variant when multiple variants exist and no default is valid;
  - [ ] reject out-of-stock product/variant;
  - [ ] compute price from variant price fallback to product price;
  - [ ] snapshot product name, SKU, image;
  - [ ] snapshot variant id and attributes;
  - [ ] snapshot currency code from current `CommerceStore`;
  - [ ] deduct product/variant stock on confirmed order.
- [ ] Update admin order mapping:
  - [ ] prefer snapshot product name;
  - [ ] prefer snapshot SKU;
  - [ ] expose variant snapshot;
  - [ ] fallback to product join only for old rows.
- [ ] Update customer order item mapping where applicable.

Stop gate:

- [ ] Changing product name after order does not change old order display.
- [ ] Changing product price after order does not change old order total/line price.
- [ ] Out-of-stock product cannot be checked out.
- [ ] Successful order deducts stock.

Commit:

```text
feat(commerce-node): add stock-safe checkout snapshots
```

### Phase 7 - Admin UI

- [x] Add ControlPlane API proxy for Commerce Node catalog admin routes so Web does not receive node secrets.
- [x] Update product list UI:
  - [x] SKU column;
  - [x] category column;
  - [x] price/compare price;
  - [x] stock;
  - [ ] published;
  - [x] updated at;
  - [x] search/filter/sort/paging.
- [x] Update product create/edit UI:
  - [x] SKU;
  - [x] short description;
  - [x] full description;
  - [x] compare price;
  - [x] image URL;
  - [x] display order;
  - [ ] publish field.
- [x] Add variant inline editor:
  - [x] SKU;
  - [x] attributes rows;
  - [x] price override;
  - [x] stock;
  - [x] default flag.
- [x] Add category tree UI:
  - [x] parent category;
  - [x] display order;
  - [x] image URL;
  - [x] publish/archive.
- [x] Update inventory UI to show attributes/display name.

Stop gate:

- [x] Admin can create product with SKU and variant attributes.
- [x] Admin can create category tree.
- [x] Admin can filter/search products without loading entire catalog.

Commit:

```text
feat(control-plane): expand commerce catalog admin UI
```

### Phase 8 - Storefront V2 UI

- [x] Update Storefront catalog client models.
- [ ] Use category tree for navigation/menu where currently suitable.
- [x] Product listing supports:
  - [x] price range;
  - [x] in-stock filter;
  - [x] display order sort;
  - [x] updated sort.
- [x] Product detail displays:
  - [x] short/full description appropriately;
  - [x] compare price when present;
  - [x] variant attribute selections;
  - [x] effective price;
  - [x] out-of-stock state.
- [x] Add cart payload with `ProductVariantId`.
- [x] Disable buy when selected product/variant is out of stock.

Stop gate:

- [ ] Storefront can browse category tree.
- [x] Storefront can select Color/Size variant.
- [x] Storefront cart sends selected variant id.

Commit:

```text
feat(storefront-v2): support expanded catalog browsing
```

### Phase 9 - Seed And QA

- [ ] Add or update clean DB seed data:
  - [ ] one active store;
  - [ ] category tree;
  - [ ] products with SKU;
  - [ ] product with variants:
    - [ ] Color = Red, Size = M;
    - [ ] Color = Red, Size = XL;
    - [ ] Color = Black, Size = M;
  - [ ] low-stock product/variant;
  - [ ] sample order with snapshot fields.
- [ ] Update `QA-CommerceNode.todo.md` with catalog expansion checklist.
- [ ] Update `QA-StorefrontV2.todo.md` with storefront variant/cart checklist.
- [ ] Run API QA on clean Commerce Node DB.
- [ ] Run StorefrontV2 QA for browse/detail/cart/checkout.
- [ ] Verify no legacy `BlazorShop.Presentation` reference is required.

Stop gate:

- [ ] Clean DB can migrate and seed.
- [ ] Product CRUD verified.
- [ ] Category tree verified.
- [ ] Variant combination verified.
- [ ] Checkout stock validation verified.
- [ ] Order snapshot verified.
- [ ] Store scope regression verified.

Commit:

```text
test(commerce-node): verify catalog expansion flows
```

## QA Checklist To Add

### Commerce Node API

- [ ] Create category parent.
- [ ] Create category child.
- [ ] Query category tree.
- [ ] Create product with SKU.
- [ ] Create product with short/full description.
- [ ] Create product with compare price.
- [ ] Query product by SKU search.
- [ ] Query products by price range.
- [ ] Query products by in-stock filter.
- [ ] Create variant with `Color=Red`, `Size=M`.
- [ ] Reject duplicate variant combination.
- [ ] Reject second default variant.
- [ ] Update variant stock.
- [ ] Verify variant belongs to current store.
- [ ] Archive product and verify storefront does not show it.
- [ ] Archive category and verify storefront does not show it.

### Storefront API

- [ ] Load category tree.
- [ ] Load product listing with filters.
- [ ] Load product detail with variants.
- [ ] Add selected variant to cart.
- [ ] Reject checkout when out of stock.
- [ ] Confirm order and deduct stock.
- [ ] Verify order line snapshots product name, SKU, image, variant attributes.

### Store Scope

- [ ] Store A cannot query Store B category.
- [ ] Store A cannot query Store B product.
- [ ] Store A cannot update Store B variant stock.
- [ ] Store A order does not include Store B product.

## Risks

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Variant JSON becomes messy | Medium | Normalize attributes and use signature for uniqueness. |
| Soft delete breaks existing admin expectations | Medium | Keep response messages compatible and document archive behavior. |
| Checkout stock deduction creates race condition | Medium | Use transaction around order creation and stock update. |
| Existing DTO consumers break | Medium | Add fields without removing old fields; add new query endpoint for paged admin products. |
| Store scope leak | High | Require store context in admin/query/update paths and add QA regression. |

## Deferred Items

These remain deliberately out of this round:

- Product CSV export/import.
- Category import.
- Image bulk import.
- Product media gallery.
- Media library.
- Specification filters.
- Manual related products.
- Recommendation tuning.
- Tier price.
- Coupon discount.
- Special price date range.
- Weight/dimensions.
- Tax category.
- Free shipping.
- Variant matrix generator.

## Final Acceptance

- [ ] All selected `yes` rows from `BlazorShop.CommerceNode.CatalogExpansion.proposal.csv` are either implemented or explicitly moved to a later reviewed plan.
- [ ] No selected `no` rows are implemented accidentally.
- [ ] Commerce Node clean DB migration succeeds.
- [ ] StorefrontV2 can browse products and select variants.
- [ ] Checkout creates stock-safe orders.
- [ ] Admin order history uses snapshots, not mutable product joins.
- [ ] QA files are updated and verified.
