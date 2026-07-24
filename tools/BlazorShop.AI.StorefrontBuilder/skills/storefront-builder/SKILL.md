---
name: storefront-builder
description: Development-time visual reverse engineering workflow for generating a safe BlazorShop Storefront.{Name} project from Starter.
---

# Storefront Builder

Use this skill only for development-time StorefrontBuilder workflows.

Required boundaries:

- Read `BlazorShop.Storefront.Starter/starter-generation.contract.yaml`.
- Generate into `BlazorShop.PresentationV2/BlazorShop.Storefront.{Name}`.
- Do not write store-specific presentation into Starter.
- Do not edit generated client, Runtime security primitives, BFF transport, cart commands, checkout commands, or package manifests unless a human explicitly reopens those contracts.
- Record evidence and inference artifacts under `docs/storefront-analysis` in the generated project.
