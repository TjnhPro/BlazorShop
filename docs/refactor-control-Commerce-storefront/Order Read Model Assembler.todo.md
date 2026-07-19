# Order Read Model Assembler

Status: planned
Date: 2026-07-18
Purpose: gom logic order read-model projection dang bi copy trong Commerce Node services vao mot assembler dung chung, giu authorization va route behavior hien co.

## Muc Tieu

- Loai bo duplicate mapping `Order -> GetOrder` trong active Commerce Node V2 services.
- Giu service hien tai chiu trach nhiem authorization/scope:
  - admin service scope theo current store/admin API.
  - customer service scope theo current customer.
  - guest service scope theo guest access token.
- Cho phep projection khac nhau theo visibility:
  - admin thay admin note va full history.
  - customer chi thay public/customer-safe fields.
  - guest chi thay order da duoc lookup bang access token, customer-safe fields.
- Khong doi public DTO shape `GetOrder`, `GetOrderLine`, `GetOrderPaymentSummary`, history/tracking DTO.
- Khong doi API route, OpenAPI contract, order placement, payment, shipment, checkout behavior.
- Khong refactor legacy `AppDbContext` order services trong phase nay.

## Codebase Baseline

Da kiem tra truc tiep:

- `BlazorShop.Infrastructure/Data/CommerceNode/Services/CommerceNodeOrderQueryService.cs`
  - 251 dong.
  - Co `MapOrdersAsync` tu dong 61.
  - Load product names, shipment tracking events, customer-visible order history, payment attempts, snapshots, lines.
  - Hien set `AdminNote = order.AdminNote` du service nay khong phai admin-only endpoint.
- `BlazorShop.Infrastructure/Data/CommerceNode/Services/StorefrontCustomerOrderService.cs`
  - 377 dong.
  - Co `MapOrdersAsync` tu dong 175.
  - Logic gan nhu trung voi `CommerceNodeOrderQueryService`.
  - Customer projection khong set `AdminNote`.
  - Payment summary hien khong expose `PaymentAttemptPublicId`/`ProviderKey`.
- `BlazorShop.Infrastructure/Data/CommerceNode/Services/StorefrontGuestOrderService.cs`
  - 211 dong.
  - Co `MapAsync` tu dong 70.
  - Guest lookup verify `GuestAccessTokenHash` va store truoc khi map.
  - Chua map tracking events.
  - Line product name chi dung `line.ProductName`, khong fallback sang product table.
  - Payment summary co `PaymentAttemptPublicId`/`ProviderKey`.
- `BlazorShop.Infrastructure/Data/CommerceNode/Services/CommerceNodeAdminOrderService.cs`
  - 478 dong.
  - Co `MapOrdersAsync` tu dong 289.
  - Admin projection co all history va admin note.
  - Line projection hien thieu mot so money fields so voi customer/query projection.
- `BlazorShop.Infrastructure/Data/CommerceNode/Services/OrderSnapshotProjection.cs`
  - 130 dong.
  - Da la internal shared projection helper cho order snapshot, chung minh codebase da chap nhan pattern gom projection dung chung.
- DI hien dang register:
  - `IOrderQueryService -> CommerceNodeOrderQueryService`
  - `IStorefrontGuestOrderService -> StorefrontGuestOrderService`
  - `IStorefrontCustomerOrderService -> StorefrontCustomerOrderService`
  - `IAdminOrderService -> CommerceNodeAdminOrderService`

## Autoplan Decisions

| Decision | Chon | Ly do |
| --- | --- | --- |
| Them abstraction khong | Co | Projection lap o 4 service, co policy visibility khac nhau va da co drift. |
| Dat abstraction o dau | `BlazorShop.Infrastructure/Data/CommerceNode/Services` | Assembler dung EF/CommerceNodeDbContext va nhan `Order` entity, khong nen dua len Application. |
| Interface hay concrete | Bat dau bang public concrete `OrderReadModelAssembler`; interface optional | Public services can inject public concrete safely; internal interface trong public constructor se gay accessibility issue. |
| Authorization nam o dau | Giu trong calling service | Guest token/customer ownership/admin store scope la access control, khong dua vao projection assembler. |
| Visibility model | Dung options object thay vi enum thuan | Hien co khac biet nho nhu guest payment attempt public id/provider key va tracking events; options de giu behavior. |
| Behavior phase dau | Khong doi behavior | Refactor nay phai mechanical truoc, behavior drift chi duoc lam bang phase/test rieng. |
| Legacy services | Out of scope | `Application.Services.Payment.OrderQueryService` va `Infrastructure.Services.Admin.AdminOrderService` thuoc legacy/AppDbContext path. |

