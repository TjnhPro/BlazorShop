# BlazorShop Storefront V2 Local Auth Todo

## Goal

Implement login and register directly inside `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`.

This keeps Storefront V2 independent from legacy `BlazorShop.Web` while still reusing Commerce Node auth APIs and existing Storefront layout/CSS. Each future store can render its own login/register experience without sharing a single external customer app.

## Current State

- Storefront V2 already renders its own public layout and account menu.
- `/signin`, `/register`, and anonymous `/checkout` currently redirect to `ClientApp:BaseUrl`.
- `StorefrontClientAppUrlResolver` still uses the legacy `adminclient` service-discovery name before `ClientApp:BaseUrl`.
- Commerce Node already owns Storefront auth API:
  - `POST /api/internal/auth/create`
  - `POST /api/internal/auth/login`
  - `POST /api/internal/auth/refresh-token`
  - `POST /api/internal/auth/logout`
- `StorefrontSessionResolver` already calls `api/internal/auth/refresh-token` and copies `Set-Cookie` headers from Commerce Node back to the Storefront response.

## Decision Summary

Use Storefront V2 local pages for customer auth:

- `/signin` renders a Storefront V2 sign-in page.
- `/register` renders a Storefront V2 registration page.
- Form submit goes through Storefront V2 server endpoints, not browser-to-CommerceNode JavaScript.
- Storefront V2 calls Commerce Node `api/internal/auth/*` server-side.
- Storefront V2 copies Commerce Node `Set-Cookie` headers into its own HTTP response so the browser receives the refresh cookie.
- No new database tables.
- No new auth database.
- No legacy `BlazorShop.Web` dependency.

## Not In Scope

- Rebuilding customer account dashboard.
- Rebuilding full checkout UI.
- Multi-store domain isolation beyond preserving route/config extension points.
- Replacing Commerce Node auth service logic.
- Adding social login, email confirmation UI, password reset, or MFA.
- Changing Commerce Node database schema unless implementation exposes a missing column/constraint.

## Architecture

```text
Browser
  -> StorefrontV2 /signin, /register
      -> StorefrontAuthClient
          -> CommerceNode API /api/internal/auth/login|create
      <- copies Set-Cookie from CommerceNode
  -> StorefrontV2 pages read session through StorefrontSessionResolver
      -> CommerceNode API /api/internal/auth/refresh-token
```

Boundary rule:

- Storefront V2 owns UI, layout, validation display, return URL behavior, and cookie propagation.
- Commerce Node owns credential validation, customer identity, refresh token creation, JWT issuance, and auth persistence.

## Database Plan

No new database work.

Use existing Commerce Node tables in `CommerceNodeDbContext`:

- `AspNetUsers`
- `AspNetRoles`
- `AspNetUserRoles`
- `AspNetUserClaims`
- `AspNetUserLogins`
- `AspNetUserTokens`
- `RefreshTokens`

Verification:

- Confirm register creates customer identity in Commerce Node DB only.
- Confirm login creates/rotates refresh token in Commerce Node DB only.
- Confirm Control Plane DB is not touched.
- Confirm legacy `AppDbContext` is not used.

## API Plan

### Reuse Existing Commerce Node APIs

No API route rename in this phase.

Storefront V2 should call:

| Storefront action | Commerce Node endpoint | Notes |
| --- | --- | --- |
| Register | `POST api/internal/auth/create` | Uses existing `CreateUser` DTO and API envelope. |
| Login | `POST api/internal/auth/login` | Uses existing `LoginUser` DTO; response includes JWT in data and refresh cookie in `Set-Cookie`. |
| Refresh session | `POST api/internal/auth/refresh-token` | Already used by `StorefrontSessionResolver`. |
| Logout | `POST api/internal/auth/logout` | Add local Storefront logout route if needed for account menu completeness. |

### New Storefront V2 Client

Add a small Storefront-side auth client, separate from catalog read client:

- `IStorefrontAuthClient`
- `StorefrontAuthClient`

Responsibilities:

- POST JSON to Commerce Node auth routes.
- Parse the standard API response envelope.
- Return `Success`, `Message`, `LoginResponse`, and raw `Set-Cookie` headers.
- Treat unreachable Commerce Node as a safe UI error, not a crash.
- Avoid direct dependency on legacy `BlazorShop.Web` auth services.

### Cookie Handling

Important implementation detail:

- The browser submits the login form to Storefront V2.
- Storefront V2 calls Commerce Node using server-side `HttpClient`.
- Commerce Node returns `Set-Cookie`.
- Storefront V2 must copy Commerce Node `Set-Cookie` headers into the Storefront response.

