import { test, expect } from '@playwright/test';
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawn } from 'node:child_process';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const toolRoot = path.resolve(__dirname, '../..');

test('captures static fixture at desktop tablet and mobile viewports', async () => {
  const outputRoot = path.join(toolRoot, 'tests/playwright/.capture-output');
  await fs.rm(outputRoot, { recursive: true, force: true });

  const fixtureUrl = `file://${path.join(__dirname, 'fixtures/static-storefront.html')}`;
  await new Promise((resolve, reject) => {
    const child = spawn(
      process.execPath,
      [
        path.join(toolRoot, 'scripts/capture/capture-storefront.mjs'),
        '--url',
        fixtureUrl,
        '--outputRoot',
        outputRoot
      ],
      { stdio: 'inherit' });

    child.on('exit', (code) => code === 0 ? resolve() : reject(new Error(`capture exited ${code}`)));
  });

  const manifest = JSON.parse(await fs.readFile(path.join(outputRoot, 'capture-manifest.json'), 'utf8'));
  expect(manifest.evidence.map((entry) => entry.id)).toEqual(['desktop-1440', 'tablet-768', 'mobile-390']);
  for (const entry of manifest.evidence) {
    await expect(path.join(outputRoot, entry.screenshotFile)).toBeTruthy();
    await fs.access(path.join(outputRoot, entry.domSnapshotFile));
    await fs.access(path.join(outputRoot, entry.computedStyleSampleFile));
    await fs.access(path.join(outputRoot, entry.boundingBoxesFile));
    await fs.access(path.join(outputRoot, entry.assetListFile));
  }
});
