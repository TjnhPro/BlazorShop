# BlazorShop CommerceNode Store Resolution Autoplan

Generated: 2026-07-15
Scope: Store resolution hardening for CommerceNode Nginx, Storefront V2 current-store guard, and public URL normalization.

## 1. Mục tiêu

Phase này harden store resolution theo mô hình hiện tại: mỗi Storefront runtime vẫn đi theo `STORE_KEY/storeKey`, CommerceNode Storefront API vẫn scoped bằng route `api/storefront/stores/{storeKey}/*`, và Nginx là entrypoint public cho deployed storefront traffic.

Mục tiêu cụ thể:

- Thêm Nginx default/catch-all server trả `403` cho request không khớp store domain nào.
- Xác định current store sớm trong Storefront V2 trước khi đọc catalog, settings, SEO, cart, checkout hoặc customer context.
- Chuẩn hóa absolute URL generation theo public store URL, scheme public, và trusted forwarded headers.
- Bảo vệ lỗi config sai như missing/invalid `STORE_KEY`, store bị disabled/archived, CommerceNode không resolve được store.
- Giữ OpenAPI/API contract hiện tại: Storefront API scope vẫn là `{storeKey}`, không thêm host-based public API.

## 2. Kết luận scope sau review

Chọn hướng **HOLD SCOPE**.

Không mở rộng sang multi-host alias hoặc host-based tenant resolution trong phase này vì codebase hiện đã có `CommerceStoreDomain` và `CommerceStoreDomainResolver`, nhưng deployment/runtime Storefront V2 vẫn là store-key scoped. Vấn đề cần xử lý ngay là mismatch safety và canonical URL correctness, không phải tenant routing mới.

## 3. Hiện trạng đã xác minh

| Khu vực | Hiện trạng | File |
|---|---|---|
| Storefront API scope | Store scope lấy từ route `{storeKey}`. | `docs/architecture/03-runtime-boundaries.md`, `docs/architecture/09-api-contract-standards.md` |
| Storefront V2 API client | Base address được build từ `StorefrontApi:StoreKey`, `StoreKey`, `STORE_KEY`. | `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs` |
| Current store endpoint | Đã có `GET api/storefront/stores/{storeKey}/store/current`. | `StorefrontScopedStoreController` |
| Commerce store context | Storefront API route dùng route value, admin dùng query, fallback host/header chỉ ngoài route scoped. | `CommerceStoreContext.cs` |
| Nginx store proxy | Store deployment render `server_name <domain>` và proxy `/` về Storefront container. | `NginxDeploymentService.cs` |
| Nginx default deny | Chưa thấy default/catch-all `default_server` trả 403 trong `runtime/nginx/conf.d`. | `runtime/nginx/conf.d` |
| Public URL resolver | Ưu tiên `PublicUrl:BaseUrl`, sau đó SEO configured base, sau cùng request host/scheme. | `StorefrontPublicUrlResolver.cs` |
| Forwarded headers | ControlPlane API có config forwarded headers; Storefront V2 chưa có cấu hình tương tự trong `Program.cs`. | `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs` |

## 4. Không trong scope

- Không đổi CommerceNode Storefront API từ route `{storeKey}` sang host-derived route.
- Không dùng lại `X-Store-Key` cho active Storefront APIs.
- Không làm UI quản lý nhiều host alias.
- Không thay đổi ControlPlane/CommerceNode database ownership.
- Không gộp public store URL và manager/control-plane URL.
- Không tạo cơ chế wildcard domain hoặc catch-all tenant resolver.
- Không refactor toàn bộ Storefront client layer nếu current-store guard có thể làm bằng service/middleware nhỏ.

## 5. Kiến trúc mục tiêu

