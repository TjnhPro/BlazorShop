const fs = require("fs");
const path = require("path");

const playwrightPath = path.resolve(__dirname, "../../.gstack/playwright-qa/node_modules/playwright");
const { chromium } = require(playwrightPath);

const baseUrl = trimEnd(process.env.STOREFRONT_QA_BASE_URL || "http://localhost:18598", "/");
const commerceNodeApiUrl = trimEnd(process.env.COMMERCENODE_QA_API_URL || "http://localhost:5180", "/");
const mailpitApiUrl = trimEnd(process.env.MAILPIT_API_URL || "http://localhost:8025/api/v1", "/");
const customerEmail = process.env.STOREFRONT_QA_ORDER_EMAIL || "qa.customer@example.local";
const customerPassword = process.env.STOREFRONT_QA_ORDER_PASSWORD || "QaCustomer123!";
const defaultStoreKey = process.env.STOREFRONT_QA_STORE_KEY || "default";
const secondStoreKey = process.env.STOREFRONT_QA_SECOND_STORE_KEY || "qa-s2";
const expectedStoreName = process.env.STOREFRONT_QA_STORE_NAME || "Default QA Store";
const expectedDefaultFrom = process.env.STOREFRONT_QA_EXPECTED_FROM || "default-sender@example.local";
const expectedSecondFrom = process.env.STOREFRONT_QA_SECOND_EXPECTED_FROM || "s2-sender@example.local";
const nodeKey = process.env.COMMERCENODE_QA_NODE_KEY || "dev-node";
const nodeSecret = process.env.COMMERCENODE_QA_NODE_SECRET || "dev-node-secret";
const headed = (process.env.HEADLESS || "false").toLowerCase() !== "true";
const artifactRoot = path.resolve(process.env.STOREFRONT_QA_ARTIFACT_DIR || ".gstack/qa-reports/order-email-e2e");

