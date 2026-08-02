# Architecture Boundary Reference

Version: `0.1.0`
Provenance: StorefrontBuilder Phase 4.10 MVP visual skill workspace.

## Ownership

StorefrontBuilder owns generation, regeneration, generated file manifests, constrained visual write recording, visual QA command execution, and bounded mechanical repair helpers.

ReverseEngineering owns source evidence capture, reviewed analysis, portable `analysis/agent-handoff/*` packages, readiness, schema-backed handoff validation, and handoff package hashes.

Visual skills own planning, visual-only implementation guidance, checkpoint evidence, and QA reports around generated storefront visual files. They do not create generated projects directly and they do not reinterpret raw source evidence.

## Boundaries

Visual skills may read StorefrontBuilder generated artifacts under a generated project `docs/storefront-analysis/` folder. They may write only generated-project-local visual plan, checklist, checkpoint, implementation report, QA report, and generated-owned visual files approved by the task package.

Visual skills must not edit StorefrontBuilder, ReverseEngineering, Starter, Presentation, Runtime, Client, Browser, commerce services, control services, database code, OpenAPI contracts, BFF behavior, SEO behavior, auth/session behavior, cart behavior, checkout behavior, account behavior, payment behavior, or order behavior.

Browser interactions must stay same-origin and descriptor-driven. Visual skills must not add direct calls to platform, admin, commerce, or legacy API routes.
