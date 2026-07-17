const antiforgeryTokenSelector = 'meta[name="blazorshop-antiforgery-token"]';
const antiforgeryHeaderSelector = 'meta[name="blazorshop-antiforgery-header"]';
const badgeSelector = "[data-storefront-cart-badge]";
const cartChangedEventName = "blazorshop:cart-changed";

export function readAntiforgery() {
  const token = document.querySelector(antiforgeryTokenSelector)?.getAttribute("content");
  if (!token) {
    return null;
  }

  const headerName = document.querySelector(antiforgeryHeaderSelector)?.getAttribute("content") || "X-CSRF-TOKEN";
  return { headerName, token };
}

export function publishCartChanged(count) {
  const normalizedCount = Number.isFinite(count) && count > 0 ? Math.trunc(count) : 0;
  document.querySelectorAll(badgeSelector).forEach((badge) => {
    badge.textContent = normalizedCount > 99 ? "99+" : String(normalizedCount);
    badge.hidden = normalizedCount <= 0;
    badge.classList.toggle("hidden", normalizedCount <= 0);
  });
  document.dispatchEvent(new CustomEvent(cartChangedEventName, { detail: { count: normalizedCount } }));
}
