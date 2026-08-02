#!/usr/bin/env node
import { readFileSync, readdirSync } from "node:fs";
import { basename, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const workspaceRoot = resolve(fileURLToPath(new URL("..", import.meta.url)));
const schemaRoot = join(workspaceRoot, "schemas");
const exampleRoot = join(workspaceRoot, "examples");

const schemas = new Map();
for (const file of readdirSync(schemaRoot).filter((name) => name.endsWith(".schema.json")).sort()) {
  const schema = readJson(join(schemaRoot, file));
  schemas.set(file.replace(".schema.json", ""), schema);
}

if (schemas.size < 6) {
  throw new Error(`Expected at least 6 visual schemas, found ${schemas.size}.`);
}

for (const [name, schema] of schemas) {
  for (const field of ["$schema", "title", "type", "required", "properties"]) {
    if (!Object.prototype.hasOwnProperty.call(schema, field)) {
      throw new Error(`Schema ${name} is missing ${field}.`);
    }
  }
}

const examples = readdirSync(exampleRoot).filter((name) => name.endsWith(".valid.json")).sort();
if (examples.length < schemas.size) {
  throw new Error(`Expected at least ${schemas.size} valid examples, found ${examples.length}.`);
}

for (const file of examples) {
  const schemaName = file.replace(".valid.json", "");
  const schema = schemas.get(schemaName);
  if (!schema) {
    throw new Error(`Example ${file} has no matching schema.`);
  }

  const artifact = readJson(join(exampleRoot, file));
  validate(schema, artifact, basename(file), schema);
}

console.log(`Visual schema examples validated: ${examples.length}.`);

function readJson(path) {
  return JSON.parse(readFileSync(path, "utf8"));
}

function validate(schema, value, path, rootSchema) {
  const resolved = resolveRef(schema, rootSchema);
  validateType(resolved, value, path);

  if (resolved.enum && !resolved.enum.includes(value)) {
    throw new Error(`${path} must be one of: ${resolved.enum.join(", ")}.`);
  }

  if (typeof value === "string" && resolved.minLength && value.length < resolved.minLength) {
    throw new Error(`${path} must have minLength ${resolved.minLength}.`);
  }

  if (resolved.type === "object") {
    const required = resolved.required ?? [];
    for (const field of required) {
      if (!Object.prototype.hasOwnProperty.call(value, field)) {
        throw new Error(`${path} is missing required field ${field}.`);
      }
    }

    if (resolved.additionalProperties === false) {
      const allowed = new Set(Object.keys(resolved.properties ?? {}));
      for (const field of Object.keys(value)) {
        if (!allowed.has(field)) {
          throw new Error(`${path}.${field} is not allowed by schema.`);
        }
      }
    }

    for (const [field, childSchema] of Object.entries(resolved.properties ?? {})) {
      if (Object.prototype.hasOwnProperty.call(value, field)) {
        validate(childSchema, value[field], `${path}.${field}`, rootSchema);
      }
    }
  }

  if (resolved.type === "array") {
    if (resolved.items) {
      value.forEach((item, index) => validate(resolved.items, item, `${path}[${index}]`, rootSchema));
    }
  }
}

function resolveRef(schema, rootSchema) {
  if (!schema.$ref) {
    return schema;
  }

  const prefix = "#/$defs/";
  if (!schema.$ref.startsWith(prefix)) {
    throw new Error(`Unsupported schema ref ${schema.$ref}.`);
  }

  const name = schema.$ref.slice(prefix.length);
  const resolved = rootSchema.$defs?.[name];
  if (!resolved) {
    throw new Error(`Unknown schema ref ${schema.$ref}.`);
  }

  return resolved;
}

function validateType(schema, value, path) {
  if (!schema.type) {
    return;
  }

  if (schema.type === "array" && !Array.isArray(value)) {
    throw new Error(`${path} must be an array.`);
  }

  if (schema.type === "object" && (value === null || Array.isArray(value) || typeof value !== "object")) {
    throw new Error(`${path} must be an object.`);
  }

  if (schema.type === "integer" && !Number.isInteger(value)) {
    throw new Error(`${path} must be an integer.`);
  }

  if (schema.type !== "array" && schema.type !== "object" && schema.type !== "integer" && typeof value !== schema.type) {
    throw new Error(`${path} must be ${schema.type}.`);
  }
}
