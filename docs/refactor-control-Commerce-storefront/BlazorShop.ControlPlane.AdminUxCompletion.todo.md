# BlazorShop Control Plane Admin UX Completion Todo

## Goal

Hoan thien admin UX trong `BlazorShop.ControlPlane.Web` cho cac nghiep vu Commerce Node da co san, giup van hanh catalog/order bang UI thay vi phai goi API thu cong.

Muc tieu la **hoan thien trai nghiem quan tri**, khong them nghiep vu ecommerce moi.

## Locked Decisions

- `BlazorShop.ControlPlane.Web` chi goi `BlazorShop.ControlPlane.API`.
- `BlazorShop.ControlPlane.Web` khong bao gio goi truc tiep `BlazorShop.CommerceNode.API`, `api/commerce/*`, `api/internal/*`, hoac public media endpoint cua Commerce Node.
- `BlazorShop.ControlPlane.API` la gateway/proxy duy nhat sang `BlazorShop.CommerceNode.API`.
- Nhom menu trong Control Plane Web: `Commerce Admin`.
- UI dung Tailwind + FontAwesome theo style hien co.
- Detail dung right drawer. Modal chi dung cho confirm hoac form nho.
- Khong tao product thu cong. Product chi duoc create bang CSV import.
- Product detail chi edit cac field don gian/an toan.
- Khong edit `Sku`, `StoreId`, `ProductType`, `VariationTemplateId`, variant structure tu Product Detail.
- Product Media nam trong Product Detail, khong lam media library rieng.
- Product List co the lazy-load thumbnail nho qua ControlPlane API proxy, nhung phai co local placeholder.
- Inventory khong co page rieng trong MVP; chi hien thi/update trong Product Detail.
- Orders va Shipment nam chung trong Orders page; Order drawer cho tao/cap nhat shipment don gian.

## Non-Goals

- Khong them database table moi.
- Khong them Inventory Movement Ledger.
- Khong them Brand/Manufacturer.
- Khong them Product Specifications.
- Khong them dashboard/report moi.
- Khong lam product create form thu cong.
- Khong lam spreadsheet editor, dry-run, retry tung row, Excel import.
- Khong lam media library, local file upload, crop/editor, variant-specific media.
- Khong lam shipping lifecycle/tracking provider integration.
- Khong refactor legacy `BlazorShop.Presentation`.

## Current State

Da co san:

- `ControlPlaneCommerceCatalogController` voi route hien tai:
  - `api/control-plane/stores/{storePublicId}/catalog/products`
  - `api/control-plane/stores/{storePublicId}/catalog/products/import`
  - `api/control-plane/stores/{storePublicId}/catalog/products/imports`
  - `api/control-plane/stores/{storePublicId}/catalog/categories`
  - `api/control-plane/stores/{storePublicId}/catalog/inventory`
  - product media proxy actions co ban.
- `ControlPlaneCommerceCatalogService` da proxy Commerce Node qua node/store credential.
- `ControlPlaneCatalogClient` da co nhieu method cho products/categories/media/inventory.
- `Catalog.razor` da co UI ban dau nhung dang gom qua nhieu thu vao mot page va van co create product/manual stock page style cu.
- Commerce Node da co API cho:
  - Products/categories/variants.
  - Product media.
  - Product import jobs/rows.
  - Variation templates.
  - Orders/shipments.

Can bo sung/chuang hoa:

- ControlPlane API route canonical moi cho Admin UX: `/api/controlplane/commerce/*`.
- Web client route moi dung `commerce/*`, co the giu route cu tam thoi de tranh break.
- Product import CSV template download.
- Product import error CSV download.
- Product media preview proxy qua ControlPlane API.
- Variation Template proxy/client/UI.
- Orders/Shipment proxy/client/UI.
- Product list/detail drawer UI thay cho side panel hien tai.

## Boundary Diagram

```text
ControlPlane.Web
  |
  | private API client
  v
ControlPlane.API
  |
  | node key + node secret + store scope
  v
CommerceNode.API
  |
  v
CommerceNodeDbContext
```

Forbidden:

```text
ControlPlane.Web -> CommerceNode.API
ControlPlane.Web -> /media/products/*
ControlPlane.Web -> api/commerce/*
ControlPlane.Web -> api/internal/*
```