async function main() {
  ensureDir(artifactRoot);
  const evidence = {
    generatedAt: new Date().toISOString(),
    baseUrl,
    commerceNodeApiUrl,
    mailpitApiUrl,
    defaultStoreKey,
    secondStoreKey,
    customerEmail,
    expectedStoreName,
    expectedDefaultFrom,
    expectedSecondFrom,
    headed,
    steps: [],
  };

  const originalDefaultSettings = await getStoreEmailSettings(defaultStoreKey);
  const originalSecondSettings = await getStoreEmailSettings(secondStoreKey);
  await clearMailpit();
  evidence.steps.push({ step: "mailpit.clear.before-success", ok: true });

  const browser = await chromium.launch({ headless: !headed });
  const context = await browser.newContext({ viewport: { width: 1366, height: 768 } });
  const page = await context.newPage();
  const network = createNetworkAudit(page);

  try {
    const success = await placeCodOrder(page, "success");
    evidence.steps.push({
      step: "checkout.success.place-cod-order",
      ok: true,
      orderReference: success.orderReference,
      confirmationScreenshot: success.confirmationScreenshot,
      orderListScreenshot: success.orderListScreenshot,
      orderDetailScreenshot: success.orderDetailScreenshot,
      orderReceiptScreenshot: success.orderReceiptScreenshot,
    });

    const orderCreatedTask = await waitForTask("order.created", success.orderReference, "succeeded", 30000);
    evidence.steps.push({
      step: "task.order-created.succeeded",
      ok: true,
      publicId: getValue(orderCreatedTask, "publicId"),
      status: getValue(orderCreatedTask, "status"),
    });

    const queued = await waitForQueuedMessage(defaultStoreKey, success.orderReference, ["sent"], 45000);
    evidence.steps.push({
      step: "queued-message.order-placed.sent",
      ok: true,
      publicId: queued.publicId,
      status: queued.status,
      subject: queued.subject,
    });

    const orderMessage = await waitForMail(
      (candidate) => mailContains(candidate, success.orderReference),
      30000);
    const orderDetail = await readMessageDetail(orderMessage);
    await delay(1500);
    const matchingMessages = await listMail((candidate) => mailContains(candidate, success.orderReference));
    assertEqual(matchingMessages.length, 1, `Expected exactly one order email for ${success.orderReference}.`);
    assertOrderEmail(orderMessage, orderDetail, success.orderReference, expectedDefaultFrom);
    evidence.steps.push({
      step: "mailpit.order-placed.exactly-one",
      ok: true,
      orderReference: success.orderReference,
      matchCount: matchingMessages.length,
      subject: getValue(orderMessage, "Subject") || getValue(orderDetail, "Subject"),
      from: stringifyAddress(getValue(orderMessage, "From") || getValue(orderDetail, "From")),
      to: stringifyAddress(getValue(orderMessage, "To") || getValue(orderDetail, "To")),
    });

    await clearMailpit();
    await updateStoreEmailSettings(defaultStoreKey, { ...originalDefaultSettings, enabled: false });
    evidence.steps.push({ step: "smtp.disable.before-outage-order", ok: true });

    const outage = await placeCodOrder(page, "smtp-outage");
    evidence.steps.push({
      step: "checkout.smtp-outage.place-cod-order",
      ok: true,
      orderReference: outage.orderReference,
      confirmationScreenshot: outage.confirmationScreenshot,
    });

    await waitForTask("order.created", outage.orderReference, "succeeded", 30000);
    const failedQueued = await waitForQueuedMessage(defaultStoreKey, outage.orderReference, ["waiting_retry", "failed"], 45000);
    evidence.steps.push({
      step: "queued-message.smtp-outage.waiting-retry-or-failed",
      ok: true,
      publicId: failedQueued.publicId,
      status: failedQueued.status,
      errorCode: failedQueued.errorCode,
    });

    const outageMessage = await waitForMail(
      (candidate) => mailContains(candidate, outage.orderReference),
      5000,
      { allowTimeout: true });
    if (outageMessage) {
      throw new Error(`SMTP-disabled order unexpectedly sent email for ${outage.orderReference}.`);
    }

    await updateStoreEmailSettings(defaultStoreKey, originalDefaultSettings);
    await retryQueuedMessage(defaultStoreKey, failedQueued.publicId);
    const retried = await waitForQueuedMessage(defaultStoreKey, outage.orderReference, ["sent"], 70000);
    const retriedMail = await waitForMail(
      (candidate) => mailContains(candidate, outage.orderReference),
      30000);
    assertOrderEmail(retriedMail, await readMessageDetail(retriedMail), outage.orderReference, expectedDefaultFrom);
    evidence.steps.push({
      step: "queued-message.retry.sent-after-restore",
      ok: true,
      publicId: retried.publicId,
      status: retried.status,
    });

    await clearMailpit();
    const defaultTestSubject = `SMTP isolation ${Date.now()} default`;
    const secondTestSubject = `SMTP isolation ${Date.now()} qa-s2`;
    await sendStoreEmailTest(defaultStoreKey, defaultTestSubject);
    await sendStoreEmailTest(secondStoreKey, secondTestSubject);
    const defaultTestMail = await waitForMail(
      (candidate) => mailContains(candidate, defaultTestSubject),
      30000);
    const secondTestMail = await waitForMail(
      (candidate) => mailContains(candidate, secondTestSubject),
      30000);
    assertSender(defaultTestMail, expectedDefaultFrom, defaultTestSubject);
    assertSender(secondTestMail, expectedSecondFrom, secondTestSubject);
    evidence.steps.push({
      step: "mailpit.store-sender-isolation",
      ok: true,
      defaultFrom: stringifyAddress(getValue(defaultTestMail, "From")),
      secondFrom: stringifyAddress(getValue(secondTestMail, "From")),
    });

    assertNoUnexpectedNetwork(network);
    evidence.steps.push({ step: "network.guardrails", ok: true, summary: network.summary() });
    evidence.result = "passed";
  } catch (error) {
    evidence.result = "failed";
    evidence.error = error instanceof Error ? error.message : String(error);
    await page.screenshot({ path: path.join(artifactRoot, "failure.png"), fullPage: true }).catch(() => {});
    throw error;
  } finally {
    await updateStoreEmailSettings(defaultStoreKey, originalDefaultSettings).catch(() => {});
    await updateStoreEmailSettings(secondStoreKey, originalSecondSettings).catch(() => {});
    await browser.close();
    fs.writeFileSync(path.join(artifactRoot, "result.json"), JSON.stringify(evidence, null, 2));
  }
}

