# Codex Adapter

This adapter is a thin pointer to the canonical visual skill files. Do not copy skill bodies here.

Canonical paths:

- `tools/BlazorShop.AI.Visual/skills/storefront-visual-plan/SKILL.md`
- `tools/BlazorShop.AI.Visual/skills/storefront-visual-implement/SKILL.md`
- `tools/BlazorShop.AI.Visual/skills/storefront-visual-qa/SKILL.md`

Recommended Codex invocation pattern:

1. Ask Codex to read the canonical `SKILL.md` for the visual phase you need.
2. Provide the generated project root and the current StorefrontBuilder artifact paths.
3. Require Codex to follow the canonical skill file as the source of truth.
4. Require StorefrontBuilder recorder, visual QA, and gate commands exactly as listed by the canonical skill and project docs.

If these skills are not installed in the user's skill root, invoke them explicitly by path and keep all updates in the repository workspace.
