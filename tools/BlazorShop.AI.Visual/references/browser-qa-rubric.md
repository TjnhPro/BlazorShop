# Browser QA Rubric

Version: `0.1.0`
Provenance: StorefrontBuilder Phase 4.10 MVP visual skill workspace.

## Required Evidence

Visual QA must use browser evidence from StorefrontBuilder's `run-visual-qa.mjs` path or a report produced by that path. Compile-only, restore-only, or smoke-only output cannot pass visual QA.

Evidence must cover desktop, tablet, and mobile viewports when the generated project and fixture support those routes. Screenshots, per-page status, console or network failure summaries, CSS asset status, broken image summaries, overflow findings, blank-page findings, and placeholder findings must be available to the QA skill.

Use StorefrontBuilder's `--screenshot-root <path>` option as the stable evidence root convention. The default visual QA report remains generated-project-local at `docs/storefront-analysis/visual-qa-report.md`.

## Review Checklist

Inspect:

- blank or near-blank body state
- overlapping text or controls
- cropped buttons, inputs, headers, prices, and actions
- mobile navigation availability
- visible cart, account, and checkout entry points where applicable
- product gallery 1:1 presentation
- product price and action readability
- broken image placeholders
- visual hierarchy and ecommerce scanability
- required visual slots from the generation plan

Functional commerce flows remain covered by existing StorefrontBuilder browser gates and commerce regression gates. Visual QA may report functional-looking symptoms, but it must not replace those gates or repair business behavior.
