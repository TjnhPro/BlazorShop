#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

export function readStorefrontBuilderGeneratorVersion() {
  const versionPath = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..", "version.json");
  let versionDocument;
  try {
    versionDocument = JSON.parse(readFileSync(versionPath, "utf8"));
  } catch (error) {
    throw new Error(`[SFB-GENERATOR-001] StorefrontBuilder version.json is missing or malformed. Problem: generatorVersion cannot be read from '${versionPath}'. Cause: ${error.message}. Fix: restore tools/BlazorShop.AI.StorefrontBuilder/version.json with a generatorVersion value.`);
  }

  if (!versionDocument.generatorVersion || typeof versionDocument.generatorVersion !== "string") {
    throw new Error(`[SFB-GENERATOR-001] StorefrontBuilder version.json is missing generatorVersion. Problem: generatorVersion must be a non-empty string in '${versionPath}'. Fix: set generatorVersion to a non-empty version string.`);
  }

  return versionDocument.generatorVersion;
}

export const generatorVersion = readStorefrontBuilderGeneratorVersion();
