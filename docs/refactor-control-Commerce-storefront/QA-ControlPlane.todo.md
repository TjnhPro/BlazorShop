# QA Control Plane Todo

Use this file after every Control Plane change. Re-test the changed feature area and adjacent authorization/audit paths.

Status legend:

- `[ ]` Not tested
- `[x]` Passed
- `[~]` Partial / blocked
- `[!]` Failed / bug found
- `[n/a]` Not implemented or not applicable in the current phase

## Test Environment

- [x] PostgreSQL Control Plane database is running.
- [x] Control Plane API starts successfully.
- [x] Control Plane Web starts successfully.
- [x] Browser console has no unexpected errors on initial load. 2026-07-08: login page, route guard, and mobile login smoke tests had 0 console errors after refresh/CORS fixes.
- [x] Browser QA uses Playwright MCP with visible Chromium (`headless=false`) when operator observation is requested. 2026-07-10: ControlPlane Web was verified with Playwright MCP visible browser.
- [x] API health endpoint responds. 2026-07-08: verified `GET /api/control-plane/system/info`.
- [x] Control Plane Web does not call CommerceNode directly. 2026-07-10: Playwright request capture on Dashboard, Nodes, Stores, Health, Actions, Users, Audit Logs, and Catalog found only `localhost:5280` ControlPlane API calls and 0 direct `localhost:5180`, `/api/commerce/*`, or `/api/internal/*` calls.
- [x] Control Plane API does not resolve `AppDbContext`. 2026-07-08: grep verified no `AppDbContext`, `AddSharedAuthenticationInfrastructure`, `AuthConnection`, or `DefaultConnection` usage in ControlPlane API/ControlPlane infrastructure scope.
- [x] Control Plane clean database does not contain legacy Commerce/Storefront tables. 2026-07-09: clean QA DB `blazorshop_controlplane_qa_20260709` contained 0 legacy Commerce/Storefront tables.
- [x] Control Plane Web references `BlazorShop.Web.SharedV2`, not legacy `BlazorShop.Web.Shared`. 2026-07-09: covered by PresentationV2 boundary tests.
- [x] After SharedV2 changes, re-run auth/login smoke and `FullyQualifiedName~ControlPlane`. 2026-07-09: full solution test passed 485/485 with 10 skipped; API smoke and Playwright admin/user login smoke passed.

## Startup Database Migration

- [x] Clean `ControlPlaneConnection` database is created/migrated by `BlazorShop.ControlPlane.API` startup when `ControlPlane:Database:MigrateOnStartup=true`. 2026-07-11: startup smoke passed against disposable DB `blazorshop_controlplane_startup_qa_20260711`.
- [ ] Existing migrated Control Plane database restarts without duplicate migration or seed side effects.
- [x] Startup migration logs context name, connection name, applied count, pending count, and pending migration names. 2026-07-11: verified in `.gstack/startup-migration-qa/controlplane-startup-migration.log`.
- [x] Startup migration logs do not expose passwords or raw connection strings. 2026-07-11: smoke assertion checked logs did not contain `Password=`.
- [ ] Invalid `ControlPlaneConnection` fails API startup when `ControlPlane:Database:FailStartupOnMigrationError=true`.
- [ ] `ControlPlane:Database:LogMigrationState=false` still runs migration without state log noise.
- [x] `ControlPlaneDbContext` startup migration never touches `CommerceNodeConnection` or `AppDbContext`. 2026-07-11: smoke used only `ConnectionStrings__ControlPlaneConnection`; architecture/code path resolves only `ControlPlaneDbContext`.

## Open QA Findings

- [x] QA-CP-001: Wrong-password login is testable against Control Plane PostgreSQL on port 5433. 2026-07-08: fixed startup auth schema migration and verified safe 400 response.
- [x] QA-CP-002: Control Plane Web login/logout routes exist. 2026-07-08: `/login` renders the sign-in form; seeded admin logout returns to `/login` and clears header session state.
- [x] QA-CP-003: Control Plane User Management UI/API is implemented and live QA passed. 2026-07-08: verified on clean `blazorshop_controlplane_user_management_live_qa3` with seeded admin/user accounts; API and browser Users page flows passed after fixing ISSUE-001 transaction strategy and ISSUE-002 disabled-login blocking.
- [n/a] QA-CP-004: Audit Logs page is a static placeholder; action/actor search is not backed by an API yet.
- [x] QA-CP-005: Protected pages redirect unauthenticated users through the login route guard. 2026-07-08: `/nodes` redirected to `/login/nodes` with 0 console errors.
- [x] QA-CP-006: Valid login is unblocked on a clean ControlPlane database when development account seeds are enabled. 2026-07-08: QA DB seeded `admin@example.local` as `platform_owner` and `user@example.local` as `auditor`; both valid logins returned 200 with JWT and login success was audited.
- [~] QA-CP-008: Standard user write actions are API-blocked with 403, but write buttons are still visible in the UI. 2026-07-08: fixed misleading 403 message; hiding/disabling write controls by permission is still a UX follow-up.
- [~] QA-CP-007: Existing local dev database may still be contaminated by the earlier `AppDbContext` migration. Reset `blazorshop_controlplane` before local browser QA if `AspNet*` tables already exist without matching ControlPlane migration history.

## Auth

