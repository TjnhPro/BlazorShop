# Production Runbook

## Purpose

Use this runbook together with the architecture docs before promoting BlazorShop to a real production environment.

Important current-state note: production deployment is V2 canonical. Active runtime work lives under `BlazorShop.PresentationV2/*` and follows the boundaries in [docs/architecture](architecture/README.md).

Active V2 has these deployable surfaces with different configuration ownership:

- Control Plane API: platform auth, permissions, users, nodes, stores, credentials, actions, health, audit, and gateway calls to Commerce Node.
- Control Plane Web: authenticated admin/control Blazor WebAssembly UI; calls Control Plane API only.
- Commerce Node API: node-local ecommerce admin/control APIs, Storefront APIs, task orchestration, media, deployment support, and Commerce Node database migration.
- Storefront V2: server-side public storefront, public canonical/discovery origin, account/cart/checkout forms, media proxy routes, and storefront-to-Commerce Node Storefront API calls.

Use the active V2 example files under `BlazorShop.PresentationV2/*/appsettings.Production.example.json` and verify production config against [Deployment And Local Run](architecture/07-deployment-and-local-run.md), [Runtime Boundaries](architecture/03-runtime-boundaries.md), and [Data Ownership](architecture/04-data-ownership.md) before release.

## Replace the placeholders

- `https://shop.example.com`: the public storefront origin.
- `https://account.shop.example.com`: the authenticated Web client origin used for confirmation links, password-reset links, Stripe success/cancel redirects, and storefront handoff targets.
- `https://api.shop.example.com`: the public API origin used for JWT issuer and audience if you keep origin-based token settings.
- `10.0.0.10`: the exact IP of the reverse proxy or ingress hop directly in front of the API.
- `<set-in-secret-store-or-env>`: values that must come from a secret manager or environment variables, never from source control.

If you deploy the storefront and authenticated client under the same origin with path-based routing, set both public URLs to that same origin. If you split them by subdomain, keep the storefront and client-app URLs distinct and configure them explicitly.

## Forwarded Headers Profiles

### No reverse proxy in front of the API

Use this when the app receives the real client connection directly.

```json
"ForwardedHeaders": {
  "Enabled": false,
  "KnownProxies": [],
  "KnownNetworks": [],
  "ForwardLimit": 1
}
```

### Single reverse proxy or ingress hop

Use this when one trusted proxy sits directly in front of the API.

```json
"ForwardedHeaders": {
  "Enabled": true,
  "KnownProxies": [
    "10.0.0.10"
  ],
  "KnownNetworks": [],
  "ForwardLimit": 1
}
```

### Proxy subnet or load balancer network

Use this when the last trusted hop can come from a small internal CIDR range.

```json
"ForwardedHeaders": {
  "Enabled": true,
  "KnownProxies": [],
  "KnownNetworks": [
    "10.0.0.0/24"
  ],
  "ForwardLimit": 1
}
```

Do not trust broad public ranges. Only trust the exact proxy IPs or CIDR blocks you own.

## Environment Variable Mapping

Use these names if your platform injects configuration via environment variables instead of an appsettings file.

```text
ConnectionStrings__ControlPlaneConnection=Host=controlplane-postgres;Port=5432;Database=blazorshop_controlplane;Username=blazorshop_controlplane;Password=<secret>
ConnectionStrings__CommerceNodeConnection=Host=commercenode-postgres;Port=5432;Database=blazorshop_commerce_node;Username=blazorshop_commerce_node;Password=<secret>
JWT__Key=<secret>
JWT__Issuer=https://api.shop.example.com
JWT__Audience=https://api.shop.example.com
ClientApp__BaseUrl=https://account.shop.example.com
ControlPlane__Database__MigrateOnStartup=false
CommerceNode__Database__MigrateOnStartup=false
CommerceNode__DataProtection__KeyRingPath=/app/runtime/data-protection-keys
Runtime__Health__ExposeInProduction=true
Api__BaseUrl=https://commerce-api.shop.example.com/api/
Api__StoreKey=default
PublicUrl__BaseUrl=https://shop.example.com/
Runtime__RateLimiting__Auth__PermitLimit=5
Runtime__RateLimiting__Auth__WindowSeconds=60
Runtime__RateLimiting__Auth__QueueLimit=0
```

Standalone storefront hosts have a separate runtime contract:

```text
Api__BaseUrl=https://api.shop.example.com/api/
ClientApp__BaseUrl=https://account.shop.example.com
PublicUrl__BaseUrl=https://shop.example.com
```

Outside `Development`, the storefront now fails startup unless:

- `Api:BaseUrl` is configured as an absolute URL, or Aspire service discovery provides `Services:apiservice:*`
- `ClientApp:BaseUrl` is configured as an absolute URL, or Aspire service discovery provides `Services:adminclient:*`
- `PublicUrl:BaseUrl` is configured as an absolute URL

For active V2 Commerce Node email, do not put store SMTP credentials in Storefront V2 config. Transactional customer messages are queued in Commerce Node and delivered with store-scoped SMTP settings managed through Control Plane.

Production V2 operators must configure SMTP per active store before public traffic:

- Open Control Plane Web, then `Commerce Admin -> Email`.
- Select the store and verify the effective SMTP status.
- Set `Enabled`, host, port, SSL mode, username if required, write-only password, from email, from display name, and optional reply-to.
- Use the store-scoped test-send action before enabling account recovery or checkout release gates.
- Verify queued-message delivery through the same Control Plane page without exposing SMTP password or rendered reset-token bodies.

Legacy `EmailSettings:*` remains a legacy/bootstrap compatibility surface. For V2 production, keep `CommerceNode:EmailTransport:AllowGlobalEmailSettingsFallback=false` unless you are intentionally running a transition environment and have documented the risk.

## Auth and Email Production Notes

- Production auth assumes `Identity:RequireConfirmedAccount=true` and `Identity:RequireConfirmedEmail=true` unless you intentionally override both.
- Confirmation emails and password-reset style links depend on `ClientApp:BaseUrl` because they link back into the authenticated Web client, not the SSR storefront.
- Stripe success and cancel URLs also depend on `ClientApp:BaseUrl`; do not point that value at the public storefront domain unless the authenticated client is actually hosted there.
- Refresh-token cookies remain `HttpOnly` and `Secure`. `SameSite=Strict` is correct for the standard `shop.example.com` / `account.shop.example.com` / `api.shop.example.com` model because those subdomains are cross-origin but still same-site.
- V2 password recovery and order placed emails must go through queued messages, not synchronous SMTP inside account recovery or checkout/order transactions.
- SMTP delivery failures for queued V2 messages must leave the source command intact and move the queued message into a retry/failure state with an actionable error code such as `message_delivery.smtp_not_configured`.
- SMTP failure handling must not log SMTP passwords, reset tokens, node credentials, or rendered reset-token email bodies.

## V2 Store SMTP Operations

Local capture setup:

1. Start V2 locally with `.\scripts\run-v2-local.ps1 -StopExisting`.
2. Confirm `compose.commercenode.yml` started Mailpit on SMTP port `1025` and inbox/API port `8025`.
3. Open `http://localhost:8025` or use Mailpit API `http://localhost:8025/api/v1/messages`.
4. On a fresh local Commerce Node database, development seed configures `default` and `qa-s2` store email settings in capture mode with different sender addresses. After the core QA fixture exists, startup seeding exits without overwriting existing store runtime or store email settings; use a clean database when a full fixture reset is required.
5. Run `.\scripts\qa\run-storefront-email-recovery-e2e.ps1` and `.\scripts\qa\run-storefront-order-email-e2e.ps1` before public release checks that depend on email.

Staging capture setup:

- Use a Mailpit/MailHog-style sandbox only in staging or test environments.
- Configure store SMTP through Control Plane, not Storefront env files.
- Keep `CommerceNode:EmailTransport:CaptureModeAllowed=true` only for the staging/test runtime that intentionally captures email.
- Keep customer fixtures synthetic and clear or filter the capture inbox by unique test email/reference before each Playwright run.

Production secret protection:

- Persist ASP.NET Core Data Protection keys for `BlazorShop.CommerceNode.API` outside the database and outside checked-in config.
- Set `CommerceNode:DataProtection:KeyRingPath` or `CommerceNode__DataProtection__KeyRingPath` to the mounted key-ring directory; the V2 production compose uses `/app/runtime/data-protection-keys`.
- Protect the key ring with the platform secret store or encrypted volume controls.
- Do not rotate or delete Data Protection keys until all encrypted store SMTP password values that depend on old keys have been rotated.
- In multi-instance production, all Commerce Node API instances that read the same Commerce Node database must share the same protected key ring.