## Kien Truc Dich

```text
CommerceNodeOrderQueryService
StorefrontCustomerOrderService
StorefrontGuestOrderService
CommerceNodeAdminOrderService
  -> OrderReadModelAssembler
       -> CommerceNodeDbContext
       -> OrderSnapshotProjection
       -> ProductVariantAttributeNormalizer
       -> GetOrder / GetOrderLine / GetOrderPaymentSummary
```

Suggested types:

```csharp
public sealed class OrderReadModelAssembler
{
    Task<IReadOnlyList<GetOrder>> BuildAsync(
        IReadOnlyCollection<Order> orders,
        OrderReadModelOptions options,
        CancellationToken cancellationToken = default);
}

public sealed record OrderReadModelOptions(
    OrderReadVisibility Visibility,
    bool IncludeTrackingEvents,
    bool IncludePaymentAttemptPublicReference,
    bool IncludeAdminNote,
    bool IncludeAllHistory);

public enum OrderReadVisibility
{
    Admin,
    Customer,
    Guest,
    Internal
}
```

Phase dau co the cung cap factory options de tranh caller tu dat sai flag:

```text
OrderReadModelOptions.Admin()
OrderReadModelOptions.Customer()
OrderReadModelOptions.Guest()
OrderReadModelOptions.Internal()
```

## Visibility Matrix

| Field/Child Data | Admin | Customer | Guest | Internal/Query |
| --- | --- | --- | --- | --- |
| Store snapshot | yes | yes | yes | yes |
| Billing/shipping snapshot | yes | yes | yes | yes |
| Order lines | yes | yes | yes | yes |
| Product name fallback from Products | yes | yes | keep current first, then consider yes | yes |
| Payment summary | yes | yes | yes | yes |
| Payment attempt public id/provider key | yes | no by current behavior | yes by current behavior | yes by current behavior |
| Tracking events | current admin service no, can add later | yes | current guest no, add later only if approved | yes |
| History entries | all | visible-to-customer only | visible-to-customer only | visible-to-customer unless proven admin-only |
| Admin note | yes | no | no | review current behavior, likely no unless active consumer needs it |

## Phase 0 - Inventory And Behavioral Lock

- [x] Re-run `rg` for all `MapOrdersAsync`, `MapAsync(Order`, `CreatePaymentSummary`, `GetOrderLine`, `GetOrderHistoryEntry`, `GetShipmentTrackingEvent`.
- [x] Confirm active V2 consumers of:
  - `CommerceNodeOrderQueryService`
  - `StorefrontCustomerOrderService`
  - `StorefrontGuestOrderService`
  - `CommerceNodeAdminOrderService`
- [x] Confirm whether `IOrderQueryService` remains active after `Storefront V2 Commerce Flow Cutover.todo.md`.
- [x] Capture current behavior as tests before refactor:
  - admin sees `AdminNote`.
  - admin sees all history including not visible to customer.
  - customer does not see `AdminNote`.
  - customer sees only `VisibleToCustomer` history.
  - guest lookup requires valid access token and current store.
  - guest sees only `VisibleToCustomer` history.
  - current payment summary field differences remain documented.
  - current tracking event differences remain documented.
- [x] Decide whether `CommerceNodeOrderQueryService` should use `Customer`, `Admin`, or `Internal` visibility while it still exists.

Phase 0 notes:

- Inventory found the active duplicate projection methods in `CommerceNodeAdminOrderService`, `StorefrontCustomerOrderService`, `StorefrontGuestOrderService`, and the still-present `CommerceNodeOrderQueryService`.
- `StorefrontScopedOrdersController` consumes `IStorefrontGuestOrderService` and `IStorefrontCustomerOrderService`; `CommerceOrdersController` consumes `IAdminOrderService`.
- `StorefrontCommerceFlowCutoverTests` currently asserts `IOrderQueryService -> CommerceNodeOrderQueryService` is not registered in active Commerce Node DI and not injected into `StorefrontScopedOrdersController`. Treat `CommerceNodeOrderQueryService` visibility as legacy/internal-reference while it still exists.
- `OrderReadModelBehaviorLockTests` captures current projection differences:
  - admin: `AdminNote`, all history, payment attempt public id/provider key, no tracking events.
  - customer: no `AdminNote`, customer-visible history only, tracking events included, payment attempt public id/provider key hidden.
  - guest: valid token and current store required, no `AdminNote`, customer-visible history only, no tracking events, payment attempt public id/provider key retained, no product-table fallback.
  - legacy/internal query: user-id scoped, `AdminNote` currently included, visible history only, tracking events included, payment attempt public id/provider key retained.