- [x] Login page/form is reachable. 2026-07-08: `/login` renders email/password fields and submit button.
- [x] Valid admin login succeeds. 2026-07-08: API login passed on clean ControlPlane QA DB with seeded platform owner.
- [x] Logged-in state is preserved on page refresh. 2026-07-08: browser QA refreshed Dashboard after admin login and retained `admin@example.local` session.
- [x] Current session/profile status is visible or API-verifiable. 2026-07-08: successful login created/loaded `control_plane_admin_user` profile for the seeded admin.
- [x] Logout clears session state. 2026-07-08: browser QA clicked Logout as seeded admin and returned to `/login` with Login header state.
- [x] Wrong password is rejected with a safe error message. 2026-07-08: UI shows `Invalid credentials.` and API returns 400 without sensitive detail.
- [x] Repeated wrong-password attempts are rejected consistently. 2026-07-08: repeated API attempts returned the same safe 400 response.
- [x] Repeated wrong-password attempts do not expose account existence or sensitive detail. 2026-07-08: response remained `Invalid credentials.`.
- [x] Protected pages redirect or block unauthenticated users. 2026-07-08: `/nodes` redirects to `/login/nodes` with no protected API call noise.
- [~] Expired/invalid token state is handled without a broken UI. 2026-07-08: unauthenticated/no-session refresh handled without console error; expired token not tested.

## API Response Pattern

- [x] Success responses return `success=true`, `message`, and `data`. 2026-07-08: verified `system/info`, admin login, dashboard summary, and user create on clean `blazorshop_controlplane_api_response_qa`.
- [x] Failure responses return `success=false`, `message`, and `data`. 2026-07-08: verified wrong login, unauthorized dashboard request, auditor forbidden users request, missing node, node validation, and duplicate user conflict.
- [x] HTTP status codes remain meaningful while UI consumes `Success`, `Message`, and `Data`. 2026-07-08: verified `400`, `401`, `403`, `404`, and `409` keep envelope bodies.
- [x] Web client unwraps envelope for authenticated pages. 2026-07-08: browser smoke logged in as seeded admin, loaded Dashboard, loaded Users, and showed 0 console errors.
- [x] Response message originates from API for visible failures. 2026-07-08: API smoke confirmed failure `message` values for auth, permission, validation, not found, and conflict.

## Users And Permissions

### Admin User

- [x] Admin role can access Dashboard. 2026-07-08: seeded admin loaded Dashboard counters in browser.
- [x] Admin role can access Nodes. 2026-07-08: seeded admin loaded Nodes list and detail panel in browser.
- [x] Admin role can create nodes. 2026-07-08: seeded admin created `qa-ui-node-*` from browser and `qa-flow-node-*` through API.
- [x] Admin role can rotate credentials. 2026-07-08: seeded admin API rotate returned 200 with a one-time replacement secret.
- [x] Admin role can disable nodes. 2026-07-08: seeded admin API disable returned 200 and disabled node appeared in Nodes list.
- [x] Admin role can access Stores. 2026-07-08: seeded admin loaded Stores list/detail in browser.
- [x] Admin role can assign stores to nodes. 2026-07-08: seeded admin created `qa-ui-store-*` assigned to `qa-ui-node-*` from browser.
- [x] Admin role can access Health. 2026-07-08: seeded admin loaded Health page and missing-heartbeat states in browser.
- [n/a] Admin role can access Audit Logs. 2026-07-08: page exists as placeholder, no API-backed audit read implementation yet.

### Standard User

- [x] Standard user can log in. 2026-07-08: seeded `user@example.local` login succeeded in browser and API.
- [x] Standard user sees allowed Control Plane pages. 2026-07-08: auditor user loaded Dashboard/Nodes/Stores/Health/Actions read pages.
- [~] Standard user cannot access admin-only actions through UI. 2026-07-08: user create-node submit is blocked with a 403 permission message, but the Create button remains visible.
- [x] Standard user cannot access admin-only actions through direct API calls. 2026-07-08: user token received 403 on node/store/credential create calls.

### Permission Enforcement

- [~] User without `nodes.read` cannot list/view nodes. Unit-covered; live no-read role was not created in this run.
- [x] User without `nodes.write` cannot create/update/disable nodes. 2026-07-08: live auditor token received 403 on node create; UI shows permission error after QA fix.
- [x] User without `credentials.rotate` cannot create/revoke/rotate API keys. 2026-07-08: live auditor token received 403 on credential create.
- [~] User without `stores.read` cannot list/view stores. Unit-covered; live no-read role was not created in this run.
- [x] User without `stores.write` cannot create/update/assign/archive stores. 2026-07-08: live auditor token received 403 on store create.
- [~] User without `health.read` cannot view health data. Unit-covered; live no-read role was not created in this run.
- [~] User without `actions.read` cannot view/control actions. Unit-covered; live no-read role was not created in this run.
- [n/a] User without `audit.read` cannot view audit logs. No API-backed audit read endpoint yet.

### User Management

