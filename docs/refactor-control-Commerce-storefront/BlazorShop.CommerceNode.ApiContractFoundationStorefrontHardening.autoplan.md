# CommerceNode API Contract Foundation And Storefront Hardening - Autoplan

Date: 2026-07-14
Status: Ready for phased implementation
Mode: HOLD SCOPE contract hardening
Primary boundary: `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API`
Primary route family: `api/storefront/stores/{storeKey}/*`

## Objective

Make the Commerce Node Storefront OpenAPI document complete enough that Storefront clients, generated clients, and AI agents can consume it without guessing request models, response models, security, validation rules, or error shape.

This phase is an API contract foundation phase. It hardens Storefront-facing contracts and OpenAPI metadata, fixes the risky public contract issues already identified, and adds contract tests so future endpoint work cannot silently degrade Swagger quality.

## Premises

| Premise | Evidence | Risk if wrong | Plan response |
|---|---|---|---|
| Storefront Swagger is currently incomplete. | `StorefrontScopedControllers.cs` returns `IActionResult` widely and has little `ProducesResponseType`, operationId, summary, request body, or security metadata. | AI/client generation guesses shapes or emits `object`/anonymous models. | Add explicit request/response DTOs and endpoint metadata before snapshotting. |
| The Storefront API must not expose admin/domain knobs. | Storefront product query binds `ProductCatalogQuery`, which includes `IsPublished`; `GetCatalogProduct` and `GetProduct` expose `IsPublished`. | Public callers can see or attempt to send fields that belong to admin/runtime internals. | Introduce Storefront-specific query and public response DTOs; map from application DTOs. |
| Storefront mutation contracts need auth and validation clarity. | Save checkout uses `CreateOrderItem` with `UserId`; confirm accepts `status`; PayPal capture is `GET`; quantity lacks minimum metadata. | Public clients can send identity/status values the server should own, and generated clients model unsafe calls as valid. | Replace public request DTOs and update service mapping while preserving server-side behavior. |
| Standardized errors should not break successful Storefront V2 reads. | Storefront V2 parses existing `{ success, message, data }` envelopes. | Changing every response envelope at once risks Storefront UI regressions. | Keep success envelope for 2xx; introduce a documented non-2xx error DTO and teach clients/tests to handle it. |
| The contract test should protect Swagger, not business logic. | Existing tests already cover Storefront route use and scoped route boundary. | Over-testing service internals slows this phase and blurs contract scope. | Add reflection/OpenAPI tests only for metadata, schema quality, security, validation, snapshot, and generated-client smoke. |

## What Already Exists

### Runtime And Swagger

- Storefront controllers are consolidated in `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontScopedControllers.cs`.
- Controller helper lives in `StorefrontApiControllerBase.cs`.
- Swagger split lives in `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Swagger/CommerceNodeSwaggerExtensions.cs`.
- Current Storefront Swagger document is `/swagger/storefront/swagger.json`.
- Swagger uses Swashbuckle `10.2.3`; annotations are available through the Swashbuckle stack and should be enabled explicitly if used.

### Current Contract Gaps Observed

- `StorefrontScopedAuthController.Register/Login/RefreshToken/Logout/ChangePassword/UpdateProfile` lacks stable operation metadata and complete response declarations.
- `StorefrontScopedCartController.SaveCheckout` accepts `IEnumerable<CreateOrderItem>`; `CreateOrderItem` includes client-supplied `UserId`.
- `StorefrontScopedOrdersController.ConfirmOrder` accepts `[FromQuery] string? status`; status should be server-owned.
- `StorefrontScopedPaymentsController.CapturePayPal` is `GET payments/paypal/capture` and performs capture side effects.
- `StorefrontScopedCatalogController.GetProducts` accepts `[FromQuery] ProductCatalogQuery`; this exposes `IsPublished` and enum values as numeric schema.
- Storefront request models lack enough `Required`, `Range`, `EmailAddress`, `MinLength`, `MaxLength`, and body-required metadata for generated clients.
- Protected endpoints have `[Authorize]`, but Swagger does not declare Bearer/cookie schemes or per-operation security requirements.
- Error responses are mostly ad hoc `CommerceNodeApiResponse<T>.Failed(...)` and are not declared consistently in OpenAPI.
- Anonymous object response in `StorefrontScopedStoreController.Maintenance` cannot produce a stable generated client model.

### Existing Tests

- `BlazorShop.Tests/PresentationV2/CommerceNode/CommerceNodeRouteBoundaryTests.cs` already reflects over CommerceNode API controllers.
- Storefront V2 client tests assert scoped Storefront route usage.
- No current test parses `/swagger/storefront/swagger.json`, validates operation response schema coverage, or generates a client.

