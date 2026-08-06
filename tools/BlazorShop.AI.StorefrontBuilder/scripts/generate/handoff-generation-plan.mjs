import { createHash } from "node:crypto";
import { existsSync, readFileSync } from "node:fs";
import { isAbsolute, join, resolve } from "node:path";

const HANDOFF_ROOT = "analysis/agent-handoff";
const HANDOFF_ARTIFACTS = {
  manifest: "analysis/agent-handoff/manifest.json",
  readiness: "analysis/agent-handoff/handoff-readiness.json",
  pageCompositions: "analysis/agent-handoff/page-compositions.json",
  storefrontPattern: "analysis/agent-handoff/storefront-pattern.json",
  presentationCatalog: "analysis/agent-handoff/presentation-catalog.json",
  presentationMappings: "analysis/agent-handoff/presentation-mappings.json",
  allowedFiles: "analysis/agent-handoff/allowed-files.json",
  protectedFiles: "analysis/agent-handoff/protected-files.json",
  designTokens: "analysis/agent-handoff/design-tokens.json",
  visualStyle: "analysis/agent-handoff/visual-style.json",
  responsiveBehavior: "analysis/agent-handoff/responsive-behavior.json",
  interactionModels: "analysis/agent-handoff/interaction-models.json",
  originalityRestrictions: "analysis/agent-handoff/originality-restrictions.json",
  evidenceManifest: "analysis/agent-handoff/evidence-manifest.json",
  unresolvedRegions: "analysis/agent-handoff/unresolved-regions.json",
};

const VISUAL_SHELL_SLOTS = new Set(["cart.page", "checkout.page", "account.shell", "system.error"]);
const DEFAULT_TOKEN_TARGET = "wwwroot/css/storefront-builder.generated.css";

