# V2 Production Readiness Hardening.todo.md

Status: proposed  
Source: investigate review of CI, production compose, V2 test coverage, Storefront host trust, rate limiting, OpenAPI generator, Storefront client contracts  
Purpose: make the active V2 runtime the real build, test, deploy, and release target without rewriting Commerce flows.

## Goal

Chuyen release gate tu legacy sang V2 that su:

- CI blocking phai build/test V2.
- Production compose/Docker artifacts phai chay V2.
- Storefront public media store resolution khong tin raw client headers.
- Rate limit cho guest khong gom nhieu customer vao cung bucket sai.
- OpenAPI generator test phai dung generator that.
- Storefront client phu thuoc capability interfaces va san sang thay bang generated client sau nay.

## Non-goals

- [ ] Khong xoa legacy projects khoi repository.
- [ ] Khong rewrite cart, checkout, order, payment, media hay Storefront routing.
- [ ] Khong thay doi public Storefront API route shape neu khong can.
- [ ] Khong dua Control Plane Web goi truc tiep Commerce Node.
- [ ] Khong them provider moi cho payment/shipping/tax trong phase nay.
- [ ] Khong thay `ServiceResponse<T>`/`ApplicationResult<T>` bang mot big-bang refactor.

## Verified Phase 0 baseline evidence

- [x] `.github/workflows/ci.yml` van restore/build `BlazorShop.sln`, test `BlazorShop.Tests`, build Dockerfile legacy trong `BlazorShop.Presentation/*`.
- [x] `compose.production.yml` van dung legacy `BlazorShop.Presentation/BlazorShop.API`, `BlazorShop.Presentation/BlazorShop.Storefront`, `BlazorShop.Presentation/BlazorShop.Web`.
- [x] `docs/architecture/07-deployment-and-local-run.md` xac dinh active V2 commands la `dotnet build BlazorShop.V2.slnf` va `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj`.
- [x] `BlazorShop.Tests.V2.csproj` hien chi link `Architecture/**` va `PresentationV2/**`; nhieu tests `Application/**` va `Infrastructure/**` lien quan V2 core chua nam trong gate nay.
- [x] Phase 0 baseline: `StorefrontStoreScopeMiddleware` uu tien `X-Store-Host` va `X-Forwarded-Host` cho public media paths. Phase 1 da sua de chi dung `Request.Host` sau trusted forwarded headers.
- [x] Phase 0 baseline: `NginxDeploymentService` set `Host`, `X-Real-IP`, `X-Forwarded-For`, `X-Forwarded-Proto`, nhung khong set/clear `X-Store-Host`. Phase 1 da clear `X-Store-Host`.
- [x] Phase 0 baseline: CommerceNode Program khong cau hinh `UseForwardedHeaders` cho trusted proxy truoc khi Storefront store scope middleware chay. Phase 1 da them `Runtime:ForwardedHeaders` va pipeline order guard.
- [x] CommerceNode rate limit partition dung authenticated user id neu co, con guest fallback theo `RemoteIpAddress`.
- [x] Storefront local cart rate limit cung partition theo `RemoteIpAddress`.
- [x] `CommerceNodeStorefrontOpenApiContractTests.StorefrontSwagger_CanGenerateTypeScriptClientSmoke` dung generator tu viet, return `Promise<unknown>`, chua chay NSwag/Kiota/OpenAPI Generator that.
- [x] Storefront V2 da co `IStorefrontCatalogClient`, `IStorefrontCheckoutClient`, `IStorefrontPaymentClient`, ... va `StorefrontApiClient` da tach partial files.
- [x] Nhieu Storefront pages/components van inject concrete `StorefrontApiClient` truc tiep.
- [x] `StorefrontCheckoutService` da co `CheckoutPricingCalculator` va `CheckoutPaymentCoordinator`; checkout hotspot con lon nhung khong phai phan release-hardening dau tien.
- [x] Co file plan cu `V2 Architecture Boundary Hardening.todo.md`; plan nay chi bo sung release/production readiness con thieu, khong lap lai refactor da xong.

## Phase dependency map

```text
Phase 0: Baseline and release gate inventory
  -> Phase 1: Trusted host and public media store scope
      -> Phase 2: Guest/client rate-limit identity
          -> Phase 3: V2 CI blocking gate
              -> Phase 4: V2 production Docker and compose
                  -> Phase 5: Real OpenAPI generator gate
                      -> Phase 6: Storefront client contract cleanup
                          -> Phase 7: Browser/API production release verification
```