## API Plan

Canonical route group for new Admin UX:

```text
/api/controlplane/commerce/stores/{storePublicId}/products
/api/controlplane/commerce/stores/{storePublicId}/product-imports
/api/controlplane/commerce/stores/{storePublicId}/categories
/api/controlplane/commerce/stores/{storePublicId}/variation-templates
/api/controlplane/commerce/stores/{storePublicId}/orders
```

Compatibility note:

- Existing route group `api/control-plane/stores/{storePublicId}/catalog/*` can remain during transition.
- New Web work should call the canonical `api/controlplane/commerce/*` routes after they exist.
- If route duplication is too noisy, use route attributes to support both paths on the same controller/actions until old client paths are removed.

### Product Import Endpoints

Add/standardize:

```text
GET  /api/controlplane/commerce/stores/{storePublicId}/product-imports/template
POST /api/controlplane/commerce/stores/{storePublicId}/product-imports
GET  /api/controlplane/commerce/stores/{storePublicId}/product-imports
GET  /api/controlplane/commerce/stores/{storePublicId}/product-imports/{jobPublicId}
GET  /api/controlplane/commerce/stores/{storePublicId}/product-imports/{jobPublicId}/rows
GET  /api/controlplane/commerce/stores/{storePublicId}/product-imports/{jobPublicId}/errors.csv
```

CSV template:

```csv
sku,title,short_description,full_description,price,compare_price,quantity,category_slug,variation_template_slug,product_type,image_urls,is_published
```

Rules:

- Template chi co header, khong co sample row.
- Error CSV uu tien xuat raw row + `error_column`, `error_message` neu data co san.
- Neu raw row khong co san, MVP xuat: `row_number,sku,status,error_column,error_message`.
- Upload mode chi gom `create_only` va `upsert`.

### Products Endpoints

Add/standardize:

```text
GET /api/controlplane/commerce/stores/{storePublicId}/products
GET /api/controlplane/commerce/stores/{storePublicId}/products/{productId}
PUT /api/controlplane/commerce/stores/{storePublicId}/products/{productId}
GET /api/controlplane/commerce/stores/{storePublicId}/products/{productId}/media
POST /api/controlplane/commerce/stores/{storePublicId}/products/{productId}/media/import
POST /api/controlplane/commerce/stores/{storePublicId}/products/{productId}/media/{mediaPublicId}/primary
POST /api/controlplane/commerce/stores/{storePublicId}/products/{productId}/media/{mediaPublicId}/retry
DELETE /api/controlplane/commerce/stores/{storePublicId}/products/{productId}/media/{mediaPublicId}
GET /api/controlplane/commerce/stores/{storePublicId}/products/{productId}/media/{mediaPublicId}/preview?w=80&h=80&fit=cover&format=webp
PUT /api/controlplane/commerce/stores/{storePublicId}/products/{productId}/inventory
PUT /api/controlplane/commerce/stores/{storePublicId}/products/{productId}/variants/{variantId}/inventory
```

Product list DTO should expose enough for lightweight thumbnail:

- `PrimaryMediaId` or `PrimaryMediaPublicId`.
- `HasPrimaryMedia`.
- `Slug`.
- `Sku`.
- `Name`.
- `CategoryName`.
- `Price`.
- `IsPublished`.
- `UpdatedAt`.

Product update must reject or ignore unsafe edits from Web:

- No `Sku` update.
- No `ProductType` update.
- No `StoreId` update.
- No `VariationTemplateId` update.
- No variant structure update.

Editable product fields:

- SEO: `Slug`, `MetaTitle`, `MetaDescription`, `CanonicalUrl`, `RobotsIndex`, `RobotsFollow`.
- Basic: `Name`, `ShortDescription`, `FullDescription`, `Price`, `ComparePrice`, `IsPublished`, `DisplayOrder`.
- Category: `CategoryId`/category picker if supported by existing DTO.
- Inventory: product quantity and existing variant stock only in Inventory section.

### Categories Endpoints

Add/standardize:

```text
GET  /api/controlplane/commerce/stores/{storePublicId}/categories
GET  /api/controlplane/commerce/stores/{storePublicId}/categories/tree
POST /api/controlplane/commerce/stores/{storePublicId}/categories
PUT  /api/controlplane/commerce/stores/{storePublicId}/categories/{categoryId}
DELETE /api/controlplane/commerce/stores/{storePublicId}/categories/{categoryId}
```

UI must support copy `category_slug`.

### Variation Template Endpoints

Add proxy/client endpoints for existing Commerce Node APIs:

```text
GET  /api/controlplane/commerce/stores/{storePublicId}/variation-templates
POST /api/controlplane/commerce/stores/{storePublicId}/variation-templates
GET  /api/controlplane/commerce/stores/{storePublicId}/variation-templates/{templatePublicId}
PUT  /api/controlplane/commerce/stores/{storePublicId}/variation-templates/{templatePublicId}
POST /api/controlplane/commerce/stores/{storePublicId}/variation-templates/{templatePublicId}/options
PUT  /api/controlplane/commerce/stores/{storePublicId}/variation-templates/{templatePublicId}/options/{optionPublicId}
POST /api/controlplane/commerce/stores/{storePublicId}/variation-templates/{templatePublicId}/options/{optionPublicId}/values
PUT  /api/controlplane/commerce/stores/{storePublicId}/variation-templates/{templatePublicId}/options/{optionPublicId}/values/{valuePublicId}
```

Rules:

- Disable only in UI. No delete action in MVP.
- Copy `variation_template_slug`.
- Active values shown before inactive values.
- Storefront only receives active name/value; Admin UX may show inactive values for management.

### Orders And Shipments Endpoints

Add proxy/client endpoints for existing Commerce Node order/shipment APIs:

```text
GET /api/controlplane/commerce/stores/{storePublicId}/orders
GET /api/controlplane/commerce/stores/{storePublicId}/orders/{orderId}
PUT /api/controlplane/commerce/stores/{storePublicId}/orders/{orderId}/admin-note
PUT /api/controlplane/commerce/stores/{storePublicId}/orders/{orderId}/shipping-status
GET /api/controlplane/commerce/stores/{storePublicId}/orders/{orderId}/shipment
PUT /api/controlplane/commerce/stores/{storePublicId}/orders/{orderId}/shipment
```

Shipment drawer fields:

- `CarrierName`
- `CarrierService`
- `TrackingNumber`
- `TrackingUrl`
- `ShipDate`
- `Note`

No delete/cancel shipment.

## UI Plan

### Navigation

Add a `Commerce Admin` group in `NavMenu.razor`:

- Products
- Product Imports
- Categories
- Variation Templates
- Orders

Existing `Catalog` page can be split/replaced gradually.

### Shared UI Components

Create/reuse lightweight components:

- `CommerceStoreSelector`
- `RightDrawer`
- `ApiMessageBanner`
- `StatusBadge`
- `CopyButton`
- `TableSkeleton`
- `EmptyState`
- `ConfirmDialog`

Drawer behavior:

- Desktop: right drawer, `min(1100px, 90vw)` for product/order; `760px-900px` for import/category/template.
- Mobile: full-screen sheet.
- Closing drawer preserves list filters/search/page state.
- Refreshing page can return to list in MVP.

### Products Page

Route:

```text
/commerce-admin/products
```

List table:

- Local placeholder thumbnail first.
- Lazy-load real thumbnail only when `PrimaryMediaId` exists.
- Thumbnail request uses ControlPlane API preview endpoint, small size only.
- Product cell combines:
  - title on first line.
  - SKU + slug on second line.
- Other columns:
  - category.
  - price.
  - published status.
  - updated date.
- No stock column.
- Search title/SKU/slug.
- Filter category.
- Filter published status.
- No inline edit.
- No bulk edit.
- No create product button.

Product drawer sections in order:

1. SEO
2. Basic info
3. Media
4. Variations
5. Inventory
6. Import History optional

SEO section:

- Slug is prominent.
- Show derived public path if store domain/path is known.
- Edit slug/meta/canonical/robots.

Basic section:

- Edit only safe fields.
- Category picker allowed.
- Price/compare price/published/display order.

Media section:

- Textarea for source URLs, one URL per line.
- Import URLs.
- List media rows with thumbnail preview through ControlPlane API.
- Set primary.
- Retry failed.
- Delete if API supports it.
- Show status/error message.