export function buildHandoffGenerationPlan(options) {
  const repoRoot = resolve(options.repoRoot ?? process.cwd());
  const packageRoot = resolveHandoffPackageRoot(options.handoffRoot);
  const projectName = options.projectName ?? "BlazorShop.Storefront.GeneratedProof";
  const storeKey = options.storeKey ?? "sample";
  const outputRoot = normalizePath(options.outputRoot ?? "artifacts/storefront-builder/generated").replace(/\/$/, "");
  const projectRoot = `${outputRoot}/${projectName}`;
  const generatorVersion = options.generatorVersion ?? readGeneratorVersion(repoRoot);
  const artifacts = readHandoffArtifacts(packageRoot);

  validateReadyManifest(artifacts.manifest, artifacts.readiness);

  const starterContractPath = join(repoRoot, "BlazorShop.PresentationV2", "BlazorShop.Storefront.Starter", "starter-generation.contract.yaml");
  const sourceStarterContractHash = shaFile(starterContractPath);
  const sourceHandoffReadinessHash = shaFile(join(packageRoot, HANDOFF_ARTIFACTS.readiness));
  const sourceHandoffPackageHash = artifacts.manifest.packageHash;
  const slotCatalog = buildSlotCatalog(artifacts.storefrontPattern);
  const contractCatalog = artifacts.storefrontPattern.pageContracts ?? [];
  const evidenceCatalog = buildEvidenceCatalog(artifacts.evidenceManifest);
  const mappingCatalog = buildPresentationMappingCatalog(artifacts.presentationMappings);
  const blockedItems = buildBlockedItems(artifacts.unresolvedRegions, artifacts.interactionModels, artifacts.originalityRestrictions);
  const assetPlan = buildAssetPlan(artifacts.originalityRestrictions);
  const tokenPlan = buildTokenPlan(artifacts.designTokens, artifacts.visualStyle);
  const filesByPath = new Map();
  const slotPlans = [];
  const warnings = [];

  addTokenFile(filesByPath, projectRoot, sourceStarterContractHash, tokenPlan);

  for (const composition of sortedBy(artifacts.pageCompositions.compositions ?? artifacts.pageCompositions.pages ?? [], item => item.pageId ?? "")) {
    const pageId = requiredString(composition.pageId, "SFB-HANDOFF-PLAN-001", "Page composition is missing pageId.");
    const pageArchetype = composition.pageArchetype ?? composition.archetype ?? pageId;
    const contract = findPageContract(contractCatalog, pageId, pageArchetype);
    const sections = composition.sectionTree ?? composition.compositionTree ?? [];
    const sectionSlots = new Set(sections.map(section => slotFromSection(section, pageId, evidenceCatalog, mappingCatalog)).filter(Boolean));
    if (composition.targetViewSlot) {
      sectionSlots.add(composition.targetViewSlot);
    }

    const fallbackSections = buildSharedLayoutFallbackSections(artifacts.presentationMappings, contract, pageId, sectionSlots);
    for (const section of fallbackSections) {
      sectionSlots.add(section.starterSlotId);
    }

    for (const requiredSlot of contract.requiredSlotIds ?? []) {
      if (!sectionSlots.has(requiredSlot)) {
        throw planError("SFB-HANDOFF-PLAN-004", `Required slot '${requiredSlot}' is missing for page '${pageId}'.`, "The reviewed page composition does not contain a required Presentation slot section.", "Resolve the reviewed page composition before compiling the generation plan.");
      }
    }

    for (const optionalSlot of contract.optionalSlotIds ?? []) {
      if (!sectionSlots.has(optionalSlot)) {
        warnings.push({ code: "optional-slot-not-reviewed", pageId, slotId: optionalSlot, message: `Optional slot '${optionalSlot}' has no reviewed section for page '${pageId}'.` });
      }
    }

    for (const section of sortedBy([...sections, ...fallbackSections], item => item.nodeId ?? "")) {
      const slotId = slotFromSection(section, pageId, evidenceCatalog, mappingCatalog);
      if (!slotId) {
        warnings.push({ code: "section-without-slot", pageId, sectionId: section.nodeId ?? "", message: "Reviewed section has no Presentation slot mapping." });
        continue;
      }

      const slotContract = slotCatalog.get(slotId);
      if (!slotContract) {
        throw planError("SFB-HANDOFF-PLAN-005", `Slot '${slotId}' is not declared by the Storefront pattern contract.`, "The page composition references a slot outside Starter/Presentation metadata.", "Regenerate the handoff after updating Storefront pattern contracts.");
      }

      if (section.targetFilePath) {
        validateTargetPath(normalizeTargetPath(section.targetFilePath), artifacts.storefrontPattern, artifacts.protectedFiles);
      }

      const targetPath = normalizeTargetPath(slotContract.path);
      validateTargetPath(targetPath, artifacts.storefrontPattern, artifacts.protectedFiles);
      const ownership = slotContract.owner ?? (VISUAL_SHELL_SLOTS.has(slotId) ? "managed" : "generated");
      const evidenceRefs = evidenceCatalog.refsFor(pageId, slotId);
      validateHandoffEvidenceReferences(evidenceRefs);

      const file = ensureFilePlan(filesByPath, {
        projectRoot,
        targetPath,
        ownership,
        action: ownership === "managed" ? "patch" : "replace",
        slotId,
        pageId,
        sourceHandoffArtifacts: [
          HANDOFF_ARTIFACTS.pageCompositions,
          HANDOFF_ARTIFACTS.storefrontPattern,
          ...(section.sourceHandoffArtifacts ?? []),
          HANDOFF_ARTIFACTS.evidenceManifest,
          HANDOFF_ARTIFACTS.designTokens,
          HANDOFF_ARTIFACTS.visualStyle,
          HANDOFF_ARTIFACTS.responsiveBehavior,
          HANDOFF_ARTIFACTS.interactionModels,
          HANDOFF_ARTIFACTS.originalityRestrictions,
        ],
        sourceEvidenceReferences: evidenceRefs,
        rationale: buildRationale(pageId, slotId, ownership),
        sourceSpecHash: sourceStarterContractHash,
      });

      file.slots = sortedUnique([...file.slots, slotId]);
      file.pages = sortedUnique([...file.pages, pageId]);
      file.sourceEvidenceReferences = sortedUnique([...file.sourceEvidenceReferences, ...evidenceRefs]);
      file.generatedHash = fileHash(file);

      slotPlans.push({
        pageId,
        pageArchetype,
        sectionId: section.nodeId ?? "",
        slotId,
        targetPath,
        plannedFileId: file.id,
        ownership,
        required: (contract.requiredSlotIds ?? []).includes(slotId),
        visualShellOnly: VISUAL_SHELL_SLOTS.has(slotId),
        sourceHandoffArtifacts: [HANDOFF_ARTIFACTS.pageCompositions, ...(section.sourceHandoffArtifacts ?? []), HANDOFF_ARTIFACTS.evidenceManifest],
        sourceEvidenceReferences: evidenceRefs,
      });
    }
  }

  const plan = {
    schemaVersion: "2.0.0",
    artifactKind: "generation-plan",
    artifactId: `generation-plan.${slug(projectName)}.${sourceHandoffPackageHash.slice(0, 12)}`,
    generatorVersion,
    sourceHandoffPackageHash,
    sourceHandoffReadinessHash,
    sourceStarterContractHash,
    projectName,
    storeKey,
    projects: buildProjects(projectName),
    generationMode: "handoff",
    generationOrder: ["generate-from-starter", "compile-handoff-plan", "apply-visual-files"],
    files: sortedBy([...filesByPath.values()].map(finalizeFilePlan), item => item.targetPath),
    slots: sortedBy(slotPlans.map(item => stableObject(item)), item => `${item.pageId}:${item.slotId}:${item.sectionId}`),
    assets: sortedBy(assetPlan.map(item => stableObject(item)), item => item.assetId),
    copyBlocks: buildCopyBlocks(artifacts.originalityRestrictions),
    tokens: tokenPlan,
    warnings: sortedBy(warnings.map(item => stableObject(item)), item => `${item.code}:${item.pageId ?? ""}:${item.slotId ?? ""}`),
    blockedItems: sortedBy(blockedItems.map(item => stableObject(item)), item => `${item.code}:${item.pageId ?? ""}:${item.itemId ?? ""}`),
  };

  validateCompiledPlan(plan, artifacts.storefrontPattern);
  return stableObject(plan);
}