## Not In Scope

- No new ecommerce business behavior.
- No database migration.
- No removal of legacy `BlazorShop.Presentation/*` projects.
- No broad Commerce Admin API contract hardening beyond shared Swagger helpers needed by Storefront.
- No public media URL redesign.
- No new auth scheme implementation. This phase only documents existing Bearer JWT and refresh cookie behavior in Swagger.
- No full generated SDK package. Only a smoke generation test to prove the OpenAPI contract is machine-consumable.

## Architecture Target

```text
Storefront V2 / generated clients / AI agents
  -> /swagger/storefront/swagger.json
       -> operationId + summary
       -> request DTO schemas
       -> 2xx response envelope schemas
       -> standard error schemas
       -> security requirements
       -> validation constraints
       -> named enum values

Storefront API runtime
  -> StorefrontScoped*Controller
       -> Storefront public request DTOs
       -> application services
       -> Storefront public response DTOs
       -> CommerceNodeApiResponse<T> success envelope
       -> CommerceNodeApiErrorResponse error envelope
```

## Contract Principles

1. Public Storefront request DTOs must be Storefront-specific. They must not expose domain entities, EF entities, admin DTOs, `UserId`, `IsPublished`, or server-owned status fields.
2. Every Storefront operation must have a stable `operationId`, short summary, request schema where applicable, response schema for all expected status codes, and explicit security metadata.
3. Every non-2xx response should use one public error model with machine-readable code and field errors.
4. Quantity, page size, email, password, and shipping address validation must exist both at runtime and in OpenAPI schema metadata.
5. String enum values are the public contract. Numeric enum values are an implementation detail.
6. Contract tests should fail on incomplete metadata before clients or AI agents see the broken Swagger.

## Public Error Contract

Introduce a public error DTO for Storefront non-2xx responses:

```csharp
public sealed record CommerceNodeApiErrorResponse(
    bool Success,
    string Code,
    string Message,
    string? TraceId,
    IReadOnlyDictionary<string, string[]>? FieldErrors = null);
```

Rules:

- `Success` is always `false`.
- `Code` is stable and machine-readable, for example `validation_error`, `unauthorized`, `not_found`, `conflict`, `store_scope_missing`, `payment_capture_failed`, `internal_error`.
- `Message` is safe for Storefront UI display.
- `TraceId` comes from `HttpContext.TraceIdentifier`.
- `FieldErrors` is present for model validation failures.
- Success responses keep the existing `CommerceNodeApiResponse<TData>` shape to avoid breaking Storefront V2 reads.

## Storefront Operation Contract Matrix

All operation IDs are stable public names. If an implementation needs a different method name, the `operationId` below still wins.

