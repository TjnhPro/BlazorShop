# BlazorShop.Web.SharedV2 Todo

## Goal

Create a new `BlazorShop.Web.SharedV2` project under `BlazorShop.PresentationV2` so V2 projects no longer depend on the legacy `BlazorShop.Presentation/BlazorShop.Web.Shared` boundary.

This is a boundary isolation task, not a behavior rewrite.

## Current Problem

`BlazorShop.Web.Shared` is physically inside `BlazorShop.Presentation`, so any V2 reference to it keeps V2 coupled to the legacy presentation boundary.

The project also mixes two kinds of code:

- Generic reusable web client infrastructure:
  - browser/session storage
  - cookie storage
  - token service
  - auth state/session helpers
  - HTTP client helpers
  - toast model/service
  - basic response/query models
- Legacy commerce client surface:
  - legacy route constants
  - product/category/cart/payment/SEO/admin service clients
  - admin DTOs and older Web UI support models

V2 should keep only the generic and explicitly needed DTO pieces.

## Current References

```text
BlazorShop.PresentationV2
  ControlPlane.Web
    -> BlazorShop.Presentation/BlazorShop.Web.Shared
       Uses: BrowserStorage, CookieStorage, Authentication, Helper, Toast,
             ServiceResponse, QueryResult, LoginResponse, auth models

  Storefront.V2
    -> BlazorShop.Presentation/BlazorShop.Web.Shared
       Uses: catalog/product/category/discovery/seo/payment DTOs,
             PagedResult, LoginResponse, Constant.Cart.Name,
             Constant.Administration.AdminRole

Legacy projects
  BlazorShop.Web
  BlazorShop.Storefront
    -> keep using BlazorShop.Web.Shared
```

## Target Boundary

```text
BlazorShop.Presentation
  BlazorShop.Web.Shared        legacy shared, unchanged
  BlazorShop.Web               legacy
  BlazorShop.Storefront        legacy

BlazorShop.PresentationV2
  BlazorShop.Web.SharedV2      V2 shared only
  BlazorShop.ControlPlane.Web  -> SharedV2
  BlazorShop.Storefront.V2     -> SharedV2
  BlazorShop.ControlPlane.API  -> no SharedV2 reference
  BlazorShop.CommerceNode.API  -> no SharedV2 reference
```

## Rules

- Do not modify legacy `BlazorShop.Web.Shared` behavior.
- Do not remove legacy `BlazorShop.Web.Shared` from legacy projects.
- Do not move code from legacy in place; copy/extract into V2 first.
- Do not introduce ABP/module-style structure.
- Do not rewrite Storefront V2 or ControlPlane Web logic.
- Do not migrate Application/Infrastructure/Domain to `SharedV2`.
- Do not put CommerceNode API business services into `SharedV2`.
- Use namespace `BlazorShop.Web.SharedV2`.
- Keep each phase independently buildable.
- Commit each phase separately during implementation.

## Approach Review

### Approach A - Keep current `Web.Shared`

Summary: Keep using legacy shared and rely on architecture tests allowlisting it.

Effort: S
Risk: Medium

Pros:
- No code movement.
- Lowest immediate implementation cost.
- Current V2 already builds and runs.

Cons:
- V2 remains coupled to `BlazorShop.Presentation`.
- Future cleanup is harder because legacy and V2 shared models drift together.
- Boundary tests must keep allowing a legacy exception.

### Approach B - Create `Web.SharedV2` with minimal allowlist

Summary: Create a V2 shared project and copy only code used by ControlPlane Web and Storefront V2.

Effort: M
Risk: Medium

Pros:
- Clean V2 boundary.
- Keeps legacy untouched.
- Avoids pulling legacy commerce service clients into V2.
- Allows future V2 API response patterns and constants to evolve independently.

Cons:
- Requires namespace migration across V2 projects and tests.
- Short-term duplication is intentional.
- DTO selection must be careful to avoid missing transitive dependencies.

### Approach C - Move all shared DTOs into Application

Summary: Promote DTOs/models into `BlazorShop.Application` and keep only browser helpers in a small UI shared project.

Effort: L
Risk: High

Pros:
- Long-term layering could be cleaner for API contracts.
- Avoids having server/client DTOs in presentation projects.

