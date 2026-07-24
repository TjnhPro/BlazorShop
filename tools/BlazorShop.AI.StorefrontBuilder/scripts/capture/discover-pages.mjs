import fs from 'node:fs/promises';

const args = new Map();
for (let index = 2; index < process.argv.length; index += 2) {
  args.set(process.argv[index], process.argv[index + 1]);
}

const input = args.get('--input');
const output = args.get('--output') ?? 'page-inventory.yaml';
if (!input) {
  throw new Error('Usage: node discover-pages.mjs --input <urls.json> --output <page-inventory.yaml>');
}

const urls = JSON.parse(await fs.readFile(input, 'utf8'));
const archetypes = [
  { id: 'home', test: (url) => new URL(url).pathname === '/' },
  { id: 'catalog', test: (url) => /category|collection|catalog/i.test(url) },
  { id: 'product', test: (url) => /product|products|p\//i.test(url) },
  { id: 'search', test: (url) => /search|q=/i.test(url) },
  { id: 'cart', test: (url) => /cart/i.test(url) },
  { id: 'checkout', test: (url) => /checkout/i.test(url) },
  { id: 'account', test: (url) => /login|signin|account/i.test(url) },
  { id: 'content', test: (url) => /about|contact|pages|content/i.test(url) }
];

const pages = [];
const seen = new Set();
for (const item of urls) {
  const url = typeof item === 'string' ? item : item.url;
  const discoveredBy = typeof item === 'string' ? 'seed' : item.discoveredBy ?? 'seed';
  const match = archetypes.find((archetype) => archetype.test(url));
  const archetype = match?.id ?? 'content';
  if (seen.has(archetype)) {
    continue;
  }

  seen.add(archetype);
  pages.push({
    archetype,
    url,
    evidencePath: `docs/storefront-analysis/evidence/${archetype}`,
    confidence: match ? 0.9 : 0.55,
    reason: match ? `Matched ${archetype} URL pattern from ${discoveredBy}.` : `Fallback content archetype from ${discoveredBy}.`,
    discoveredBy
  });
}

const lines = [
  'schemaVersion: 1',
  'artifactKind: page-inventory',
  'artifactId: page-inventory.generated',
  'pages:'
];
for (const page of pages) {
  lines.push(`  - archetype: ${page.archetype}`);
  lines.push(`    url: ${page.url}`);
  lines.push(`    evidencePath: ${page.evidencePath}`);
  lines.push(`    confidence: ${page.confidence}`);
  lines.push(`    reason: ${page.reason}`);
  lines.push(`    discoveredBy: ${page.discoveredBy}`);
}

await fs.writeFile(output, `${lines.join('\n')}\n`, 'utf8');