| Route | Method | Operation ID | Security | Request model | Success response | Error responses |
|---|---|---|---|---|---|---|
| `/auth/register` | POST | `StorefrontAuth_Register` | Anonymous | `StorefrontRegisterRequest` | `CommerceNodeApiResponse<StorefrontAuthResponse>` | 400, 409, 500 |
| `/auth/login` | POST | `StorefrontAuth_Login` | Anonymous | `StorefrontLoginRequest` | `CommerceNodeApiResponse<StorefrontAuthResponse>` | 400, 401, 423, 500 |
| `/auth/refresh-token` | POST | `StorefrontAuth_RefreshToken` | Refresh cookie | none | `CommerceNodeApiResponse<StorefrontAuthResponse>` | 400, 401, 500 |
| `/auth/logout` | POST | `StorefrontAuth_Logout` | Refresh cookie optional | none | `CommerceNodeApiResponse<object>` | 400, 500 |
| `/auth/change-password` | POST | `StorefrontAuth_ChangePassword` | Bearer | `StorefrontChangePasswordRequest` | `CommerceNodeApiResponse<object>` | 400, 401, 500 |
| `/auth/confirm-email` | GET | `StorefrontAuth_ConfirmEmail` | Anonymous | query `userId`, `token` | `CommerceNodeApiResponse<object>` | 400, 404, 500 |
| `/auth/update-profile` | POST | `StorefrontAuth_UpdateProfile` | Bearer | `StorefrontUpdateProfileRequest` | `CommerceNodeApiResponse<object>` | 400, 401, 500 |
| `/catalog/categories` | GET | `StorefrontCatalog_ListCategories` | Anonymous | none | `CommerceNodeApiResponse<IReadOnlyList<StorefrontCategoryResponse>>` | 500 |
| `/catalog/categories/tree` | GET | `StorefrontCatalog_GetCategoryTree` | Anonymous | none | `CommerceNodeApiResponse<IReadOnlyList<StorefrontCategoryTreeNodeResponse>>` | 500 |
| `/catalog/categories/{id}` | GET | `StorefrontCatalog_GetCategoryById` | Anonymous | route `id` | `CommerceNodeApiResponse<StorefrontCategoryResponse>` | 404, 500 |
| `/catalog/categories/slug/{slug}` | GET | `StorefrontCatalog_GetCategoryBySlug` | Anonymous | route `slug` | `CommerceNodeApiResponse<StorefrontCategoryPageResponse>` | 404, 500 |
| `/catalog/categories/{categoryId}/products` | GET | `StorefrontCatalog_ListProductsByCategory` | Anonymous | route `categoryId` | `CommerceNodeApiResponse<IReadOnlyList<StorefrontCatalogProductResponse>>` | 404, 500 |
| `/catalog/products` | GET | `StorefrontCatalog_QueryProducts` | Anonymous | `StorefrontProductCatalogQuery` | `CommerceNodeApiResponse<PagedResult<StorefrontCatalogProductResponse>>` | 400, 500 |
| `/catalog/products/{id}` | GET | `StorefrontCatalog_GetProductById` | Anonymous | route `id` | `CommerceNodeApiResponse<StorefrontProductResponse>` | 404, 500 |
| `/catalog/products/slug/{slug}` | GET | `StorefrontCatalog_GetProductBySlug` | Anonymous | route `slug` | `CommerceNodeApiResponse<StorefrontProductResponse>` | 404, 500 |
| `/catalog/sitemap` | GET | `StorefrontCatalog_GetSitemap` | Anonymous | none | `CommerceNodeApiResponse<GetPublicCatalogSitemap>` | 500 |
| `/cart/checkout` | POST | `StorefrontCart_Checkout` | Anonymous or Bearer | `StorefrontCheckoutRequest` | `CommerceNodeApiResponse<StorefrontCheckoutResult>` | 400, 401, 409, 500 |
| `/cart/save-checkout` | POST | `StorefrontCart_SaveCheckout` | Bearer | `IReadOnlyList<StorefrontOrderItemRequest>` | `CommerceNodeApiResponse<object>` | 400, 401, 500 |
| `/newsletter/subscribe` | POST | `StorefrontNewsletter_Subscribe` | Anonymous | `StorefrontNewsletterSubscribeRequest` | `CommerceNodeApiResponse<object>` | 400, 409, 500 |
| `/orders/confirm` | POST | `StorefrontOrders_Confirm` | Bearer | `IReadOnlyList<StorefrontCartItemRequest>` | `CommerceNodeApiResponse<object>` | 400, 401, 409, 500 |
| `/orders/current-user` | GET | `StorefrontOrders_ListCurrentUserOrders` | Bearer | none | `CommerceNodeApiResponse<IReadOnlyList<StorefrontOrderResponse>>` | 401, 500 |
| `/orders/current-user/items` | GET | `StorefrontOrders_ListCurrentUserOrderItems` | Bearer | none | `CommerceNodeApiResponse<IReadOnlyList<StorefrontOrderItemHistoryResponse>>` | 401, 404, 500 |
| `/pages/{slug}` | GET | `StorefrontPages_GetBySlug` | Anonymous | route `slug` | `CommerceNodeApiResponse<StorefrontPagePublicDto>` | 404, 500 |
| `/payments/methods` | GET | `StorefrontPayments_ListMethods` | Anonymous | none | `CommerceNodeApiResponse<IReadOnlyList<StorefrontPaymentMethodResponse>>` | 404, 500 |
| `/payments/paypal/capture` | POST | `StorefrontPayments_CapturePayPal` | Anonymous or Bearer | `StorefrontPayPalCaptureRequest` | `CommerceNodeApiResponse<StorefrontPayPalCaptureResponse>` | 400, 409, 500 |
| `/recommendations/products/{productId}` | GET | `StorefrontRecommendations_ListProductRecommendations` | Anonymous | route `productId` | `CommerceNodeApiResponse<IReadOnlyList<StorefrontProductRecommendationResponse>>` | 400, 404, 500 |
| `/seo/settings` | GET | `StorefrontSeo_GetSettings` | Anonymous | none | `CommerceNodeApiResponse<SeoSettingsDto>` | 500 |
| `/seo/redirects/resolve` | GET | `StorefrontSeo_ResolveRedirect` | Anonymous | query `path` | `CommerceNodeApiResponse<SeoRedirectResolutionDto>` | 400, 404, 500 |
| `/store/current` | GET | `StorefrontStore_GetCurrent` | Anonymous | none | `CommerceNodeApiResponse<CommerceStorePublicDto>` | 400, 404, 409, 500 |
| `/store/maintenance` | GET | `StorefrontStore_GetMaintenance` | Anonymous | none | `CommerceNodeApiResponse<StorefrontMaintenanceResponse>` | 400, 404, 409, 500 |

