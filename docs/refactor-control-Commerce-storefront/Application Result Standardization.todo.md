# Application Result Standardization

Status: proposed
Date: 2026-07-19
Purpose: thong nhat application operation result de giam duplicate `Success/Message/Payload/Failure`, nhung van giu on dinh HTTP response envelope va khong pha active V2 API contract.

## Codebase Baseline

- `BlazorShop.Application` hien co nhieu operation result/failure gan giong nhau:
  - `ControlPlaneUserOperationResult<TPayload>` trong `BlazorShop.Application/ControlPlane/Users/ControlPlaneUserDtos.cs`.
  - `ControlPlaneStoreOperationResult<TPayload>` trong `BlazorShop.Application/ControlPlane/Stores/ControlPlaneStoreDtos.cs`.
  - `ControlPlaneNodeOperationResult<TPayload>` trong `BlazorShop.Application/ControlPlane/Nodes/ControlPlaneNodeDtos.cs`.
  - `ControlPlaneHealthOperationResult<TPayload>` trong `BlazorShop.Application/ControlPlane/Health/ControlPlaneHealthDtos.cs`.
  - `ControlPlaneCredentialOperationResult<TPayload>` trong `BlazorShop.Application/ControlPlane/Credentials/ControlPlaneCredentialDtos.cs`.
  - `ControlPlaneActionOperationResult<TPayload>` trong `BlazorShop.Application/ControlPlane/Actions/ControlPlaneActionDtos.cs`.
  - `ControlPlaneStoreDeploymentOperationResult<TPayload>` trong `BlazorShop.Application/ControlPlane/Stores/ControlPlaneStoreDeploymentDtos.cs`.
  - `CommerceStoreOperationResult<TPayload>` trong `BlazorShop.Application/CommerceNode/Stores/CommerceStoreDtos.cs`.
  - `CommerceTaskOperationResult<TPayload>` trong `BlazorShop.Application/CommerceNode/Tasks/CommerceTaskDtos.cs`.
  - `ProductMediaOperationResult<TPayload>` trong `BlazorShop.Application/CommerceNode/ProductMedia/ProductMediaDtos.cs`.
  - `CommerceMediaAssetOperationResult<TPayload>` trong `BlazorShop.Application/CommerceNode/Media/CommerceMediaAssetDtos.cs`.
  - `CategoryMediaOperationResult<TPayload>` trong `BlazorShop.Application/CommerceNode/Media/CategoryMediaDtos.cs`.
- Nhieu result co cung shape:
  - `bool Success`.
  - `string? Message`.
  - `TPayload? Payload`.
  - `*OperationFailure? Failure` hoac enum co `None`.
- Mot so result co field rieng:
  - `ControlPlaneActionOperationResult<TPayload>` co `AlreadyExists`.
  - `CommerceTaskOperationResult<TPayload>` co `AlreadyExists`.
  - `ControlPlaneStoreDeploymentOperationFailure` co `RemoteFailure`.
- `ServiceResponse<TPayload>` trong `BlazorShop.Application/DTOs/ServiceResponseOfT.cs` da duoc dung rong rai, co `ResponseType`.
- `ServiceResponse<TPayload>` va `ServiceResponseType` bi duplicate them trong `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/Models`.
- Presentation hien co nhieu mapper lap lai:
  - Control Plane controllers tu switch failure enum sang `StatusCodes`.
  - Commerce Node controllers tu switch failure enum sang `StatusCodes` hoac `ServiceResponseType`.
- Existing public envelopes dang la:
  - `ControlPlaneApiResponse<T>` trong Control Plane API.
  - `CommerceNodeApiResponse<T>` trong Commerce Node API.
  - Storefront local endpoint error DTO rieng cho WASM/local browser flow.

## Problem

Application dang tu nhan ban contract ket qua operation theo tung domain. Moi feature moi co xu huong tao them:

```text
SomeFeatureOperationResult<TPayload>
SomeFeatureOperationFailure
Controller-specific failure-to-HTTP mapper
```

He qua:

