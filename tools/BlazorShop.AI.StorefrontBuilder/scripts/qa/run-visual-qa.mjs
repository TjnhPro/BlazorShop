#!/usr/bin/env node
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { chromium } from "@playwright/test";

const baseUrl = readArg("--base-url") ?? "http://127.0.0.1:18991";
const projectRoot = readArg("--project-root") ?? "BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo";
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

try {
  for (const [viewportName, width, height] of viewports) {
    const page = await browser.newPage({ viewport: { width, height } });
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

      const screenshot = join(screenshotRoot, `${pageName}-${viewportName}.png`);
      await page.screenshot({ path: screenshot, fullPage: true });
      captures.push({ pageName, viewportName, route, screenshot: screenshot.replaceAll("\\", "/") });
    }

    await page.close();
  }
} finally {
  await browser.close();
}

const criticalCount = discrepancies.filter((item) => item.severity === "Critical").length;
const majorCount = discrepancies.filter((item) => item.severity === "Major").length;
const minorCount = discrepancies.filter((item) => item.severity === "Minor").length;
const report = [
  "# StorefrontBuilder Visual QA Report",
  "",
  `Base URL: ${baseUrl}`,
  "",
  "## Severity Model",
  "",
  "- Critical: wrong section order, broken layout, hidden primary content, unusable mobile, missing major component.",
  "- Major: strong typography/spacing/color mismatch, incorrect product card layout, weak responsive behavior.",
  "- Minor: decorative mismatch, small animation/shadow/icon difference.",
  "",
  "## Summary",
  "",
  `- Critical: ${criticalCount}`,
  `- Major: ${majorCount}`,
  `- Minor: ${minorCount}`,
  "- Major threshold: 3",
  "- MVP result: " + (criticalCount === 0 && majorCount <= 3 ? "pass" : "fail"),
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
