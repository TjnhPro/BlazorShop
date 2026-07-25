# Storefront Independence Boundary.todo

Status: in progress

Goal: tách hoàn toàn Storefront presentation/platform khỏi cụm Control Plane, Commerce Node implementation và `BlazorShop.Web.SharedV2`. Sau phase này, `Storefront.V2`, `Storefront.WASM`, `Storefront.Components`, `Storefront.Starter` và storefront sinh theo `BlazorShop.Storefront.{Name}` không được phụ thuộc source/project vào Control Plane, Commerce Node API, backend core implementation, hoặc `Web.SharedV2`.

Important distinction: Storefront vẫn cần gọi Commerce Node Storefront API ở runtime qua HTTP contract. Dependency được phép là `Storefront.Client` generated từ OpenAPI và `Storefront.Runtime` server/BFF primitives. Dependency không được phép là project/source reference tới `BlazorShop.CommerceNode.API`, `BlazorShop.ControlPlane.*`, `BlazorShop.Application`, `BlazorShop.Domain`, `BlazorShop.Infrastructure`, hoặc `BlazorShop.Web.SharedV2`.

## Current codebase evidence

- `Storefront.V2` hiện đã reference `Storefront.Client`, `Storefront.Runtime`, `Storefront.Components`, `Storefront.WASM`.
- `Storefront.V2` vẫn còn `ProjectReference` tới `BlazorShop.Web.SharedV2`.
- `Storefront.WASM` hiện chỉ reference `Storefront.Components`, nên boundary ban đầu sạch.
- `Storefront.Components` không reference `Web.SharedV2`, Control Plane, Commerce Node API, Application, Domain hoặc Infrastructure.
- `Storefront.Starter` dùng package `BlazorShop.Storefront.Client` và `BlazorShop.Storefront.Runtime`, không reference `Web.SharedV2`.
- `Web.SharedV2` vẫn chứa browser/auth helpers, JS interop, JWT-related code, toast helpers và business/transitional models.
- Storefront V2 source hiện còn dùng `Web.SharedV2` chủ yếu qua shared constants như `StorefrontCookieNames` và `RoleNames`, không phải qua `Web.SharedV2.Models` trực tiếp trong source chính.
- Control Plane Web vẫn dùng `Web.SharedV2` thật cho browser storage, token service, HTTP helper, auth state, refresh handler, toast và auth models. Không nên kéo Control Plane vào cùng một big-bang migration.
- Working tree hiện đang có thay đổi Storefront/cart runtime in-flight; phase triển khai phải baseline lại trước khi sửa để tránh trộn refactor boundary với cart behavior.

## Target dependency graph

```text
Allowed Storefront platform graph

Storefront.V2
  -> Storefront.Runtime
      -> Storefront.Client
  -> Storefront.Components
  -> Storefront.WASM

Storefront.WASM
  -> Storefront.Components

Storefront.Starter
  -> package BlazorShop.Storefront.Client
  -> package BlazorShop.Storefront.Runtime
  -> package/component reference only when explicitly introduced

Storefront.{Name}
  -> package BlazorShop.Storefront.Client
  -> package BlazorShop.Storefront.Runtime
  -> package BlazorShop.Storefront.Components when presentation components are reused
```

```text
Forbidden Storefront source/project graph

Storefront.*
  -/-> ControlPlane.*
  -/-> CommerceNode.API
  -/-> Application
  -/-> Domain
  -/-> Infrastructure
  -/-> Web.SharedV2
  -/-> Web.SharedV2.Models
```

## Non-goals

- Không xóa `Web.SharedV2` ngay trong phase này.
- Không tách toàn bộ Control Plane khỏi `Web.SharedV2` trong cùng phase.
- Không rewrite Storefront UI.
- Không đổi Commerce Node API behavior.
- Không copy Commerce Node DTO/domain entity vào Storefront.
- Không tạo shared project mới như `Storefront.Web.Shared` khi chưa có ít nhất hai consumer thật cần cùng một primitive.
- Không đưa generated Commerce Node client trực tiếp vào WASM/browser.
- Không biến `Storefront.V2` thành template neutral; V2 vẫn là storefront production/reference host.

## Phase SIB0 - Scope lock, baseline and in-flight work check

