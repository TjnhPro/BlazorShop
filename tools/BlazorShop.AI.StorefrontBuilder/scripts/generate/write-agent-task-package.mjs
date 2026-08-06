#!/usr/bin/env node
import { copyFileSync, existsSync, mkdirSync, readFileSync, readdirSync, rmSync, writeFileSync } from "node:fs";
import { createHash } from "node:crypto";
import { dirname, join, resolve } from "node:path";

const HANDOFF_ROOT = "analysis/agent-handoff";
const FORBIDDEN_SOURCE_ONLY_PREFIXES = [
  "captures/",
  "analysis/pages/",
  "analysis/resolved/",
  "presentation-catalog/",
  "review/",
  "reports/",
];
const FORBIDDEN_PACKAGE_TEXT_MARKERS = [
  "BlazorShop.Storefront.V2",
  "BlazorShop.CommerceNode.API",
  "BlazorShop.ControlPlane.API",
];

if (process.argv.includes("--help") || process.argv.includes("-h")) {
  console.log(`Usage: node write-agent-task-package.mjs --project-root <generated-project-root> [options]

Options:
  --project-root <path>  Generated storefront project root.
  --handoff-root <path>  Portable handoff package root or analysis/agent-handoff folder.
  --plan-json <path>     Generation plan JSON path. Defaults under project docs/storefront-analysis.
  --output <path>        Agent task package output folder. Defaults under project docs/storefront-analysis.
  --help, -h             Show this help text.`);
  process.exit(0);
}

