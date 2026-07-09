# QA Storefront V2 Todo

## Scope

QA nay theo doi `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`.

Muc tieu hien tai:

- Xac nhan Storefront V2 copy/reuse tu legacy Storefront ma khong sua legacy project.
- Xac nhan V2 goi Commerce Node `api/internal/*` mac dinh.
- Xac nhan legacy API fallback bi tat mac dinh.
- Xac nhan cac route Storefront core chay duoc truoc khi cat over.

## Required Services

- [x] Commerce Node PostgreSQL container.
  - Compose file: `compose.commercenode.yml`
  - Container: `blazorshop-commercenode-postgres`
  - Host port: `5434`
- [x] Commerce Node API.
  - Project: `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API`
  - Local HTTP URL: `http://localhost:5180`
- [x] Storefront V2.
  - Project: `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`
  - Local HTTP URL: `http://localhost:18598`
- [n/a] Client app, only when testing login/register/checkout UI handoff. 2026-07-09: StorefrontV2 returned the expected 302 handoff; external client app was intentionally not started for independent V2 QA.

## Clean DB Setup

- [x] Start Commerce Node DB:

```powershell
docker compose -f compose.commercenode.yml up -d
```

- [x] Apply Commerce Node migrations:

```powershell
dotnet ef database update --project BlazorShop.Infrastructure/BlazorShop.Infrastructure.csproj --startup-project BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --context CommerceNodeDbContext
```

- [x] Seed at least one published category.
  - 2026-07-09: QA categories loaded from Commerce Node DB.
- [x] Seed at least two published products.
  - 2026-07-09: QA products loaded from Commerce Node DB.
- [x] Seed at least one product variant.
  - 2026-07-09: variant selector verified on product detail.
- [x] Seed one Storefront customer account. 2026-07-09: API smoke registered and logged in a QA customer through `api/internal/auth`.
- [x] Seed SEO settings and one redirect rule.
  - 2026-07-09: redirect rule verified with 301 to `/product/qa-product-20260708234046`.

## Automated Verification

- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj`
- [x] `dotnet build BlazorShop.sln`
- [x] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter FullyQualifiedName~PresentationV2.Storefront`
  - 2026-07-09: passed 7/7.
- [x] `dotnet test BlazorShop.sln`
  - 2026-07-09: passed with 482 passed, 10 skipped.
- [x] API client parses API response envelope.
- [x] API client calls Commerce Node internal catalog route.
- [x] API client does not call legacy fallback when `Api:EnableLegacyFallback=false`.
- [x] API client can use legacy fallback only when explicitly enabled.
- [x] Checkout anonymous redirect is covered by V2 host smoke test.
- [x] Robots route is covered by V2 host smoke test.
- [x] Sitemap route is covered by V2 host smoke test.
- [x] Empty cart route is covered by V2 host smoke test.
- [x] Storefront V2 references `BlazorShop.Web.SharedV2`, not legacy `BlazorShop.Web.Shared`.
  - 2026-07-09: covered by PresentationV2 boundary tests.
- [x] After SharedV2 changes, re-run catalog/cart smoke and `FullyQualifiedName~PresentationV2.Storefront`. 2026-07-09: full `dotnet test BlazorShop.sln --no-restore` passed 485/485 with 10 skipped; Playwright catalog/product/cart smoke passed.

## Runtime Smoke

- [x] Start Commerce Node API with launch profile `http`.
- [x] Call `GET http://localhost:5180/api/commerce/healthz` with headers:
  - `X-Node-Key: dev-node`
  - `X-Node-Secret: dev-node-secret`
  - 2026-07-09: returned 200.
- [x] Start Storefront V2.
- [x] Load `GET http://localhost:18598/`.
  - 2026-07-09: returned 200 while Commerce Node API was running.
- [x] Stop Commerce Node API and reload Storefront V2 `/`.
  - 2026-07-09: returned 503 and did not show legacy fallback markers.

## Public Catalog Pages

- [x] `/`
  - [x] Shows published category data from Commerce Node.
  - [x] Shows published product cards from Commerce Node.
  - [x] Does not require legacy `BlazorShop.API`.
  - 2026-07-09: verified with legacy API process not running.
- [x] `/category/{slug}`
  - [x] Shows category breadcrumb.
  - [x] Shows products under category.
  - [x] Missing slug returns noindex 404 state.
- [x] `/product/{slug}`
  - [x] Shows product image.
  - [x] Shows product price.
  - [x] Shows product category link.
  - [x] Shows variants selector when variants exist.
  - [x] Shows add-to-cart button.
  - [x] Shows related products/recommendations block.
  - [x] Missing slug returns noindex 404 state.
- [x] `/new-releases`
  - [x] Shows latest published products.
- [x] `/todays-deals`
  - [x] Shows discount/deal products when available.

## Cart Flow

