const fs = require("fs");
const path = require("path");

const playwrightPath = path.resolve(__dirname, "../../.gstack/playwright-qa/node_modules/playwright");
const { chromium } = require(playwrightPath);

const storefrontBaseUrl = trimEnd(process.env.STOREFRONT_QA_BASE_URL || "http://localhost:18598", "/");
const controlPlaneApiUrl = trimEnd(process.env.CONTROLPLANE_QA_API_URL || "http://localhost:5280", "/");
const controlPlaneWebUrl = trimEnd(process.env.CONTROLPLANE_QA_WEB_URL || "http://localhost:5281", "/");
const commerceNodeApiUrl = trimEnd(process.env.COMMERCENODE_QA_API_URL || "http://localhost:5180", "/");
const adminEmail = process.env.CONTROLPLANE_QA_ADMIN_EMAIL || "admin@example.local";
const adminPassword = process.env.CONTROLPLANE_QA_ADMIN_PASSWORD || "Admin123!";
const storeKey = process.env.STOREFRONT_QA_STORE_KEY || "default";
const headed = (process.env.HEADLESS || "false").toLowerCase() !== "true";
const artifactRoot = path.resolve(process.env.STOREFRONT_QA_ARTIFACT_DIR || ".gstack/qa-reports/registration-policy-e2e");

async function main() {
  ensureDir(artifactRoot);
  const evidence = {
    generatedAt: new Date().toISOString(),
    storefrontBaseUrl,
    controlPlaneApiUrl,
    controlPlaneWebUrl,
    commerceNodeApiUrl,
    storeKey,
    headed,
    steps: [],
  };

  const token = await loginControlPlane();
  const store = await findStore(token, storeKey);
  const original = await getSecurityPrivacy(token, store.publicId);
  evidence.steps.push({ step: "control-plane.security-privacy.loaded", ok: true, storePublicId: store.publicId, originalMode: original.registration.mode });

  const browser = await chromium.launch({ headless: !headed });
  const context = await browser.newContext({ viewport: { width: 1366, height: 768 } });
  const page = await context.newPage();
  let storefrontNetwork = null;

  try {
    await loginControlPlaneWeb(page);
    await page.goto(`${controlPlaneWebUrl}/commerce-admin/security-privacy`, { waitUntil: "networkidle" });
    await page.screenshot({ path: path.join(artifactRoot, "control-plane-security-privacy.png"), fullPage: true });
    evidence.steps.push({ step: "control-plane.web.security-privacy.opened", ok: true, screenshot: "control-plane-security-privacy.png" });

    await updateSecurityPrivacy(token, store.publicId, original, "disabled");
    const disabledPolicy = await getStorefrontRegistrationPolicy();
    assert(disabledPolicy.registrationAllowed === false, "Storefront policy should be disabled.");
    evidence.steps.push({ step: "control-plane.api.registration-disabled.saved", ok: true, mode: disabledPolicy.mode });

    storefrontNetwork = createNetworkAudit(page, storefrontBaseUrl);
    await page.goto(`${storefrontBaseUrl}/register`, { waitUntil: "networkidle" });
    await expectText(page, "Customer registration is disabled.");
    const registerFields = await page.locator('input[name="FullName"], input[data-storefront-captcha-token="registration"]').count();
    assert(registerFields === 0, "Register form fields should not be rendered while disabled.");
    await page.screenshot({ path: path.join(artifactRoot, "storefront-register-disabled.png"), fullPage: true });
    evidence.steps.push({ step: "storefront.register.disabled-state", ok: true, screenshot: "storefront-register-disabled.png" });

    const tamper = await directRegisterAttempt();
    assert(tamper.status === 403, `Direct register should be 403, got ${tamper.status}.`);
    assert(tamper.body && tamper.body.code === "auth.registration_disabled", "Direct register should return auth.registration_disabled.");
    evidence.steps.push({ step: "commerce-node.direct-register.disabled-forbidden", ok: true, status: tamper.status, code: tamper.body.code });

    await updateSecurityPrivacy(token, store.publicId, original, "standard");
    const enabledPolicy = await getStorefrontRegistrationPolicy();
    assert(enabledPolicy.registrationAllowed === true, "Storefront policy should be standard after restore.");
    evidence.steps.push({ step: "control-plane.api.registration-standard.restored", ok: true, mode: enabledPolicy.mode });

    await page.goto(`${storefrontBaseUrl}/register`, { waitUntil: "networkidle" });
    await expectText(page, "Create account");
    const enabledFields = await page.locator('input[name="FullName"], input[data-storefront-captcha-token="registration"]').count();
    assert(enabledFields >= 2, "Register form should be rendered after restore.");
    await page.screenshot({ path: path.join(artifactRoot, "storefront-register-enabled.png"), fullPage: true });
    evidence.steps.push({ step: "storefront.register.enabled-state", ok: true, screenshot: "storefront-register-enabled.png" });

    const audit = storefrontNetwork.summary();
    assert(audit.forbiddenBrowserCalls.length === 0, `Forbidden browser calls found: ${audit.forbiddenBrowserCalls.join(", ")}`);
    assert(audit.response5xx.length === 0, `Unexpected 5xx responses found: ${audit.response5xx.join(", ")}`);
    evidence.network = audit;
    evidence.result = "passed";
  } catch (error) {
    evidence.result = "failed";
    evidence.error = error && error.stack ? error.stack : String(error);
    try {
      await page.screenshot({ path: path.join(artifactRoot, "failure.png"), fullPage: true });
      evidence.failureScreenshot = "failure.png";
    } catch {
      // Ignore screenshot errors while reporting original failure.
    }
    throw error;
  } finally {
    try {
      await updateSecurityPrivacy(token, store.publicId, original, original.registration.mode || "standard");
      evidence.steps.push({ step: "control-plane.api.registration-policy.final-restore", ok: true, mode: original.registration.mode || "standard" });
    } finally {
      await browser.close();
      fs.writeFileSync(path.join(artifactRoot, "result.json"), JSON.stringify(evidence, null, 2));
    }
  }
}