async function placeCodOrder(page, label) {
  await signIn(page, customerEmail, customerPassword);
  await addSimpleProductToCart(page);
  await page.goto(`${baseUrl}/checkout`, { waitUntil: "domcontentloaded" });
  await dismissConsentIfVisible(page);
  await page.locator("[data-storefront-checkout-shell]").waitFor({ state: "visible", timeout: 20000 });
  await expectBodyContains(page, "Cash on Delivery");
  await expectBodyContains(page, "EUR 100.00");

  const checkoutState = await readCheckoutState(page);
  if (!checkoutState.body.checkoutSessionId || !checkoutState.body.cartVersion) {
    throw new Error("Checkout state did not include checkoutSessionId and cartVersion.");
  }

  const selectedAddressId = await page.locator("[data-storefront-address-select]").inputValue();
  if (!selectedAddressId) {
    throw new Error("Checkout did not select a synthetic customer address.");
  }

  await postCheckoutJson(page, "/api/checkout/addresses", {
    checkoutSessionId: checkoutState.body.checkoutSessionId,
    expectedCartVersion: checkoutState.body.cartVersion,
    shippingAddressId: selectedAddressId,
    billingAddressId: selectedAddressId,
    useShippingAddressAsBillingAddress: true,
  });

  await page.locator("[data-storefront-checkout-shell]").getByRole("button", { name: "Refresh" }).click();
  await page.locator("[data-storefront-checkout-shell]").waitFor({ state: "visible", timeout: 10000 });

  const shippingOptions = page.locator("input[name='wasmShippingOption']");
  if (await shippingOptions.count()) {
    await shippingOptions.first().check();
  }

  const paymentOptions = page.locator("input[name='wasmPaymentMethod']");
  await paymentOptions.first().waitFor({ state: "visible", timeout: 10000 });
  await paymentOptions.first().check();

  await page.getByRole("button", { name: "Review latest checkout" }).click();
  await expectBodyMatches(page, /review|ready|Place order/i);
  const place = page.locator("[data-storefront-checkout-shell]").getByRole("button", { name: "Place order" });
  await place.waitFor({ state: "visible", timeout: 10000 });
  await place.dblclick();
  await page.waitForURL("**/checkout?orderReference=*", { timeout: 30000 });
  await expectBodyContains(page, "Thank you");
  const orderReference = new URL(page.url()).searchParams.get("orderReference");
  if (!orderReference) {
    throw new Error("Order confirmation URL did not include orderReference.");
  }

  const confirmationScreenshot = path.join(artifactRoot, `${label}-confirmation.png`);
  await page.screenshot({ path: confirmationScreenshot, fullPage: true });

  await page.goto(`${baseUrl}/account/orders`, { waitUntil: "domcontentloaded" });
  await expectBodyContains(page, orderReference);
  const orderListScreenshot = path.join(artifactRoot, `${label}-order-list.png`);
  await page.screenshot({ path: orderListScreenshot, fullPage: true });

  await page.goto(`${baseUrl}/account/orders/${encodeURIComponent(orderReference)}`, { waitUntil: "domcontentloaded" });
  await expectBodyContains(page, orderReference);
  await expectBodyMatches(page, /EUR 100\.00|100\.00/);
  const orderDetailScreenshot = path.join(artifactRoot, `${label}-order-detail.png`);
  await page.screenshot({ path: orderDetailScreenshot, fullPage: true });

  await page.goto(`${baseUrl}/account/orders/${encodeURIComponent(orderReference)}/receipt`, { waitUntil: "domcontentloaded" });
  await expectBodyContains(page, orderReference);
  await expectBodyMatches(page, /receipt|EUR 100\.00|100\.00/i);
  const orderReceiptScreenshot = path.join(artifactRoot, `${label}-order-receipt.png`);
  await page.screenshot({ path: orderReceiptScreenshot, fullPage: true });

  return { orderReference, confirmationScreenshot, orderListScreenshot, orderDetailScreenshot, orderReceiptScreenshot };
}

