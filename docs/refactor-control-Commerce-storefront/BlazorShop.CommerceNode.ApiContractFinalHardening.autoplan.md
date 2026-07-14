# CommerceNode API Contract Final Hardening - Autoplan

Date: 2026-07-14
Status: Ready for phased implementation
Mode: HOLD SCOPE final contract hardening
Primary boundary: `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API`
Primary route family: `api/storefront/stores/{storeKey}/*`
Previous phase: `BlazorShop.CommerceNode.ApiContractFoundationStorefrontHardening.autoplan.md`

## Objective

Finish the Storefront API contract hardening pass so `/swagger/storefront/swagger.json` is truthful enough for Storefront V2, generated C#/TypeScript clients, and AI agents to consume without reading source code or guessing.

This phase is not a Cart Lifecycle or Checkout business expansion. It fixes the remaining contract defects exposed by review:

- no response schema may collapse to `object`;
- auth/token responses must not double-envelope `success` and `message`;
- required/nullable response metadata must reflect real runtime behavior;
- arrays in public responses must be non-null arrays;
- error responses must have required machine-readable fields;
- OpenAPI security must match runtime authentication;
- payment capture must have safe command semantics;
- snapshots and contract tests must permanently guard these rules.

## Scope

In scope:

- Storefront API response DTO and envelope cleanup.
- Storefront OpenAPI schema metadata cleanup.
- Storefront auth, checkout, order, payment, catalog, SEO, page, store, recommendation contract tests.
- Focused runtime behavior alignment where the current behavior contradicts the contract.
- Storefront V2 model/client compatibility updates only where API contract names change.
- QA todo updates for CommerceNode and Storefront V2.

Out of scope:

- No broad cart aggregate redesign.
- No guest cart token, cart merge, coupon, reservation, checkout session, or order state machine.
- No payment provider redesign beyond PayPal capture contract safety.
- No full SDK package or public SDK publishing.
- No Commerce Admin contract hardening except shared helpers directly needed by Storefront Swagger.
- No legacy `api/internal/*` feature work.

## Root Cause Investigation

The review findings are real. The current phase achieved route coverage and basic response-schema presence, but it did not make the schemas semantically safe for generated clients.

| Finding | Evidence | Root cause | Required fix |
|---|---|---|---|
| Storefront Swagger still exposes `object` success envelopes. | Snapshot contains `ObjectCommerceNodeApiResponse` and `StorefrontAuthResponseCommerceNodeApiResponse`; local snapshot scan found 13 references. `CommerceNodeSwaggerExtensions.cs` maps register/logout/change-password/checkout/save-checkout/newsletter/confirm to `CommerceNodeApiResponse<object>`. | Command operations reused generic success helper and Swagger metadata used `object` as a placeholder. Contract tests only required "some schema", not a typed schema. | Add a non-generic command response and typed command/result DTOs. Contract test must assert zero `ObjectCommerceNodeApiResponse`. |
| Auth response is double-enveloped. | `StorefrontAuthResponse` includes `Success`, `Message`, `Token`, then controller returns `CommerceNodeApiResponse<StorefrontAuthResponse>` in login/refresh. | Service response shape leaked into public DTO instead of exposing only token payload inside the success envelope. | Replace with `StorefrontTokenResponse` containing token data only. Keep refresh token in HttpOnly cookie, never in body. |
| Refresh token without cookie returns wrong status. | `StorefrontScopedAuthController.RefreshToken` returns `400 validation_error` when the refresh cookie is missing. | Runtime treats missing credentials as validation instead of unauthenticated. | Return `401 auth.refresh_cookie_missing` or equivalent stable auth code and update OpenAPI/tests. |
| Error schema required fields are not enforced in OpenAPI. | Snapshot scan shows `CommerceNodeApiErrorResponse.required` is empty and `traceId` nullable. | Record constructor parameters are not being marked required by generated schema, and `TraceId` is nullable in the contract. | Make error DTO/schema required for `success`, `code`, `message`, `traceId`; `fieldErrors` remains optional. |
| Public collection schemas are optional/nullable. | Snapshot scan found 21 array fields nullable or not required, including response `data`, paged `items`, category `children`, order `lines`, variant `attributes`, sitemap lists, options/values. | DTO properties use nullable/shared DTO types or Swashbuckle does not infer required collection properties. | Mark public response collections required and non-null through DTO defaults/schema filter/tests. Runtime mappings must always emit arrays. |
| `amountPayed` typo is part of the public schema. | `StorefrontOrderItemHistoryResponse` exposes `AmountPayed`; mapping reads `item.AmountPayed`; snapshot contains `amountPayed`. | Application legacy spelling leaked into the Storefront public DTO. | Rename public contract field to `AmountPaid`; keep internal mapping from legacy/application spelling if needed. |
| PayPal capture returns success envelope even when capture fails. | `CapturePayPal` returns `Success(new StorefrontPayPalCaptureResponse(false, ..., "failed."), "failed.")`, which is HTTP 200 with `success=true`. | Payment-provider failure is modeled as a successful transport operation. | Make failed capture return typed non-2xx error, likely 409 conflict or 400 depending service result. Keep no raw token logging and guard return URL. |
| Sort values are string-pattern only, not a named enum schema. | `StorefrontProductCatalogQuery.SortBy` uses `RegularExpression(StorefrontContractValidation.SortByPattern)`. Current test only checks pattern contains `newest`. | Regex validation protects runtime but generated clients do not get a named enum. | Emit string enum values in OpenAPI and keep runtime validation. |
| Contract tests are too shallow. | Current tests check response schema exists and `responses.Count > 1`, but not typed command models, required error fields, nullable collections, broken refs, refresh security, or real generator build. | Previous tests protected metadata presence, not contract semantics. | Add stricter OpenAPI contract tests plus integration smoke for auth/errors/payment and generated-client compile. |