Variations section:

- For `CustomVariations`, show linked VariationTemplate read-only.
- For existing variant inventory products, show current variants read-only or minimal stock controls only.
- No variant structure edit.

Inventory section:

- Update product quantity.
- Update existing variant stock.
- No separate inventory page.

### Product Imports Page

Route:

```text
/commerce-admin/product-imports
```

Controls:

- Store selector.
- Download CSV template.
- Upload CSV.
- Mode segmented/select: `create_only`, `upsert`.
- Submit import.
- Links to Categories and Variation Templates pages for copy slug.

Job list:

- Status.
- File name/hash if available.
- Mode.
- Total/success/failed.
- Media queued/succeeded/failed if available.
- Created/updated time.

Job drawer:

- Summary counts.
- Recent rows.
- Row table: row number, SKU, action/status, error column/message, media status.
- Filter failed rows if supported.
- Download error CSV.
- No row edit.
- No retry individual row.

### Categories Page

Route:

```text
/commerce-admin/categories
```

Scope:

- Category list/tree.
- Show name, slug, parent, published/display order if available.
- Copy `category_slug`.
- Drawer for create/edit if existing API supports it.
- No new category business rules.

### Variation Templates Page

Route:

```text
/commerce-admin/variation-templates
```

List:

- name.
- slug.
- active status.
- option count/value count.
- updated time.
- copy `variation_template_slug`.

Drawer:

- Edit name/slug/active.
- Manage options: name, display order, active.
- Manage values: value, display order, active.
- Disable only, no delete action in MVP.
- No create product action.

### Orders Page

Route:

```text
/commerce-admin/orders
```

List:

- reference.
- customer/email if available.
- total.
- order status.
- shipping status.
- created date.

Order drawer:

- order lines.
- totals.
- customer/shipping fields currently available.
- admin note.
- shipment form.

Shipment form:

- carrier name.
- carrier service.
- tracking number.
- tracking URL.
- ship date.
- note.
- upsert shipment only.
- no delete/cancel.

## Phase Plan

### Phase 0 - Route And Surface Audit

- [x] Confirm exact CommerceNode endpoints for variation templates, orders, shipments, media preview requirements.
- [x] Confirm current DTOs expose enough product list thumbnail metadata. 2026-07-10: product list needs `PrimaryMediaPublicId`/`HasPrimaryMedia` support added during implementation.
- [x] Decide whether to add route aliases or fully move Web client to new `/api/controlplane/commerce/*` routes. 2026-07-10: add canonical route aliases while keeping existing `api/control-plane/stores/{storePublicId}/catalog/*` routes during transition.
- [x] Confirm Tailwind generated CSS flow after new pages/classes. 2026-07-10: ControlPlane Web uses Tailwind CSS generation through the existing project setup; regenerate/build CSS when new classes are added.
- [x] Commit: `docs/admin-ux-completion-plan` if this file is reviewed independently.

### Phase 1 - ControlPlane API Gateway Completion

- [x] Add canonical `/api/controlplane/commerce/*` route group or route aliases.
- [x] Add product import template endpoint.
- [x] Add product import error CSV endpoint.
- [x] Add product media preview proxy endpoint.
- [x] Add VariationTemplate proxy methods in application service/interface.
- [x] Add VariationTemplate controller actions.
- [x] Add Orders/Shipment proxy methods in application service/interface.
- [x] Add Orders/Shipment controller actions.
- [x] Ensure all responses use ControlPlane API response envelope.
- [x] Ensure all write routes require `StoresWrite`; read routes require `StoresRead`.
- [x] Verify Web never needs node key/secret. 2026-07-10: node credentials remain inside `ControlPlaneCommerceCatalogService`.
- [x] Commit: `feat(control-plane): complete commerce admin gateway`.

### Phase 2 - Web Client And Shared Admin UI Components