async function signIn(page, email, password) {
  await page.goto(`${baseUrl}/signin?returnUrl=${encodeURIComponent("/account/profile")}`, { waitUntil: "domcontentloaded" });
  if (/\/account\/profile/i.test(new URL(page.url()).pathname)) {
    return;
  }

  const passwordInput = page.getByLabel("Password", { exact: true });
  if (await passwordInput.count() === 0) {
    await page.goto(`${baseUrl}/account/profile`, { waitUntil: "domcontentloaded" });
    if (/\/account\/profile/i.test(new URL(page.url()).pathname)) {
      return;
    }
  }

  await page.getByLabel("Email address").fill(email);
  await passwordInput.fill(password);
  await page.getByRole("button", { name: /^sign in$/i }).click();
  await page.waitForURL(/\/account\/profile/, { timeout: 20000 });
}

async function addSimpleProductToCart(page) {
  await page.goto(`${baseUrl}/product/qa-simple-product-100`, { waitUntil: "domcontentloaded" });
  await dismissConsentIfVisible(page);
  const purchase = page.locator("#purchase");
  await purchase.getByRole("button", { name: /add to cart/i }).click();
  await page.locator("#product-cart-feedback").waitFor({ state: "visible", timeout: 10000 });
  const feedback = await page.locator("#product-cart-feedback").innerText({ timeout: 10000 });
  if (!/added to cart/i.test(feedback)) {
    throw new Error(`Expected add-to-cart feedback, got "${feedback}".`);
  }
}

async function dismissConsentIfVisible(page) {
  const essential = page.getByRole("button", { name: "Essential only" });
  if (await essential.count()) {
    await essential.first().click().catch(() => {});
  }
}

async function readCheckoutState(page) {
  return await page.evaluate(async () => {
    const response = await fetch("/api/checkout", { credentials: "same-origin" });
    return { status: response.status, body: await response.json() };
  });
}

async function postCheckoutJson(page, url, payload) {
  const csrf = await readAntiforgery(page);
  const result = await page.evaluate(async ({ targetUrl, body, csrf }) => {
    const response = await fetch(targetUrl, {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "application/json", [csrf.headerName]: csrf.token },
      credentials: "same-origin",
      body: JSON.stringify(body),
    });
    const text = await response.text();
    return { status: response.status, body: text ? JSON.parse(text) : null };
  }, { targetUrl: url, body: payload, csrf });
  if (result.status < 200 || result.status >= 300) {
    throw new Error(`${url} returned ${result.status}: ${JSON.stringify(result.body)}`);
  }

  return result;
}

async function readAntiforgery(page) {
  const csrf = await page.evaluate(() => {
    const token = document.querySelector('meta[name="blazorshop-antiforgery-token"]')?.getAttribute("content");
    const headerName = document.querySelector('meta[name="blazorshop-antiforgery-header"]')?.getAttribute("content") || "X-CSRF-TOKEN";
    return { token, headerName };
  });
  if (!csrf.token) {
    throw new Error("Storefront antiforgery token was not found.");
  }

  return csrf;
}

