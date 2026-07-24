#!/usr/bin/env node
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { chromium } from "@playwright/test";

const baseUrl = readArg("--base-url") ?? "http://127.0.0.1:18991";
const projectRoot = readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof";
const screenshotRoot = readArg("--screenshot-root") ?? "output/playwright/storefront-builder-visual-qa";
const reportPath = `${projectRoot}/docs/storefront-analysis/visual-qa-report.md`;

const pages = [
  ["shell-home", "/"],
  ["catalog", "/category/sample-category"],
  ["product", "/product/sample-product"],
  ["cart", "/cart"],
  ["checkout", "/checkout"],
  ["account", "/account"],
];

const viewports = [
  ["desktop-1440", 1440, 1000],
  ["tablet-768", 768, 1000],
  ["mobile-390", 390, 900],
];

mkdirSync(screenshotRoot, { recursive: true });
const browser = await chromium.launch();
const discrepancies = [];
const captures = [];
const cssResponses = [];

try {
  for (const [viewportName, width, height] of viewports) {
    const page = await browser.newPage({ viewport: { width, height } });
    page.on("response", async (response) => {
      const url = response.url();
      if (!url.includes(".css")) {
        return;
      }

      try {
        const body = await response.text();
        cssResponses.push({
          url,
          status: response.status(),
          contentType: response.headers()["content-type"] ?? "",
          length: body.length,
        });
      } catch {
        cssResponses.push({
          url,
          status: response.status(),
          contentType: response.headers()["content-type"] ?? "",
          length: -1,
        });
      }
    });

    for (const [pageName, route] of pages) {
      const url = new URL(route, baseUrl).toString();
      await page.goto(url, { waitUntil: "networkidle" });
      const bodyText = await page.locator("body").innerText();
      if (!bodyText.trim()) {
        discrepancies.push(critical(pageName, viewportName, "Hidden primary content or blank body."));
      }

      const primaryHeading = await page.locator("h1").count();
      if (primaryHeading === 0) {
        discrepancies.push(critical(pageName, viewportName, "Missing primary h1 content."));
      }

      const cssState = await page.evaluate(() => {
        const sheets = Array.from(document.styleSheets).map((sheet) => {
          try {
            return { href: sheet.href ?? "inline", ruleCount: sheet.cssRules.length, readable: true };
          } catch {
            return { href: sheet.href ?? "inline", ruleCount: 0, readable: false };
          }
        });

        const bodyStyle = window.getComputedStyle(document.body);
        return {
          bodyFont: bodyStyle.fontFamily,
          bodyBackground: bodyStyle.backgroundColor,
          sheets,
        };
      });

      const loadedRuleCount = cssState.sheets.reduce((sum, sheet) => sum + sheet.ruleCount, 0);
      if (loadedRuleCount === 0) {
        discrepancies.push(critical(pageName, viewportName, "No readable stylesheet rules are applied in the browser."));
      }

      if (cssState.bodyFont.toLowerCase().includes("times new roman")) {
        discrepancies.push(critical(pageName, viewportName, `Browser default body font is still active: ${cssState.bodyFont}.`));
      }

      const screenshot = join(screenshotRoot, `${pageName}-${viewportName}.png`);
      await page.screenshot({ path: screenshot, fullPage: true });
      captures.push({ pageName, viewportName, route, screenshot: screenshot.replaceAll("\\", "/") });
    }

    await page.close();
  }
} finally {
  await browser.close();
}

for (const response of cssResponses) {
  if (response.status < 200 || response.status > 399 || response.length <= 0 || !response.contentType.includes("css")) {
    discrepancies.push(critical("stylesheet", "network", `Invalid CSS response ${response.status} length=${response.length} contentType=${response.contentType} url=${response.url}`));
  }
}

const criticalCount = discrepancies.filter((item) => item.severity === "Critical").length;
const majorCount = discrepancies.filter((item) => item.severity === "Major").length;
const minorCount = discrepancies.filter((item) => item.severity === "Minor").length;
const report = [
  "# StorefrontBuilder Visual Smoke QA Report",
  "",
  `Base URL: ${baseUrl}`,
  "Reference visual diff: not implemented",
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
  "- Smoke result: " + (criticalCount === 0 && majorCount <= 3 ? "pass" : "fail"),
  "- Visual fidelity result: not implemented",
  "",
  "## CSS Responses",
  "",
  ...(cssResponses.length === 0 ? ["- None."] : cssResponses.map((response) => `- ${response.status} length=${response.length} contentType=${response.contentType} ${response.url}`)),
  "",
  "## Captures",
  "",
  ...captures.map((capture) => `- ${capture.pageName} ${capture.viewportName} ${capture.route}: ${capture.screenshot}`),
  "",
  "## Discrepancies",
  "",
  ...(discrepancies.length === 0 ? ["- None."] : discrepancies.map((item) => `- ${item.severity}: ${item.pageName} ${item.viewportName} - ${item.message}`)),
  "",
].join("\n");

mkdirSync(dirname(reportPath), { recursive: true });
writeFileSync(reportPath, report, "utf8");
console.log(`Visual QA report written to ${reportPath}`);

if (criticalCount > 0 || majorCount > 3) {
  process.exitCode = 1;
}

function critical(pageName, viewportName, message) {
  return { severity: "Critical", pageName, viewportName, message };
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}
