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
  console.log(`Usage: node write-agent-task-package.mjs --workspace-root <generated-workspace-root> [options]

Options:
  --workspace-root <path>  Generated storefront workspace root.
  --project-root <path>    Compatibility alias for --workspace-root.
  --handoff-root <path>  Portable handoff package root or analysis/agent-handoff folder.
  --plan-json <path>     Generation plan JSON path. Defaults under project docs/storefront-analysis.
  --output <path>        Agent task package output folder. Defaults under project docs/storefront-analysis.
  --help, -h             Show this help text.`);
  process.exit(0);
}

const workspaceRootArg = readArg("--workspace-root");
const projectRootAliasArg = readArg("--project-root");
if (!workspaceRootArg && projectRootAliasArg) {
  console.warn("[SFB-AGENT-PACKAGE-WARN] --project-root is a compatibility alias for --workspace-root and will be removed after the workspace migration.");
}

const projectRoot = resolve(workspaceRootArg ?? projectRootAliasArg ?? "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof");
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
const projects = normalizeProjects(plan.projectName, plan.projects);
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
const protectedFiles = sortedUnique([
  ...listProtectedFiles(artifacts.protectedFiles),
  ...listGeneratedProtectedFiles(projectRoot),
]);
const protectedFilesByProject = groupPathsByProject(plan.projectName, protectedFiles.map(path => ({
  targetPath: path,
  targetProject: inferTargetProject(plan.projectName, path),
  projectRelativePath: inferProjectRelativePath(plan.projectName, path),
})));
const allowedOutputFilesByProject = groupPathsByProject(plan.projectName, allowedOutputs);
const packageProvenance = readGeneratedPackageProvenance(projectRoot);

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
  allowedOutputFilesByProject,
  protectedFilesByProject,
});
writeJson(join(outputRoot, "inputs", "originality-restrictions.json"), artifacts.originalityRestrictions);
writeFileSync(join(outputRoot, "instructions.md"), buildInstructions(plan, allowedOutputs), "utf8");