- [x] `/my-cart` renders without Commerce Node for empty cart smoke.
- [x] Cart badge updates after add-to-cart.
- [x] Add product without variant.
- [x] Add product with required variant.
- [x] Product detail blocks add-to-cart until required variant is selected.
- [x] Quantity update works.
- [x] Remove item works.
- [x] Clear cart works.
- [x] Cart refreshes product details from `api/internal/catalog/products/{id}`.
- [x] Invalid cart cookie does not crash page.
- [~] Unavailable product in cart shows warning state. Code path exists in `CartPage` for missing catalog products; live QA used available seeded products only.
- [x] Cart route stays private/noindex.
  - 2026-07-09: verified `X-Robots-Tag`, noindex meta, and no-store/no-cache.

## Auth And Checkout Handoff

- [x] Anonymous `/checkout` redirect is covered by automated smoke test.
- [x] Refresh-token cookie name is compatible:
  - `__Host-blazorshop-refresh`
- [x] Storefront V2 calls `api/internal/auth/refresh-token`. 2026-07-09: verified `StorefrontSessionResolver` uses configured `Api:RefreshTokenRoute=internal/auth/refresh-token`.
- [~] Authenticated `/checkout` redirects to client app checkout. Code path maps authenticated sessions to `/account/checkout`; browser QA only covered anonymous handoff because the external client app was not running.
- [x] Anonymous `/checkout` redirects to client app login checkout path.
- [x] `/signin` redirects to client app login.
- [x] `/register` redirects to client app register.
- [~] Missing Commerce Node auth endpoint degrades to anonymous and does not crash. Session resolver catches refresh failure and returns anonymous; live QA did not disable only the auth endpoint.

## SEO And Discovery

- [x] `/robots.txt` route is covered by automated smoke test.
- [x] `/sitemap.xml` route is covered by automated smoke test.
- [x] `SeoHead` renders title.
- [x] `SeoHead` renders meta description.
- [x] `SeoHead` renders canonical URL.
- [x] `SeoHead` renders robots meta.
- [x] `SeoHead` renders OpenGraph.
- [x] `JsonLdScript` renders structured data.
  - 2026-07-09: verified `schema.org` structured data on home and product pages.
- [x] `/sitemap.xml` uses `api/internal/catalog/sitemap`.
  - 2026-07-09: fixed and verified sitemap after ISSUE-001.
- [x] `/robots.txt` points at V2 public sitemap URL.
- [x] Redirect middleware uses `api/internal/seo/redirects/resolve`.
- [x] Missing route has no canonical and includes noindex.
- [x] Commerce Node downtime has noindex 503 surface.

## Static Assets

- [x] `/css/storefront.css` returns CSS.
- [x] `/js/storefrontCommerce.js` returns JS.
- [x] Linked shared assets from `BlazorShop.Presentation/BlazorShop.Web/wwwroot` load from V2 project location.
- [x] No missing image/static asset requests on home/category/product pages.
  - 2026-07-09: QA seed image URLs were corrected from `https://example.test/*` to `/images/banner-bg.jpg`.

## Failure States

- [x] Commerce Node down returns Storefront V2 service-unavailable behavior instead of legacy fallback.
- [~] Empty catalog shows empty state and does not crash. Current QA database intentionally contains seeded catalog data; empty DB visual state remains a dataset-specific follow-up.
- [x] Missing category slug returns 404/noindex.
- [x] Missing product slug returns 404/noindex.
- [x] Invalid cart cookie is ignored or reset safely.
- [x] Commerce Node returns `success=false` and Storefront shows safe error state. 2026-07-09: Commerce Node downtime/failed catalog smoke returned Storefront service-unavailable behavior without legacy fallback or crash.

## Cutover Readiness

- [x] Storefront V2 project exists in solution.
- [x] Storefront V2 builds.
- [x] Storefront V2 can run while legacy Storefront remains untouched.
- [x] Storefront V2 default API base URL points to Commerce Node local API.
- [x] Legacy fallback is disabled by default.
- [x] Browser QA passes for home/category/product.
- [x] Browser QA passes for cart.
- [x] Browser QA passes for checkout handoff.
- [x] SEO/discovery browser QA passes.
- [n/a] Reverse proxy/deployment route for V2 is defined. Out of scope for local MVP independence QA.

## Suggested Extra QA

- [~] Verify V2 and legacy Storefront can run side by side on different ports. 2026-07-09: V2 was verified independently with legacy API/Storefront not running; explicit side-by-side runtime remains a cutover rehearsal item.
- [x] Verify V2 does not require `BlazorShop.Presentation/BlazorShop.API` process.
- [x] Verify `Api:EnableLegacyFallback=true` only for emergency rollback and never in default config.
- [x] Verify production config requires explicit `Api:BaseUrl`. 2026-07-09: `StorefrontOptionsValidators` enforce `Api:BaseUrl` outside Development when service discovery is absent.
- [n/a] Verify public reverse proxy does not expose Commerce Node `api/internal/*` directly. No reverse proxy/deployment config is part of this local MVP QA run.