export function summarizePlan(plan) {
  const counts = new Map();
  for (const file of plan.files) {
    const key = `${file.action}/${file.ownership}`;
    counts.set(key, (counts.get(key) ?? 0) + 1);
  }

  return [
    `Generation plan: ${plan.artifactId}`,
    `Mode: ${plan.generationMode}`,
    `Files: ${plan.files.length}`,
    `Slots: ${plan.slots.length}`,
    `Assets: ${plan.assets.length}`,
    `Warnings: ${plan.warnings.length}`,
    `Blocked items: ${plan.blockedItems.length}`,
    ...[...counts.entries()].sort((a, b) => a[0].localeCompare(b[0], "en")).map(([key, value]) => `${key}: ${value}`),
  ];
}

export function stableJson(value) {
  return `${JSON.stringify(stableObject(value), null, 2)}\n`;
}

function readHandoffArtifacts(packageRoot) {
  return Object.fromEntries(Object.entries(HANDOFF_ARTIFACTS).map(([key, relativePath]) => [key, readJson(join(packageRoot, relativePath))]));
}

function validateReadyManifest(manifest, readiness) {
  if (readiness.passed !== true) {
    throw planError("SFB-HANDOFF-PLAN-002", "Handoff readiness is not passed.", "The compiler only accepts final ready packages.", "Rerun preflight after resolving handoff readiness blockers.");
  }

  if (manifest.readinessPassed !== readiness.passed) {
    throw planError("SFB-HANDOFF-PLAN-003", "Manifest readiness disagrees with handoff-readiness.json.", "The package is stale or was edited after assembly.", "Regenerate the portable handoff package.");
  }
}

function buildSlotCatalog(pattern) {
  const slots = new Map();
  for (const slot of pattern.slots ?? []) {
    slots.set(slot.slotId, slot);
  }

  return slots;
}