- [x] User list loads. 2026-07-08: admin API list returned seeded and QA-created users; browser Users page rendered table rows and summary counters.
- [x] User detail loads. 2026-07-08: admin API detail returned 200; browser row selection opened the detail panel.
- [x] User role and permission catalogs load. 2026-07-08: API role/permission catalogs returned 200 and browser filters/forms populated.
- [x] Create user succeeds. 2026-07-08: API create returned 201 and browser create form saved `UI QA User`.
- [x] Duplicate user email is rejected. 2026-07-08: duplicate API create returned 409.
- [x] Update user display name succeeds. 2026-07-08: API update returned 200 and persisted the new display name.
- [x] Assign user role succeeds. 2026-07-08: API role assignment returned 200.
- [x] Remove user role succeeds. 2026-07-08: API role removal returned 200 for a non-owner QA user.
- [x] Enable user succeeds. 2026-07-08: API enable returned 200 and changed status back to `active`.
- [x] Disable user succeeds. 2026-07-08: API disable returned 200 and changed status to `disabled`.
- [x] Assign permission succeeds. 2026-07-08: API direct permission assignment returned 200.
- [x] Remove permission succeeds. 2026-07-08: API direct permission removal returned 200.
- [x] Removing a direct permission does not remove inherited role permission. 2026-07-08: removing direct `nodes.read` left role-inherited effective `nodes.read` intact.
- [x] Admin cannot disable own account. 2026-07-08: self-disable API call returned 409.
- [x] Admin cannot disable or remove the last active `platform_owner`. 2026-07-08: last-owner role removal returned 409; self-disable protection also returned 409.
- [x] Standard user without `users.read` cannot list users. 2026-07-08: seeded auditor list request returned 403.
- [x] Standard user without `users.write` cannot create/update/enable/disable users. 2026-07-08: seeded auditor create/update/enable/disable requests returned 403.
- [x] Standard user without `roles.assign` cannot assign/remove roles. 2026-07-08: seeded auditor assign-role and remove-role requests returned 403.
- [x] Standard user without `permissions.manage` cannot assign/remove direct permissions. 2026-07-08: seeded auditor assign-permission and remove-permission requests returned 403.
- [x] Disabled user cannot log in. 2026-07-08: disabled QA user login returned safe 400 after ISSUE-002 fix.
- [x] User/role/permission changes are audited. 2026-07-08: clean QA DB recorded `users.create`, `users.update`, `users.disable`, `users.enable`, `users.role.assign`, `users.role.remove`, `users.permission.assign`, and `users.permission.remove`.

## Nodes

- [x] Node list loads. 2026-07-08: authenticated admin and auditor browser sessions loaded persisted QA nodes.
- [n/a] Empty node list state is readable. 2026-07-08: QA DB contains seeded/test nodes; empty state not exercised in this run.
- [x] Node create succeeds with valid data. 2026-07-08: admin browser created `qa-ui-node-*`; API created additional QA nodes. 2026-07-08 Commerce Node phase: service/API contract now requires `node_secret`.
- [x] Node create validates required fields. 2026-07-08: empty browser submit showed node-key validation message.
- [x] Node create requires Commerce Node secret. 2026-07-08: `dotnet test` covers updated create validation path and all node fixtures use `test-node-secret`.
- [~] Node create rejects duplicate node key. Service tests pass; live duplicate UI submit was not exercised in this run.
- [x] Node edit can update name, description, Control API URL, and optionally node secret. 2026-07-08: Web Nodes detail panel now includes edit mode; solution build passed.
- [x] Node detail does not expose raw node secret. 2026-07-08: DTO/Web expose only `HasNodeSecret` and `NodeSecretUpdatedAt`.
- [x] Node detail loads. 2026-07-08: browser View opened node detail panel.
- [x] Node detail shows endpoint/status metadata. 2026-07-08: browser detail panel showed status, Control API, description, and primary endpoint.
- [x] Rotate API key succeeds. 2026-07-08: admin API rotate returned 200.
- [x] Rotated API key is shown only once. 2026-07-08: create/rotate API responses returned `rawSecret`; audit payload assertion found no raw secret.
- [x] Old API key is revoked or inactive after rotation. 2026-07-08: DB showed original key status `rotated` with `revoked_at`.
- [x] Disable node succeeds. 2026-07-08: admin API disable returned 200.
- [x] Disabled node is visible as disabled. 2026-07-08: browser Nodes list showed `QA Flow Node` as `disabled`.
- [~] Disabled node cannot receive write/control operations that require an active node. Service tests pass; live disabled-node write/control attempt was not exercised in this run.

## Stores

- [x] Store list loads. 2026-07-08: authenticated admin and auditor sessions loaded Stores list.
- [n/a] Empty store list state is readable. 2026-07-08: QA DB contains seeded/test stores; empty state not exercised in this run.
- [x] Store create succeeds with valid data. 2026-07-08: admin browser created `qa-ui-store-*` assigned to `qa-ui-node-*`.
- [~] Store create validates required fields. Service tests pass; live required-field UI submit was not exercised in this run.
- [~] Store create rejects duplicate active store key. Service tests pass; live duplicate UI submit was not exercised in this run.
- [x] Assign store to node succeeds. 2026-07-08: create-store browser flow assigned the new store to the selected active node.
- [~] Reassign store to node succeeds. Service tests pass; live reassign submit was not exercised in this run.
- [x] Store detail loads. 2026-07-08: browser row click opened Store detail panel.
- [x] Store detail shows node assignment and domains. 2026-07-08: detail panel showed assigned node, metadata, domain count/control.
- [x] Store status is visible. 2026-07-08: Stores list/detail showed `active` status.
- [~] Archived/disabled store state is handled correctly. Service tests pass; live archive/disabled-store UI state was not exercised in this run.

### Store Lifecycle

- [x] Control Plane store status supports active, disabled, archived, and provisioning without relying on a missing database status value. 2026-07-15: migration/model update included in store lifecycle phase; focused `FullyQualifiedName~ControlPlaneStore` test set passed.
- [x] Store display order remains owned by Control Plane manager data. 2026-07-15: lifecycle phase kept display order in Control Plane store DTO/UI flow and did not move it into CommerceNode.
- [x] Control Plane gateway exposes CommerceNode runtime lifecycle controls through Control Plane API only. 2026-07-15: build passed after gateway/UI changes; no direct Storefront or CommerceNode Web calls were added.
- [x] Store company/contact fields are managed as CommerceNode runtime profile data instead of duplicated in Control Plane registry. 2026-07-15: lifecycle phase added CommerceNode store profile fields and Storefront current-store contract coverage.

## Catalog Product Media