async function getStoreEmailSettings(storeKey) {
  const payload = await adminJson(`/api/commerce/admin/email-settings?storeKey=${encodeURIComponent(storeKey)}`);
  return normalizeSettings(unwrapData(payload));
}

async function updateStoreEmailSettings(storeKey, settings) {
  const request = {
    enabled: Boolean(settings.enabled),
    smtpHost: settings.smtpHost,
    smtpPort: settings.smtpPort,
    useSsl: Boolean(settings.useSsl),
    username: settings.username,
    password: null,
    clearPassword: false,
    useExistingPassword: true,
    fromEmail: settings.fromEmail,
    fromDisplayName: settings.fromDisplayName,
    replyToEmail: settings.replyToEmail,
    deliveryMode: settings.deliveryMode,
    captureRedirectToEmail: settings.captureRedirectToEmail,
  };
  const payload = await adminJson(`/api/commerce/admin/email-settings?storeKey=${encodeURIComponent(storeKey)}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  return normalizeSettings(unwrapData(payload));
}

async function sendStoreEmailTest(storeKey, subject) {
  return await adminJson(`/api/commerce/admin/email-settings/test-send?storeKey=${encodeURIComponent(storeKey)}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ toEmail: "qa.customer@example.local", subject }),
  });
}

async function retryQueuedMessage(storeKey, publicId) {
  return await adminJson(`/api/commerce/admin/queued-messages/${encodeURIComponent(publicId)}/retry?storeKey=${encodeURIComponent(storeKey)}`, {
    method: "POST",
  });
}

async function waitForTask(taskType, correlationId, expectedStatus, timeoutMs) {
  return await waitFor(async () => {
    const payload = await adminJson(`/api/commerce/tasks?taskType=${encodeURIComponent(taskType)}&take=50`);
    const items = getValue(unwrapData(payload), "items") || [];
    return items.find((item) => {
      return String(getValue(item, "correlationId") || "").toLowerCase() === correlationId.toLowerCase()
        && String(getValue(item, "status") || "").toLowerCase() === expectedStatus.toLowerCase();
    });
  }, timeoutMs, `Timed out waiting for ${taskType} ${correlationId} to reach ${expectedStatus}.`);
}

async function waitForQueuedMessage(storeKey, orderReference, statuses, timeoutMs) {
  return await waitFor(async () => {
    const payload = await adminJson(`/api/commerce/admin/queued-messages?storeKey=${encodeURIComponent(storeKey)}&templateSystemName=order.placed&take=50`);
    const data = unwrapData(payload);
    const items = getValue(data, "items") || [];
    const item = items.find((candidate) => {
      const subject = String(getValue(candidate, "subject") || "");
      return subject.toLowerCase().includes(orderReference.toLowerCase());
    });
    if (!item) {
      return null;
    }

    const publicId = getValue(item, "publicId");
    const detailPayload = await adminJson(`/api/commerce/admin/queued-messages/${encodeURIComponent(publicId)}?storeKey=${encodeURIComponent(storeKey)}`);
    const detail = unwrapData(detailPayload);
    const status = String(getValue(detail, "status") || getValue(item, "status") || "").toLowerCase();
    if (!statuses.map((value) => value.toLowerCase()).includes(status)) {
      return null;
    }

    return {
      publicId,
      status,
      subject: getValue(detail, "subject") || getValue(item, "subject"),
      errorCode: getValue(detail, "errorCode"),
    };
  }, timeoutMs, `Timed out waiting for order.placed queued message for ${orderReference}.`);
}

async function clearMailpit() {
  await fetchJson(`${mailpitApiUrl}/messages`, { method: "DELETE" });
}

async function waitForMail(predicate, timeoutMs, options = {}) {
  const result = await waitFor(async () => {
    const payload = await fetchJson(`${mailpitApiUrl}/messages`);
    const messages = payload.messages || payload.Messages || [];
    return messages.find(predicate) || null;
  }, timeoutMs, "Timed out waiting for Mailpit message.", options.allowTimeout);
  return result;
}