Acceptance:

- [x] Tests describe current behavior before moving projection code.
- [x] Any intentional future behavior correction is separated from mechanical refactor.
- [x] No legacy `AppDbContext` service is included in the scope.

## Phase 1 - Add OrderReadModelOptions And Assembler Skeleton

- [x] Add `OrderReadVisibility` enum under `BlazorShop.Infrastructure/Data/CommerceNode/Services` or a nearby internal models folder.
- [x] Add `OrderReadModelOptions` with safe factory methods:
  - `Admin()`
  - `Customer()`
  - `Guest()`
  - `Internal()`
- [x] Add public concrete `OrderReadModelAssembler`.
- [x] Inject `CommerceNodeDbContext` into assembler.
- [x] Register assembler in `BlazorShop.Infrastructure/Data/CommerceNode/DependencyInjection.cs`.
- [x] Keep `OrderSnapshotProjection` as existing helper; do not duplicate snapshot parsing inside service callers.
- [x] Add focused tests for options factory defaults.

Phase 1 notes:

- Added `OrderReadVisibility`, `OrderReadModelOptions`, and `OrderReadModelAssembler` under `BlazorShop.Infrastructure/Data/CommerceNode/Services`.
- `OrderReadModelOptions` includes the flags needed to preserve Phase 0 behavior drift: tracking events, payment attempt public reference, admin note, all-history visibility, product-name fallback, and line money detail projection.
- `OrderReadModelAssembler` is registered in Commerce Node DI but no service consumes it yet.
- Focused tests: `OrderReadModelOptionsTests`.

Acceptance:

- [x] Project builds with assembler registered.
- [x] No service uses assembler yet.
- [x] Options defaults match current behavior matrix.

## Phase 2 - Move Shared Child Data Loaders Into Assembler

- [x] Move product name lookup into assembler:
  - collect product ids from order lines.
  - query `context.Products.AsNoTracking()`.
  - build product name dictionary.
- [x] Move payment attempt summary lookup into assembler:
  - query latest `PaymentAttempts` by `UpdatedAtUtc`.
  - map to `GetOrderPaymentSummary`.
  - include/exclude public id/provider key based on options.
- [x] Move history lookup into assembler:
  - all history for admin.
  - `VisibleToCustomer` only for customer/guest/internal current behavior.
- [x] Move tracking event lookup into assembler:
  - support include/exclude by options.
  - preserve existing order by `OccurredAtUtc`.
- [x] Keep all EF queries batched by `orderIds`, not per order.

Phase 2 notes:

- Added private batched child loaders inside `OrderReadModelAssembler` for product names, payment summaries, order history, and shipment tracking events.
- Loader visibility is gated by `OrderReadModelOptions` so customer/guest/admin differences remain explicit before services migrate.
- Loader methods stay private to avoid widening the runtime surface only for tests; `OrderReadModelOptionsTests` includes a source-shape guard for batching and option gates.
- `BuildAsync` still returns immediately for empty input and still does not project non-empty orders until Phase 3.

Acceptance:

- [x] Assembler can build lookup dictionaries for multiple orders with one query per child data type.
- [x] No N+1 query pattern introduced.
- [x] Empty order collection returns empty list without DB child queries.

## Phase 3 - Move Core Order And Line Projection Into Assembler

- [x] Move common `new GetOrder` mapping into assembler.
- [x] Move `CreatePaymentSummary` into assembler.
- [x] Move `GetOrderLine` mapping into assembler.
- [x] Preserve existing fields:
  - currency and base currency values.
  - totals and base totals.
  - exchange rate metadata.
  - shipping snapshot and legacy shipping fields.
  - shipping method snapshot.
  - completed/cancelled timestamps.
  - variant attributes via `ProductVariantAttributeNormalizer.Deserialize`.
- [x] Preserve admin/customer/guest differences through options.
- [x] Keep line money fields behavior identical per caller until a later behavior correction phase.

Phase 3 notes:

- `OrderReadModelAssembler.BuildAsync` now projects non-empty order collections to `GetOrder`.
- Added `IncludeUserId` to `OrderReadModelOptions` so admin/internal projections keep current `UserId` behavior while customer/guest projections stay hidden.
- Projection tests cover admin, customer, guest, and internal option shapes directly against the assembler.
- Services still use their old private mapping methods until migration phases 4-6.