async function loginControlPlaneWeb(page) {
  await page.goto(`${controlPlaneWebUrl}/login`, { waitUntil: "networkidle" });
  await page.getByLabel("Email").fill(adminEmail);
  await page.getByLabel("Password").fill(adminPassword);
  await page.getByRole("button", { name: /sign in/i }).click();
  await page.waitForURL((url) => !url.pathname.startsWith("/login"), { timeout: 30000 });
}

async function loginControlPlane() {
  const response = await fetchJson(`${controlPlaneApiUrl}/api/control-plane/auth/login`, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ email: adminEmail, password: adminPassword }),
  });

  assert(response.status === 200, `Control Plane login failed: ${response.status}`);
  assert(response.body && response.body.success && response.body.data && response.body.data.token, "Control Plane login did not return a token.");
  return response.body.data.token;
}

async function findStore(token, key) {
  const response = await fetchJson(`${controlPlaneApiUrl}/api/control-plane/stores`, {
    headers: { authorization: `Bearer ${token}` },
  });

  assert(response.status === 200, `Control Plane stores failed: ${response.status}`);
  const items = response.body && response.body.data && Array.isArray(response.body.data.items)
    ? response.body.data.items
    : [];
  const store = items.find((candidate) => candidate.storeKey === key);
  assert(store, `Store '${key}' was not found in Control Plane registry.`);
  return store;
}

async function getSecurityPrivacy(token, storePublicId) {
  const response = await fetchJson(`${controlPlaneApiUrl}/api/controlplane/commerce/stores/${storePublicId}/security-privacy`, {
    headers: { authorization: `Bearer ${token}` },
  });

  assert(response.status === 200, `Security/privacy load failed: ${response.status}`);
  assert(response.body && response.body.success && response.body.data, "Security/privacy response was empty.");
  return response.body.data;
}

async function updateSecurityPrivacy(token, storePublicId, current, mode) {
  const body = {
    consent: current.consent,
    captcha: {
      enabled: current.captcha.enabled,
      providerSystemName: current.captcha.providerSystemName,
      publicSiteKey: current.captcha.publicSiteKey,
      secretReference: null,
      clearSecret: false,
      minimumScore: current.captcha.minimumScore,
      targets: current.captcha.targets,
    },
    privacy: current.privacy,
    registration: {
      mode,
      registrationAllowed: mode === "standard",
    },
  };
  const response = await fetchJson(`${controlPlaneApiUrl}/api/controlplane/commerce/stores/${storePublicId}/security-privacy`, {
    method: "PUT",
    headers: {
      authorization: `Bearer ${token}`,
      "content-type": "application/json",
    },
    body: JSON.stringify(body),
  });

  assert(response.status >= 200 && response.status < 300, `Security/privacy update failed: ${response.status} ${JSON.stringify(response.body)}`);
  assert(response.body && response.body.success, "Security/privacy update returned failure.");
  return response.body.data;
}

async function getStorefrontRegistrationPolicy() {
  const response = await fetchJson(`${commerceNodeApiUrl}/api/storefront/stores/${storeKey}/auth/registration-policy`);
  assert(response.status === 200, `Storefront registration policy failed: ${response.status}`);
  assert(response.body && response.body.success && response.body.data, "Storefront registration policy response was empty.");
  return response.body.data;
}

async function directRegisterAttempt() {
  return await fetchJson(`${commerceNodeApiUrl}/api/storefront/stores/${storeKey}/auth/register`, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({
      fullName: "Disabled Registration QA",
      email: `disabled-${Date.now()}@example.local`,
      password: "Password123!",
      confirmPassword: "Password123!",
    }),
  });
}

async function fetchJson(url, options) {
  const response = await fetch(url, options);
  const text = await response.text();
  let body = null;
  if (text) {
    try {
      body = JSON.parse(text);
    } catch {
      body = { raw: text };
    }
  }

  return { status: response.status, body };
}

async function expectText(page, text) {
  await page.getByText(text, { exact: false }).first().waitFor({ timeout: 15000 });
}

function createNetworkAudit(page, auditedOrigin) {
  const requests = [];
  const response5xx = [];
  const forbiddenBrowserCalls = [];
  const origin = new URL(auditedOrigin).origin;
  page.on("request", (request) => {
    const url = new URL(request.url());
    if (url.origin !== origin) {
      return;
    }

    requests.push(`${request.method()} ${url.pathname}`);
    if (url.pathname.includes("/api/internal")
      || url.pathname.includes("/api/commerce")
      || url.pathname.includes("/api/control-plane")
      || url.pathname.includes("/api/controlplane")) {
      forbiddenBrowserCalls.push(`${request.method()} ${url.pathname}`);
    }
  });
  page.on("response", (response) => {
    const url = new URL(response.url());
    if (url.origin !== origin) {
      return;
    }

    if (response.status() >= 500) {
      response5xx.push(`${response.status()} ${url.pathname}`);
    }
  });

  return {
    summary() {
      return {
        requestCount: requests.length,
        forbiddenBrowserCalls,
        response5xx,
        sampledRequests: requests.slice(0, 50),
      };
    },
  };
}

function ensureDir(dir) {
  fs.mkdirSync(dir, { recursive: true });
}

function trimEnd(value, suffix) {
  let result = value;
  while (result.endsWith(suffix)) {
    result = result.slice(0, -suffix.length);
  }
  return result;
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