- [ ] Add/standardize `ControlPlaneCommerceAdminClient` or extend `ControlPlaneCatalogClient` for new canonical routes.
- [ ] Add typed methods for product imports/template/error CSV.
- [ ] Add typed methods for variation templates.
- [ ] Add typed methods for orders/shipments.
- [ ] Add typed media preview URL builder that points to ControlPlane API only.
- [ ] Add `RightDrawer` component.
- [ ] Add compact `CopyButton`.
- [ ] Add status/empty/loading helpers if not already reusable.
- [ ] Add local placeholder image/icon strategy for product thumbnails.
- [ ] Commit: `feat(control-plane-web): add commerce admin client and drawer components`.

### Phase 3 - Product Imports UX

- [ ] Create `/commerce-admin/product-imports`.
- [ ] Add store selector.
- [ ] Add download CSV template button.
- [ ] Add mode selector: `create_only`, `upsert`.
- [ ] Add CSV upload and submit.
- [ ] Add job list.
- [ ] Add job detail drawer.
- [ ] Add row error table.
- [ ] Add download error CSV button.
- [ ] Add quick links to Categories and Variation Templates.
- [ ] Remove any spreadsheet editor/dry-run/retry-row scope if it appears during implementation.
- [ ] Commit: `feat(control-plane-web): add product import admin UX`.

### Phase 4 - Variation Templates UX

- [ ] Create `/commerce-admin/variation-templates`.
- [ ] Add list with name/slug/status/counts/updated time.
- [ ] Add copy slug action.
- [ ] Add drawer for create/edit template.
- [ ] Add option management.
- [ ] Add value management.
- [ ] Implement disable-only behavior in UI.
- [ ] Show active values before inactive values.
- [ ] Commit: `feat(control-plane-web): add variation template admin UX`.

### Phase 5 - Categories UX Polish

- [ ] Create or polish `/commerce-admin/categories`.
- [ ] Add list/tree view.
- [ ] Add copy category slug action.
- [ ] Add drawer for create/edit if supported by existing API.
- [ ] Keep category business rules unchanged.
- [ ] Commit: `feat(control-plane-web): add commerce category admin UX`.

### Phase 6 - Products UX And Product Detail Drawer

- [ ] Create `/commerce-admin/products`.
- [ ] Add compact product table.
- [ ] Combine title/SKU/slug in one product cell.
- [ ] Remove stock column from list.
- [ ] Add category and published filters.
- [ ] Add title/SKU/slug search.
- [ ] Add placeholder thumbnail with lazy-loaded proxy thumbnail when `PrimaryMediaId` exists.
- [ ] Remove manual create product UI.
- [ ] Add product detail drawer.
- [ ] Add SEO section first.
- [ ] Add basic info section.
- [ ] Add media section through ControlPlane media proxy.
- [ ] Add variations section read-only/minimal.
- [ ] Add inventory section inside drawer.
- [ ] Add optional import history link/filter if cheap.
- [ ] Ensure unsafe product fields are not editable.
- [ ] Commit: `feat(control-plane-web): add product admin drawer UX`.

### Phase 7 - Orders And Shipment UX

- [ ] Create `/commerce-admin/orders`.
- [ ] Add order list.
- [ ] Add order drawer.
- [ ] Add order line/totals/customer display.
- [ ] Add admin note edit if API supports it.
- [ ] Add shipment upsert form in order drawer.
- [ ] Keep shipment fields simple.
- [ ] Do not add delete/cancel shipment.
- [ ] Commit: `feat(control-plane-web): add orders and shipment admin UX`.

### Phase 8 - Navigation, Permission UX, And Cleanup

- [ ] Add `Commerce Admin` nav group.
- [ ] Wire nav links:
  - [ ] Products.
  - [ ] Product Imports.
  - [ ] Categories.
  - [ ] Variation Templates.
  - [ ] Orders.
- [ ] Remove or redirect old `/catalog` page if replaced.
- [ ] Hide/disable write actions when user lacks write permission if permission data is available.
- [ ] Ensure API 403 message is surfaced cleanly if UI permission hiding is incomplete.
- [ ] Ensure no direct CommerceNode URLs are hardcoded in Web.
- [ ] Commit: `refactor(control-plane-web): organize commerce admin navigation`.

### Phase 9 - QA And Docs

