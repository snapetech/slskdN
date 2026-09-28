// <copyright file="player.spec.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as fs from 'node:fs/promises';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import * as path from 'node:path';
import { MultiPeerHarness } from './harness/MultiPeerHarness';
import { login } from './helpers';
import { expect, test } from '@playwright/test';

// Generated PCM exercises the browser decoder without downloaded media,
// personal files, or remote peer requests.
function makeTone(seconds = 40): Buffer {
  const sampleRate = 22050;
  const samples = sampleRate * seconds;
  const wave = Buffer.alloc(44 + samples * 2);
  wave.write('RIFF', 0);
  wave.writeUInt32LE(wave.length - 8, 4);
  wave.write('WAVEfmt ', 8);
  wave.writeUInt32LE(16, 16);
  wave.writeUInt16LE(1, 20);
  wave.writeUInt16LE(1, 22);
  wave.writeUInt32LE(sampleRate, 24);
  wave.writeUInt32LE(sampleRate * 2, 28);
  wave.writeUInt16LE(2, 32);
  wave.writeUInt16LE(16, 34);
  wave.write('data', 36);
  wave.writeUInt32LE(samples * 2, 40);
  for (let index = 0; index < samples; index++) {
    wave.writeInt16LE(Math.round(Math.sin(index * 2 * Math.PI * 440 / sampleRate) * 1600), 44 + index * 2);
  }
  return wave;
}

