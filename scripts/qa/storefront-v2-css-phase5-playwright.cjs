async (page) => {
  const baseUrl = "http://localhost:18598";
  const assetUrls = [
    `${baseUrl}/css/site.css`,
    `${baseUrl}/css/wasm-site.css`,
    `${baseUrl}/css/storefront.css`,
    `${baseUrl}/js/storefrontCommerce.js`,
  ];

  const failedRequests = [];
  const consoleMessages = [];
  const responseStatuses = [];

  page.on("requestfailed", (request) => {
    failedRequests.push({
      url: request.url(),
      failure: request.failure()?.errorText ?? "",
    });
  });

  page.on("console", (message) => {
    consoleMessages.push({
      type: message.type(),
      text: message.text(),
    });
  });

  page.on("response", (response) => {
    const url = response.url();
    if (url.startsWith(baseUrl)) {
      responseStatuses.push({
        url,
        status: response.status(),
      });
    }
  });

  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto(`${baseUrl}/css/site.css`, { waitUntil: "load" });
  await page.setContent(
    `<!doctype html>
<html>
<head>
<meta charset="utf-8">
<link rel="stylesheet" href="${baseUrl}/css/site.css">
<link rel="stylesheet" href="${baseUrl}/css/wasm-site.css">
<link rel="stylesheet" href="${baseUrl}/css/storefront.css">
<script src="${baseUrl}/js/storefrontCommerce.js"></script>
</head>
<body class="bg-neutral-50 text-neutral-950">
<main class="mx-auto max-w-7xl px-4 py-10">
  <section data-proof="cart" class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_360px]">
    <div data-cart-card class="rounded-3xl border border-neutral-200/70 bg-white/95 p-5 shadow-md">
      <div class="flex gap-4">
        <div data-cart-image class="flex h-24 w-24 shrink-0 items-center justify-center overflow-hidden rounded-2xl bg-neutral-50 ring-1 ring-black/5">IMG</div>
        <div class="min-w-0 flex-1"><h2 class="text-xl font-bold text-neutral-950">Cart line</h2><p class="mt-2 text-sm text-neutral-600">Stable line layout</p></div>
      </div>
      <div data-cart-controls class="mt-4 grid gap-4 lg:grid-cols-[minmax(0,1fr)_auto_auto] lg:items-end">
        <input class="mt-1 w-24 rounded-xl border border-neutral-300 bg-white px-3 py-2 text-sm font-semibold text-neutral-900" value="2">
        <button class="inline-flex items-center justify-center rounded bg-white px-4 py-2 text-sm font-semibold text-rose-700 ring-1 ring-rose-200">Remove</button>
      </div>
    </div>
    <aside data-cart-summary class="rounded-3xl border border-neutral-200/70 bg-white/95 p-6 shadow-lg"><a class="inline-flex w-full items-center justify-center rounded bg-amber-500 px-4 py-3 font-semibold text-white">Checkout</a></aside>
  </section>
  <section data-proof="checkout" data-storefront-checkout-shell class="mt-10 mb-6 rounded border border-neutral-200 bg-white px-5 py-4">
    <div data-checkout-grid class="mt-4 grid gap-4 lg:grid-cols-2"><div class="rounded border border-neutral-200 p-4">Shipping</div><div class="rounded border border-neutral-200 p-4">Payment</div></div>
    <button data-checkout-button class="mt-4 rounded bg-neutral-900 px-4 py-2 text-sm font-semibold text-white">Review</button>
  </section>
  <section data-proof="account" data-account-layout class="mt-10 grid gap-6 lg:grid-cols-[240px_minmax(0,1fr)] lg:items-start">
    <nav data-account-nav class="rounded-3xl border border-neutral-200/70 bg-white/95 p-3 text-sm shadow-lg lg:sticky lg:top-24"><a class="mt-2 flex items-center rounded-2xl bg-neutral-950 px-4 py-3 font-semibold text-white shadow-sm first:mt-0">Profile</a><a class="mt-2 flex items-center rounded-2xl px-4 py-3 font-semibold text-neutral-700">Orders</a></nav>
    <article data-account-article class="rounded-3xl border border-neutral-200/70 bg-white/95 shadow-lg"><div class="p-6"><form data-profile-form class="grid max-w-3xl gap-5 sm:grid-cols-2"><input class="mt-2 min-h-11 w-full rounded-xl border border-neutral-300 bg-white px-3 text-sm text-neutral-900 outline-none" value="Alex"><input class="mt-2 min-h-11 w-full rounded-xl border border-neutral-300 bg-white px-3 text-sm text-neutral-900 outline-none" value="Customer"></form></div></article>
  </section>
</main>
</body>
</html>`,
    { waitUntil: "load" },
  );

  await page.waitForTimeout(750);

  async function measure(label) {
    return await page.evaluate((currentLabel) => {
      const read = (selector) => {
        const element = document.querySelector(selector);
        const style = getComputedStyle(element);
        const rect = element.getBoundingClientRect();
        return {
          selector,
          width: Math.round(rect.width),
          height: Math.round(rect.height),
          display: style.display,
          gridTemplateColumns: style.gridTemplateColumns,
          borderRadius: style.borderRadius,
          boxShadow: style.boxShadow,
          backgroundColor: style.backgroundColor,
          padding: style.padding,
          position: style.position,
          top: style.top,
        };
      };

      const stylesheets = Array.from(document.querySelectorAll('link[rel="stylesheet"]')).map((link) => link.href);
      return {
        label: currentLabel,
        viewport: {
          width: window.innerWidth,
          height: window.innerHeight,
        },
        stylesheets,
        duplicateStylesheets: stylesheets.filter((href, index) => stylesheets.indexOf(href) !== index),
        cartCard: read("[data-cart-card]"),
        cartImage: read("[data-cart-image]"),
        cartControls: read("[data-cart-controls]"),
        checkoutShell: read("[data-storefront-checkout-shell]"),
        checkoutGrid: read("[data-checkout-grid]"),
        accountLayout: read("[data-account-layout]"),
        accountNav: read("[data-account-nav]"),
        profileForm: read("[data-profile-form]"),
      };
    }, label);
  }

  const desktop = await measure("desktop");
  await page.screenshot({ path: "output/playwright/storefront-v2-css-phase5-desktop.png", fullPage: true });

  await page.setViewportSize({ width: 390, height: 844 });
  await page.waitForTimeout(500);
  const mobile = await measure("mobile");
  await page.screenshot({ path: "output/playwright/storefront-v2-css-phase5-mobile.png", fullPage: true });

  const evidence = {
    generatedAtUtc: new Date().toISOString(),
    baseUrl,
    mode: "static-css-layout-fallback-because-docker-daemon-unavailable",
    dockerBlocker:
      "scripts/run-v2-local.ps1 failed because Docker Desktop daemon pipe dockerDesktopLinuxEngine was unavailable; backend-dependent Storefront routes could not be rendered.",
    assetUrls,
    responseStatuses,
    failedRequests,
    consoleMessages,
    directCommerceNodeRequests: responseStatuses
      .concat(failedRequests)
      .filter((item) => item.url.includes("localhost:5180") || item.url.includes("/api/storefront/")),
    desktop,
    mobile,
    screenshots: [
      "output/playwright/storefront-v2-css-phase5-desktop.png",
      "output/playwright/storefront-v2-css-phase5-mobile.png",
    ],
  };

  return evidence;
}
