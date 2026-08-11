# Storefront V2 Release QA

## Purpose

This is the active, runnable release gate for `BlazorShop.Storefront.V2`. Historical execution evidence, old route assertions, prior screenshots, timestamps, and order references are preserved in [QA-StorefrontV2-History.md](archive/QA-StorefrontV2-History.md).

## Current Setup

- [x] Start the local V2 stack with `./scripts/run-v2-local.ps1 -StopExisting -NoOpenBrowser`.
- [x] Confirm Storefront V2 is healthy at `http://localhost:18598` and Commerce Node is healthy at `http://localhost:5180`.
- [x] Use the scoped `default` store fixture and the dedicated QA customer; do not use legacy or `api/internal/*` routes.
- [x] Use a new, unique QA marker for mutable address data and remove it during the same run.

## Build And Test Gate

- [x] Run `dotnet build BlazorShop.sln -c Release --no-restore`.
- [x] Run `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore`.
- [x] Run the focused component-mode, render-mode, visual-boundary, browser-controller, commerce-flow, cart/checkout, and account ownership suites after relevant changes.

Known baseline notices: MessagePack advisory warnings (`NU1902`/`NU1903`) and the stale Browserslist database notice. Existing baseline skips must be recorded, not silently reclassified as passes.

## Browser Instrumentation

For every release journey, record console errors, page errors, request URLs, response statuses, and screenshots on failure.

- [x] No unexpected 5xx response.
- [x] No direct browser request to Commerce Node or `api/storefront/stores/*`.
- [x] No `/_blazor` circuit request.
- [x] No WebSocket or EventSource UI transport.
- [x] No unexpected console or page error.

Expected negative validation/not-found `400`/`404` responses are allowed only when the journey explicitly triggers them.

## Canonical Browser Journeys

### 1. Public catalog

- [x] Home, category/search, and product detail render with header/navigation, product grid/cards, gallery, purchase panel, price, availability, and actions.
- [x] Variant selection works when the fixture exposes variants.
- [x] Add-to-cart sends one same-origin mutation, updates the badge, and shows feedback/toast.
- [x] Desktop full flow and one mobile critical interaction pass.

### 2. Cart

- [x] Cart lines render; quantity update changes the summary with one mutation.
- [x] Removing the final line shows the empty state and correct checkout CTA state.
- [x] No direct Commerce Node request or unexpected console/page error occurs.

### 3. Checkout

- [x] Checkout accepts a valid fixture cart and validates contact/address input.
- [x] Current shipping rules and COD/sandbox payment selection are respected.
- [x] One place-order submit produces one mutation, the expected result/redirect, a visible order reference, and expected cart closure.

### 4. Account

- [x] QA login, profile, address create/update/default/delete cleanup, order list/detail, and invalid-password validation pass.
- [x] Invalid password remains visible and does not sign the customer out.
- [x] Desktop full flow plus mobile account navigation and one critical form interaction pass.

### 5. Content and security

- [x] Standard content and policy/FAQ/support fixture pages render when present.
- [x] Consent save and revoke/change work.
- [x] An anonymous account route redirects appropriately and an unknown route renders not-found UI.

### 6. SEO, network, and runtime

- [x] `robots.txt`, sitemap, canonical/meta tags, and applicable noindex pages are correct.
- [x] Same-origin/BFF-only browser traffic, no circuit transport, no unexpected 5xx, console error, or page error.

## Screenshot Matrix

- [x] Desktop and mobile: home, category, search, product, cart, checkout, account, and content.
- [x] Additional states: empty cart, toast, consent, account address cards, order detail, and payment result.
- [x] Tablet is optional when tooling/time allows; screenshot review is visual evidence, not pixel-perfect automation.

## Release Sign-off

- [x] All six canonical journeys pass with instrumentation.
- [x] No direct Commerce Node/circuit transport regression is present.
- [x] Required screenshots are captured and reviewed.
- [x] Current warnings/skips are documented above.
- [x] Final test and architecture closure evidence is recorded in `Storefront V2 Final Visual Cleanup Closure.todo.md`.