```text
Browser
  |
  | Host: store.example.com
  v
CommerceNode Nginx
  |
  +-- server_name store.example.com ---------------> Storefront V2 container
  |                                                   STORE_KEY=store-a
  |
  +-- default_server / unmatched host -- 403

Storefront V2 request
  |
  +-- static assets / health endpoints: skip current-store guard
  |
  +-- page/discovery/media proxy request
        |
        +-- resolve configured STORE_KEY
        +-- call CommerceNode Storefront API:
              GET /api/storefront/stores/{storeKey}/store/current
        +-- cache/request-store context
        +-- only then read catalog/settings/customer/cart/checkout data
```

## 6. Phase plan

### Phase 0 - Baseline and safety checks

Goal: lock current assumptions before changing runtime behavior.

Checklist:

- [ ] Confirm generated Nginx store configs currently have no `default_server`.
- [ ] Confirm mounted Nginx config directory hides the image default config in `compose.commercenode.yml`.
- [ ] Add/update tests around `NginxDeploymentService.CreatePlan` and rendered store proxy config if current tests are missing.
- [ ] Record current Storefront V2 config sources: `StorefrontApi:StoreKey`, `StoreKey`, `STORE_KEY`.
- [ ] Confirm `store/current` response contains enough fields for guard decisions: active store resolved, maintenance flag, public URL/primary domain if available.

Verification:

```powershell
rg -n "default_server|server_name|STORE_KEY|store/current" BlazorShop.PresentationV2 BlazorShop.Infrastructure docs
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~Nginx|FullyQualifiedName~Storefront"
```

Commit:

```text
test: capture store resolution baseline
```

### Phase 1 - Nginx default/catch-all deny

Goal: unmatched host traffic must not accidentally route to the first store server block.

Implementation:

- [ ] Add a static Nginx config file in `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/runtime/nginx/conf.d/`, for example `00-default-deny.conf`.
- [ ] Use an explicit default server:

```nginx
server {
    listen 80 default_server;
    server_name _;
    return 403;
}
```

- [ ] Keep generated store configs as named `server_name` configs only.
- [ ] Do not put app-specific JSON/body handling in Nginx. This is an edge deny, not app error rendering.
- [ ] Add a test or static assertion proving the default deny file exists and contains `default_server` plus `return 403`.
- [ ] Add deployment/QA note that unmatched host should return `403`, not Nginx welcome page and not a random storefront.

Verification:

```powershell
docker compose -f compose.commercenode.yml up -d commercenode-nginx
docker exec blazorshop-commercenode-nginx nginx -t
curl.exe -i -H "Host: unknown.invalid" http://localhost:8088/
```

Expected:

- `nginx -t` succeeds.
- Unknown host returns `HTTP/1.1 403 Forbidden`.
- Existing generated store configs still validate.

Commit:

```text
infra: deny unmatched commerce node hosts
```

### Phase 2 - Storefront current-store guard

Goal: Storefront V2 resolves the configured current store before reading store-scoped runtime data.

Implementation:

- [ ] Add a small Storefront V2 current-store service, for example `IStorefrontCurrentStoreProvider`.
- [ ] Provider calls existing `StorefrontApiClient.GetCurrentStoreAsync()` using the scoped API base address.
- [ ] Cache the resolved store per request, and optionally short-cache success in memory with a small TTL if repeated calls are noisy.
- [ ] Add middleware or a narrow page/service entry guard that runs before catalog/settings/customer/cart/checkout paths.
- [ ] Exclude static assets, framework assets, health endpoints, and other non-store paths from the guard.
- [ ] Return clear responses:
  - missing `STORE_KEY` in production: startup/options validation failure or `503` with clear log;
  - store not found: `404`;
  - CommerceNode unavailable: `503`;
  - invalid/ambiguous store response: `503`;
  - maintenance mode: route to existing maintenance behavior if already present, otherwise return `503` with maintenance message.
- [ ] Add structured logs with `storeKey`, request path, result status, and trace id.
- [ ] Do not fallback to another store.

Tests:

- [ ] Unit test provider success caches/resolves current store.
- [ ] Unit test not found maps to 404 and does not call catalog.
- [ ] Unit test CommerceNode unavailable maps to 503 and does not call catalog.
- [ ] Test static/health paths are skipped.
- [ ] Storefront integration test: catalog page does not render product calls when current store fails.

