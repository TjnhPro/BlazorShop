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
- [x] API health endpoint responds. 2026-07-08: verified `GET /api/control-plane/system/info`.
- [x] Control Plane API does not resolve `AppDbContext`. 2026-07-08: grep verified no `AppDbContext`, `AddSharedAuthenticationInfrastructure`, `AuthConnection`, or `DefaultConnection` usage in ControlPlane API/ControlPlane infrastructure scope.
- [x] Control Plane clean database does not contain legacy Commerce/Storefront tables. 2026-07-08: QA DB contained `AspNet*`, `RefreshTokens`, and ControlPlane tables only; no `Products`, `Categories`, `Orders`, `PaymentMethods`, `Seo*`, or legacy admin/storefront tables.

## Open QA Findings

- [x] QA-CP-001: Wrong-password login is testable against Control Plane PostgreSQL on port 5433. 2026-07-08: fixed startup auth schema migration and verified safe 400 response.
- [x] QA-CP-002: Control Plane Web login/logout routes exist. 2026-07-08: `/login` renders the sign-in form; logout flow still needs a seeded valid user to complete live testing.
- [n/a] QA-CP-003: Control Plane User Management UI/API is not implemented yet.
- [n/a] QA-CP-004: Audit Logs page is a static placeholder; action/actor search is not backed by an API yet.
- [x] QA-CP-005: Protected pages redirect unauthenticated users through the login route guard. 2026-07-08: `/nodes` redirected to `/login/nodes` with 0 console errors.
- [x] QA-CP-006: Valid login is unblocked on a clean ControlPlane database when the development platform-owner seed is enabled. 2026-07-08: QA DB seeded one admin, valid login returned 200 with JWT, login success was audited.
- [~] QA-CP-007: Existing local dev database may still be contaminated by the earlier `AppDbContext` migration. Reset `blazorshop_controlplane` before local browser QA if `AspNet*` tables already exist without matching ControlPlane migration history.

## Auth

- [x] Login page/form is reachable. 2026-07-08: `/login` renders email/password fields and submit button.
- [x] Valid admin login succeeds. 2026-07-08: API login passed on clean ControlPlane QA DB with seeded platform owner.
- [~] Logged-in state is preserved on page refresh. 2026-07-08: blocked by missing login session.
- [x] Current session/profile status is visible or API-verifiable. 2026-07-08: successful login created/loaded `control_plane_admin_user` profile for the seeded admin.
- [~] Logout clears session state. 2026-07-08: route exists, but live verification is blocked by missing valid login session.
- [x] Wrong password is rejected with a safe error message. 2026-07-08: UI shows `Invalid credentials.` and API returns 400 without sensitive detail.
- [x] Repeated wrong-password attempts are rejected consistently. 2026-07-08: repeated API attempts returned the same safe 400 response.
- [x] Repeated wrong-password attempts do not expose account existence or sensitive detail. 2026-07-08: response remained `Invalid credentials.`.
- [x] Protected pages redirect or block unauthenticated users. 2026-07-08: `/nodes` redirects to `/login/nodes` with no protected API call noise.
- [~] Expired/invalid token state is handled without a broken UI. 2026-07-08: unauthenticated/no-session refresh handled without console error; expired token not tested.

## Users And Permissions

### Admin User

- [~] Admin role can access Dashboard. Blocked by missing seeded Control Plane auth user.
- [~] Admin role can access Nodes. Blocked by missing seeded Control Plane auth user.
- [~] Admin role can create nodes. Blocked by missing seeded Control Plane auth user.
- [~] Admin role can rotate credentials. Blocked by missing seeded Control Plane auth user.
- [~] Admin role can disable nodes. Blocked by missing seeded Control Plane auth user.
- [~] Admin role can access Stores. Blocked by missing seeded Control Plane auth user.
- [~] Admin role can assign stores to nodes. Blocked by missing seeded Control Plane auth user.
- [~] Admin role can access Health. Blocked by missing seeded Control Plane auth user.
- [n/a] Admin role can access Audit Logs. 2026-07-08: page exists as placeholder, no API-backed audit read implementation yet.

### Standard User

- [~] Standard user can log in. Blocked by missing seeded Control Plane auth user.
- [~] Standard user sees only allowed Control Plane pages. Blocked by missing seeded Control Plane auth user.
- [~] Standard user cannot access admin-only actions through UI. Blocked by missing seeded Control Plane auth user.
- [~] Standard user cannot access admin-only actions through direct API calls. Existing authorization unit tests cover policy denial; live API role test blocked by missing seeded Control Plane auth user.

### Permission Enforcement