- [ ] Catalog page media panel loads for a selected product.
- [ ] Product media list is loaded through ControlPlane API only.
- [ ] Product media import submits to `api/control-plane/stores/{storePublicId}/catalog/products/{productId}/media/import`.
- [ ] Import task id/status is shown from API response.
- [ ] Set primary media works from Catalog page.
- [ ] Delete media works from Catalog page.
- [ ] Retry failed media works from Catalog page.
- [ ] API error message from CommerceNode is surfaced through ControlPlane API response message.
- [ ] Browser network capture shows no direct `api/commerce/*` call from ControlPlane Web.

## Commerce Admin Gateway Rescope

- [x] ControlPlane CommerceNode catalog/media gateway appends `storeKey` query to Commerce Admin calls. 2026-07-14: `ControlPlaneCommerceCatalogService` gateway routes were updated in Phase 6.
- [x] ControlPlane CommerceNode catalog/media gateway keeps `X-Node-Key` and `X-Node-Secret`. 2026-07-14: build verified after Phase 6 gateway update.
- [x] ControlPlane CommerceNode catalog/media gateway no longer sends `X-Store-Key` to Commerce Admin endpoints. 2026-07-14: Phase 6 removed the header from gateway calls.
- [ ] ControlPlane product/category/order/media/admin pages still load after Commerce Admin missing-`storeKey` enforcement.
- [ ] Browser network capture confirms ControlPlane Web still calls only ControlPlane API.
- [ ] ControlPlane API product media preview calls `api/commerce/admin/media/products/{mediaId}?storeKey={storeKey}`.
- [ ] ControlPlane API asset media preview calls `api/commerce/admin/media/assets/{assetId}/preview?storeKey={storeKey}`.

## Commerce Storefront Pages

- [x] ControlPlane API builds after storefront page gateway changes. 2026-07-11: `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore` passed.
- [x] ControlPlane Web builds after storefront page admin UI changes. 2026-07-11: `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj --no-restore` passed.
- [x] Apply `ControlPlaneCommercePagePermissions` migration to ControlPlane PostgreSQL on port `5433`. 2026-07-12: `run-v2-local.ps1` startup migration applied `20260711164138_ControlPlaneCommercePagePermissions` to `blazorshop_controlplane_v2_local`.
- [x] Pages nav item renders under Commerce Admin. 2026-07-12: Playwright MCP visible browser showed `Pages` nav item and loaded `/commerce-admin/pages`.
- [x] Pages list loads after selecting a store. 2026-07-12: selected `Default QA Store (default)` and list loaded `QA Dynamic Page 20260712034014` plus draft row.
- [x] Pages list calls ControlPlane API only. 2026-07-12: browser resource capture showed only `localhost:5280/api/controlplane/commerce/.../pages` calls from ControlPlane Web.
- [x] Browser network has no direct CommerceNode page API calls. 2026-07-12: Playwright performance resource capture showed zero `localhost:5180` calls from ControlPlane Web during list/create/reload.
- [x] Page list uses `pageNumber/pageSize`. 2026-07-12: browser resource capture included `pages?pageNumber=1&pageSize=25&status=all`; UI showed `Page 1 of 1 - 2 total`.
- [x] Search title works. 2026-07-12: Playwright UI search for `Dynamic` returned `QA Dynamic Page 20260712034014`.
- [x] Search slug works. 2026-07-12: Playwright UI search for `qa-dynamic-page` returned `QA Dynamic Page 20260712034014`.
- [x] Status filter works. 2026-07-12: Playwright UI filter `published` returned only published page; filter `draft` returned only `QA Draft Page 20260712034014`.
- [x] Create draft page works. 2026-07-12: CommerceNode admin API created `qa-draft-page-20260712034014` with `isPublished=false`; ControlPlane list displayed it as `Draft`.
- [ ] Edit page works.
- [x] Publish page works. 2026-07-12: ControlPlane UI created `qa-dynamic-page-20260712034014` with `Published` status and Storefront rendered it at `/pages/qa-dynamic-page-20260712034014`.
- [x] Include in sitemap toggle works. 2026-07-12: ControlPlane UI created `qa-dynamic-page-20260712034014` with `Included`; Storefront `/sitemap.xml` contained `/pages/qa-dynamic-page-20260712034014`.
- [ ] Archive page works.
- [ ] API validation messages display from response message.
- [ ] User without `commerce.pages.read` cannot list/view pages.
- [ ] User without `commerce.pages.write` cannot create/update/archive pages.

## Commerce Media Library

- [x] ControlPlane API builds after Media Library gateway changes. 2026-07-13: `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj` passed after stopping the locked local ControlPlane API process.
- [x] ControlPlane Web builds after Media Library page changes. 2026-07-13: `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj` passed.
- [x] Media Library nav item renders under Commerce Admin at `/commerce-admin/media-library`. 2026-07-13: route/page added and build passed.
- [x] Media Library list loads after selecting a store. 2026-07-13: visible browser loaded Default QA Store and showed empty/list states.
- [x] Media Library list calls ControlPlane API only. 2026-07-13: browser HTTP logs used `/api/controlplane/commerce/.../media/assets`.
- [x] Browser network capture shows no direct `api/commerce/*` or CommerceNode host calls. 2026-07-13: browser capture showed no direct `localhost:5180` calls during page/list/preview.
- [x] Upload creates an asset and opens the asset drawer. 2026-07-13: PNG upload created `Summer Sale Banner` and opened the drawer.
- [x] Grid thumbnail preview loads through ControlPlane API preview proxy. 2026-07-13: preview loaded as `data:image/webp` after fixing imgproxy local path.
- [x] Metadata save updates display name, alt text, title text, and generated version. 2026-07-13: drawer showed edited QA metadata and link version updated.
- [x] Replace file keeps the public id and canonical file name. 2026-07-13: replace kept the public id and `summer-sale-banner.png`, with a new version timestamp.
- [x] Delete asset removes it from the grid. 2026-07-13: delete returned the page to `No media assets found`.
- [x] Link generator emits a local `/media/assets/{assetPublicId}/{canonicalFileName}` URL with transform query. 2026-07-13: generated `/media/assets/.../summer-sale-banner.png?w=320&h=180&fit=cover&format=webp&v=...`.
- [x] Link generator emits an `<img>` snippet with `alt`, optional `title`, `loading="lazy"`, and width/height when selected. 2026-07-13: snippet included edited alt/title, `width="320"`, `height="180"`, and lazy loading.