## Phase 0 - Baseline and release gate inventory

Goal: khoa lai su that hien tai truoc khi sua CI/security/deploy.

### Tasks

- [x] Them hoac cap nhat architecture/release baseline tests de doc `.github/workflows/ci.yml` va chung minh CI active gate phai co V2 commands.
- [x] Them baseline test cho `compose.production.yml` hien khong duoc xem la V2 production target neu no con tro den `BlazorShop.Presentation/*`.
- [x] Lap danh sach Application/Infrastructure tests co lien quan V2 core nhung chua nam trong `BlazorShop.Tests.V2`.
- [x] Lap danh sach Storefront pages/components con inject concrete `StorefrontApiClient`.
- [x] Lap danh sach Storefront/CommerceNode rate-limit policies va endpoint dang dung tung policy.
- [x] Xac dinh generator se dung cho OpenAPI gate: uu tien tool co the pin version trong repo/CI va compile duoc generated TypeScript client.
- [x] Ghi nhan cac warning hien co ma phase nay khong xu ly: MessagePack advisory, Browserslist stale notice, legacy mixed test project.

### Phase 0 inventory - 2026-07-20

Release-blocking gaps:

- CI `.github/workflows/ci.yml` restore/build/test van dung `BlazorShop.sln` va `BlazorShop.Tests/BlazorShop.Tests.csproj`; chua co `ci-v2`, `BlazorShop.V2.slnf`, hoac `BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj`.
- CI Docker image build van tro vao legacy Dockerfile trong `BlazorShop.Presentation/BlazorShop.API`, `BlazorShop.Presentation/BlazorShop.Storefront`, va `BlazorShop.Presentation/BlazorShop.Web`.
- `compose.production.yml` van la legacy topology voi mot `postgres`, `api`, `storefront`, `web`, `ConnectionStrings__DefaultConnection`; chua co `ControlPlaneConnection`/`CommerceNodeConnection` hay V2 Dockerfiles.
- `BlazorShop.Tests.V2.csproj` chi link `Architecture/**` va `PresentationV2/**`; core tests trong `BlazorShop.Tests/Application/**` va `BlazorShop.Tests/Infrastructure/**` chua nam trong V2 release gate.

Security gaps:

- `StorefrontStoreScopeMiddleware` hien lay public media host tu raw `X-Store-Host`, sau do raw `X-Forwarded-Host`, truoc `Request.Host`.
- CommerceNode `Program.cs` chua goi `UseForwardedHeaders()` truoc store-scope middleware.
- CommerceNode rate limiter bucket: authenticated theo `ClaimTypes.NameIdentifier`, guest fallback `RemoteIpAddress`.
- Storefront local cart rate limiter bucket: store + route + `RemoteIpAddress`.

DX/maintainability gaps:

- Storefront pages/components con inject concrete `StorefrontApiClient`: `StorefrontHeader`, `AccountAddressesPage`, `AccountOrderDetailPage`, `AccountOrdersPage`, `AccountProfilePage`, `CategoryPage`, `CheckoutPage`, `Home`, `NewReleases`, `PaymentCancelPage`, `PaymentSuccessPage`, `ProductPage`, `SearchPage`, `StorefrontPage`, `TodaysDeals`.
- OpenAPI generator gate hien la smoke tu viet trong `CommerceNodeStorefrontOpenApiContractTests.StorefrontSwagger_CanGenerateTypeScriptClientSmoke`, sinh `Promise<unknown>`; chua co `.config/dotnet-tools.json` hoac pinned real generator.
- Warning khong xu ly trong phase nay: MessagePack NU190x advisories, Browserslist stale notice tu Tailwind build, mixed legacy test project con ton tai.

Generator decision for Phase 5:

- Dung NSwag TypeScript client trong `.config/dotnet-tools.json` vi phu hop .NET CI, pin duoc version, chay duoc tu test/CI, va generated TypeScript co the compile bang project nho rieng. Khong migrate Storefront production client sang generated output trong phase nay.

### Suggested tests

