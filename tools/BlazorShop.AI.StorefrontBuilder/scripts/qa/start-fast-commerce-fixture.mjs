#!/usr/bin/env node
import { writeFileSync } from "node:fs";
import { createServer } from "node:http";

const storeKey = readArg("--store-key") ?? "default";
const port = Number.parseInt(readArg("--port") ?? "0", 10);
const readyFile = readArg("--ready-file");
const host = "127.0.0.1";
const proofStoreId = "00000000-0000-0000-0000-000000000010";
const proofSeoSettingsId = "00000000-0000-0000-0000-000000000011";
const proofPaymentMethodId = "00000000-0000-0000-0000-000000000012";
const proofCheckoutId = "00000000-0000-0000-0000-000000000013";
const proofCategory = {
  id: "00000000-0000-0000-0000-000000000101",
  parentCategoryId: null,
  name: "Apparel",
  slug: "apparel",
  description: "Generated proof category.",
  image: null,
  displayOrder: 1,
  isPublished: true,
  metaTitle: "Apparel",
  metaDescription: "Generated proof category.",
  canonicalUrl: null,
  ogTitle: "Apparel",
  ogDescription: "Generated proof category.",
  ogImage: null,
  seoContent: "",
  robotsIndex: true,
  robotsFollow: true,
};
const proofProduct = {
  id: "00000000-0000-0000-0000-000000000201",
  variantId: "00000000-0000-0000-0000-000000000202",
  slug: "qa-simple-product-100",
};
const proofCart = {
  id: "00000000-0000-0000-0000-000000000301",
  lineId: "00000000-0000-0000-0000-000000000302",
  token: "proof-cart-token",
};

if (process.argv.includes("--help") || process.argv.includes("-h")) {
  console.log(`Usage: node start-fast-commerce-fixture.mjs [options]

Options:
  --store-key <key>      Store key accepted by the fake Storefront API. Defaults to default.
  --port <port>          Port to bind. Defaults to an available random port.
  --ready-file <path>    Write JSON readiness metadata after binding.
  --help, -h             Show this help text.`);
  process.exit(0);
}

const server = createServer((request, response) => {
  const url = new URL(request.url ?? "/", "http://fake-commerce.local");
  const prefix = `/api/storefront/stores/${encodeURIComponent(storeKey)}`;

  if (!url.pathname.startsWith(`${prefix}/`)) {
    json(response, 404, { success: false, message: "Not found." });
    return;
  }

  const path = url.pathname.slice(prefix.length);
  routeStorefrontRequest(request, response, path, url);
});

server.listen(port, host, () => {
  const address = server.address();
  const url = `http://${host}:${address.port}`;
  if (readyFile) {
    writeFileSync(readyFile, JSON.stringify({ url, storeKey, startedUtc: nowIso() }, null, 2), "utf8");
  }
  console.log(`Fast Commerce fixture listening at ${url} for store '${storeKey}'.`);
});

process.on("SIGTERM", () => server.close(() => process.exit(0)));
process.on("SIGINT", () => server.close(() => process.exit(0)));