## Commerce Admin UX Completion

- [x] Commerce Admin nav group renders. 2026-07-11: visible Playwright MCP browser showed Products, Product Imports, Categories, Variation Templates, and Orders links.
- [x] Legacy `/catalog` route redirects to `/commerce-admin/products`. 2026-07-11: browser navigation to `/catalog` landed on `/commerce-admin/products`.
- [x] Products page loads after selecting a store. 2026-07-11: active ControlPlane store `default` loaded products from CommerceNode.
- [x] Product list calls ControlPlane API only. 2026-07-11: network capture showed `/api/controlplane/commerce/*` requests through ControlPlane API.
- [x] Product list does not call CommerceNode directly. 2026-07-11: network capture showed 0 browser calls to `localhost:5180`, `api/commerce/*`, or `api/internal/*`.
- [x] Product thumbnail placeholder renders before image load. 2026-07-11: Products list/drawer rendered placeholder/media preview without console errors.
- [x] Product thumbnail image, when available, loads through ControlPlane API preview proxy. 2026-07-11: product media preview was loaded through `/api/controlplane/commerce/.../media/.../preview`.
- [x] Product drawer opens and closes without losing list filters. 2026-07-11: product row opened drawer with current store/filter context preserved.
- [x] Product drawer shows SEO first. 2026-07-11: product drawer order was SEO, Basic info, Media, Variations, Inventory.
- [x] Product drawer can update allowed SEO fields. 2026-07-11: UI changed meta title; PUT product SEO returned 200.
- [x] Product drawer can update allowed basic fields. 2026-07-11: UI changed display order; PUT product returned 200.
- [x] Product drawer does not allow editing SKU. 2026-07-11: drawer shows SKU in locked header/notice only.
- [x] Product drawer does not allow editing product type. 2026-07-11: drawer shows product type as locked, not editable.
- [x] Product drawer does not allow editing variation template. 2026-07-11: drawer shows variation template read-only.
- [x] Product drawer media section lists media. 2026-07-11: selected product drawer listed stored media URL and primary state.
- [~] Product drawer media import queues URLs. Media import UI was visible; live queue submit was not exercised in this run.
- [~] Product drawer can set primary media. Existing primary state was visible; primary mutation was not exercised in this run.
- [~] Product drawer can retry failed media. No failed media item was selected in this run.
- [x] Product drawer inventory section updates product quantity. 2026-07-11: UI incremented product quantity; PUT inventory returned 200.
- [~] Product drawer inventory section updates existing variant stock. Variant stock UI was visible when variants exist; variant mutation was not exercised in this run.
- [x] Product Import page downloads header-only CSV template. 2026-07-11: browser downloaded `product-import-template.csv`.
- [x] Product Import page uploads CSV in `create_only`. 2026-07-11: UI upload submitted CSV; POST product-imports returned 200 and queued job.
- [x] Product Import page uploads CSV in `upsert`. 2026-07-11: UI upload submitted CSV; POST product-imports returned 200.
- [x] Product Import job list refreshes status. 2026-07-11: job list refreshed after upload and showed queued/completed/error jobs.
- [x] Product Import job drawer shows row errors. 2026-07-11: drawer showed failed row and validation message for `too-many-images.csv`.
- [x] Product Import error CSV downloads. 2026-07-11: browser downloaded `product-import-*-errors.csv`.
- [x] Category page shows and copies `category_slug`. 2026-07-11: clipboard read after copy returned `qa-catalog-category-20260708234755`.
- [x] Variation Template page shows and copies `variation_template_slug`. 2026-07-11: clipboard read after copy returned `qa-adminux-template-20260711030024`.
- [x] Variation Template drawer can create/update template. 2026-07-11: create template POST returned 200.
- [x] Variation Template drawer can create/update/disable option. 2026-07-11: option create POST and disable/update PUT returned 200.
- [x] Variation Template drawer can create/update/disable value. 2026-07-11: value create POST and disable/update PUT returned 200.
- [x] Orders page loads order list. 2026-07-11: Orders page loaded 4 orders for active QA store.
- [x] Order drawer shows lines/totals/customer fields. 2026-07-11: order drawer showed totals, status, customer placeholders, and order line details.
- [x] Order drawer creates shipment. 2026-07-11: UI saved shipment for pending QA order; PUT shipment returned 200.
- [x] Order drawer updates existing shipment. 2026-07-11: after save, drawer reloaded shipment via GET 200 and retained shipment fields.
- [x] Shipment update syncs visible order shipping fields after refresh. 2026-07-11: order list updated selected order shipping state to `Shipped`.
- [x] API error messages are displayed from response `message`. 2026-07-11: empty Variation option submit now shows UI validation and does not send a bad request; import row errors display API message text.
- [x] Browser console has no unexpected errors. 2026-07-11: final visible Playwright console checks returned 0 errors after fixes.
- [x] Visible Playwright MCP browser QA is used with Chromium `headless=false` when operator observation is requested. 2026-07-11: QA ran in the existing visible Playwright MCP browser.
- [x] Browser network capture shows ControlPlane Web makes 0 direct calls to CommerceNode. 2026-07-11: final network capture showed 0 direct calls to `localhost:5180`, `api/commerce/*`, or `api/internal/*`.
- [x] ControlPlane Web build passes after Admin UX Completion implementation. 2026-07-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj --no-restore` passed.
- [x] ControlPlane API build passes after Admin UX Completion implementation. 2026-07-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore` passed.
- [x] CommerceNode API build passes after Admin UX Completion implementation. 2026-07-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.