- Duplicate DTO va enum tang nhanh.
- HTTP mapping bi copy nhieu noi, de lech behavior.
- Application result dang bi tron voi transport-ish concept nhu `ServiceResponseType`.
- Test phai mock constructor cua nhieu result type, lam refactor ve sau dat hon.
- Error code public khong nhat quan giua Control Plane, Commerce Node admin va Storefront scoped API.

## Target Shape

```text
BlazorShop.Application
  Common/Results/ApplicationResult.cs
  Common/Results/ApplicationError.cs
  Common/Results/ApplicationErrorKind.cs

Application services
  -> return ApplicationResult<TPayload>
  -> use ApplicationError.Kind for semantic failure
  -> use ApplicationError.Code for domain/public-safe machine code
  -> use ApplicationError.Message for safe user/admin message
  -> use ApplicationError.Metadata only for non-secret structured context

PresentationV2
  ControlPlane API mapper
    ApplicationResult<T> -> ControlPlaneApiResponse<T> + HTTP status
  Commerce Node API mapper
    ApplicationResult<T> -> CommerceNodeApiResponse<T> + HTTP status
```

Suggested model:

```csharp
public sealed record ApplicationResult<TValue>(
    bool Success,
    TValue? Value = default,
    ApplicationError? Error = null,
    string? Message = null)
{
    public static ApplicationResult<TValue> Succeeded(TValue value, string? message = null);
    public static ApplicationResult<TValue> Failed(ApplicationError error);
}

public sealed record ApplicationError(
    ApplicationErrorKind Kind,
    string Code,
    string Message,
    IReadOnlyDictionary<string, string>? Metadata = null);

public enum ApplicationErrorKind
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    RemoteFailure,
    Failure
}
```

Rules:

- `ApplicationResult<T>` is application-layer use-case result, not HTTP response envelope.
- Public HTTP JSON shape must remain `ControlPlaneApiResponse<T>` / `CommerceNodeApiResponse<T>` unless a separate API contract change is explicitly approved.
- `ApplicationError.Message` must be public-safe and must not carry exception details, secrets, provider credentials, SMTP password, private key, raw stack trace, internal ids, or node secrets.
- `ApplicationError.Code` should be stable and domain-specific, for example `store.not_found`, `media.validation`, `task.already_exists`, `deployment.remote_failure`.
- `ApplicationErrorKind` maps to HTTP status only in Presentation.
- Failure cases that currently return partial payload may keep `Value` or use `Metadata` only if existing API behavior requires it.

## Autoplan Decisions

| Decision | Chon | Ly do |
| --- | --- | --- |
| Scope | Lam sau file-splitting/characterization tests | Result contract cham nhieu service, controller va tests. |
| Core model owner | `BlazorShop.Application/Common/Results` | Application layer dang own DTO/service contracts, khong phu thuoc Presentation. |
| HTTP envelope | Giu nguyen | Tranh breaking API cho Control Plane Web, Storefront V2 va generated clients. |
| Migration style | Adapter-first, migrate theo capability | Giam blast radius va co the verify tung cum. |
| First pilot | Commerce Node media results | Shape don gian, it policy dac biet hon checkout/payment/task. |
| `ServiceResponse<T>` | Chua xoa ngay | Dang duoc dung rong trong legacy/shared va V2; can adapter de migrate dan. |
| Domain error detail | `Code` + optional `Metadata` | Thay the enum rieng ma khong mat semantic nhu `AlreadyExists`, `RemoteFailure`. |
| Presentation mapping | Dung helper chung theo boundary | Van giu Control Plane va Commerce Node response writer rieng. |

## Phase 0 - Inventory And Characterization

Goal: khoa behavior hien tai truoc khi refactor.

Tasks:

- [x] Inventory all active V2 operation result/failure definitions in `BlazorShop.Application`.
- [x] Mark each result type as:
  - [x] Simple shape: `Success/Message/Payload/Failure`.
  - [x] Extended shape: `AlreadyExists`, partial payload, remote failure, special error code.
  - [x] Non-operation result: calculation/validation object that should not migrate.
- [x] Inventory all Presentation V2 failure mappers:
  - [x] Control Plane controllers.
  - [x] Commerce Node admin/control controllers.
  - [x] Storefront scoped controllers.
  - [x] Storefront local endpoints.