function buildPresentationMappingCatalog(presentationMappings) {
  const byId = new Map();
  for (const mapping of presentationMappings.mappings ?? []) {
    if (mapping.sourceCandidateId) {
      byId.set(mapping.sourceCandidateId, mapping);
    }
  }

  return { byId };
}

function buildSharedLayoutFallbackSections(presentationMappings, contract, pageId, observedSlots) {
  const allowedSlots = new Set([
    ...(contract.requiredSlotIds ?? []),
    ...(contract.optionalSlotIds ?? []),
    ...(contract.repeatableSlotIds ?? []),
    ...(contract.allowedAdditionalSlotIds ?? []),
  ]);

  const fallbackSections = [];
  for (const mapping of presentationMappings.mappings ?? []) {
    const slotId = mapping.starterSlotId ?? mapping.presentationComponentId;
    if (!isApprovedMapping(mapping) ||
        !isSharedLayoutFallbackMapping(mapping) ||
        !allowedSlots.has(slotId) ||
        observedSlots.has(slotId)) {
      continue;
    }

    fallbackSections.push({
      nodeId: `shared-layout:${slotId}:${mapping.sourceCandidateId ?? "mapping"}`,
      role: "shared layout",
      presentationMappingId: mapping.sourceCandidateId ?? null,
      componentMappingRef: mapping.sourceCandidateId ?? null,
      starterSlotId: slotId,
      targetFilePath: mapping.targetGeneratedPath,
      sourceHandoffArtifacts: [HANDOFF_ARTIFACTS.presentationMappings],
      sourcePageId: pageId,
    });
    observedSlots.add(slotId);
  }

  return fallbackSections;
}

function findPageContract(contracts, pageId, pageArchetype) {
  return contracts.find(contract => contract.pageId === pageId || contract.stablePageArchetype === pageArchetype)
    ?? { pageId, stablePageArchetype: pageArchetype, requiredSlotIds: [], optionalSlotIds: [] };
}

function buildEvidenceCatalog(evidenceManifest) {
  const byPageSlot = new Map();
  const byPageSection = new Map();
  for (const page of evidenceManifest.pages ?? []) {
    for (const section of page.sections ?? []) {
      const slotId = section.starterSlotId ?? section.suggestedSlotId;
      if (!slotId) {
        continue;
      }

      byPageSection.set(`${page.pageId}:${section.sectionId}`, slotId);
      const key = `${page.pageId}:${slotId}`;
      const refs = byPageSlot.get(key) ?? [];
      refs.push(section.handoffPath);
      byPageSlot.set(key, refs);
    }
  }

  return {
    refsFor(pageId, slotId) {
      return sortedUnique(byPageSlot.get(`${pageId}:${slotId}`) ?? []);
    },
    slotFor(pageId, sectionId) {
      return byPageSection.get(`${pageId}:${sectionId}`);
    },
  };
}

function addTokenFile(filesByPath, projectRoot, sourceSpecHash, tokenPlan) {
  const file = ensureFilePlan(filesByPath, {
    projectRoot,
    targetPath: DEFAULT_TOKEN_TARGET,
    ownership: "generated",
    action: "replace",
    slotId: "theme.foundation",
    pageId: "",
    sourceHandoffArtifacts: [HANDOFF_ARTIFACTS.designTokens, HANDOFF_ARTIFACTS.visualStyle],
    sourceEvidenceReferences: [],
    rationale: "Compile reviewed design tokens and visual style into generated CSS tokens.",
    sourceSpecHash,
  });
  file.tokenGroups = tokenPlan.map(item => item.tokenGroup);
  file.generatedHash = fileHash(file);
}