Current baseline from `storefront-openapi.snapshot.json`:

- `ops=30`
- `opsMissing500=0`
- `only200=0`
- `ObjectCommerceNodeApiResponse=True`
- `StorefrontAuthResponseCommerceNodeApiResponse=True`
- `amountPayed=True`
- `nullableOrOptionalPublicArrayIssues=21`
- `CommerceNodeApiErrorResponse.required=[]`

## Target Contract

### Success Envelopes

Keep the existing generic success envelope for typed query/result payloads:

```csharp
public sealed record CommerceNodeApiResponse<TData>(
    bool Success,
    string Message,
    TData Data);
```

Add a non-generic command response for commands that do not return a resource:

```csharp
public sealed record CommerceNodeApiResponse(
    bool Success,
    string? Message);
```

Rules:

- Do not use `CommerceNodeApiResponse<object>` in Storefront OpenAPI metadata.
- Do not use anonymous objects as Storefront public success payloads.
- `data` is required and non-null for generic success envelopes.
- Commands use the non-generic command response unless they have a real typed result.

### Auth Token Response

Replace public auth payload with:

```csharp
public sealed record StorefrontTokenResponse(
    string AccessToken,
    DateTime ExpiresAtUtc);
```

If the service does not currently expose `ExpiresAtUtc`, add the narrowest safe source:

- prefer existing JWT/options expiry information if already available;
- otherwise add a clearly named expiry field to the application auth response;
- do not expose refresh token in response body.

### Error Response

```csharp
public sealed record CommerceNodeApiErrorResponse(
    bool Success,
    string Code,
    string Message,
    string TraceId,
    IReadOnlyDictionary<string, string[]>? FieldErrors = null);
```

Rules:

- `success` is always `false`.
- `code`, `message`, and `traceId` are required.
- `fieldErrors` appears only when relevant.
- No raw exception details in `message`.
- Use stable dotted or namespaced codes for new auth/payment/store errors, for example:
  - `auth.invalid_credentials`
  - `auth.refresh_cookie_missing`
  - `auth.unauthenticated`
  - `store.not_found`
  - `store.conflict`
  - `validation.failed`
  - `payment.paypal_capture_failed`
  - `internal.unexpected`

### Security Metadata