- [x] Add or update characterization tests for current HTTP behavior:
  - [x] Validation maps to 400.
  - [x] Not found maps to 404.
  - [x] Conflict maps to 409.
  - [x] Remote failure maps to 502 where currently supported.
  - [x] Unknown/failure maps to 500 for Commerce Node admin and 400 where Control Plane currently does so.
  - [x] Response envelope remains unchanged.
  - [x] Message fallback remains unchanged.
- [x] Add snapshots or assertions for representative endpoints before replacing result types.

Exit criteria:

- [x] A test fails if a controller changes status code mapping unexpectedly.
- [x] A test fails if public response envelope shape changes unexpectedly.
- [x] Inventory clearly separates safe-to-migrate types from deferred types.

Phase 0 inventory:

- Simple operation result/failure shapes:
  - `ControlPlaneUserOperationResult<TPayload>` / `ControlPlaneUserOperationFailure`.
  - `ControlPlaneStoreOperationResult<TPayload>` / `ControlPlaneStoreOperationFailure`.
  - `ControlPlaneNodeOperationResult<TPayload>` / `ControlPlaneNodeOperationFailure`.
  - `ControlPlaneHealthOperationResult<TPayload>` / `ControlPlaneHealthOperationFailure`.
  - `ControlPlaneCredentialOperationResult<TPayload>` / `ControlPlaneCredentialOperationFailure`.
  - `CommerceStoreOperationResult<TPayload>` / `CommerceStoreOperationFailure`.
  - `ProductMediaOperationResult<TPayload>` / `ProductMediaOperationFailure`.
  - `CommerceMediaAssetOperationResult<TPayload>` / `CommerceMediaAssetOperationFailure`.
  - `CategoryMediaOperationResult<TPayload>` / `CategoryMediaOperationFailure`.
- Extended operation results:
  - `ControlPlaneActionOperationResult<TPayload>` has `AlreadyExists`.
  - `CommerceTaskOperationResult<TPayload>` has `AlreadyExists`.
  - `ControlPlaneStoreDeploymentOperationResult<TPayload>` supports `RemoteFailure` and failure payload.
- Deferred non-migration result:
  - `PaymentProviderOperationResult` is operation payload/state recommendation for provider workflows, not a generic application use-case result.
- Presentation mapper inventory:
  - Control Plane controllers use local enum switch methods and `ControlPlaneApiResponseWriter`.
  - Control Plane commerce gateway base maps remote catalog failures to `404`, `502`, `400`.
  - Commerce Node admin controllers use local enum switch methods and `CommerceNodeApiResponse<T>`.
  - Storefront scoped controllers use `StorefrontApiControllerBase.FromServiceResponse` for `ServiceResponse<T>` and local store result switches for current store/configuration.
  - Storefront local endpoints use their own local browser-flow response/error DTOs and are deferred unless visible behavior changes.
- Characterization tests added in `ApplicationResultStandardizationPhase0Tests`:
  - Inventory source scan locks expected operation result names.
  - `CommerceStoresController` mapper locks validation/not found/conflict status and `CommerceNodeApiResponse<T>` envelope.
  - `ControlPlaneStoresController` deployment mapper locks validation/not found/conflict/remote failure status and `ControlPlaneApiResponse<T>` envelope.
  - Unknown failure fallback is locked: Commerce Node admin maps to `500`; Control Plane deployment maps to `400`.
  - `ControlPlaneApiResponseWriter` locks failure envelope fallback message.
- Focused command passed 11/11 tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~ApplicationResultStandardizationPhase0Tests" --no-restore --nologo --verbosity minimal`

## Phase 1 - Add Core Application Result Model

Goal: them model chung ma chua doi service signatures.

Tasks:

- [ ] Add `BlazorShop.Application/Common/Results/ApplicationResult.cs`.
- [ ] Add `ApplicationResult<TValue>` with static factory helpers:
  - [ ] `Succeeded(value, message)`.
  - [ ] `Failed(error)`.
- [ ] Add non-generic `ApplicationResult` only if command-without-payload services need it.
- [ ] Add `ApplicationError`.
- [ ] Add `ApplicationErrorKind`.
- [ ] Add common factory helpers for known errors:
  - [ ] `ApplicationError.Validation(code, message)`.
  - [ ] `ApplicationError.NotFound(code, message)`.
  - [ ] `ApplicationError.Conflict(code, message)`.
  - [ ] `ApplicationError.RemoteFailure(code, message)`.
  - [ ] `ApplicationError.Failure(code, message)`.