## Risky Contract Fixes Required In This Phase

### Remove Client-Supplied Identity

- Remove `UserId` from the public Storefront save-checkout request.
- Prefer introducing `StorefrontOrderItemRequest` and mapping to the existing service-layer shape internally.
- If `CreateOrderItem.UserId` is removed from shared `BlazorShop.Application.DTOs.Payment`, update the blast radius intentionally:
  - `CartService`
  - mapping config
  - active V2 tests
  - legacy compile/test references only as needed to keep solution healthy, without extending legacy behavior.

### Remove Client-Supplied Order Status

- Remove `status` from `POST /orders/confirm`.
- The API should call `ConfirmOrderAsync(carts, userId)` without public query/body status.
- Server-side order/payment status remains owned by payment/order services.

### Do Not Expose `IsPublished` To Storefront Callers

- Replace `[FromQuery] ProductCatalogQuery` on Storefront product query with `StorefrontProductCatalogQuery`.
- `StorefrontProductCatalogQuery` must not contain `IsPublished`.
- Map it to `ProductCatalogQuery` server-side with published-only behavior enforced by `IPublicCatalogService`.
- Public product/category response DTOs should not expose `IsPublished` unless there is a deliberate user-facing reason. If retained for current UI compatibility, mark it as a temporary compatibility field and add a later removal TODO.

### Replace PayPal Capture GET Side Effect

- Add `POST /payments/paypal/capture` with body:
  - `token` required,
  - optional `returnUrl` only if strictly needed and validated as relative or allowed client URL.
- Return a typed `StorefrontPayPalCaptureResponse` instead of direct GET capture side effects:
  - `captured: bool`
  - `redirectPath` or `redirectUrl`
  - `message`
- Do not call PayPal capture from any GET endpoint.
- Keep any old GET route only as a non-mutating compatibility redirect if Storefront V2 or PayPal callback still needs a browser landing route. It must not perform capture.

### Validation Metadata

- `Quantity` must have minimum 1 in runtime validation and OpenAPI schema.
- `PageSize` must have `minimum: 1` and a bounded maximum. Recommended Storefront maximum: `100`, default: `24`.
- Email fields must use `[EmailAddress]`, max length 254, and required where needed.
- Password fields must align with Identity settings: required, minimum 8, at least uppercase/lowercase/digit/special for register and change password.
- Shipping address must validate required fields:
  - `fullName`
  - `email`
  - `address1`
  - `city`
  - `postalCode`
  - `countryCode` with length 2.
- Request bodies for POST operations must be explicit `[FromBody]` and required in OpenAPI.

### Named Sort Enum

- Public Storefront sort values must be strings:
  - `newest`
  - `oldest`
  - `priceLowToHigh`
  - `priceHighToLow`
  - `nameAscending`
  - `nameDescending`
  - `displayOrder`
  - `updated`
- Do not expose integer enum values in Swagger.
- Update Storefront V2 client tests so emitted `sortBy` is a named value and never numeric.

## Implementation Alternatives Reviewed

| Approach | Summary | Effort | Risk | Decision |
|---|---|---:|---:|---|
| A. Attribute-only Swagger polish | Keep current DTOs/actions and add annotations. | S | High | Rejected. It documents unsafe contracts instead of fixing them. |
| B. Storefront contract layer with targeted mappings | Add public request/response DTOs, standard error DTO, annotations, and contract tests. | M | Medium | Selected. It fixes generator quality without rewriting services. |
| C. Full API versioning and generated SDK project | Add versioned Storefront API, generated SDK package, compatibility shims. | L | High | Deferred. Valuable later, too much for this foundation phase. |

## Phase Plan

### Phase 0 - Baseline And OpenAPI Inventory

Goal:

- Capture the current Storefront API contract shape before changes.
- Confirm no dirty worktree state will be mixed into the plan implementation.

Tasks:

- Run `git status --short`.
- Generate current Storefront Swagger from the running API if available, or from test host if implemented first.
- Inventory every route/action in `StorefrontScopedControllers.cs`.
- Inventory every schema currently exposed by `/swagger/storefront/swagger.json`.
- Record known bad contract findings in `QA-CommerceNode.todo.md`.

Candidate files:

- `StorefrontScopedControllers.cs`
- `CommerceNodeSwaggerExtensions.cs`
- `QA-CommerceNode.todo.md`
- New snapshot path under `BlazorShop.Tests/PresentationV2/CommerceNode/Snapshots/`

