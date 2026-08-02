# Visual Ownership Reference

Version: `0.1.0`
Provenance: StorefrontBuilder Phase 4.10 MVP visual skill workspace.

## Generated-Owned Visual Files

Generated-owned Razor files are visual components, visual page bodies, visual layout shells, and state shells that StorefrontBuilder marks as generated or managed in the task package and generated file manifest.

Generated-owned CSS files are StorefrontBuilder-generated visual CSS files such as `wwwroot/css/storefront-builder.generated.css` when listed by the generation plan and task package.

Generated-owned static assets are generated project-local files listed by the task package as allowed visual outputs. Asset edits must not create runtime transport, auth, SEO, or commerce behavior.

## Protected Files

Protected files include generated package metadata, starter contract files, route registration, BFF endpoints, SEO/media composition, app startup behavior, browser action binders, generated file manifests, and any path listed as protected by the StorefrontBuilder task package.

Protected descriptors include purchase, cart, checkout, account, consent, product-selection, gallery, route, and same-origin browser action descriptors required by Presentation and Browser contracts.

## Required Validation

Every implementation or repair pass must run StorefrontBuilder's visual write recorder with the changed generated visual file list. A changed file outside the task package allowed set is a blocker, not a candidate for manual override inside visual skills.
