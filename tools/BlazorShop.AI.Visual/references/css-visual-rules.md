# CSS Visual Rules

Version: `0.1.0`
Provenance: StorefrontBuilder Phase 4.10 MVP visual skill workspace.

## Responsive Rules

Generated CSS must support desktop, tablet, and mobile states for files in scope. Use stable layout constraints, predictable grid/flex behavior, and explicit media or container rules where needed.

Do not use hidden overflow masking as a general fix. Overflow hiding is allowed only for deliberate media cropping or local decoration that cannot hide controls, text, focus outlines, product information, or actionable UI.

Do not add blocking overlays, full-page visual masks, or z-index layers that can cover header navigation, product actions, cart/account/checkout entry points, forms, or browser action descriptors.

## Visual Quality Rules

Avoid one-note palettes. Ecommerce pages should remain scanable: product title, price, availability, variants, quantity, add-to-cart, cart summary, checkout entry, and account entry points must be easy to find.

Product gallery frames should remain 1:1 when the gallery is in scope. Images must not be stretched or clipped in a way that prevents product inspection.

Text must not overlap, clip, or depend on viewport-width font scaling. Controls should keep stable dimensions across hover, loading, empty, and error states.
