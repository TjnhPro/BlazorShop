# StorefrontBuilder Functional Commerce Report

Base URL: http://127.0.0.1:18991

## Checks

- [x] Home renders
- [x] Catalog renders
- [x] Product renders
- [x] Product link navigation works
- [x] Product image/gallery region renders
- [x] Quantity control can change
- [x] Add-to-cart command works through same-origin BFF
- [x] Cart badge updates
- [x] Cart page renders
- [x] Checkout route renders
- [x] Account route renders
- [x] Login/register shell renders according to store policy
- [x] Product SEO initial HTML exists
- [x] Browser does not call Commerce Node protected APIs directly

## Browser Network Guard

- No direct Commerce Node browser calls detected.

## Payment Notes

- COD order placement requires a configured test store/env.
- PayPal/Stripe production providers are outside this MVP gate.
