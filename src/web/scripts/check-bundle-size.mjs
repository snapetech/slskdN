import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const assetsRoot = path.join(root, 'build', 'assets');
const baselinePath = path.join(root, 'performance', 'bundle-baseline.json');
const maxChunkBytes = Number(process.env.MAX_BUNDLE_CHUNK_KB ?? 600) * 1024;
const maxTotalBytes = Number(process.env.MAX_BUNDLE_TOTAL_MB ?? 8) * 1024 * 1024;

const collectFiles = (directory) => {
  if (!fs.existsSync(directory)) return [];
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const filePath = path.join(directory, entry.name);
    return entry.isDirectory() ? collectFiles(filePath) : [filePath];
  });
};

const assets = collectFiles(assetsRoot).filter((filePath) => /\.(?:js|css)$/.test(filePath));
if (assets.length === 0) {
  console.error(`No Vite assets found under ${assetsRoot}. Run pnpm --filter @slskdn/web build first.`);
  process.exit(1);
}

const sizes = assets.map((filePath) => ({ filePath, bytes: fs.statSync(filePath).size }));
const totalBytes = sizes.reduce((total, asset) => total + asset.bytes, 0);
const oversized = sizes.filter(
  (asset) =>
    asset.bytes > maxChunkBytes &&
    !path.basename(asset.filePath).startsWith('milkdrop-presets-')
);
const lazyLargeChunks = sizes.filter(
  (asset) =>
    asset.bytes > maxChunkBytes &&
    path.basename(asset.filePath).startsWith('milkdrop-presets-')
);
for (const asset of oversized) {
  console.error(
    `Bundle budget exceeded: ${path.relative(root, asset.filePath)} is ${(asset.bytes / 1024).toFixed(1)} KB (limit ${(maxChunkBytes / 1024).toFixed(1)} KB).`
  );
}
if (totalBytes > maxTotalBytes) {
  console.error(
    `Total bundle budget exceeded: ${(totalBytes / 1024 / 1024).toFixed(2)} MB (limit ${(maxTotalBytes / 1024 / 1024).toFixed(2)} MB).`
  );
}

if (process.argv.includes('--write-baseline')) {
  fs.mkdirSync(path.dirname(baselinePath), { recursive: true });
  fs.writeFileSync(
    baselinePath,
    `${JSON.stringify({ generatedAt: new Date().toISOString(), totalBytes }, null, 2)}\n`
  );
} else if (fs.existsSync(baselinePath)) {
  const baseline = JSON.parse(fs.readFileSync(baselinePath, 'utf8'));
  const maxGrowth = Number(process.env.MAX_BUNDLE_GROWTH ?? 0.1);
  if (baseline.totalBytes > 0 && totalBytes > baseline.totalBytes * (1 + maxGrowth)) {
    console.error(
      `Bundle growth budget exceeded: ${((totalBytes / baseline.totalBytes - 1) * 100).toFixed(1)}% growth (limit ${(maxGrowth * 100).toFixed(1)}%).`
    );
    process.exit(1);
  }
}

console.log(`Checked ${assets.length} assets (${(totalBytes / 1024 / 1024).toFixed(2)} MB total).`);
for (const asset of lazyLargeChunks) {
  console.log(
    `  ${(asset.bytes / 1024).toFixed(1)} KB ${path.relative(root, asset.filePath)} (lazy visualizer exception)`
  );
}
if (oversized.length > 0 || totalBytes > maxTotalBytes) process.exit(1);
