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
- [~] Browser console has no unexpected errors on initial load. 2026-07-08: static/runtime errors fixed; protected unauthenticated API calls still produce expected 401 console entries until login flow exists.
- [x] API health endpoint responds. 2026-07-08: verified `GET /api/control-plane/system/info`.

## Open QA Findings

- [!] QA-CP-001: Wrong-password login cannot be fully tested because shared auth `DefaultConnection` points to PostgreSQL `localhost:5432`, which was unavailable during QA. The API request timed out while EF retried the connection.
- [n/a] QA-CP-002: Control Plane Web has no login/logout UI yet, so auth/session UX cannot be tested from the browser.
- [n/a] QA-CP-003: Control Plane User Management UI/API is not implemented yet.
- [n/a] QA-CP-004: Audit Logs page is a static placeholder; action/actor search is not backed by an API yet.
- [~] QA-CP-005: Protected pages show safe auth errors, but unauthenticated API requests still appear as 401 console entries. Re-check once login/route guard is added.

## Auth

- [n/a] Login page/form is reachable. 2026-07-08: no Control Plane login UI exists yet.
- [~] Valid admin login succeeds. 2026-07-08: blocked because shared legacy `DefaultConnection` PostgreSQL on `localhost:5432` was not running.
- [~] Logged-in state is preserved on page refresh. 2026-07-08: blocked by missing login session.
- [~] Current session/profile status is visible or API-verifiable. 2026-07-08: `me` endpoint exists, but no authenticated session was available.
- [~] Logout clears session state. 2026-07-08: blocked by missing login session.
- [!] Wrong password is rejected with a safe error message. 2026-07-08: request timed out because shared auth DB was unavailable; API logs show failed connection to `localhost:5432`.
- [~] Repeated wrong-password attempts are rejected consistently. 2026-07-08: blocked by shared auth DB unavailable.
- [~] Repeated wrong-password attempts do not expose account existence or sensitive detail. 2026-07-08: blocked by shared auth DB unavailable.
- [x] Protected pages redirect or block unauthenticated users. 2026-07-08: Dashboard, Nodes, Stores, Credentials, Health, and Actions show auth error/401 rather than data.
- [~] Expired/invalid token state is handled without a broken UI. 2026-07-08: unauthenticated state handled; expired token not tested.

## Users And Permissions

### Admin User

- [~] Admin role can access Dashboard. Blocked by unavailable shared auth DB.
- [~] Admin role can access Nodes. Blocked by unavailable shared auth DB.
- [~] Admin role can create nodes. Blocked by unavailable shared auth DB.
- [~] Admin role can rotate credentials. Blocked by unavailable shared auth DB.
- [~] Admin role can disable nodes. Blocked by unavailable shared auth DB.
- [~] Admin role can access Stores. Blocked by unavailable shared auth DB.
- [~] Admin role can assign stores to nodes. Blocked by unavailable shared auth DB.
- [~] Admin role can access Health. Blocked by unavailable shared auth DB.
- [n/a] Admin role can access Audit Logs. 2026-07-08: page exists as placeholder, no API-backed audit read implementation yet.

### Standard User

- [~] Standard user can log in. Blocked by unavailable shared auth DB.
- [~] Standard user sees only allowed Control Plane pages. Blocked by unavailable shared auth DB.
- [~] Standard user cannot access admin-only actions through UI. Blocked by unavailable shared auth DB.
- [~] Standard user cannot access admin-only actions through direct API calls. Existing authorization unit tests cover policy denial; live API role test blocked by unavailable shared auth DB.

### Permission Enforcement