| Operation family | Security target |
|---|---|
| register, login, confirm email | anonymous |
| refresh token | `RefreshCookie` |
| logout | `RefreshCookie` unless implementation supports Bearer logout; document exactly what runtime enforces |
| change password, update profile | `Bearer` |
| checkout | anonymous or Bearer if guest checkout is supported; OpenAPI should use `[{}, { Bearer: [] }]` only when runtime genuinely supports both |
| save checkout, confirm order, current-user orders/items | `Bearer` |
| catalog, pages, SEO, store, payment methods, recommendations | anonymous unless runtime changes |
| PayPal capture | provider token request body, no Bearer requirement unless runtime actually requires customer auth |

## Implementation Alternatives

| Approach | Summary | Effort | Risk | Decision |
|---|---|---:|---:|---|
| A. Patch Swagger metadata only | Change `CommerceNodeSwaggerExtensions.cs` to point at better types, leave runtime envelopes and DTOs as-is. | S | High | Rejected. It can make Swagger lie while runtime still returns double envelopes, 200 failed captures, and nullable collections. |
| B. Contract-first Storefront cleanup | Add/adjust public DTOs and response helpers, align runtime return shapes, then harden OpenAPI tests. | M | Medium | Selected. It fixes the real public contract while staying inside the Storefront boundary. |
| C. Full API versioning plus SDK packaging | Introduce versioned API and publish generated client package. | L | High | Deferred. Useful later, but it expands beyond final hardening and requires version/package policy decisions. |

Recommendation: implement Approach B.

## Phase Plan

### Phase 0 - Red Baseline And Snapshot Inventory

Goal:

- Convert the review findings into failing contract tests before changing production code.

Tasks:

- Add or extend OpenAPI tests to assert current baseline defects:
  - zero `ObjectCommerceNodeApiResponse`;
  - zero `StorefrontAuthResponseCommerceNodeApiResponse`;
  - no `StorefrontAuthResponse` payload with `success` or `message`;
  - `CommerceNodeApiErrorResponse` required includes `success`, `code`, `message`, `traceId`;
  - no nullable or optional public response collections for the agreed collection list;
  - no `amountPayed` in public Storefront schemas;
  - protected endpoints have exact security scheme names;
  - refresh token without cookie is modeled as 401;
  - PayPal capture failure is not modeled as 200 success.
- Keep snapshot update disabled by default.
- Record the current baseline numbers in the test comments or failure messages.

Candidate files:

- `BlazorShop.Tests/PresentationV2/CommerceNode/CommerceNodeStorefrontOpenApiContractTests.cs`
- `BlazorShop.Tests/PresentationV2/CommerceNode/Snapshots/storefront-openapi.snapshot.json`

Exit criteria:

- Focused contract tests fail for the known review findings before production fixes are applied.
- No runtime code has been changed in this phase.

Verification:

```powershell
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests"
```

Commit boundary:

- Commit test baseline separately.
- Suggested message: `test: expose storefront openapi final hardening gaps`

### Phase 1 - Response Envelope And Auth Payload Cleanup

Goal:

- Remove `object` success responses and auth double-envelope from Storefront runtime and Swagger.

Tasks:

- Add non-generic `CommerceNodeApiResponse`.
- Add typed command/result responses:
  - register: `StorefrontRegistrationResponse` or command response if no token/resource should be returned;
  - logout: command response;
  - change password: command response;
  - confirm email: command response;
  - update profile: command response unless runtime returns profile;
  - newsletter subscribe: command response;
  - save checkout: command response;
  - checkout: `StorefrontCheckoutResponse`;
  - confirm order: `StorefrontOrderResponse` or a narrow `StorefrontOrderConfirmationResponse`;
  - PayPal capture: `StorefrontPayPalCaptureResponse`.
- Replace `StorefrontAuthResponse` with `StorefrontTokenResponse`.
- Update login and refresh to return `CommerceNodeApiResponse<StorefrontTokenResponse>`.
- Update `StorefrontApiControllerBase.FromServiceResponse` so Storefront command responses do not produce `CommerceNodeApiResponse<object>`.
- Keep admin/control response helpers unchanged unless compilation requires shared helper adjustment.
- Update Swagger operation metadata success response types.

