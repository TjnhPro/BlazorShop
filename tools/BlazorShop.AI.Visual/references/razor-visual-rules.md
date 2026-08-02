# Razor Visual Rules

Version: `0.1.0`
Provenance: StorefrontBuilder Phase 4.10 MVP visual skill workspace.

## Hard Rules

- Keep generated visual files as no @page files.
- Do not add `@page` to generated visual files.
- Do not add API transport, direct HTTP calls, fetch calls, or guessed endpoint URLs.
- Do not add business logic for pricing, inventory, cart, checkout, account, auth, payment, order, SEO, routing, media, or customer state.
- Do not assume direct auth, session, customer, order, or token state in generated markup.
- Preserve same-origin browser action descriptors and product purchase descriptors.
- Preserve component parameters, cascades, render fragments, semantic CSS hooks, and data attributes required by StorefrontBuilder and Presentation contracts.

## Allowed Visual Work

Generated Razor work may change static layout, hierarchy, copy placeholders, visual grouping, responsive markup structure, classes, decorative wrappers, and data-display composition when the target file is allowed by the task package.

Unsupported behavior must be recorded as a blocker instead of implemented through a new route, transport call, script controller, or business rule.