- [x] Ghi baseline commit SHA.
- [x] Kiểm tra working tree và phân biệt:
  - [x] Thay đổi in-flight của cart/runtime đang có.
  - [x] File sẽ chạm cho boundary decoupling.
  - [x] File không được đụng trong phase này.
- [x] Chạy inventory dependency graph:
  - [x] `ProjectReference` của `Storefront.V2`.
  - [x] `ProjectReference` của `Storefront.WASM`.
  - [x] `ProjectReference` của `Storefront.Components`.
  - [x] Package references của `Storefront.Starter`.
  - [x] Namespace usages `BlazorShop.Web.SharedV2`.
  - [x] Namespace usages `BlazorShop.ControlPlane`.
  - [x] Namespace usages `BlazorShop.CommerceNode`.
  - [x] Namespace usages `BlazorShop.Application`, `Domain`, `Infrastructure`.
- [x] Ghi migration matrix:
  - [x] File.
  - [x] Current dependency.
  - [x] Symbol used.
  - [x] Target owner.
  - [x] Replacement action.
  - [x] Required test.
- [x] Không bắt đầu migration nếu cart/runtime in-flight chưa build được hoặc chưa được chủ động chấp nhận làm baseline.

### SIB0 QA gate

- [x] `dotnet build BlazorShop.sln`.
- [x] Focused Storefront architecture tests hiện có.
- [x] Ghi danh sách dependency offenders vào QA note.
- [x] Nếu build đang fail vì in-flight change không thuộc phase này, ghi rõ blocker trước khi triển khai.

## Phase SIB1 - Guardrails first

- [x] Thêm hoặc siết architecture tests cho Storefront source/project dependencies:
  - [x] `Storefront.V2` không được reference `Web.SharedV2`.
  - [x] `Storefront.V2` không được import `BlazorShop.Web.SharedV2`.
  - [x] `Storefront.V2` không được reference/import `ControlPlane.*`.
  - [x] `Storefront.V2` không được reference/import `CommerceNode.API`.
  - [x] `Storefront.V2` không được reference/import `Application`, `Domain`, `Infrastructure`.
  - [x] `Storefront.WASM` chỉ được reference browser-safe Storefront packages.
  - [x] `Storefront.Components` không được reference Runtime, Client, V2, Web.SharedV2, backend/core/API projects.
  - [x] `Storefront.Runtime` không được reference V2, Components, WASM, Web.SharedV2, backend/core/API projects.
  - [x] `Storefront.Client` không được reference V2, Runtime, Components, Web.SharedV2, backend/core/API projects.
  - [x] `Storefront.Starter` không được reference V2, Web.SharedV2, Control Plane, Commerce Node API, backend/core projects.
- [x] Guardrail test phải phân biệt source dependency và allowed HTTP contract dependency qua `Storefront.Client`.
- [x] Guardrail ban đầu có thể có expected failure cho `Storefront.V2 -> Web.SharedV2`; phase SIB3 phải làm test pass.

### SIB1 QA gate

- [x] Focused architecture tests chạy và fail/pass đúng theo trạng thái hiện tại.
- [x] Test message khi fail phải chỉ rõ offender file/reference.
- [x] Không thay đổi runtime behavior.

## Phase SIB2 - Storefront-owned constants and host primitives extraction

- [x] Di chuyển `StorefrontCookieNames` ra khỏi `Web.SharedV2` cho Storefront:
  - [x] Target owner ưu tiên: `Storefront.V2` vì cookie/session/cart-token/currency preference là host/BFF policy.
  - [x] Nếu `Starter` hoặc generated storefront cần cùng cookie names thật sự, sau này cân nhắc `Storefront.Runtime`; không move sớm nếu chỉ V2 dùng.
- [x] Thay usages trong:
  - [x] `StorefrontCartTokenService`.
  - [x] `StorefrontDisplayContextProvider`.
  - [x] `StorefrontRateLimitIdentity`.
  - [x] Cart/checkout/auth/consent/media/SEO endpoints đang đọc/xóa cookie.
- [x] Di chuyển hoặc thay `RoleNames.Admin` trong Storefront:
  - [x] Target owner ưu tiên: V2-local `StorefrontRoleNames` hoặc auth/session helper.
  - [x] Không kéo admin role constant từ Control Plane/shared project vào Storefront.
  - [x] Nếu về lâu dài role claim names cần contract chung, định nghĩa qua API/claims contract rõ, không qua `Web.SharedV2`.