Candidate files:

- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Responses/CommerceNodeApiResponse.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontApiControllerBase.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontScopedControllers.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/StorefrontApiContracts.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/StorefrontContractMappings.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Swagger/CommerceNodeSwaggerExtensions.cs`
- Storefront V2 auth/client models if response names changed.

Exit criteria:

- `rg "CommerceNodeApiResponse<object>" BlazorShop.PresentationV2/BlazorShop.CommerceNode.API` returns no Storefront controller or Storefront Swagger metadata usage.
- Storefront OpenAPI has zero `ObjectCommerceNodeApiResponse`.
- Auth success payload contains access token fields only, not nested `success` or `message`.

Verification:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests|FullyQualifiedName~StorefrontV2Auth"
```

Commit boundary:

- Suggested message: `fix: remove storefront object response contracts`

### Phase 2 - Error Contract, Status Codes, And Security Alignment

Goal:

- Make runtime error responses and OpenAPI security match actual auth/error behavior.

Tasks:

- Make `CommerceNodeApiErrorResponse.TraceId` non-nullable.
- Configure schema generation so error response has required `success`, `code`, `message`, `traceId`.
- Normalize model validation errors to `CommerceNodeApiErrorResponse` with `fieldErrors`.
- Align statuses:
  - validation: 400;
  - unauthenticated: 401;
  - forbidden: 403 only where runtime can return it;
  - not found: 404;
  - conflict: 409;
  - unexpected: 500;
  - do not declare 422, 429, or 503 unless runtime truly returns them.
- Change refresh-token missing cookie and invalid cookie paths from 400 to 401.
- Review logout behavior and make OpenAPI declare exactly `RefreshCookie` or `Bearer` according to runtime.
- Add store-related errors for list categories, category tree, catalog sitemap, SEO settings only if store context middleware/runtime can return them on those operations; otherwise do not declare impossible statuses.
- Ensure anonymous checkout security is represented as anonymous-or-Bearer only if runtime accepts both paths.

Candidate files:

- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Responses/CommerceNodeApiErrorResponse.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontApiControllerBase.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontScopedControllers.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Swagger/CommerceNodeSwaggerExtensions.cs`
- `Program.cs` or API behavior options if model-state response factory is configured there.

Exit criteria:

- Protected endpoint without token returns 401 with typed error response.
- Refresh without cookie returns 401 with typed error response.
- Validation errors return 400 with `fieldErrors`.
- All declared error statuses have `CommerceNodeApiErrorResponse` schema.
- No protected operation has anonymous-only `security`.

Verification:

```powershell
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests|FullyQualifiedName~CommerceNodeStorefront"
```

Commit boundary:

- Suggested message: `fix: align storefront error and auth contracts`

### Phase 3 - Required, Nullable, Collections, And Validation Metadata

Goal:

- Make generated models reflect actual required fields, nullable fields, and non-null arrays.

Tasks:

- Ensure public success envelopes require `success`, `message`, and `data` where generic.
- Ensure public command response requires `success` and includes nullable `message` only if runtime may omit it.
- Ensure public response collections are required and non-null:
  - generic envelope `data` when `TData` is a collection;
  - paged `items`;
  - category tree `children`;
  - category page `products`;
  - product `variants`;
  - variant `attributes`;
  - order `lines`;
  - order line `variantAttributes`;
  - sitemap `categories`, `products`, `pages`;
  - variation template `options`;
  - variation option `values`;
  - recommendations/payment/category/product list payloads.
- Keep request collection nullability deliberate:
  - `selectedAttributes` may remain optional only if business logic truly accepts no selections.
- Add/confirm validation metadata:
  - `confirm-email.userId` required;
  - `confirm-email.token` required;
  - SEO redirect `path` required and bounded;
  - `storeKey`, `slug`, and search terms have length limits;
  - quantity has min 1 and a real business maximum, not `int.MaxValue`;
  - page size has max 100 unless changed deliberately;
  - email format and max length;
  - password min length and documented requirements;
  - shipping address required fields;
  - country code uppercase ISO alpha-2 if runtime expects it.
- Convert `SortBy` OpenAPI from regex-only to a named string enum while keeping runtime validation.
- Mark required request bodies on every body-reading operation.

Candidate files:

- `StorefrontApiContracts.cs`
- `StorefrontContractMappings.cs`
- `CommerceNodeSwaggerExtensions.cs`
- New schema filter if Swashbuckle needs explicit required/nullable overrides.
- Contract tests and snapshot.

Exit criteria:

- Contract test finds zero nullable/optional public response collection issues for the target list.
- Sort schema emits string enum values.
- Request bodies are required where runtime requires a body.
- Validation metadata is visible in OpenAPI.

Verification:

```powershell
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests"
```

Commit boundary:

- Suggested message: `fix: harden storefront schema nullability and validation`

### Phase 4 - Monetary Fields And Public Naming Cleanup

Goal:

- Make money fields and public names generator-safe and typo-free.

Tasks:

- Ensure all public monetary fields are decimal numbers in OpenAPI:
  - `price`
  - `comparePrice`
  - `effectivePrice`
  - `unitPrice`
  - `lineTotal`
  - `totalAmount`
  - `amountPaid`
- Add schema format/extension for decimal if Swashbuckle does not emit the desired decimal format.
- Rename public `AmountPayed` to `AmountPaid` in `StorefrontOrderItemHistoryResponse`.
- Keep internal/application `AmountPayed` as-is unless a broader application rename is approved; map it to public `AmountPaid`.
- Update Storefront V2 shared model `GetOrderItem` if it consumes this Storefront public contract.
- Add compatibility note if any UI JSON parser expected `amountPayed`.

Candidate files:

- `StorefrontApiContracts.cs`
- `StorefrontContractMappings.cs`
- `BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/Models/Payment/GetOrderItem.cs`
- Storefront V2 tests that read current-user order items.
- OpenAPI snapshot.

Exit criteria:

- No Storefront public OpenAPI schema contains `amountPayed`.
- Contract tests assert monetary fields use number/decimal-compatible schema.
- Storefront V2 compiles and tests pass.

Verification:

```powershell
rg -n "amountPayed|AmountPayed" BlazorShop.PresentationV2/BlazorShop.CommerceNode.API BlazorShop.PresentationV2/BlazorShop.Storefront.V2 BlazorShop.PresentationV2/BlazorShop.Web.SharedV2
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests|FullyQualifiedName~PresentationV2.Storefront"
```

Commit boundary:

- Suggested message: `fix: normalize storefront monetary contracts`

### Phase 5 - PayPal Capture Runtime Safety

Goal:

- Make PayPal capture a safe, typed command contract without expanding payment architecture.

Tasks:

- Keep `POST /payments/paypal/capture`.
- Require body and token.
- Do not log raw PayPal token.
- Validate `returnUrl` as relative or allowlisted if the field remains.
- Return typed success response only when provider capture succeeds.
- Return typed `CommerceNodeApiErrorResponse` when capture fails:
  - 400 for invalid/missing token/request;
  - 409 for provider says capture cannot complete or was already handled, depending current service capability;
  - 500 for unexpected provider/runtime errors.
- Investigate whether `IPayPalPaymentService.CaptureAsync` can distinguish duplicate capture from generic failure. If not, keep idempotency test scoped to "does not create duplicate payment/order" where existing data model/service can prove it.
- Add integration/contract tests:
  - GET capture returns 405 and does not perform capture;
  - POST missing token returns 400 typed error;
  - POST failed provider response is not `success=true`;
  - repeat capture does not duplicate payment/order if service exposes enough state.

Candidate files:

- `StorefrontScopedControllers.cs`
- Payment service tests if service semantics need a small result type.
- `CommerceNodeStorefrontOpenApiContractTests.cs`
- Storefront V2 PayPal callback/client tests if present.

Exit criteria:

- Failed PayPal capture cannot be represented as HTTP 200 `success=true`.
- OpenAPI has typed request/response/error contract for capture.
- No GET performs capture side effects.

Verification:

```powershell
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~PayPal|FullyQualifiedName~CommerceNodeStorefront"
```

Commit boundary:

- Suggested message: `fix: harden storefront paypal capture contract`

### Phase 6 - OpenAPI Validation, Client Generation, And Snapshot

Goal:

- Make Storefront Swagger mechanically trustworthy.

Tasks:

- Validate OpenAPI document with a real parser/validator, not only `JsonDocument`.
- Add broken `$ref` traversal test.
- Add test that 100% operations have:
  - stable `operationId`;
  - non-empty `summary`;
  - success response schema;
  - 500 response schema;
  - required request body when body exists;
  - expected security scheme when protected.
- Add tests that no operation declares only `200 OK`.
- Add tests that no public schema name or serialized schema contains forbidden domain/entity names.
- Generate a TypeScript or C# client from Swagger and compile/build it if the chosen generator supports it reliably in tests.
- Keep generated client output in temp/TestResults, not committed, unless a future SDK phase approves packaging.
- Update and commit deterministic Swagger snapshot after all fixes are green.

Candidate files:

- `CommerceNodeStorefrontOpenApiContractTests.cs`
- `BlazorShop.Tests/BlazorShop.Tests.csproj`
- `BlazorShop.Tests/PresentationV2/CommerceNode/Snapshots/storefront-openapi.snapshot.json`
- `BlazorShop.Tests/PresentationV2/CommerceNode/Snapshots/storefront-openapi.paths.snapshot.txt`

Exit criteria:

- Contract test suite proves every review DoD item that can be verified from OpenAPI.
- Generated-client smoke uses operation IDs as method names and does not emit untyped `object`/`any` for public response payloads.
- Snapshot is updated after tests pass.

Verification:

```powershell
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests"
```

Commit boundary:

- Suggested message: `test: enforce storefront openapi final contract gates`

### Phase 7 - Integration Smoke, QA Docs, And Final Report

Goal:

- Prove runtime behavior and document the completed hardening.

Tasks:

- Add or update focused integration tests:
  - change-password without token -> 401 typed error;
  - current-user orders without token -> 401 typed error;
  - refresh without cookie -> 401 typed error;
  - wrong store key -> correct status and stable error code;
  - validation failure -> `fieldErrors`;
  - not found -> typed 404;
  - unhandled exception path -> typed 500 with traceId where feasible in test host;
  - guest checkout still works if it worked before.
- Update QA docs:
  - `QA-CommerceNode.todo.md` with OpenAPI final hardening evidence.
  - `QA-StorefrontV2.todo.md` for changed auth/payment/order-item public fields.
- Update architecture docs only if the standards doc needs a final addendum based on implementation.
- Run focused test set and record commands/results in final report.

Candidate files:

- `BlazorShop.Tests/PresentationV2/CommerceNode/*`
- `docs/refactor-control-Commerce-storefront/QA-CommerceNode.todo.md`
- `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- This plan file if implementation notes are appended.

Exit criteria:

- OpenAPI contract tests pass.
- Focused Storefront V2 tests pass.
- QA docs reflect the final hardening pass.
- Worktree contains only intended changes per phase.

Verification:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests|FullyQualifiedName~PresentationV2.Storefront|FullyQualifiedName~CommerceNodeRouteBoundaryTests"
git status --short
```

Commit boundary:

- Suggested message: `docs: record storefront api final hardening`

## Contract Test Checklist

OpenAPI tests must permanently assert:

- 30/30 operations have `operationId`.
- 30/30 operations have `summary`.
- 30/30 operations have success response schema.
- 30/30 operations have 500 response schema.
- Every declared response has `application/json` schema.
- Every required request body has `requestBody.required=true`.
- Protected endpoints reference `Bearer` or `RefreshCookie` exactly as intended.
- No protected endpoint uses anonymous-only `security`.
- No `ObjectCommerceNodeApiResponse`.
- No auth double-envelope schema.
- No broken `$ref`.
- No nullable or optional public response collections for the target list.
- Success envelope required fields are explicit.
- Error response required fields include `success`, `code`, `message`, `traceId`.
- No public schema contains domain entities or unsafe server-owned fields.
- No `amountPayed` in public Storefront schema.
- Sort values are named strings.
- PayPal capture is POST with required body.
- OpenAPI validates.
- Generated client smoke passes.
- Snapshot detects breaking contract change.

## Risk Register

| Risk | Severity | Detection | Mitigation |
|---|---:|---|---|
| Fixing response envelope breaks Storefront V2 auth parsing. | High | Storefront V2 auth tests. | Update auth client models in the same phase as runtime response shape. |
| Error response shape diverges from ASP.NET Core model-state errors. | High | Validation integration test. | Configure `InvalidModelStateResponseFactory` or equivalent Storefront-specific model-state response. |
| Required/non-null schema lies while runtime can emit null. | High | Mapping review and response tests. | Initialize arrays with `Array.Empty<T>()` and map null source collections to empty arrays. |
| PayPal capture service cannot distinguish failure classes. | Medium | Payment service test review. | Keep status mapping conservative; do not invent 409 if service cannot prove conflict. |
| Decimal schema format is generator-specific. | Medium | Generated client smoke. | Use standard `type:number` plus stable decimal format/extension only if generator supports it. |
| Snapshot churn hides real contract changes. | Medium | Snapshot diff review. | Normalize ordering and keep explicit snapshot update flow. |
| Contract filters overfit to current 30 operations. | Medium | Route reflection tests. | Derive expected operation list from route family plus curated allowlist only for security semantics. |

## Autoplan Review Decisions

CEO review:

- Hold scope. The fastest path to product value is making the existing Storefront contract trustworthy before new checkout/cart features.
- Do not introduce API versioning or SDK publishing in this pass.
- Treat the OpenAPI document as product surface, not documentation garnish.

Engineering review:

- Fix runtime contract first, then Swagger metadata. Swagger must not lie.
- Keep changes inside Storefront public contracts and response helpers where possible.
- Add red tests before fixes so each review finding has a permanent gate.
- Use boring DTO/schema filters; no new module architecture.
- Preserve admin/control behavior unless a shared helper compile break requires a narrow change.

DX review:

- Primary developer persona: Storefront V2 developer, partner integrator, or AI coding agent generating a client from Swagger.
- Target experience: open Swagger, see exact auth/security, generate typed models, handle errors by code/traceId/fieldErrors, and notice breaking changes through snapshot diffs.
- DX target after this phase: generated client in under 5 minutes from a running/test-host Swagger document.

## Implementation Order Summary

```text
Phase 0  Red tests for known gaps
Phase 1  Response envelope + auth payload cleanup
Phase 2  Error/status/security alignment
Phase 3  Required/nullable/collections/validation metadata
Phase 4  Monetary fields + amountPaid public naming
Phase 5  PayPal capture runtime safety
Phase 6  OpenAPI validation + generated client + snapshot
Phase 7  Integration smoke + QA docs + final report
```

## Final Approval Gate

Implement phase by phase and commit at each phase boundary. Do not start Cart Lifecycle or Checkout expansion from this plan. If a phase reveals that application services cannot support a contract promise, update the contract to match runtime truth or split a separate service-behavior phase before documenting it in Swagger.

## GSTACK REVIEW REPORT

| Run | Status | Findings | Resolution |
|---|---|---:|---|
| Investigate | Complete | 8 | Root causes traced to response wrappers, auth DTOs, Swagger metadata, runtime status behavior, nullable schema generation, and shallow tests. |
| CEO | Complete | 2 | Hold scope; defer SDK publishing/API versioning and finish trustworthy Storefront OpenAPI first. |
| Design | Skipped | 0 | No UI design scope; Swagger/API DX covered by DX review. |
| Engineering | Complete | 7 | Use red tests, runtime-aligned DTO fixes, schema filters only where needed, phase commits, and narrow Storefront boundary. |
| DX | Complete | 6 | Enforce typed responses, exact security, required/nullable metadata, generated-client smoke, stable error codes, and snapshot diffs. |

VERDICT: APPROVED FOR PHASED IMPLEMENTATION.

NO UNRESOLVED DECISIONS