Queued message inspection and retry:

- Use Control Plane Web `Commerce Admin -> Email` to inspect message templates and queued messages for the selected store.
- Use queued-message detail for status, error code, attempt count, template key, recipient, and delivery metadata only.
- Do not expose rendered reset-password bodies, reset tokens, idempotency keys, SMTP passwords, or protected secret material in admin detail.
- Retry a failed or waiting-retry message only after the store SMTP status is ready or the upstream SMTP outage has been corrected.
- Cancel messages that should not be sent after policy, template, recipient, or store-status review.

Actionable SMTP errors:

| Error | Likely cause | Operator action |
| --- | --- | --- |
| `message_delivery.smtp_not_configured` | Store email settings are disabled, missing, incomplete, or password was cleared. | Open Control Plane email settings for the store, complete SMTP fields, send a test email, then retry queued messages. |
| `message_delivery.smtp_send_failed` | SMTP host rejected connection/auth/message or network path failed. | Check host, port, SSL mode, username/password, sender policy, firewall, and provider logs, then retry. |
| `message_delivery.store_not_found` | Queued message references a deleted or unavailable store. | Investigate store lifecycle and queue data before retrying; do not fallback to another store sender. |
| `message_delivery.template_missing` | Required transactional template is missing or disabled. | Reset the template to default or restore a valid store override, preview it, then retry. |

## Web Runtime Config

Control Plane Web receives `CONTROLPLANE_API_BASE_URL` through its container entrypoint. Storefront V2 receives `Api:BaseUrl`, `Api:StoreKey`, and `PublicUrl:BaseUrl` through appsettings or environment variables. Do not rely on legacy WebAssembly appsettings from removed Presentation projects.

## Edge TLS and HSTS Ownership

The repository's standard container stack is designed around a public HTTPS edge and private HTTP container-to-container hops.

- Terminate TLS at the public ingress, load balancer, reverse proxy, or CDN that faces the Internet.
- Emit HSTS only on that public HTTPS edge.
- Keep the refresh-token cookie `HttpOnly` and `Secure`; the API now rotates it server-side and no longer relies on JavaScript to persist refresh tokens.
- Keep the bundled Web container on internal HTTP only.
- Keep `Runtime__Security__EnableHsts=false` and `Runtime__Security__EnableHttpsRedirection=false` when the API is only reachable through the internal Web proxy.
- Re-enable both API settings only if you expose the API itself directly over HTTPS and there is no outer proxy layer already responsible for redirects and HSTS.

Use `Runtime__Security__RefreshTokenCookieSameSite=Strict` for the standard same-site deployment model shown in this repository, including `shop.example.com` and `api.shop.example.com`. Only relax it to `None` if the browser frontend truly lives on a different site and must send the refresh cookie cross-site; if you do that, keep HTTPS and browser credentials enabled end to end.

## V2 Standard Container Deployment

Use the repository V2 Dockerfiles together with `compose.production.yml`.

Required environment variables before startup:

- `BLAZORSHOP_CONTROLPLANE_DB_PASSWORD`
- `BLAZORSHOP_COMMERCENODE_DB_PASSWORD`
- `BLAZORSHOP_CONTROLPLANE_JWT_KEY`
- `BLAZORSHOP_COMMERCENODE_JWT_KEY`
- `BLAZORSHOP_CONTROLPLANE_API_BASE_URL`
- `BLAZORSHOP_CONTROLPLANE_WEB_BASE_URL`
- `BLAZORSHOP_COMMERCENODE_API_BASE_URL`
- `BLAZORSHOP_COMMERCENODE_NODE_KEY`
- `BLAZORSHOP_COMMERCENODE_NODE_SECRET`
- `BLAZORSHOP_STOREFRONT_BASE_URL`
- `BLAZORSHOP_STOREFRONT_STORE_KEY`

Optional compose overrides:

- `BLAZORSHOP_CONTROLPLANE_API_PORT`
- `BLAZORSHOP_CONTROLPLANE_WEB_PORT`
- `BLAZORSHOP_COMMERCENODE_API_PORT`
- `BLAZORSHOP_COMMERCENODE_NGINX_PORT`
- `BLAZORSHOP_STOREFRONT_PORT`
- `BLAZORSHOP_CONTROLPLANE_MIGRATE_ON_STARTUP`
- `BLAZORSHOP_COMMERCENODE_MIGRATE_ON_STARTUP`