Do not rely on browser-side calls to Commerce Node for MVP. That would require cross-origin credential handling and would fight the current internal API boundary.

## UI Plan

### Login Page

Route:

- `GET /signin`

Layout:

- Use the existing Storefront V2 shell/header/footer.
- Use Storefront classes and Tailwind utilities already used by catalog/cart pages.
- Do not reuse legacy `BlazorShop.Web` page components directly.
- Reuse text/input structure only where it helps.

Fields:

- Email
- Password

States:

- Empty form
- Submitting
- Invalid credentials
- Commerce Node unavailable
- Already signed in
- Success redirect

Behavior:

- If already authenticated, redirect to `returnUrl` when safe, otherwise `/`.
- On successful login, copy refresh cookie and redirect to safe `returnUrl` or `/`.
- On failure, show API `message`.
- Never show raw exception text.

### Register Page

Route:

- `GET /register`

Fields:

- Full name
- Email
- Password
- Confirm password

States:

- Empty form
- Client-side/server-side validation errors
- Duplicate email or API validation failure
- Commerce Node unavailable
- Registration success

Behavior:

- Submit through Storefront V2.
- On success, redirect to `/signin?registered=1&returnUrl=...` or show success message and link to sign in.
- Use API `message` where available.
- Do not auto-login unless Commerce Node API explicitly returns a login token in a later phase.

### Account Menu Follow-up

The anonymous account menu already links to local `/signin` and `/register`.

For authenticated users, current links still resolve through `ClientAppUrlResolver`:

- `My Account`
- `Admin Panel`

For this auth phase:

- Add local logout if implementation touches account menu.
- Keep account dashboard as follow-up unless needed to avoid a broken link.
- Prefer not to route customers back to legacy Web after this phase.

## Route Plan

Replace redirect behavior:

| Current route | Current behavior | New behavior |
| --- | --- | --- |
| `GET /signin` | Redirects to client app login | Render Storefront V2 login page |
| `GET /register` | Redirects to client app register | Render Storefront V2 register page |
| `GET /checkout` anonymous | Redirects to client app login checkout | Redirect to `/signin?returnUrl=/checkout` |
| `GET /checkout` authenticated | Redirects to client app checkout | Keep existing behavior temporarily or defer to checkout phase |

Recommended MVP:

- Change anonymous checkout to local login.
- Leave authenticated checkout behavior explicitly documented if checkout is not implemented yet.
- Add QA item so this cannot be mistaken as final checkout completion.

## Phase 1 - Storefront Auth Client

- [x] Add `StorefrontAuthClient` for Commerce Node auth API calls.
- [x] Add DTO/result wrappers for auth response and `Set-Cookie` propagation.
- [x] Reuse existing `CreateUser`, `LoginUser`, and `LoginResponse` models where practical.
- [x] Add tests for:
  - [x] login success parses envelope and captures cookie headers
  - [x] login failure returns API message
  - [x] register success returns API message
  - [x] Commerce Node unavailable returns safe failure

Stop gate:

- Storefront V2 can call auth endpoints through a typed client without rendering pages yet. 2026-07-09: `StorefrontV2AuthClientTests` passed 4/4.

## Phase 2 - Local Login Page

- [ ] Remove `MapGet(StorefrontRoutes.SignIn, redirect...)`.
- [ ] Add Storefront V2 login page at `/signin`.
- [ ] Add POST handler or static SSR form handling for login submit.
- [ ] Copy Commerce Node `Set-Cookie` headers into Storefront response.
- [ ] Add safe return URL handling.
- [ ] Add already-authenticated redirect behavior.
- [ ] Show API message for invalid credentials.
- [ ] Update anonymous checkout redirect to `/signin?returnUrl=/checkout`.
- [ ] Add tests for:
  - `/signin` returns 200
  - invalid login stays on page with safe message
  - successful login sets refresh cookie
  - successful login redirects to safe return URL
  - unsafe absolute return URL is rejected or normalized
  - anonymous `/checkout` redirects to local `/signin`

Stop gate:

- Customer can sign in from Storefront V2 without touching legacy Web.

## Phase 3 - Local Register Page

