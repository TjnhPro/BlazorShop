(function () {
  const root = window.blazorShopStorefront = window.blazorShopStorefront || {};
  const cartApiRoute = "/api/cart";
  const consentApiRoute = "/api/consent";
  const productSelectionPreviewRoute = "/api/product-selection-preview";
  const antiforgeryTokenSelector = 'meta[name="blazorshop-antiforgery-token"]';
  const antiforgeryHeaderSelector = 'meta[name="blazorshop-antiforgery-header"]';
  const legacyCartChangedEventName = "blazorshop:cart-changed";
  const events = {
    cartChanged: "storefront:cart:changed",
    cartError: "storefront:cart:error",
    consentChanged: "storefront:consent:changed",
    productSelectionChanged: "storefront:product-selection:changed",
    productSelectionError: "storefront:product-selection:error"
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

  function publishProductSelectionChanged(preview) {
    dispatch(events.productSelectionChanged, { preview: preview || null });
    return preview;
  }

  function publishProductSelectionError(error) {
    dispatch(events.productSelectionError, {
      message: error instanceof Error ? error.message : "This selection could not be previewed."
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

  const consent = {
    async current() {
      return publishConsentChanged(await requestJson(`${consentApiRoute}/current`, "GET", undefined, "Consent could not be loaded."));
    },
    async save(selection) {
      return publishConsentChanged(await requestJson(consentApiRoute, "POST", selection, "Consent could not be updated."));
    },
    async revoke() {
      return publishConsentChanged(await requestJson(`${consentApiRoute}/revoke`, "POST", undefined, "Consent could not be updated."));
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

  root.application = {
    events,
    requestJson,
    cart,
    consent,
    productSelection
  };
})();