async function listMail(predicate) {
  const payload = await fetchJson(`${mailpitApiUrl}/messages`);
  const messages = payload.messages || payload.Messages || [];
  return messages.filter(predicate);
}

async function readMessageDetail(message) {
  const id = getValue(message, "ID") || getValue(message, "Id") || getValue(message, "id");
  if (!id) {
    return message;
  }

  return await fetchJson(`${mailpitApiUrl}/message/${encodeURIComponent(id)}`);
}

async function adminJson(pathAndQuery, options = {}) {
  const headers = {
    "X-Node-Key": nodeKey,
    "X-Node-Secret": nodeSecret,
    ...(options.headers || {}),
  };
  return await fetchJson(`${commerceNodeApiUrl}${pathAndQuery}`, { ...options, headers });
}

async function fetchJson(url, options = {}) {
  const response = await fetch(url, options);
  if (!response.ok) {
    const text = await response.text().catch(() => "");
    throw new Error(`${options.method || "GET"} ${url} returned ${response.status}: ${text}`);
  }

  const text = await response.text();
  if (!text) {
    return {};
  }

  try {
    return JSON.parse(text);
  } catch {
    return { raw: text };
  }
}

function assertOrderEmail(summary, detail, orderReference, expectedFrom) {
  const subject = String(getValue(summary, "Subject") || getValue(detail, "Subject") || "");
  const payloadText = decodeHtmlEntities(JSON.stringify(detail));
  const from = stringifyAddress(getValue(summary, "From") || getValue(detail, "From"));
  if (!subject.includes(expectedStoreName) && !payloadText.includes(expectedStoreName)) {
    throw new Error(`Order email did not contain store name ${expectedStoreName}.`);
  }

  if (!subject.includes(orderReference) || !payloadText.includes(orderReference)) {
    throw new Error(`Order email did not contain order reference ${orderReference} in subject and body.`);
  }

  if (!payloadText.includes("EUR") || !/\b\d+\.\d{2}\b/.test(payloadText)) {
    throw new Error("Order email did not contain total amount and currency.");
  }

  if (!payloadText.includes(`/account/orders/${orderReference}`)) {
    throw new Error("Order email did not contain account order detail link.");
  }

  assertSender({ From: from }, expectedFrom, orderReference);
}

function assertSender(message, expectedFrom, label) {
  const from = stringifyAddress(getValue(message, "From"));
  if (!from.toLowerCase().includes(expectedFrom.toLowerCase())) {
    throw new Error(`Expected sender ${expectedFrom} for ${label}, got ${from || "(empty)"}.`);
  }
}

function assertNoUnexpectedNetwork(network) {
  const unexpected5xx = network.responses.filter((item) => item.status >= 500);
  const forbiddenCalls = network.requests.filter((item) => {
    const url = new URL(item.url);
    return url.origin === baseUrl
      && (url.pathname.startsWith("/api/internal")
        || url.pathname.startsWith("/api/commerce")
        || url.pathname.startsWith("/api/control-plane"));
  });
  const retiredFlowCalls = network.requests.filter((item) => {
    const url = new URL(item.url);
    return url.origin === baseUrl && isRetiredStorefrontFlowPath(url.pathname);
  });

  if (unexpected5xx.length > 0) {
    throw new Error(`Unexpected 5xx responses: ${JSON.stringify(unexpected5xx)}`);
  }

  if (forbiddenCalls.length > 0) {
    throw new Error(`Storefront browser made forbidden admin/control calls: ${JSON.stringify(forbiddenCalls)}`);
  }

  if (retiredFlowCalls.length > 0) {
    throw new Error(`Storefront browser called retired commerce flow routes: ${JSON.stringify(retiredFlowCalls)}`);
  }
}

function isRetiredStorefrontFlowPath(pathname) {
  return pathname.includes("/cart/save-checkout")
    || pathname.includes("/orders/confirm")
    || pathname.includes("/orders/current-user/items")
    || pathname.includes("/payments/paypal/capture");
}