Verification:

```powershell
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~Storefront"
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
```

Commit:

```text
feat: guard storefront requests with current store
```

### Phase 3 - Public URL and scheme normalization

Goal: absolute URLs use the public store URL/scheme, not accidental internal host or wrong proxy scheme.

Implementation:

- [ ] Keep `PublicUrl:BaseUrl` as the first priority for canonical/discovery URLs.
- [ ] Keep SEO configured base URL as second priority when present.
- [ ] Harden request fallback only after forwarded headers are trusted.
- [ ] Add Storefront V2 forwarded header configuration equivalent to ControlPlane API:
  - `X-Forwarded-For`
  - `X-Forwarded-Proto`
  - `X-Forwarded-Host`
  - configurable known proxies/networks
- [ ] Call `UseForwardedHeaders()` before middleware/services that read `Request.Scheme` or `Request.Host`.
- [ ] Normalize fallback URL:
  - allow only `http` and `https`;
  - remove query and fragment;
  - keep path base;
  - ensure trailing slash;
  - prefer configured public URL in production.
- [ ] Audit sitemap, robots, canonical, structured data, checkout/payment success/cancel links for resolver usage.
- [ ] Do not infer store identity from host in this phase.

Tests:

- [ ] `StorefrontPublicUrlResolver` prefers `PublicUrl:BaseUrl`.
- [ ] Request fallback uses forwarded proto/host only when middleware/config permits it.
- [ ] Resolver normalizes trailing slash and strips query/fragment.
- [ ] Sitemap/canonical uses `https://public-store.example/` when configured.
- [ ] Payment redirect URL tests keep using the correct client/public base URL.

Verification:

```powershell
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~StorefrontPublicUrl|FullyQualifiedName~Seo|FullyQualifiedName~Payment"
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
```

Commit:

```text
fix: normalize storefront public urls
```

### Phase 4 - QA checklist and docs

Goal: make the behavior explicit for future agents and operators.

Docs:

- [ ] Update `docs/architecture/03-runtime-boundaries.md`:
  - Nginx unmatched host returns 403.
  - Storefront API scope remains route `{storeKey}`.
  - Storefront V2 current-store guard is configured-store-key based.
- [ ] Update `docs/architecture/07-deployment-and-local-run.md`:
  - local Nginx 403 smoke command;
  - forwarded header requirements;
  - public URL config rules.
- [ ] Update `AGENTS.md` if the runtime rule is load-bearing enough for future agents.
- [ ] Update QA todo files:
  - `QA-CommerceNode.todo.md`: Nginx unknown host 403, known host proxy, nginx -t.
  - `QA-StorefrontV2.todo.md`: current-store guard, bad store key, public URL/canonical scheme.

QA:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~Nginx|FullyQualifiedName~Storefront|FullyQualifiedName~Seo|FullyQualifiedName~Payment"
docker compose -f compose.commercenode.yml up -d commercenode-nginx
docker exec blazorshop-commercenode-nginx nginx -t
curl.exe -i -H "Host: unknown.invalid" http://localhost:8088/
```

Commit:

```text
docs: document store resolution hardening
```

## 7. Error and failure map

| Codepath | Failure | Expected behavior |
|---|---|---|
| Nginx unmatched host | Host does not match any store `server_name`. | `403 Forbidden` at Nginx edge. |
| Storefront config | `STORE_KEY` missing in production. | Fail startup or return `503` before catalog read with clear log. |
| Storefront config | `STORE_KEY` points to missing store. | Current-store guard returns `404`; no catalog/settings/customer calls. |
| CommerceNode API | `store/current` returns 500/unavailable. | Storefront returns `503`; no fallback store. |
| Storefront public URL | Proxy sends internal scheme/host. | Configured `PublicUrl:BaseUrl` wins; request fallback only uses trusted forwarded headers. |
| Nginx generated config | Invalid generated config. | `nginx -t` fails before reload; deployment task fails safely. |

## 8. Test coverage map

```text
Nginx default deny
  ├── [unit/static] default config exists and returns 403
  ├── [integration] nginx -t passes
  └── [smoke] unknown Host -> 403