function ensureFilePlan(filesByPath, input) {
  const existing = filesByPath.get(input.targetPath);
  if (existing) {
    existing.sourceHandoffArtifacts = sortedUnique([...existing.sourceHandoffArtifacts, ...input.sourceHandoffArtifacts]);
    existing.sourceEvidenceReferences = sortedUnique([...existing.sourceEvidenceReferences, ...input.sourceEvidenceReferences]);
    return existing;
  }

  const file = {
    id: `file.${slug(input.targetPath)}`,
    filePath: `${input.projectRoot}/${input.targetPath}`,
    targetPath: input.targetPath,
    targetProject: inferTargetProject(input.projectRoot, input.targetPath),
    projectRelativePath: inferProjectRelativePath(input.projectRoot, input.targetPath),
    ownership: input.ownership,
    action: input.action,
    allowedOperation: input.action,
    sourceArtifactIds: input.sourceHandoffArtifacts,
    sourceHandoffArtifacts: input.sourceHandoffArtifacts,
    sourceEvidenceReferences: input.sourceEvidenceReferences,
    expectedSlot: input.slotId,
    slotId: input.slotId,
    slots: input.slotId === "none" ? [] : [input.slotId],
    pageId: input.pageId,
    pages: input.pageId ? [input.pageId] : [],
    validationRuleIds: validationRulesFor(input.ownership, input.slotId),
    conflictBehavior: input.ownership === "protected" ? "never edit" : "replace only when generated hash matches",
    routeOwnership: "none",
    declaresRoute: false,
    visualShellOnly: VISUAL_SHELL_SLOTS.has(input.slotId),
    sourceSpecHash: input.sourceSpecHash,
    checksumSeed: sha(`${input.targetPath}:${input.ownership}:${input.action}:${input.slotId}`),
    generatedHash: "",
    rationale: input.rationale,
  };
  file.generatedHash = fileHash(file);
  filesByPath.set(input.targetPath, file);
  return file;
}

function buildProjects(projectName) {
  return {
    server: {
      name: projectName,
      rootPath: ".",
      projectPath: `${projectName}.csproj`,
    },
    wasm: {
      name: `${projectName}.WASM`,
      rootPath: `${projectName}.WASM`,
      projectPath: `${projectName}.WASM/${projectName}.WASM.csproj`,
    },
  };
}

function inferTargetProject(projectRoot, targetPath) {
  const projectName = normalizePath(projectRoot).split("/").pop();
  return targetPath.startsWith(`${projectName}.WASM/`) ? "wasm" : "server";
}

function inferProjectRelativePath(projectRoot, targetPath) {
  const projectName = normalizePath(projectRoot).split("/").pop();
  return targetPath.startsWith(`${projectName}.WASM/`) ? targetPath.slice(`${projectName}.WASM/`.length) : targetPath;
}

function finalizeFilePlan(file) {
  file.sourceArtifactIds = sortedUnique(file.sourceHandoffArtifacts);
  file.sourceHandoffArtifacts = sortedUnique(file.sourceHandoffArtifacts);
  file.sourceEvidenceReferences = sortedUnique(file.sourceEvidenceReferences);
  file.slots = sortedUnique(file.slots);
  file.pages = sortedUnique(file.pages);
  file.expectedSlot = file.slots[0] ?? file.expectedSlot;
  file.generatedHash = fileHash(file);
  return stableObject(file);
}

function validateCompiledPlan(plan, storefrontPattern) {
  for (const file of plan.files) {
    validateTargetPath(file.targetPath, storefrontPattern, { paths: [] }, file.ownership === "protected");
    validateHandoffEvidenceReferences(file.sourceEvidenceReferences ?? []);
    if (file.declaresRoute === true) {
      throw planError("SFB-HANDOFF-PLAN-009", `Planned file '${file.targetPath}' declares route ownership.`, "Generated visual files must not declare @page routes.", "Remove route ownership from the generated visual plan.");
    }
  }
}

