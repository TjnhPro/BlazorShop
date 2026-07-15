# QA Report: Store Lifecycle

Date: 2026-07-15

Scope:

- Store active/inactive and provisioning readiness behavior.
- Store closed/maintenance page and redirect behavior.
- Store company/contact information shown on Storefront maintenance state.
- Store display-order and profile ownership assumptions from the implemented lifecycle phase.

Environment:

- Storefront V2: `http://localhost:18598`
- Fake scoped CommerceNode current-store API: `http://localhost:5199/api/`
- Storefront config: `StoreResolution:RequireCurrentStore=true`, `Api:StoreKey=default`
- Browser QA: Playwright Chromium. `gstack browse` was unavailable in this shell because `/bin/bash` was missing, so Playwright MCP was used.

Final Result: DONE_WITH_CONCERNS

- Overall health: 98/100.
- Main concern: the maintenance page intentionally returns HTTP 503, so browser console/resource logs include the expected navigation resource error for that document. No app JavaScript/page errors were observed.
- Existing package vulnerability warnings remained during build; they were not introduced by this phase.

Issue Found And Fixed:

- ISSUE-001, High, functional: HTML requests for an unavailable store returned `503` with a `Location` header instead of a real redirect. Browsers did not follow the redirect and Playwright failed navigation with `net::ERR_HTTP_RESPONSE_CODE_FAILURE`.
- Root cause: `StorefrontResponseHeaders.ApplyServiceUnavailable(context)` registered an `OnStarting` callback before `Response.Redirect(...)`, which overwrote the redirect status.
- Fix: HTML unavailable responses now apply private/noindex headers before redirecting. The target maintenance page still returns HTTP 503.
- Regression: `StorefrontCurrentStoreMiddlewareRegressionTests.HtmlUnavailableRedirect_RemainsRedirectWhenResponseStarts`.

HTTP Matrix After Fix:

| State | `/` status | `/` location | `/maintenance` status | Expected text | Support contact |
| --- | ---: | --- | ---: | --- | --- |
| maintenance | 302 | `/maintenance?reason=maintenance` | 503 | yes | yes |
| closed | 302 | `/maintenance?reason=closed` | 503 | yes | yes |
| not-ready | 302 | `/maintenance?reason=not-ready` | 503 | yes | yes |

Static asset check:

- `GET /_framework/blazor.web.js` returned 200 while the store was blocked.

Browser Evidence:

- `.gstack/qa-reports/screenshots/store-lifecycle-maintenance-after.png`
- `.gstack/qa-reports/screenshots/store-lifecycle-closed.png`
- `.gstack/qa-reports/screenshots/store-lifecycle-not-ready.png`

Verification:

- `dotnet build BlazorShop.sln --no-restore` passed with existing warnings.
- `dotnet test BlazorShop.Tests\BlazorShop.Tests.csproj --no-build --filter "FullyQualifiedName~StorefrontCurrentStoreMiddlewareRegressionTests|FullyQualifiedName~StorefrontCurrentStore"` passed 10/10.
- `dotnet test BlazorShop.Tests\BlazorShop.Tests.csproj --no-build --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests|FullyQualifiedName~StorefrontCurrentStore|FullyQualifiedName~ControlPlaneStore"` passed 37/37.
- `dotnet test BlazorShop.Tests\BlazorShop.Tests.csproj --no-build --filter "FullyQualifiedName~StorefrontStructuredData|FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests"` passed 28/28.