Exit criteria:

- Baseline route/schema inventory exists.
- Worktree is clean or unrelated changes are explicitly identified.

Verification:

```powershell
git status --short
rg -n "api/storefront/stores" BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers
```

Commit boundary:

- Commit only the plan and baseline docs if this phase changes repository files.
- Suggested message: `docs: plan storefront api contract hardening`

### Phase 1 - Contract Test Harness And Snapshot Foundation

Goal:

- Add tests that can inspect the generated Storefront OpenAPI document before metadata refactors begin.

Tasks:

- Add a CommerceNode API `WebApplicationFactory` based contract test harness.
- Fetch `/swagger/storefront/swagger.json` in tests.
- Parse OpenAPI using a supported parser package.
- Add initial failing or pending tests for:
  - every Storefront path has at least one response schema,
  - no operation declares only `200`,
  - protected endpoints have security metadata,
  - public schemas do not include `BlazorShop.Domain.Entities`,
  - OpenAPI document parses/validates,
  - snapshot file can be compared deterministically.
- Add a deterministic snapshot writer/reader. Implementation should require explicit snapshot update, not silently rewrite snapshots on normal test runs.

Candidate files:

- New `BlazorShop.Tests/PresentationV2/CommerceNode/CommerceNodeStorefrontOpenApiContractTests.cs`
- `BlazorShop.Tests/BlazorShop.Tests.csproj`
- `BlazorShop.Tests/PresentationV2/CommerceNode/Snapshots/storefront-openapi.v1.json`

Exit criteria:

- Test harness can retrieve and parse Storefront Swagger.
- Tests clearly fail on missing response schema or missing security metadata.

Verification:

```powershell
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests"
```

Commit boundary:

- Commit contract test harness separately, even if some tests are initially marked to assert current gaps.
- Suggested message: `test: add storefront openapi contract harness`

### Phase 2 - Public Storefront DTO Foundation

Goal:

- Separate public Storefront contracts from shared/domain/admin DTOs where contract risk exists.

Tasks:

- Add Storefront public request DTOs:
  - `StorefrontRegisterRequest`
  - `StorefrontLoginRequest`
  - `StorefrontChangePasswordRequest`
  - `StorefrontUpdateProfileRequest`
  - `StorefrontProductCatalogQuery`
  - `StorefrontCartItemRequest`
  - `StorefrontOrderItemRequest`
  - `StorefrontNewsletterSubscribeRequest`
  - `StorefrontPayPalCaptureRequest`
- Add Storefront public response DTOs:
  - `StorefrontAuthResponse`
  - `StorefrontCatalogProductResponse`
  - `StorefrontProductResponse`
  - `StorefrontCategoryResponse`
  - `StorefrontCategoryTreeNodeResponse`
  - `StorefrontCategoryPageResponse`
  - `StorefrontOrderResponse`
  - `StorefrontOrderItemHistoryResponse`
  - `StorefrontPaymentMethodResponse`
  - `StorefrontProductRecommendationResponse`
  - `StorefrontMaintenanceResponse`
  - `StorefrontPayPalCaptureResponse`
- Add mapping helpers close to the API boundary or in Application if shared by Storefront V2.
- Avoid anonymous object responses in Storefront controllers.
- Keep DTOs flat and boring. Do not add a new module architecture.

Candidate locations:

- `BlazorShop.Application/CommerceNode/Storefront/` or `BlazorShop.Application/DTOs/Storefront/`
- `StorefrontScopedControllers.cs`

Exit criteria:

- Storefront controller public actions no longer expose `CreateOrderItem.UserId`, `ProductCatalogQuery.IsPublished`, anonymous maintenance objects, or domain entities.
- Build passes.