function routeStorefrontRequest(request, response, path, url) {
  if (request.method === "GET" && path === "/store/current") {
    json(response, 200, envelope(currentStore()));
    return;
  }

  if (request.method === "GET" && path === "/configuration") {
    json(response, 200, envelope(publicConfiguration()));
    return;
  }

  if (request.method === "GET" && path === "/catalog/categories") {
    json(response, 200, envelope([proofCategory]));
    return;
  }

  if (request.method === "GET" && path === "/catalog/categories/tree") {
    json(response, 200, envelope([{ ...proofCategory, children: [] }]));
    return;
  }

  if (request.method === "GET" && path.startsWith("/catalog/categories/slug/")) {
    json(response, 200, envelope({
      category: proofCategory,
      breadcrumbs: [{ id: proofCategory.id, name: proofCategory.name, slug: proofCategory.slug }],
      products: [catalogProduct()],
      directProductCount: 1,
      descendantProductCount: 0,
    }));
    return;
  }

  if (request.method === "GET" && path === "/catalog/products") {
    json(response, 200, envelope({
      items: [catalogProduct()],
      pageNumber: Number.parseInt(url.searchParams.get("pageNumber") || "1", 10),
      pageSize: Number.parseInt(url.searchParams.get("pageSize") || "24", 10),
      totalCount: 1,
    }));
    return;
  }

  if (request.method === "GET" && path.startsWith("/catalog/products/slug/")) {
    json(response, 200, envelope(productDetail()));
    return;
  }

  if (request.method === "GET" && path.startsWith("/catalog/products/")) {
    json(response, 200, envelope(productDetail()));
    return;
  }

  if (request.method === "POST" && path.startsWith("/catalog/products/") && path.endsWith("/selection-preview")) {
    json(response, 200, envelope(selectionPreview()));
    return;
  }

  if (request.method === "GET" && path === "/catalog/filters") {
    json(response, 200, envelope({ categories: [proofCategory], price: { min: 0, max: 50 }, attributes: [] }));
    return;
  }

  if (request.method === "GET" && path === "/catalog/search/suggestions") {
    json(response, 200, envelope({ suggestions: [] }));
    return;
  }

  if (request.method === "GET" && path === "/catalog/sitemap") {
    json(response, 200, envelope({ categories: [{ slug: proofCategory.slug }], products: [{ slug: proofProduct.slug }], pages: [] }));
    return;
  }

  if (request.method === "GET" && (path.startsWith("/navigation/menus/") || path.startsWith("/navigation/"))) {
    const systemName = decodeURIComponent(path.split("/").pop() || "main");
    json(response, 200, envelope({ systemName, generatedAt: nowIso(), items: [] }));
    return;
  }

  if (request.method === "GET" && path === "/pages/navigation") {
    json(response, 200, envelope([]));
    return;
  }

  if (request.method === "GET" && path.startsWith("/pages/")) {
    json(response, 200, envelope(pageContent(decodeURIComponent(path.split("/").pop() || "home"))));
    return;
  }

  if (request.method === "GET" && path === "/seo/settings") {
    json(response, 200, envelope(seoSettings()));
    return;
  }

  if (request.method === "GET" && path === "/seo/redirects/resolve") {
    json(response, 200, envelope({ newPath: null, statusCode: 0 }));
    return;
  }

  if (request.method === "POST" && path === "/cart/session") {
    json(response, 200, envelope({ cartToken: proofCart.token, expiresAtUtc: futureIso() }));
    return;
  }

  if (path === "/cart" || path.startsWith("/cart/")) {
    json(response, 200, envelope(cartResponse()));
    return;
  }

  if (request.method === "POST" && path === "/auth/refresh-token") {
    json(response, 200, envelope({
      accessToken: proofAccessToken(),
      refreshToken: "proof-refresh-token",
      expiresAtUtc: futureIso(),
    }));
    return;
  }

  if (request.method === "POST" && path === "/checkout/start") {
    json(response, 200, envelope(checkoutSession()));
    return;
  }

  if (request.method === "GET" && path === "/payments/methods") {
    json(response, 200, envelope([paymentMethod()]));
    return;
  }

  if (request.method === "GET" && path === "/address/countries") {
    json(response, 200, envelope([{ code: "US", name: "United States" }]));
    return;
  }

  if (request.method === "GET" && path === "/address/countries/US/states") {
    json(response, 200, envelope([{ code: "CA", name: "California" }]));
    return;
  }

  if (request.method === "GET" && path === "/address/configuration") {
    json(response, 200, envelope({ phoneEnabled: true, phoneRequired: false, postalCodeRequired: true }));
    return;
  }

  if (path.startsWith("/consent")) {
    json(response, 200, envelope(consentState()));
    return;
  }

  json(response, 404, { success: false, message: `Unhandled fake Commerce path ${request.method} ${path}` });
}

function json(response, statusCode, payload) {
  response.writeHead(statusCode, { "content-type": "application/json" });
  response.end(JSON.stringify(payload));
}

function envelope(data, message = "Request completed.") {
  return { success: true, message, data };
}

function currentStore() {
  return {
    publicId: proofStoreId,
    storeKey,
    name: "Generated Proof Store",
    status: "active",
    baseUrl: null,
    primaryDomain: null,
    forceHttps: false,
    cdnHost: null,
    logoUrl: null,
    companyName: "Generated Proof Store",
    companyEmail: "support@example.test",
    companyPhone: null,
    companyAddress: null,
    faviconUrl: null,
    pngIconUrl: null,
    appleTouchIconUrl: null,
    msTileImageUrl: null,
    msTileColor: null,
    defaultCurrencyCode: "USD",
    defaultCulture: "en-US",
    supportEmail: "support@example.test",
    supportPhone: null,
    maintenanceModeEnabled: false,
    maintenanceMessage: null,
    htmlBodyId: null,
  };
}

