(function () {
  const cartApiRoute = "/api/cart";
  const buttonSelector = "[data-storefront-add-to-cart]";
  const badgeSelector = "[data-storefront-cart-badge]";
  const cartRemoveSelector = "[data-storefront-cart-remove]";
  const cartClearSelector = "[data-storefront-cart-clear]";
  const cartQuantitySelector = "[data-storefront-cart-quantity]";
  const selectionPreviewSelector = "[data-storefront-selection-preview]";
  const selectionQuantitySelector = "[data-storefront-selection-quantity]";
  const attributeControlSelector = "[data-storefront-attribute-control]";
  const addressSelectSelector = "[data-storefront-address-select]";
  const manualAddressSelector = "[data-storefront-manual-address]";
  const manualAddressFieldSelector = "[data-storefront-manual-address-field]";
  const toastRegionSelector = "[data-storefront-toast-region]";
  const toastTemplateSelector = "[data-storefront-toast-template]";
  const antiforgeryTokenSelector = 'meta[name="blazorshop-antiforgery-token"]';
  const antiforgeryHeaderSelector = 'meta[name="blazorshop-antiforgery-header"]';
  const consentBannerSelector = "[data-storefront-consent-banner]";
  const consentManageSelector = "[data-storefront-consent-manage]";
  const cartChangedEventName = "blazorshop:cart-changed";
  const pendingToastStorageKey = "blazorshop:storefront:pending-toast";
  const badgePollIntervalMs = 1500;
  const buttonResetDelayMs = 1600;
  const toastDurationMs = 5000;
  const buttonResetTimers = new WeakMap();
  const previewTimers = new WeakMap();
  let badgePollHandle = 0;

  function parseInteger(value, fallback = 0) {
    const parsed = Number.parseInt(value, 10);
    return Number.isFinite(parsed) ? parsed : fallback;
  }

  function updateBadgesFromCount(cartCount) {
    document.querySelectorAll(badgeSelector).forEach((badge) => {
      badge.textContent = cartCount > 99 ? "99+" : String(cartCount);
      badge.hidden = cartCount <= 0;
      badge.classList.toggle("hidden", cartCount <= 0);
    });
  }

  function applyCartSummary(summary) {
    const count = parseInteger(summary?.count ?? summary?.Count, 0);
    updateBadgesFromCount(count);
    document.dispatchEvent(new CustomEvent(cartChangedEventName, { detail: { count } }));
  }

  function readAntiforgeryHeader() {
    const token = document.querySelector(antiforgeryTokenSelector)?.getAttribute("content");
    const headerName = document.querySelector(antiforgeryHeaderSelector)?.getAttribute("content") || "X-CSRF-TOKEN";
    return token ? { headerName, token } : null;
  }

  async function sendConsentRequest(route, method, body) {
    const normalizedMethod = (method || "GET").toUpperCase();
    const options = {
      method: normalizedMethod,
      credentials: "same-origin",
      headers: { "Accept": "application/json" }
    };

    if (normalizedMethod !== "GET") {
      const antiforgery = readAntiforgeryHeader();
      if (antiforgery) {
        options.headers[antiforgery.headerName] = antiforgery.token;
      }
    }

    if (body !== undefined) {
      options.headers["Content-Type"] = "application/json";
      options.body = JSON.stringify(body);
    }

    const response = await fetch(route, options);
    const text = await response.text();
    const payload = text ? JSON.parse(text) : null;
    if (!response.ok) {
      throw new Error(payload?.message || payload?.Message || "Consent could not be updated.");
    }

    return payload;
  }

  function initConsentBanner() {
    const banner = document.querySelector(consentBannerSelector);
    if (!(banner instanceof HTMLElement)) {
      return;
    }

    const preferences = banner.querySelector("[data-storefront-consent-preferences]");
    const analytics = banner.querySelector("[data-storefront-consent-analytics]");
    const marketing = banner.querySelector("[data-storefront-consent-marketing]");

    if (!(preferences instanceof HTMLInputElement) || !(analytics instanceof HTMLInputElement) || !(marketing instanceof HTMLInputElement)) {
      return;
    }

    const applyState = (state) => {
      if (!state || state.enabled === false || state.bannerRequired === false) {
        banner.classList.add("hidden");
        return;
      }

      preferences.checked = Boolean(state.categories?.preferences);
      analytics.checked = Boolean(state.categories?.analytics);
      marketing.checked = Boolean(state.categories?.marketing);
      banner.classList.remove("hidden");
    };

    const save = async (selection) => {
      const state = await sendConsentRequest("/api/consent", "POST", selection);
      applyState({ ...state, bannerRequired: false });
    };

    banner.querySelector("[data-storefront-consent-essential]")?.addEventListener("click", () => {
      void save({ preferences: false, analytics: false, marketing: false });
    });
    banner.querySelector("[data-storefront-consent-selected]")?.addEventListener("click", () => {
      void save({ preferences: preferences.checked, analytics: analytics.checked, marketing: marketing.checked });
    });
    banner.querySelector("[data-storefront-consent-all]")?.addEventListener("click", () => {
      void save({ preferences: true, analytics: true, marketing: true });
    });
    banner.querySelector("[data-storefront-consent-revoke]")?.addEventListener("click", () => {
      void sendConsentRequest("/api/consent/revoke", "POST")
        .then(applyState)
        .catch(() => banner.classList.add("hidden"));
    });
    document.querySelectorAll(consentManageSelector).forEach((button) => {
      button.addEventListener("click", () => {
        banner.classList.remove("hidden");
      });
    });

    void sendConsentRequest("/api/consent/current", "GET")
      .then(applyState)
      .catch(() => banner.classList.add("hidden"));
  }

  async function sendCartRequest(route, method, body) {
    const normalizedMethod = (method || "GET").toUpperCase();
    const options = {
      method: normalizedMethod,
      credentials: "same-origin",
      headers: { "Accept": "application/json" }
    };

    if (normalizedMethod !== "GET") {
      const antiforgery = readAntiforgeryHeader();
      if (antiforgery) {
        options.headers[antiforgery.headerName] = antiforgery.token;
      }
    }

    if (body !== undefined) {
      options.headers["Content-Type"] = "application/json";
      options.body = JSON.stringify(body);
    }

    const response = await fetch(route, options);
    const text = await response.text();
    const payload = text ? JSON.parse(text) : null;
    if (!response.ok) {
      throw new Error(payload?.message || payload?.Message || "Cart could not be updated.");
    }

    return payload;
  }

  async function refreshCartSummary() {
    try {
      const summary = await sendCartRequest(cartApiRoute, "GET");
      applyCartSummary(summary);
    } catch {
      updateBadgesFromCount(0);
    }
  }

  function setFeedback(button, message, isError) {
    const feedbackSelector = button.dataset.feedbackTarget;
    if (!feedbackSelector) {
      return;
    }

    const feedbackElement = document.querySelector(feedbackSelector);
    if (!(feedbackElement instanceof HTMLElement)) {
      return;
    }

    feedbackElement.textContent = message;
    feedbackElement.classList.remove("text-emerald-700", "text-red-700");
    feedbackElement.classList.add(isError ? "text-red-700" : "text-emerald-700");
  }

  function flashButton(button) {
    const defaultLabel = button.dataset.defaultLabel || button.textContent.trim();
    const successLabel = button.dataset.successLabel || "Added";
    button.dataset.defaultLabel = defaultLabel;
    button.textContent = successLabel;

    const existingTimer = buttonResetTimers.get(button);
    if (existingTimer) {
      window.clearTimeout(existingTimer);
    }

    const timer = window.setTimeout(() => {
      button.textContent = button.dataset.defaultLabel || defaultLabel;
      buttonResetTimers.delete(button);
    }, buttonResetDelayMs);

    buttonResetTimers.set(button, timer);
  }

  function resolveToastTheme(level) {
    switch ((level || "info").toLowerCase()) {
      case "success":
        return { background: "rgba(20, 83, 45, 0.96)", accentBackground: "rgba(187, 247, 208, 0.18)", accentColor: "#dcfce7" };
      case "warning":
        return { background: "rgba(180, 83, 9, 0.96)", accentBackground: "rgba(253, 230, 138, 0.18)", accentColor: "#fef3c7" };
      case "error":
        return { background: "rgba(153, 27, 27, 0.96)", accentBackground: "rgba(254, 202, 202, 0.18)", accentColor: "#fee2e2" };
      default:
        return { background: "rgba(3, 105, 161, 0.96)", accentBackground: "rgba(186, 230, 253, 0.18)", accentColor: "#e0f2fe" };
    }
  }

  function resolveToastIcon(level) {
    switch ((level || "info").toLowerCase()) {
      case "success":
        return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-5 w-5"><path d="m5 13 4 4L19 7" /></svg>';
      case "warning":
        return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-5 w-5"><path d="M12 9v4" /><path d="M12 17h.01" /><path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0Z" /></svg>';
      case "error":
        return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-5 w-5"><circle cx="12" cy="12" r="10" /><path d="m15 9-6 6" /><path d="m9 9 6 6" /></svg>';
      default:
        return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="h-5 w-5"><circle cx="12" cy="12" r="10" /><path d="M12 16v-4" /><path d="M12 8h.01" /></svg>';
    }
  }

  function showToast(level, heading, message, duration = toastDurationMs) {
    const region = document.querySelector(toastRegionSelector);
    const template = document.querySelector(toastTemplateSelector);
    if (!(region instanceof HTMLElement) || !(template instanceof HTMLTemplateElement)) {
      return;
    }

    const fragment = template.content.cloneNode(true);
    const toast = fragment.querySelector("[data-storefront-toast]");
    const accent = fragment.querySelector("[data-storefront-toast-accent]");
    const headingElement = fragment.querySelector("[data-storefront-toast-heading]");
    const messageElement = fragment.querySelector("[data-storefront-toast-message]");
    const closeButton = fragment.querySelector("[data-storefront-toast-close]");

    if (!(toast instanceof HTMLElement) || !(accent instanceof HTMLElement) || !(headingElement instanceof HTMLElement) || !(messageElement instanceof HTMLElement)) {
      return;
    }

    const theme = resolveToastTheme(level);
    toast.style.backgroundColor = theme.background;
    accent.style.backgroundColor = theme.accentBackground;
    accent.style.color = theme.accentColor;
    accent.innerHTML = resolveToastIcon(level);
    headingElement.textContent = heading || "Info";
    messageElement.textContent = message || "An event occurred.";

    const dismiss = () => {
      if (toast.dataset.dismissed === "true") {
        return;
      }

      toast.dataset.dismissed = "true";
      toast.style.opacity = "0";
      toast.style.transform = "translateY(-8px)";
      window.setTimeout(() => toast.remove(), 180);
    };

    if (closeButton instanceof HTMLButtonElement) {
      closeButton.addEventListener("click", dismiss);
    }

    region.appendChild(fragment);
    window.requestAnimationFrame(() => {
      toast.style.opacity = "1";
      toast.style.transform = "translateY(0)";
    });

    window.setTimeout(dismiss, Math.max(1500, parseInteger(duration, toastDurationMs)));
  }

  function queueToastForNextLoad(level, heading, message, duration = toastDurationMs) {
    try {
      window.sessionStorage.setItem(pendingToastStorageKey, JSON.stringify({ level, heading, message, duration }));
    } catch {
      // Ignore storage restrictions; the cart mutation itself already succeeded.
    }
  }

  function flushQueuedToast() {
    try {
      const raw = window.sessionStorage.getItem(pendingToastStorageKey);
      if (!raw) {
        return;
      }

      window.sessionStorage.removeItem(pendingToastStorageKey);
      const toast = JSON.parse(raw);
      if (!toast || !toast.message) {
        return;
      }

      showToast(toast.level, toast.heading, toast.message, toast.duration);
    } catch {
      window.sessionStorage.removeItem(pendingToastStorageKey);
    }
  }

  function formatCartLabel(productName, sizeValue) {
    const resolvedName = (productName || "product").trim() || "product";
    const resolvedSize = (sizeValue || "").trim();
    return resolvedSize ? `${resolvedName} (size ${resolvedSize})` : resolvedName;
  }

  function findPreviewContainer(button) {
    const selector = button.dataset.previewContainer;
    if (selector) {
      const container = document.querySelector(selector);
      if (container instanceof HTMLElement) {
        return container;
      }
    }

    return button.closest(selectionPreviewSelector);
  }

  function readSelectionQuantity(container) {
    const input = container?.querySelector(selectionQuantitySelector);
    if (!(input instanceof HTMLInputElement)) {
      return 1;
    }

    const quantity = parseInteger(input.value, 1);
    return Math.max(1, quantity);
  }

  function collectSelectedAttributes(container) {
    if (!(container instanceof HTMLElement)) {
      return [];
    }

    const attributes = [];
    container.querySelectorAll(attributeControlSelector).forEach((control) => {
      if (!(control instanceof HTMLElement)) {
        return;
      }

      const name = (control.dataset.attributeName || "").trim();
      if (!name) {
        return;
      }

      if (control instanceof HTMLInputElement && control.type === "radio" && !control.checked) {
        return;
      }

      const value = (control.value || "").trim();
      if (!value) {
        return;
      }

      if (attributes.some((attribute) => attribute.Name.toLowerCase() === name.toLowerCase())) {
        return;
      }

      attributes.push({ Name: name, Value: value });
    });

    return attributes;
  }

  function resolveSelectedVariantId(button, container, includeResolvedVariant = true) {
    const variantSelectSelector = button.dataset.variantSelect;
    if (variantSelectSelector) {
      const select = document.querySelector(variantSelectSelector);
      if (select instanceof HTMLSelectElement && select.value) {
        return select.value.trim();
      }
    }

    return includeResolvedVariant
      ? (container?.dataset.resolvedVariantId || button.dataset.resolvedVariantId || "").trim()
      : "";
  }

  function buildSelectionPreviewPayload(container) {
    const button = container.querySelector(buttonSelector);
    if (!(button instanceof HTMLButtonElement)) {
      return { error: "This product cannot be previewed right now." };
    }

    const productId = (container.dataset.productId || button.dataset.productId || "").trim();
    if (!productId) {
      return { error: "This product cannot be previewed right now." };
    }

    const selectedAttributes = collectSelectedAttributes(container);
    return {
      payload: {
        ProductId: productId,
        ProductVariantId: resolveSelectedVariantId(button, container, false) || null,
        SelectedAttributes: selectedAttributes.length > 0 ? selectedAttributes : null,
        Quantity: readSelectionQuantity(container),
        CurrencyCode: (container.dataset.currencyCode || button.dataset.currencyCode || "").trim() || null
      },
      button
    };
  }

  function setText(element, text) {
    if (element instanceof HTMLElement) {
      element.textContent = text || "";
    }
  }

  function toggleHidden(element, hidden) {
    if (element instanceof HTMLElement) {
      element.classList.toggle("hidden", Boolean(hidden));
    }
  }

  function syncManualAddressFields(select) {
    if (!(select instanceof HTMLSelectElement)) {
      return;
    }

    const manualAddress = document.querySelector(manualAddressSelector);
    const useSavedAddress = Boolean((select.value || "").trim());
    toggleHidden(manualAddress, useSavedAddress);
    document.querySelectorAll(manualAddressFieldSelector).forEach((field) => {
      if (field instanceof HTMLInputElement || field instanceof HTMLSelectElement || field instanceof HTMLTextAreaElement) {
        field.disabled = useSavedAddress;
      }
    });
  }

  function initCheckoutAddressSelection() {
    document.querySelectorAll(addressSelectSelector).forEach((select) => {
      syncManualAddressFields(select);
    });
  }

  function applySelectionPreview(container, preview) {
    const button = container.querySelector(buttonSelector);
    const price = container.closest("main")?.querySelector("[data-storefront-selection-price]") || document.querySelector("[data-storefront-selection-price]");
    const compare = container.closest("main")?.querySelector("[data-storefront-selection-compare]") || document.querySelector("[data-storefront-selection-compare]");
    const stock = container.closest("main")?.querySelector("[data-storefront-selection-stock]") || document.querySelector("[data-storefront-selection-stock]");
    const sku = container.closest("main")?.querySelector("[data-storefront-selection-sku]") || document.querySelector("[data-storefront-selection-sku]");
    const message = container.querySelector("[data-storefront-selection-message]");
    const validationMessages = Array.isArray(preview.validationMessages)
      ? preview.validationMessages.filter(Boolean)
      : [];

    setText(price, preview.formattedUnitPrice || "");
    setText(compare, preview.formattedComparePrice || "");
    toggleHidden(compare, !preview.formattedComparePrice);
    setText(stock, preview.isAvailable ? `${preview.stockQuantity} in stock` : "Out of stock");
    setText(sku, preview.sku ? `SKU ${preview.sku}` : "");
    toggleHidden(sku, !preview.sku);
    setText(message, validationMessages[0] || (preview.canAddToCart ? "Selection ready." : "This selection is not available."));

    if (button instanceof HTMLButtonElement) {
      button.disabled = !preview.canAddToCart;
      button.dataset.resolvedVariantId = preview.productVariantId || "";
      button.dataset.unitPrice = String(preview.unitPrice ?? button.dataset.unitPrice ?? "");
      button.dataset.currencyCode = preview.currencyCode || button.dataset.currencyCode || "";
      button.dataset.stock = String(preview.stockQuantity ?? button.dataset.stock ?? "0");
    }

    container.dataset.resolvedVariantId = preview.productVariantId || "";
  }

  async function previewSelection(container) {
    const request = buildSelectionPreviewPayload(container);
    if (request.error) {
      return;
    }

    try {
      const preview = await sendCartRequest(container.dataset.previewRoute || "/api/product-selection-preview", "POST", request.payload);
      applySelectionPreview(container, preview);
    } catch (error) {
      const button = request.button;
      if (button instanceof HTMLButtonElement) {
        button.disabled = true;
        setFeedback(button, error instanceof Error ? error.message : "This selection could not be previewed.", true);
      }
    }
  }

  function scheduleSelectionPreview(container) {
    if (!(container instanceof HTMLElement)) {
      return;
    }

    const existing = previewTimers.get(container);
    if (existing) {
      window.clearTimeout(existing);
    }

    const timer = window.setTimeout(() => {
      previewTimers.delete(container);
      void previewSelection(container);
    }, 180);
    previewTimers.set(container, timer);
  }

  function buildCartPayload(button) {
    const productId = (button.dataset.productId || "").trim();
    const productName = (button.dataset.productName || "Product").trim() || "Product";

    if (!productId) {
      return { error: "This product cannot be added right now." };
    }

    const payload = {
      ProductId: productId,
      CurrencyCode: (button.dataset.currencyCode || "").trim() || null,
      Quantity: 1
    };

    const previewContainer = findPreviewContainer(button);
    if (previewContainer instanceof HTMLElement) {
      payload.Quantity = readSelectionQuantity(previewContainer);
      const selectedAttributes = collectSelectedAttributes(previewContainer);
      if (selectedAttributes.length > 0) {
        payload.SelectedAttributes = selectedAttributes;
      }

      const resolvedVariantId = resolveSelectedVariantId(button, previewContainer);
      if (resolvedVariantId) {
        payload.ProductVariantId = resolvedVariantId;
      }
    }

    const variantSelectSelector = button.dataset.variantSelect;
    const productStock = parseInteger(button.dataset.stock, 0);
    if (!variantSelectSelector && productStock <= 0) {
      return { error: "This product is out of stock." };
    }

    if (variantSelectSelector) {
      const select = document.querySelector(variantSelectSelector);
      if (!(select instanceof HTMLSelectElement)) {
        return { error: "This product variant selector is unavailable right now." };
      }

      const selectedOption = select.selectedOptions[0];
      if (!selectedOption || !selectedOption.value) {
        return { error: "Select a variant before adding to cart." };
      }

      if (parseInteger(selectedOption.dataset.stock, 0) <= 0) {
        return { error: "This variant is out of stock." };
      }

      payload.ProductVariantId = selectedOption.value.trim();
      payload.SizeValue = selectedOption.dataset.displayName || selectedOption.dataset.sizeValue || selectedOption.textContent.trim();
      payload.CurrencyCode = (selectedOption.dataset.currencyCode || payload.CurrencyCode || "").trim() || null;
    }

    return { payload, productName };
  }

  async function addToCart(button) {
    const result = buildCartPayload(button);
    if (result.error) {
      setFeedback(button, result.error, true);
      showToast("error", "Cart", result.error);
      return;
    }

    const { payload, productName } = result;
    const feedbackMessage = `Product ${formatCartLabel(productName, payload.SizeValue)} added to cart`;

    button.disabled = true;
    try {
      const summary = await sendCartRequest(`${cartApiRoute}/lines`, "POST", {
        ProductId: payload.ProductId,
        ProductVariantId: payload.ProductVariantId || null,
        SelectedAttributes: payload.SelectedAttributes || null,
        CurrencyCode: payload.CurrencyCode || null,
        Quantity: payload.Quantity
      });
      applyCartSummary(summary);
      setFeedback(button, feedbackMessage, false);
      showToast("success", "Cart", feedbackMessage);
      flashButton(button);
    } catch (error) {
      const message = error instanceof Error ? error.message : "Cart could not be updated.";
      setFeedback(button, message, true);
      showToast("error", "Cart", message);
    } finally {
      button.disabled = false;
    }
  }

  async function removeCartLine(button) {
    const lineId = (button.dataset.lineId || "").trim();
    if (!lineId) {
      showToast("error", "Cart", "This cart item could not be removed.");
      return;
    }

    const productName = formatCartLabel(button.dataset.productName, button.dataset.sizeValue);
    try {
      await sendCartRequest(`${cartApiRoute}/lines/${encodeURIComponent(lineId)}`, "DELETE");
      queueToastForNextLoad("warning", "Cart", `Removed ${productName} from cart.`);
      window.location.reload();
    } catch (error) {
      showToast("error", "Cart", error instanceof Error ? error.message : "This cart item could not be removed.");
    }
  }

  async function clearCart() {
    try {
      await sendCartRequest(cartApiRoute, "DELETE");
      queueToastForNextLoad("info", "Cart", "Cart cleared.");
      window.location.reload();
    } catch (error) {
      showToast("error", "Cart", error instanceof Error ? error.message : "Cart could not be cleared.");
    }
  }

  async function updateCartQuantity(input) {
    const lineId = (input.dataset.lineId || "").trim();
    const productName = formatCartLabel(input.dataset.productName, input.dataset.sizeValue);
    const nextQuantity = parseInteger(input.value, Number.NaN);
    const currentQuantity = parseInteger(input.getAttribute("value"), 1);

    if (!lineId) {
      showToast("error", "Cart", "This cart item could not be updated.");
      return;
    }

    if (!Number.isFinite(nextQuantity) || nextQuantity < 0) {
      input.value = String(currentQuantity);
      showToast("error", "Cart", "Enter a valid quantity.");
      return;
    }

    if (nextQuantity === currentQuantity) {
      return;
    }

    if (nextQuantity === 0) {
      try {
        await sendCartRequest(`${cartApiRoute}/lines/${encodeURIComponent(lineId)}`, "DELETE");
        queueToastForNextLoad("warning", "Cart", `Removed ${productName} from cart.`);
        window.location.reload();
      } catch (error) {
        input.value = String(currentQuantity);
        showToast("error", "Cart", error instanceof Error ? error.message : "This cart item could not be removed.");
      }
      return;
    }

    try {
      await sendCartRequest(`${cartApiRoute}/lines/${encodeURIComponent(lineId)}`, "PUT", { Quantity: nextQuantity });
      queueToastForNextLoad("info", "Cart", `Updated quantity of ${productName}.`);
      window.location.reload();
    } catch (error) {
      input.value = String(currentQuantity);
      showToast("error", "Cart", error instanceof Error ? error.message : "This cart item could not be updated.");
    }
  }

  function handleClick(event) {
    const clearButton = event.target.closest(cartClearSelector);
    if (clearButton instanceof HTMLButtonElement) {
      event.preventDefault();
      clearCart();
      return;
    }

    const removeButton = event.target.closest(cartRemoveSelector);
    if (removeButton instanceof HTMLButtonElement) {
      event.preventDefault();
      removeCartLine(removeButton);
      return;
    }

    const button = event.target.closest(buttonSelector);
    if (!(button instanceof HTMLButtonElement)) {
      return;
    }

    event.preventDefault();
    addToCart(button);
  }

  function handleChange(event) {
    const target = event.target;
    if (target instanceof HTMLElement && target.matches(attributeControlSelector)) {
      scheduleSelectionPreview(target.closest(selectionPreviewSelector));
      return;
    }

    if (target instanceof HTMLInputElement && target.matches(selectionQuantitySelector)) {
      scheduleSelectionPreview(target.closest(selectionPreviewSelector));
      return;
    }

    if (target instanceof HTMLSelectElement && target.matches("[data-storefront-variant-select]")) {
      const container = document.querySelector(selectionPreviewSelector);
      scheduleSelectionPreview(container);
      return;
    }

    if (target instanceof HTMLSelectElement && target.matches(addressSelectSelector)) {
      syncManualAddressFields(target);
      return;
    }

    if (!(target instanceof HTMLInputElement) || !target.matches(cartQuantitySelector)) {
      return;
    }

    updateCartQuantity(target);
  }

  function handleInput(event) {
    const target = event.target;
    if (target instanceof HTMLInputElement && target.matches(selectionQuantitySelector)) {
      scheduleSelectionPreview(target.closest(selectionPreviewSelector));
    }
  }

  function startBadgePolling() {
    if (badgePollHandle) {
      return;
    }

    badgePollHandle = window.setInterval(refreshCartSummary, badgePollIntervalMs);
  }

  function initialize() {
    flushQueuedToast();
    initConsentBanner();
    initCheckoutAddressSelection();
    refreshCartSummary();
    startBadgePolling();
    document.querySelectorAll(selectionPreviewSelector).forEach((container) => {
      if (container instanceof HTMLElement) {
        scheduleSelectionPreview(container);
      }
    });
    document.addEventListener("click", handleClick);
    document.addEventListener("change", handleChange);
    document.addEventListener("input", handleInput);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initialize, { once: true });
  } else {
    initialize();
  }
})();
