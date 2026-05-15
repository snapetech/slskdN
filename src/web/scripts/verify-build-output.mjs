import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const buildDir = path.resolve(scriptDir, '..', 'build');
const indexPath = path.join(buildDir, 'index.html');

function fail(message) {
  console.error(`ERROR: ${message}`);
  process.exit(1);
}

if (!fs.existsSync(indexPath)) {
  fail(`Missing built index.html at ${indexPath}`);
}

const html = fs.readFileSync(indexPath, 'utf8');

const requiredPatterns = [
  { pattern: /(?:src|href)="\/assets\//, reason: 'expected root-absolute built asset URLs so deep links do not resolve under client-side routes' },
  { pattern: /href="\.\/favicon\.ico"/, reason: 'expected relative favicon path for reverse-proxy subpaths' },
  { pattern: /href="\.\/manifest\.json"/, reason: 'expected relative manifest path for reverse-proxy subpaths' },
  { pattern: /href="\.\/logo192\.png"/, reason: 'expected relative icon path for reverse-proxy subpaths' },
];

const forbiddenPatterns = [
  { pattern: /(?:src|href)="\.\/assets\//, reason: 'route-relative assets break hard refreshes on client-side deep links' },
];

for (const { pattern, reason } of requiredPatterns) {
  if (!pattern.test(html)) {
    fail(`Built index.html is missing an expected path (${pattern}): ${reason}`);
  }
}

for (const { pattern, reason } of forbiddenPatterns) {
  if (pattern.test(html)) {
    fail(`Built index.html contains a forbidden path (${pattern}): ${reason}`);
  }
}

console.log('Verified built web output uses root-absolute Vite asset references.');
