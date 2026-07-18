const fs = require("fs");
const path = require("path");

const playwrightPath = path.resolve(__dirname, "../../.gstack/playwright-qa/node_modules/playwright");
const { chromium } = require(playwrightPath);

const baseUrl = trimEnd(process.env.STOREFRONT_QA_BASE_URL || "http://localhost:18598", "/");
const mailpitApiUrl = trimEnd(process.env.MAILPIT_API_URL || "http://localhost:8025/api/v1", "/");
const knownEmail = process.env.STOREFRONT_QA_RECOVERY_EMAIL || "qa.customer@example.local";
const resetPassword = process.env.STOREFRONT_QA_RECOVERY_PASSWORD || "QaCustomer123!";
const unknownEmail = process.env.STOREFRONT_QA_UNKNOWN_EMAIL || `missing-${Date.now()}@example.local`;
const expectedFrom = process.env.STOREFRONT_QA_EXPECTED_FROM || "default-sender@example.local";
const headed = (process.env.HEADLESS || "false").toLowerCase() !== "true";
const artifactRoot = path.resolve(process.env.STOREFRONT_QA_ARTIFACT_DIR || ".gstack/qa-reports/email-recovery-e2e");

async function main() {
  ensureDir(artifactRoot);
  const evidence = {
    generatedAt: new Date().toISOString(),
    baseUrl,
    mailpitApiUrl,
    knownEmail,
    unknownEmail,
    expectedFrom,
    headed,
    steps: [],
  };

  await clearMailpit();
  evidence.steps.push({ step: "mailpit.clear.before-known", ok: true });

  const browser = await chromium.launch({ headless: !headed });
  const context = await browser.newContext({ viewport: { width: 1366, height: 768 } });
  const page = await context.newPage();
  const network = createNetworkAudit(page);

  try {
    await requestRecovery(page, knownEmail);
    evidence.steps.push({ step: "known.submit", ok: true, url: page.url() });

    const message = await waitForMail(
      (candidate) => messageMatches(candidate, knownEmail, "customer.password_recovery"),
      30000);
    const detail = await readMessageDetail(message);
    const resetLink = extractResetLink(detail) || extractResetLink(message);
    if (!resetLink) {
      throw new Error("Recovery message did not contain a reset-password link.");
    }

    const from = stringifyAddress(message.From || detail.From || message.from || detail.from);
    if (!from.toLowerCase().includes(expectedFrom.toLowerCase())) {
      throw new Error(`Expected recovery sender ${expectedFrom}, got ${from || "(empty)"}.`);
    }

    assertTokenRedaction(detail, resetLink);
    evidence.steps.push({
      step: "known.mailpit.message",
      ok: true,
      subject: message.Subject || detail.Subject || message.subject || detail.subject,
      from,
      to: stringifyAddress(message.To || detail.To || message.to || detail.to),
      resetLink: redactResetLink(resetLink),
    });

    await resetPasswordThroughBrowser(page, resolveResetLink(resetLink), resetPassword);
    evidence.steps.push({ step: "known.reset-browser", ok: true, url: page.url() });

    await signIn(page, knownEmail, resetPassword);
    evidence.steps.push({ step: "known.login-new-password", ok: true, url: page.url() });

    await clearMailpit();
    await requestRecovery(page, unknownEmail);
    evidence.steps.push({ step: "unknown.submit", ok: true, url: page.url() });
    const unknownMessage = await waitForMail(
      (candidate) => messageMatches(candidate, unknownEmail),
      5000,
      { allowTimeout: true });
    if (unknownMessage) {
      throw new Error(`Unknown email produced a Mailpit message: ${unknownEmail}.`);
    }

    evidence.steps.push({ step: "unknown.no-mailpit-message", ok: true });
    assertNoUnexpectedNetwork(network);
    evidence.steps.push({ step: "network.guardrails", ok: true, summary: network.summary() });
    evidence.result = "passed";
  } catch (error) {
    evidence.result = "failed";
    evidence.error = error instanceof Error ? error.message : String(error);
    await page.screenshot({ path: path.join(artifactRoot, "failure.png"), fullPage: true }).catch(() => {});
    throw error;
  } finally {
    await browser.close();
    fs.writeFileSync(path.join(artifactRoot, "result.json"), JSON.stringify(redactEvidence(evidence), null, 2));
  }
}

async function requestRecovery(page, email) {
  await page.goto(`${baseUrl}/forgot-password`, { waitUntil: "domcontentloaded" });
  await page.getByLabel("Email address").fill(email);
  await page.getByRole("button", { name: /send reset instructions/i }).click();
  await page.waitForURL(/\/forgot-password.*sent=1/, { timeout: 20000 });
  await page.getByRole("status").waitFor({ state: "visible", timeout: 10000 });
}