Acceptance:

- [x] Unit tests compare assembler output to existing service behavior for representative admin/customer/guest orders.
- [x] Mapping remains deterministic for order of lines, history and tracking events.
- [x] No public DTO shape changes.

## Phase 4 - Migrate Customer And Query Services First

These two have the largest duplicate block and lowest admin mutation risk.

- [x] Inject `OrderReadModelAssembler` into `StorefrontCustomerOrderService`.
- [x] Replace private `MapOrdersAsync` with assembler call using `OrderReadModelOptions.Customer()`.
- [x] Remove duplicate `CreatePaymentSummary` from customer service.
- [x] Inject `OrderReadModelAssembler` into `CommerceNodeOrderQueryService`.
- [x] Replace private `MapOrdersAsync` with assembler call using selected `Internal()` or documented visibility.
- [x] Remove duplicate `CreatePaymentSummary` from query service.
- [x] Keep `CreateOwnedOrderQuery`, customer scope resolution and store scope logic unchanged.
- [x] Keep `GetCurrentStoreOrdersAsync` logic unchanged.

Phase 4 notes:

- `StorefrontCustomerOrderService` now delegates read-model projection to `OrderReadModelAssembler` with `OrderReadModelOptions.Customer()`.
- `CommerceNodeOrderQueryService` now delegates read-model projection to `OrderReadModelAssembler` with `OrderReadModelOptions.Internal()` to preserve the current legacy/internal-reference visibility.
- Tests that construct the services directly now pass the registered concrete assembler dependency.
- Initial parallel build attempt hit a transient `VBCSCompiler` file lock; after `dotnet build-server shutdown`, CommerceNode API build passed.

Acceptance:

- [x] `StorefrontCustomerOrderServiceTests` pass.
- [x] `CommerceNodeOrderQueryServiceTests` pass.
- [x] Customer still cannot see admin note.
- [x] Store scoping remains in service query, not assembler.

## Phase 5 - Migrate Guest Service

- [x] Inject `OrderReadModelAssembler` into `StorefrontGuestOrderService`.
- [x] Keep guest access token validation exactly where it is.
- [x] Replace private `MapAsync` with assembler call using `OrderReadModelOptions.Guest()`.
- [x] Preserve current guest behavior:
  - valid access token required.
  - current store required.
  - only customer-visible history.
  - payment attempt public id/provider key retained if current tests rely on it.
  - tracking events not added unless separately approved.
- [x] Remove duplicate guest payment/history/line mapping.

Phase 5 notes:

- `StorefrontGuestOrderService` still performs reference, access token hash, token expiry, and current-store checks before projection.
- The authorized order is now projected through `OrderReadModelAssembler` with `OrderReadModelOptions.Guest()`.
- Checkout snapshot tests were updated for the new direct service constructor dependency and continue to validate guest lookup/wrong-token/wrong-store behavior.

Acceptance:

- [x] `StorefrontGuestOrderServiceTests` pass.
- [x] `StorefrontCheckoutServiceTests` guest lookup/order snapshot cases pass.
- [x] Invalid token and wrong store still return not found.
- [x] Guest does not gain admin-only data.

## Phase 6 - Migrate Admin Service

- [ ] Inject `OrderReadModelAssembler` into `CommerceNodeAdminOrderService`.
- [ ] Replace private `MapOrdersAsync` with assembler call using `OrderReadModelOptions.Admin()`.
- [ ] Keep admin mutations unchanged:
  - update tracking.
  - update shipping status.
  - update admin note.
  - complete.
  - cancel.
  - audit log writes.
- [ ] Keep current store query/scope in admin service.
- [ ] Remove duplicate `CreatePaymentSummary` from admin service.
- [ ] Decide in tests whether admin should continue current no-tracking-events behavior or begin receiving tracking events:
  - If no behavior change, set admin options to exclude tracking events first.
  - If adding tracking events is desired, make it a separate assertion and update QA/docs.

Acceptance:

- [ ] Admin order tests pass.
- [ ] Admin sees admin note and all history.
- [ ] Admin mutation responses still return fresh projected order.
- [ ] Audit behavior unchanged.

## Phase 7 - Remove Duplicate Mapping And Lock The Pattern

