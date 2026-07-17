const antiforgeryTokenSelector = 'meta[name="blazorshop-antiforgery-token"]';
const antiforgeryHeaderSelector = 'meta[name="blazorshop-antiforgery-header"]';

export function readAntiforgery() {
  const token = document.querySelector(antiforgeryTokenSelector)?.getAttribute("content");
  if (!token) {
    return null;
  }

  const headerName = document.querySelector(antiforgeryHeaderSelector)?.getAttribute("content") || "X-CSRF-TOKEN";
  return { headerName, token };
}
