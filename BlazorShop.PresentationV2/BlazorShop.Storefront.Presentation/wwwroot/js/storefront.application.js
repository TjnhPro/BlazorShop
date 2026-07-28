(function () {
  const root = window.blazorShopStorefront = window.blazorShopStorefront || {};
  const cartApiRoute = "/api/cart";
  const consentApiRoute = "/api/consent";
  const productSelectionPreviewRoute = "/api/product-selection-preview";
  const consentBannerSelector = "[data-storefront-consent-banner]";
  const consentManageSelector = "[data-storefront-consent-manage]";
  const antiforgeryTokenSelector = 'meta[name="blazorshop-antiforgery-token"]';
  const antiforgeryHeaderSelector = 'meta[name="blazorshop-antiforgery-header"]';
  const legacyCartChangedEventName = "blazorshop:cart-changed";
  // F1.54 compatibility aliases remain until generated markup migrates in F1.55.
  const productPurchaseRootSelector = "[data-storefront-product-purchase], [data-storefront-selection-preview]";
  const productPurchaseSubmitSelector = "[data-storefront-product-purchase-submit], [data-storefront-add-to-cart]";
  const productPurchaseQuantitySelector = "[data-storefront-purchase-quantity], [data-storefront-selection-quantity], [data-storefront-generated-quantity]";
  const productPurchaseAttributeSelector = "[data-storefront-purchase-attribute], [data-storefront-attribute-control]";
  const productPurchaseVariantSelector = "[data-storefront-purchase-variant], [data-storefront-variant-select]";
  const cartBadgeSelector = "[data-storefront-cart-badge]";
  const purchasePreviewTimers = new WeakMap();
  const purchaseState = new WeakMap();
  const events = {
    cartChanged: "storefront:cart:changed",
    cartError: "storefront:cart:error",
    consentChanged: "storefront:consent:changed",
    consentManageRequested: "storefront:consent:manage-requested",
    productSelectionChanged: "storefront:product-selection:changed",
    productSelectionError: "storefront:product-selection:error",
    productPurchaseSelectionChanged: "storefront:product-purchase:selection-changed",
    productPurchaseSelectionError: "storefront:product-purchase:selection-error",
    productPurchaseAddLineSucceeded: "storefront:product-purchase:add-line-succeeded",
    productPurchaseAddLineFailed: "storefront:product-purchase:add-line-failed"
  };

  function parseInteger(value, fallback = 0) {
    const parsed = Number.parseInt(value, 10);
    return Number.isFinite(parsed) ? parsed : fallback;
  }

  function normalizeMethod(method) {
    return (method || "GET").toUpperCase();
  }

  function assertLocalRoute(route) {
    const value = String(route || "").trim();
    if (!value) {
      throw new Error("A local application route is required.");
    }

    if (/^[a-z][a-z0-9+.-]*:/i.test(value) || value.startsWith("//")) {
      throw new Error("Only same-origin storefront application routes are allowed.");
    }

    return value;
  }

  function readAntiforgeryHeader() {
    const token = document.querySelector(antiforgeryTokenSelector)?.getAttribute("content");
    const headerName = document.querySelector(antiforgeryHeaderSelector)?.getAttribute("content") || "X-CSRF-TOKEN";
    return token ? { headerName, token } : null;
  }

  function dispatch(name, detail) {
    document.dispatchEvent(new CustomEvent(name, { detail: detail || {} }));
  }

  function cartCount(summary) {
    return parseInteger(summary?.count ?? summary?.Count, 0);
  }

  function publishCartSummary(summary) {
    const count = cartCount(summary);
    const detail = { count, summary: summary || null };
    dispatch(events.cartChanged, detail);
    dispatch(legacyCartChangedEventName, detail);
    return summary;
  }

  function publishCartError(error) {
    dispatch(events.cartError, {
      message: error instanceof Error ? error.message : "Cart could not be updated."
    });
  }

  function publishConsentChanged(state) {
    dispatch(events.consentChanged, { state: state || null });
    return state;
  }

  function publishConsentManageRequested() {
    dispatch(events.consentManageRequested, {});
  }

  function publishProductSelectionChanged(preview) {
    dispatch(events.productSelectionChanged, { preview: preview || null });
    return preview;
  }

  function publishProductSelectionError(error) {
    dispatch(events.productSelectionError, {
      message: error instanceof Error ? error.message : "This selection could not be previewed."
    });
  }

  function publishPurchaseSelectionChanged(rootElement, submitter, preview, selection) {
    dispatch(events.productPurchaseSelectionChanged, {
      root: rootElement,
      submitter,
      preview: preview || null,
      selection
    });
  }

  function publishPurchaseSelectionError(rootElement, submitter, error) {
    dispatch(events.productPurchaseSelectionError, {
      root: rootElement,
      submitter,
      message: error instanceof Error ? error.message : "This selection could not be previewed."
    });
  }

  function publishPurchaseAddLineSucceeded(rootElement, submitter, summary, selection) {
    dispatch(events.productPurchaseAddLineSucceeded, {
      root: rootElement,
      submitter,
      summary: summary || null,
      selection,
      message: buildAddedMessage(rootElement, selection)
    });
  }

  function publishPurchaseAddLineFailed(rootElement, submitter, error, selection) {
    dispatch(events.productPurchaseAddLineFailed, {
      root: rootElement,
      submitter,
      selection: selection || null,
      message: error instanceof Error ? error.message : "Cart could not be updated."
    });
  }

  async function requestJson(route, method, body, defaultErrorMessage) {
    const normalizedMethod = normalizeMethod(method);
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

    const response = await fetch(assertLocalRoute(route), options);
    const text = await response.text();
    let payload = null;
    if (text) {
      try {
        payload = JSON.parse(text);
      } catch {
        payload = { message: text };
      }
    }

    if (!response.ok) {
      throw new Error(payload?.message || payload?.Message || defaultErrorMessage || "The request could not be completed.");
    }

    return payload;
  }

  const cart = {
    async current() {
      try {
        return publishCartSummary(await requestJson(cartApiRoute, "GET", undefined, "Cart could not be loaded."));
      } catch (error) {
        publishCartError(error);
        throw error;
      }
    },
    async addLine(payload) {
      try {
        return publishCartSummary(await requestJson(`${cartApiRoute}/lines`, "POST", payload, "Cart could not be updated."));
      } catch (error) {
        publishCartError(error);
        throw error;
      }
    },
    async updateLine(lineId, payload) {
      try {
        return publishCartSummary(await requestJson(`${cartApiRoute}/lines/${encodeURIComponent(lineId)}`, "PUT", payload, "Cart could not be updated."));
      } catch (error) {
        publishCartError(error);
        throw error;
      }
    },
    async removeLine(lineId) {
      try {
        return publishCartSummary(await requestJson(`${cartApiRoute}/lines/${encodeURIComponent(lineId)}`, "DELETE", undefined, "Cart could not be updated."));
      } catch (error) {
        publishCartError(error);
        throw error;
      }
    },
    async clear() {
      try {
        return publishCartSummary(await requestJson(cartApiRoute, "DELETE", undefined, "Cart could not be updated."));
      } catch (error) {
        publishCartError(error);
        throw error;
      }
    },
    async recalculate() {
      try {
        return publishCartSummary(await requestJson(`${cartApiRoute}/recalculate`, "POST", undefined, "Cart could not be updated."));
      } catch (error) {
        publishCartError(error);
        throw error;
      }
    }
  };

  function readConsentActions(banner) {
    return {
      currentUrl: banner?.dataset.storefrontConsentCurrentUrl || `${consentApiRoute}/current`,
      acceptUrl: banner?.dataset.storefrontConsentAcceptUrl || consentApiRoute,
      revokeUrl: banner?.dataset.storefrontConsentRevokeUrl || `${consentApiRoute}/revoke`,
      currentMethod: banner?.dataset.storefrontConsentCurrentMethod || "GET",
      acceptMethod: banner?.dataset.storefrontConsentAcceptMethod || "POST",
      revokeMethod: banner?.dataset.storefrontConsentRevokeMethod || "POST"
    };
  }

  const consent = {
    async current(actions) {
      const resolved = actions || readConsentActions(null);
      return publishConsentChanged(await requestJson(resolved.currentUrl, resolved.currentMethod, undefined, "Consent could not be loaded."));
    },
    async accept(selection, actions) {
      const resolved = actions || readConsentActions(null);
      return publishConsentChanged(await requestJson(resolved.acceptUrl, resolved.acceptMethod, selection, "Consent could not be updated."));
    },
    async save(selection, actions) {
      return consent.accept(selection, actions);
    },
    async revoke(actions) {
      const resolved = actions || readConsentActions(null);
      return publishConsentChanged(await requestJson(resolved.revokeUrl, resolved.revokeMethod, undefined, "Consent could not be updated."));
    }
  };

  const productSelection = {
    async preview(route, payload) {
      try {
        return publishProductSelectionChanged(await requestJson(route || productSelectionPreviewRoute, "POST", payload, "This selection could not be previewed."));
      } catch (error) {
        publishProductSelectionError(error);
        throw error;
      }
    }
  };

  function bindConsent() {
    const banner = document.querySelector(consentBannerSelector);
    if (!(banner instanceof HTMLElement)) {
      return;
    }

    if (banner.dataset.storefrontConsentEnabled === "false") {
      banner.classList.add("hidden");
      return;
    }

    const actions = readConsentActions(banner);
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
      const state = await consent.accept(selection, actions);
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
      void consent.revoke(actions)
        .then(applyState)
        .catch(() => banner.classList.add("hidden"));
    });
    document.querySelectorAll(consentManageSelector).forEach((button) => {
      button.addEventListener("click", () => {
        publishConsentManageRequested();
        banner.classList.remove("hidden");
      });
    });

    void consent.current(actions)
      .then(applyState)
      .catch(() => banner.classList.add("hidden"));
  }

  function updateCartBadges(count) {
    document.querySelectorAll(cartBadgeSelector).forEach((badge) => {
      if (!(badge instanceof HTMLElement)) {
        return;
      }

      badge.textContent = count > 99 ? "99+" : String(count);
      badge.hidden = count <= 0;
      badge.classList.toggle("hidden", count <= 0);
    });
  }

  function bindCartBadge() {
    document.addEventListener(events.cartChanged, (event) => {
      updateCartBadges(parseInteger(event.detail?.count, 0));
    });

    void cart.current()
      .catch(() => updateCartBadges(0));
  }

  function findPurchaseRoot(source) {
    if (!(source instanceof Element)) {
      return null;
    }

    const rootElement = source.matches(productPurchaseRootSelector)
      ? source
      : source.closest(productPurchaseRootSelector);
    return rootElement instanceof HTMLElement ? rootElement : null;
  }

  function findSubmit(rootElement) {
    if (rootElement.matches(productPurchaseSubmitSelector)) {
      return rootElement;
    }

    const submit = rootElement.querySelector(productPurchaseSubmitSelector);
    return submit instanceof HTMLElement ? submit : null;
  }

  function readQuantity(rootElement) {
    const input = rootElement.querySelector(productPurchaseQuantitySelector);
    if (!(input instanceof HTMLInputElement)) {
      return 1;
    }

    return Math.max(1, parseInteger(input.value, 1));
  }

  function readSelectedAttributes(rootElement) {
    const attributes = [];
    rootElement.querySelectorAll(productPurchaseAttributeSelector).forEach((control) => {
      if (!(control instanceof HTMLElement)) {
        return;
      }

      const name = (control.dataset.storefrontPurchaseAttributeName || control.dataset.attributeName || "").trim();
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

  function findVariantSelect(rootElement, submitter) {
    const explicitSelector = submitter?.dataset.storefrontPurchaseVariantSelector || submitter?.dataset.variantSelect;
    if (explicitSelector) {
      const explicit = document.querySelector(explicitSelector);
      if (explicit instanceof HTMLSelectElement) {
        return explicit;
      }
    }

    const local = rootElement.querySelector(productPurchaseVariantSelector);
    return local instanceof HTMLSelectElement ? local : null;
  }

  function readVariantId(rootElement, submitter, includeResolvedVariant) {
    const select = findVariantSelect(rootElement, submitter);
    if (select instanceof HTMLSelectElement && select.value) {
      return select.value.trim();
    }

    return includeResolvedVariant
      ? (rootElement.dataset.resolvedVariantId || submitter?.dataset.resolvedVariantId || "").trim()
      : "";
  }

  function readPurchaseDescriptor(rootElement, submitter) {
    const productId = (rootElement.dataset.productId || submitter?.dataset.productId || "").trim();
    return {
      productId,
      productName: (rootElement.dataset.productName || submitter?.dataset.productName || "Product").trim() || "Product",
      currencyCode: (rootElement.dataset.currencyCode || submitter?.dataset.currencyCode || "").trim(),
      previewRoute: (rootElement.dataset.selectionPreviewRoute || rootElement.dataset.previewRoute || submitter?.dataset.selectionPreviewRoute || submitter?.dataset.previewRoute || "").trim(),
      quantity: readQuantity(rootElement),
      selectedAttributes: readSelectedAttributes(rootElement),
      selectedVariantId: readVariantId(rootElement, submitter, false),
      resolvedVariantId: readVariantId(rootElement, submitter, true)
    };
  }

  function buildSelectionPreviewPayload(descriptor) {
    if (!descriptor.productId) {
      return { error: "This product cannot be previewed right now." };
    }

    return {
      ProductId: descriptor.productId,
      ProductVariantId: descriptor.selectedVariantId || null,
      SelectedAttributes: descriptor.selectedAttributes.length > 0 ? descriptor.selectedAttributes : null,
      Quantity: descriptor.quantity,
      CurrencyCode: descriptor.currencyCode || null
    };
  }

  function readPreviewValue(preview, camelName, pascalName) {
    return preview?.[camelName] ?? preview?.[pascalName];
  }

  function normalizePreview(rootElement, submitter, descriptor, preview) {
    const validationMessages = Array.isArray(readPreviewValue(preview, "validationMessages", "ValidationMessages"))
      ? readPreviewValue(preview, "validationMessages", "ValidationMessages").filter(Boolean)
      : [];
    const isValid = Boolean(readPreviewValue(preview, "isValid", "IsValid"));
    const isAvailable = Boolean(readPreviewValue(preview, "isAvailable", "IsAvailable"));
    const isReady = Boolean(readPreviewValue(preview, "canAddToCart", "CanAddToCart"));
    const stockAmount = readPreviewValue(preview, "stockQuantity", "StockQuantity");
    const variantId = readPreviewValue(preview, "productVariantId", "ProductVariantId") || descriptor.resolvedVariantId || "";
    const currencyCode = readPreviewValue(preview, "currencyCode", "CurrencyCode") || descriptor.currencyCode || "";
    const message = validationMessages[0] || (isReady ? "Selection ready." : "This selection is not available.");
    const selection = {
      ready: isReady,
      valid: isValid,
      available: isAvailable,
      productId: descriptor.productId,
      productName: descriptor.productName,
      productVariantId: variantId,
      selectedAttributes: descriptor.selectedAttributes,
      quantity: descriptor.quantity,
      currencyCode,
      unitPrice: readPreviewValue(preview, "unitPrice", "UnitPrice"),
      priceText: readPreviewValue(preview, "formattedUnitPrice", "FormattedUnitPrice") || "",
      comparePriceText: readPreviewValue(preview, "formattedComparePrice", "FormattedComparePrice") || "",
      stockText: isValid ? (isAvailable ? `${stockAmount ?? 0} in stock` : "Out of stock") : "",
      skuText: readPreviewValue(preview, "sku", "Sku") ? `SKU ${readPreviewValue(preview, "sku", "Sku")}` : "",
      gtinText: readPreviewValue(preview, "gtin", "Gtin") ? `GTIN ${readPreviewValue(preview, "gtin", "Gtin")}` : "",
      mainImageUrl: readPreviewValue(preview, "primaryImageUrl", "PrimaryImageUrl") || rootElement.dataset.mainImageUrl || "",
      message
    };

    rootElement.dataset.resolvedVariantId = variantId || "";
    rootElement.dataset.storefrontPurchaseReady = isReady ? "true" : "false";
    purchaseState.set(rootElement, selection);

    if (submitter instanceof HTMLButtonElement) {
      submitter.disabled = !isReady;
      submitter.dataset.resolvedVariantId = variantId || "";
      submitter.dataset.currencyCode = currencyCode || submitter.dataset.currencyCode || "";
    }

    return selection;
  }

  async function previewPurchase(rootElement, submitter) {
    const descriptor = readPurchaseDescriptor(rootElement, submitter);
    if (!descriptor.previewRoute) {
      const selection = {
        ready: true,
        valid: true,
        available: true,
        productId: descriptor.productId,
        productName: descriptor.productName,
        productVariantId: descriptor.resolvedVariantId || "",
        selectedAttributes: descriptor.selectedAttributes,
        quantity: descriptor.quantity,
        currencyCode: descriptor.currencyCode,
        message: "Selection ready."
      };
      purchaseState.set(rootElement, selection);
      return selection;
    }

    const payload = buildSelectionPreviewPayload(descriptor);
    if (payload.error) {
      throw new Error(payload.error);
    }

    const preview = await productSelection.preview(descriptor.previewRoute, payload);
    const selection = normalizePreview(rootElement, submitter, descriptor, preview);
    publishPurchaseSelectionChanged(rootElement, submitter, preview, selection);
    return selection;
  }

  function schedulePurchasePreview(rootElement) {
    const existing = purchasePreviewTimers.get(rootElement);
    if (existing) {
      window.clearTimeout(existing);
    }

    const timer = window.setTimeout(() => {
      purchasePreviewTimers.delete(rootElement);
      const submitter = findSubmit(rootElement);
      void previewPurchase(rootElement, submitter)
        .catch((error) => publishPurchaseSelectionError(rootElement, submitter, error));
    }, 180);
    purchasePreviewTimers.set(rootElement, timer);
  }

  function buildAddLinePayload(selection) {
    if (!selection?.productId) {
      return { error: "This product cannot be added right now." };
    }

    if (selection.ready === false) {
      return { error: selection.message || "This product is not available for purchase." };
    }

    return {
      ProductId: selection.productId,
      ProductVariantId: selection.productVariantId || null,
      SelectedAttributes: selection.selectedAttributes?.length > 0 ? selection.selectedAttributes : null,
      CurrencyCode: selection.currencyCode || null,
      Quantity: Math.max(1, parseInteger(selection.quantity, 1))
    };
  }

  function buildAddedMessage(rootElement, selection) {
    const productName = (selection?.productName || rootElement.dataset.productName || "product").trim() || "product";
    const size = (selection?.sizeText || "").trim();
    return size ? `Product ${productName} (size ${size}) added to cart` : `Product ${productName} added to cart`;
  }

  async function addPurchaseLine(rootElement, submitter) {
    let selection = purchaseState.get(rootElement);
    if (!selection || rootElement.dataset.selectionPreviewRoute || rootElement.dataset.previewRoute) {
      selection = await previewPurchase(rootElement, submitter);
    }

    const payload = buildAddLinePayload(selection);
    if (payload.error) {
      throw new Error(payload.error);
    }

    return cart.addLine(payload);
  }

  function bindProductPurchase() {
    document.querySelectorAll(productPurchaseRootSelector).forEach((rootCandidate) => {
      if (rootCandidate instanceof HTMLElement) {
        schedulePurchasePreview(rootCandidate);
      }
    });

    document.addEventListener("change", (event) => {
      const rootElement = findPurchaseRoot(event.target);
      if (rootElement) {
        schedulePurchasePreview(rootElement);
      }
    });

    document.addEventListener("input", (event) => {
      const target = event.target;
      if (target instanceof HTMLInputElement && target.matches(productPurchaseQuantitySelector)) {
        const rootElement = findPurchaseRoot(target);
        if (rootElement) {
          schedulePurchasePreview(rootElement);
        }
      }
    });

    document.addEventListener("click", (event) => {
      const target = event.target;
      if (!(target instanceof Element)) {
        return;
      }

      const submitter = target.closest(productPurchaseSubmitSelector);
      if (!(submitter instanceof HTMLElement)) {
        return;
      }

      const rootElement = findPurchaseRoot(submitter);
      if (!rootElement) {
        return;
      }

      event.preventDefault();
      if (submitter instanceof HTMLButtonElement) {
        submitter.disabled = true;
      }

      void addPurchaseLine(rootElement, submitter)
        .then((summary) => {
          const selection = purchaseState.get(rootElement) || readPurchaseDescriptor(rootElement, submitter);
          publishPurchaseAddLineSucceeded(rootElement, submitter, summary, selection);
        })
        .catch((error) => {
          publishPurchaseAddLineFailed(rootElement, submitter, error, purchaseState.get(rootElement));
        })
        .finally(() => {
          const selection = purchaseState.get(rootElement);
          if (submitter instanceof HTMLButtonElement) {
            submitter.disabled = selection?.ready === false;
          }
        });
    });
  }

  function initializeBindings() {
    bindConsent();
    bindCartBadge();
    bindProductPurchase();
  }

  root.application = {
    events,
    requestJson,
    cart,
    consent,
    productSelection
  };

  root.bindings = {
    consent: { bindAll: bindConsent },
    cartBadge: { bindAll: bindCartBadge },
    productPurchase: { bindAll: bindProductPurchase },
    productSelection: { previewPurchase },
    addToCart: { addPurchaseLine }
  };

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initializeBindings, { once: true });
  } else {
    initializeBindings();
  }
})();