function publicConfiguration() {
  return {
    storeIdentity: currentStore(),
    branding: currentStore(),
    localeOptions: { defaultCulture: "en-US", supportedCultures: ["en-US"] },
    currencyOptions: { defaultCurrencyCode: "USD", supportedCurrencyCodes: ["USD"] },
    consent: {
      enabled: true,
      bannerRequired: false,
      currentVersion: "v1",
      policyPagePath: "/pages/privacy",
      categories: [
        { name: "essential", required: true, defaultEnabled: true },
        { name: "preferences", required: false, defaultEnabled: false },
        { name: "analytics", required: false, defaultEnabled: false },
        { name: "marketing", required: false, defaultEnabled: false },
      ],
      visitorCookieLifetimeDays: 180,
    },
    captcha: { enabled: false, providerSystemName: "", publicSiteKey: null, enabledTargets: [], actionNames: {} },
    maintenanceState: { maintenanceModeEnabled: false, maintenanceMessage: null },
    featureFlags: {
      customerAccountsEnabled: true,
      cartEnabled: true,
      checkoutEnabled: true,
      paymentsEnabled: true,
      newsletterEnabled: true,
      recommendationsEnabled: true,
    },
    features: {},
    paymentMethods: [paymentMethod()],
    seoDefaults: seoSettings(),
  };
}

function catalogProduct() {
  return {
    id: proofProduct.id,
    slug: proofProduct.slug,
    name: "QA Simple Product 100",
    description: "Generated proof product.",
    sku: "SKU-FAST",
    gtin: "GTIN-FAST",
    shortDescription: "Generated proof product.",
    price: 19,
    comparePrice: null,
    displayPrice: 19,
    displayComparePrice: null,
    displayCurrencyCode: "USD",
    image: "",
    createdOn: nowIso(),
    updatedAt: nowIso(),
    displayOrder: 1,
    inStock: true,
    quantity: 7,
    purchasable: true,
    purchaseBlockReasons: [],
    stockStatus: "In stock",
    availableQuantity: 7,
    minOrderQuantity: 1,
    maxOrderQuantity: 9,
    quantityStep: 1,
    manageStock: true,
    shippingRequired: true,
    freeShipping: false,
    shippingSurcharge: null,
    deliveryEstimateText: "Ships soon",
    categoryId: proofCategory.id,
    categoryName: proofCategory.name,
    categorySlug: proofCategory.slug,
    hasVariants: true,
  };
}

function productDetail() {
  return {
    ...catalogProduct(),
    fullDescription: "Generated proof product detail.",
    metaTitle: "QA Simple Product 100",
    metaDescription: "Generated proof product.",
    canonicalUrl: null,
    ogTitle: "QA Simple Product 100",
    ogDescription: "Generated proof product.",
    ogImage: null,
    seoContent: "",
    robotsIndex: true,
    robotsFollow: true,
    category: proofCategory,
    variationTemplate: {
      name: "Default options",
      slug: "default-options",
      options: [{ name: "Size", controlType: "button", isRequired: true, values: [{ value: "M", colorHex: null }] }],
    },
    mediaGallery: [],
    variants: [{
      id: proofProduct.variantId,
      productId: proofProduct.id,
      sku: "SKU-FAST-M",
      attributes: [{ name: "Size", value: "M" }],
      attributeSignature: "Size=M",
      displayName: "QA Simple Product 100 / M",
      sizeScale: 0,
      sizeValue: "M",
      price: 19,
      effectivePrice: 19,
      displayPrice: 19,
      displayCurrencyCode: "USD",
      stock: 7,
      purchasable: true,
      purchaseBlockReasons: [],
      stockStatus: "In stock",
      availableQuantity: 7,
      color: null,
      isActive: true,
      isDefault: true,
    }],
  };
}

