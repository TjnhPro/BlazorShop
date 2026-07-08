# BlazorShop Control Plane API Response Pattern Todo

Goal: standardize every `BlazorShop.ControlPlane.API` JSON response as:

```json
{
  "success": true,
  "message": "string",
  "data": {}
}
```

The API owns validation, permission, domain, and infrastructure error messages. `BlazorShop.ControlPlane.Web` should read `message` and show it without re-deriving business failure reasons in Razor pages.

Important decision: keep meaningful HTTP status codes at the API boundary, but handle them only inside the Web client/service layer. Razor UI components must not branch on raw HTTP status codes; they only consume `Success`, `Message`, and `Data`.

## Autoplan Summary

### Product Decision

- Keep this change scoped to Control Plane only. Do not retrofit legacy Commerce or Storefront APIs in this phase.
- Keep HTTP status codes meaningful. The envelope standardizes the body, but `400`, `401`, `403`, `404`, `409`, `429`, and `500` must remain correct for browser tools, auth flows, QA, and future API clients.
- Do not make a `200 OK` only API. `success=false` is the UI/domain contract; HTTP status remains the transport and infrastructure contract.
- Keep sensitive detail out of API messages. Internal exceptions should return a generic message plus a correlation id in `data` when useful.

### Engineering Decision

- Add a Control Plane-specific response envelope and response helpers instead of scattering anonymous `{ message }` payloads across controllers.
- Prefer API boundary mapping over pushing response-envelope concerns into Application services.
- Migrate clients feature by feature to avoid breaking all pages at once.
- Centralize all HTTP status handling in `ControlPlaneApiClient` and feature client services.
- Do not make Razor pages parse HTTP status codes. Pages consume typed client results with `Success`, `Message`, and `Data`.

### DX Decision

- Use one API response reader in the Web project so each client does not need custom `ResolveErrorMessageAsync`.
- Web client infrastructure may inspect status codes for cross-cutting behavior such as token refresh, login redirect, malformed responses, network failures, and retry decisions.
- Keep fallback messages in client infrastructure only for network failures or malformed server responses. Domain and permission messages come from API.
- Update QA checklist with response-shape tests so future endpoints cannot drift back to ad hoc payloads.

## Target Contract

### Success

```json
{
  "success": true,
  "message": "Loaded nodes.",
  "data": {
    "items": [],
    "nextCursor": null
  }
}
```

### Failure

HTTP status example: `403 Forbidden`

```json
{
  "success": false,
  "message": "Your Control Plane account does not have permission for User Management.",
  "data": null
}
```

### Validation Failure

HTTP status example: `400 Bad Request`

```json
{
  "success": false,
  "message": "Validation failed.",
  "data": {
    "errors": {
      "email": ["Email is required."]
    }
  }
}
```

### Exception Failure

HTTP status example: `500 Internal Server Error`

```json
{
  "success": false,
  "message": "An unexpected Control Plane error occurred.",
  "data": {
    "correlationId": "..."
  }
}
```

## Scope

### In Scope

- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API`
- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web`
- Control Plane client services under `Services/*`
- Control Plane auth/session handling
- Control Plane QA checklist updates

### Out Of Scope

- Legacy `BlazorShop.Presentation`
- Commerce/Storefront API contracts
- Database schema changes
- Changing permission rules
- Rewriting Application service result types unless a small adapter is needed

## Phase 0 - Baseline And Inventory

- [x] Record current API response shapes for:
  - [x] Auth
  - [x] Dashboard
  - [x] Nodes
  - [x] Credentials
  - [x] Stores
  - [x] Health
  - [x] Actions
  - [x] Users
  - [x] System info