Verification:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~PresentationV2.Storefront|FullyQualifiedName~CommerceNode"
```

Commit boundary:

- Commit DTO/mapping foundation separately from Swagger metadata.
- Suggested message: `refactor: add public storefront api contracts`

### Phase 3 - Runtime Contract Hardening

Goal:

- Fix the risky request contracts and validation behavior before documenting them.

Tasks:

- Remove public `userId` from save-checkout request flow.
- Remove public `status` from order confirmation.
- Ensure Storefront product query cannot accept `IsPublished`.
- Convert PayPal capture to POST body contract and remove capture side effects from GET.
- Add runtime validation through `[ApiController]` DataAnnotations and existing FluentValidation where appropriate.
- Ensure request body parameters are annotated `[FromBody]`.
- Normalize validation failures into `CommerceNodeApiErrorResponse` with field errors.
- Add Storefront V2 client updates if any public route/query/body shape changes:
  - PayPal capture route,
  - sort values,
  - error parsing if non-2xx error shape changes.

Candidate files:

- `StorefrontScopedControllers.cs`
- Storefront public DTO files
- `StorefrontApiControllerBase.cs`
- `CommerceNodeApiResponse.cs`
- `CommerceNodeApiResponseWriter.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.cs`
- Relevant Storefront V2 tests

Exit criteria:

- Public requests cannot carry user identity, order status, or publish state.
- Quantity minimum and page size limit are enforced at runtime.
- PayPal capture mutation is not performed by GET.

Verification:

```powershell
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CartServiceTests|FullyQualifiedName~StorefrontV2ApiClientTests|FullyQualifiedName~CommerceNode"
```

Commit boundary:

- Commit behavior-level contract corrections together.
- Suggested message: `fix: harden storefront request contracts`

### Phase 4 - Swagger Metadata Completion

Goal:

- Make the Storefront OpenAPI document explicit, stable, and generated-client friendly.

Tasks:

- Enable Swashbuckle annotations if using `[SwaggerOperation]`.
- Add stable operation IDs and summaries to every Storefront action.
- Add `ProducesResponseType` for all success and error status codes.
- Add request body metadata for every POST body.
- Add schema ID customization for generic envelopes so generated clients get stable model names.
- Add string enum schema for Storefront sort values.
- Add Bearer JWT security scheme.
- Add refresh cookie security scheme for cookie-backed auth operations.
- Add an operation filter that applies security requirements to `[Authorize]` endpoints and cookie-only auth operations.
- Add an operation filter or convention that adds standard error responses where missing.
- Keep Storefront Swagger free of node key/secret and `X-Store-Key`.

Candidate files:

- `CommerceNodeSwaggerExtensions.cs`
- `StorefrontScopedControllers.cs`
- New Swagger operation/schema filters under `BlazorShop.CommerceNode.API/Swagger/`

Exit criteria:

- Every Storefront operation has:
  - `operationId`,
  - `summary`,
  - request schema if it accepts a body,
  - at least one 2xx response schema,
  - declared non-2xx error responses,
  - security metadata when protected.

Verification:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests"
```

Commit boundary:

- Commit OpenAPI metadata completion separately.
- Suggested message: `docs: complete storefront openapi metadata`

### Phase 5 - OpenAPI Contract Gates

Goal:

- Turn the user's contract quality requirements into permanent tests.

Required tests:

- Swagger has response schema for 100% of Storefront endpoints.
- Protected endpoints have security metadata.
- No Storefront endpoint declares only `200 OK`.
- No public Storefront schema references `BlazorShop.Domain.Entities`.
- OpenAPI document parses and validates.
- Request bodies for POST endpoints are marked required.
- Quantity schema has `minimum: 1`.
- PageSize schema has minimum and maximum.
- Sort enum is string-based and does not expose integer enum values.
- Storefront Swagger does not include node credentials or `X-Store-Key`.
- Storefront Swagger has no `api/internal/*`.

Generated-client smoke:

- Generate a C# client in-memory from the Storefront Swagger using a test-time generator package such as NSwag.
- The generated client must contain methods for all expected operation IDs.
- The generated models must not contain public `UserId`, public `IsPublished`, or numeric sort enum contracts.
- Generated output may be written to `TestResults` or a temporary directory; do not commit generated client source unless the team explicitly wants a checked-in SDK.

Snapshot:

- Commit `storefront-openapi.v1.json` snapshot after metadata is complete.
- Normalize volatile fields before snapshot compare.
- Snapshot updates require an explicit environment variable or test switch, for example `UPDATE_OPENAPI_SNAPSHOT=1`.

Candidate files:

- `CommerceNodeStorefrontOpenApiContractTests.cs`
- `BlazorShop.Tests/BlazorShop.Tests.csproj`
- Snapshot file under `BlazorShop.Tests/PresentationV2/CommerceNode/Snapshots/`

Exit criteria:

- Contract tests fail on any future endpoint with missing response schema, missing security metadata, missing error responses, entity schema leakage, or unvalidated public request fields.

Verification:

```powershell
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests"
```

Commit boundary:

- Commit contract gates and snapshot separately.
- Suggested message: `test: enforce storefront openapi contract quality`

### Phase 6 - Storefront V2 Compatibility Pass

Goal:

- Keep Storefront V2 working after contract changes.

Tasks:

- Update Storefront V2 client request models or query generation where needed.
- Confirm named sort enum values in outgoing URLs.
- Confirm Storefront V2 does not send `userId`, `status`, or `isPublished`.
- Confirm Storefront V2 handles standardized error response message extraction for form submit failures.
- Update Storefront V2 host/client tests.