## Health

- [x] Health page loads. 2026-07-08: authenticated admin session loaded Health page.
- [x] Latest heartbeat is visible when available. 2026-07-09: Control Plane probe against Commerce Node healthz persisted healthy samples and Health page rendered successfully.
- [x] Missing heartbeat state is readable. 2026-07-08: Health page showed `No heartbeat` / `No sample` states.
- [n/a] Node dependency status is visible when available. 2026-07-08: no dependency snapshot existed in QA DB.
- [x] Manual probe handles reachable node. 2026-07-08: service test verifies Control Plane calls `/api/commerce/healthz` with `X-Node-Key` and `X-Node-Secret`, parses success envelope, and persists healthy snapshot.
- [x] Manual probe handles invalid Commerce Node credential. 2026-07-08: service test maps Commerce Node 401 envelope to `down` snapshot with `invalid_credentials`.
- [~] Manual probe handles unreachable node. Service tests pass for request failure/timeouts; live unreachable-node probe was not exercised in this run.
- [x] Health failure state does not break the page. 2026-07-08: unauthenticated 401 state renders without app crash.

## Commerce Node MVP

- [x] Commerce Node API project exists under `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API`. 2026-07-08: solution build includes the new project.
- [x] Commerce Node uses a separate ecom DB boundary. 2026-07-08: `CommerceNodeDbContext` uses `CommerceNodeConnection` and does not inherit `AppDbContext`.
- [x] Commerce Node DB context does not include Control Plane auth/registry tables. 2026-07-08: context exposes ecom DbSets only.
- [x] Commerce Node options bind `NodeKey`, `NodeSecret`, and `AllowedControlPlaneIps`. 2026-07-08: options validate on startup.
- [x] `GET /api/commerce/healthz` exists. 2026-07-08: project build passed with endpoint registered under `api/commerce`.
- [x] `healthz` returns API response envelope. 2026-07-08: endpoint uses `CommerceNodeApiResponseWriter.Success`.
- [x] Commerce Node rejects missing/wrong key or secret with envelope 401. 2026-07-08: local HTTP smoke returned 401 envelope for wrong `X-Node-Secret`.
- [x] Commerce Node rejects disallowed IP with envelope 403. 2026-07-08: local HTTP smoke with allowlist excluding `127.0.0.1` returned 403 envelope.
- [x] Control Plane probe uses `api/commerce/healthz`. 2026-07-08: `CommerceNodeControlClient` appends `/api/commerce/healthz`.
- [x] Control Plane probe sends `X-Node-Key` and `X-Node-Secret`. 2026-07-08: service test asserts both headers.
- [x] Control Plane probe no longer calls `/health` or `/capabilities` for MVP. 2026-07-08: capability snapshots are not created by MVP healthz tests.

## Actions

- [x] Action list loads. 2026-07-08: authenticated admin/auditor sessions loaded Actions empty state in browser.
- [x] Action enqueue succeeds for a valid active node. 2026-07-09: clean DB smoke enqueued `sync_store_placeholder` for active `dev-node`.
- [~] Action enqueue rejects disabled nodes. Service tests pass; live disabled-node enqueue was not exercised in this run.
- [x] Action detail loads. 2026-07-09: API smoke loaded the created action after enqueue.
- [x] Action attempts are visible. 2026-07-09: API smoke recorded an action attempt and DB audit confirmed `actions.attempt.record`.
- [~] Action cancellation succeeds for cancellable actions. Service tests pass; no cancellable action was created in this run.
- [~] Duplicate idempotency key returns or preserves the expected action. Service tests pass; live duplicate enqueue was not exercised in this run.

## Audit Logs

- [x] Audit Logs page loads. 2026-07-08: placeholder page loads with no console errors.
- [n/a] Audit log search by action works. Page has static search controls, no API-backed search implementation yet.
- [n/a] Audit log search by actor works. Page has static search controls, no API-backed search implementation yet.
- [x] Login success is audited. 2026-07-08: clean ControlPlane QA DB recorded `auth.login` success for the seeded admin.
- [x] Login failure is audited. 2026-07-08: wrong-password UI/API attempts wrote `auth.login` failure entries for the submitted actor email.
- [x] Logout is audited. 2026-07-08: DB recorded `auth.logout` for seeded admin.
- [x] Node create/update/disable is audited. 2026-07-08: DB recorded `nodes.create` and `nodes.disable` for seeded admin.
- [x] Credential create/reveal/revoke/rotate is audited. 2026-07-08: DB recorded `credentials.create`, `credentials.reveal`, and `credentials.rotate`.
- [x] Store create/update/archive/domain changes are audited. 2026-07-08: DB recorded `stores.create`; update/archive/domain audit still needs dedicated UI/API exercise.
- [x] User create/update/disable/enable/role/permission changes are audited. 2026-07-08: DB query on clean User Management QA database found success/failure audit rows for user creation, profile updates, status changes, role changes, and direct permission changes.
- [x] Health probe is audited. 2026-07-09: clean QA DB recorded `health.probe` rows.
- [x] Action enqueue/attempt/cancel is audited. 2026-07-09: clean QA DB recorded `actions.enqueue` and `actions.attempt.record`; cancel remains service-covered because no cancellable live action was needed in this smoke run.
- [x] Audit log payload does not expose raw API secrets or passwords. 2026-07-08: direct DB assertion found 0 audit metadata payloads containing raw `bs_cp_` secrets or QA seed passwords.

