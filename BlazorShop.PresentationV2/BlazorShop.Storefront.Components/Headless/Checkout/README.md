# Checkout Headless Behavior

Headless checkout behavior lives here after migration. It may expose checkout state/actions over host-supplied commands, but it must not hardcode `/api/checkout` paths or visual shell layout.

Headless code may consume Contracts and intentional Browser primitives, but it must not import Features compatibility wrappers.