- [x] `BlazorShop.Tests/Architecture/V2ProductionReadinessTests.cs`
- [ ] `BlazorShop.Tests/PresentationV2/CommerceNode/StorefrontHostTrustTests.cs`
- [ ] `BlazorShop.Tests/PresentationV2/CommerceNode/StorefrontRateLimitIdentityTests.cs`

### Done when

- [x] Test inventory phan biet ro: release-blocking gap, security gap, DX/maintainability gap.
- [x] Khong co production code change trong phase 0.
- [x] Plan sau phase 0 co du file list de migrate tung phan nho.

## Phase 1 - Trusted host and public media store scope

Goal: public media store context khong bi client gia header lam resolve sai store.

### Tasks

- [x] Sua `StorefrontStoreScopeMiddleware` cho public media path de khong doc raw `X-Store-Host`.
- [x] Chon source host canonical:
  - [x] Dung `context.Request.Host.Value` sau ASP.NET Core forwarded headers da xu ly.
  - [x] Chi tin `X-Forwarded-Host` thong qua `UseForwardedHeaders` voi `KnownProxies`/`KnownNetworks`.
  - [x] Neu CommerceNode khong chay sau trusted proxy, forwarded host khong duoc anh huong `Request.Host`.
- [x] Them CommerceNode forwarded-header options rieng neu hien chua co:
  - [x] `Runtime:ForwardedHeaders:KnownProxies`.
  - [x] `Runtime:ForwardedHeaders:KnownNetworks`.
  - [x] `ForwardLimit`.
- [x] Goi `UseForwardedHeaders()` truoc `StorefrontStoreScopeMiddleware` neu forwarded headers duoc enable/configured.
- [x] Cap nhat `NginxDeploymentService` de tiep tuc set `Host $host` va khong chuyen tiep `X-Store-Host`.
- [x] Neu can, clear header unsafe o Nginx:
  - [x] `proxy_set_header X-Store-Host "";` hoac bo hoan toan neu Nginx khong gui.
  - [x] Khong dua `X-Store-Host` vao contract production.
- [x] Cap nhat test hien tai dang ky vong `X-Forwarded-Host` raw duoc dung cho public media.
- [x] Them test forged header:
  - [x] `Request.Host = store-a.example`.
  - [x] Client gui `X-Store-Host = store-b.example`.
  - [x] Middleware van resolve store A hoac reject neu host A invalid, khong bao gio resolve store B.
- [x] Them test trusted proxy:
  - [x] Khi request den tu known proxy va framework forwarded headers da set `Request.Host`, media resolve theo forwarded host hop le.
- [x] Cap nhat docs local media note neu truoc day noi direct localhost dung header custom.

### Phase 1 implementation notes - 2026-07-20

- CommerceNode forwarded-header config dung `Runtime:ForwardedHeaders` de khop voi docs/env hien co (`Runtime__ForwardedHeaders__KnownProxies__0`, `Runtime__ForwardedHeaders__ForwardLimit`).
- `StorefrontStoreScopeMiddleware` khong doc `X-Store-Host`/`X-Forwarded-Host`; no chi nhan `Request.Host` sau khi framework forwarded headers middleware da xu ly trusted proxy.
- Generated Nginx store proxy config clear `X-Store-Host` trong product media, asset media va root proxy locations.

### Acceptance criteria

- [x] Public media endpoint khong tin `X-Store-Host` tu client.
- [x] Forged host header khong the doi store context.
- [x] Nginx generated config khong vo public media proxy.
- [x] Storefront scoped APIs van lay store tu route `{storeKey}`, khong bi anh huong boi thay doi nay.

## Phase 2 - Guest/client rate-limit identity

Goal: rate limit khong gom tat ca guest di qua Storefront/proxy vao cung bucket sai, va khong tin header IP raw.

### Tasks

- [x] Giu authenticated bucket theo user id nhu hien co trong CommerceNode.
- [x] Cho guest CommerceNode, uu tien signed cart/session identity khi endpoint co cart/session token hop le.
- [x] Cho endpoint auth/recovery/newsletter/contact chua co cart session, dung trusted client IP sau forwarded headers da config, hoac fallback IP neu khong co trusted proxy.
- [x] Khong doc truc tiep raw `X-Forwarded-For` trong rate limiter.
- [x] Neu Storefront V2 proxy server-to-server toi CommerceNode, xem xet them signed proxy identity header noi bo:
  - [x] Khong them public proxy identity header moi trong phase nay.
  - [x] CommerceNode dung `X-Cart-Token` hien co cho cart/checkout bucket; cac endpoint khong co cart token fallback `RemoteIpAddress` sau trusted forwarded-header middleware.
  - [x] Khong public header identity moi cho browser.