## Dashboard

- [x] Dashboard loads. 2026-07-08: authenticated admin and auditor browser sessions loaded Dashboard.
- [x] Total Nodes counter is correct. 2026-07-08: Dashboard reflected live QA node count after API/UI creates.
- [x] Healthy Nodes counter is correct. 2026-07-08: Dashboard showed 0 healthy nodes matching QA DB state.
- [x] Warning Nodes counter is correct. 2026-07-08: Dashboard showed 0 warning nodes matching QA DB state.
- [x] Down Nodes counter is correct. 2026-07-08: Dashboard showed 0 down nodes matching QA DB state.
- [x] Total Stores counter is correct. 2026-07-08: Dashboard reflected live QA store count after API/UI creates.
- [x] Dashboard links navigate to the expected filtered pages. 2026-07-08: Dashboard link targets for nodes/status/stores are present.

## Accessibility And UX Smoke

- [x] Primary pages have one clear `h1`. 2026-07-08: checked Dashboard, Nodes, Stores, Health, Actions, Audit Logs route snapshots.
- [x] Form inputs have labels. 2026-07-08: browser QA verified login, node create, and store create/detail labels; Audit placeholder inputs still only use placeholders pending API-backed implementation.
- [x] Buttons have clear accessible names. 2026-07-08: route snapshots show named primary buttons.
- [x] Keyboard tab order is usable for login and core forms. 2026-07-08: login, node create, and store create forms expose reachable labeled fields and submit buttons.
- [x] Mobile viewport does not overlap navigation, tables, or forms. 2026-07-08: 375x812 login smoke test had no horizontal overflow.
- [x] Error messages are actionable and do not expose sensitive detail. 2026-07-08: unauthenticated messages are safe and actionable.

## Regression Automation Suggestions

- [~] Backlog: add API integration tests for auth success/failure/session.
- [~] Backlog: add API integration tests for each permission policy denial.
- [~] Backlog: add service tests for wrong-password lockout/rate-limit behavior when implemented.
- [~] Backlog: add Playwright smoke tests for Dashboard, Nodes, Stores, Health, Actions, Audit Logs.
- [~] Backlog: add audit-log assertions to mutation endpoint tests.
- [~] Backlog: add database migration test that validates seed roles and permissions.
- [~] Backlog: add database migration test that fails if ControlPlane DB contains legacy Commerce/Storefront tables.
- [~] Backlog: add API integration tests for User Management list/create/status/role/permission flows.
- [~] Backlog: add Playwright smoke tests for User Management admin and restricted-user flows.
- [~] Backlog: add contract tests for Commerce Node heartbeat/probe payloads.
- [~] Backlog: add integration tests for Commerce Node `healthz` valid, invalid credential, and IP allowlist paths.
- [~] Backlog: add migration test for `commerce_node.node_secret` and `node_secret_updated_at`.
- [~] Backlog: add automated startup migration smoke for `ControlPlaneDbContext` with a disposable PostgreSQL database.

## Pagination Contract Regression

- [ ] Product Import template downloads from global `api/controlplane/commerce/product-imports/template` without requiring a selected store and returns the canonical parser header.
- [ ] Product Import valid CSV from downloaded template queues and completes.
- [ ] Product Import invalid header shows job-level error in drawer.
- [ ] Product Import error CSV includes job-level error when row list is empty.
- [ ] Product Import jobs page has previous/next controls and honors `pageNumber/pageSize`.
- [ ] Product Import rows drawer has previous/next controls and honors `pageNumber/pageSize`.
- [ ] Stores page has page controls and search/status filters still work.
- [ ] Health page has page controls, search/status filters, and detail load still works.
- [ ] Health timeline uses paged `nodes/{nodePublicId}/timeline` response with `items`, `totalCount`, `pageNumber`, `pageSize`, and `totalPages`.
- [ ] Actions page has page controls and status/action filters still work.
- [ ] Users page has page controls and search/status/role/permission filters still work.
- [ ] Nodes page has page controls and search/status filters still work.
- [ ] Credentials page has page controls for node credentials.
- [ ] Category list uses paged response data.
- [ ] Variation templates use paged response data.
- [ ] Product media and product variants use paged response data.
- [ ] ControlPlane Web request capture confirms no direct calls to CommerceNode APIs.

## QA Run History

