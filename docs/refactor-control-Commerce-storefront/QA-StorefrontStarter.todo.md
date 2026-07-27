# QA Storefront Starter Todo

## Scope

This QA checklist tracks `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter` as the neutral second consumer for Storefront Presentation.

Starter is not the production storefront. Its QA gate proves that Presentation can run outside Storefront V2 visual source and that independent package restore/build still works for generated storefronts.

## Required Checks

- [x] Starter builds with Runtime and Components packages plus monorepo Presentation ProjectReference.
- [x] Starter does not reference Storefront V2, backend/core/API projects, Control Plane Web, or `Web.SharedV2`.
- [x] Starter source does not directly compile against `BlazorShop.Storefront.Client` types and does not copy `Generated/StorefrontClient.g.cs`.
- [x] Starter visual views do not declare `@page`; Presentation owns route pages.
- [x] Starter visual views do not render `PageTitle`, `HeadContent`, `StorefrontSeoHead`, or response-status/header metadata.
- [x] Starter registered views use Presentation context parameters for home, catalog, product, search, cart, checkout, account, content, system, and payment surfaces.
- [x] Presentation page services and endpoint dependencies resolve with Starter view registration.
- [x] Starter HTTP smoke covers `/`, `/category/{slug}`, `/product/{slug}`, `/search?q=...`, `/my-cart`, `/checkout`, `/account`, `/robots.txt`, and `/sitemap.xml`.
- [x] Independent Starter package proof rewrites Presentation to PackageReference, restores from the local feed, builds, publishes, and rejects monorepo ProjectReference/backend/V2/Web.SharedV2 source paths.

## Evidence

- 2026-07-27 SPF21: `StorefrontStarterHostSmokeTests` proved Starter routes for `/`, `/product/{slug}`, `/category/{slug}`, `/search?q=...`, `/my-cart`, `/checkout`, `/account`, `/robots.txt`, and `/sitemap.xml` through `WebApplicationFactory<BlazorShop.Storefront.Starter.Program>`.
- 2026-07-27 SPF22: `run-storefront-starter-isolation-gate.ps1` packed Client/Runtime/Presentation/Components, rewrote isolated Starter Presentation ProjectReference to PackageReference, restored, built, and published from a local feed.
- 2026-07-27 SPF23: focused Storefront test gate passed `172/172`; Starter build and isolation proof passed after the final package/dependency cleanup.