- [x] Add a short endpoint inventory table to this file before implementation.
- [x] Run current build:
  - [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore`
  - [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj --no-restore`
- [x] Confirm current QA DB can still seed admin/user accounts. 2026-07-08 baseline: previous clean QA runs seeded admin/user through Control Plane DB; no schema changes in this phase.

Commit: `docs(control-plane): inventory api response shapes`

## Phase 1 - API Envelope Infrastructure

### API Contract

- [x] Add `ControlPlaneApiResponse<TData>`:
  - [x] `bool Success`
  - [x] `string Message`
  - [x] `TData? Data`
- [x] Add optional non-generic factory helpers:
  - [x] `Ok(data, message)`
  - [x] `Created(data, message)`
  - [x] `Failed(message, statusCode, data = null)`
  - [x] `ValidationFailed(errors)`
- [x] Keep JSON property names as camelCase: `success`, `message`, `data`.

Recommended location:

- API-only helper: `BlazorShop.ControlPlane.API/Responses/ControlPlaneApiResponse.cs`
- API-only controller helpers: `BlazorShop.ControlPlane.API/Responses/ControlPlaneApiResponseFactory.cs`

Rationale: the envelope is a presentation/API boundary concern. Application services should continue to return business operation results.

### Controller Helper

- [x] Add a shared helper or base controller to remove duplicate mapping:
  - [x] Map success result to `Ok(ApiResponse.Success(data, message))`
  - [x] Map created result to `CreatedAtAction(..., ApiResponse.Success(data, message))`
  - [x] Map `NotFound` to `success=false`
  - [x] Map `Conflict` to `success=false`
  - [x] Map validation to `success=false`
- [x] Do not hide HTTP status code behind `success=false`.

### Global API Failures

- [x] Configure `[ApiController]` invalid model state responses to return the envelope.
- [x] Configure exception handling to return the envelope with a generic safe message.
- [x] Configure `401` and `403` responses to return the envelope body.
- [x] Configure rate limit `429` response to return the envelope body.
- [x] Keep correlation id available in error `data`.

Commit: `feat(control-plane): add api response envelope infrastructure`

## Phase 2 - Web Envelope Reader Infrastructure

### Client Contract

- [x] Add `ControlPlaneApiEnvelope<TData>` in the Web project:
  - [x] `bool Success`
  - [x] `string Message`
  - [x] `TData? Data`
- [x] Add `ControlPlaneClientResult<TData>` for page-facing client methods:
  - [x] `bool Success`
  - [x] `string Message`
  - [x] `TData? Data`
  - [x] `HttpStatusCode? StatusCode`
- [x] Add one shared reader:
  - [x] `ReadEnvelopeAsync<TData>(HttpResponseMessage response, string fallbackMessage)`
  - [x] `GetAsync<TData>(route, fallbackMessage)`
  - [x] `PostAsync<TRequest,TData>(route, request, fallbackMessage)`
  - [x] `PutAsync<TRequest,TData>(route, request, fallbackMessage)`
  - [x] `DeleteAsync<TData>(route, fallbackMessage)`

Recommended location:

- `BlazorShop.ControlPlane.Web/Services/Common/ControlPlaneApiEnvelope.cs`
- `BlazorShop.ControlPlane.Web/Services/Common/ControlPlaneApiClient.cs`

### UI Rule

- [x] Razor pages show `result.Message`.
- [x] Razor pages check only `result.Success` and `result.Data`.
- [x] Razor pages do not inspect raw HTTP status or parse JSON errors.
- [x] Razor pages do not know whether a business failure came from `400`, `403`, `404`, or `409`.
- [x] `ControlPlaneApiClient` is the only layer that reads HTTP status for generic behavior.
- [x] Feature clients may expose `StatusCode` for diagnostics, but pages should not use it for business UI branching.
- [x] Network failure fallback is allowed in `ControlPlaneApiClient`, not pages.

Commit: `feat(control-plane): add web api envelope reader`

## Phase 3 - Migrate Low-Risk Read Endpoints

Migrate read-only or simple response endpoints first.

- [x] `SystemController.GetInfo`
- [x] `ControlPlaneDashboardController.Summary`
- [x] `ControlPlaneNodesController.List`
- [x] `ControlPlaneStoresController.List`
- [x] `ControlPlaneHealthController.List`
- [x] `ControlPlaneActionsController.List`
- [x] `ControlPlaneUsersController.ListRoles`
- [x] `ControlPlaneUsersController.ListPermissions`

For each endpoint:

- [x] API returns `{ success=true, message, data }`.
- [x] Web client reads `data`.
- [x] Page behavior is unchanged.
- [ ] Direct API smoke verifies the envelope.

Commit: `feat(control-plane): wrap read api responses`

## Phase 4 - Migrate Auth Responses

Auth must be migrated carefully because login, refresh, logout, route guard, and session sync depend on it.

- [x] `POST /api/control-plane/auth/login`
  - [x] Success: `data` contains existing login response/token fields.
  - [x] Failure: `success=false`, safe `message`, no account existence leak.
- [x] `POST /api/control-plane/auth/refresh-token`
  - [x] No-session startup check returns envelope and does not create console noise.
  - [x] Expired/invalid access token is handled by Web client infrastructure through `401` and refresh retry.
  - [x] Invalid refresh returns failure envelope and clears cookie.
- [x] `POST /api/control-plane/auth/logout`
  - [x] Success envelope with `data=null`.
- [x] `GET /api/control-plane/auth/me`
  - [x] Success envelope with profile data.
  - [x] Inactive profile returns `403` with envelope.

Web changes:

- [x] `ControlPlaneAuthenticationService` reads envelope.
- [x] Login page displays API `message`.
- [x] Route guard behavior remains unchanged.
- [x] Session refresh no longer requires custom failure parsing.

Commit: `feat(control-plane): wrap auth api responses`

## Phase 5 - Migrate Mutation Endpoints

Migrate business mutation endpoints after auth/read routes are stable.

### Nodes

- [ ] `GET /nodes/{publicId}`
- [ ] `POST /nodes`
- [ ] `PUT /nodes/{publicId}`
- [ ] `POST /nodes/{publicId}/disable`

### Credentials

- [ ] `GET /nodes/{nodePublicId}/credentials`
- [ ] `POST /nodes/{nodePublicId}/credentials`
- [ ] `POST /nodes/{nodePublicId}/credentials/{keyId}/revoke`
- [ ] `POST /nodes/{nodePublicId}/credentials/{keyId}/rotate`

### Stores

- [ ] `GET /stores/{publicId}`
- [ ] `POST /stores`
- [ ] `PUT /stores/{publicId}`
- [ ] `POST /stores/{publicId}/archive`
- [ ] `POST /stores/{publicId}/domains`
- [ ] `POST /stores/{publicId}/domains/{domainId}/verify`
- [ ] `POST /stores/{publicId}/domains/{domainId}/disable`

### Health

- [ ] `GET /health/{nodePublicId}`
- [ ] `POST /health/{nodePublicId}/probe`

### Actions

- [ ] `GET /actions/{publicId}`
- [ ] `POST /actions`
- [ ] `POST /actions/{publicId}/attempts`
- [ ] `POST /actions/{publicId}/cancel`

For each mutation:

- [ ] API maps service `Message` into envelope `message`.
- [ ] API maps service `Payload` into envelope `data`.
- [ ] Web client returns `ControlPlaneClientResult<TData>`.
- [ ] Page displays `result.Message` only.
- [ ] Audit behavior remains unchanged.

Commit: `feat(control-plane): wrap mutation api responses`

## Phase 6 - Migrate User Management Endpoints

User Management has the most permission and conflict rules, so keep it as a separate phase.

- [ ] `GET /users/{publicId}`
- [ ] `POST /users`
- [ ] `PUT /users/{publicId}`
- [ ] `POST /users/{publicId}/disable`
- [ ] `POST /users/{publicId}/enable`
- [ ] `POST /users/{publicId}/roles`
- [ ] `DELETE /users/{publicId}/roles/{roleKey}`
- [ ] `POST /users/{publicId}/permissions`
- [ ] `DELETE /users/{publicId}/permissions/{permissionKey}`

Acceptance:

- [ ] Duplicate email response is `409` with `success=false`.
- [ ] Last platform owner response is `409` with `success=false`.
- [ ] Permission denial is `403` with `success=false`.
- [ ] Disabled login is still blocked.
- [ ] Web client reads status for generic handling but User page only consumes `Success`, `Message`, and `Data`.
- [ ] User page uses only API messages for visible failure text.

Commit: `feat(control-plane): wrap user management api responses`

## Phase 7 - Remove Old Client Error Parsing

- [ ] Delete per-client `ResolveErrorMessageAsync` methods from:
  - [ ] Node client
  - [ ] Store client
  - [ ] Credential client
  - [ ] Health client
  - [ ] Action client
  - [ ] User client
  - [ ] Authentication service
- [ ] Remove duplicate local mutation result records where `ControlPlaneClientResult<T>` can replace them.
- [ ] Keep feature-specific result types only where they add real domain meaning.
- [ ] Ensure pages do not contain hard-coded permission/business error messages that duplicate API logic.

Commit: `refactor(control-plane): centralize web api error handling`

## Phase 8 - Tests And QA

### API Contract Tests

- [ ] Add integration tests or scripted smoke tests for every controller group:
  - [ ] success envelope contains `success=true`, `message`, `data`
  - [ ] failure envelope contains `success=false`, `message`, `data`
  - [ ] validation failures include error details under `data.errors`
  - [ ] unauthorized returns envelope
  - [ ] forbidden returns envelope
  - [ ] conflict returns envelope
  - [ ] not found returns envelope

### Browser QA

- [ ] Login wrong password shows API `message`.
- [ ] Auditor forbidden action shows API `message`.
- [ ] User Management conflict shows API `message`.
- [ ] Node/Store validation shows API `message`.
- [ ] Browser console has no unexpected errors.

### Checklist

- [ ] Update `QA-ControlPlane.todo.md` with an API Response Pattern section.
- [ ] Mark each migrated feature after live QA.

Commit: `test(control-plane): verify api response envelope`

## Endpoint Inventory Template

Baseline recorded before implementation.

| Area | Endpoint | Current success body | Current failure body | Target body | Notes |
| --- | --- | --- | --- | --- | --- |
| Auth | `POST /api/control-plane/auth/login` | `LoginResponse` | `LoginResponse` or `{ message }` | `ApiResponse<LoginResponse>` | Keep safe auth message. |
| Auth | `POST /api/control-plane/auth/refresh-token` | `LoginResponse` | `LoginResponse` or framework/default | `ApiResponse<LoginResponse>` | Preserve quiet no-session startup behavior. |
| Auth | `POST /api/control-plane/auth/logout` | `ServiceResponse` | `ServiceResponse` or framework/default | `ApiResponse<ServiceResponse>` | Must clear refresh cookie. |
| Auth | `GET /api/control-plane/auth/me` | `ControlPlaneProfileResponse` | framework/default | `ApiResponse<ControlPlaneProfileResponse>` | Inactive profile remains forbidden. |
| Dashboard | `GET /api/control-plane/dashboard/summary` | `DashboardSummary` | framework/default | `ApiResponse<DashboardSummary>` | Read-only low risk. |
| System | `GET /api/control-plane/system/info` | `SystemInfoResponse` | framework/default | `ApiResponse<SystemInfoResponse>` | Anonymous endpoint. |
| Nodes | `GET /api/control-plane/nodes` | `NodeListResponse` | framework/default | `ApiResponse<NodeListResponse>` | Read list. |
| Nodes | `GET /api/control-plane/nodes/{publicId}` | `NodeDetail` | `{ message }` | `ApiResponse<NodeDetail>` | Preserve `404`. |
| Nodes | `POST /api/control-plane/nodes` | `NodeDetail` | `{ message }` | `ApiResponse<NodeDetail>` | Preserve `201 Created`. |
| Nodes | `PUT /api/control-plane/nodes/{publicId}` | `NodeDetail` | `{ message }` | `ApiResponse<NodeDetail>` | Preserve validation/conflict status. |
| Nodes | `POST /api/control-plane/nodes/{publicId}/disable` | `NodeDetail` | `{ message }` | `ApiResponse<NodeDetail>` | Preserve `404`. |
| Credentials | `GET /api/control-plane/nodes/{nodePublicId}/credentials` | `CredentialListResponse` | `{ message }` | `ApiResponse<CredentialListResponse>` | Requires rotate permission today. |
| Credentials | `POST /api/control-plane/nodes/{nodePublicId}/credentials` | `CredentialSecretResult` | `{ message }` | `ApiResponse<CredentialSecretResult>` | Raw secret remains data only once. |
| Credentials | `POST /api/control-plane/nodes/{nodePublicId}/credentials/{keyId}/revoke` | `CredentialSummary` | `{ message }` | `ApiResponse<CredentialSummary>` | Preserve audit. |
| Credentials | `POST /api/control-plane/nodes/{nodePublicId}/credentials/{keyId}/rotate` | `CredentialSecretResult` | `{ message }` | `ApiResponse<CredentialSecretResult>` | Preserve audit. |
| Stores | `GET /api/control-plane/stores` | `StoreListResponse` | framework/default | `ApiResponse<StoreListResponse>` | Read list. |
| Stores | `GET /api/control-plane/stores/{publicId}` | `StoreDetail` | `{ message }` | `ApiResponse<StoreDetail>` | Preserve `404`. |
| Stores | store mutation endpoints | `StoreDetail` | `{ message }` | `ApiResponse<StoreDetail>` | Preserve audit and `201` on create. |
| Health | `GET /api/control-plane/health/nodes` | `HealthListResponse` | framework/default | `ApiResponse<HealthListResponse>` | Read list. |
| Health | `GET /api/control-plane/health/nodes/{nodePublicId}` | `HealthDetail` | `{ message }` | `ApiResponse<HealthDetail>` | Preserve `404`. |
| Health | `POST /api/control-plane/health/nodes/{nodePublicId}/probe` | `ProbeResult` | `{ message }` | `ApiResponse<ProbeResult>` | Preserve audit. |
| Actions | `GET /api/control-plane/actions` | `ActionListResponse` | framework/default | `ApiResponse<ActionListResponse>` | Read list. |
| Actions | action mutation endpoints | `ActionDetail` | `{ message }` | `ApiResponse<ActionDetail>` | Preserve `201` on enqueue when new. |
| Users | `GET /api/control-plane/users` | `UserListResponse` | framework/default | `ApiResponse<UserListResponse>` | Read list. |
| Users | `GET /api/control-plane/users/{publicId}` | `UserDetail` | `{ message }` | `ApiResponse<UserDetail>` | Preserve `404`. |
| Users | `GET /api/control-plane/users/roles` | `RoleCatalogResponse` | framework/default | `ApiResponse<RoleCatalogResponse>` | Read catalog. |
| Users | `GET /api/control-plane/users/permissions` | `PermissionCatalogResponse` | framework/default | `ApiResponse<PermissionCatalogResponse>` | Read catalog. |
| Users | `POST /api/control-plane/users` | `CreateUserResponse` | `{ message }` | `ApiResponse<CreateUserResponse>` | Preserve `409` duplicate email. |
| Users | user mutation endpoints | `UserDetail` | `{ message }` | `ApiResponse<UserDetail>` | Preserve permission and last-owner checks. |

## Acceptance Criteria

- [ ] Every Control Plane API JSON response has `success`, `message`, and `data`.
- [ ] Error messages shown in UI originate from API response `message`.
- [ ] UI clients do not duplicate domain/permission error decisions.
- [ ] HTTP status codes remain semantically correct.
- [ ] Raw HTTP status handling is centralized in Web services/common client.
- [ ] Razor pages do not inspect raw HTTP status codes.
- [ ] Auth, route guard, refresh, and logout still pass QA.
- [ ] User Management QA still passes on a clean database.
- [ ] Audit logging behavior is unchanged.
- [ ] API and Web builds pass.

## Risks

- Auth refresh currently has a silent no-session path. Changing its `success` semantics can break startup session sync if not migrated deliberately.
- Token refresh is cross-cutting. If `401` handling is duplicated in feature clients or pages, session behavior will drift.
- `CreatedAtAction` responses must preserve `201` and route values while wrapping body.
- `401/403` bodies may require middleware or authorization result handling; controller helpers alone will not catch all auth failures.
- Existing Web Shared `ServiceResponse<T>` uses `Payload`, not `Data`. Reusing it directly would violate the requested response shape.
- Returning envelope for framework-level model validation requires MVC options configuration, not only controller edits.

## Open Decisions

- [x] Keep meaningful HTTP status codes. Web client/service layer can inspect status; Razor UI only reads `Success`, `Message`, and `Data`.
- [ ] Should no active session on refresh-token startup check be `success=false` with HTTP `200`, or HTTP `401`? Recommended for current WASM startup: keep HTTP `200` with `success=false` and message `No active session.` to avoid noisy first-load errors.
- [ ] Should validation details in `data.errors` be shown field-by-field later, or only summarized through `message` in this phase? Recommended: API returns details now, UI shows summary now.
- [ ] Should a small `BlazorShop.ControlPlane.Contracts` project be introduced later for shared DTO contracts? Recommended: not required for this phase; keep envelope reader in Web and response factory in API unless DTO drift becomes painful.