- [ ] Update `QA-ControlPlane.todo.md` with Admin UX Completion checklist.
- [ ] Update `QA-CommerceNode.todo.md` only for API cases that Admin UX depends on and are still unverified.
- [ ] Add visible Playwright MCP QA note: use Chromium `headless=false` when requested.
- [ ] Verify Product Imports page.
- [ ] Verify Variation Templates page.
- [ ] Verify Categories page.
- [ ] Verify Products/Product Detail drawer.
- [ ] Verify media preview proxy.
- [ ] Verify Orders/Shipment page.
- [ ] Verify network capture: ControlPlane Web makes 0 direct calls to CommerceNode.
- [ ] Verify `dotnet build` for:
  - [ ] `BlazorShop.ControlPlane.API`
  - [ ] `BlazorShop.ControlPlane.Web`
  - [ ] `BlazorShop.CommerceNode.API` if shared DTO/API changes require it.
- [ ] Commit: `test(control-plane): document admin ux qa coverage`.

## QA Checklist To Add

### Control Plane Commerce Admin UX

- [ ] Commerce Admin nav group renders.
- [ ] Products page loads after selecting a store.
- [ ] Product list calls ControlPlane API only.
- [ ] Product list does not call CommerceNode directly.
- [ ] Product thumbnail placeholder renders before image load.
- [ ] Product thumbnail image, when available, loads through ControlPlane API preview proxy.
- [ ] Product drawer opens and closes without losing list filters.
- [ ] Product drawer shows SEO first.
- [ ] Product drawer can update allowed SEO fields.
- [ ] Product drawer can update allowed basic fields.
- [ ] Product drawer does not allow editing SKU.
- [ ] Product drawer does not allow editing product type.
- [ ] Product drawer does not allow editing variation template.
- [ ] Product drawer media section lists media.
- [ ] Product drawer media import queues URLs.
- [ ] Product drawer can set primary media.
- [ ] Product drawer can retry failed media.
- [ ] Product drawer inventory section updates product quantity.
- [ ] Product drawer inventory section updates existing variant stock.
- [ ] Product Import page downloads header-only CSV template.
- [ ] Product Import page uploads CSV in `create_only`.
- [ ] Product Import page uploads CSV in `upsert`.
- [ ] Product Import job list refreshes status.
- [ ] Product Import job drawer shows row errors.
- [ ] Product Import error CSV downloads.
- [ ] Category page shows and copies `category_slug`.
- [ ] Variation Template page shows and copies `variation_template_slug`.
- [ ] Variation Template drawer can create/update template.
- [ ] Variation Template drawer can create/update/disable option.
- [ ] Variation Template drawer can create/update/disable value.
- [ ] Orders page loads order list.
- [ ] Order drawer shows lines/totals/customer fields.
- [ ] Order drawer creates shipment.
- [ ] Order drawer updates existing shipment.
- [ ] Shipment update syncs visible order shipping fields after refresh.
- [ ] API error messages are displayed from response `message`.
- [ ] Browser console has no unexpected errors.
- [ ] Visible Playwright MCP browser QA is used when operator observation is requested.

## Risks And Mitigations

| Risk | Mitigation |
|---|---|
| Web accidentally calls CommerceNode/public media directly | Add network-capture QA and use ControlPlane API preview URL builder only. |
| Existing `/catalog` page behavior conflicts with new pages | Split new pages under `/commerce-admin/*`, then remove/redirect `/catalog` only after parity. |
| Product edit accidentally changes identity/structure fields | Use narrow update DTO/model for Admin UX, do not bind SKU/product type/template/store id. |
| Product list thumbnails become expensive | Placeholder first, lazy load small preview only for current page and only when primary media exists. |
| Route naming churn breaks existing client | Keep route aliases during migration or update Web client in same phase. |
| Error CSV lacks original row data | MVP fallback exports row number/SKU/status/error column/message. |

## Completion Criteria

- ControlPlane Web has Commerce Admin pages for Product Imports, Variation Templates, Categories, Products, and Orders.
- Product create is only available through CSV import.
- Product Detail drawer supports SEO/basic/media/variation/inventory in the locked order.
- Orders page supports simple shipment upsert in order drawer.
- Media thumbnails/previews in ControlPlane Web go through ControlPlane API only.
- QA checklist is updated and verified with visible Playwright MCP when requested.
- Network capture confirms 0 direct CommerceNode calls from ControlPlane Web.