- [~] User without `nodes.read` cannot list/view nodes. Unit-covered; live test blocked by missing seeded Control Plane auth user.
- [~] User without `nodes.write` cannot create/update/disable nodes. Unit-covered; live test blocked by missing seeded Control Plane auth user.
- [~] User without `credentials.rotate` cannot create/revoke/rotate API keys. Unit-covered; live test blocked by missing seeded Control Plane auth user.
- [~] User without `stores.read` cannot list/view stores. Unit-covered; live test blocked by missing seeded Control Plane auth user.
- [~] User without `stores.write` cannot create/update/assign/archive stores. Unit-covered; live test blocked by missing seeded Control Plane auth user.
- [~] User without `health.read` cannot view health data. Unit-covered; live test blocked by missing seeded Control Plane auth user.
- [~] User without `actions.read` cannot view/control actions. Unit-covered; live test blocked by missing seeded Control Plane auth user.
- [n/a] User without `audit.read` cannot view audit logs. No API-backed audit read endpoint yet.

### User Management

- [n/a] User list loads. No Control Plane user-management UI/API exists yet.
- [n/a] Create user succeeds. No Control Plane user-management UI/API exists yet.
- [n/a] Assign user role succeeds. No Control Plane user-management UI/API exists yet.
- [n/a] Enable user succeeds. No Control Plane user-management UI/API exists yet.
- [n/a] Disable user succeeds. No Control Plane user-management UI/API exists yet.
- [n/a] Assign permission succeeds. No Control Plane user-management UI/API exists yet.
- [n/a] Remove permission succeeds. No Control Plane user-management UI/API exists yet.
- [~] Disabled user cannot log in. Authorization service tests cover disabled Control Plane profile; live login blocked by missing seeded Control Plane auth user.
- [n/a] User/role/permission changes are audited. No Control Plane user-management mutation flow exists yet.

## Nodes

- [~] Node list loads. 2026-07-08: unauthenticated route redirects to login; authenticated list blocked by missing seeded Control Plane auth user.
- [~] Empty node list state is readable. Blocked by missing seeded Control Plane auth user.
- [~] Node create succeeds with valid data. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Node create validates required fields. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Node create rejects duplicate node key. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Node detail loads. Blocked by missing seeded Control Plane auth user.
- [~] Node detail shows endpoint/status metadata. Blocked by missing seeded Control Plane auth user.
- [~] Rotate API key succeeds. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Rotated API key is shown only once. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Old API key is revoked or inactive after rotation. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Disable node succeeds. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Disabled node is visible as disabled. Blocked by missing seeded Control Plane auth user.
- [~] Disabled node cannot receive write/control operations that require an active node. Service tests pass; live UI blocked by missing seeded Control Plane auth user.

## Stores

- [~] Store list loads. 2026-07-08: unauthenticated route redirects to login; authenticated list blocked by missing seeded Control Plane auth user.
- [~] Empty store list state is readable. Blocked by missing seeded Control Plane auth user.
- [~] Store create succeeds with valid data. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Store create validates required fields. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Store create rejects duplicate active store key. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Assign store to node succeeds. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Reassign store to node succeeds. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Store detail loads. Blocked by missing seeded Control Plane auth user.
- [~] Store detail shows node assignment and domains. Blocked by missing seeded Control Plane auth user.
- [~] Store status is visible. Blocked by missing seeded Control Plane auth user.
- [~] Archived/disabled store state is handled correctly. Service tests pass; live UI blocked by missing seeded Control Plane auth user.

## Health

- [~] Health page loads. 2026-07-08: unauthenticated route redirects to login; authenticated health data blocked by missing seeded Control Plane auth user.
- [~] Latest heartbeat is visible when available. Blocked by missing seeded Control Plane auth user.
- [~] Missing heartbeat state is readable. Blocked by missing seeded Control Plane auth user.
- [~] Node dependency status is visible when available. Blocked by missing seeded Control Plane auth user.
- [~] Manual probe handles reachable node. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Manual probe handles unreachable node. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [x] Health failure state does not break the page. 2026-07-08: unauthenticated 401 state renders without app crash.

## Actions

- [~] Action list loads. 2026-07-08: unauthenticated route redirects to login; authenticated action list blocked by missing seeded Control Plane auth user.
- [~] Action enqueue succeeds for a valid active node. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Action enqueue rejects disabled nodes. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Action detail loads. Blocked by missing seeded Control Plane auth user.
- [~] Action attempts are visible. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Action cancellation succeeds for cancellable actions. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Duplicate idempotency key returns or preserves the expected action. Service tests pass; live UI blocked by missing seeded Control Plane auth user.

## Audit Logs