Start the stack with:

```powershell
docker compose -f compose.production.yml up -d --build
```

The production compose file uses required-variable expansion. `docker compose -f compose.production.yml config` and `docker compose -f compose.production.yml up` fail immediately if any required secret or public URL variable is unset or blank.

Notes:

- The compose stack runs Control Plane API/Web, Commerce Node API, Commerce Node Nginx/imgproxy, Storefront V2, and separate Control Plane/Commerce Node PostgreSQL databases.
- Storefront V2 is the public SSR shopping surface. Its `PublicUrl:BaseUrl` must match the real public storefront origin.
- Control Plane Web calls only Control Plane API.
- Storefront-facing media is served through Commerce Node Nginx/imgproxy and Commerce Node API.
- Commerce Node Data Protection keys persist in `commercenode_data_protection_keys`.
- The compose example disables API-level HSTS and HTTPS redirection because the API sits behind the Web proxy. If you expose the API directly over HTTPS instead, re-enable both.
- PayPal is intentionally disabled in this build until a real provider integration and capture flow are implemented.

## V2 Startup Database Migration

The active V2 runtimes use startup EF Core migration for MVP. This applies to:

- `BlazorShop.ControlPlane.API` with `ControlPlaneDbContext` and `ControlPlaneConnection`.
- `BlazorShop.CommerceNode.API` with `CommerceNodeDbContext` and `CommerceNodeConnection`.

Enable startup migration intentionally through environment variables:

```text
ControlPlane__Database__MigrateOnStartup=true
ControlPlane__Database__FailStartupOnMigrationError=true
ControlPlane__Database__LogMigrationState=true
CommerceNode__Database__MigrateOnStartup=true
CommerceNode__Database__FailStartupOnMigrationError=true
CommerceNode__Database__LogMigrationState=true
```

Manual production workflow:

1. Backup the target PostgreSQL database with the platform's backup tool or `pg_dump`.
2. Stop or scale down extra API instances so one runtime owns migration for that database.
3. Pull or deploy the latest API image.
4. Start the API and watch startup logs for migration state, pending migration names, and completion.
5. Only open traffic after the API reaches ready/healthy state.

Failure behavior:

- Migration errors must fail API startup when `FailStartupOnMigrationError=true`.
- Do not keep a container running against a partially migrated or incompatible schema.
- Restore the DB backup or roll back the API image manually after reviewing logs.

Review long or data-heavy migrations before release. Startup migration is acceptable for MVP, but a migration that rewrites large tables can delay readiness and should be handled as an explicit maintenance operation.

## Logging and Failure Visibility

- V2 API startup/runtime logs go to container stdout/stderr by default.
- Storefront runtime signals for discovery, redirect, and public catalog failures are emitted as structured log event names such as `public.discovery.sitemap_failure`, `public.redirect.invalid_target_blocked`, and `public.product.service_unavailable`.
- SMTP failures are logged with the exception details and no longer fail silently in confirmation-required auth paths.
- If storefront handoff routes start returning `503`, check `ClientApp:BaseUrl` on the storefront host first. That is the explicit failure mode when neither standalone config nor service discovery can resolve the authenticated client origin.

## Deployment Smoke Checks

Use these as a minimum post-deploy checklist in addition to the test suites:

1. Fetch the storefront home page and confirm `200` from the public storefront origin.
2. Fetch `/robots.txt` and `/sitemap.xml` from the storefront origin and confirm `200` plus the expected public URLs.
3. Fetch the authenticated client login page from the client-app origin and confirm `200`.
4. Request storefront `/checkout` without auth and confirm it redirects to the configured client-app login handoff.
5. If the API is exposed directly, fetch the readiness path and confirm `200`.

PowerShell examples:

```powershell
Invoke-WebRequest -Uri https://shop.example.com/ | Select-Object StatusCode, StatusDescription
Invoke-WebRequest -Uri https://shop.example.com/robots.txt | Select-Object StatusCode, StatusDescription
Invoke-WebRequest -Uri https://shop.example.com/sitemap.xml | Select-Object StatusCode, StatusDescription
Invoke-WebRequest -Uri https://account.shop.example.com/authentication/login/account | Select-Object StatusCode, StatusDescription
Invoke-WebRequest -Uri https://shop.example.com/checkout -MaximumRedirection 0 -SkipHttpErrorCheck | Select-Object StatusCode, Headers
```

## Image Update Cadence