- [ ] Add unit tests for factories and invariants:
  - [ ] Success must not require error.
  - [ ] Failure must carry non-empty code and safe message.
  - [ ] Metadata is optional and read-only.

Exit criteria:

- [ ] New model builds without changing existing service/controller behavior.
- [ ] No public API schema changes.

## Phase 2 - Add Boundary Mappers Without Changing Services

Goal: Presentation co helper chung cho `ApplicationResult<T>`, nhung chua migrate domain services.

Tasks:

- [ ] Add Control Plane mapper close to existing response writer:
  - [ ] Keep `ControlPlaneApiResponseWriter`.
  - [ ] Add `ApplicationResult` extension/helper that returns `ObjectResult`.
  - [ ] Map `ApplicationErrorKind` to current Control Plane status behavior.
- [ ] Add Commerce Node mapper close to existing response writer/controller base:
  - [ ] Keep `CommerceNodeApiResponseWriter`.
  - [ ] Add `ApplicationResult` extension/helper for MVC controllers.
  - [ ] Add minimal endpoint helper only if Storefront scoped minimal endpoints need it later.
- [ ] Keep `ControlPlaneApiResponse<T>` and `CommerceNodeApiResponse<T>` response JSON unchanged.
- [ ] Add mapper tests:
  - [ ] `Validation -> 400`.
  - [ ] `NotFound -> 404`.
  - [ ] `Conflict -> 409`.
  - [ ] `Forbidden -> 403`.
  - [ ] `Unauthorized -> 401`.
  - [ ] `RemoteFailure -> 502`.
  - [ ] `Failure -> 500` for Commerce Node.

Exit criteria:

- [ ] New mapper can be used by migrated controllers.
- [ ] Existing controllers still compile without migration.
- [ ] Contract tests still pass.

## Phase 3 - Pilot Migration: Commerce Node Media

Goal: migrate mot cum it rui ro de chung minh model dung voi codebase.

Suggested order:

- [ ] `CommerceMediaAssetOperationResult<TPayload>`.
- [ ] `CategoryMediaOperationResult<TPayload>`.
- [ ] `ProductMediaOperationResult<TPayload>`.

Tasks:

- [ ] Update service contracts:
  - [ ] `ICommerceMediaAssetService`.
  - [ ] `ICategoryMediaService`.
  - [ ] `IProductMediaService`.
- [ ] Update implementations:
  - [ ] `CommerceMediaAssetService`.
  - [ ] `CategoryMediaService`.
  - [ ] `ProductMediaService`.
- [ ] Replace controller-specific failure enum mapping with shared `ApplicationResult` mapper.
- [ ] Convert failure codes:
  - [ ] `media.validation`.
  - [ ] `media.not_found`.
  - [ ] `media.conflict`.
  - [ ] `product_media.validation`.
  - [ ] `category_media.not_found`.
- [ ] Remove old media operation result/failure definitions only after all references are gone.
- [ ] Update tests that construct old media result types.

Exit criteria:

- [ ] Media service tests pass.
- [ ] Media controller tests prove status codes and envelopes unchanged.
- [ ] `rg` shows old media operation result types no longer referenced.

## Phase 4 - Migrate Commerce Store And Task Results

Goal: migrate Commerce Node store/task results, including special behavior.

Tasks:

- [ ] Migrate `CommerceStoreOperationResult<TPayload>`:
  - [ ] Store validation -> `ApplicationErrorKind.Validation`.
  - [ ] Store not found -> `ApplicationErrorKind.NotFound`.
  - [ ] Store conflict -> `ApplicationErrorKind.Conflict`.
  - [ ] Storefront store readiness/public configuration controllers keep existing error code behavior.
- [ ] Migrate `CommerceTaskOperationResult<TPayload>`:
  - [ ] Replace `AlreadyExists` with `ApplicationError.Code = "task.already_exists"` or metadata if the API currently exposes this state.
  - [ ] Keep task response envelope unchanged.
  - [ ] Preserve enqueue idempotency behavior.