test.describe('player browser playback', () => {
  test.use({ serviceWorkers: 'block' });
  test.setTimeout(60_000);
  let harness: MultiPeerHarness;
  let fixtureDirectory: string;
  let firstFile: string;
  let secondFile: string;

  test.beforeAll(async () => {
    fixtureDirectory = await fs.mkdtemp(path.resolve('../../test-data/slskdn-test-fixtures/music/player-runtime-'));
    firstFile = path.join(fixtureDirectory, 'Player runtime first.wav');
    secondFile = path.join(fixtureDirectory, 'Player runtime second.wav');
    await fs.writeFile(firstFile, makeTone());
    await fs.writeFile(secondFile, makeTone(41));
    await promisify(execFile)('ffmpeg', ['-hide_banner', '-loglevel', 'error', '-i', firstFile, '-c:a', 'pcm_s16be', path.join(fixtureDirectory, 'Player decoded runtime.aiff')]);
    harness = new MultiPeerHarness();
    await harness.startNode('A', 'test-data/slskdn-test-fixtures/music', { noConnect: true });
    await fs.writeFile(path.join(harness.getNode('A').getAppDir(), 'downloads', 'Downloaded runtime.wav'), makeTone(42));
    await fs.writeFile(path.join(harness.getNode('A').getAppDir(), 'downloads', 'Downloaded runtime second.wav'), makeTone(43));
  });

  test.afterAll(async () => {
    if (harness) await harness.stopAll();
    if (fixtureDirectory) await fs.rm(fixtureDirectory, { recursive: true, force: true });
  });

  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      const audioWindow = window as Window & { __playerAudioContexts?: number };
      audioWindow.__playerAudioContexts = 0;
      const original = window.AudioContext;
      window.AudioContext = new Proxy(original, {
        construct(target, argumentsList, newTarget) {
          audioWindow.__playerAudioContexts! += 1;
          return Reflect.construct(target, argumentsList, newTarget);
        },
      });
      if (localStorage.getItem('slskdn.player.collapsed') === null) {
        localStorage.setItem('slskdn.player.collapsed', 'false');
      }
    });
    await login(page, harness.getNode('A').nodeCfg);
  });

  test('plays local PCM, pauses, seeks, remounts and advances the queue', async ({ page }) => {
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles([firstFile, secondFile]);
    const activeAudio = page.locator('audio');
    await expect.poll(() => activeAudio.evaluateAll((elements) => elements.some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(() => activeAudio.evaluateAll((elements) => elements.every((element) => element.paused))).toBe(true);
    await page.getByLabel('Seek playback', { exact: true }).press('Home');
    for (let second = 0; second < 12; second++) {
      await page.getByLabel('Seek playback', { exact: true }).press('ArrowRight');
    }
    await expect.poll(() => activeAudio.evaluateAll((elements) => Math.max(...elements.map((element) => element.currentTime)))).toBeGreaterThanOrEqual(12);
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(() => activeAudio.evaluateAll((elements) => elements.some((element) => !element.paused && element.currentTime > 12.2))).toBe(true);
    await page.getByTestId('player-collapse').click();
    await expect.poll(() => activeAudio.evaluateAll((elements) => elements.some((element) => !element.paused && element.currentTime > 12))).toBe(true);
    await page.getByRole('button', { name: 'Next local track', exact: true }).click();
    await expect(page.locator('.player-title')).toHaveText('Player runtime second');
    expect(await page.evaluate(() => (window as Window & { __playerAudioContexts?: number }).__playerAudioContexts)).toBe(0);
    await expect.poll(() => activeAudio.evaluateAll((elements) => elements.some((element) => !element.paused && element.currentTime > 0.2 && element.currentTime < 5))).toBe(true);
  });

  for (const selection of ['direct', 'queue']) {
    test(`switches between unindexed downloads through ${selection} without a scan cooldown failure`, async ({ page }) => {
      await page.getByTestId('player-open-file-browser').click();
      const modal = page.getByTestId('player-file-browser-modal');
      await modal.getByTestId('player-file-browser-search').locator('input').fill('Downloaded runtime');
      await modal.getByRole('button', { name: 'Play Downloaded runtime.wav', exact: true }).click();
      await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
        elements.some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
      await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
      await page.getByTestId('player-open-file-browser').click();
      await modal.getByTestId('player-file-browser-search').locator('input').fill('Downloaded runtime');
      if (selection === 'queue') {
        await modal.getByRole('button', { name: 'Play Downloaded runtime second.wav next', exact: true }).click();
        await modal.getByRole('button', { name: 'Close', exact: true }).click();
        await page.getByTestId('player-next').click();
      } else {
        await modal.getByRole('button', { name: 'Play Downloaded runtime second.wav', exact: true }).click();
      }
      await expect(page.locator('.player-title')).toHaveText('Downloaded runtime second.wav');
      await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
        elements.some((element) => !element.paused && element.currentTime > 0.2)), { timeout: 3_000 }).toBe(true);
    });
  }

  test('decodes server AIFF and seeks absolutely while paused and playing', async ({ page }) => {
    await page.getByTestId('player-open-file-browser').click();
    const modal = page.getByTestId('player-file-browser-modal');
    await modal.getByTestId('player-file-browser-search').locator('input').fill('Player decoded runtime');
    await modal.getByRole('button', { name: 'Play Player decoded runtime.aiff', exact: true }).click();
    await page.getByRole('button', { name: 'Decode for playback', exact: true }).click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.some((element) =>
      !element.paused && element.currentTime > 0.2 && element.currentSrc.includes('/transcoded')))).toBe(true);
    await page.getByTestId('player-toggle-playback').click();
    const seek = page.getByLabel('Seek playback', { exact: true });
    await seek.press('Home');
    for (let second = 0; second < 12; second++) await seek.press('ArrowRight');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.some((element) =>
      element.currentSrc.includes('startSeconds=12')))).toBe(true);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.every((element) => element.paused))).toBe(true);
    await expect(seek).toHaveValue('12');
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.some((element) =>
      !element.paused && element.currentTime > 0.2 && element.currentSrc.includes('startSeconds=12')))).toBe(true);
    await seek.press('Home');
    for (let second = 0; second < 5; second++) await seek.press('ArrowRight');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.some((element) =>
      !element.paused && element.currentTime > 0.2 && element.currentSrc.includes('startSeconds=5')))).toBe(true);
  });

  test('records native playback and paused browser resource use', async ({ page }, testInfo) => {
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles(firstFile);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
      elements.some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    const session = await page.context().newCDPSession(page);
    await session.send('Performance.enable');
    const metrics = async () => {
      const result = await session.send('Performance.getMetrics');
      return Object.fromEntries(result.metrics.map(({ name, value }) => [name, value]));
    };
    const samples = [];
    for (const state of ['playing', 'paused']) {
      if (state === 'paused') await page.getByTestId('player-toggle-playback').click();
      const before = await metrics();
      await page.waitForTimeout(5_000);
      const after = await metrics();
      samples.push({
        state,
        seconds: after.Timestamp - before.Timestamp,
        rendererTaskPercent: (after.TaskDuration - before.TaskDuration) / (after.Timestamp - before.Timestamp) * 100,
        scriptPercent: (after.ScriptDuration - before.ScriptDuration) / (after.Timestamp - before.Timestamp) * 100,
        jsHeapMiB: after.JSHeapUsedSize / 1024 / 1024,
        audioContexts: await page.evaluate(() => (window as Window & { __playerAudioContexts?: number }).__playerAudioContexts),
      });
    }
    await session.detach();
    expect(samples.every((sample) => sample.audioContexts === 0)).toBe(true);
    await testInfo.attach('player-native-resources', {
      body: Buffer.from(JSON.stringify(samples, null, 2)),
      contentType: 'application/json',
    });
    await fs.writeFile(testInfo.outputPath('native-resources.json'), JSON.stringify(samples, null, 2));
  });

  test('crossfades two local streams and pauses both on transport Pause', async ({ page }) => {
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles([firstFile, secondFile]);
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    await page.getByTestId('player-toggle-crossfade').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    const seek = page.getByLabel('Seek playback', { exact: true });
    await seek.press('Home');
    for (let second = 0; second < 34; second++) await seek.press('ArrowRight');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.filter((element) => !element.paused).length)).toBe(2);
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.every((element) => element.paused))).toBe(true);
  });

  test('keeps compact and expanded controls within desktop and narrow viewports', async ({ page }, testInfo) => {
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles(firstFile);
    for (const width of [1440, 768, 390, 320]) {
      await page.setViewportSize({ width, height: 900 });
      await expect(page.getByTestId('player-toggle-playback')).toBeInViewport();
      if (width <= 720) {
        const touchBounds = await page.getByTestId('player-toggle-playback').boundingBox();
        expect(touchBounds!.width).toBeGreaterThanOrEqual(44);
        expect(touchBounds!.height).toBeGreaterThanOrEqual(44);
      }
      const bar = page.locator('.player-bar');
      const bounds = await bar.boundingBox();
      expect(bounds).not.toBeNull();
      expect(bounds!.x).toBeGreaterThanOrEqual(0);
      expect(bounds!.x + bounds!.width).toBeLessThanOrEqual(width + 1);
      await page.screenshot({ path: testInfo.outputPath(`expanded-${width}.png`) });
      await page.getByTestId('player-collapse').click();
      await expect(page.getByTestId('player-collapsed-toggle-playback')).toBeInViewport();
      await expect(page.getByTestId('player-expand')).toBeInViewport();
      if (width <= 420) {
        await expect(page.getByTestId('player-collapsed-toggle-mute')).toBeHidden();
        const titleBounds = await page.locator('.player-title').boundingBox();
        expect(titleBounds!.width).toBeGreaterThanOrEqual(72);
      }
      const overflow = await bar.evaluate((element) => element.scrollWidth - element.clientWidth);
      expect(overflow).toBeLessThanOrEqual(1);
      await page.screenshot({ path: testInfo.outputPath(`compact-${width}.png`) });
      await page.getByTestId('player-expand').click();
    }
  });

  for (const filename of ['Player runtime first.wav', 'Downloaded runtime.wav']) {
    test(`streams ${filename} and restores its queue without autoplay`, async ({ page }) => {
      await page.getByTestId('player-open-file-browser').click();
      const modal = page.getByTestId('player-file-browser-modal');
      await modal.getByTestId('player-file-browser-search').locator('input').fill(filename.replace('.wav', ''));
      const play = modal.getByRole('button', { name: `Play ${filename}`, exact: true });
      await expect(play).toBeVisible({ timeout: 10_000 });
      await play.click();
      await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.some((element) => !element.paused && element.currentTime > 0.2 && element.currentSrc.includes('/streams/')))).toBe(true);
      await page.reload({ waitUntil: 'domcontentloaded' });
      await expect(page.locator('.player-title')).toHaveText(filename);
      await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.every((element) => element.paused))).toBe(true);
      await page.getByTestId('player-toggle-playback').click();
      await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    });
  }
});