- [x] Xóa `using BlazorShop.Web.SharedV2` khỏi Storefront V2 files đã thay.
- [x] Không sửa logic đọc/ghi cookie ngoài namespace/owner move.

### SIB2 QA gate

- [x] Unit/focused tests cho cart token cookie vẫn pass.
- [x] Focused tests cho currency preference cookie vẫn pass.
- [x] Focused tests cho admin maintenance access/session vẫn pass nếu có coverage.
- [x] `rg "BlazorShop.Web.SharedV2" BlazorShop.PresentationV2/BlazorShop.Storefront.V2` chỉ còn Docker/tailwind/project offenders chưa xử lý ở phase sau, hoặc empty nếu xử lý luôn.

## Phase SIB3 - Remove Storefront.V2 project/build dependency on Web.SharedV2

- [x] Xóa `ProjectReference` tới `BlazorShop.Web.SharedV2` khỏi `Storefront.V2.csproj`.
- [x] Xóa copy step `Web.SharedV2` khỏi `Storefront.V2/Dockerfile`.
- [x] Xóa `../BlazorShop.Web.SharedV2/**/*.razor` và `../BlazorShop.Web.SharedV2/**/*.cs` khỏi `Storefront.V2/tailwind.config.js`.
- [x] Xóa `using BlazorShop.Web.SharedV2` còn lại trong `Program.cs` và endpoints/services.
- [x] Verify Storefront static assets không phụ thuộc class scan từ shared project.
- [x] Không xóa `Web.SharedV2` project vì Control Plane vẫn dùng.

### SIB3 QA gate

- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj`.
- [x] `dotnet publish BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj`.
- [x] Dockerfile static scan không copy `Web.SharedV2`.
- [x] Tailwind config static scan không include `Web.SharedV2`.
- [x] Architecture test `Storefront.V2_DoesNotReferenceWebSharedV2` pass.

## Phase SIB4 - Storefront business model and DTO boundary audit

- [ ] Confirm `Storefront.V2` không import `Web.SharedV2.Models`.
- [ ] Confirm `Storefront.Components` không có HTTP/API DTO clone:
  - [ ] Product/category render models chỉ chứa presentation state.
  - [ ] Cart/checkout/account render models không chứa server-owned fields.
  - [ ] Không có admin-only fields, credentials, secrets, internal store ownership fields.
- [ ] Confirm `Storefront.Runtime` dùng neutral result/context/capability primitives, không chứa V2-specific route, layout, cookie, CSS, Razor component.
- [ ] Confirm `Storefront.Client` là generated/source-of-truth HTTP contract, không thêm handwritten duplicate DTO.
- [ ] Đối với DTO đang sống trong `Storefront.V2/Services/Contracts`:
  - [ ] Nếu là same-origin BFF contract: giữ V2-local.
  - [ ] Nếu là Storefront API contract: thay bằng generated client DTO hoặc runtime projection.
  - [ ] Nếu là render model: đưa về Components feature folder.
- [ ] Không migrate toàn bộ model cùng lúc; đi theo capability slice.

### SIB4 QA gate

- [ ] Static guard cấm `Web.SharedV2.Models` trong tất cả `Storefront.*`.
- [ ] Static guard cấm `BlazorShop.Application.DTOs` trong all `Storefront.*`.
- [ ] Generated client compile tests pass.
- [ ] Component guardrail tests pass.

## Phase SIB5 - Storefront API access boundary hardening

- [ ] Ensure Storefront API access shape là:
  - [ ] V2 SSR/BFF calls `Storefront.Runtime`.
  - [ ] `Storefront.Runtime` calls `Storefront.Client`.
  - [ ] `Storefront.Client` calls Commerce Node Storefront API via HTTP.
  - [ ] Browser/WASM calls same-origin V2 BFF endpoints only.
- [ ] Không cho browser/WASM có Commerce Node base URL hoặc generated Commerce Node client trực tiếp.
- [ ] Không cho Storefront host gọi Control Plane API.
- [ ] Không cho Storefront host đọc node credentials hoặc Control Plane registry.
- [ ] Current store resolution vẫn ở V2 boundary và store scope được truyền qua `storeKey` route/API contract.
- [ ] Nếu có manual `StorefrontApiClient` còn lại:
  - [ ] Đưa vào exception registry.
  - [ ] Ghi capability, owner, test, revisit trigger.
  - [ ] Không copy sang Starter/generated storefront.

### SIB5 QA gate

- [ ] Static scan không có `ControlPlane` namespace trong `Storefront.*`.
- [ ] Static scan không có `CommerceNode.API` namespace/project reference trong `Storefront.*`.
- [ ] Static scan không có `Application`, `Domain`, `Infrastructure` project reference trong `Storefront.*`.
- [ ] Browser network assertion: protected Storefront browser actions không gọi trực tiếp Commerce Node host.
- [ ] Storefront generated-client tests pass.

## Phase SIB6 - Starter and Storefront.{Name} independence contract

- [ ] Update docs/guardrails cho `Storefront.Starter`:
  - [ ] Package-first consumption.
  - [ ] Không reference `Storefront.V2`.
  - [ ] Không reference `Web.SharedV2`.
  - [ ] Không reference backend/core/API projects.
  - [ ] Không copy manual `StorefrontApiClient`.
  - [ ] Protected browser actions phải qua same-origin BFF.
- [ ] Define rules cho `Storefront.{Name}`:
  - [ ] Project sinh theo `BlazorShop.Storefront.{Name}`.
  - [ ] Không nằm trong solution active mặc định.
  - [ ] Dùng package `Storefront.Client`, `Storefront.Runtime`, `Storefront.Components`.
  - [ ] Không đoán response shape; dùng generated contract.
  - [ ] Không phụ thuộc source của V2/ControlPlane/Commerce.
- [ ] Chỉ cần compile/static proof cho Starter/generated contract ở phase này; chưa chạy production browser QA cho generated storefront.

### SIB6 QA gate

- [ ] `StorefrontStarterFoundationBoundaryTests` pass.
- [ ] Generated storefront static guard nếu có pass.
- [ ] Package pack/restore proof cho `Storefront.Client` và `Storefront.Runtime`.
- [ ] No `Web.SharedV2` reference trong Starter/generated sample source.

## Phase SIB7 - Control Plane remaining Web.SharedV2 containment

- [ ] Không tách Control Plane trong cùng phase, nhưng cần containment để Storefront không bị kéo ngược lại.
- [ ] Ghi rõ `Web.SharedV2` hiện là transitional Control Plane/shared browser helper bucket.
- [ ] Cấm thêm Storefront-specific file vào `Web.SharedV2`.
- [ ] Cấm thêm Storefront business model vào `Web.SharedV2/Models`.
- [ ] Nếu `Web.SharedV2` chỉ còn Control Plane dùng sau Storefront cutover:
  - [ ] Plan sau sẽ merge về `ControlPlane.Web`.
  - [ ] Hoặc tách helper generic nhỏ nếu có ít nhất hai active consumer thật.
- [ ] Không để Control Plane auth/token/JWT helper được tái sử dụng ngầm bởi Storefront.

### SIB7 QA gate

- [ ] Architecture test freeze `Web.SharedV2/Models` folders vẫn pass.
- [ ] Architecture test cấm new Storefront namespace trong `Web.SharedV2`.
- [ ] Control Plane build pass.
- [ ] Control Plane auth focused tests pass nếu có.

## Phase SIB8 - Storefront.V2 functional regression QA

- [ ] Chạy Storefront V2 functional smoke ở mức build/test trước.
- [ ] Chạy Playwright browser QA cho các flow bị ảnh hưởng bởi boundary move:
  - [ ] Home/store bootstrap.
  - [ ] Product detail.
  - [ ] Add to cart.
  - [ ] Cart load/update/remove.
  - [ ] Checkout start/review/place order COD nếu cart/checkout code bị chạm.
  - [ ] Login/logout.
  - [ ] Maintenance/admin access nếu auth role parsing bị chạm.
  - [ ] Currency preference nếu cookie name bị chạm.
- [ ] Network assertion:
  - [ ] Browser chỉ gọi same-origin `/api/*`.
  - [ ] Không gọi Control Plane.
  - [ ] Không gọi Commerce Node host trực tiếp từ browser.
- [ ] Public response assertion:
  - [ ] Không trả provider secret.
  - [ ] Không trả internal settings.
  - [ ] Không trả node credential/control-plane information.

### SIB8 QA gate

- [ ] `dotnet build BlazorShop.sln`.
- [ ] Focused Storefront tests pass.
- [ ] Focused Control Plane build/tests pass để chứng minh không phá shared consumer còn lại.
- [ ] Playwright targeted QA pass trên `Storefront.V2`.
- [ ] QA evidence được ghi vào `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md` hoặc QA report riêng nếu phase triển khai yêu cầu.

## Definition of Done

- [ ] `Storefront.V2.csproj` không reference `Web.SharedV2`.
- [ ] `Storefront.V2` source không import `BlazorShop.Web.SharedV2`.
- [ ] `Storefront.V2` Dockerfile không copy `Web.SharedV2`.
- [ ] `Storefront.V2` Tailwind config không scan `Web.SharedV2`.
- [ ] `Storefront.WASM` không reference `Web.SharedV2`, Control Plane, Commerce Node API, Application, Domain, Infrastructure.
- [ ] `Storefront.Components` không reference `Web.SharedV2`, Runtime, Client, V2, Control Plane, Commerce Node API, Application, Domain, Infrastructure.
- [ ] `Storefront.Runtime` không reference `Web.SharedV2`, V2, Components, WASM, Control Plane, Commerce Node API, Application, Domain, Infrastructure.
- [ ] `Storefront.Client` không reference `Web.SharedV2`, V2, Runtime, Components, Control Plane, Commerce Node API, Application, Domain, Infrastructure.
- [ ] `Storefront.Starter` không reference `Web.SharedV2`, V2, Control Plane, Commerce Node API, Application, Domain, Infrastructure.
- [ ] Generated/custom `Storefront.{Name}` rules cấm `Web.SharedV2` và backend/core/API source dependencies.
- [ ] Storefront vẫn gọi Commerce Node Storefront API được qua generated `Storefront.Client`/`Runtime`.
- [ ] Control Plane Web vẫn build và hoạt động với `Web.SharedV2` tạm thời.
- [ ] Storefront V2 Playwright targeted QA pass cho flow bị ảnh hưởng.

## Suggested implementation order

1. SIB0 inventory and baseline.
2. SIB1 guardrails.
3. SIB2 move Storefront-owned constants.
4. SIB3 remove `Web.SharedV2` project/build dependency from V2.
5. SIB4 audit DTO/model boundary.
6. SIB5 harden API access dependency boundary.
7. SIB6 document Starter/generated storefront independence contract.
8. SIB7 contain remaining Control Plane usage of `Web.SharedV2`.
9. SIB8 run Storefront.V2 targeted QA.

## Suggested verification commands

```powershell
dotnet build BlazorShop.sln
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~Storefront"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~Architecture"
```

For browser QA after implementation:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

Then run the existing Playwright harness or add a focused script only if the current harness cannot express the boundary regression cases.

## Risk controls

- [ ] Do not move Control Plane auth/token helpers during Storefront decoupling.
- [ ] Do not move business DTOs manually into `Storefront.Client`.
- [ ] Do not add a new shared package unless at least two real consumers exist.
- [ ] Do not remove `Web.SharedV2` until Control Plane migration has its own phase.
- [ ] Do not change cookie names or role semantics while moving constants.
- [ ] Do not mix cart runtime behavior changes with dependency-boundary cleanup without separate characterization tests.
- [ ] Do not loosen existing Storefront API contract tests to make migration pass.

## Decision audit trail

| # | Decision | Rationale | Rejected |
| --- | --- | --- | --- |
| 1 | Storefront independence means no source/project dependency on Control Plane, Commerce implementation, backend core, or `Web.SharedV2` | Storefront still needs Commerce API over HTTP, but contract must be through generated client/runtime boundary | Treating API runtime dependency as forbidden |
| 2 | Remove `Storefront.V2 -> Web.SharedV2` before rewriting Control Plane shared helpers | Storefront currently has small shared dependency footprint, while Control Plane uses many shared browser/auth helpers | Big-bang migration of Control Plane and Storefront together |
| 3 | Move `StorefrontCookieNames` to V2-local first | Cookie/session/cart-token/currency preference are host/BFF policy today | Creating a new shared browser package prematurely |
| 4 | Keep `Storefront.WASM`, `Components`, and `Starter` clean with guardrails | These projects are already clean or near-clean; regression prevention matters more than migration there | Waiting until later to add guardrails |
| 5 | Generated `Storefront.Client` remains the only storefront HTTP contract source | Avoids handwritten DTO clones and lets React/other FE read backend contract from generated clients/OpenAPI | Copying DTOs into another shared project |
