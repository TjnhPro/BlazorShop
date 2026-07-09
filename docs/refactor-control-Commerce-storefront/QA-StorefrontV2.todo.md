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
- [ ] Client app, only when testing login/register/checkout UI handoff.

## Clean DB Setup

- [x] Start Commerce Node DB:

```powershell
docker compose -f compose.commercenode.yml up -d
```

- [x] Apply Commerce Node migrations:

```powershell
dotnet ef database update --project BlazorShop.Infrastructure/BlazorShop.Infrastructure.csproj --startup-project BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --context CommerceNodeDbContext
```

- [ ] Seed at least one published category.
- [ ] Seed at least two published products.
- [ ] Seed at least one product variant.
- [ ] Seed one Storefront customer account.
- [ ] Seed SEO settings and one redirect rule.

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

- [ ] `/`
  - [ ] Shows published category data from Commerce Node.
  - [ ] Shows published product cards from Commerce Node.
  - [ ] Does not require legacy `BlazorShop.API`.
- [ ] `/category/{slug}`
  - [ ] Shows category breadcrumb.
  - [ ] Shows products under category.
  - [ ] Missing slug returns noindex 404 state.
- [ ] `/product/{slug}`
  - [ ] Shows product image.
  - [ ] Shows product price.
  - [ ] Shows product category link.
  - [ ] Shows variants selector when variants exist.
  - [ ] Shows add-to-cart button.
  - [ ] Shows related products/recommendations block.
  - [ ] Missing slug returns noindex 404 state.
- [ ] `/new-releases`
  - [ ] Shows latest published products.
- [ ] `/todays-deals`
  - [ ] Shows discount/deal products when available.

## Cart Flow

- [x] `/my-cart` renders without Commerce Node for empty cart smoke.
- [ ] Cart badge updates after add-to-cart.
- [ ] Add product without variant.
- [ ] Add product with required variant.
- [ ] Product detail blocks add-to-cart until required variant is selected.
- [ ] Quantity update works.
- [ ] Remove item works.
- [ ] Clear cart works.
- [ ] Cart refreshes product details from `api/internal/catalog/products/{id}`.
- [ ] Invalid cart cookie does not crash page.
- [ ] Unavailable product in cart shows warning state.
- [ ] Cart route stays private/noindex.

## Auth And Checkout Handoff

- [x] Anonymous `/checkout` redirect is covered by automated smoke test.
- [ ] Refresh-token cookie name is compatible:
  - `__Host-blazorshop-refresh`
- [ ] Storefront V2 calls `api/internal/auth/refresh-token`.
- [ ] Authenticated `/checkout` redirects to client app checkout.
- [ ] Anonymous `/checkout` redirects to client app login checkout path.
- [ ] `/signin` redirects to client app login.
- [ ] `/register` redirects to client app register.
- [ ] Missing Commerce Node auth endpoint degrades to anonymous and does not crash.

## SEO And Discovery

- [x] `/robots.txt` route is covered by automated smoke test.
- [x] `/sitemap.xml` route is covered by automated smoke test.
- [ ] `SeoHead` renders title.
- [ ] `SeoHead` renders meta description.
- [ ] `SeoHead` renders canonical URL.
- [ ] `SeoHead` renders robots meta.
- [ ] `SeoHead` renders OpenGraph.
- [ ] `JsonLdScript` renders structured data.
- [ ] `/sitemap.xml` uses `api/internal/catalog/sitemap`.
- [ ] `/robots.txt` points at V2 public sitemap URL.
- [ ] Redirect middleware uses `api/internal/seo/redirects/resolve`.
- [ ] Missing route has no canonical and includes noindex.
- [ ] Commerce Node downtime has noindex 503 surface.

## Static Assets

- [ ] `/css/storefront.css` returns CSS.
- [ ] `/js/storefrontCommerce.js` returns JS.
- [ ] Linked shared assets from `BlazorShop.Presentation/BlazorShop.Web/wwwroot` load from V2 project location.
- [ ] No missing image/static asset requests on home/category/product pages.

## Failure States

- [x] Commerce Node down returns Storefront V2 service-unavailable behavior instead of legacy fallback.
- [ ] Empty catalog shows empty state and does not crash.
- [ ] Missing category slug returns 404/noindex.
- [ ] Missing product slug returns 404/noindex.
- [ ] Invalid cart cookie is ignored or reset safely.
- [ ] Commerce Node returns `success=false` and Storefront shows safe error state.

## Cutover Readiness

- [x] Storefront V2 project exists in solution.
- [x] Storefront V2 builds.
- [x] Storefront V2 can run while legacy Storefront remains untouched.
- [x] Storefront V2 default API base URL points to Commerce Node local API.
- [x] Legacy fallback is disabled by default.
- [ ] Browser QA passes for home/category/product.
- [ ] Browser QA passes for cart.
- [ ] Browser QA passes for checkout handoff.
- [ ] SEO/discovery browser QA passes.
- [ ] Reverse proxy/deployment route for V2 is defined.

## Suggested Extra QA

- [ ] Verify V2 and legacy Storefront can run side by side on different ports.
- [ ] Verify V2 does not require `BlazorShop.Presentation/BlazorShop.API` process.
- [ ] Verify `Api:EnableLegacyFallback=true` only for emergency rollback and never in default config.
- [ ] Verify production config requires explicit `Api:BaseUrl`.
- [ ] Verify public reverse proxy does not expose Commerce Node `api/internal/*` directly.
