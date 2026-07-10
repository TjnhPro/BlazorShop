# Domain Docs

How engineering skills should consume this repo's domain documentation when exploring the codebase.

## Layout

BlazorShop uses a **single-context** domain documentation layout.

The product is an ecommerce system. It currently includes legacy commerce/storefront projects plus new Control Plane and Commerce Node boundaries. Future plugins should still be treated as part of the same BlazorShop ecommerce product unless a later architecture decision explicitly splits the domain.

## Before Exploring, Read These

- **`AGENTS.md`** at the repo root.
- **`docs/architecture/README.md`** and the relevant architecture page for the area being changed.
- **`CONTEXT.md`** at the repo root, if it exists.
- **`docs/adr/`**, if it exists. Read ADRs that touch the area being changed.
- Existing planning docs under `docs/refactor-control-Commerce-storefront/` when working on Control Plane, Commerce Node, Storefront migration, or legacy cutover topics.

If `CONTEXT.md` or `docs/adr/` do not exist yet, proceed silently. Do not flag their absence or create them upfront unless the current task is specifically domain modeling or architecture documentation.

## File Structure

Single-context target layout:

```text
/
├── CONTEXT.md
├── AGENTS.md
├── docs/adr/
│   ├── 0001-example-decision.md
│   └── 0002-example-decision.md
├── docs/architecture/
│   ├── README.md
│   ├── 01-system-map.md
│   ├── 02-layered-architecture.md
│   ├── 03-runtime-boundaries.md
│   ├── 04-data-ownership.md
│   ├── 05-project-and-folder-guide.md
│   ├── 06-feature-map.md
│   ├── 07-deployment-and-local-run.md
│   └── 08-agent-decision-rules.md
└── docs/refactor-control-Commerce-storefront/
```

## Use The Glossary's Vocabulary

When output names a domain concept, use the term as defined in `CONTEXT.md` if present. Examples in this repo include Control Plane, Commerce Node, Storefront, legacy Presentation, Admin API, internal Storefront API, and node credential.

If a needed concept is missing from the glossary, either avoid inventing a new term or note the gap for a future domain-modeling pass.

## Control Plane Gateway Boundary

Treat `BlazorShop.ControlPlane.Web` as a UI-only client. It must only call `BlazorShop.ControlPlane.API`.

`BlazorShop.ControlPlane.API` is the gateway that may call `BlazorShop.CommerceNode.API`. It owns the security-sensitive distribution path: node key, node secret, allowed Control Plane IP, store key scope, audit, and platform permission checks.

When existing docs say "Control Plane calls Commerce Node", read that as:

```text
BlazorShop.ControlPlane.Web
  -> BlazorShop.ControlPlane.API
      -> BlazorShop.CommerceNode.API
```

Do not design or implement:

```text
BlazorShop.ControlPlane.Web
  -> BlazorShop.CommerceNode.API
```

Do not expose Commerce Node credentials, Commerce Node base URLs, or node security headers to `BlazorShop.ControlPlane.Web`.

## Flag ADR Conflicts

If an output contradicts an existing ADR, surface it explicitly rather than silently overriding it.
