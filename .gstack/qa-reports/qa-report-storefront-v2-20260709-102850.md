# QA Report - Storefront V2

Date: 2026-07-09T10:28:50.8874588+07:00
Base URL: http://localhost:18598

## Environment

- Commerce Node API: `http://localhost:5180`
- Commerce Node DB: Docker `blazorshop-commercenode-postgres`, host port `5434`
- Storefront V2: `http://localhost:18598`
- Browser QA: Playwright MCP, visible Chromium session

## Result

- Public catalog home/category/product/new-releases/todays-deals: passed.
- Cart flow add/update/remove/clear/invalid-cookie/noindex: passed.
- Anonymous checkout/signin/register handoff redirects: passed.
- SEO robots/sitemap/meta/OpenGraph/JSON-LD/redirect/missing-slug noindex: passed.
- Static CSS/JS/shared images: passed after QA seed image cleanup.
- Mobile home/product viewport smoke: passed.

## Issues Found

### ISSUE-001 - Storefront sitemap returned 503

- Severity: High
- Surface: `GET /sitemap.xml`
- Cause: Commerce Node internal sitemap endpoint queried category and product repositories concurrently while sharing one scoped EF `DbContext`.
- Fix: `PublicCatalogService.GetPublishedSitemapAsync` now loads sitemap categories and products sequentially.
- Regression test: `PublicCatalogServiceTests.GetPublishedSitemapAsync_LoadsRepositoriesSequentially`
- Commit: `0296a7b fix(qa): ISSUE-001 - avoid concurrent sitemap queries`
- Verification: `GET /sitemap.xml` now returns `200 OK`; `GET /api/internal/catalog/sitemap` returns success.

### DATA-001 - QA product image URLs caused browser console failures

- Severity: Low
- Surface: home/category/product image requests
- Cause: seed data used `https://example.test/*` image URLs.
- Fix: QA DB rows were updated to `/images/banner-bg.jpg`.
- Verification: no missing image/static asset requests on checked pages.

### DATA-002 - QA SEO redirect pointed at old plural route

- Severity: Low
- Surface: `/legacy-qa-product-20260708234046`
- Cause: seed redirect target used `/products/...` while V2 route is `/product/...`.
- Fix: QA DB redirect target updated to `/product/qa-product-20260708234046`.
- Verification: route now returns `301 Location: /product/qa-product-20260708234046`.

## Remaining Open QA

- Storefront customer account seed and authenticated checkout.
- Refresh-token cookie compatibility and `api/internal/auth/refresh-token`.
- Missing Commerce Node auth endpoint degradation.
- Unavailable product in cart warning state.
- Empty catalog state.
- Commerce Node `success=false` UI error surface.
- Reverse proxy/deployment route for V2.
- Production config enforcement for explicit `Api:BaseUrl`.
- Public reverse proxy guard that prevents direct exposure of `api/internal/*`.

## Screenshots

- `screenshots/storefront-v2-home-after-seed-image-fix.png`
- `screenshots/storefront-v2-category.png`
- `screenshots/storefront-v2-product-variant.png`
- `screenshots/storefront-v2-cart-with-item.png`
- `screenshots/storefront-v2-new-releases.png`
- `screenshots/storefront-v2-todays-deals.png`
- `screenshots/storefront-v2-mobile-home.png`
- `screenshots/storefront-v2-mobile-product.png`
