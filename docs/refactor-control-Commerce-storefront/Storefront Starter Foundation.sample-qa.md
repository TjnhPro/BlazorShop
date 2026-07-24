# Storefront Starter Foundation Sample QA

Date: 2026-07-24

Scope: S10 release gate evidence for the deterministic `BlazorShop.Storefront.Sample` generated from `BlazorShop.Storefront.Starter`.

## Gate Command

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
.\scripts\qa\run-storefront-sample-release-gate.ps1
```

For package/static verification without local Commerce Node dependencies:

```powershell
.\scripts\qa\run-storefront-sample-release-gate.ps1 -SkipRuntime
```

## Evidence Covered

- `Storefront.Client` and `Storefront.Runtime` are packed to the local feed before Sample restore.
- `Storefront.Sample` restores, builds, and publishes from package references.
- Sample source is rejected if it contains backend/core/API/V2 references, copied generated client source, or legacy manual `StorefrontApiClient`.
- Generated client compatibility is checked for cart and checkout clients, including the COD order operation, while provider callback/webhook routes remain excluded from the frontend client surface.
- Functional route coverage is checked for home, category, product, cart, checkout, payment result, auth shell, account host, maintenance, and not-found.
- Rendering and SEO conventions are checked for `HeadOutlet`, canonical links, product JSON-LD placeholder, sitemap, robots, account noindex, and noindex system pages.
- Security conventions are checked for antiforgery, same-origin cart BFF, HttpOnly cart token, SameSite policy, runtime error mapping tests for 401/403/409/422, and browser-facing token/Commerce URL absence.
- Performance guard conventions are checked for SSR initial snapshot ownership, no duplicate initial fetch policy, early add-to-cart placement, and account host isolation.

## Result

The S10 gate is a release readiness gate for the Starter/Sample foundation. It proves package isolation, boundary rules, generated-client adoption, route coverage, SEO/security conventions, publishability, and optional live route smoke for the generated Sample.
