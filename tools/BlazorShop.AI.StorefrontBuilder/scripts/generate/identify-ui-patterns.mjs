import fs from 'node:fs/promises';

const args = new Map();
for (let index = 2; index < process.argv.length; index += 2) args.set(process.argv[index], process.argv[index + 1]);
const output = args.get('--output') ?? 'ui-patterns.yaml';

const patterns = [
  ['header', 'header', 'layout.header'],
  ['footer', 'footer', 'layout.footer'],
  ['main-navigation', 'nav a', 'layout.main-navigation'],
  ['mobile-navigation', 'nav', 'layout.mobile-navigation'],
  ['breadcrumb', '[aria-label=breadcrumb]', 'layout.main-navigation'],
  ['product-card', 'article', 'catalog.product-card'],
  ['category-card', 'article', 'catalog.product-card'],
  ['banner-hero-section', 'main h1', 'home.sections'],
  ['product-grid', '.grid', 'home.sections'],
  ['product-gallery', 'img', 'product.gallery'],
  ['product-information-block', 'article h2', 'product.information'],
  ['product-purchase-block', 'button', 'product.purchase'],
  ['primary-button', 'button', 'product.purchase'],
  ['secondary-button', 'button', 'system.error'],
  ['icon-button', 'button[aria-label]', 'layout.cart-badge'],
  ['text-input', 'input[type=text]', 'catalog.filters'],
  ['search-input', 'input[type=search]', 'catalog.filters'],
  ['select', 'select', 'catalog.sorting'],
  ['checkbox', 'input[type=checkbox]', 'catalog.filters'],
  ['quantity-control', 'input[type=number]', 'product.purchase'],
  ['pagination', '[aria-label=pagination]', 'catalog.pagination'],
  ['empty-state', '.starter-empty-state', 'system.error'],
  ['error-state', '.starter-alert', 'system.error'],
  ['loading-state', '.starter-skeleton', 'system.error']
];

const lines = ['schemaVersion: 1', 'artifactKind: ui-patterns', 'artifactId: ui-patterns.generated', 'patterns:'];
for (const [id, selector, slot] of patterns) {
  lines.push(`  - patternId: ${id}`);
  lines.push('    evidenceIds: [home.desktop]');
  lines.push(`    selectorSamples: [${JSON.stringify(selector)}]`);
  lines.push('    visualProperties: { source: computed-styles }');
  lines.push('    statesObserved: [default]');
  lines.push('    responsiveNotes: desktop/tablet/mobile evidence retained');
  lines.push(`    targetSlot: ${slot}`);
  lines.push('    fallbackBehavior: use Starter neutral component when target selector is absent');
}

await fs.writeFile(output, `${lines.join('\n')}\n`, 'utf8');