Candidate files:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontAuthClient.cs`
- `BlazorShop.Tests/PresentationV2/Storefront/StorefrontV2ApiClientTests.cs`
- `BlazorShop.Tests/PresentationV2/Storefront/StorefrontV2AuthClientTests.cs`
- `BlazorShop.Tests/PresentationV2/Storefront/StorefrontV2HostSmokeTests.cs`

Exit criteria:

- Storefront V2 tests pass.
- Recording handlers assert no unsafe Storefront request fields are emitted.

Verification:

```powershell
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~PresentationV2.Storefront"
```

Commit boundary:

- Commit Storefront V2 compatibility separately if files are touched.
- Suggested message: `test: align storefront v2 with hardened api contracts`

### Phase 7 - QA Docs And Final Verification

Goal:

- Record the new contract guarantees and final verification commands.

Tasks:

- Update `QA-CommerceNode.todo.md` with OpenAPI contract checks.
- Update `QA-StorefrontV2.todo.md` if Storefront V2 request/route behavior changes.
- Add a short current-state note to `BlazorShop.CommerceNode.ApiSwaggerRescope.todo.md` only if the old document would mislead readers.
- Run focused build and tests.
- Optionally run HTTP smoke against a running CommerceNode API:
  - `GET /swagger/storefront/swagger.json` returns 200.
  - Storefront paths all start with `api/storefront/stores/{storeKey}/`.
  - A sampled protected operation shows Bearer security.
  - A sampled request body operation shows `requestBody.required=true`.

Verification:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests|FullyQualifiedName~PresentationV2.Storefront|FullyQualifiedName~CommerceNodeRouteBoundaryTests"
git status --short
git diff --stat
```

Commit boundary:

- Commit docs and final QA notes separately.
- Suggested message: `docs: record storefront api contract hardening`

## Test Coverage Map

```text
OpenAPI document
  -> parse/validate
  -> snapshot
  -> response schema coverage
  -> error response coverage
  -> operationId and summary coverage

Security metadata
  -> [Authorize] endpoints
  -> refresh-cookie endpoints
  -> anonymous endpoints have no accidental Bearer requirement

Public DTO safety
  -> no UserId in public request schemas
  -> no IsPublished in public query schemas
  -> no status on order confirm
  -> no domain entity schemas

Validation metadata
  -> quantity minimum
  -> page size min/max
  -> email/password/shipping address constraints
  -> request body required

Generated client smoke
  -> operation methods exist
  -> generated models compile or parse
  -> unsafe fields absent

Storefront V2 compatibility
  -> client URL/query/body generation
  -> auth submit parsing
  -> checkout/payment flow tests
```

## Failure Modes Registry

| Failure mode | Severity | Detection | Mitigation |
|---|---:|---|---|
| Swagger shows success responses as `object` or no schema. | High | OpenAPI contract tests. | Explicit `ProducesResponseType` and generic schema naming. |
| AI/generated client sends `userId`, `status`, or `isPublished`. | High | Schema absence tests and Storefront V2 request recording tests. | Public request DTOs and server-owned mapping. |
| Protected endpoints appear anonymous in Swagger. | High | Security metadata tests. | Operation filter maps `[Authorize]` and cookie auth to security requirements. |
| Sort enum still generated as integers. | Medium | Schema test for string enum values. | Storefront-specific string enum/query DTO. |
| Standardized error shape breaks Storefront V2 submit handling. | Medium | Storefront V2 auth/checkout tests. | Keep success envelope stable and update non-2xx parsing intentionally. |
| PayPal provider callback still expects GET capture. | Medium | Route/client tests and manual payment QA note. | Split browser callback from capture mutation; POST does the side effect. |
| Snapshot churn from non-deterministic Swagger output. | Medium | Snapshot test instability. | Normalize paths/schemas/order before compare. |
| New test-time generator adds brittle dependency. | Medium | CI restore/test failures. | Prefer mature NSwag test package; keep generation smoke local to tests and do not ship SDK. |
| Public response DTO removal breaks Storefront UI SEO/catalog assumptions. | Medium | Storefront V2 tests. | Map only fields currently used, remove risky fields in a compatibility-aware pass. |

## CEO Review Summary

This is the right next phase after removing `api/internal/*`: the runtime now has one Storefront route family, but the contract is not yet trustworthy enough for generated clients or AI agents. The highest product risk is shipping an OpenAPI document that looks official while still exposing unsafe or vague contracts. The selected approach fixes the public contract rather than only decorating existing actions.

Dream state:

```text
CURRENT
  Storefront Swagger exists but has incomplete metadata and unsafe request shapes.

THIS PLAN
  Storefront Swagger becomes explicit, validated, snapshot-tested, and generator-smoke-tested.

12-MONTH IDEAL
  Storefront API is versioned, SDK-ready, contract-tested in CI, and safe for AI agents to consume without reading source code.
```

CEO scope decision:

- Hold scope on Storefront API contract hardening.
- Defer full SDK packaging, API versioning, public playground, and broad Commerce Admin contract work.

## Engineering Review Summary

Architecture is sound if the contract layer stays thin:

```text
Controller action
  -> public Storefront request DTO
  -> validation/model state
  -> mapper to existing service DTO/domain query
  -> existing application service
  -> mapper to public Storefront response DTO
  -> typed success/error envelope
  -> OpenAPI metadata/test gate
```

Critical engineering decisions:

- Add public DTOs only where current shared DTOs leak unsafe public fields.
- Keep application services as the business boundary; do not rewrite service internals just for Swagger.
- Use tests to enforce OpenAPI quality instead of relying on reviewer discipline.
- Apply auth metadata from attributes/conventions so protected endpoints do not depend on manual Swagger edits only.

## DX Review Summary

Primary developer persona:

- An internal or partner developer, plus AI coding agents, consuming `/swagger/storefront/swagger.json` to generate Storefront clients or understand Storefront API behavior.

Developer journey target:

| Stage | Target experience |
|---|---|
| Discover | Open Swagger UI and choose `Storefront API`. |
| Inspect | See stable operation IDs, short summaries, auth requirements, and typed request/response models. |
| Generate | Run a generator and get named models, not `object` or domain entities. |
| Use | Submit named sort values, validated request bodies, and correct auth. |
| Debug | Non-2xx response has code, message, trace ID, and field errors. |
| Upgrade | Snapshot diff shows contract changes before implementation lands. |

DX target:

- Current estimated DX score: 4/10.
- Target after this plan: 8/10.
- Time to generate a usable client from Swagger: under 5 minutes after API is running or test fixture emits Swagger.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|---|---|---|---|---|---|
| 1 | Scope | Harden Storefront API contract before adding more Storefront features. | Auto-decided | Complete contract first | The route migration is complete; the next bottleneck is machine-readable contract quality. | Continue feature work with incomplete Swagger. |
| 2 | Error shape | Keep 2xx success envelope stable and add explicit non-2xx error DTO. | Auto-decided | Minimize regression | Storefront V2 already parses success envelopes; errors need standardization most. | Replace all envelopes with ProblemDetails in one pass. |
| 3 | DTO strategy | Add Storefront public DTOs where current DTOs leak unsafe fields. | Auto-decided | Explicit over clever | This fixes public schema without service rewrite. | Attribute-only decoration of unsafe DTOs. |
| 4 | PayPal | Move capture side effect to POST. | Auto-decided | HTTP semantics | GET mutation is unsafe and misleading in generated clients. | Keep GET capture and only annotate it. |
| 5 | Sort enum | Use named string values for public sort contract. | Auto-decided | DX consistency | Generated clients should not need numeric enum guesses. | Keep numeric enum schema. |
| 6 | Contract tests | Add OpenAPI parser/snapshot/generated-client smoke tests. | Auto-decided | Make regressions visible | Metadata quality must be enforced mechanically. | Manual Swagger review only. |
| 7 | SDK | Generate client only as smoke test, not a packaged SDK. | Taste decision, recommended | Hold scope | The objective is contract foundation; SDK packaging is a later product decision. | Add full checked-in SDK package now. |

## Final Approval Gate

Recommendation: implement Approach B, Storefront contract layer with targeted mappings, in the phase order above.

Taste choice:

- Recommended: generated-client smoke only in tests for this phase.
- Alternative: create and commit a full generated SDK project now. This is useful later but expands scope and introduces package/versioning decisions before the contract is stable.

No user challenge remains. The user-requested risky contract fixes are all included.

## GSTACK REVIEW REPORT

| Run | Status | Findings | Resolution |
|---|---|---:|---|
| CEO | Complete | 2 | Hold scope; harden contract before new Storefront features; defer SDK packaging. |
| Design | Skipped | 0 | No end-user UI scope; Swagger/DX covered by DX review. |
| Engineering | Complete | 6 | Add public DTOs, standard errors, metadata filters, contract tests, generated-client smoke, snapshot. |
| DX | Complete | 5 | Stabilize operation IDs, schemas, validation metadata, security metadata, and error model for generated clients/AI agents. |

VERDICT: APPROVED FOR PHASED IMPLEMENTATION.

NO UNRESOLVED DECISIONS