async function resetPasswordThroughBrowser(page, resetLink, password) {
  await page.goto(resetLink, { waitUntil: "domcontentloaded" });
  await page.getByLabel("New password", { exact: true }).fill(password);
  await page.getByLabel("Confirm new password", { exact: true }).fill(password);
  await page.getByRole("button", { name: /^reset password$/i }).click();
  await page.waitForURL(/\/signin.*passwordReset=1/, { timeout: 20000 });
}

async function signIn(page, email, password) {
  await page.goto(`${baseUrl}/signin?returnUrl=${encodeURIComponent("/account/profile")}`, { waitUntil: "domcontentloaded" });
  await page.getByLabel("Email address").fill(email);
  await page.getByLabel("Password", { exact: true }).fill(password);
  await page.getByRole("button", { name: /^sign in$/i }).click();
  await page.waitForURL(/\/account\/profile/, { timeout: 20000 });
}

async function clearMailpit() {
  await fetchJson(`${mailpitApiUrl}/messages`, { method: "DELETE" });
}

async function waitForMail(predicate, timeoutMs, options = {}) {
  const startedAt = Date.now();
  let lastMessages = [];
  while (Date.now() - startedAt < timeoutMs) {
    const payload = await fetchJson(`${mailpitApiUrl}/messages`);
    lastMessages = payload.messages || payload.Messages || [];
    const match = lastMessages.find(predicate);
    if (match) {
      return match;
    }

    await delay(500);
  }

  if (options.allowTimeout) {
    return null;
  }

  throw new Error(`Timed out waiting for Mailpit message. Last count: ${lastMessages.length}.`);
}

async function readMessageDetail(message) {
  const id = message.ID || message.Id || message.id;
  if (!id) {
    return message;
  }

  return await fetchJson(`${mailpitApiUrl}/message/${encodeURIComponent(id)}`);
}

async function fetchJson(url, options = {}) {
  const response = await fetch(url, options);
  if (!response.ok) {
    throw new Error(`${options.method || "GET"} ${url} returned ${response.status}.`);
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

function messageMatches(message, email, templateName) {
  const text = JSON.stringify(message).toLowerCase();
  return text.includes(email.toLowerCase())
    && (!templateName || text.includes(templateName.toLowerCase()) || text.includes("password"));
}

function extractResetLink(payload) {
  const decoded = decodeHtmlEntities(JSON.stringify(payload));
  const absolute = decoded.match(/https?:\/\/[^"'\\\s<>]+\/reset-password\?[^"'\\\s<>]+/i);
  if (absolute) {
    return absolute[0];
  }

  const relative = decoded.match(/\/reset-password\?[^"'\\\s<>]+/i);
  return relative ? relative[0] : null;
}

function resolveResetLink(link) {
  if (/^https?:\/\//i.test(link)) {
    const url = new URL(link);
    return `${baseUrl}${url.pathname}${url.search}`;
  }

  return `${baseUrl}${link.startsWith("/") ? "" : "/"}${link}`;
}

function assertTokenRedaction(detail, resetLink) {
  const token = new URL(resolveResetLink(resetLink)).searchParams.get("token");
  if (!token) {
    throw new Error("Reset link does not include token query parameter.");
  }

  const decodedPayload = decodeURIComponentSafe(decodeHtmlEntities(JSON.stringify(detail)));
  const tokenOccurrences = countOccurrences(decodedPayload, token);
  if (tokenOccurrences > 2) {
    throw new Error("Reset token appears outside the expected reset-link payload.");
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

  if (unexpected5xx.length > 0) {
    throw new Error(`Unexpected 5xx responses: ${JSON.stringify(unexpected5xx)}`);
  }

  if (forbiddenCalls.length > 0) {
    throw new Error(`Storefront browser made forbidden admin/control calls: ${JSON.stringify(forbiddenCalls)}`);
  }
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
      };
    },
  };
}

function redactResetLink(link) {
  const resolved = new URL(resolveResetLink(link));
  if (resolved.searchParams.has("token")) {
    resolved.searchParams.set("token", "[redacted]");
  }

  return `${resolved.pathname}${resolved.search}`;
}

function redactEvidence(value) {
  return JSON.parse(JSON.stringify(value).replace(/token=([^"&\s]+)/gi, "token=[redacted]"));
}

function stringifyAddress(value) {
  if (!value) {
    return "";
  }

  if (typeof value === "string") {
    return value;
  }

  return JSON.stringify(value);
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

function decodeURIComponentSafe(value) {
  try {
    return decodeURIComponent(value);
  } catch {
    return value;
  }
}

function countOccurrences(haystack, needle) {
  if (!needle) {
    return 0;
  }

  let count = 0;
  let index = 0;
  while ((index = haystack.indexOf(needle, index)) !== -1) {
    count += 1;
    index += needle.length;
  }

  return count;
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
