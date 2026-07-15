# BlazorShop Store Mapping Autoplan

Generated: 2026-07-15

Scope: 3.4 Store mapping

## Muc tieu

Store mapping can duoc harden cho cac luong V2 hien co, uu tien ngan ro ri du lieu cross-store va giu dung runtime boundary:

- ControlPlane.Web chi goi ControlPlane.API.
- ControlPlane.API gateway sang CommerceNode.API khi can thao tac commerce admin.
- Storefront.V2 chi goi CommerceNode Storefront API tai `api/storefront/stores/{storeKey}/*`.
- Commerce data thuoc `CommerceNodeDbContext`; khong them feature V2 vao legacy `AppDbContext`.

Muc tieu ky thuat la bien store scope thanh bat buoc o cac query/command quan trong, nhung khong rebuild module/catalog architecture khi codebase da co nen tang store-scoped.

## Pham vi duoc duyet

| Feature | Quyet dinh |
| --- | --- |
| Product duoc theo store | Can thiet. Harden admin/read/write paths va data constraint. |
| Category duoc theo store | Can thiet. Harden admin/read/write paths va data constraint. |
| Topic/page gioi han theo store | Da co nen tang tot. Can verify/test, khong rewrite. |
| Discount gioi han theo store | Chua can implement ngay vi discount engine chua ton tai. Khi lam discount phai store-scoped tu dau. |
| Module active khac nhau theo store | Chua can generic module system. Dung pattern per-feature nhu `StorePaymentMethod`; chi them generic registry khi co nhieu module that su can. |

## Hien trang da xac minh

| Area | Evidence trong codebase | Nhan xet |
| --- | --- | --- |
| Product entity | `BlazorShop.Domain/Entities/Product.cs` co `Guid? StoreId` | Da co cot store, nhung nullable nen can backfill/guard. |
| Product index | `ProductConfiguration` co index theo `StoreId`, `Sku`, `Slug`, `CategoryId`, `IsPublished` | Nen tang DB phu hop cho store mapping. |
| Product storefront read | `CommerceNodeProductReadRepository` filter `product.StoreId == storeId` cho storefront paths | Storefront product scope da dung huong. |
| Product admin read | `GetCatalogPageAsync`, `GetProductDetailsByIdAsync`, `ProductSlugExistsAsync` co path global/unscoped | Can harden de tranh cross-store leak hoac duplicate rule sai. |
| Category entity | `BlazorShop.Domain/Entities/Category.cs` co `Guid? StoreId` | Da co cot store, nhung nullable. |
| Category index | `CategoryConfiguration` co index theo `StoreId`, `Slug`, `ParentCategoryId`, `DisplayOrder` | Nen tang DB phu hop. |
| Category service | `CategoryService.GetAllAsync`, `QueryAsync`, `GetByIdAsync` dung generic repository unscoped | Can thay bang store-scoped repository/query. |
| Category parent validation | `CategoryService.ValidateParentAsync` da check parent cung store | Diem tot, can giu va them test. |
| Page/topic | `StorefrontPage.StoreId` required; `StorefrontPageService` query theo current store | Da co store ownership ro. Can QA/contract tests. |
| Discount | Khong co `Discount`, `Coupon`, `Promotion` entity/service. Checkout chi co `DiscountTotal = 0m` | Defer implementation. |
| Module | Khong co generic module registry. `StorePaymentMethod` la per-store module-like config | Defer generic module; reuse concrete per-store config pattern. |

## Autoplan review summary

CEO review:

- Viec can lam nhat la chong ro ri cross-store trong admin/catalog, vi day la loi co tac dong business va security.
- Khong nen xay discount/module framework som khi product/category/page mapping chua duoc dong chat.
- Ke hoach nen ship theo phase nho, moi phase co verification ro.

Design review:

- Khong can them UI lon o phase dau. UI manager chi can hien thi store context ro rang va thong bao Not Found/Conflict dung khi object khong thuoc store hien tai.
- Display/order manager da co huong tu store lifecycle; store mapping nen tap trung vao data ownership.

Engineering review:

- Rui ro lon nhat la cac path admin dung generic repository khong scope theo store.
- `StoreId` nullable tren Product/Category la debt can xu ly bang backfill + constraint theo CommerceNode, nhung phai can than vi entity hien dang shared voi legacy.
- Khong dua `StoreId` tu client request vao DTO public/admin command. Store scope phai den tu `storeKey` route/query va middleware/context.

DX review:

- Moi endpoint moi/changed can co operationId, summary, DTO ro, error schema, security metadata va contract test theo `docs/architecture/09-api-contract-standards.md`.
- Repository/service names nen noi ro store scope de reviewer nhin thay boundary, vi du `GetByIdForCurrentStoreAsync` hoac tham so `storeId` bat buoc.

## Decision audit trail

| ID | Decision | Ly do |
| --- | --- | --- |
| D1 | Product/category: harden store mapping hien co, khong rebuild catalog | Entity/index da co `StoreId`; sua unscoped paths it rui ro hon rebuild. |
| D2 | Khong cho client gui `StoreId` trong request admin/storefront | Store ownership la server-derived tu `storeKey`, tranh spoofing. |
| D3 | Product/Category `StoreId` tien toi required trong CommerceNode | Nullable store data lam query/update de loi va kho verify. |
| D4 | Page/topic chi verify va test them | `StorefrontPage` da required `StoreId` va service da scoped. |
| D5 | Discount mapping defer | Chua co discount domain; tao mapping truoc se la speculative design. |
| D6 | Generic module activation defer | Hien chi co concrete pattern `StorePaymentMethod`; generic registry chi dang khi nhieu module can chung model. |
| D7 | Control Plane Web khong goi truc tiep CommerceNode | Giu dung architecture boundary hien co. |

## Target architecture

```text
ControlPlane.Web
  -> ControlPlane.API
      -> CommerceNode.API api/commerce/admin/*?storeKey={storeKey}

Storefront.V2
  -> CommerceNode.API api/storefront/stores/{storeKey}/*

CommerceNodeDbContext
  CommerceStore
    -> Product
    -> Category
    -> StorefrontPage
    -> StorePaymentMethod
    -> future Discount
    -> future StoreModuleSetting, only if justified
```

## Phase 0 - Baseline va guardrail tests

Goal: dong lai hanh vi hien tai bang tests truoc khi sua service/repository.

Tasks:

- Them tests cho product admin list/query/detail de chung minh object store A khong xuat hien khi dang o store B.
- Them tests cho product update/delete theo id: object khong thuoc current store phai tra Not Found hoac error chuan, khong duoc update/delete.
- Them tests cho category admin list/query/detail/update/delete voi cung cross-store cases.
- Them tests cho duplicate slug/SKU rule theo store:
  - Cung store: duplicate bi chan.
  - Khac store: duplicate duoc phep neu index/domain rule cho phep.
- Them tests cho category parent: category store A khong duoc gan parent store B.
- Them tests cho page/topic store scope de xac nhan behavior hien co.

Exit criteria:

- Tests hien thi ro nhung path dang fail do unscoped query.
- Khong sua schema trong phase nay.
- QA checklist duoc cap nhat voi store mapping cases.

## Phase 1 - Product store scope hardening

Goal: tat ca product admin read/write paths phai store-scoped.

Status 2026-07-15: completed for active V2 CommerceNode product admin read/write and product SEO paths.

Implementation notes:

- CommerceNode product admin list/page/detail reads now use current-store scoped repository methods when `ICommerceStoreContext` is available.
- Product update/delete now return `Product not found` when the target product does not belong to the current store.
- Product create/update validates assigned category through current-store category ownership before persisting.
- Product SEO get/update now uses current-store readable product lookup and store-scoped slug duplicate checks.
- Legacy repository fallback methods keep legacy behavior without adding new V2 feature work to `AppDbContext`.

Verification:

- `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~CommerceNodeProductStoreScopeTests|FullyQualifiedName~ProductServiceTests.UpdateAsync_WhenProductBelongsToDifferentCurrentStore_ReturnsNotFound|FullyQualifiedName~ProductServiceTests.DeleteAsync_WhenProductBelongsToDifferentCurrentStore_ReturnsNotFound|FullyQualifiedName~ProductServiceTests.AddAsync_WhenCategoryBelongsToDifferentCurrentStore_ReturnsValidationFailure|FullyQualifiedName~ProductSeoServiceTests.UpdateAsync_WhenProductIsOutsideCurrentStore_ReturnsNotFound|FullyQualifiedName~ProductSeoServiceTests.UpdateAsync_WhenSlugExistsOnlyInAnotherStore_AllowsUpdate" --no-restore` passed 8/8.

Tasks:

- [x] Sua `CommerceNodeProductReadRepository.GetCatalogPageAsync` de filter theo current `storeId`.
- [x] Tach method admin product detail thanh store-scoped:
  - `GetProductDetailsByIdAsync(id, storeId)` hoac equivalent.
  - Path nao can global lookup phai dat ten ro va chi dung cho internal migration/admin superuser neu thuc su can.
