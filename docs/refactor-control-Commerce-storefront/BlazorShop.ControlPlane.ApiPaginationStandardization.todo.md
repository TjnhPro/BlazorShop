# BlazorShop Control Plane API Pagination Standardization Todo

Status: draft
Created: 2026-07-11
Scope: fix Control Plane admin list API contracts, establish a reusable paging pattern for every list/search method, fix CSV import template, Product Import job diagnostics, and matching Web pagination UX.

## Goal

Chuan hoa cac API list/search cua `BlazorShop.ControlPlane.API` de admin UI khong con fetch het du lieu hoac hard-code limit lon.

Bat ky method nao la `List`, `Query`, `Search`, hoac tra ve collection co the tang truong theo database/runtime data deu phai dung paging contract. Khong lam theo kieu "sua vai endpoint bi lo" roi de nhung endpoint list khac load-all.

Phase nay cung sua bug truc tiep cua Product Import:

- CSV template general dang sai header so voi CommerceNode parser.
- Product Import job bi `Failed` o cap file/header nhung khong co row error de debug.

Muc tieu khong phai refactor lon, ma la sua dung contract API de Control Plane co the mo rong node/store/import/health ma khong bi load-all.

## Root Cause Summary

- `ControlPlaneCommerceCatalogController` download Product Import template voi header `sku,title,short_description,full_description,...`.
- `ProductImportCsvParser` yeu cau `sku,name,slug,category_slug,product_type,variation_template_slug,price,compare_price,quantity,is_published,short_description,description,image_urls`.
- Parser fail o cap header/file, job status thanh `Failed`, `total_rows=0`, `failed_count=1`.
- `ProductImportJob` hien khong co `ErrorMessage`/`ErrorJson`; `product_import_row` khong co row nao nen Web UI va error CSV khong hien ly do fail.
- Control Plane list APIs dang khong dong nhat:
  - Products/Orders/Inventory dung `pageNumber/pageSize`.
  - Users/Nodes dung `cursor/limit`.
  - Actions dung `beforeId/limit`.
  - Product Import dung `skip/take`.
  - Stores va Health khong co paging, load all hoac `.Take(200)` am tham.

## Locked Decisions

- Control Plane admin list APIs dung chuan `pageNumber/pageSize`.
- Moi public API/method co y nghia `List`, `Query`, hoac `Search` phai paging.
- Neu du lieu la catalog/lookup nho va co chu y khong paging, khong dat ten API/method la `List`; dung ten `Catalog`, `Lookup`, `Tree`, hoac `Detail` de lam ro day khong phai list co the tang truong.
- Khong co `ListResponse` chi gom `Items` ma khong co paging metadata.
- Khong co endpoint list nao duoc `.ToListAsync()` toan bo bang roi moi cat tren memory.
- Khong co `.Take(200)` am tham de thay cho paging.
- Khong expose `skip/take` cho Web/API admin list.
- Response list page phai co:
  - `items`
  - `totalCount`
  - `pageNumber`
  - `pageSize`
  - `totalPages`
- API service/repository co the tinh `skip` noi bo, nhung khong expose `skip/take` len Web/API contract admin.
- Default page size:
  - Admin table/list page: 25.
  - Product grid/import rows can use 20 or 25 tuy UI hien co, nhung API max la 100.
- Max page size: 100, tru khi endpoint co ly do rieng va duoc ghi trong code.
- `ControlPlane.Web` chi goi `ControlPlane.API`.
- `ControlPlane.API` van la gateway duy nhat sang `CommerceNode.API`.
- Khong dua node key/node secret vao Web client.
- Khong doi legacy `BlazorShop.Presentation`.
- Khong them ABP/module-style structure.

## Non Goals

- Khong xay lai toan bo response envelope.
- Khong refactor CommerceNode task worker.
- Khong them UI dashboard moi.
- Khong gop ControlPlaneDbContext va CommerceNodeDbContext.
- Khong sua Storefront V2.
- Khong thay doi business behavior cua Product Import ngoai template/pagination/diagnostic.

## Target API Standard

### Request

```text
GET ...?pageNumber=1&pageSize=25
```

Optional filters giu nguyen:

```text
search
status
nodePublicId
storePublicId
actionType
roleKey
permissionKey
```

### Response

```json
{
  "success": true,
  "message": "string",
  "data": {
    "items": [],
    "totalCount": 0,
    "pageNumber": 1,
    "pageSize": 25,
    "totalPages": 0
  }
}
```