Cons:
- High blast radius across legacy and V2.
- Blurs current migration goal.
- More likely to break legacy admin/storefront code.

Recommendation: Approach B.

Reason: It gives the requested V2 isolation while keeping the change mechanical and reversible. Approach C may be better later, but it is too broad for this phase.

## Scope

In scope:

- Create `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2`.
- Copy only required generic helpers and DTOs.
- Rename namespaces from `BlazorShop.Web.Shared` to `BlazorShop.Web.SharedV2`.
- Split broad legacy constants into focused V2 constants.
- Update `ControlPlane.Web` to reference `SharedV2`.
- Update `Storefront.V2` to reference `SharedV2`.
- Update Storefront V2 Dockerfile copy steps.
- Update tests and architecture boundary tests.
- Add guard tests to prevent V2 projects from referencing legacy presentation shared.

Out of scope:

- Removing legacy `BlazorShop.Web.Shared`.
- Rewriting Storefront V2 pages.
- Rewriting ControlPlane Web auth flow.
- Changing API endpoints or response envelopes.
- Moving DTOs into Application.
- Refactoring CommerceNode API services.
- Changing database schema.
- Changing runtime behavior.

## SharedV2 Allowlist

### Keep

| Area | Files/Types | Reason |
|---|---|---|
| Browser storage | `BrowserSessionStorageService`, `IBrowserSessionStorageService` | ControlPlane Web auth/token persistence |
| Cookie storage | `BrowserCookieStorageService`, `IBrowserCookieStorageService` | ControlPlane Web browser credential/session helpers |
| Token helper | `TokenService`, `ITokenService` | ControlPlane Web login/logout |
| HTTP helper | `HttpClientHelper`, `IHttpClientHelper` | ControlPlane API clients use public/private named clients |
| API helper | `ApiCallHelper`, `IApiCallHelper`, `ApiCall` | Keep only if current ControlPlane registration still needs it |
| Auth state | `CustomAuthStateProvider`, notifier/session/refresher/bootstrapper/sync handlers | ControlPlane auth reuse |
| Toast | `Toast*`, `ToastService`, `IToastService`, `ToastModel` | ControlPlane UI notifications |
| Response models | `ServiceResponse`, `QueryResult`, `PagedResult`, `LoginResponse`, `ServiceResponseType`, `Unit` | ControlPlane auth and Storefront API client |
| Auth models | `LoginUser`, `CreateUser`, `PasswordChangeModel`, `UpdateProfileModel`, `AuthenticationBase` | ControlPlane auth service interface compatibility |
| Storefront catalog DTOs | `Category`, `Product`, `Discovery`, `Seo`, selected `Payment` DTOs | Storefront V2 page/API contracts |

### Exclude

| Area | Exclude | Reason |
|---|---|---|
| Legacy commerce service clients | `CategoryService`, `ProductService`, `CartService`, `PaymentMethodService`, `Seo*Service`, `NewsletterService` | V2 should call its own clients, not legacy routes |
| Admin service clients | `Admin*Service`, `IAdmin*Service` | Admin commerce migration belongs in CommerceNode/Application, not web shared |
| File upload client | `FileUploadService` | Not used by current V2 scope |
| Metrics client | `MetricsClient` | Not used by current V2 scope |
| Legacy route constants | `Constant.Product`, `Constant.Category`, `Constant.Seo`, `Constant.Cart` routes, `Constant.Admin*` | Coupled to old endpoints |
| Legacy admin DTOs | `Models/Admin/*` | Not used by V2 web projects now |
| Analytics/newsletter/notification DTOs | `Models/Analytics`, `Models/Newsletter`, `Models/Notifications` | Keep out unless a V2 consumer is proven |

## Constants Plan

Replace broad `Constant.cs` with focused V2 constants:

```text
BlazorShop.Web.SharedV2
  AuthStorageConstants
    JwtTokenKey = "token"

  HttpClientNames
    Public = "Blazor-Client-Public"
    Private = "Blazor-Client-Private"

  StorefrontCookieNames
    Cart = "my-cart"

  RoleNames
    Admin = "Admin"
```

Migration mapping:

| Old | New |
|---|---|
| `Constant.TokenStorage.Key` | `AuthStorageConstants.JwtTokenKey` |
| `Constant.ApiClient.PublicName` | `HttpClientNames.Public` |
| `Constant.ApiClient.PrivateName` | `HttpClientNames.Private` |
| `Constant.Cart.Name` | `StorefrontCookieNames.Cart` |
| `Constant.Administration.AdminRole` | `RoleNames.Admin` |

## Target Project File

`BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/BlazorShop.Web.SharedV2.csproj`

Expected package references:

```xml
<PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="10.0.6" />
<PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="10.0.6" />
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.6" />
<PackageReference Include="Microsoft.Extensions.Http" Version="10.0.6" />
<PackageReference Include="Microsoft.JSInterop" Version="10.0.6" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.15.0" />
```

Keep versions aligned with existing `BlazorShop.Web.Shared` unless the solution already centralizes package versions later.

## Phase 0 - Inventory And Safety Baseline

- [x] Confirm V2 usages of `BlazorShop.Web.Shared`.
- [x] Confirm no Domain/Application/Infrastructure/CommerceNode API references `BlazorShop.Web.Shared`.
- [x] Capture current list of allowed source files.
- [x] Run baseline builds:
  - [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj`
    - 2026-07-09: passed.
  - [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj`
    - 2026-07-09: passed.
  - [x] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter FullyQualifiedName~PresentationV2`
    - 2026-07-09: passed 15/15.
- [x] Commit only if baseline docs/tests change.

Exit criteria:

- Current dependency graph is documented.
- Implementation file allowlist is explicit.

## Phase 1 - Scaffold SharedV2 Project

- [ ] Create `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2`.
- [ ] Add `BlazorShop.Web.SharedV2.csproj`.
- [ ] Add project to `BlazorShop.sln`.
- [ ] Create namespace root `BlazorShop.Web.SharedV2`.
- [ ] Copy generic infrastructure folders:
  - [ ] `BrowserStorage`
  - [ ] `CookieStorage`
  - [ ] `Helper`
  - [ ] `Interop` only if needed by copied auth/session code
  - [ ] `Authentication`
  - [ ] `Toast`
- [ ] Rename namespaces and using statements inside copied files.
- [ ] Replace copied `Constant.cs` with focused V2 constants.
- [ ] Build only `SharedV2`.

Verification:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/BlazorShop.Web.SharedV2.csproj
```

Commit:

```text
feat(shared-v2): scaffold web shared v2
```

## Phase 2 - Copy Required Shared Models

- [ ] Copy basic models:
  - [ ] `ApiCall`
  - [ ] `ServiceResponse`
  - [ ] `ServiceResponseType`
  - [ ] `QueryResult`
  - [ ] `PagedResult`
  - [ ] `LoginResponse`
  - [ ] `ToastModel`
  - [ ] `Unit`
- [ ] Copy auth models:
  - [ ] `AuthenticationBase`
  - [ ] `CreateUser`
  - [ ] `LoginUser`
  - [ ] `PasswordChangeModel`
  - [ ] `UpdateProfileModel`
- [ ] Copy Storefront DTO folders used by V2:
  - [ ] `Models/Category`
  - [ ] `Models/Product`
  - [ ] `Models/Discovery`
  - [ ] `Models/Seo`
  - [ ] selected `Models/Payment`: `ProcessCart`, `GetOrder`, `GetOrderItem`, `GetOrderLine`, `GetPaymentMethod`, `Checkout`, `CreateOrderItem` only if referenced
- [ ] Rename namespaces to `BlazorShop.Web.SharedV2.*`.
- [ ] Build `SharedV2`.

Verification:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/BlazorShop.Web.SharedV2.csproj
```

Commit:

```text
feat(shared-v2): copy v2 shared models
```

## Phase 3 - Migrate ControlPlane Web

- [ ] Change project reference in `BlazorShop.ControlPlane.Web.csproj` from legacy shared to `SharedV2`.
- [ ] Update namespaces:
  - [ ] `BlazorShop.Web.Shared` -> `BlazorShop.Web.SharedV2`
  - [ ] `BlazorShop.Web.Shared.Authentication` -> `BlazorShop.Web.SharedV2.Authentication`
  - [ ] `BlazorShop.Web.Shared.Helper` -> `BlazorShop.Web.SharedV2.Helper`
  - [ ] `BlazorShop.Web.Shared.Services.Contracts` -> `BlazorShop.Web.SharedV2.Services.Contracts`
- [ ] Replace constants:
  - [ ] `Constant.ApiClient.PublicName` -> `HttpClientNames.Public`
  - [ ] `Constant.ApiClient.PrivateName` -> `HttpClientNames.Private`
  - [ ] `Constant.TokenStorage.Key` -> `AuthStorageConstants.JwtTokenKey`
- [ ] Confirm `ControlPlaneAuthenticationService` compiles against `SharedV2` response/auth models.
- [ ] Build ControlPlane Web.

Verification:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter FullyQualifiedName~ControlPlane
```

Commit:

```text
refactor(control-plane): use web shared v2
```

## Phase 4 - Migrate Storefront V2

- [ ] Change project reference in `BlazorShop.Storefront.V2.csproj` from legacy shared to `SharedV2`.
- [ ] Update `_Imports.razor` shared namespaces.
- [ ] Update Storefront V2 service and page namespaces:
  - [ ] `Models`
  - [ ] `Models.Category`
  - [ ] `Models.Product`
  - [ ] `Models.Discovery`
  - [ ] `Models.Seo`
  - [ ] `Models.Payment`
- [ ] Replace constants:
  - [ ] `Constant.Cart.Name` -> `StorefrontCookieNames.Cart`
  - [ ] `Constant.Administration.AdminRole` -> `RoleNames.Admin`
- [ ] Update Storefront V2 Dockerfile:
  - [ ] copy `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/*.csproj`
  - [ ] copy `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/`
  - [ ] remove legacy shared copy if no longer needed
- [ ] Build Storefront V2.

Verification:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter FullyQualifiedName~Storefront
```

Commit:

```text
refactor(storefront-v2): use web shared v2
```

## Phase 5 - Migrate Tests And Boundary Guards

- [ ] Update test references/usings that target V2 to use `SharedV2`.
- [ ] Keep legacy tests that target legacy Storefront/Web on `BlazorShop.Web.Shared`.
- [ ] Update `BlazorShop.Tests.csproj` to reference `SharedV2` if V2 tests require direct model access.
- [ ] Update `ControlPlaneArchitectureBoundaryTests`:
  - [ ] V2 projects must not reference any `BlazorShop.Presentation/*` project.
  - [ ] V2 projects may reference `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2`.
  - [ ] ControlPlane Web allowed namespaces become `BlazorShop.Web.SharedV2.*`.
- [ ] Add Storefront V2 boundary test:
  - [ ] `StorefrontV2_DoesNotReferenceLegacyPresentationShared`
  - [ ] `StorefrontV2_UsesOnlySharedV2Namespaces`
- [ ] Add guard against copying excluded legacy services into `SharedV2`.

Suggested guard checks:

```text
SharedV2 must not contain:
- Services/Admin*
- Services/CategoryService.cs
- Services/ProductService.cs
- Services/CartService.cs
- Services/PaymentMethodService.cs
- Services/SeoRedirectService.cs
- Services/SeoSettingsService.cs
- Services/NewsletterService.cs
- Models/Admin/*
- Models/Analytics/*
- Models/Newsletter/*
- Models/Notifications/*
```

Verification:

```powershell
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter FullyQualifiedName~PresentationV2
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter FullyQualifiedName~Storefront
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter FullyQualifiedName~ControlPlane
```

Commit:

```text
test(shared-v2): enforce v2 shared boundary
```

## Phase 6 - Documentation And Cutover Notes

- [ ] Update `legacy-cutover-readiness.md`:
  - [ ] remove exception that allows V2 to reference legacy `BlazorShop.Web.Shared`
  - [ ] add requirement that V2 references `BlazorShop.Web.SharedV2`
- [ ] Update `BlazorShop.StorefrontV2.Reuse.todo.md` note:
  - [ ] prior choice "Keep Web.Shared" is superseded by `SharedV2`
  - [ ] explain this is copy/reuse isolation, not rewrite
- [ ] Update ControlPlane docs where they mention `BlazorShop.Web.Shared` helper reuse.
- [ ] Add final checklist to this file.

Verification:

```powershell
rg -n "BlazorShop.Web.Shared" BlazorShop.PresentationV2 BlazorShop.Tests/PresentationV2 docs/refactor-control-Commerce-storefront
```

Expected after phase:

- Any remaining doc references to legacy shared must be historical or explicitly marked superseded.
- No V2 source code should reference `BlazorShop.Web.Shared`.

Commit:

```text
docs(shared-v2): document v2 shared cutover
```

## Phase 7 - Full Verification

- [ ] Build solution.
- [ ] Run full test suite.
- [ ] Run Storefront V2 smoke if needed.
- [ ] Run ControlPlane Web smoke if needed.
- [ ] Confirm legacy projects still build with legacy `Web.Shared`.
- [ ] Confirm V2 projects build without legacy `Web.Shared`.

Commands:

```powershell
dotnet build BlazorShop.sln
dotnet test BlazorShop.sln
dotnet build BlazorShop.Presentation/BlazorShop.Web/BlazorShop.Web.csproj
dotnet build BlazorShop.Presentation/BlazorShop.Storefront/BlazorShop.Storefront.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj
```

Optional browser QA:

```powershell
docker compose -f compose.commercenode.yml up -d
dotnet ef database update --project BlazorShop.Infrastructure/BlazorShop.Infrastructure.csproj --startup-project BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --context CommerceNodeDbContext
```

Then run Storefront V2 and ControlPlane Web smoke checks if this phase touches runtime behavior.

Commit:

```text
chore(shared-v2): verify v2 shared isolation
```

## Risk Register

| Risk | Impact | Mitigation |
|---|---|---|
| Missing transitive DTO dependency | Build failure | Migrate by compiler, one V2 project at a time |
| Namespace mass replace touches legacy | Legacy breakage | Limit replacement to `BlazorShop.PresentationV2` and selected tests only |
| Constants copied with legacy route strings | Hidden legacy coupling remains | Replace `Constant.cs` with focused constants |
| Tests accidentally migrate legacy scenarios to SharedV2 | False confidence | Keep legacy tests using legacy namespaces unless test target is V2 |
| Dockerfile misses SharedV2 copy | Container build failure | Update Dockerfile in same phase as Storefront V2 reference |
| SharedV2 becomes another dumping ground | Future drift | Add boundary tests excluding legacy service clients and admin DTO folders |

## Failure Modes Registry

| Failure Mode | Detection | Recovery |
|---|---|---|
| ControlPlane Web auth breaks | Login/logout QA and ControlPlane auth tests | Compare copied auth/session helpers against legacy source |
| Storefront V2 cart cookie breaks | QA-StorefrontV2 cart checklist | Verify `StorefrontCookieNames.Cart == "my-cart"` |
| Storefront V2 role parsing changes | Checkout/session tests | Verify `RoleNames.Admin == "Admin"` |
| V2 still references legacy shared | Architecture tests and `rg` check | Update project references/usings |
| Legacy stops building | Legacy project build commands | Revert accidental legacy changes; legacy shared should remain unchanged |

## Test Plan

### Unit / Architecture

- [ ] `ControlPlaneArchitectureBoundaryTests` updated.
- [ ] New Storefront V2 boundary tests added.
- [ ] SharedV2 exclusion test added for forbidden folders/classes.

### Build

- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/BlazorShop.Web.SharedV2.csproj`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj`
- [ ] `dotnet build BlazorShop.sln`

### Regression

- [ ] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter FullyQualifiedName~PresentationV2`
- [ ] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter FullyQualifiedName~ControlPlane`
- [ ] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter FullyQualifiedName~Storefront`
- [ ] `dotnet test BlazorShop.sln`

### Runtime Smoke

- [ ] Storefront V2 home/category/product still load.
- [ ] Storefront V2 cart cookie still uses `my-cart`.
- [ ] Storefront V2 checkout handoff still works.
- [ ] ControlPlane Web login/logout still works.
- [ ] ControlPlane Web private API calls still attach bearer token.
- [ ] No V2 runtime requires legacy `BlazorShop.Web.Shared` assembly.

## QA Checklist Updates

- [ ] Add SharedV2 dependency isolation checks to `QA-StorefrontV2.todo.md`.
- [ ] Add ControlPlane Web login/logout regression checks to `QA-ControlPlane.todo.md`.
- [ ] Add note that any V2 change touching shared models/helpers must rerun:
  - [ ] Storefront V2 cart/product/catalog checks
  - [ ] ControlPlane auth/private API checks
  - [ ] architecture boundary tests

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|---|---|---|---|---|---|
| 1 | CEO | Create `SharedV2` instead of keeping legacy shared exception | Auto-decided | Boundary clarity | User goal is full V2 isolation from legacy presentation | Keep current `Web.Shared` |
| 2 | Eng | Use copy/extract allowlist, not move-in-place | Auto-decided | Lowest blast radius | Legacy projects must remain untouched | Move existing project |
| 3 | Eng | Exclude legacy commerce service clients from SharedV2 | Auto-decided | Layered architecture | V2 API clients already live in V2 projects; shared should not own legacy endpoints | Copy whole project |
| 4 | Eng | Split broad constants into focused V2 constants | Auto-decided | Explicit over clever | Avoid silently importing old routes | Copy `Constant.cs` as-is |
| 5 | DX | Add architecture tests as guardrails | Auto-decided | Fast feedback | Future changes should fail at test time if V2 references legacy shared again | Rely on manual review |

## Review Scores

| Review | Score | Notes |
|---|---:|---|
| CEO | 8/10 | Strong alignment with V2 isolation. Main risk is scope creep if DTO migration expands beyond current V2 needs. |
| Design | N/A | No new UI surface. Existing UI should be behavior-preserving. |
| Engineering | 8/10 | Good strangler-style migration. Needs strict allowlist and boundary tests to avoid accidental full copy. |
| DX | 7/10 | Improves developer clarity by removing legacy exception. Needs clear docs and commands so future V2 work knows where shared code belongs. |

## Cross-Phase Themes

- Boundary clarity matters more than deduplication in this phase.
- Copy/reuse is acceptable because the old shared project is legacy-owned.
- Tests must enforce the architecture decision; docs alone are not enough.
- The broad `Constant.cs` is the main hidden coupling risk.

## Implementation Tasks

- [ ] TASK-001: Create `BlazorShop.Web.SharedV2` project.
- [ ] TASK-002: Copy and namespace generic browser/auth/helper/toast infrastructure.
- [ ] TASK-003: Copy and namespace required DTO models.
- [ ] TASK-004: Replace `Constant.cs` with focused V2 constants.
- [ ] TASK-005: Migrate `ControlPlane.Web` to `SharedV2`.
- [ ] TASK-006: Migrate `Storefront.V2` to `SharedV2`.
- [ ] TASK-007: Update Storefront V2 Dockerfile.
- [ ] TASK-008: Add/update architecture boundary tests.
- [ ] TASK-009: Update documentation and QA checklists.
- [ ] TASK-010: Run full build/test/runtime smoke.

## Final Acceptance Criteria

- [ ] `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2` exists and builds.
- [ ] `BlazorShop.ControlPlane.Web` references `SharedV2`, not legacy `Web.Shared`.
- [ ] `BlazorShop.Storefront.V2` references `SharedV2`, not legacy `Web.Shared`.
- [ ] `BlazorShop.ControlPlane.API` does not reference `SharedV2`.
- [ ] `BlazorShop.CommerceNode.API` does not reference `SharedV2`.
- [ ] Legacy `BlazorShop.Web` and legacy `BlazorShop.Storefront` still reference legacy `Web.Shared`.
- [ ] No source file under `BlazorShop.PresentationV2` contains `BlazorShop.Web.Shared`.
- [ ] No V2 project references any project under `BlazorShop.Presentation`.
- [ ] Full solution build passes.
- [ ] Full solution tests pass.

## GSTACK REVIEW REPORT

| Run | Status | Findings |
|---|---|---|
| CEO | Passed with scope control | SharedV2 is the right direction for V2 isolation; do not expand into Application DTO migration now. |
| Design | Skipped | No new UI design surface. |
| Engineering | Passed with guardrails | Use allowlist copy/extract, focused constants, and boundary tests. |
| DX | Passed with docs requirement | Update docs and QA checklists so future V2 contributors do not reuse legacy shared. |

VERDICT: APPROVED PLAN - implement in phases, commit each phase, and keep legacy `BlazorShop.Web.Shared` untouched.

NO UNRESOLVED DECISIONS