The production Dockerfiles and compose file pin exact image tags so base-image updates stay explicit and reviewable.

- Review the pinned .NET, nginx, and PostgreSQL tags at least monthly.
- Review them immediately after vendor security advisories or when CI/container scanning flags a base-image issue.
- Update the pinned tags in `compose.production.yml` and the active V2 Dockerfiles in the same change.
- Re-run the full release verification after every image bump, even when the application code is unchanged.

Current pinned images in this repository:

- PostgreSQL: `postgres:16.13-alpine3.23`
- API build image: `mcr.microsoft.com/dotnet/sdk:10.0.202-noble`
- API runtime image: `mcr.microsoft.com/dotnet/aspnet:10.0.6-noble`
- Web build image: `mcr.microsoft.com/dotnet/sdk:10.0.202-noble`
- Web runtime image: `nginx:1.27.5-alpine3.21`

## Storefront SEO Runtime Signals

The SSR storefront now emits structured SEO runtime events for high-signal public-route outcomes only. Watch the event name field in application logs for these values:

- `public.product.resolved`
- `public.product.not_found`
- `public.product.service_unavailable`
- `public.category.resolved`
- `public.category.not_found`
- `public.category.service_unavailable`
- `public.redirect.resolved`
- `public.redirect.loop_blocked`
- `public.redirect.chain_blocked`
- `public.redirect.invalid_target_blocked`
- `public.discovery.sitemap_failure`
- `public.discovery.robots_failure`

Treat these as likely SEO regression signals:

- repeated `public.product.service_unavailable` or `public.category.service_unavailable`: published catalog routes are unstable or the API is degraded
- spikes in `public.product.not_found` or `public.category.not_found`: broken internal links, bad redirects, missing published content, or stale indexed URLs
- any `public.redirect.loop_blocked`, `public.redirect.chain_blocked`, or `public.redirect.invalid_target_blocked`: redirect data needs immediate review before crawlers get trapped or lose canonical consolidation
- any `public.discovery.sitemap_failure` or `public.discovery.robots_failure`: discovery documents are unhealthy and crawl/indexation can regress quickly

First inspection steps:

1. Check whether the failures cluster on one slug, one category, or all published routes.
2. Check the API and storefront deployment health at the same timestamp.
3. For redirect anomalies, inspect the active redirect records for the logged source and target paths.
4. For discovery failures, fetch `/sitemap.xml` and `/robots.txt` directly from the public origin and confirm the current response status, cache headers, and body.

## Storefront SEO Smoke Checks

The test project now includes a live-storefront SEO smoke suite tagged with `Category=SeoSmoke`. It is designed for fast post-deploy or pre-release verification against a running SSR storefront without browser automation.

Run it with:

```powershell
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj -c Release --filter "Category=SeoSmoke"
```

Required environment variable:

- `BLAZORSHOP_SEO_SMOKE_BASE_URL`: absolute storefront base URL to test, for example `https://shop.example.com/` or `https://localhost:18597/`

Optional environment variables:

- `BLAZORSHOP_SEO_SMOKE_ALLOW_INVALID_CERTIFICATE=true`: only for local/dev HTTPS when the certificate is not trusted
- `BLAZORSHOP_SEO_SMOKE_REQUIRE_CONFIGURATION=true`: useful in CI or release automation; fails the smoke suite if `BLAZORSHOP_SEO_SMOKE_BASE_URL` was not provided
- `BLAZORSHOP_SEO_SMOKE_STATIC_PATH`: defaults to `/about-us`
- `BLAZORSHOP_SEO_SMOKE_CATEGORY_PATH`: defaults to `/category/sneakers`
- `BLAZORSHOP_SEO_SMOKE_PRODUCT_PATH`: defaults to `/product/metro-runner`
- `BLAZORSHOP_SEO_SMOKE_MISSING_PATH`: defaults to `/product/missing-product`
- `BLAZORSHOP_SEO_SMOKE_REDIRECT_SOURCE_PATH`: defaults to `/product/legacy-runner`; set both redirect variables blank to disable the redirect-specific smoke check
- `BLAZORSHOP_SEO_SMOKE_REDIRECT_TARGET_PATH`: defaults to `/product/metro-runner`
- `BLAZORSHOP_SEO_SMOKE_REDIRECT_STATUS_CODE`: defaults to `301`