function validateTargetPath(targetPath, pattern, protectedFiles, allowProtected = false) {
  if (isAbsolute(targetPath) || targetPath.includes(":") || targetPath.startsWith("../") || targetPath.includes("/../")) {
    throw planError("SFB-HANDOFF-PLAN-006", `Target path '${targetPath}' is not a normalized generated-project relative path.`, "Generation plans cannot write absolute paths or escape the generated project.", "Use a path under an allowed generated zone.");
  }

  const protectedZones = sortedUnique([
    ...(pattern.generationZones?.protectedZones ?? []),
    ...(protectedFiles.paths ?? []),
    "BlazorShop.Storefront.Presentation",
    "BlazorShop.Storefront.Runtime",
    "BlazorShop.Storefront.Client",
    "BlazorShop.Storefront.V2",
    "BlazorShop.Storefront.Starter",
    "StorefrontPackageVersions.props",
    "starter-generation.contract.yaml",
  ]).map(normalizePath);
  const isProtected = protectedZones.some(zone => targetPath === zone || targetPath.startsWith(`${zone}/`) || targetPath.includes(zone));
  if (isProtected && !allowProtected) {
    throw planError("SFB-HANDOFF-PLAN-007", `Target path '${targetPath}' is protected.`, "Handoff generation cannot plan edits to Starter, package metadata, Presentation, Runtime, Client, V2, or protected docs.", "Map the slot to a generated-owned visual file instead.");
  }

  const zones = sortedUnique([...(pattern.generationZones?.generatedZones ?? []), ...(pattern.generationZones?.managedZones ?? [])]).map(normalizePath);
  const inAllowedZone = zones.some(zone => targetPath === zone || targetPath.startsWith(`${zone}/`));
  if (!allowProtected && !inAllowedZone) {
    throw planError("SFB-HANDOFF-PLAN-006", `Target path '${targetPath}' is outside allowed generated zones.`, "The target is not declared by the Starter generation contract.", "Regenerate the handoff from current Starter/Presentation contracts.");
  }
}

function validateHandoffEvidenceReferences(refs) {
  for (const reference of refs) {
    const normalized = normalizePath(reference);
    if (!normalized.startsWith(`${HANDOFF_ROOT}/`)) {
      throw planError("SFB-HANDOFF-PLAN-008", `Plan evidence reference '${reference}' is not handoff-local.`, "Generation plans must not read raw captures, source analysis, review folders, or reports.", "Use evidence-manifest handoffPath values under analysis/agent-handoff only.");
    }
  }
}

function buildBlockedItems(unresolved, interactions, originality) {
  const items = [];
  for (const region of unresolved.blockingRegions ?? []) {
    items.push({ code: "unresolved-blocking-region", severity: "blocking", itemId: region, message: "Unresolved handoff blocker prevents visual generation." });
  }

  for (const page of interactions.pages ?? []) {
    for (const interaction of page.interactions ?? []) {
      const text = JSON.stringify(interaction).toLowerCase();
      if (interaction.requiresBusinessLogic === true || text.includes("business-logic") || text.includes("direct-commerce") || text.includes("fetch(")) {
        items.push({ code: "unsupported-functional-interaction", severity: "blocking", pageId: page.pageId, itemId: interaction.interactionId ?? interaction.id ?? "interaction", message: "Interaction requires business logic or direct transport and must become a manual blocker." });
      }
    }
  }

  for (const asset of buildAssetPlan(originality)) {
    if (asset.replacementRequired) {
      items.push({ code: "restricted-copied-asset", severity: "blocking", itemId: asset.assetId, message: "Originality policy requires asset replacement instead of copying source material." });
    }
  }

  return items;
}

function buildAssetPlan(originality) {
  return (originality.decisions ?? []).map((decision, index) => {
    const usage = String(decision.usage ?? decision.policy ?? decision.status ?? decision.decision ?? "").toLowerCase();
    const replacementRequired = usage.includes("disallow") || usage.includes("restricted") || usage.includes("not-production-safe") || usage.includes("reference-only");
    return {
      assetId: decision.assetId ?? decision.evidenceId ?? decision.itemId ?? `asset-${String(index + 1).padStart(2, "0")}`,
      sourceHandoffArtifacts: [HANDOFF_ARTIFACTS.originalityRestrictions],
      evidenceReference: decision.handoffPath ?? decision.evidenceReference ?? "",
      reusePolicy: usage || "review-required",
      replacementRequired,
      copyAllowed: !replacementRequired,
    };
  });
}