- [ ] Update controllers:
  - [ ] `CommerceStoresController`.
  - [ ] `CommerceTasksController`.
  - [ ] Storefront scoped store/configuration controllers if they consume store result.
- [ ] Update tests and fake current-store resolvers.

Exit criteria:

- [ ] Store lifecycle/store readiness behavior unchanged.
- [ ] Task enqueue/cancel/retry behavior unchanged.
- [ ] `AlreadyExists` semantics are still test-covered.

## Phase 5 - Migrate Control Plane Operation Results

Goal: migrate Control Plane result types sau khi mapper va pilot da on dinh.

Suggested order:

- [ ] Nodes.
- [ ] Credentials.
- [ ] Health.
- [ ] Actions.
- [ ] Stores.
- [ ] Users.
- [ ] Store deployment.

Tasks:

- [ ] Update service contracts:
  - [ ] `IControlPlaneNodeService`.
  - [ ] `IControlPlaneCredentialService`.
  - [ ] `IControlPlaneHealthService`.
  - [ ] `IControlPlaneActionService`.
  - [ ] `IControlPlaneStoreService`.
  - [ ] `IControlPlaneUserManagementService`.
  - [ ] `IControlPlaneStoreDeploymentService`.
- [ ] Update implementations under `BlazorShop.Infrastructure/Data/ControlPlane`.
- [ ] Replace controller-local failure switches with Control Plane mapper.
- [ ] Preserve Control Plane audit behavior:
  - [ ] Audit still records success/failure.
  - [ ] Audit actor lookup unchanged.
  - [ ] Audit message does not leak internal exception details.
- [ ] Convert `RemoteFailure` in deployment to `ApplicationErrorKind.RemoteFailure`.
- [ ] Convert action `AlreadyExists` to stable code or metadata.

Exit criteria:

- [ ] Control Plane controller tests cover status and envelope.
- [ ] Control Plane Web behavior remains unchanged.
- [ ] Old Control Plane operation result/failure definitions have no references.

## Phase 6 - Add ServiceResponse Adapter, Then Decide Migration Scope

Goal: xu ly `ServiceResponse<T>` theo huong an toan, khong xoa dot ngot.

Tasks:

- [ ] Add extension helpers:
  - [ ] `ServiceResponse<T>.ToApplicationResult(defaultCode)`.
  - [ ] `ApplicationResult<T>.ToServiceResponse()` only if needed for transitional compatibility.
- [ ] Define `ServiceResponseType -> ApplicationErrorKind` mapping:
  - [ ] `ValidationError -> Validation`.
  - [ ] `NotFound -> NotFound`.
  - [ ] `Conflict -> Conflict`.
  - [ ] `Failure -> Failure`.
- [ ] Do not migrate checkout/cart/payment in this phase unless a later plan explicitly targets them.
- [ ] Do not remove `ServiceResponse<T>` while legacy/shared tests still depend on it.
- [ ] Identify services where `ServiceResponse<T>` is still appropriate because they are HTTP-client/service-client projection, not application use-case result.

Exit criteria:

- [ ] Adapters exist for future migrations.
- [ ] No behavior change in checkout/cart/payment.
- [ ] A follow-up inventory shows remaining `ServiceResponse<T>` use by category.

## Phase 7 - Web Shared V2 Client Result Cleanup

Goal: giam duplicate phia client ma khong tron voi Application core.

Tasks:

- [ ] Review `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/Models/ServiceResponse.cs`.
- [ ] Keep client DTOs separate if they represent HTTP response payload shape.
- [ ] Avoid referencing `BlazorShop.Application` from UI client just to reuse server-side result model if it would pull unwanted dependencies.
- [ ] If duplicate enum is kept, document why it is transport/client projection.
- [ ] If duplicate enum is removed, replace with generated/shared API contract DTO only after contract tests cover clients.

Exit criteria:

- [ ] Client-side result model decision is documented.
- [ ] No Control Plane Web direct dependency on Commerce Node internals.

## Phase 8 - Contract, QA, And Release Gate

Goal: dam bao refactor khong doi hanh vi nguoi dung/API.

Tests:

- [ ] `dotnet test BlazorShop.Tests --filter CommerceMedia`.
- [ ] `dotnet test BlazorShop.Tests --filter ProductMedia`.
- [ ] `dotnet test BlazorShop.Tests --filter CategoryMedia`.
- [ ] `dotnet test BlazorShop.Tests --filter CommerceTask`.
- [ ] `dotnet test BlazorShop.Tests --filter ControlPlane`.
- [ ] OpenAPI/contract tests for changed controllers.
- [ ] Full `dotnet test` before final commit.

QA:

- [ ] Update `docs/refactor-control-Commerce-storefront/QA-ControlPlane.todo.md` if Control Plane behavior changes.
- [ ] Update `docs/refactor-control-Commerce-storefront/QA-CommerceNode.todo.md` if Commerce Node admin/storefront behavior changes.
- [ ] Browser Playwright is not required for pure application result refactor unless visible UI behavior or local Storefront endpoints change.

Release gate:

- [ ] No response envelope changes.
- [ ] No dropped error messages without replacement.
- [ ] No secret/internal exception leakage through `ApplicationError.Message`.
- [ ] No domain entities exposed in public schemas.
- [ ] `rg "OperationResult"` shows only approved remaining non-migrated types.
- [ ] `rg "OperationFailure"` shows only approved remaining non-migrated enums.
- [ ] All changed controllers use shared mapper instead of controller-local switch.

## Failure Modes Registry

| Risk | Symptom | Prevention |
| --- | --- | --- |
| API envelope changes accidentally | Control Plane Web or Storefront client fails to parse response | Contract tests and snapshot response shape before migration. |
| Status code regression | 404 becomes 400 or 500 | Mapper tests plus endpoint characterization tests. |
| Domain-specific semantics lost | `AlreadyExists` or `RemoteFailure` disappears | Use `ApplicationError.Code` and `Metadata`; migrate special cases after simple pilot. |
| Too-wide migration breaks tests | Many constructor mocks fail at once | Migrate one capability cluster per phase. |
| Error messages leak internals | Exception message reaches public API | Require safe message factory and tests for known sensitive fields. |
| `ServiceResponse<T>` removal breaks legacy/shared | Legacy tests and Web.SharedV2 fail | Adapter-first, no immediate deletion. |
| UI client gains wrong dependencies | Control Plane Web references server Application internals in a bad direction | Keep client projection separate unless contract generation replaces it. |

## Test Diagram

```text
Application service
  -> ApplicationResult<T>
      -> ApplicationError.Kind/Code/Message

ControlPlane API mapper
  -> ControlPlaneApiResponse<T>
  -> HTTP status
  -> contract tests

CommerceNode API mapper
  -> CommerceNodeApiResponse<T>
  -> HTTP status
  -> contract tests

Existing clients
  -> same response envelope
  -> same user-visible messages
```

## Implementation Checklist

- [ ] Phase 0 complete.
- [ ] Phase 1 complete.
- [ ] Phase 2 complete.
- [ ] Phase 3 complete and old media result types removed.
- [ ] Phase 4 complete or explicitly deferred.
- [ ] Phase 5 complete or explicitly deferred.
- [ ] Phase 6 adapter decision documented.
- [ ] Phase 7 client result decision documented.
- [ ] Phase 8 verification complete.

## Not In Scope

- [ ] Rewrite every service signature in one commit.
- [ ] Change public HTTP response envelope.
- [ ] Replace `ControlPlaneApiResponse<T>` or `CommerceNodeApiResponse<T>`.
- [ ] Remove `ServiceResponse<T>` globally in first phase.
- [ ] Refactor checkout/cart/payment service behavior.
- [ ] Touch legacy Presentation unless a test-only compatibility adapter requires it.
- [ ] Introduce FluentResults/OneOf/third-party result libraries.
- [ ] Convert domain validation objects that are not operation results.

## Decision Audit Trail

- Keep result standardization behind stable HTTP adapters.
- Prefer small capability pilots over a broad mechanical rewrite.
- Preserve existing public API contract first.
- Treat `ServiceResponse<T>` as a later migration target, not the first target.
- Use `ApplicationError.Code` for domain-specific semantics instead of creating a new enum per feature.
- Keep Presentation responsible for HTTP status mapping.