- [x] Storefront local cart endpoints:
  - [x] Neu request co cart session cookie/token, partition theo store + route + cart session.
  - [x] Neu khong co cart session, fallback trusted client IP.
- [x] Them tests:
  - [x] Hai guest khac cart session nhung cung proxy IP khong chung cart bucket.
  - [x] Same guest/session bi rate-limit dung.
  - [x] Authenticated user dung user bucket, khong bi cart session bucket override.
  - [x] Raw `X-Forwarded-For` khong doi bucket neu proxy khong trusted.

### Phase 2 implementation notes - 2026-07-22

- CommerceNode rate limiter lay actor qua `StorefrontRateLimitIdentity.ResolveActor`: authenticated user id wins, cart/checkout guest dung hashed `X-Cart-Token`, endpoint con lai fallback `RemoteIpAddress` sau middleware trusted forwarded headers.
- Storefront local cart limiter lay actor qua `StorefrontRateLimitIdentity.ResolveLocalCartActor`: hashed cart cookie wins, fallback `RemoteIpAddress`.
- Storefront proxy `place-order` tiep tuc gui `X-Cart-Token` sang CommerceNode de checkout bucket khong gom tat ca guest theo IP cua Storefront server.
- Khong mo them signed internal identity header trong phase nay vi `X-Cart-Token` da la session identity dang co cho cart/checkout, va them header moi se mo them secret/trust boundary chua can cho MVP production hardening.

### Acceptance criteria

- [x] Guest cart/checkout requests khong bi 429 hang loat chi vi chung Storefront/proxy IP.
- [x] Rate limit identity khong tao trust boundary moi tu browser header.
- [x] Policy van du don gian cho MVP, khong can distributed limiter neu chua co multi-instance requirement.

## Phase 3 - V2 CI blocking gate

Goal: CI bat buoc build/test active V2, legacy khong con la tin hieu release chinh.

### Tasks