### Compatibility Rule

- If an endpoint already uses `pageNumber/pageSize`, preserve it.
- If an endpoint exposes `skip/take`, replace with `pageNumber/pageSize`.
- If an endpoint exposes cursor/beforeId and is used by admin table UI, migrate to page contract in this phase unless it is explicitly documented as an infinite feed.
- If an endpoint is a small static catalog/lookup and does not use paging, rename the API/client method away from `List*` and document why it is a lookup, not a list.
- Detail child collections must be bounded. If the child collection can grow without a strict domain limit, expose a separate paged list endpoint instead of embedding all children in detail.

## Endpoint Inventory

| Endpoint area | Current contract | Problem | Target |
| --- | --- | --- | --- |
| Product Import jobs | `skip/take` | Web hard-codes `Take=100`; no real page UX | `pageNumber/pageSize` + page UI |
| Product Import rows | `skip/take` | Drawer loads max 100 rows; no page UX | `pageNumber/pageSize` + page controls in drawer |
| Stores | no paging, service `.Take(200)` | Silent truncation and no page UX | `pageNumber/pageSize` + total count |
| Health nodes | no paging, loads all nodes with snapshots | Bad scaling; user explicitly flagged | `pageNumber/pageSize` + total count |
| Actions | `beforeId/limit` | Web only loads first page, no `NextBeforeId` UX | migrate to page or complete feed pagination |
| Users | `cursor/limit` | Works, but inconsistent admin contract | migrate to page after high-risk endpoints |
| Nodes | `cursor/limit` | Works, but inconsistent admin contract | migrate to page after Health/Stores |
| Products | `pageNumber/pageSize` | OK | keep |
| Orders | `pageNumber/pageSize` | OK | keep |
| Inventory | `pageNumber/pageSize` | OK | keep |
| Credentials | no paging | Named/listed as list and can grow per node | `pageNumber/pageSize` |
| Roles/Permissions | no paging | Static catalog but method name is `List*` | rename to catalog/lookup or add paging |
| Categories list | no paging | Can grow per store | `pageNumber/pageSize` |
| Categories tree | no paging | Tree document, not list table | keep as `tree`, bounded by category page strategy |
| Variants by product | no paging | Can grow per product | `pageNumber/pageSize` |
| Product media by product | no paging | Can grow per product unless strict max enforced | `pageNumber/pageSize` or enforce/document strict max |
| Variation templates | no paging | Can grow per store | `pageNumber/pageSize` |

## Data / DTO Design

### New shared page contract

Prefer a small Control Plane DTO contract under `BlazorShop.Application/ControlPlane/Common`:

```csharp
public sealed record ControlPlanePageQuery(
    int PageNumber = 1,
    int PageSize = 25);

public sealed record ControlPlanePagedResponse<TItem>(
    IReadOnlyList<TItem> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
```

Use helper methods for normalization:

- `pageNumber = Math.Max(1, pageNumber)`
- `pageSize = Math.Clamp(pageSize <= 0 ? defaultPageSize : pageSize, 1, 100)`
- `skip = (pageNumber - 1) * pageSize`
- `totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)`

### Pattern rule for new code

All new Control Plane API list/search/query work must start from this contract:

```csharp
public sealed record SomeFeatureListQuery(
    string? Search = null,
    string? Status = null,
    int PageNumber = 1,
    int PageSize = 25);

public sealed record SomeFeatureListResponse(
    IReadOnlyList<SomeFeatureSummary> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
```

Acceptable alternatives:

- Use `ControlPlanePagedResponse<TItem>` directly.
- Use domain-specific response name, but it must include the same metadata.

Not acceptable:

- `IReadOnlyList<T>` as the whole list response.
- `ListResponse(IReadOnlyList<T> Items)` without total/page metadata.
- `skip/take` in API/Web query contracts.
- Cursor pagination for ordinary Control Plane admin tables.
- Hidden caps such as `.Take(100)` or `.Take(200)` without user-visible paging.

### Product Import diagnostics

Add job-level diagnostic fields:

- `ProductImportJob.ErrorMessage`
- `ProductImportJob.ErrorJson` optional, for file/header-level validation errors.

Reason:

- Row-level errors are not enough.
- File/header errors happen before rows are created.
- Admin UI must be able to show "missing header X" even when `product_import_row` is empty.

Database impact:

- CommerceNode migration only.
- Add nullable `error_message` text and `error_json` jsonb to `product_import_job`.
- Do not add these fields to ControlPlaneDbContext.

## Phase Plan

### Phase 1 - CSV Template And Job-Level Import Diagnostics

Goal: fix the direct import failure and make file/header failures debuggable.

Tasks:

- [x] Update Product Import template header to canonical parser schema:
  - `sku,name,slug,category_slug,product_type,variation_template_slug,price,compare_price,quantity,is_published,short_description,description,image_urls`
- [x] Remove `title` and `full_description` from template.
- [x] Add `ErrorMessage` and `ErrorJson` to `ProductImportJob`.
- [x] Add CommerceNode EF migration for `product_import_job.error_message` and `product_import_job.error_json`.
- [x] Update `ProductImportJobDto` to expose job-level errors.
- [x] Update `ProductImportTaskHandler.FailJobAsync` to save the exact parser/file error message.
- [x] Update `CommerceProductImports.razor` drawer to show job-level error if present.
- [x] Update Product Import error CSV to include a job-level error row when there are no failed import rows.
- [ ] Add QA checklist for:
  - Download template header matches parser.
  - Upload template-derived CSV succeeds when data is valid.
  - Upload missing-header CSV shows job-level error in UI.
  - Error CSV includes job-level error when no rows exist.

Commit:

```text
fix(control-plane): correct product import template diagnostics
```

### Phase 2 - Introduce Control Plane Page Contract And Guardrails

Goal: add shared paging contract and create a repo rule so every list/search method uses it.

Tasks:

- [ ] Add `ControlPlanePageQuery`.
- [ ] Add `ControlPlanePagedResponse<TItem>`.
- [ ] Add normalization helper or local static methods following existing project style.
- [ ] Add/update docs rule: every Control Plane API `List`, `Query`, `Search` endpoint must be paged.
- [ ] Add a source audit checklist in this todo for all existing list methods.
- [ ] Rename non-paged static catalog methods away from `List*`, or explicitly convert them to paged responses.
- [ ] Do not migrate all endpoints in this phase yet, but the pattern must be committed first.
- [ ] Build `BlazorShop.Application`.

Commit:

```text
feat(control-plane): add shared page contract
```

### Phase 3 - Product Import Jobs And Rows Pagination

Goal: remove exposed `skip/take` from Product Import admin flow.

Tasks:

- [ ] Replace `ProductImportJobListQuery.Skip/Take` with `PageNumber/PageSize`.
- [ ] Replace `ProductImportRowsQuery.Skip/Take` with `PageNumber/PageSize`.
- [ ] Replace response `Skip/Take` fields with `PageNumber/PageSize/TotalPages`.
- [ ] Update CommerceNode import service query math internally.
- [ ] Update CommerceNode API binding.
- [ ] Update ControlPlane API proxy route/query mapping.
- [ ] Update ControlPlane Web typed client.
- [ ] Add page controls to Product Import jobs table.
- [ ] Add page controls to Product Import drawer rows table.
- [ ] Avoid hard-coded `Take=100`.
- [ ] Keep error CSV bounded but page through failed rows internally if needed, or document max export rows.

Commit:

```text
feat(control-plane): page product import jobs
```

### Phase 4 - Stores Pagination

Goal: remove store list load-all and `.Take(200)` truncation.

Tasks:

- [ ] Update `ControlPlaneStoreListQuery` with `PageNumber/PageSize`.
- [ ] Update `ControlPlaneStoreListResponse` to paged response.
- [ ] Update `ControlPlaneStoreService.ListAsync`:
  - filter first;
  - count total;
  - order;
  - skip/take internally;
  - map only current page.
- [ ] Update `ControlPlaneStoresController.List`.
- [ ] Update `ControlPlaneStoreClient.ListAsync`.
- [ ] Add page controls to `Stores.razor`.
- [ ] Check all places that call `StoreClient.ListAsync(status: "active")`.
- [ ] For dropdown/reference data, either:
  - fetch first 100 active stores explicitly and document it as dropdown reference, or
  - add a separate lightweight lookup endpoint later.

Commit:

```text
feat(control-plane): page store registry
```

### Phase 5 - Health Nodes Pagination

Goal: node health page must not load all nodes/snapshots.

Tasks:

- [ ] Add `ControlPlaneHealthListQuery` with `PageNumber/PageSize`, optional `status/search`.
- [ ] Update `ControlPlaneHealthListResponse` to paged response.
- [ ] Rewrite `ControlPlaneHealthService.ListAsync`:
  - query node page first;
  - only load latest health and current capability for nodes on that page;
  - avoid loading full snapshot collections for every node.
- [ ] Keep `GetDetailAsync(nodePublicId)` responsible for recent timeline of one node.
- [ ] Ensure detail timeline remains capped, currently `Take(25)` behavior should stay.
- [ ] Update `ControlPlaneHealthController.List`.
- [ ] Update `ControlPlaneHealthClient.ListAsync`.
- [ ] Add page controls to `Health.razor`.

Commit:

```text
feat(control-plane): page node health list
```

### Phase 6 - Actions Pagination Cleanup

Goal: make Actions page a real paged admin list.

Tasks:

- [ ] Decide final contract:
  - Preferred: convert to `pageNumber/pageSize`.
  - Alternative: keep `beforeId/limit` only if UI is explicitly an infinite feed.
- [ ] For admin table consistency, implement `pageNumber/pageSize`.
- [ ] Update `ControlPlaneActionListQuery`.
- [ ] Update `ControlPlaneActionListResponse`.
- [ ] Update `ControlPlaneActionService.ListAsync` with total count and page math.
- [ ] Update `ControlPlaneActionsController.List`.
- [ ] Update `ControlPlaneActionClient.ListAsync`.
- [ ] Add page controls to `Actions.razor`.
- [ ] Remove unused `NextBeforeId` from Web DTO if page contract wins.

Commit:

```text
feat(control-plane): page control actions
```

### Phase 7 - Users And Nodes Contract Alignment

Goal: align existing cursor list APIs with the Control Plane admin page standard.

Rationale:

- Users and Nodes currently work with `cursor/limit`, but Control Plane Web admin pages are not API-feed clients.
- Page number UX is easier to test, navigate, and reason about for admin tables.

Tasks:

- [ ] Migrate `ControlPlaneUserListQuery` from `Cursor/Limit` to `PageNumber/PageSize`.
- [ ] Migrate `ControlPlaneUserListResponse` from `NextCursor` to paged response.
- [ ] Update `Users.razor` from "Load more" to page controls.
- [ ] Migrate `ControlPlaneNodeListQuery` from `Cursor/Limit` to `PageNumber/PageSize`.
- [ ] Migrate `ControlPlaneNodeListResponse` from `NextCursor` to paged response.
- [ ] Update `Nodes.razor` from "Load more" to page controls.
- [ ] Update all reference callers that currently call first node page only:
  - `Stores.razor`
  - `Actions.razor`
  - `Credentials.razor`
- [ ] For dropdown/reference node lists, explicitly request `pageSize=100` and document current MVP limit, or create lookup endpoint later.

Commit:

```text
feat(control-plane): align users and nodes pagination
```

### Phase 8 - Remaining List Endpoint Sweep

Goal: remove all remaining non-paged `List*` methods from ControlPlane API/Web contracts.

Tasks:

- [ ] Page node credentials:
  - `ControlPlaneCredentialListQuery`
  - `ControlPlaneCredentialListResponse`
  - `ControlPlaneCredentialService.ListAsync`
  - `Credentials.razor`
- [ ] Page categories list:
  - Keep `categories/tree` as tree endpoint.
  - Convert `categories` list endpoint to `pageNumber/pageSize`.
- [ ] Page product variants by product.
- [ ] Page product media by product or enforce/document a strict max count. If no strict max exists, page it.
- [ ] Page variation templates.
- [ ] Handle roles/permissions:
  - Preferred: rename API/client methods to `GetRoleCatalog` and `GetPermissionCatalog`.
  - Alternative: page them too.
  - Do not leave methods named `ListRoles`/`ListPermissions` returning unpaged collection.
- [ ] Search codebase for:
  - `ListResponse(IReadOnlyList`
  - `Task<.*IReadOnlyList`
  - `public async Task<IActionResult> List`
  - `.Take(100)`
  - `.Take(200)`
  - `Skip =`
  - `Take =`
- [ ] For every match, either migrate to page contract or document why it is not a list endpoint.

Commit:

```text
feat(control-plane): page remaining list endpoints
```

### Phase 9 - QA And Documentation

Goal: verify paging behavior across Control Plane after a broad contract change.

Tasks:

- [ ] Update `QA-ControlPlane.todo.md` with cases for:
  - Product Import template header.
  - Product Import job page navigation.
  - Product Import row page navigation.
  - Stores page navigation and filters.
  - Health node page navigation and detail load.
  - Actions page navigation and filters.
  - Users page navigation and filters.
  - Nodes page navigation and filters.
  - Credentials page navigation.
  - Category list page/navigation or lookup behavior.
  - Variation Template page navigation.
  - Product media and variants pagination where applicable.
- [ ] Update architecture docs if the API paging rule should become permanent:
  - `docs/architecture/03-runtime-boundaries.md`
  - `docs/architecture/08-agent-decision-rules.md`
- [ ] Add explicit agent rule: "List methods must be paged; static lookup/catalog methods must not be named List."
- [ ] Run focused builds:
  - `dotnet build BlazorShop.Application/BlazorShop.Application.csproj --no-restore`
  - `dotnet build BlazorShop.Infrastructure/BlazorShop.Infrastructure.csproj --no-restore`
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore`
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj --no-restore`
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore`
- [ ] Run visible-browser QA if user requests observation.
- [ ] Verify no direct Web calls to CommerceNode.

Commit:

```text
test(control-plane): verify pagination standardization
```

## QA Checklist To Add

Add to `QA-ControlPlane.todo.md`:

- [ ] Product Import template downloads canonical parser header.
- [ ] Product Import valid CSV from downloaded template queues and completes.
- [ ] Product Import invalid header shows job-level error in drawer.
- [ ] Product Import error CSV includes job-level error when row list is empty.
- [ ] Product Import jobs page has previous/next or page controls and honors page size.
- [ ] Product Import rows drawer has page controls and honors page size.
- [ ] Stores page has page controls and filters keep current page state sane.
- [ ] Health page has page controls and does not load all nodes.
- [ ] Health detail still loads recent timeline for selected node.
- [ ] Actions page has page controls and status/action filters work.
- [ ] Users page has page controls and search/status/role/permission filters work.
- [ ] Nodes page has page controls and search/status filters work.
- [ ] Credentials page has page controls or renamed lookup contract.
- [ ] Category list no longer load-all without paging.
- [ ] Variation templates no longer load-all without paging.
- [ ] Product child collections either page or have documented strict domain limits.

## Source Audit Checklist

Every checked item must either become paged or be renamed away from `List`.

- [ ] `ControlPlaneUsersController.List`
- [ ] `ControlPlaneNodesController.List`
- [ ] `ControlPlaneStoresController.List`
- [ ] `ControlPlaneHealthController.List`
- [ ] `ControlPlaneActionsController.List`
- [ ] `ControlPlaneCredentialsController.List`
- [ ] `ControlPlaneUsersController.ListRoles`
- [ ] `ControlPlaneUsersController.ListPermissions`
- [ ] `ControlPlaneCommerceCatalogController.QueryProducts`
- [ ] `ControlPlaneCommerceCatalogController.ListProductImports`
- [ ] `ControlPlaneCommerceCatalogController.ListProductImportRows`
- [ ] `ControlPlaneCommerceCatalogController.ListProductMedia`
- [ ] `ControlPlaneCommerceCatalogController.ListCategories`
- [ ] `ControlPlaneCommerceCatalogController.GetCategoryTree`
- [ ] `ControlPlaneCommerceCatalogController.ListVariants`
- [ ] `ControlPlaneCommerceCatalogController.QueryInventory`
- [ ] `ControlPlaneCommerceCatalogController.ListVariationTemplates`
- [ ] `ControlPlaneCommerceCatalogController.QueryOrders`

## Risk Notes

- This phase touches shared DTOs used by ControlPlane API, Web, Infrastructure, and CommerceNode Product Import.
- Product Import query contract changes may break existing manual API calls; update docs/QA examples in the same phase.
- Dropdown/reference lists still need a bounded strategy. They should not silently assume the first 25 rows is "all".
- Store and Health list changes are high-value because they currently have no real paging.
- If the diff becomes too large, implement through phases exactly as listed and commit each phase separately.

## Implementation Order Summary

1. Fix direct Product Import template and job error visibility.
2. Add shared page contract and guardrails.
3. Page Product Import jobs/rows.
4. Page Stores.
5. Page Health nodes.
6. Page Actions.
7. Align Users/Nodes.
8. Sweep remaining list endpoints.
9. Update QA/docs and verify.