The default category/product/redirect routes assume the local demo/seeded storefront data already used by the SEO QA layer. For deployed environments that do not carry those demo slugs, set the route variables explicitly to stable published URLs that should always exist.

If `BLAZORSHOP_SEO_SMOKE_BASE_URL` is not set, the smoke tests are skipped by default so the normal full test pass stays green without a live storefront target. For release automation, set both `BLAZORSHOP_SEO_SMOKE_BASE_URL` and `BLAZORSHOP_SEO_SMOKE_REQUIRE_CONFIGURATION=true` so a missing target configuration fails the smoke stage instead of being skipped.

Local example:

```powershell
$env:BLAZORSHOP_SEO_SMOKE_BASE_URL = "https://localhost:18597/"
$env:BLAZORSHOP_SEO_SMOKE_ALLOW_INVALID_CERTIFICATE = "true"
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj -c Release --filter "Category=SeoSmoke"
```

Deployed-environment example:

```powershell
$env:BLAZORSHOP_SEO_SMOKE_BASE_URL = "https://shop.example.com/"
$env:BLAZORSHOP_SEO_SMOKE_STATIC_PATH = "/about-us"
$env:BLAZORSHOP_SEO_SMOKE_CATEGORY_PATH = "/category/sneakers"
$env:BLAZORSHOP_SEO_SMOKE_PRODUCT_PATH = "/product/metro-runner"
$env:BLAZORSHOP_SEO_SMOKE_MISSING_PATH = "/product/missing-product"
$env:BLAZORSHOP_SEO_SMOKE_REDIRECT_SOURCE_PATH = "/product/legacy-runner"
$env:BLAZORSHOP_SEO_SMOKE_REDIRECT_TARGET_PATH = "/product/metro-runner"
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj -c Release --filter "Category=SeoSmoke"
```

Smoke checks currently verify:

- home, one static page, one category page, and one product page return `200`, expose a single expected canonical, stay indexable, emit expected JSON-LD types, and avoid obvious broken asset references
- one missing product/category route returns `404` without canonical or structured data and keeps `noindex, nofollow` protection on the response
- `/sitemap.xml` returns XML and includes the critical smoke URLs
- `/robots.txt` returns plain text and references the sitemap URL
- one deterministic old-slug redirect returns the expected redirect status and target when configured

Release-blocker guidance:

- Treat any failing smoke assertion as a release blocker for the checked environment.
- If the redirect smoke check is intentionally disabled because no deterministic old slug exists in that environment, that skip is acceptable, but the other smoke checks should still pass before traffic is opened.

## Legacy Development Auth Smoke Checks

The test project also includes a legacy live auth smoke suite tagged with `Category=AuthSmoke`. It is intended for local Development or other disposable legacy environments where open registration is allowed and Identity confirmation is disabled for smoke users.

Run it with:

```powershell
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj -c Release --filter "Category=AuthSmoke"
```

Required environment variables:

- `BLAZORSHOP_AUTH_SMOKE_API_BASE_URL`: absolute legacy API base URL or authentication base URL, for example the legacy Development API origin or its `/api/authentication/` route.
- `BLAZORSHOP_AUTH_SMOKE_STOREFRONT_BASE_URL`: absolute storefront base URL, for example `https://localhost:18597/`
- `BLAZORSHOP_AUTH_SMOKE_CLIENT_APP_BASE_URL`: absolute authenticated client base URL, for example `https://localhost:7258/`

Optional environment variables:

- `BLAZORSHOP_AUTH_SMOKE_ALLOW_INVALID_CERTIFICATE=true`: only for local/dev HTTPS when the certificate is not trusted
- `BLAZORSHOP_AUTH_SMOKE_REQUIRE_CONFIGURATION=true`: useful in CI or scripted smoke stages; fails the suite if the required auth smoke URLs were not provided
- `BLAZORSHOP_AUTH_SMOKE_REFRESH_COOKIE_NAME`: override only if `Runtime:Security:RefreshTokenCookieName` is intentionally changed from the default `__Host-blazorshop-refresh`

Local example:

```powershell
$env:BLAZORSHOP_AUTH_SMOKE_API_BASE_URL = "https://legacy-api.example.local/"
$env:BLAZORSHOP_AUTH_SMOKE_STOREFRONT_BASE_URL = "https://legacy-storefront.example.local/"
$env:BLAZORSHOP_AUTH_SMOKE_CLIENT_APP_BASE_URL = "https://legacy-web.example.local/"
$env:BLAZORSHOP_AUTH_SMOKE_ALLOW_INVALID_CERTIFICATE = "true"
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj -c Release --filter "Category=AuthSmoke"
```