function selectionPreview() {
  return {
    productId: proofProduct.id,
    productVariantId: proofProduct.variantId,
    isValid: true,
    isAvailable: true,
    canAddToCart: true,
    validationMessages: [],
    selectedAttributes: [{ name: "Size", value: "M" }],
    attributeSignature: "Size=M",
    sku: "SKU-FAST-M",
    gtin: "GTIN-FAST-M",
    displayName: "QA Simple Product 100 / M",
    unitPrice: 19,
    comparePrice: null,
    currencyCode: "USD",
    stockQuantity: 7,
    minQuantity: 1,
    maxQuantity: 9,
    primaryImageUrl: "",
  };
}

function cartResponse() {
  return {
    cartId: proofCart.id,
    cartToken: proofCart.token,
    expiresAtUtc: futureIso(),
    version: 1,
    currencyCode: "USD",
    subtotal: 19,
    grandTotal: 19,
    summaryCount: 1,
    lines: [{
      lineId: proofCart.lineId,
      productId: proofProduct.id,
      productVariantId: proofProduct.variantId,
      productSlug: proofProduct.slug,
      productUrl: `/product/${proofProduct.slug}`,
      displayName: "QA Simple Product 100",
      imageUrl: "",
      quantity: 1,
      unitPrice: 19,
      unitPriceSnapshot: 19,
      lineTotal: 19,
      lineSubtotal: 19,
      currencyCodeSnapshot: "USD",
      selectedAttributes: [{ name: "Size", value: "M" }],
      quantityMinimum: 1,
      quantityMaximum: 9,
      quantityStep: 1,
      purchasable: true,
      warnings: [],
    }],
    checkoutAllowed: true,
    warnings: [],
    adjustments: [],
  };
}

function checkoutSession() {
  return {
    checkoutSessionId: proofCheckoutId,
    cartId: proofCart.id,
    checkoutVersion: 1,
    cartVersion: 1,
    lastValidatedCartVersion: 1,
    currencyCode: "USD",
    subtotal: 19,
    shippingTotal: 0,
    taxTotal: 0,
    discountTotal: 0,
    grandTotal: 19,
    requiresShipping: true,
    shippingAddress: null,
    billingAddress: null,
    selectedShippingOption: null,
    shippingOptions: [],
    selectedPaymentMethod: paymentMethodOption(),
    paymentMethods: [paymentMethodOption()],
    lines: [],
    issues: [],
  };
}

function paymentMethod() {
  return {
    id: proofPaymentMethodId,
    key: "cod",
    name: "Cash on delivery",
    description: "Pay when the order arrives.",
    shortDisplayText: "COD",
    iconUrl: null,
    supportedCurrencyCodes: ["USD"],
    supportedCountryCodes: ["US"],
  };
}

function paymentMethodOption() {
  return { key: "cod", displayName: "Cash on delivery", description: "Pay when the order arrives." };
}

function pageContent(slug) {
  return {
    slug,
    title: slug === "home" ? "Generated Proof Store" : "Proof page",
    intro: "Generated proof content.",
    bodyHtml: "<p>Generated proof content.</p>",
    seo: { metaTitle: "Proof page", metaDescription: "Generated proof content.", robotsIndex: true, robotsFollow: true },
    updatedAt: nowIso(),
    pageKey: slug,
  };
}

function seoSettings() {
  return {
    id: proofSeoSettingsId,
    siteName: "Generated Proof Store",
    defaultTitleSuffix: "| Generated Proof Store",
    defaultMetaDescription: "Generated proof storefront.",
    defaultOgImage: null,
    baseCanonicalUrl: null,
    companyName: "Generated Proof Store",
    companyLogoUrl: null,
    companyPhone: null,
    companyEmail: "support@example.test",
    companyAddress: null,
    facebookUrl: null,
    instagramUrl: null,
    xUrl: null,
  };
}

function consentState() {
  return {
    enabled: true,
    bannerRequired: false,
    consentVersion: "v1",
    consentKey: "visitor-key",
    categories: { essential: true, preferences: false, analytics: false, marketing: false },
  };
}

function nowIso() {
  return new Date().toISOString();
}

function futureIso() {
  return new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString();
}

function proofAccessToken() {
  const header = base64UrlJson({ alg: "none", typ: "JWT" });
  const payload = base64UrlJson({
    email: "proof@example.test",
    unique_name: "Proof Customer",
    exp: Math.floor(Date.now() / 1000) + 3600,
  });
  return `${header}.${payload}.`;
}

function base64UrlJson(value) {
  return Buffer.from(JSON.stringify(value)).toString("base64url");
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] : null;
}