- [x] Sua product service update/delete de verify product thuoc current store truoc khi thay doi.
- [x] Sua slug/SKU validation de scope theo store:
  - SKU unique theo `{StoreId, Sku}`.
  - Slug unique theo `{StoreId, Slug}`.
- [x] Khi gan category cho product, validate category thuoc cung current store.
- [x] Dam bao cache/catalog invalidation neu co phai theo store, khong clear nham store khac.
- [x] Khong them `StoreId` vao client request DTO; service lay store tu `ICommerceStoreContext`.

API/contract tasks:

- [x] Neu thay doi Commerce Admin endpoint contract, cap nhat OpenAPI operationId/summary/error schema/security metadata. No public request/response contract shape changed in this phase.
- [x] Them/duy tri contract tests cho response schema va expected 404/409. No endpoint schema change; service/repository guardrail tests added.

Exit criteria:

- [x] Product store A khong doc/sua/xoa duoc tu store B.
- [x] Duplicate slug/SKU duoc validate dung theo store.
- [x] Storefront product flows van dung `api/storefront/stores/{storeKey}/*`.

## Phase 2 - Category store scope hardening

Goal: category admin khong con dung generic unscoped repository cho luong V2.

Tasks:

- Thay `CategoryService.GetAllAsync`, `QueryAsync`, `GetByIdAsync` bang store-scoped query.
- Tao repository/query method ro rang cho category current store, uu tien `CommerceNodeDbContext` va `ICommerceStoreContext`.
- Sua update/delete de verify category thuoc current store.
- Sua `CategorySlugExistsAsync` de nhan `storeId` bat buoc hoac thay bang method scoped moi.
- Giu va test `ValidateParentAsync` de parent category phai cung store.
- Kiem tra luong archive/delete category de khong tac dong product/category cua store khac.

API/contract tasks:

- Expected cross-store access nen tra Not Found de khong expose object ton tai o store khac.
- Cap nhat OpenAPI/contract tests neu endpoint response/error thay doi.

Exit criteria:

- Category store A khong doc/sua/xoa duoc tu store B.
- Category tree khong the noi parent/child khac store.
- Storefront category navigation/catalog khong bi regress.

## Phase 3 - Data migration va required store ownership

Goal: xoa debt `StoreId` nullable tren Product/Category trong CommerceNode ma khong pha legacy.

Tasks:

- Audit du lieu Product/Category co `StoreId == null` trong CommerceNode.
- Tao migration backfill:
  - Neu CommerceNode chi co mot store, gan null rows vao store do.
  - Neu co nhieu store va null rows khong suy luan duoc, migration phai fail ro rang hoac yeu cau manual mapping truoc khi apply.
- Sau backfill, enforce required `StoreId` o CommerceNode model/database.
- Can than voi shared domain entity:
  - Neu doi CLR `Guid?` thanh `Guid` lam legacy `AppDbContext` bi anh huong, tam thoi giu CLR nullable va enforce `.IsRequired()`/DB constraint trong CommerceNode.
  - Chi doi domain nullability khi co phase tach legacy hoac user chap thuan migration rong hon.
- Xem xet FK Product/Category -> CommerceStore voi delete restrict neu EF model cho phep khong tac dong legacy.
- Them tests/migration verification cho null store rows.

Exit criteria:

- CommerceNode Product/Category moi va cu deu co store ownership.
- Startup migration cua CommerceNode chi tac dong `CommerceNodeDbContext`.
- Legacy `AppDbContext` khong bi them migration/feature V2.

## Phase 4 - Topic/page verification

Goal: confirm page/topic da store-scoped va bo sung guardrails can thiet.

Tasks:

- Them integration tests cho `StorefrontPageService`:
  - List pages chi lay current store.
  - Slug lookup chi lay current store.
  - Sitemap chi include pages cua current store.
- Verify admin/control page endpoints neu co:
  - Store scope den tu `storeKey`.
  - Cross-store detail/update/delete tra Not Found.
- Khong rewrite page model neu tests pass.

Exit criteria:

- Page/topic behavior co automated coverage.
- Khong phat sinh schema churn khong can thiet.

## Phase 5 - Discount store scope design, deferred

Goal: chuan bi nguyen tac cho discount ma khong implement khi domain chua ton tai.

Tasks khi discount feature duoc approve:

- Them domain rieng trong CommerceNode, vi du `Discount`, `DiscountRequirement`, `DiscountUsage`.
- Moi discount entity chinh phai co required `StoreId`.
- Discount code unique theo store, khong global.
- Checkout chi apply discount cua current store.
- Admin discount endpoints dung `api/commerce/admin/*?storeKey=`.
- Storefront discount apply dung route `api/storefront/stores/{storeKey}/*`.
- `DiscountTotal` hien tai trong checkout/order tiep tuc la output field cho den khi engine hoan chinh.