- [ ] Remove `MapGet(StorefrontRoutes.Register, redirect...)`.
- [ ] Add Storefront V2 register page at `/register`.
- [ ] Add POST handler or static SSR form handling for register submit.
- [ ] Display validation/API errors using Storefront styling.
- [ ] On success, route to `/signin` with success state.
- [ ] Preserve safe `returnUrl` through register -> sign-in.
- [ ] Add tests for:
  - `/register` returns 200
  - password mismatch is blocked before API call
  - duplicate/invalid registration shows API message
  - successful registration creates user in Commerce Node DB
  - no legacy Web route is used

Stop gate:

- Customer can register from Storefront V2 and then sign in from Storefront V2.

## Phase 4 - Logout And Account Menu Cleanup

- [ ] Add local `/logout` route or POST action.
- [ ] Call `api/internal/auth/logout`.
- [ ] Delete/copy expired refresh cookie back to browser.
- [ ] Update account menu authenticated actions:
  - local logout
  - avoid sending customer to legacy `BlazorShop.Web`
  - keep `My Account` as local placeholder or hide until account page exists
- [ ] Add tests for:
  - logout clears cookie
  - account menu shows signed-in state after login
  - account menu returns anonymous state after logout

Stop gate:

- Storefront V2 auth lifecycle is local: register, login, session display, logout.

## Phase 5 - QA Checklist Update

Update `QA-StorefrontV2.todo.md`:

- [ ] `/signin` renders Storefront V2 page.
- [ ] `/register` renders Storefront V2 page.
- [ ] Login success sets `__Host-blazorshop-refresh`.
- [ ] Login wrong password shows safe API message.
- [ ] Register success creates Commerce Node customer.
- [ ] Duplicate register shows safe API message.
- [ ] Anonymous checkout redirects to local `/signin?returnUrl=/checkout`.
- [ ] Login return URL does not allow open redirect.
- [ ] Logout clears session.
- [ ] Browser console has no unexpected errors.
- [ ] No request goes to legacy `BlazorShop.Web`.
- [ ] No request goes to legacy `BlazorShop.API`.

## QA Commands

```powershell
docker compose -f compose.commercenode.yml up -d
dotnet ef database update --project BlazorShop.Infrastructure/BlazorShop.Infrastructure.csproj --startup-project BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --context CommerceNodeDbContext
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~PresentationV2.Storefront"
dotnet test BlazorShop.sln --no-restore
```

Browser QA:

- Start Commerce Node API on `http://localhost:5180`.
- Start Storefront V2 on `http://localhost:18598`.
- Use Playwright to verify `/signin`, `/register`, failed login, successful login, account menu, logout, and checkout return URL.

## Risk Review

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Cookie from Commerce Node not reaching browser | Login appears successful but session remains anonymous | Storefront V2 must copy `Set-Cookie` headers from Commerce Node into its response. |
| Open redirect through `returnUrl` | Security issue | Only allow root-relative local paths. Reject absolute URLs and protocol-relative URLs. |
| Static SSR form wiring breaks validation | Login/register unusable | Prefer explicit POST endpoints or proven static SSR form pattern, then test with browser. |
| Auth links still point to legacy Web | V2 independence incomplete | Remove `/signin` and `/register` redirects; audit account menu and checkout links. |
| API unavailable shows exception | Bad UX | Auth client returns safe failure message and page renders normally. |

## Decision Audit Trail

| # | Decision | Classification | Rationale | Rejected |
|---|---|---|---|---|
| 1 | Implement login/register pages inside Storefront V2 | Accepted direction | Matches per-store future UX and removes dependency on legacy Web auth UI. | Redirect to `BlazorShop.Web` or Commerce Node API. |
| 2 | Keep Commerce Node as auth API backend | Auto-decided | Auth persistence and token logic already live there; no DB or service rewrite needed. | Move auth logic into Storefront V2. |
| 3 | Use server-side Storefront V2 form submit | Auto-decided | Preserves internal API boundary and avoids browser CORS/credential complexity. | Browser JavaScript POST directly to Commerce Node. |
| 4 | No DB changes in this phase | Auto-decided | Existing Commerce Node auth tables satisfy MVP login/register. | Add new auth tables or new auth database. |
| 5 | Defer full account dashboard and checkout UI | Scope control | Login/register can ship independently; checkout/account are adjacent but larger workflows. | Rebuild account and checkout in same phase. |

## Completion Definition

This plan is complete when:

- Storefront V2 `/signin` and `/register` are real pages.
- A customer can register and log in without legacy Web.
- The refresh cookie is set and Storefront session state becomes authenticated.
- Anonymous checkout uses local login instead of external client app login.
- Tests and browser QA prove no dependency on legacy `BlazorShop.Web` or legacy `BlazorShop.API`.