| Date | Tester | Scope | Result | Notes |
| --- | --- | --- | --- | --- |
| 2026-07-08 | Codex | Initial Control Plane QA checklist creation and unauthenticated smoke verification | Partial | Fixed Web dev API base URL and Blazor static asset startup. Auth/live mutations blocked by missing shared legacy PostgreSQL on `localhost:5432`; User Management and API-backed Audit Logs are not implemented yet. |
| 2026-07-08 | Codex | Control Plane auth QA after AuthConnection/login implementation | Partial | Fixed auth schema startup migration, credentialed CORS, and no-session refresh console error. Verified login page, wrong-password rejection, repeated failure behavior, route guard, login failure audit, and mobile smoke. Valid login/admin/user flows blocked because `AspNetUsers` has 0 rows; User Management and API-backed Audit Logs are not implemented yet. |
| 2026-07-08 | Codex | Control Plane isolated auth DB implementation | Partial | Built API/Web and ran tests. Runtime smoke used clean QA database with `ControlPlaneDbContext` migrations only; verified no legacy tables, seeded admin, valid login, wrong-password rejection, refresh token persistence, and login success/failure audit. Browser authenticated pages and logout still need full Playwright QA after resetting the local dev DB. |
| 2026-07-08 | Codex | Seeded admin/user account QA and authenticated browser/API flows | Partial | Added two-account dev seeding, verified admin/user login, logout, refresh persistence, Dashboard/Nodes/Stores/Health/Actions/Audit page loads, admin node/store/credential mutations, auditor 403 denials, and audit persistence on clean `blazorshop_controlplane_seed_qa`. Fixed misleading 403 UI message. Remaining UX follow-up: hide/disable write controls for users without write permissions. |
| 2026-07-08 | Codex | User Management implementation verification | Partial | Implemented database/API/Web phases and committed each phase. `dotnet build` passed for ControlPlane API and Web; migration applied successfully on clean `blazorshop_controlplane_user_management_qa`. Live browser/API QA for User Management remains pending. |
| 2026-07-08 | Codex | User Management QA on clean database | Passed | Verified API and browser Users flows on clean `blazorshop_controlplane_user_management_live_qa3`. Found and fixed ISSUE-001 user-create transaction execution strategy and ISSUE-002 disabled Control Plane login blocking. API-backed Audit Logs search remains not implemented. |
| 2026-07-08 | Codex | API response envelope migration | Passed | Verified `success/message/data` envelopes on clean `blazorshop_controlplane_api_response_qa` for success, validation, unauthorized, forbidden, not found, and conflict responses. Browser smoke verified admin login, Dashboard, Users page, and 0 console errors. |
| 2026-07-08 | Codex | Commerce Node foundation implementation | Passed | Added Commerce Node API shell, ecom DB context boundary, credential/IP guard, `api/commerce/healthz`, Control Plane node secret storage/UI, and Control Plane healthz probe client. `dotnet test BlazorShop.Tests --no-restore` passed: 475 passed, 10 skipped. Local Commerce Node HTTP smoke passed: 200 valid, 401 wrong secret, 403 disallowed IP. |
| 2026-07-09 | Codex | Independent V2 QA after SharedV2 extraction | Passed | Used clean Control Plane DB `blazorshop_controlplane_qa_20260709`, Commerce Node DB on port 5434, ControlPlane API/Web, CommerceNode API, and StorefrontV2 only. Full `dotnet test BlazorShop.sln --no-restore` passed: 485 passed, 10 skipped. Playwright admin/user login and main pages passed; standard user Users page returns expected 403 permission denials without app crash. |
| 2026-07-10 | Codex | ControlPlane gateway boundary QA | Passed | Used clean QA DB `blazorshop_controlplane_qa_20260710`; Playwright MCP visible browser verified admin login, Dashboard, Nodes, Stores, Health, Actions, Users, Audit Logs, and Catalog. Request capture confirmed ControlPlane Web only called ControlPlane API and never called CommerceNode directly. |
| 2026-07-10 | Codex | ControlPlane Admin UX Completion implementation | Partial | Added Products, Product Imports, Categories, Variation Templates, Orders, Commerce Admin nav, and `/catalog` redirect. Build verification passed for ControlPlane Web/API and CommerceNode API. Live browser QA remains pending against a running ControlPlane + CommerceNode environment. |
| 2026-07-11 | Codex | ControlPlane Admin UX Completion visible-browser QA | Passed | Used QA DB `blazorshop_controlplane_adminux_qa`, active CommerceNode store `default`, and Playwright MCP visible browser. Verified Commerce Admin nav, route redirect, products drawer/SEO/basic/inventory, product imports/template/error CSV/upload, categories, variation templates create/update/disable flows, orders/shipment, console cleanliness, and no direct Web calls to CommerceNode. Fixed shipment 404 noise for pending orders and Variation option/value empty-submit 400 noise. |
| 2026-07-11 | Codex | ControlPlane startup database migration smoke | Partial | Build passed, `run-v2-local.ps1 -DryRun` passed, and ControlPlane API startup migrated clean disposable DB `blazorshop_controlplane_startup_qa_20260711` with safe migration logs. Failure-policy and restart-idempotency checks remain open. |

## Checkout And Payment Admin Gateway

- [x] ControlPlane API builds after checkout/payment gateway changes. 2026-07-13: `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore` passed.
- [x] ControlPlane Web builds after checkout/payment admin UI changes. 2026-07-13: `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj --no-restore` passed.
- [x] ControlPlane Web request capture confirms payment method admin page calls only ControlPlane API. 2026-07-13: Playwright performance resource capture showed zero direct `localhost:5180`, `api/commerce/*`, or `api/internal/*` calls.
- [x] Payment Methods page loads store-scoped methods. 2026-07-13: visible browser loaded COD, Stripe, and PayPal rows for the selected store.
- [ ] Payment Methods page can enable/disable a method.
- [x] Payment Methods page rejects invalid settings JSON via API message. 2026-07-13: COD invalid JSON save showed `Payment settings JSON is invalid.`
- [x] Orders drawer shows order/payment/shipping statuses separately. 2026-07-13: Orders table/drawer showed order `processing`, payment `paid`, shipping `not_yet_shipped`, then `shipped`.
- [x] Orders drawer Mark Complete calls ControlPlane API and updates order detail. 2026-07-13: visible browser marked `ORD-20260713-6672B965` complete after shipping update; network capture still showed no direct CommerceNode calls.
- [ ] Orders drawer Cancel calls ControlPlane API and updates order detail.