Exit criteria cho future phase:

- Khong co discount nao cross-store apply duoc.
- Usage/limits duoc dem theo store.

## Phase 6 - Module activation design, deferred

Goal: khong xay generic module system som; reuse concrete store config pattern.

Tasks hien tai:

- Ghi nhan `StorePaymentMethod` la pattern chuan cho module-like config:
  - `StoreId`
  - `PaymentMethodKey`
  - `Enabled`
  - `DisplayOrder`
  - `SettingsJson`
- Neu them module moi trong thoi gian ngan, tao concrete per-store config rieng thay vi generic registry.

Trigger de xay generic registry:

- Co it nhat 2-3 module doc lap can chung activation UI/API.
- Can common fields nhu `ModuleKey`, `Enabled`, `DisplayOrder`, `SettingsJson`, audit va health.
- Co nhu cau Control Plane manager quan ly module chung theo store.

Future shape neu du dieu kien:

```text
StoreModuleSetting
  Id
  StoreId
  ModuleKey
  Enabled
  DisplayOrder
  SettingsJson
  CreatedAt
  UpdatedAt
```

Exit criteria:

- Khong them abstraction khi chi mot feature can.
- Payment method per-store behavior khong bi pha.

## Phase 7 - Control Plane va manager integration

Goal: neu product/category/page manager can expose store mapping, Control Plane phai dung gateway boundary.

Tasks:

- ControlPlane.Web tiep tuc goi ControlPlane.API only.
- ControlPlane.API append/pass `storeKey` khi goi CommerceNode admin APIs.
- Manager UI nen hien current store context khi list/edit product/category/page.
- Error handling:
  - Cross-store object: Not Found.
  - Duplicate trong cung store: Conflict/validation error.
  - Store chua san sang/closed: theo store lifecycle rules da implement.
- Khong them direct CommerceNode client vao ControlPlane.Web.

Exit criteria:

- Network/API flow dung boundary docs.
- User khong the vo tinh edit object cua store khac qua manager.

## Phase 8 - QA, docs, va release gate

Goal: phase chi duoc ket thuc khi co verification du cho blast radius.

Tasks:

- Cap nhat checklist lien quan:
  - `QA-CommerceNode.todo.md`
  - `QA-ControlPlane.todo.md` neu Control Plane gateway/UI thay doi.
  - `QA-StorefrontV2.todo.md` neu storefront product/category/page flow thay doi.
- Chay focused tests cho CommerceNode services/controllers.
- Chay API contract/OpenAPI tests neu endpoint contract thay doi.
- Neu UI manager thay doi, dung Playwright de verify browser flow.
- Review diff de dam bao khong sua legacy presentation/AppDbContext ngoai y muon.

Release gate:

- Khong con unscoped Product/Category admin paths trong active V2.
- Product/Category/Page cross-store cases co tests.
- Migration/backfill co rollback/operational note.
- Discount/module deferred scope duoc ghi ro, khong co speculative code.

## Risk controls

| Risk | Control |
| --- | --- |
| Cross-store data leak | Bat buoc store-scoped query/detail/update/delete; Not Found cho object khac store. |
| Client spoof `StoreId` | Khong nhan `StoreId` tu request DTO; derive tu `storeKey` va context. |
| Nullable StoreId gay loi update | Backfill + CommerceNode required constraint theo phase. |
| Duplicate slug/SKU rule sai | Validate unique theo `{StoreId, Slug}` va `{StoreId, Sku}`. |
| Pha legacy | Khong them feature vao `AppDbContext`; can than khi doi shared domain nullability. |
| Sai runtime boundary | ControlPlane.Web khong goi CommerceNode truc tiep. |
| Over-engineering module/discount | Defer cho den khi domain/use case that su ton tai. |
| Cache stale/cross-store | Invalidate theo store khi product/category thay doi. |

## Recommended implementation order

1. Phase 0: tests va QA checklist.
2. Phase 1: product hardening.
3. Phase 2: category hardening.
4. Phase 4: page/topic verification.
5. Phase 3: migration/backfill/required ownership sau khi service behavior da on dinh.
6. Phase 7: Control Plane manager integration neu can UI/API gateway changes.
7. Phase 8: final QA/release gate.

Phase 5 va Phase 6 duoc giu la deferred design cho den khi discount/module feature duoc mo rieng.