function buildCopyBlocks(originality) {
  return sortedBy((originality.decisions ?? [])
    .filter(decision => decision.copyBlockId || decision.text || decision.copy)
    .map((decision, index) => ({
      copyBlockId: decision.copyBlockId ?? decision.itemId ?? `copy-${String(index + 1).padStart(2, "0")}`,
      sourceHandoffArtifacts: [HANDOFF_ARTIFACTS.originalityRestrictions],
      rewriteRequired: true,
      reusePolicy: decision.usage ?? decision.policy ?? "review-required",
    })), item => item.copyBlockId);
}

function buildTokenPlan(designTokens, visualStyle) {
  return [
    { tokenGroup: "design-tokens", sourceHandoffArtifact: HANDOFF_ARTIFACTS.designTokens, tokenCount: countScalarLeaves(designTokens), targetPath: DEFAULT_TOKEN_TARGET },
    { tokenGroup: "visual-style", sourceHandoffArtifact: HANDOFF_ARTIFACTS.visualStyle, tokenCount: countScalarLeaves(visualStyle), targetPath: DEFAULT_TOKEN_TARGET },
  ];
}

function validationRulesFor(ownership, slotId) {
  const rules = ["SFB-HANDOFF-PLAN-OWNERSHIP", "SFB-HANDOFF-PLAN-HANDOFF-LOCAL-EVIDENCE"];
  if (ownership === "protected") {
    rules.push("SFB-PROTECTED-001");
  }

  if (VISUAL_SHELL_SLOTS.has(slotId)) {
    rules.push("SFB-HANDOFF-PLAN-VISUAL-SHELL-ONLY");
  }

  return rules;
}

function slotFromSection(section, pageId, evidenceCatalog, mappingCatalog) {
  if (section.starterSlotId) {
    return section.starterSlotId;
  }

  if (section.suggestedSlotId) {
    return section.suggestedSlotId;
  }

  const mappingId = section.componentMappingRef ?? section.presentationMappingId;
  const mapping = mappingId ? mappingCatalog?.byId.get(mappingId) : undefined;
  if (mapping && isApprovedMapping(mapping) && mappingAppliesToSection(mapping, pageId, section.nodeId)) {
    return mapping.starterSlotId ?? mapping.presentationComponentId;
  }

  const evidenceSlot = evidenceCatalog?.slotFor(pageId, section.nodeId);
  if (evidenceSlot) {
    return evidenceSlot;
  }

  const mappingText = mappingId ?? "";
  const prefix = `${pageId}-`;
  return mappingText.startsWith(prefix) ? mappingText.slice(prefix.length) : mappingText.includes("-") ? mappingText.slice(mappingText.indexOf("-") + 1) : mappingText;
}

function mappingAppliesToSection(mapping, pageId, sectionId) {
  return stringEquals(mapping.sourcePageId, pageId) && stringEquals(mapping.sourceSectionId, sectionId);
}

function isApprovedMapping(mapping) {
  return !mapping.reviewState || stringEquals(mapping.reviewState, "Approved");
}

function isSharedLayoutFallbackMapping(mapping) {
  const slotId = mapping.starterSlotId ?? mapping.presentationComponentId ?? "";
  return slotId.startsWith("layout.") &&
    String(mapping.targetGeneratedPath ?? "").replaceAll("\\", "/").includes("Components/Layout/") &&
    (isBlank(mapping.sourcePageId) || isBlank(mapping.sourceSectionId) || stringEquals(mapping.sourcePageId, "unknown") || stringEquals(mapping.sourceSectionId, "unknown"));
}

function isBlank(value) {
  return value === undefined || value === null || String(value).trim().length === 0;
}

function stringEquals(left, right) {
  return String(left ?? "").localeCompare(String(right ?? ""), "en", { sensitivity: "accent" }) === 0;
}