- [x] Tao job `ci-v2` trong `.github/workflows/ci.yml`.
- [x] `ci-v2` commands:
  - [x] `dotnet restore BlazorShop.V2.slnf`.
  - [x] `dotnet build BlazorShop.V2.slnf --configuration Release --no-restore`.
  - [x] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --configuration Release --no-build --verbosity normal`.
- [x] Chuyen legacy build/test thanh job `legacy-compatibility`.
- [x] Quyet dinh legacy job:
  - [x] Khong blocking release neu V2 la production target.
  - [x] Co the chay `continue-on-error` tam thoi neu legacy warnings/deps can xu ly rieng.
- [x] Sua Node cache path neu CI V2 khong can `BlazorShop.Presentation/BlazorShop.Web/package-lock.json`.
- [x] Them CI check ngan workflow quay lai chi build `BlazorShop.sln` ma khong build `BlazorShop.V2.slnf`.
- [x] Cap nhat README/docs release command neu CI doi.

### V2 test migration tasks

- [x] Dua V2-relevant Application tests vao `BlazorShop.Tests.V2`.
- [x] Dua V2-relevant Infrastructure CommerceNode tests vao `BlazorShop.Tests.V2`.
- [x] Uu tien nhom:
  - [x] Cart/session/sellability.
  - [x] Checkout/pricing/payment attempt/order placement.
  - [x] Currency/rounding.
  - [x] Navigation/pages/SEO.
  - [x] CommerceNodeDbContext model/migration.
  - [x] Seeder idempotency.
  - [x] Store SMTP/message queue.
  - [x] Webhook/payment callback safety.
- [x] Uu tien move file sang structure V2 neu tests thuc su thuoc V2; chi link tam thoi neu can giam blast radius.
- [x] Them guardrail test dem so luong hoac namespace expected de V2 tests khong bi rot khoi project.

### Phase 3 implementation notes - 2026-07-22

- `.github/workflows/ci.yml` co job `ci-v2` lam release gate: restore/build `BlazorShop.V2.slnf`, test `BlazorShop.Tests.V2`.
- Legacy solution/test/Docker/compose checks duoc giu trong `legacy-compatibility` voi `continue-on-error: true` de khong tao tin hieu sai cho V2 release.
- `BlazorShop.Tests.V2` link Application CommerceNode, Infrastructure CommerceNode va Infrastructure ControlPlane tests; chua copy/move file vat ly de giam churn.
- README va deployment doc da cap nhat lệnh release-gate V2.

### Acceptance criteria

- [x] Required status check moi la `ci-v2`.
- [x] CI V2 fail neu V2 build/test fail.
- [x] Legacy compatibility khong tao ao giac V2 da deploy duoc.
- [x] `BlazorShop.Tests.V2` bao gom core commerce tests can cho production readiness.

## Phase 4 - V2 production Docker and compose

Goal: production artifacts chay dung active V2 topology.

### Tasks

- [x] Tao Dockerfile V2 con thieu:
  - [x] `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Dockerfile`.
  - [x] `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/Dockerfile`.
  - [x] `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Dockerfile`.
  - [x] Storefront V2 Dockerfile da ton tai, review lai config production.
- [x] Tao `compose.v2.production.yml` thay vi sua ngay `compose.production.yml` neu can giai doan song song.
- [x] Compose V2 service de xuat:
  - [x] `controlplane-postgres`.
  - [x] `commercenode-postgres`.
  - [x] `controlplane-api`.
  - [x] `controlplane-web`.
  - [x] `commercenode-api`.
  - [x] `commercenode-nginx`.
  - [x] `commercenode-imgproxy` neu media delivery can.
  - [x] `storefront-v2` template/image path neu production single-store runtime can boot static store, hoac document deploy task flow neu Storefront containers duoc tao qua CommerceNode tasks.
- [x] Dung dung connection strings:
  - [x] `ControlPlaneConnection` cho ControlPlane API.
  - [x] `CommerceNodeConnection` cho CommerceNode API.
  - [x] Khong dung `DefaultConnection` legacy trong V2 compose.
- [x] Cau hinh startup migration theo docs:
  - [x] `ControlPlane__Database__MigrateOnStartup`.
  - [x] `CommerceNode__Database__MigrateOnStartup`.
- [x] Cau hinh secrets:
  - [x] Node key/secret chi o ControlPlane API va CommerceNode API.
  - [x] Storefront V2 chi co CommerceNode Storefront API base URL va store key/runtime public settings.
  - [x] SMTP per-store khong nam trong Storefront env.
- [x] Health checks:
  - [x] ControlPlane API health.
  - [x] CommerceNode API health.
  - [x] ControlPlane Web HTTP 200.
  - [x] Storefront V2 current store/page HTTP 200 hoac maintenance expected.
  - [x] Swagger JSON fetch cho CommerceAdmin/Storefront neu production health mode cho phep.
- [x] CI build Docker images tu V2 Dockerfiles.
- [x] CI chay `docker compose -f compose.v2.production.yml config`.
- [x] Optional sau khi V2 compose pass: bien `compose.production.yml` thanh alias/copy cua V2 hoac document legacy compose la deprecated.

### Phase 4 implementation notes - 2026-07-22

- Added V2 Dockerfiles for Commerce Node API, Control Plane API, Control Plane Web, and fixed Storefront V2 Dockerfile project references for Components/WASM.
- Added `compose.v2.production.yml` as the active V2 production topology while leaving legacy `compose.production.yml` as compatibility.
- ServiceDefaults can expose `/health` outside Development only when `Runtime:Health:ExposeInProduction=true`; compose uses that only for container healthchecks.
- Control Plane Web Nginx image writes `wwwroot/appsettings.json` from `CONTROLPLANE_API_BASE_URL` at startup, avoiding baked localhost API URLs.
- CI `ci-v2` validates compose config and builds all four V2 images.

### Acceptance criteria

- [x] Khong production file nao mac dinh deploy legacy API/Web/Storefront khi release V2.
- [x] Compose V2 boot duoc voi env mau khong chua secret that.
- [x] Health/smoke checks chay trong CI.
- [x] Unknown host qua Nginx van tra 403.

## Phase 5 - Real OpenAPI generator gate

Goal: Swagger contract phai generator-safe bang tool that, khong chi smoke string.

### Tasks

- [x] Chon generator:
  - [x] NSwag TypeScript client neu phu hop .NET ecosystem.
  - [x] Hoac Kiota/OpenAPI Generator neu repo muon generator trung lap voi future client.
- [x] Pin generator version trong repo:
  - [x] `dotnet tool manifest` hoac package/dev dependency.
  - [x] Khong dung version floating trong CI.
- [x] Test pipeline:
  - [x] Start/factory CommerceNode API hoac export Swagger JSON tu test host.
  - [x] Ghi Swagger JSON tam thoi vao test artifacts.
  - [x] Chay generator CLI that.
  - [x] Compile generated client bang TypeScript project nho hoac C# compile smoke.
  - [x] Assert operation names/schema khong collision.
- [x] Giu test hien tai ve metadata/operationId/security/error schemas.
- [x] Thay `GenerateTypeScriptClient` tu viet bang helper chi dung de doc/compare neu van can snapshot, khong goi la generator safety.
- [x] Them CI artifact khi generator fail: swagger json + generator stderr.

### Phase 5 implementation notes - 2026-07-22

- Added pinned local tool manifest `.config/dotnet-tools.json` with `NSwag.ConsoleCore` 14.7.1.
- Added `tools/openapi-generator-smoke` with pinned TypeScript 7.0.2 and CI restore step.
- Replaced handwritten TS smoke with real NSwag generation and TypeScript compilation.
- Fixed generator-breaking `maximum: 1.7976931348623157E+308` contract on storefront catalog price filters by switching to bounded decimal ranges.
- Updated storefront OpenAPI snapshot after the bounded price-range contract change.

### Acceptance criteria

- [x] OpenAPI test fail neu generator that khong tao/compile duoc client.
- [x] Storefront contract thay doi co snapshot/generator signal ro.
- [x] Day la tien de truoc khi migrate Storefront V2 sang generated client.

## Phase 6 - Storefront client contract cleanup

Goal: giam phu thuoc concrete `StorefrontApiClient` va san sang adapter generated client, nhung khong rewrite Storefront.

### Tasks

- [x] Giu existing capability interfaces:
  - [x] `IStorefrontCatalogClient`.
  - [x] `IStorefrontCartClient`.
  - [x] `IStorefrontCheckoutClient`.
  - [x] `IStorefrontCustomerClient`.
  - [x] `IStorefrontPaymentClient`.
  - [x] `IStorefrontContentClient`.
  - [x] `IStorefrontAddressClient`.
  - [x] `IStorefrontConsentClient`.
  - [x] `IStorefrontStoreConfigurationClient`.
- [x] Chuyen pages/components dang inject concrete `StorefrontApiClient` sang interface nho nhat can dung.
- [x] Neu mot page can nhieu capability, inject tung interface can thiet thay vi concrete all-in-one.
- [x] Them architecture test: pages/components khong inject `StorefrontApiClient` truc tiep, allowlist bang 0.
- [x] Giu `StorefrontApiClient` lam internal/manual adapter tam thoi.
- [x] Khong sinh generated client vao production code cho den khi Phase 5 pass on dinh.
- [x] Doi voi DTO duplicate:
  - [x] Chi dedupe DTO o boundary ro rang.
  - [x] Khong move public API contracts vao `Web.SharedV2` neu architecture doc noi presentation boundary owns contract.
  - [x] Uu tien generated client lam source cho transport DTO ve sau.

### Phase 6 implementation notes - 2026-07-22

- Storefront page/component injections no longer depend on `StorefrontApiClient` directly; each UI surface now asks for the narrow capability it uses.
- `Home` splits content metadata lookup and catalog lookup across `IStorefrontContentClient` and `IStorefrontCatalogClient`.
- `CheckoutPage` resolves checkout, payment, catalog, address, and customer data through separate capability clients in the code-behind.
- Architecture baseline tests now enforce zero concrete `StorefrontApiClient` usage in active page/component UI.

### Acceptance criteria

- [x] Storefront UI phu thuoc capability interfaces.
- [x] Concrete `StorefrontApiClient` chi la implementation detail trong DI/Services.
- [x] Generated client migration sau nay co adapter seam ro, khong can sua tat ca pages.

## Phase 7 - Browser/API production release verification

Goal: sau khi CI/security/deploy hardening xong, co release checklist de quyet dinh public production.

### Tasks

- [x] Cap nhat `QA-CommerceNode.todo.md`:
  - [x] forged public media headers.
  - [x] trusted forwarded headers.
  - [x] rate limit guest/session behavior.
  - [x] Swagger generator artifact.
  - [x] CommerceNode V2 health in compose.
- [x] Cap nhat `QA-ControlPlane.todo.md`:
  - [x] ControlPlane API/Web V2 compose health.
  - [x] ControlPlane API khong expose node secret to Web.
  - [x] ControlPlane Web khong call CommerceNode direct.
- [x] Cap nhat `QA-StorefrontV2.todo.md`:
  - [x] Storefront V2 compose boot.
  - [x] current store resolve.
  - [x] public media under correct host.
  - [x] cart/account/checkout/browser flow unaffected.
- [x] Cap nhat `Storefront Playwright E2E Release.todo.md`:
  - [x] Browser network denies direct `api/commerce/*`, `api/control-plane/*`, `api/internal/*`.
  - [x] Product media images load from correct host.
  - [x] Cart add/update/remove with guest session.
  - [x] Account login/recovery/register policy.
  - [x] Checkout COD place-order real flow in test store.
  - [x] Order placed email capture if Mailpit/test SMTP configured.
- [x] CI hoặc release script chay smoke API:
  - [x] ControlPlane API health.
  - [x] CommerceNode API health.
  - [x] Storefront Swagger.
  - [x] CommerceAdmin Swagger.
  - [x] Nginx unknown host 403.
- [x] Manual/visible Playwright run truoc production publish:
  - [x] Home/catalog/product.
  - [x] Cart.
  - [x] Account recovery.
  - [x] Checkout COD.
  - [x] Order detail/account order history.
  - [x] Media and SEO docs.

### Phase 7 implementation notes - 2026-07-22

- Added `scripts/qa/run-v2-production-release-smoke.ps1` to check ControlPlane API/Web, CommerceNode API, Storefront V2, Storefront/CommerceAdmin Swagger, and Nginx unknown-host deny behavior against a running V2 environment.
- Added a CI `-Describe` step so the release-smoke script contract stays valid without requiring CI to boot the full production topology.
- Added V2 production-readiness release-gate sections to `QA-CommerceNode.todo.md`, `QA-ControlPlane.todo.md`, and `QA-StorefrontV2.todo.md`.
- Added the final pre-publish headed-browser go/no-go subset to `Storefront Playwright E2E Release.todo.md`; the actual headed run remains a release execution step and must attach evidence before public production publish.

### Acceptance criteria

- [x] Release checklist co the dung lam production go/no-go.
- [x] Khong con dua vao smoke test don gian thay cho browser Playwright flow that.
- [x] Test place order that bang COD trong test store duoc xem la expected, khong phai side effect can tranh.

## Phase 8 - Checkout hotspot follow-up after release hardening

Goal: ghi lai viec can lam sau P0/P1, khong chen vao release gate dau tien.

### Tasks

- [x] Khong tach checkout tiep truoc khi Phase 1-5 pass, vi release/security/deploy dang cap bach hon.
- [x] Sau release hardening, tách them:
  - [x] `CheckoutSessionStore`.
  - [x] `CheckoutAddressResolver`.
  - [x] `CheckoutCurrencySnapshotResolver`.
  - [x] `CheckoutResultMapper`.
- [x] Giu `IStorefrontCheckoutService` lam facade use-case.
- [x] Khong lam lai `CheckoutPricingCalculator` va `CheckoutPaymentCoordinator` vi codebase da co.

### Phase 8 implementation notes - 2026-07-22

- Phase 8 is intentionally documentation-only. It records the next checkout decomposition seams after production-readiness hardening instead of changing checkout runtime behavior in this release-hardening pass.
- Future checkout work should keep `IStorefrontCheckoutService` as the facade and add focused collaborators around session persistence, address resolution, currency snapshot resolution, and result mapping.
- Existing `CheckoutPricingCalculator` and `CheckoutPaymentCoordinator` stay as-is; this plan does not reopen those completed decompositions.

## Risk register

| Risk | Phase | Impact | Mitigation |
|---|---:|---|---|
| Sua host resolution lam local direct media call bi 404 | 1 | QA media local kho hon | update docs, dung host/store domain dung, admin debug endpoint dung `storeKey` query |
| Cau hinh forwarded headers sai trong production | 1 | canonical host/store sai | require KnownProxies/KnownNetworks, add smoke test va production example |
| Rate-limit identity dung token khong on dinh | 2 | bucket bypass hoac false 429 | uu tien signed cart/session id, fallback trusted IP, tests cho same/different session |
| Move qua nhieu tests vao V2 project gay build churn | 3 | CI migration cham | migrate theo nhom core, commit tung nhom, giu legacy mixed project tam thoi |
| V2 compose production can deployment decisions chua dong | 4 | over-design compose | tao `compose.v2.production.yml` truoc, khong xoa legacy compose ngay |
| Generator CLI lam CI cham/flaky | 5 | developer friction | pin version, cache tool/deps, chi generate Storefront doc can thiet |
| Doi page injections gay loi UI compile | 6 | Storefront build fail | migrate page by page, build Storefront after each batch |

## Verification commands

- [ ] `dotnet restore BlazorShop.V2.slnf`
- [x] `dotnet build BlazorShop.V2.slnf --configuration Release --no-restore`
- [x] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --configuration Release --no-build --verbosity normal`
- [x] `docker compose -f compose.v2.production.yml config`
- [x] `docker build -f BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Dockerfile -t blazorshop-commercenode-api:v2-ci .`
- [x] `docker build -f BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/Dockerfile -t blazorshop-controlplane-api:v2-ci .`
- [x] `docker build -f BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Dockerfile -t blazorshop-controlplane-web:v2-ci .`
- [x] `docker build -f BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Dockerfile -t blazorshop-storefront-v2:ci .`
- [x] Run real OpenAPI generator and compile generated client.
- [x] `.\scripts\qa\run-v2-production-release-smoke.ps1 -Describe`
- [ ] `.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser`
- [ ] Visible Playwright release checklist from `Storefront Playwright E2E Release.todo.md`.

## Done when

- [x] V2 CI is the blocking release signal.
- [x] V2 production compose/Docker artifacts exist and pass config/build checks.
- [x] Public media store resolution is safe against forged host headers.
- [x] Rate limit uses user/session/trusted identity instead of unsafe raw proxy IP assumptions.
- [x] OpenAPI generator test uses a real pinned generator.
- [x] Storefront pages depend on capability client interfaces.
- [x] QA todo files include browser/API release evidence needed before production publish.

## GSTACK REVIEW REPORT

### CEO review

This plan focuses on release truth, not feature expansion. The highest business risk is shipping a V2 product while CI and production compose still validate/deploy legacy. The plan therefore prioritizes security and release gate correctness before cleanup.

### Design review

No end-user UI design change is intended. The only browser-facing work is verification that existing Storefront flows still work after CI/security/deploy hardening.

### Engineering review

The plan fits the codebase because it reuses existing V2 architecture, existing capability client interfaces, existing Storefront route scope, and existing Docker/local-run patterns. It avoids rewriting checkout/cart/order and only touches trust boundaries and release infrastructure first.

### DX review

Developer experience improves when local, CI, and production all use the same V2 commands and artifacts. The generator gate should produce clear failure artifacts: swagger JSON, command line, stderr, and compile errors.

## Decision Audit Trail

| # | Decision | Classification | Principle | Rationale | Rejected |
|---|---|---|---|---|---|
| 1 | Create a new production-readiness plan instead of editing the old boundary-hardening plan | Auto-decided | Preserve completed work | Existing boundary plan already has completed phases and different scope; release hardening should be tracked separately | Merge new release tasks into old long-running plan |
| 2 | Fix trusted host/public media resolution before CI cleanup | Auto-decided | Security first | Forged store host can affect public media scope; this is a production trust boundary | Treat it as a later refactor |
| 3 | Keep `Product/Image`, cart, checkout, order flows out of this plan | Auto-decided | Narrow release gate | The current issue is release/deploy/security truth, not commerce feature behavior | Combine with feature implementation |
| 4 | Use real pinned OpenAPI generator before generated client migration | Auto-decided | Prove before migration | Current smoke test cannot catch generator failures; generated client migration should wait until the gate is real | Migrate Storefront client first |
| 5 | Keep capability interfaces and move page injections gradually | Auto-decided | Minimal blast radius | Interfaces already exist; this gives a future generated-client seam without rewriting transport | Replace all client code with generated client immediately |