const manifest = stableObject({
  schemaVersion: "1.0.0",
  artifactKind: "agent-visual-task-package",
  artifactId: `agent-visual-task-package.${plan.projectName}`,
  projectName: plan.projectName,
  storeKey: plan.storeKey,
  projects,
  serverProjectRoot: projects.server.rootPath,
  wasmProjectRoot: projects.wasm.rootPath,
  handoffHash,
  sourceHandoffPackageHash: handoffHash,
  generationPlanHash: planHash,
  packageProvenance,
  packageHashes: Object.fromEntries(packageProvenance.packages.map(item => [item.id, item.sha256])),
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
  allowedOutputFilesByProject,
  protectedFiles,
  protectedFilesByProject,
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
    .map(file => {
      const target = normalizePlannedWorkspaceTarget(plan.projectName, file);
      if (target.projectKind === "workspace") {
        fail("SFB-AGENT-PACKAGE-005", `Allowed visual output has ambiguous workspace target '${target.targetPath}'. Use '${plan.projectName}/...' for server files or '${plan.projectName}.WASM/...' for WASM files.`);
      }

      return stableObject({
        targetPath: target.targetPath,
        workspaceRelativePath: target.targetPath,
        targetProject: target.projectKind,
        projectKind: target.projectKind,
        projectName: target.projectName,
        projectRelativePath: target.projectRelativePath,
        artifactRootRelativePath: file.artifactRootRelativePath ?? `${plan.projectName}/${target.targetPath}`,
        planEntryId: file.id,
        ownership: file.ownership,
        allowedOperation: file.allowedOperation ?? file.action,
        visualShellOnly: file.visualShellOnly === true,
        slots: file.slots ?? [],
        sourceEvidenceReferences: (file.sourceEvidenceReferences ?? []).map(assertHandoffReference),
      });
    })
    .sort((a, b) => a.targetPath.localeCompare(b.targetPath, "en"));
}

function buildProjects(projectName) {
  return {
    workspace: {
      name: projectName,
      rootPath: ".",
      solutionPath: `${projectName}.sln`,
      analysisRoot: "docs/storefront-analysis",
    },
    server: {
      name: projectName,
      rootPath: projectName,
      projectPath: `${projectName}/${projectName}.csproj`,
    },
    wasm: {
      name: `${projectName}.WASM`,
      rootPath: `${projectName}.WASM`,
      projectPath: `${projectName}.WASM/${projectName}.WASM.csproj`,
    },
  };
}

function normalizeProjects(projectName, value) {
  const fallback = buildProjects(projectName);
  return {
    server: {
      ...fallback.server,
      ...(value?.server ?? {}),
    },
    wasm: {
      ...fallback.wasm,
      ...(value?.wasm ?? {}),
    },
  };
}

function inferTargetProject(projectName, targetPath) {
  return describeWorkspaceTarget(projectName, targetPath).projectKind;
}

function groupPathsByProject(projectName, items) {
  const grouped = { workspace: [], server: [], wasm: [] };
  for (const item of items) {
    const target = normalizePlannedWorkspaceTarget(projectName, typeof item === "string" ? { targetPath: item } : item);
    grouped[target.projectKind].push(stableObject({
      targetPath: target.targetPath,
      workspaceRelativePath: target.targetPath,
      targetProject: target.projectKind,
      projectKind: target.projectKind,
      projectName: target.projectName,
      projectRelativePath: target.projectRelativePath,
      artifactRootRelativePath: item.artifactRootRelativePath ?? `${projectName}/${target.targetPath}`,
      ownership: item.ownership,
      allowedOperation: item.allowedOperation,
      visualShellOnly: item.visualShellOnly === true,
      planEntryId: item.planEntryId,
    }));
  }

  grouped.workspace.sort((a, b) => a.targetPath.localeCompare(b.targetPath, "en"));
  grouped.server.sort((a, b) => a.targetPath.localeCompare(b.targetPath, "en"));
  grouped.wasm.sort((a, b) => a.targetPath.localeCompare(b.targetPath, "en"));
  return grouped;
}

function readGeneratedPackageProvenance(projectRoot) {
  const metadataPath = join(projectRoot, "docs", "storefront-analysis", "metadata.yaml");
  if (!existsSync(metadataPath)) {
    return { feedPath: "unknown", packages: [] };
  }

  const text = readFileSync(metadataPath, "utf8");
  const feedPath = text.match(/^\s*feedPath:\s*(\S+)\s*$/m)?.[1] ?? "unknown";
  const packages = [...text.matchAll(/^\s+- id:\s*(?<id>\S+)\s*\r?\n\s+version:\s*(?<version>\S+)\s*\r?\n\s+sha256:\s*(?<sha256>\S+)\s*$/gm)]
    .map(match => stableObject({
      id: match.groups.id,
      version: match.groups.version,
      sha256: match.groups.sha256,
    }))
    .sort((a, b) => a.id.localeCompare(b.id, "en"));

  return stableObject({ feedPath, packages });
}


function inferProjectRelativePath(projectName, targetPath) {
  return describeWorkspaceTarget(projectName, targetPath).projectRelativePath;
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
    "Respect each allowed file's `targetProject`: `server` paths are relative to the generated server project root, `wasm` paths are relative to the sibling WASM project root and appear in `targetPath` with the `<ProjectName>.WASM/` prefix.",
    "Use `allowedOutputFilesByProject` and `protectedFilesByProject` before editing; never move a planned server change into WASM or a planned WASM change into the server.",
    "Generated visual files must not declare routes, endpoints, HTTP clients, DTOs, commerce commands, auth flow logic, SEO canonical logic, server configuration, appsettings secrets, or runtime registration.",
    "Use existing Presentation descriptors and semantic `data-storefront-*` attributes where a plan slot already depends on them.",
    "All UX copy written by the agent must be store-owned copy that can be localized later.",
    "Images and assets must follow `inputs/originality-restrictions.json`; restricted reference material must be replaced, not copied.",
    "",
    "Allowed output files:",
    ...allowedOutputs.map(file => `- ${file.targetPath} (${file.targetProject}; ${file.projectRelativePath}; ${file.planEntryId})`),
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

function normalizePlannedWorkspaceTarget(projectName, file) {
  const targetPath = normalizeProjectPath(file.targetPath ?? file.path ?? file);
  const described = describeWorkspaceTarget(projectName, targetPath);
  const declaredProject = file.projectKind ?? file.targetProject;
  if (declaredProject && declaredProject !== described.projectKind) {
    fail("SFB-AGENT-PACKAGE-006", `Planned target '${targetPath}' declares project '${declaredProject}' but resolves to '${described.projectKind}'.`);
  }

  const declaredRelativePath = file.projectRelativePath ? normalizeProjectPath(file.projectRelativePath) : "";
  if (declaredRelativePath && declaredRelativePath !== described.projectRelativePath) {
    fail("SFB-AGENT-PACKAGE-006", `Planned target '${targetPath}' declares projectRelativePath '${declaredRelativePath}' but resolves to '${described.projectRelativePath}'.`);
  }

  return {
    targetPath,
    ...described,
  };
}

function describeWorkspaceTarget(projectName, targetPath) {
  const normalized = normalizeProjectPath(targetPath);
  if (normalized.startsWith(`${projectName}/`)) {
    return {
      projectKind: "server",
      projectName,
      projectRelativePath: normalized.slice(projectName.length + 1),
    };
  }

  if (normalized.startsWith(`${projectName}.WASM/`)) {
    return {
      projectKind: "wasm",
      projectName: `${projectName}.WASM`,
      projectRelativePath: normalized.slice(`${projectName}.WASM/`.length),
    };
  }

  return {
    projectKind: "workspace",
    projectName,
    projectRelativePath: normalized,
  };
}

function listProtectedFiles(value) {
  return (value.paths ?? value.protectedFiles ?? value.files ?? [])
    .map(item => typeof item === "string" ? item : item.path ?? item.targetPath ?? item.file)
    .map(normalizeProjectPath)
    .filter(Boolean)
    .map(redactForbiddenProtectedPath)
    .sort((a, b) => a.localeCompare(b, "en"));
}

function listGeneratedProtectedFiles(projectRoot) {
  const manifestPath = join(projectRoot, "docs", "storefront-analysis", "generated-files.yaml");
  if (!existsSync(manifestPath)) {
    return [];
  }

  const text = readFileSync(manifestPath, "utf8").replace(/\r\n/g, "\n").replace(/\r/g, "\n");
  const protectedFiles = [];
  for (const match of text.matchAll(/^\s+- filePath:\s*(?<file>[^\n]+)\n(?<body>.*?)(?=^\s+- filePath:|\z)/gms)) {
    const filePath = normalizeProjectPath(match.groups.file);
    if (/^\s+ownership:\s*protected\s*$/m.test(match.groups.body) || isGeneratedRuntimeProtectedPath(filePath)) {
      protectedFiles.push(filePath);
    }
  }

  return protectedFiles;
}

function isGeneratedRuntimeProtectedPath(path) {
  return path === "Program.cs" ||
    path.endsWith("/Program.cs") ||
    path.endsWith(".csproj") ||
    path === "StorefrontPackageVersions.props" ||
    path === "starter-generation.contract.yaml" ||
    path === "nuget.config" ||
    path === "appsettings.json";
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

function sortedUnique(items) {
  return [...new Set(items)].sort((a, b) => a.localeCompare(b, "en"));
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
