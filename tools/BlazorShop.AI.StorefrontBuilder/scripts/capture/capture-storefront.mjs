import { chromium } from '@playwright/test';
import fs from 'node:fs/promises';
import path from 'node:path';

const args = new Map();
for (let index = 2; index < process.argv.length; index += 2) {
  args.set(process.argv[index], process.argv[index + 1]);
}

const url = args.get('--url');
const outputRoot = args.get('--outputRoot');
if (!url || !outputRoot) {
  throw new Error('Usage: node capture-storefront.mjs --url <http-url> --outputRoot <folder>');
}

const viewports = [
  { id: 'desktop-1440', width: 1440, height: 1000 },
  { id: 'tablet-768', width: 768, height: 1000 },
  { id: 'mobile-390', width: 390, height: 900 }
];

await fs.mkdir(outputRoot, { recursive: true });
const browser = await chromium.launch();
const evidence = [];

try {
  for (const viewport of viewports) {
    const page = await browser.newPage({ viewport });
    await page.goto(url, { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('networkidle').catch(() => undefined);
    await page.hover('body').catch(() => undefined);
    await page.focus('body').catch(() => undefined);
    await page.mouse.wheel(0, 600);
    await page.waitForTimeout(100);
    await page.mouse.wheel(0, -600);
    await page.click('body').catch(() => undefined);

    const screenshotFile = `${viewport.id}.png`;
    const domFile = `${viewport.id}.dom.html`;
    const styleFile = `${viewport.id}.styles.json`;
    const boxesFile = `${viewport.id}.boxes.json`;
    const assetsFile = `${viewport.id}.assets.json`;

    await page.screenshot({ path: path.join(outputRoot, screenshotFile), fullPage: true });
    await fs.writeFile(path.join(outputRoot, domFile), await page.content(), 'utf8');

    const styles = await page.evaluate(() => {
      return Array.from(document.querySelectorAll('body, header, nav, main, footer, h1, h2, a, button, input, select'))
        .slice(0, 80)
        .map((element) => {
          const computed = getComputedStyle(element);
          return {
            selector: element.tagName.toLowerCase(),
            color: computed.color,
            backgroundColor: computed.backgroundColor,
            fontFamily: computed.fontFamily,
            fontSize: computed.fontSize,
            fontWeight: computed.fontWeight,
            lineHeight: computed.lineHeight,
            display: computed.display
          };
        });
    });

    const boxes = await page.evaluate(() => {
      return Array.from(document.querySelectorAll('header, nav, main, footer, section, article, img, button'))
        .slice(0, 120)
        .map((element) => {
          const rect = element.getBoundingClientRect();
          return {
            selector: element.tagName.toLowerCase(),
            x: rect.x,
            y: rect.y,
            width: rect.width,
            height: rect.height
          };
        });
    });

    const assets = await page.evaluate(() => {
      const urls = new Set();
      document.querySelectorAll('img[src], source[srcset], link[href], script[src]').forEach((element) => {
        urls.add(element.getAttribute('src') ?? element.getAttribute('srcset') ?? element.getAttribute('href'));
      });
      return Array.from(urls).filter(Boolean);
    });

    await fs.writeFile(path.join(outputRoot, styleFile), JSON.stringify(styles, null, 2), 'utf8');
    await fs.writeFile(path.join(outputRoot, boxesFile), JSON.stringify(boxes, null, 2), 'utf8');
    await fs.writeFile(path.join(outputRoot, assetsFile), JSON.stringify(assets, null, 2), 'utf8');

    evidence.push({
      id: viewport.id,
      url,
      timestampUtc: new Date().toISOString(),
      viewport,
      browser: 'chromium',
      screenshotFile,
      domSnapshotFile: domFile,
      computedStyleSampleFile: styleFile,
      boundingBoxesFile: boxesFile,
      assetListFile: assetsFile,
      interactionState: 'navigate,wait,resize,scroll,click,hover,focus'
    });

    await page.close();
  }
} finally {
  await browser.close();
}

await fs.writeFile(path.join(outputRoot, 'capture-manifest.json'), JSON.stringify({ url, evidence }, null, 2), 'utf8');