- [~] User without `nodes.read` cannot list/view nodes. Unit-covered; live test blocked by unavailable shared auth DB.
- [~] User without `nodes.write` cannot create/update/disable nodes. Unit-covered; live test blocked by unavailable shared auth DB.
- [~] User without `credentials.rotate` cannot create/revoke/rotate API keys. Unit-covered; live test blocked by unavailable shared auth DB.
- [~] User without `stores.read` cannot list/view stores. Unit-covered; live test blocked by unavailable shared auth DB.
- [~] User without `stores.write` cannot create/update/assign/archive stores. Unit-covered; live test blocked by unavailable shared auth DB.
- [~] User without `health.read` cannot view health data. Unit-covered; live test blocked by unavailable shared auth DB.
- [~] User without `actions.read` cannot view/control actions. Unit-covered; live test blocked by unavailable shared auth DB.
- [n/a] User without `audit.read` cannot view audit logs. No API-backed audit read endpoint yet.

### User Management

- [n/a] User list loads. No Control Plane user-management UI/API exists yet.
- [n/a] Create user succeeds. No Control Plane user-management UI/API exists yet.
- [n/a] Assign user role succeeds. No Control Plane user-management UI/API exists yet.
- [n/a] Enable user succeeds. No Control Plane user-management UI/API exists yet.
- [n/a] Disable user succeeds. No Control Plane user-management UI/API exists yet.
- [n/a] Assign permission succeeds. No Control Plane user-management UI/API exists yet.
- [n/a] Remove permission succeeds. No Control Plane user-management UI/API exists yet.
- [~] Disabled user cannot log in. Authorization service tests cover disabled Control Plane profile; live login blocked by unavailable shared auth DB.
- [n/a] User/role/permission changes are audited. No Control Plane user-management mutation flow exists yet.

## Nodes

- [~] Node list loads. 2026-07-08: route loads and shows auth message; authenticated list blocked by unavailable shared auth DB.
- [~] Empty node list state is readable. Blocked by unavailable shared auth DB.
- [~] Node create succeeds with valid data. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Node create validates required fields. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Node create rejects duplicate node key. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Node detail loads. Blocked by unavailable shared auth DB.
- [~] Node detail shows endpoint/status metadata. Blocked by unavailable shared auth DB.
- [~] Rotate API key succeeds. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Rotated API key is shown only once. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Old API key is revoked or inactive after rotation. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Disable node succeeds. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Disabled node is visible as disabled. Blocked by unavailable shared auth DB.
- [~] Disabled node cannot receive write/control operations that require an active node. Service tests pass; live UI blocked by unavailable shared auth DB.

## Stores

- [~] Store list loads. 2026-07-08: route loads and shows auth-gated state; authenticated list blocked by unavailable shared auth DB.
- [~] Empty store list state is readable. Blocked by unavailable shared auth DB.
- [~] Store create succeeds with valid data. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Store create validates required fields. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Store create rejects duplicate active store key. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Assign store to node succeeds. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Reassign store to node succeeds. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Store detail loads. Blocked by unavailable shared auth DB.
- [~] Store detail shows node assignment and domains. Blocked by unavailable shared auth DB.
- [~] Store status is visible. Blocked by unavailable shared auth DB.
- [~] Archived/disabled store state is handled correctly. Service tests pass; live UI blocked by unavailable shared auth DB.

## Health

- [~] Health page loads. 2026-07-08: route loads and shows auth-gated state; authenticated health data blocked by unavailable shared auth DB.
- [~] Latest heartbeat is visible when available. Blocked by unavailable shared auth DB.
- [~] Missing heartbeat state is readable. Blocked by unavailable shared auth DB.
- [~] Node dependency status is visible when available. Blocked by unavailable shared auth DB.
- [~] Manual probe handles reachable node. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Manual probe handles unreachable node. Service tests pass; live UI blocked by unavailable shared auth DB.
- [x] Health failure state does not break the page. 2026-07-08: unauthenticated 401 state renders without app crash.

## Actions

