async (page) => {
  const baseUrl = "http://localhost:18598";
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

  async function readPage(route, viewport, screenshotPath) {
    await page.setViewportSize(viewport);
    const response = await page.goto(`${baseUrl}${route}`, {
      waitUntil: "networkidle",
      timeout: 45000,
    });
    await page.waitForTimeout(1000);
    await page.screenshot({ path: screenshotPath, fullPage: true });

    return await page.evaluate(
      ({ currentRoute, status, viewportName }) => {
        const readOptional = (selector) => {
          const element = document.querySelector(selector);
          if (!element) {
            return null;
          }

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
          };
        };

        const stylesheets = Array.from(document.querySelectorAll('link[rel="stylesheet"]')).map((link) => link.href);
        const scripts = Array.from(document.querySelectorAll("script[src]")).map((script) => script.src);
        return {
          route: currentRoute,
          viewportName,
          status,
          url: location.href,
          title: document.title,
          bodyTextSample: document.body.innerText.slice(0, 500),
          stylesheets,
          scripts,
          duplicateStylesheets: stylesheets.filter((href, index) => stylesheets.indexOf(href) !== index),
          cartQuantity: readOptional("[data-storefront-cart-quantity]"),
          cartLineCard: readOptional("[data-storefront-cart-quantity]")?.selector
            ? readOptional("[data-storefront-cart-quantity]")
            : readOptional(".rounded-3xl"),
          checkoutShell: readOptional("[data-storefront-checkout-shell]"),
          accountLayout: readOptional("[data-account-layout]"),
          accountFallbackPanel: readOptional(".rounded-3xl"),
        };
      },
      {
        currentRoute: route,
        status: response?.status() ?? 0,
        viewportName: `${viewport.width}x${viewport.height}`,
      },
    );
  }

  const cartDesktop = await readPage(
    "/cart",
    { width: 1280, height: 900 },
    "output/playwright/storefront-v2-css-phase5-route-cart-desktop.png",
  );
  const checkoutDesktop = await readPage(
    "/checkout",
    { width: 1280, height: 900 },
    "output/playwright/storefront-v2-css-phase5-route-checkout-desktop.png",
  );
  const accountDesktop = await readPage(
    "/account",
    { width: 1280, height: 900 },
    "output/playwright/storefront-v2-css-phase5-route-account-desktop.png",
  );
  const checkoutMobile = await readPage(
    "/checkout",
    { width: 390, height: 844 },
    "output/playwright/storefront-v2-css-phase5-route-checkout-mobile.png",
  );

  return {
    generatedAtUtc: new Date().toISOString(),
    baseUrl,
    mode: "real-storefront-v2-routes",
    failedRequests,
    consoleMessages,
    responseStatuses,
    staticAssetResponses: responseStatuses.filter((item) => /\/(css|js|_framework|_content)\//.test(item.url)),
    staticOrHydrationConsoleErrors: consoleMessages.filter(
      (item) => item.type === "error" && /(css|stylesheet|script|_framework|wasm|hydration|hydrate)/i.test(item.text),
    ),
    unauthenticatedRouteConsoleErrors: consoleMessages.filter(
      (item) => item.type === "error" && /401|Unauthorized/i.test(item.text),
    ),
    directCommerceNodeRequests: responseStatuses
      .concat(failedRequests)
      .filter((item) => item.url.includes("localhost:5180") || item.url.includes("/api/storefront/")),
    staticAssetFailures: responseStatuses
      .concat(failedRequests)
      .filter((item) => /\/(css|js|_framework|_content)\//.test(item.url) && (item.status >= 400 || item.failure)),
    pages: {
      cartDesktop,
      checkoutDesktop,
      accountDesktop,
      checkoutMobile,
    },
    screenshots: [
      "output/playwright/storefront-v2-css-phase5-route-cart-desktop.png",
      "output/playwright/storefront-v2-css-phase5-route-checkout-desktop.png",
      "output/playwright/storefront-v2-css-phase5-route-account-desktop.png",
      "output/playwright/storefront-v2-css-phase5-route-checkout-mobile.png",
    ],
  };
}
