# BlazorShop Store Lifecycle Autoplan

Generated: 2026-07-15
Scope: Store active/inactive, closed/maintenance behavior, not-ready handling, manager display order, and store company/contact information for the active V2 architecture.

## 1. Muc tieu

Build store lifecycle behavior without breaking the current V2 boundaries:

```text
ControlPlane.Web
  -> ControlPlane.API
      -> CommerceNode.API

Storefront.V2
  -> CommerceNode Storefront APIs:
      api/storefront/stores/{storeKey}/*
```

Approved features:

- Store active/inactive.
- Store closed/maintenance page.
- Redirect or clear unavailable message when a store is not ready.
- Admin can still manage a store while public storefront is closed.
- Store display order in manager.
- Store company/contact information with a clearer single management source.

## 2. Ket luan sau autoplan review

Decision: **PROCEED IN PHASES**.

This work is necessary, but the first implementation step must fix the existing Control Plane status mismatch. Current deployment status code can write `provisioning` into `StoreRegistry.Status`, while the Control Plane DB constraint only allows `active`, `disabled`, and `archived`. That means deployment status updates can fail before any new lifecycle UI is added.

Primary recommendation:

- Treat `CommerceStore` as the runtime source for public store lifecycle and public store profile.
- Keep `StoreRegistry` as Control Plane platform registry: node assignment, deployment status, manager routing, and domains.
- Do not let Storefront V2 call Control Plane.
- Do not relax active-store filtering globally. Non-active store reads must be explicit readiness/admin flows.

## 3. Hien trang da xac minh

| Area | Current state | Impact |
| --- | --- | --- |
| Runtime store model | `CommerceStore` already has `Status`, `DisplayOrder`, `SupportEmail`, `SupportPhone`, `MaintenanceModeEnabled`, and `MaintenanceMessage`. | The runtime model already supports most lifecycle fields. |
| Runtime status values | `CommerceStoreStatuses` has `active`, `disabled`, and `archived`. | Active/inactive can reuse existing concepts. |
| Store resolution | `CommerceStoreDomainResolver.ActiveStoreQuery()` only resolves `Status == active`. | Public catalog/cart/order are already protected from disabled stores. |
| Storefront unavailable UX | `StorefrontCurrentStoreMiddleware` currently returns plain text `404` or `503`. | Needs a real maintenance/unavailable page for HTML traffic. |
| Control Plane registry | `StoreRegistry.Status` is constrained to `active`, `disabled`, and `archived`. | Missing `provisioning` causes DB update risk. |
| Deployment status mapping | `ControlPlaneStoreDeploymentService` maps queued/running/default task state to `provisioning`. | Must be fixed before lifecycle work. |
| Store display order | Runtime `CommerceStore.DisplayOrder` exists. | Prefer reusing it unless Control Plane needs separate platform ordering. |
| Company/contact data | Store contact data is split across runtime store/settings/SEO concepts. | Needs consolidation while the project is still in dev mode. |

## 4. Architecture decisions

### 4.1 Store lifecycle source of truth

Use two separate responsibilities:

- `ControlPlane.StoreRegistry`: platform registry and deployment lifecycle.
- `CommerceNode.CommerceStore`: runtime storefront lifecycle and public store profile.

Do not merge the DbContexts. Cross-boundary behavior must go through APIs.

### 4.2 Active/inactive behavior

Use `CommerceStore.Status` for public runtime availability:

- `active`: storefront can serve catalog, cart, checkout, SEO, and normal pages.
- `disabled`: storefront is closed/inactive and should not serve shopping flows.
- `archived`: store is not publicly available and should not be edited through normal runtime lifecycle UI.
- `provisioning`: only add to runtime if Commerce Node needs to represent a not-ready store row before activation.

Admin access must remain available through Control Plane manager APIs, not through public Storefront bypasses.

### 4.3 Maintenance and closed page

Add a real Storefront V2 page:

```text
/maintenance
```

Behavior:

- HTML GET + maintenance store: redirect or re-execute to `/maintenance?reason=maintenance`.
- HTML GET + inactive store: redirect or re-execute to `/maintenance?reason=closed`.
- HTML GET + not-ready/provisioning: redirect or re-execute to `/maintenance?reason=not-ready`.
- Non-HTML/API/static requests: return the proper status code without browser redirect.

The middleware must skip `/maintenance` itself to avoid redirect loops.

### 4.4 Store not ready semantics

Storefront unavailable states should be explicit:

| State | Meaning | Public behavior |
| --- | --- | --- |
| `maintenance` | Store exists and is temporarily under maintenance. | Show maintenance page, return `503` for non-HTML. |
| `closed` | Store exists but is inactive/disabled. | Show closed page state, block commerce flows. |
| `not-ready` | Store deployment/provisioning is not complete. | Show not-ready page state. |
| `not-found` | Store key/domain is unknown. | Keep safe 404 behavior. |

Do not make catalog/cart/order services query inactive stores just to display unavailable state. Use a narrow readiness/current-store path.

### 4.5 Display order

Use `CommerceStore.DisplayOrder` for runtime/store manager ordering.

If Control Plane already has a display-order field in the active branch, reuse it for platform manager ordering. If it does not, only add a Control Plane display-order field if the ordering is truly platform-specific. Avoid two editable display-order fields with the same meaning.

### 4.6 Company/contact information

Recommendation: consolidate management into `CommerceStore`.

Add or standardize store profile fields in the runtime store model:

- `CompanyName`
- `CompanyEmail`
- `CompanyPhone`
- `CompanyAddress`
- `SupportEmail`
- `SupportPhone`

Keep company contact and support contact as separate fields because they serve different business purposes, but manage them in one store profile UI.

SEO structured data should read company/contact information from the runtime store first. Existing SEO/settings fields can be migrated or deprecated to avoid long-term duplicate sources.

## 5. Phase plan

### Phase 0 - Fix Control Plane status mismatch

Goal: remove the known DB failure path before adding lifecycle UI.

Tasks:

- Add `provisioning` to the Control Plane store status model and DB constraint, or change deployment mapping so it never writes `provisioning`.
- Recommended choice: add `provisioning`, because the UI and deployment flow already need a not-ready state.
- Add a Control Plane EF migration for the status constraint.
- Replace scattered status string literals with constants where practical.
- Add tests for deployment queued/running status updates.

Exit criteria:

- Deployment status update can persist `provisioning` without DB error.
- Store list filtering for `provisioning` works consistently.

### Phase 1 - Commerce Node lifecycle admin API

Goal: expose explicit runtime lifecycle commands through the correct boundary.

Tasks:

- Add or standardize endpoints for:
  - activate store
  - deactivate store
  - update maintenance mode/message
  - update display order
  - update store profile/contact information
- Keep request/response DTOs explicit.
- Add OpenAPI metadata: operationId, summary, error responses, security metadata.
- Do not expose domain entities directly.

Exit criteria:

- Commerce Node admin/control API can manage lifecycle without touching legacy projects.
- Storefront public APIs remain store-scoped by route.

### Phase 2 - Control Plane API gateway and manager UI

Goal: let admins manage lifecycle while the public storefront is closed.

Tasks:

- Add Control Plane API gateway endpoints that call Commerce Node API.
- Update Control Plane Web store manager with:
  - active/inactive control
  - maintenance toggle and message
  - display order field
  - company/contact form
  - runtime status display
- Keep Control Plane Web UI-only. It must not call Commerce Node API directly.

Exit criteria:

- Admin can close/maintain/reactivate a store from manager.
- Admin can edit profile/contact data without opening the storefront.

### Phase 3 - Storefront maintenance and unavailable page

Goal: replace plain text unavailable responses with a user-facing page.

Tasks:

- Add Storefront V2 `/maintenance` page.
- Reuse existing unavailable-state UI where it fits, but make the page store-aware.
- Update `StorefrontCurrentStoreMiddleware`:
  - skip static assets and `/maintenance`
  - redirect/re-execute HTML requests
  - return status payload for non-HTML requests
  - include noindex headers
- Ensure unavailable states do not leak data from another store.

Exit criteria:

- Maintenance store shows maintenance page.
- Inactive store shows closed state.
- Not-ready store shows not-ready state.
- Unknown store remains a safe not-found flow.

### Phase 4 - Company/contact consolidation

Goal: make store profile data easy to manage and avoid duplicate sources.

Tasks:

- Add missing company/contact fields to `CommerceStore` if they do not exist yet.
- Create Commerce Node migration.
- Migrate existing dev data from SEO/settings fields into `CommerceStore` where possible.
- Update SEO structured data and Storefront display to read from current store profile first.
- Deprecate duplicate editing fields outside the store profile.

Exit criteria:

- One admin screen manages store profile/contact.
- Storefront and SEO use the same runtime store profile source.

### Phase 5 - QA and documentation updates

Goal: lock behavior into project checklists.

Tasks:

- Update:
  - `QA-ControlPlane.todo.md`
  - `QA-CommerceNode.todo.md`
  - `QA-StorefrontV2.todo.md`
  - `BlazorShop.StoreExpansion.todo.md`
- Add or update focused tests for:
  - provisioning status persistence
  - active/inactive resolver behavior
  - maintenance redirect/re-execute
  - redirect loop prevention
  - admin lifecycle update while public store is closed
  - display order sorting
  - company/contact migration and display

Exit criteria:

- Focused tests pass.
- QA docs reflect the final lifecycle behavior.

## 6. Non-goals

- Do not extend legacy `BlazorShop.Presentation/*` projects.
- Do not add lifecycle fields to `AppDbContext`.
- Do not make Storefront V2 depend on Control Plane API.
- Do not add `api/internal/*` compatibility routes.
- Do not bypass storefront closure with public admin flags.
- Do not make all store queries include inactive stores.
- Do not keep company/contact editable in multiple long-term locations.

## 7. Risk controls

| Risk | Control |
| --- | --- |
| DB status update fails | Fix Control Plane status constraint in Phase 0. |
| Storefront redirect loop | Exclude `/maintenance` and static paths in middleware. |
| Admin cannot access closed store | Keep management through Control Plane API gateway. |
| Data leak from inactive stores | Keep catalog/cart/order on active-store resolution only. |
| Duplicate company/contact source | Consolidate editable profile fields into `CommerceStore`. |
| API contract drift | Add operationId, DTOs, errors, security metadata, and contract tests. |

## 8. Recommended implementation order

1. Phase 0 - Control Plane `provisioning` status fix.
2. Phase 1 - Commerce Node lifecycle admin API.
3. Phase 3 - Storefront maintenance/unavailable page.
4. Phase 2 - Control Plane manager UI.
5. Phase 4 - Company/contact consolidation.
6. Phase 5 - QA and documentation updates.

This order removes the known database failure first, then makes backend behavior reliable before adding UI, and finishes by consolidating duplicated profile data while the project is still in dev mode.
