# Claude Adapter

This adapter is a thin pointer to the canonical visual skill files. Do not copy skill bodies here.

Canonical paths:

- `tools/BlazorShop.AI.Visual/skills/storefront-visual-plan/SKILL.md`
- `tools/BlazorShop.AI.Visual/skills/storefront-visual-implement/SKILL.md`
- `tools/BlazorShop.AI.Visual/skills/storefront-visual-qa/SKILL.md`

Recommended Claude invocation pattern:

1. Ask Claude to read the canonical `SKILL.md` for the visual phase you need.
2. Provide the generated project root and the current StorefrontBuilder artifact paths.
3. Tell Claude that adapter files are pointers only and canonical instructions stay under `tools/BlazorShop.AI.Visual/skills/`.
4. Require generated-project-local reports and StorefrontBuilder gate evidence before closure.

If these skills are not installed in the user's skill root, invoke them explicitly by path and keep all updates in the repository workspace.