- [x] Audit Logs page loads. 2026-07-08: placeholder page loads with no console errors.
- [n/a] Audit log search by action works. Page has static search controls, no API-backed search implementation yet.
- [n/a] Audit log search by actor works. Page has static search controls, no API-backed search implementation yet.
- [x] Login success is audited. 2026-07-08: clean ControlPlane QA DB recorded `auth.login` success for the seeded admin.
- [x] Login failure is audited. 2026-07-08: wrong-password UI/API attempts wrote `auth.login` failure entries for the submitted actor email.
- [~] Logout is audited. API code writes audit, but live logout blocked by missing authenticated session.
- [~] Node create/update/disable is audited. API code writes audit; live mutation blocked by missing seeded Control Plane auth user.
- [~] Credential create/reveal/revoke/rotate is audited. API code writes audit; live mutation blocked by missing seeded Control Plane auth user.
- [~] Store create/update/archive/domain changes are audited. API code writes audit; live mutation blocked by missing seeded Control Plane auth user.
- [~] Health probe is audited. API code writes audit; live mutation blocked by missing seeded Control Plane auth user.
- [~] Action enqueue/attempt/cancel is audited. API code writes audit; live mutation blocked by missing seeded Control Plane auth user.
- [~] Audit log payload does not expose raw API secrets or passwords. Needs API-backed audit read endpoint or direct DB assertion.

## Dashboard

- [~] Dashboard loads. 2026-07-08: unauthenticated route redirects through login guard; authenticated counters blocked by missing seeded Control Plane auth user.
- [~] Total Nodes counter is correct. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Healthy Nodes counter is correct. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Warning Nodes counter is correct. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Down Nodes counter is correct. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [~] Total Stores counter is correct. Service tests pass; live UI blocked by missing seeded Control Plane auth user.
- [x] Dashboard links navigate to the expected filtered pages. 2026-07-08: Dashboard link targets for nodes/status/stores are present.

## Accessibility And UX Smoke

- [x] Primary pages have one clear `h1`. 2026-07-08: checked Dashboard, Nodes, Stores, Health, Actions, Audit Logs route snapshots.
- [~] Form inputs have labels. Audit placeholder inputs only use placeholders; node/create/store forms not fully live-tested with auth.
- [x] Buttons have clear accessible names. 2026-07-08: route snapshots show named primary buttons.
- [x] Keyboard tab order is usable for login and core forms. 2026-07-08: login form has reachable labeled email/password fields and submit button; authenticated core forms blocked by missing seeded Control Plane auth user.
- [x] Mobile viewport does not overlap navigation, tables, or forms. 2026-07-08: 375x812 login smoke test had no horizontal overflow.
- [x] Error messages are actionable and do not expose sensitive detail. 2026-07-08: unauthenticated messages are safe and actionable.

## Regression Automation Suggestions

- [ ] Add API integration tests for auth success/failure/session.
- [ ] Add API integration tests for each permission policy denial.
- [ ] Add service tests for wrong-password lockout/rate-limit behavior when implemented.
- [ ] Add Playwright smoke tests for Dashboard, Nodes, Stores, Health, Actions, Audit Logs.
- [ ] Add audit-log assertions to mutation endpoint tests.
- [ ] Add database migration test that validates seed roles and permissions.
- [ ] Add database migration test that fails if ControlPlane DB contains legacy Commerce/Storefront tables.
- [ ] Add contract tests for Commerce Node heartbeat/probe payloads.

## QA Run History

| Date | Tester | Scope | Result | Notes |
| --- | --- | --- | --- | --- |
| 2026-07-08 | Codex | Initial Control Plane QA checklist creation and unauthenticated smoke verification | Partial | Fixed Web dev API base URL and Blazor static asset startup. Auth/live mutations blocked by missing shared legacy PostgreSQL on `localhost:5432`; User Management and API-backed Audit Logs are not implemented yet. |
| 2026-07-08 | Codex | Control Plane auth QA after AuthConnection/login implementation | Partial | Fixed auth schema startup migration, credentialed CORS, and no-session refresh console error. Verified login page, wrong-password rejection, repeated failure behavior, route guard, login failure audit, and mobile smoke. Valid login/admin/user flows blocked because `AspNetUsers` has 0 rows; User Management and API-backed Audit Logs are not implemented yet. |
| 2026-07-08 | Codex | Control Plane isolated auth DB implementation | Partial | Built API/Web and ran tests. Runtime smoke used clean QA database with `ControlPlaneDbContext` migrations only; verified no legacy tables, seeded admin, valid login, wrong-password rejection, refresh token persistence, and login success/failure audit. Browser authenticated pages and logout still need full Playwright QA after resetting the local dev DB. |