- [~] Action list loads. 2026-07-08: route loads and shows auth-gated state; authenticated action list blocked by unavailable shared auth DB.
- [~] Action enqueue succeeds for a valid active node. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Action enqueue rejects disabled nodes. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Action detail loads. Blocked by unavailable shared auth DB.
- [~] Action attempts are visible. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Action cancellation succeeds for cancellable actions. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Duplicate idempotency key returns or preserves the expected action. Service tests pass; live UI blocked by unavailable shared auth DB.

## Audit Logs

- [x] Audit Logs page loads. 2026-07-08: placeholder page loads with no console errors.
- [n/a] Audit log search by action works. Page has static search controls, no API-backed search implementation yet.
- [n/a] Audit log search by actor works. Page has static search controls, no API-backed search implementation yet.
- [~] Login success is audited. API code writes audit, but live login blocked by unavailable shared auth DB.
- [!] Login failure is audited. 2026-07-08: live wrong-password request timed out before app-level audit because shared auth DB was unavailable.
- [~] Logout is audited. API code writes audit, but live logout blocked by missing authenticated session.
- [~] Node create/update/disable is audited. API code writes audit; live mutation blocked by unavailable shared auth DB.
- [~] Credential create/reveal/revoke/rotate is audited. API code writes audit; live mutation blocked by unavailable shared auth DB.
- [~] Store create/update/archive/domain changes are audited. API code writes audit; live mutation blocked by unavailable shared auth DB.
- [~] Health probe is audited. API code writes audit; live mutation blocked by unavailable shared auth DB.
- [~] Action enqueue/attempt/cancel is audited. API code writes audit; live mutation blocked by unavailable shared auth DB.
- [~] Audit log payload does not expose raw API secrets or passwords. Needs API-backed audit read endpoint or direct DB assertion.

## Dashboard

- [~] Dashboard loads. 2026-07-08: route loads and shows auth-gated state; authenticated counters blocked by unavailable shared auth DB.
- [~] Total Nodes counter is correct. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Healthy Nodes counter is correct. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Warning Nodes counter is correct. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Down Nodes counter is correct. Service tests pass; live UI blocked by unavailable shared auth DB.
- [~] Total Stores counter is correct. Service tests pass; live UI blocked by unavailable shared auth DB.
- [x] Dashboard links navigate to the expected filtered pages. 2026-07-08: Dashboard link targets for nodes/status/stores are present.

## Accessibility And UX Smoke

- [x] Primary pages have one clear `h1`. 2026-07-08: checked Dashboard, Nodes, Stores, Health, Actions, Audit Logs route snapshots.
- [~] Form inputs have labels. Audit placeholder inputs only use placeholders; node/create/store forms not fully live-tested with auth.
- [x] Buttons have clear accessible names. 2026-07-08: route snapshots show named primary buttons.
- [n/a] Keyboard tab order is usable for login and core forms. No login form exists yet.
- [ ] Mobile viewport does not overlap navigation, tables, or forms.
- [x] Error messages are actionable and do not expose sensitive detail. 2026-07-08: unauthenticated messages are safe and actionable.

## Regression Automation Suggestions

- [ ] Add API integration tests for auth success/failure/session.
- [ ] Add API integration tests for each permission policy denial.
- [ ] Add service tests for wrong-password lockout/rate-limit behavior when implemented.
- [ ] Add Playwright smoke tests for Dashboard, Nodes, Stores, Health, Actions, Audit Logs.
- [ ] Add audit-log assertions to mutation endpoint tests.
- [ ] Add database migration test that validates seed roles and permissions.
- [ ] Add contract tests for Commerce Node heartbeat/probe payloads.

## QA Run History

| Date | Tester | Scope | Result | Notes |
| --- | --- | --- | --- | --- |
| 2026-07-08 | Codex | Initial Control Plane QA checklist creation and unauthenticated smoke verification | Partial | Fixed Web dev API base URL and Blazor static asset startup. Auth/live mutations blocked by missing shared legacy PostgreSQL on `localhost:5432`; User Management and API-backed Audit Logs are not implemented yet. |