function createNetworkAudit(page) {
  const requests = [];
  const responses = [];
  page.on("request", (request) => {
    requests.push({ method: request.method(), url: request.url() });
  });
  page.on("response", (response) => {
    responses.push({ method: response.request().method(), url: response.url(), status: response.status() });
  });

  return {
    requests,
    responses,
    summary() {
      return {
        requestCount: requests.length,
        responseCount: responses.length,
        response5xxCount: responses.filter((item) => item.status >= 500).length,
        retiredFlowCallCount: requests.filter((item) => isRetiredStorefrontFlowPath(new URL(item.url).pathname)).length,
      };
    },
  };
}

function mailContains(message, text) {
  return JSON.stringify(message).toLowerCase().includes(text.toLowerCase());
}

async function expectBodyContains(page, text) {
  await page.locator("body").waitFor({ state: "visible", timeout: 10000 });
  const content = await page.locator("body").innerText({ timeout: 10000 });
  if (!content.includes(text)) {
    throw new Error(`Expected page body to contain "${text}".`);
  }
}

async function expectBodyMatches(page, pattern) {
  await page.locator("body").waitFor({ state: "visible", timeout: 10000 });
  const content = await page.locator("body").innerText({ timeout: 10000 });
  if (!pattern.test(content)) {
    throw new Error(`Expected page body to match ${pattern}.`);
  }
}

async function waitFor(probe, timeoutMs, errorMessage, allowTimeout = false) {
  const startedAt = Date.now();
  while (Date.now() - startedAt < timeoutMs) {
    const value = await probe();
    if (value) {
      return value;
    }

    await delay(500);
  }

  if (allowTimeout) {
    return null;
  }

  throw new Error(errorMessage);
}

function unwrapData(payload) {
  return getValue(payload, "data") || getValue(payload, "Data") || payload;
}

function normalizeSettings(settings) {
  return {
    enabled: Boolean(getValue(settings, "enabled")),
    smtpHost: getValue(settings, "smtpHost"),
    smtpPort: getValue(settings, "smtpPort"),
    useSsl: Boolean(getValue(settings, "useSsl")),
    username: getValue(settings, "username"),
    fromEmail: getValue(settings, "fromEmail"),
    fromDisplayName: getValue(settings, "fromDisplayName"),
    replyToEmail: getValue(settings, "replyToEmail"),
    deliveryMode: getValue(settings, "deliveryMode") || "capture",
    captureRedirectToEmail: getValue(settings, "captureRedirectToEmail"),
  };
}

function getValue(value, name) {
  if (!value || typeof value !== "object") {
    return undefined;
  }

  if (Object.prototype.hasOwnProperty.call(value, name)) {
    return value[name];
  }

  const camel = name.charAt(0).toLowerCase() + name.slice(1);
  if (Object.prototype.hasOwnProperty.call(value, camel)) {
    return value[camel];
  }

  const pascal = name.charAt(0).toUpperCase() + name.slice(1);
  if (Object.prototype.hasOwnProperty.call(value, pascal)) {
    return value[pascal];
  }

  return undefined;
}

function stringifyAddress(value) {
  if (!value) {
    return "";
  }

  return typeof value === "string" ? value : JSON.stringify(value);
}

function decodeHtmlEntities(value) {
  return value
    .replace(/&amp;/g, "&")
    .replace(/&quot;/g, "\"")
    .replace(/&#x27;/g, "'")
    .replace(/&#39;/g, "'")
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">");
}

function assertEqual(actual, expected, message) {
  if (actual !== expected) {
    throw new Error(`${message} Actual=${actual} Expected=${expected}.`);
  }
}

function ensureDir(directory) {
  fs.mkdirSync(directory, { recursive: true });
}

function trimEnd(value, suffix) {
  return value.endsWith(suffix) ? value.slice(0, -suffix.length) : value;
}

function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
