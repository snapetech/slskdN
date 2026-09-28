// <copyright file="player.spec.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as fs from 'node:fs/promises';
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
    harness = new MultiPeerHarness();
    await harness.startNode('A', 'test-data/slskdn-test-fixtures/music', { noConnect: true });
    await fs.writeFile(path.join(harness.getNode('A').getAppDir(), 'downloads', 'Downloaded runtime.wav'), makeTone(42));
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