- [ ] Remove old private mapping methods from migrated services.
- [ ] Add static/code-shape tests or focused unit tests to prevent new `MapOrdersAsync` copies in these services.
- [ ] Add assembler tests for:
  - empty input.
  - multi-order batched projection.
  - all/customer-visible history filtering.
  - payment attempt latest selection.
  - admin note visibility.
  - product name fallback.
  - variant attribute deserialization.
- [ ] Document in tests or comments that authorization must stay in calling services.

Acceptance:

- [ ] No duplicate `Order -> GetOrder` projection remains in the four active Commerce Node V2 services.
- [ ] A new order read consumer can use assembler without copying projection logic.
- [ ] Security-sensitive ownership checks remain outside assembler.

## Phase 8 - Optional Behavior Corrections After Mechanical Refactor

Only start after Phases 1-7 pass.

- [ ] Review whether `CommerceNodeOrderQueryService` should expose `AdminNote`; likely no unless only admin path uses it.
- [ ] Review whether guest order detail should include tracking events.
- [ ] Review whether admin order projection should include all line money fields currently present in customer/query projection.
- [ ] Review whether customer/guest payment summary should hide provider/public attempt data consistently.
- [ ] Any behavior change must include:
  - before/after test.
  - API contract impact review.
  - QA todo update.

Acceptance:

- [ ] Behavior corrections are explicit and not hidden inside refactor.
- [ ] Storefront account/guest order views keep safe customer-facing projection.
- [ ] Admin order detail becomes richer only by approved behavior change.

## Phase 9 - QA And Verification

- [ ] Run focused tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~StorefrontCustomerOrderServiceTests"`
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~StorefrontGuestOrderServiceTests"`
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~CommerceNodeOrderQueryServiceTests"`
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~CommerceNodeAdminShipmentServiceTests"`
- [ ] Run order/checkout focused tests that cover guest lookup and order snapshot:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~StorefrontCheckoutServiceTests"`
- [ ] Run build:
  - `dotnet build BlazorShop.sln`
- [ ] If Storefront/account order browser behavior is affected, run relevant Playwright E2E cases from `Storefront Playwright E2E Release.todo.md`:
  - account order list/detail/receipt.
  - guest order completion/receipt if covered.
  - checkout place COD order.
- [ ] Update QA docs only if externally visible behavior changes:
  - `QA-CommerceNode.todo.md`
  - `QA-StorefrontV2.todo.md`

Acceptance:

- [ ] Tests pass.
- [ ] No DTO/schema route change.
- [ ] Customer/guest cannot see admin-only data.
- [ ] Admin still sees admin-specific order data.

## Out Of Scope

- [ ] Rewriting order placement.
- [ ] Changing checkout session/order transaction behavior.
- [ ] Changing payment provider behavior.
- [ ] Changing shipment write/update services except reading projected order responses.
- [ ] Moving authorization into assembler.
- [ ] Moving EF projection into Application layer.
- [ ] Refactoring legacy `AppDbContext` services:
  - `BlazorShop.Application/Services/Payment/OrderQueryService.cs`
  - `BlazorShop.Infrastructure/Services/Admin/AdminOrderService.cs`
- [ ] Changing public HTTP routes or OpenAPI operation IDs.
- [ ] Changing customer/guest order visibility without a separate approved behavior phase.

## Risks And Controls

| Risk | Control |
| --- | --- |
| Customer/guest accidentally see admin note | Visibility tests for customer and guest. |
| Guest access token check moves into projection and weakens security | Keep token lookup in `StorefrontGuestOrderService`; assembler accepts already-authorized orders only. |
| Admin loses full history | Admin assembler option includes all history; admin tests assert non-public history visible. |
| N+1 queries introduced | Assembler batches child queries by order ids. |
| Existing payment summary differences break UI | Preserve current options first; review normalization later. |
| `internal` interface causes public constructor accessibility errors | Use public concrete assembler or public infrastructure-only interface. |
| Behavior change hidden inside refactor | Phase 8 separates optional corrections from mechanical migration. |

## Definition Of Done

- [ ] `OrderReadModelAssembler` is the single active Commerce Node V2 place that maps `Order` entity collections to `GetOrder` read models.
- [ ] Four active V2 services no longer maintain separate `Order -> GetOrder` projection copies.
- [ ] Authorization/store/customer/guest scope remains in the service methods.
- [ ] Admin/customer/guest visibility is encoded in options and protected by tests.
- [ ] Existing external API contracts and DTO shapes remain unchanged.
- [ ] Focused order/customer/guest/admin tests pass.