Storefront current-store guard
  ├── [unit] success resolves once per request
  ├── [unit] missing/404 store -> 404, no downstream catalog
  ├── [unit] CommerceNode unavailable -> 503
  ├── [unit] static/health paths skipped
  └── [integration] page request blocks before catalog on invalid store

Absolute URL normalization
  ├── [unit] PublicUrl:BaseUrl wins
  ├── [unit] request fallback normalizes scheme/host/pathbase
  ├── [unit] query/fragment stripped from base URL
  ├── [integration] sitemap/canonical use public https URL
  └── [unit] payment redirect URL remains correct
```

## 9. Rollback plan

- Nginx default deny can be reverted by removing `00-default-deny.conf` and reloading Nginx.
- Current-store guard should be introduced with a narrow service/middleware so reverting is a small Storefront V2 diff.
- Public URL changes are covered by resolver tests and can be reverted independently.
- No database migration is planned for this phase.
- No public API route rename is planned, so client rollback risk is low.

## 10. Implementation order and commits

1. Baseline tests and assertions.
2. Nginx default deny.
3. Storefront current-store guard.
4. Public URL and forwarded header normalization.
5. Docs and QA checklist updates.

Each phase should be committed separately. Do not batch Nginx edge behavior and Storefront current-store behavior into the same commit.

## 11. Decision Audit Trail

| # | Decision | Classification | Rationale | Rejected |
|---|---|---|---|---|
| 1 | Keep Storefront API scoped by route `{storeKey}`. | Accepted | Matches current architecture and OpenAPI contract standards. | Host-derived unscoped Storefront APIs. |
| 2 | Add Nginx default/catch-all `403`. | Accepted | Prevents unmatched host from landing on a random/default server block. | Rely on Nginx implicit first-server behavior. |
| 3 | Current-store guard uses configured `STORE_KEY`, not host lookup. | Accepted | Matches one-store-per-Storefront-runtime deployment model. | Multi-tenant host resolver in Storefront V2. |
| 4 | Public URL config stays authoritative in production. | Accepted | Avoids canonical/payment URLs depending on untrusted request headers. | Infer public URL from every request host by default. |
| 5 | Multi-host alias management is deferred. | Deferred | No current business need; domain model already has enough foundation. | Build alias UI/workflow now. |

## 12. Completion checklist

- [ ] Unknown host through CommerceNode Nginx returns 403.
- [ ] Known host still proxies to the intended Storefront container.
- [ ] Storefront V2 fails clearly when configured store is missing or inactive.
- [ ] Storefront V2 does not call catalog/settings/customer/cart/checkout after current-store failure.
- [ ] Absolute URLs use configured public URL and correct scheme.
- [ ] Forwarded headers are trusted only through configured proxies/networks.
- [ ] QA-CommerceNode and QA-StorefrontV2 checklists are updated.
- [ ] Architecture/deployment docs reflect the new edge and Storefront runtime rule.

## GSTACK REVIEW REPORT

| Review | Trigger | Why | Runs | Status | Findings |
|--------|---------|-----|------|--------|----------|
| CEO Review | `/autoplan` | Scope & strategy | 1 | Clear | Hold scope: Nginx deny, current-store guard, URL normalization only. |
| Design Review | `/autoplan` | UI/UX gaps | 0 | Skipped | No UI scope. |
| Eng Review | `/autoplan` | Architecture & tests | 1 | Clear | Main risk is implicit Nginx default server and missing Storefront current-store preflight. |
| DX Review | `/autoplan` | Operator/developer experience | 1 | Clear | Plan includes smoke commands, QA updates, and clear error behavior. |

**VERDICT:** Ready to implement by phase.

NO UNRESOLVED DECISIONS