function buildRationale(pageId, slotId, ownership) {
  return VISUAL_SHELL_SLOTS.has(slotId)
    ? `Map reviewed ${pageId}/${slotId} to a visual shell only; Presentation/Runtime keep behavior.`
    : `Map reviewed ${pageId}/${slotId} to a ${ownership} generated visual target.`;
}

function resolveHandoffPackageRoot(handoffRoot) {
  if (!handoffRoot) {
    throw planError("SFB-HANDOFF-PLAN-000", "Handoff root is required for handoff generation planning.", "The static default plan mode does not provide portable handoff artifacts.", "Pass --handoff-root after a successful StorefrontBuilder preflight.");
  }

  const resolved = resolve(handoffRoot);
  if (existsSync(join(resolved, HANDOFF_ARTIFACTS.manifest))) {
    return resolved;
  }

  if (existsSync(join(resolved, "manifest.json")) && normalizePath(resolved).endsWith(HANDOFF_ROOT)) {
    return resolve(resolved, "..", "..");
  }

  throw planError("SFB-HANDOFF-PLAN-000", `Handoff root '${handoffRoot}' is not a portable handoff package.`, "The compiler accepts a package root or its analysis/agent-handoff folder.", "Pass the same path accepted by StorefrontBuilder preflight.");
}

function normalizeTargetPath(targetPath) {
  return normalizePath(requiredString(targetPath, "SFB-HANDOFF-PLAN-006", "Target path is missing.")).replace(/^\/+/, "");
}

function readGeneratorVersion(repoRoot) {
  return readJson(join(repoRoot, "tools", "BlazorShop.AI.StorefrontBuilder", "version.json")).generatorVersion ?? "unknown";
}

function readJson(path) {
  return JSON.parse(readFileSync(path, "utf8"));
}

function shaFile(path) {
  return createHash("sha256").update(readFileSync(path)).digest("hex");
}

function sha(value) {
  return createHash("sha256").update(value).digest("hex");
}

function fileHash(file) {
  return sha(stableJson({
    action: file.action,
    ownership: file.ownership,
    pages: file.pages,
    slots: file.slots,
    sourceEvidenceReferences: file.sourceEvidenceReferences,
    sourceHandoffArtifacts: file.sourceHandoffArtifacts,
    sourceSpecHash: file.sourceSpecHash,
    targetPath: file.targetPath,
    targetProject: file.targetProject,
    visualShellOnly: file.visualShellOnly,
  }));
}

function countScalarLeaves(value) {
  if (value === null || value === undefined) {
    return 0;
  }

  if (Array.isArray(value)) {
    return value.reduce((count, item) => count + countScalarLeaves(item), 0);
  }

  if (typeof value === "object") {
    return Object.values(value).reduce((count, item) => count + countScalarLeaves(item), 0);
  }

  return 1;
}

function requiredString(value, code, message) {
  if (typeof value !== "string" || value.length === 0) {
    throw planError(code, message, "A required handoff field is missing or empty.", "Regenerate the handoff package from reviewed artifacts.");
  }

  return value;
}

function sortedUnique(values) {
  return [...new Set(values.filter(value => value !== undefined && value !== null).map(value => normalizePath(String(value))))].sort((a, b) => a.localeCompare(b, "en"));
}

function sortedBy(values, selector) {
  return [...values].sort((a, b) => String(selector(a)).localeCompare(String(selector(b)), "en"));
}

function normalizePath(value) {
  return String(value).replaceAll("\\", "/");
}

function slug(value) {
  return normalizePath(value).replace(/[^A-Za-z0-9_.-]+/g, "-").replace(/^-+|-+$/g, "");
}

function stableObject(value) {
  if (Array.isArray(value)) {
    return value.map(stableObject);
  }

  if (value && typeof value === "object") {
    return Object.fromEntries(Object.entries(value)
      .sort(([a], [b]) => a.localeCompare(b, "en"))
      .map(([key, item]) => [key, stableObject(item)]));
  }

  return value;
}

function planError(code, problem, cause, fix) {
  return new Error(`[${code}] Handoff generation plan failed. Problem: ${problem} Cause: ${cause} Fix: ${fix}`);
}