Auth smoke checks currently verify:

- a unique disposable user can register successfully through `api/authentication/create`
- login issues a refresh-token cookie, `refresh-token` succeeds, and `logout` clears the browser session cookie
- storefront `/checkout` redirects anonymous users to the client-app login handoff, authenticated users to the client-app checkout handoff, and reverts to anonymous behavior after logout

Safety note:

- This suite creates real users and assumes Development-style confirmation bypass. Do not run it against a strict Production environment unless that environment is explicitly configured for disposable smoke registrations.

## Pre-release Verification

Run this checklist before promoting a release candidate.

1. Run `dotnet test BlazorShop.sln -c Release`.
2. Run `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj -c Release --filter "Category=SeoSmoke"` against the actual running storefront environment with `BLAZORSHOP_SEO_SMOKE_BASE_URL` and any required route overrides set.
3. Run `docker compose -f compose.production.yml config` with the production-required environment variables available.
4. Run `docker compose -f compose.production.yml build controlplane-api controlplane-web commercenode-api storefront-v2`.
5. Apply database migrations before opening traffic. For V2, the standard MVP runtime path applies migrations on API startup when the relevant `MigrateOnStartup` flag is true; backup the DB first and run one API instance during migration.
6. Smoke test login, refresh, logout, and upload persistence against the deployed environment.

Suggested smoke-test focus:

- Sign in with a normal user and verify the authenticated UI state updates.
- Let the access token expire or simulate a stale token and confirm the refresh-cookie flow still restores the session.
- Log out and confirm the browser session becomes anonymous again.
- Upload a test image, restart or replace the API container, and confirm the file still exists under the mounted uploads volume.

The GitHub Actions workflow `ci` runs the `build-test` job, which already covers the Release build/test pass, production compose rendering, and both production image builds. The checklist above intentionally adds the migration and post-deploy smoke-test steps that CI cannot prove on its own.

## Deployment Checklist

1. Put all secrets in the platform secret store or injected environment variables.
2. Apply the V2 production example settings for each runtime or convert them to environment variables.
3. Remove any extra `AllowedOrigins` entries you do not actually serve in production.
4. Leave forwarded headers disabled unless a reverse proxy or ingress is really in front of the API.
5. If forwarded headers are enabled, trust only the exact proxy IPs or CIDR blocks that should be allowed to set `X-Forwarded-*` headers.
6. Deploy the API and verify that startup logs show the expected allowed origins and forwarded-header mode.
7. Confirm the production email sender settings are populated with real values and that startup did not fail email-options validation.
8. If production health endpoints are enabled, verify that `/health` and `/alive` both return `200` and a minimal healthy payload.
9. Verify that a browser preflight request from the public web origin succeeds against a public API endpoint.
10. Verify that public traffic is throttled as expected while authenticated flows still work.
11. Upload a test image and confirm it still exists after an API container restart.

## GitHub Branch Protection

These settings cannot be enforced from source files alone, so apply them in the GitHub repository settings.

1. Protect the default branch you actually merge into. This repository currently uses `master`.
2. Require pull requests before merging.
3. Require at least one approval.
4. Dismiss stale approvals when new commits are pushed.
5. Require conversation resolution before merge.
6. Require the CI job from `.github/workflows/ci.yml`; in GitHub this is typically shown as `build-test` under the `ci` workflow, often rendered as `ci / build-test`.
7. Block force pushes and branch deletion.

## Smoke Tests

Run these commands after deployment and replace the host names with the real production hosts.

```powershell
Invoke-WebRequest https://api.shop.example.com/health -UseBasicParsing
Invoke-WebRequest https://api.shop.example.com/alive -UseBasicParsing

$headers = @{
  Origin = "https://shop.example.com"
  "Access-Control-Request-Method" = "GET"
}

Invoke-WebRequest -Method Options "https://api.shop.example.com/api/product/all" -Headers $headers -UseBasicParsing
```

Expected results:

- `/health`: `200` with `{"status":"Healthy"}` when production health exposure is enabled.
- `/alive`: `200` with `{"status":"Healthy"}` when production health exposure is enabled.
- CORS preflight: `200` or `204` with `Access-Control-Allow-Origin: https://shop.example.com`.