const projectRoot = resolve(readArg("--project-root") ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof");
const handoffRoot = readArg("--handoff-root");
const planPath = resolve(readArg("--plan-json") ?? join(projectRoot, "docs/storefront-analysis/generation-plan.json"));
const outputRoot = resolve(readArg("--output") ?? join(projectRoot, "docs/storefront-analysis/agent-task-package"));

if (!existsSync(planPath)) {
  fail("SFB-AGENT-PACKAGE-001", `Generation plan is missing: ${planPath}`);
}

const packageRoot = resolveHandoffPackageRoot(handoffRoot);
const plan = readJson(planPath);
const planText = readFileSync(planPath, "utf8");
const planHash = `sha256:${sha(normalizeText(planText))}`;
const artifacts = {
  allowedFiles: readHandoffJson(packageRoot, "allowed-files.json"),
  protectedFiles: readHandoffJson(packageRoot, "protected-files.json"),
  designTokens: readHandoffJson(packageRoot, "design-tokens.json"),
  visualStyle: readHandoffJson(packageRoot, "visual-style.json"),
  storefrontPattern: readHandoffJson(packageRoot, "storefront-pattern.json"),
  presentationMappings: readHandoffJson(packageRoot, "presentation-mappings.json"),
  originalityRestrictions: readHandoffJson(packageRoot, "originality-restrictions.json"),
};

rmSync(outputRoot, { recursive: true, force: true });
mkdirSync(join(outputRoot, "inputs"), { recursive: true });
mkdirSync(join(outputRoot, "evidence"), { recursive: true });

const allowedOutputs = buildAllowedOutputs(plan);
const evidenceReferences = collectEvidenceReferences(plan);
const copiedEvidence = copyApprovedEvidence(packageRoot, evidenceReferences, outputRoot);
const warnings = evidenceReferences
  .filter(reference => !copiedEvidence.some(item => item.handoffPath === reference))
  .map(reference => ({ code: "evidence-not-copied", handoffPath: reference, message: "Evidence path was not an approved screenshot/crop or was missing from the portable package." }));
const handoffHash = normalizeHash(plan.sourceHandoffPackageHash ?? artifacts.storefrontPattern.sourceHandoffPackageHash ?? artifacts.storefrontPattern.handoffHash);
const protectedFiles = listProtectedFiles(artifacts.protectedFiles);

writeJson(join(outputRoot, "inputs", "generation-plan.json"), plan);
writeJson(join(outputRoot, "inputs", "handoff-evidence-references.json"), {
  schemaVersion: "1.0.0",
  artifactKind: "handoff-evidence-references",
  references: evidenceReferences.map(reference => ({ handoffPath: reference })),
});
writeJson(join(outputRoot, "inputs", "design-token-style-summary.json"), {
  schemaVersion: "1.0.0",
  artifactKind: "design-token-style-summary",
  designTokens: artifacts.designTokens,
  visualStyle: artifacts.visualStyle,
});
writeJson(join(outputRoot, "inputs", "slot-contract-summary.json"), {
  schemaVersion: "1.0.0",
  artifactKind: "slot-contract-summary",
  slots: artifacts.storefrontPattern.slots ?? [],
  pageContracts: artifacts.storefrontPattern.pageContracts ?? [],
  mappings: artifacts.presentationMappings.mappings ?? artifacts.presentationMappings.components ?? [],
});
writeJson(join(outputRoot, "inputs", "file-boundary-manifest.json"), {
  schemaVersion: "1.0.0",
  artifactKind: "agent-file-boundary-manifest",
  allowedFiles: artifacts.allowedFiles,
  protectedFileManifestHash: `sha256:${sha(stableJson(artifacts.protectedFiles))}`,
  protectedFileCount: protectedFiles.length,
  protectedFiles,
  allowedOutputFiles: allowedOutputs,
});
writeJson(join(outputRoot, "inputs", "originality-restrictions.json"), artifacts.originalityRestrictions);
writeFileSync(join(outputRoot, "instructions.md"), buildInstructions(plan, allowedOutputs), "utf8");

const manifest = stableObject({
  schemaVersion: "1.0.0",
  artifactKind: "agent-visual-task-package",
  artifactId: `agent-visual-task-package.${plan.projectName}`,
  projectName: plan.projectName,
  storeKey: plan.storeKey,
  handoffHash,
  sourceHandoffPackageHash: handoffHash,
  generationPlanHash: planHash,
  inputs: [
    "inputs/generation-plan.json",
    "inputs/handoff-evidence-references.json",
    "inputs/design-token-style-summary.json",
    "inputs/slot-contract-summary.json",
    "inputs/file-boundary-manifest.json",
    "inputs/originality-restrictions.json",
    "instructions.md",
  ],
  copiedEvidence,
  allowedOutputFiles: allowedOutputs,
  protectedFiles,
  forbiddenOutputs: [
    "route declarations",
    "BFF endpoints",
    "HTTP clients",
    "DTOs",
    "commerce business commands",
    "authentication flow logic",
    "SEO route or canonical logic",
    "server configuration",
    "appsettings secrets",
    "Storefront Runtime registration",
  ],
  warnings,
});

writeJson(join(outputRoot, "manifest.json"), manifest);
validateTaskPackage(outputRoot);
console.log(`StorefrontBuilder agent task package written to ${outputRoot}`);

function buildAllowedOutputs(plan) {
  return (plan.files ?? [])
    .filter(file => ["create", "replace", "patch"].includes(file.allowedOperation ?? file.action))
    .filter(file => file.ownership === "generated" || file.visualShellOnly === true)
    .map(file => stableObject({
      targetPath: normalizeProjectPath(file.targetPath),
      planEntryId: file.id,
      ownership: file.ownership,
      allowedOperation: file.allowedOperation ?? file.action,
      visualShellOnly: file.visualShellOnly === true,
      slots: file.slots ?? [],
      sourceEvidenceReferences: (file.sourceEvidenceReferences ?? []).map(assertHandoffReference),
    }))
    .sort((a, b) => a.targetPath.localeCompare(b.targetPath, "en"));
}

function collectEvidenceReferences(plan) {
  const refs = new Set();
  for (const file of plan.files ?? []) {
    for (const reference of file.sourceEvidenceReferences ?? []) {
      refs.add(assertHandoffReference(reference));
    }
  }

  return [...refs].sort((a, b) => a.localeCompare(b, "en"));
}

function copyApprovedEvidence(packageRoot, references, outputRoot) {
  const copied = [];
  for (const reference of references) {
    const normalized = assertHandoffReference(reference);
    if (!normalized.startsWith(`${HANDOFF_ROOT}/section-screenshots/`) && !normalized.startsWith(`${HANDOFF_ROOT}/screenshots/`)) {
      continue;
    }

    const source = join(packageRoot, normalized);
    if (!existsSync(source)) {
      continue;
    }

    const packagePath = `evidence/${normalized.slice(`${HANDOFF_ROOT}/`.length)}`;
    const destination = join(outputRoot, packagePath);
    mkdirSync(dirname(destination), { recursive: true });
    copyFileSync(source, destination);
    copied.push({ handoffPath: normalized, packagePath, checksum: `sha256:${shaFile(destination)}` });
  }

  return copied.sort((a, b) => a.handoffPath.localeCompare(b.handoffPath, "en"));
}

function buildInstructions(plan, allowedOutputs) {
  return [
    "# StorefrontBuilder Agent Visual Task",
    "",
    `Project: ${plan.projectName}`,
    `Store key: ${plan.storeKey}`,
    "",
    "Only modify files listed in `manifest.json` `allowedOutputFiles`.",
    "Generated visual files must not declare routes, endpoints, HTTP clients, DTOs, commerce commands, auth flow logic, SEO canonical logic, server configuration, appsettings secrets, or runtime registration.",
    "Use existing Presentation descriptors and semantic `data-storefront-*` attributes where a plan slot already depends on them.",
    "All UX copy written by the agent must be store-owned copy that can be localized later.",
    "Images and assets must follow `inputs/originality-restrictions.json`; restricted reference material must be replaced, not copied.",
    "",
    "Allowed output files:",
    ...allowedOutputs.map(file => `- ${file.targetPath} (${file.planEntryId})`),
    "",
  ].join("\n");
}

function validateTaskPackage(root) {
  const forbidden = [...FORBIDDEN_SOURCE_ONLY_PREFIXES, ...FORBIDDEN_PACKAGE_TEXT_MARKERS];
  const stack = [root];
  while (stack.length > 0) {
    const current = stack.pop();
    for (const entry of readdir(current)) {
      const path = join(current, entry.name);
      if (entry.isDirectory()) {
        stack.push(path);
        continue;
      }

      if (/\.(png|jpg|jpeg|webp)$/i.test(entry.name)) {
        continue;
      }

      const text = readFileSync(path, "utf8");
      for (const marker of forbidden) {
        if (text.includes(marker)) {
          fail("SFB-AGENT-PACKAGE-004", `Task package contains forbidden source reference '${marker}' in ${path}`);
        }
      }
    }
  }
}

function resolveHandoffPackageRoot(handoffRoot) {
  if (!handoffRoot) {
    fail("SFB-AGENT-PACKAGE-000", "Handoff root is required to write an agent task package.");
  }

  const resolved = resolve(handoffRoot);
  if (existsSync(join(resolved, HANDOFF_ROOT, "manifest.json"))) {
    return resolved;
  }

  if (existsSync(join(resolved, "manifest.json")) && normalizeProjectPath(resolved).endsWith(HANDOFF_ROOT)) {
    return resolve(resolved, "..", "..");
  }

  fail("SFB-AGENT-PACKAGE-000", `Handoff root is not a portable handoff package: ${handoffRoot}`);
}

function assertHandoffReference(reference) {
  const normalized = normalizeProjectPath(reference);
  if (!normalized.startsWith(`${HANDOFF_ROOT}/`)) {
    fail("SFB-AGENT-PACKAGE-002", `Evidence reference is not handoff-local: ${reference}`);
  }

  if (FORBIDDEN_SOURCE_ONLY_PREFIXES.some(prefix => normalized.startsWith(prefix))) {
    fail("SFB-AGENT-PACKAGE-003", `Evidence reference points to source-only artifacts: ${reference}`);
  }

  return normalized;
}

function normalizeProjectPath(path) {
  return String(path ?? "").replaceAll("\\", "/").replace(/^\/+/, "");
}

function listProtectedFiles(value) {
  return (value.paths ?? value.protectedFiles ?? value.files ?? [])
    .map(item => typeof item === "string" ? item : item.path ?? item.targetPath ?? item.file)
    .map(normalizeProjectPath)
    .filter(Boolean)
    .map(redactForbiddenProtectedPath)
    .sort((a, b) => a.localeCompare(b, "en"));
}

function redactForbiddenProtectedPath(path) {
  return FORBIDDEN_PACKAGE_TEXT_MARKERS.some(marker => path.includes(marker))
    ? `protected-path:${sha(path)}`
    : path;
}

function normalizeHash(value) {
  const text = String(value ?? "").trim();
  if (!text) {
    return "";
  }

  return text.startsWith("sha256:") ? text : `sha256:${text}`;
}

function readHandoffJson(root, name) {
  return readJson(join(root, HANDOFF_ROOT, name));
}

function readJson(path) {
  return JSON.parse(readFileSync(path, "utf8"));
}

function writeJson(path, value) {
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, `${JSON.stringify(stableObject(value), null, 2)}\n`, "utf8");
}

function stableJson(value) {
  return JSON.stringify(stableObject(value));
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

function shaFile(path) {
  return sha(readFileSync(path));
}

function readdir(path) {
  return readdirSync(path, { withFileTypes: true });
}

function fail(code, message) {
  throw new Error(`[${code}] ${message}`);
}

function readArg(name) {
  const index = process.argv.indexOf(name);
  return index === -1 ? undefined : process.argv[index + 1];
}
