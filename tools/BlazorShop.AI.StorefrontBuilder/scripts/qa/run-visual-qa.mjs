#!/usr/bin/env node
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { pathToFileURL } from "node:url";
import { chromium } from "@playwright/test";

if (process.argv.includes("--help") || process.argv.includes("-h")) {
  console.log(`Usage: node run-visual-qa.mjs [options]

Options:
  --proof-mode <mode>             Proof mode: skeleton or runtime. Defaults to skeleton when --fixture-root is present, otherwise runtime.
  --base-url <url>                 Running generated storefront base URL.
  --project-root <path>            Generated storefront project root.
  --screenshot-root <path>         Screenshot/evidence output root.
  --operation-id <id>              Visual QA operation ID. Defaults to visual-plan.json operationId or runtime-visual-qa.
  --fixture-root <path>            File-based fixture root for handoff skeleton proof.
  --category-slug <slug>           Category slug for baseline route checks.
  --product-slug <slug>            Product slug for baseline route checks.
  --allow-planned-placeholders     Allow handoff placeholder markers during skeleton proof.
  --help, -h                       Show this help text.`);
  process.exit(0);
}

const baseUrlArg = readArg("--base-url");
const projectRoot = resolve(readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof");
const screenshotRoot = readArg("--screenshot-root") ?? "output/playwright/storefront-builder-visual-qa";
const categorySlug = readArg("--category-slug") ?? "apparel";
const productSlug = readArg("--product-slug") ?? "qa-simple-product-100";
const fixtureRoot = readArg("--fixture-root") ? resolve(readArg("--fixture-root")) : null;
const proofModeArg = readArg("--proof-mode");
const proofMode = normalizeProofMode(proofModeArg, fixtureRoot);
const runtimeSummaryContract = { proofMode: "runtime" };
const baseUrl = baseUrlArg ?? "";
const allowPlannedPlaceholders = hasFlag("--allow-planned-placeholders");
const reportPath = `${projectRoot}/docs/storefront-analysis/visual-qa-report.md`;
const runtimeSummaryPath = `${projectRoot}/docs/storefront-analysis/visual-qa-runtime-summary.json`;
const handoffPlan = readHandoffPlan(projectRoot);
const visualPlan = readVisualPlan(projectRoot);
const operationId = readArg("--operation-id") ?? visualPlan?.operationId ?? "runtime-visual-qa";
const pages = buildPages(handoffPlan);
const baseOrigin = proofMode === "runtime" ? new URL(baseUrl).origin : "";
const startedUtc = new Date().toISOString();

const viewports = [
  ["desktop-1440", 1440, 1000],
  ["mobile-390", 390, 900],
  ["tablet-768", 768, 1000],
];

mkdirSync(screenshotRoot, { recursive: true });
const browser = await chromium.launch();
const discrepancies = [];
const captures = [];
const cssResponses = [];
const cssResponseKeys = new Set();
const browserEvents = [];
const runtimeRequests = [];
const routeStatuses = [];
const assetFindings = [];

try {
  for (const [viewportName, width, height] of viewports) {
    const page = await browser.newPage({ viewport: { width, height } });
    page.on("console", (message) => {
      if (["error", "warning"].includes(message.type())) {
        browserEvents.push({ type: `console.${message.type()}`, viewportName, text: message.text() });
      }
    });
    page.on("pageerror", (error) => {
      browserEvents.push({ type: "pageerror", viewportName, text: error.message });
    });
    page.on("requestfailed", (request) => {
      browserEvents.push({
        type: "requestfailed",
        viewportName,
        url: request.url(),
        text: request.failure()?.errorText ?? "request failed",
      });
    });
    page.on("request", (request) => {
      if (proofMode !== "runtime") {
        return;
      }

      runtimeRequests.push({
        viewportName,
        method: request.method(),
        resourceType: request.resourceType(),
        url: request.url(),
      });
    });

    for (const pageSpec of pages) {
      const { pageName, route, requiredSlots } = pageSpec;
      const url = resolvePageUrl(pageSpec);
      const response = await page.goto(url, { waitUntil: "networkidle" });
      const status = response?.status() ?? 0;
      routeStatuses.push({ pageName, viewportName, route, url, status });
      if (proofMode === "runtime" && (status < 200 || status > 399)) {
        discrepancies.push(critical(pageName, viewportName, "route", `Runtime route returned HTTP status ${status}: ${url}`, "Serve every generated visual QA route from the generated runtime host."));
      }

      const bodyText = await page.locator("body").innerText();
      if (!bodyText.trim()) {
        discrepancies.push(critical(pageName, viewportName, "body", "Hidden primary content or blank body.", "Render visible route content before visual QA."));
      }

      const primaryHeading = await page.locator("h1").count();
      if (primaryHeading === 0) {
        discrepancies.push(critical(pageName, viewportName, "h1", "Missing primary h1 content.", "Render a page-owned primary heading."));
      }

      const cssState = await page.evaluate(async () => {
        const sheets = Array.from(document.styleSheets).map((sheet) => {
          try {
            return { href: sheet.href ?? "inline", ruleCount: sheet.cssRules.length, readable: true };
          } catch {
            return { href: sheet.href ?? "inline", ruleCount: 0, readable: false };
          }
        });

        const bodyStyle = window.getComputedStyle(document.body);
        const linkedStylesheets = Array.from(document.querySelectorAll('link[rel~="stylesheet"][href]'))
          .map((link) => new URL(link.getAttribute("href"), document.baseURI).toString());
        const responses = [];

        for (const href of linkedStylesheets) {
          try {
            const response = await fetch(href, { cache: "no-store", credentials: "same-origin" });
            const body = await response.text();
            responses.push({
              url: href,
              status: response.status,
              contentType: response.headers.get("content-type") ?? "",
              length: body.length,
            });
          } catch {
            responses.push({
              url: href,
              status: 0,
              contentType: "",
              length: -1,
            });
          }
        }

        return {
          bodyFont: bodyStyle.fontFamily,
          bodyBackground: bodyStyle.backgroundColor,
          hasGeneratedCssLink: linkedStylesheets.some((href) => href.includes("storefront-builder.generated.css")),
          sheets,
          responses,
          linkedStylesheets,
        };
      });

      const loadedRuleCount = cssState.sheets.reduce((sum, sheet) => sum + sheet.ruleCount, 0);
      const generatedFileCssApplied = fixtureRoot && cssState.hasGeneratedCssLink && !cssState.bodyFont.toLowerCase().includes("times new roman");
      if (loadedRuleCount === 0 && !generatedFileCssApplied) {
        discrepancies.push(critical(pageName, viewportName, "styleSheets", "No readable stylesheet rules are applied in the browser.", "Ensure generated CSS and package stylesheets are linked and readable."));
      }

      if (handoffPlan && !cssState.hasGeneratedCssLink) {
        discrepancies.push(critical(pageName, viewportName, 'link[href*="storefront-builder.generated.css"]', "Generated handoff CSS is not linked.", "Keep the generated CSS link in ApplicationHead."));
      }

      if (cssState.bodyFont.toLowerCase().includes("times new roman")) {
        discrepancies.push(critical(pageName, viewportName, "body", `Browser default body font is still active: ${cssState.bodyFont}.`, "Apply generated or package typography to the body."));
      }

      for (const response of cssState.responses) {
        const key = `${response.status}|${response.length}|${response.contentType}|${response.url}`;
        if (!cssResponseKeys.has(key)) {
          cssResponseKeys.add(key);
          cssResponses.push(response);
        }
      }

      for (const slotId of requiredSlots) {
        const selector = selectorForSlot(slotId);
        if (!selector) {
          continue;
        }

        const visibleCount = await page.locator(selector).count()
          ? await page.locator(selector).first().isVisible().catch(() => false)
          : false;
        if (!visibleCount) {
          discrepancies.push(critical(pageName, viewportName, selector, `Required handoff slot '${slotId}' is not visible.`, "Render the planned slot marker/visual component for this route."));
        }
      }

      if (requiredSlots.includes("product.purchase")) {
        for (const selector of [
          "[data-storefront-product-purchase]",
          "[data-storefront-product-purchase-submit]",
          "[data-storefront-purchase-quantity]",
          "[data-storefront-command='cart.add-line']",
        ]) {
          const found = await page.locator(selector).count();
          if (found === 0) {
            discrepancies.push(critical(pageName, viewportName, selector, "Product purchase browser-action descriptor is missing.", "Preserve Presentation semantic descriptors in generated visuals."));
          }
        }
      }

      if (requiredSlots.includes("product.gallery")) {
        const galleryBox = await page.locator(selectorForSlot("product.gallery")).first().boundingBox().catch(() => null);
        if (!galleryBox || !isRoughlySquare(galleryBox)) {
          discrepancies.push(critical(pageName, viewportName, selectorForSlot("product.gallery"), "Product gallery does not use a stable square media container.", "Use a square aspect-ratio container for product media."));
        }
      }

      const pageMetrics = await page.evaluate(() => {
        const brokenImages = Array.from(document.images)
          .filter((image) => image.currentSrc && (image.naturalWidth <= 0 || image.naturalHeight <= 0))
          .map((image) => image.currentSrc);
        const assetUrls = [
          ...Array.from(document.images).map((image) => image.currentSrc).filter(Boolean),
          ...Array.from(document.querySelectorAll("script[src]")).map((script) => new URL(script.getAttribute("src"), document.baseURI).toString()),
          ...Array.from(document.querySelectorAll("link[rel~='stylesheet'][href]")).map((link) => new URL(link.getAttribute("href"), document.baseURI).toString()),
        ];
        return {
          scrollWidth: document.documentElement.scrollWidth,
          viewportWidth: window.innerWidth,
          brokenImages,
          assetUrls,
        };
      });
      if (pageMetrics.scrollWidth > pageMetrics.viewportWidth + 2) {
        discrepancies.push(major(pageName, viewportName, "html", `Horizontal overflow detected: scrollWidth=${pageMetrics.scrollWidth}, viewport=${pageMetrics.viewportWidth}.`, "Constrain generated primary regions within the viewport."));
      }

      for (const imageUrl of pageMetrics.brokenImages) {
        discrepancies.push(critical(pageName, viewportName, "img", `Broken generated asset: ${imageUrl}`, "Ensure generated assets resolve from the project wwwroot."));
      }

      if (proofMode === "runtime") {
        for (const assetUrl of pageMetrics.assetUrls) {
          if (isExternalAssetAllowed(assetUrl)) {
            continue;
          }

          let origin = "";
          try {
            origin = new URL(assetUrl).origin;
          } catch {
            origin = "";
          }

          if (origin && origin !== baseOrigin) {
            assetFindings.push(critical(pageName, viewportName, "asset", `Runtime asset is not served by the generated host: ${assetUrl}`, "Serve generated CSS, scripts, and assets through the generated runtime host."));
          }
        }
      }

      const screenshot = join(screenshotRoot, `${pageName}-${viewportName}.png`);
      await page.screenshot({ path: screenshot, fullPage: true });
      captures.push({
        pageName,
        pageId: pageSpec.pageId,
        viewportName,
        viewport: canonicalViewport(viewportName),
        route,
        screenshot: screenshot.replaceAll("\\", "/"),
        capturedUtc: new Date().toISOString(),
      });
    }

    await page.close();
  }
} finally {
  await browser.close();
}

for (const response of cssResponses) {
  if (fixtureRoot && response.url.startsWith("file:")) {
    continue;
  }

  if (response.status < 200 || response.status > 399 || response.length <= 0 || !response.contentType.includes("css")) {
    discrepancies.push(critical("stylesheet", "network", response.url, `Invalid CSS response ${response.status} length=${response.length} contentType=${response.contentType} url=${response.url}`, "Serve generated CSS with a 2xx status and CSS content type."));
  }

  if (proofMode === "runtime") {
    const origin = new URL(response.url).origin;
    if (origin !== baseOrigin) {
      discrepancies.push(critical("stylesheet", "network", response.url, `Stylesheet was not loaded from runtime host: ${response.url}`, "Serve generated CSS from the generated runtime base URL."));
    }
  }
}

for (const event of browserEvents) {
  if (proofMode !== "runtime") {
    continue;
  }

  if (event.type === "pageerror" || event.type === "console.error" || event.type === "requestfailed") {
    discrepancies.push(critical("browser", event.viewportName, event.type, `${event.url ? `${event.url}: ` : ""}${event.text}`, "Resolve runtime browser errors before closure visual QA."));
  }
}

for (const request of runtimeRequests) {
  if (isForbiddenBrowserRequest(request.url)) {
    discrepancies.push(critical("browser", request.viewportName, "network", `Forbidden direct browser request: ${request.method} ${request.url}`, "Generated visuals must use same-origin Presentation/BFF behavior and must not call Commerce Node, Control Plane, Commerce Admin, or legacy APIs directly from the browser."));
  }
}

discrepancies.push(...dedupeFindings(assetFindings));

const placeholderFindings = validateGeneratedPlaceholderText(projectRoot, handoffPlan, allowPlannedPlaceholders);
discrepancies.push(...placeholderFindings);

const criticalCount = discrepancies.filter((item) => item.severity === "Critical").length;
const majorCount = discrepancies.filter((item) => item.severity === "Major").length;
const minorCount = discrepancies.filter((item) => item.severity === "Minor").length;
const passed = criticalCount === 0 && majorCount <= 3;
const finishedUtc = new Date().toISOString();
const report = [
  "# StorefrontBuilder Visual Smoke QA Report",
  "",
  `Proof mode: ${proofMode}`,
  `Base URL: ${baseUrl || "none"}`,
  `Fixture root: ${fixtureRoot ?? "none"}`,
  `Handoff mode: ${handoffPlan ? "true" : "false"}`,
  `Runtime proof: ${proofMode === "runtime" ? "true" : "false"}`,
  ...(proofMode === "skeleton"
    ? [
        "Reference visual diff: not implemented for skeleton proof.",
        "Visual fidelity diff is not a hard gate for skeleton proof.",
      ]
    : [
        "Reference visual review: required in visual-qa-report.json.",
        "Pixel-perfect scoring: deferred; independent reference review remains required.",
      ]),
  `Machine-readable runtime summary: ${runtimeSummaryPath.replaceAll("\\", "/")}`,
  "",
  "## Severity Model",
  "",
  "- Critical: blank body, missing h1, CSS not loaded/applied, unusable mobile, missing major component.",
  "- Major: weak responsive behavior or obvious scaffold layout defect.",
  "- Minor: decorative mismatch, small animation/shadow/icon difference.",
  "",
  "## Summary",
  "",
  `- Critical: ${criticalCount}`,
  `- Major: ${majorCount}`,
  `- Minor: ${minorCount}`,
  "- Major threshold: 3",
  "- Smoke result: " + (passed ? "pass" : "fail"),
  "- Visual fidelity result: " + (proofMode === "runtime" ? "requires independent reference QA report" : "not implemented for skeleton proof"),
  "",
  "## CSS Responses",
  "",
  ...(cssResponses.length === 0 ? ["- None."] : cssResponses.map((response) => `- ${response.status} length=${response.length} contentType=${response.contentType} ${response.url}`)),
  "",
  "## Runtime Route Statuses",
  "",
  ...(routeStatuses.length === 0 ? ["- None."] : routeStatuses.map((routeStatus) => `- ${routeStatus.status} ${routeStatus.pageName} ${routeStatus.viewportName} ${routeStatus.route}: ${routeStatus.url}`)),
  "",
  "## Runtime Network Audit",
  "",
  ...(proofMode !== "runtime" ? ["- Not applicable for skeleton proof."] : runtimeRequests.map((request) => `- ${request.method} ${request.resourceType} viewport=${request.viewportName} ${request.url}`)),
  "",
  "## Browser Event Summary",
  "",
  ...(browserEvents.length === 0 ? ["- None."] : browserEvents.map((event) => `- ${event.type} viewport=${event.viewportName} ${event.url ? `url=${event.url} ` : ""}${event.text}`)),
  "",
  "## Captures",
  "",
  ...captures.map((capture) => `- ${capture.pageName} ${capture.viewportName} ${capture.route}: ${capture.screenshot}`),
  "",
  "## Required Slots",
  "",
  ...pages.flatMap((page) => page.requiredSlots.length === 0 ? [`- ${page.pageName}: none`] : page.requiredSlots.map((slot) => `- ${page.pageName}: ${slot} -> ${selectorForSlot(slot)}`)),
  "",
  "## Discrepancies",
  "",
  ...(discrepancies.length === 0 ? ["- None."] : discrepancies.map((item) => `- ${item.severity}: route=${item.pageName} viewport=${item.viewportName} selector=${item.selector} cause=${item.message} fix=${item.fix}`)),
  "",
].join("\n");

mkdirSync(dirname(reportPath), { recursive: true });
writeFileSync(reportPath, report, "utf8");
writeFileSync(runtimeSummaryPath, JSON.stringify({
  schemaVersion: "0.1.0",
  artifactKind: "storefront-builder.visual-qa-runtime-summary",
  operationId,
  proofMode: proofMode === "runtime" ? runtimeSummaryContract.proofMode : proofMode,
  baseUrl: baseUrl || null,
  fixtureRoot: fixtureRoot ? fixtureRoot.replaceAll("\\", "/") : null,
  screenshotRoot: screenshotRoot.replaceAll("\\", "/"),
  startedUtc,
  finishedUtc,
  handoffMode: Boolean(handoffPlan),
  pages: pages.map((page) => ({
    pageName: page.pageName,
    pageId: page.pageId,
    route: page.route,
    requiredSlots: page.requiredSlots,
  })),
  routeStatuses,
  captures,
  cssResponses,
  browserEvents,
  runtimeNetworkAudit: runtimeRequests,
  discrepancies,
  counts: {
    critical: criticalCount,
    major: majorCount,
    minor: minorCount,
  },
  passed,
}, null, 2), "utf8");
console.log(`Visual QA report written to ${reportPath}`);
console.log(`Visual QA runtime summary written to ${runtimeSummaryPath}`);

if (!passed) {
  process.exitCode = 1;
}

function buildPages(plan) {
  const baseline = [
    pageSpec("shell-home", "/", "home", []),
    pageSpec("catalog", `/category/${categorySlug}`, "category", []),
    pageSpec("product", `/product/${productSlug}`, "product", []),
    pageSpec("cart", "/cart", "cart", []),
    pageSpec("checkout", "/checkout", "checkout", []),
    pageSpec("sign-in", "/signin", "auth", []),
  ];

  if (!plan) {
    return baseline;
  }

  const slotsByPage = new Map();
  for (const slot of plan.slots ?? []) {
    const pageId = slot.pageId ?? pageFromSlot(slot.slotId);
    if (!pageId || !slot.slotId) {
      continue;
    }

    if (!slotsByPage.has(pageId)) {
      slotsByPage.set(pageId, new Set());
    }

    slotsByPage.get(pageId).add(slot.slotId);
  }

  for (const spec of baseline) {
    spec.requiredSlots = [...(slotsByPage.get(spec.pageId) ?? [])].sort((a, b) => a.localeCompare(b, "en"));
  }

  for (const [pageId, slots] of slotsByPage) {
    if (baseline.some((spec) => spec.pageId === pageId)) {
      continue;
    }

    if (pageId === "search") {
      baseline.push(pageSpec("search", "/search", pageId, [...slots]));
    } else if (pageId === "deals") {
      baseline.push(pageSpec("deals", "/deals", pageId, [...slots]));
    } else if (pageId === "new-releases") {
      baseline.push(pageSpec("new-releases", "/new-releases", pageId, [...slots]));
    } else if (["system", "maintenance", "not-found", "error"].includes(pageId)) {
      baseline.push(pageSpec("state-pages", "/not-found", pageId, [...slots]));
    }
  }

  return baseline;
}

function pageSpec(pageName, route, pageId, requiredSlots) {
  return { pageName, route, pageId, requiredSlots };
}

function resolvePageUrl(pageSpec) {
  if (proofMode === "runtime") {
    return new URL(pageSpec.route, baseUrl).toString();
  }

  const fixturePath = join(fixtureRoot, `${pageSpec.pageName}.html`);
  if (!existsSync(fixturePath)) {
    throw new Error(`[SFB-VISUAL-QA-001] Fixture page is missing: ${fixturePath}`);
  }

  return pathToFileURL(fixturePath).toString();
}

function normalizeProofMode(mode, currentFixtureRoot) {
  const normalized = (mode ?? (currentFixtureRoot ? "skeleton" : "runtime")).toLowerCase();
  if (!["skeleton", "runtime"].includes(normalized)) {
    throw new Error(`[SFB-VISUAL-QA-000] Invalid --proof-mode '${mode}'. Expected skeleton or runtime.`);
  }

  if (normalized === "runtime") {
    if (!baseUrlArg) {
      throw new Error("[SFB-VISUAL-QA-002] Runtime proof requires --base-url.");
    }

    if (currentFixtureRoot) {
      throw new Error("[SFB-VISUAL-QA-003] Runtime proof must not use --fixture-root. Start the generated storefront and pass --base-url instead.");
    }
  }

  if (normalized === "skeleton" && !currentFixtureRoot) {
    throw new Error("[SFB-VISUAL-QA-004] Skeleton proof requires --fixture-root.");
  }

  return normalized;
}

function isForbiddenBrowserRequest(url) {
  let parsed;
  try {
    parsed = new URL(url);
  } catch {
    return false;
  }

  const path = parsed.pathname.toLowerCase();
  return path.includes("/api/storefront/stores/")
    || path.startsWith("/api/commerce/")
    || path.startsWith("/api/control-plane/")
    || path.startsWith("/api/admin/")
    || path.startsWith("/api/public/")
    || path.startsWith("/api/internal/");
}

function isExternalAssetAllowed(url) {
  return url.startsWith("data:")
    || url.startsWith("blob:")
    || url.startsWith("about:")
    || url.startsWith("file:");
}

function dedupeFindings(findings) {
  const seen = new Set();
  const result = [];
  for (const finding of findings) {
    const key = `${finding.severity}|${finding.pageName}|${finding.viewportName}|${finding.selector}|${finding.message}`;
    if (seen.has(key)) {
      continue;
    }

    seen.add(key);
    result.push(finding);
  }

  return result;
}

function readHandoffPlan(root) {
  const planPath = join(root, "docs", "storefront-analysis", "generation-plan.json");
  if (!existsSync(planPath)) {
    return null;
  }

  return JSON.parse(readFileSync(planPath, "utf8"));
}

function readVisualPlan(root) {
  const planPath = join(root, "docs", "storefront-analysis", "visual-plan.json");
  if (!existsSync(planPath)) {
    return null;
  }

  return JSON.parse(readFileSync(planPath, "utf8"));
}

function canonicalViewport(viewportName) {
  if (viewportName.startsWith("desktop")) {
    return "desktop";
  }

  if (viewportName.startsWith("tablet")) {
    return "tablet";
  }

  if (viewportName.startsWith("mobile")) {
    return "mobile";
  }

  return viewportName;
}

function pageFromSlot(slotId) {
  if (!slotId) {
    return "";
  }

  return slotId.split(".")[0];
}

function selectorForSlot(slotId) {
  return {
    "layout.header": ".sfb-shell-header",
    "layout.footer": "footer",
    "layout.main-navigation": ".sfb-main-nav",
    "layout.mobile-navigation": ".sfb-mobile-nav",
    "layout.cart-badge": ".sfb-cart-badge, [data-storefront-cart-badge]",
    "home.sections": ".sfb-hero",
    "catalog.product-card": ".sfb-product-card",
    "catalog.filters": ".sfb-catalog-toolbar",
    "product.gallery": ".sfb-product-gallery, [data-storefront-product-gallery]",
    "product.information": ".sfb-product-page",
    "product.purchase": ".sfb-product-purchase, [data-storefront-product-purchase]",
    "cart.page": ".sfb-fallback-page, [data-storefront-cart-page]",
    "checkout.page": ".sfb-fallback-page, [data-storefront-checkout-shell]",
    "account.shell": ".sfb-fallback-page, [data-storefront-account-app]",
    "system.error": ".sfb-fallback-page, [data-storefront-state-page]",
  }[slotId] ?? `[data-storefront-slot~="${slotId}"]`;
}

function isRoughlySquare(box) {
  if (box.width <= 0 || box.height <= 0) {
    return false;
  }

  const ratio = box.width / box.height;
  return ratio >= 0.75 && ratio <= 1.33;
}

function validateGeneratedPlaceholderText(root, plan, allowPlaceholders) {
  if (!plan || allowPlaceholders) {
    return [];
  }

  const findings = [];
  for (const file of plan.files ?? []) {
    const targetPath = String(file.targetPath ?? "").replaceAll("\\", "/").replace(/^\/+/, "");
    if (!targetPath || !["generated", "managed"].includes(file.ownership)) {
      continue;
    }

    if (!/\.(razor|css|js|mjs|ts)$/i.test(targetPath)) {
      continue;
    }

    const fullPath = join(root, targetPath);
    if (!existsSync(fullPath)) {
      continue;
    }

    const content = readFileSync(fullPath, "utf8");
    for (const marker of ["storefront-builder-handoff-placeholder", "data-storefront-generated-placeholder"]) {
      if (content.includes(marker)) {
        findings.push(major("source", "static", targetPath, `Generated-owned visual file still contains placeholder marker '${marker}'.`, "Replace planned placeholders during agent visual generation or pass --allow-planned-placeholders for skeleton proof only."));
      }
    }
  }

  return findings;
}

function critical(pageName, viewportName, selector, message, fix) {
  return { severity: "Critical", pageName, viewportName, selector, message, fix };
}

function major(pageName, viewportName, selector, message, fix) {
  return { severity: "Major", pageName, viewportName, selector, message, fix };
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}

function hasFlag(name) {
  return process.argv.includes(name);
}
