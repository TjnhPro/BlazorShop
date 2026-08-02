#!/usr/bin/env node
import { createHash } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, isAbsolute, join, resolve } from "node:path";

const projectRoot = resolve(readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof");
const taskPackageRoot = resolve(readArg("--task-package") ?? join(projectRoot, "docs/storefront-analysis/agent-task-package"));
const writtenFiles = readListArg("--written-files").map(normalizeTargetPath);

if (writtenFiles.length === 0) {
  fail("SFB-AGENT-WRITE-000", "At least one written file must be supplied through --written-files.");
}

const manifestPath = join(taskPackageRoot, "manifest.json");
if (!existsSync(manifestPath)) {
  fail("SFB-AGENT-WRITE-001", `Agent task package manifest is missing: ${manifestPath}`);
}

const packageManifest = readJson(manifestPath);
const allowedByPath = new Map((packageManifest.allowedOutputFiles ?? []).map(file => [normalizeTargetPath(file.targetPath), file]));
const records = [];

for (const targetPath of writtenFiles) {
  assertAllowedPath(targetPath, allowedByPath);
  const fullPath = resolve(projectRoot, targetPath);
  assertUnderProject(fullPath, targetPath);
  if (!existsSync(fullPath)) {
    fail("SFB-AGENT-WRITE-002", `Agent-written file does not exist: ${targetPath}`);
  }

  const allowed = allowedByPath.get(targetPath);
  const content = readFileSync(fullPath, "utf8");
  validateVisualContent(targetPath, content, allowed);
  records.push({
    filePath: targetPath,
    sourcePlanEntryId: allowed.planEntryId,
    checksum: `sha256:${sha(normalizeText(content))}`,
    slots: allowed.slots ?? [],
    ownership: allowed.ownership,
    visualShellOnly: allowed.visualShellOnly === true,
  });
}

const output = join(projectRoot, "docs/storefront-analysis/agent-written-files.json");
const document = {
  schemaVersion: "1.0.0",
  artifactKind: "agent-written-files",
  artifactId: `agent-written-files.${packageManifest.projectName}`,
  generationPlanHash: packageManifest.generationPlanHash,
  files: records.sort((a, b) => a.filePath.localeCompare(b.filePath, "en")),
};

mkdirSync(dirname(output), { recursive: true });
writeFileSync(output, `${JSON.stringify(stableObject(document), null, 2)}\n`, "utf8");
appendAgentManifestSection(projectRoot, document.files);
console.log(`StorefrontBuilder recorded ${records.length} agent-written file(s).`);

function assertAllowedPath(targetPath, allowedByPath) {
  if (isProtectedPackagePath(targetPath)) {
    fail("SFB-AGENT-WRITE-003", `Agent write targets a protected package or Starter zone: ${targetPath}`);
  }

  if (!allowedByPath.has(targetPath)) {
    fail("SFB-AGENT-WRITE-004", `Agent write is outside allowed generated-owned outputs: ${targetPath}`);
  }

  if (targetPath === "appsettings.json" || targetPath.endsWith(".csproj") || targetPath === "Program.cs") {
    fail("SFB-AGENT-WRITE-005", `Agent write targets project/server configuration: ${targetPath}`);
  }
}

function validateVisualContent(targetPath, content, allowed) {
  if (/^\s*@page\s+/m.test(content)) {
    fail("SFB-AGENT-WRITE-010", `Agent visual file must not declare routes: ${targetPath}`);
  }

  for (const token of [
    "HttpClient",
    "IHttpClientFactory",
    "fetch(",
    "XMLHttpRequest",
    "/api/storefront/stores/",
    "CommerceNodeBaseUrl",
    "MapGet(",
    "MapPost(",
    "MapPut(",
    "MapDelete(",
    "MapGroup(",
    "AddHttpClient",
    "AddStorefrontRuntime",
    "AddStorefrontPlatformRuntime",
    "StorefrontRuntimeOptions",
  ]) {
    if (content.includes(token)) {
      fail("SFB-AGENT-WRITE-011", `Agent visual file contains forbidden transport/server token '${token}': ${targetPath}`);
    }
  }

  for (const token of [
    "class Storefront",
    "record Storefront",
    "PlaceOrder",
    "CapturePayment",
    "ValidateCheckout",
    "ValidateCart",
    "ExpectedCartVersion",
    "ExpectedCheckoutVersion",
    "accessToken",
    "refreshToken",
    "rel=\"canonical\"",
  ]) {
    if (content.includes(token)) {
      fail("SFB-AGENT-WRITE-012", `Agent visual file contains forbidden business/auth/SEO token '${token}': ${targetPath}`);
    }
  }

  if ((allowed.slots ?? []).includes("product.purchase")) {
    for (const descriptor of ["data-storefront-product-purchase", "data-storefront-command=\"cart.add-line\""]) {
      if (!content.includes(descriptor)) {
        fail("SFB-AGENT-WRITE-013", `Product purchase visuals must preserve Presentation descriptor '${descriptor}': ${targetPath}`);
      }
    }
  }
}

function appendAgentManifestSection(projectRoot, records) {
  const manifestPath = join(projectRoot, "docs/storefront-analysis/generated-files.yaml");
  const existing = existsSync(manifestPath) ? readFileSync(manifestPath, "utf8").replace(/\r\n/g, "\n").replace(/\r/g, "\n") : "";
  const base = existing.replace(/\nagentWrittenFiles:\n[\s\S]*$/m, "").trimEnd();
  const section = [
    "",
    "agentWrittenFiles:",
    ...records.flatMap(record => [
      `  - filePath: ${record.filePath}`,
      `    sourcePlanEntryId: ${record.sourcePlanEntryId}`,
      `    checksum: ${record.checksum}`,
      `    ownership: ${record.ownership}`,
    ]),
    "",
  ].join("\n");
  mkdirSync(dirname(manifestPath), { recursive: true });
  writeFileSync(manifestPath, `${base}${section}`, "utf8");
}

function normalizeTargetPath(path) {
  const normalized = String(path ?? "").replaceAll("\\", "/").replace(/^\/+/, "");
  if (!normalized || isAbsolute(normalized) || normalized.includes(":") || normalized.startsWith("../") || normalized.includes("/../")) {
    fail("SFB-AGENT-WRITE-006", `Unsafe agent write path: ${path}`);
  }

  return normalized;
}

function isProtectedPackagePath(path) {
  return /(^|\/)(BlazorShop\.Storefront\.Starter|BlazorShop\.Storefront\.Presentation|BlazorShop\.Storefront\.Runtime|BlazorShop\.Storefront\.Client|BlazorShop\.Storefront\.Browser|BlazorShop\.Storefront\.Components)(\/|$)/.test(path);
}

function assertUnderProject(fullPath, targetPath) {
  const root = resolve(projectRoot);
  if (fullPath !== root && !fullPath.startsWith(`${root}\\`) && !fullPath.startsWith(`${root}/`)) {
    fail("SFB-AGENT-WRITE-006", `Agent write escapes generated project: ${targetPath}`);
  }
}

function readJson(path) {
  return JSON.parse(readFileSync(path, "utf8"));
}

function stableObject(value) {
  if (Array.isArray(value)) {
    return value.map(stableObject);
  }

  if (value && typeof value === "object") {
    return Object.fromEntries(Object.keys(value).sort().map(key => [key, stableObject(value[key])]));
  }

  return value;
}

function normalizeText(value) {
  return value.replace(/\r\n/g, "\n").replace(/\r/g, "\n");
}

function sha(value) {
  return createHash("sha256").update(value).digest("hex");
}

function fail(code, message) {
  throw new Error(`[${code}] ${message}`);
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}

function readListArg(name) {
  const value = readArg(name);
  if (!value) {
    return [];
  }

  return value.split(",").map(item => item.trim()).filter(Boolean);
}
