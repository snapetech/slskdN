// <copyright file="player.spec.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as fs from 'node:fs/promises';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import * as path from 'node:path';
import { MultiPeerHarness } from './harness/MultiPeerHarness';
import { getAuthToken, login } from './helpers';
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
      const audioWindow = window as Window & { __playerAudioContexts?: number; __playerAudioContextInstances?: AudioContext[] };
      audioWindow.__playerAudioContexts = 0;
      audioWindow.__playerAudioContextInstances = [];
      const original = window.AudioContext;
      window.AudioContext = new Proxy(original, {
        construct(target, argumentsList, newTarget) {
          audioWindow.__playerAudioContexts! += 1;
          const context = Reflect.construct(target, argumentsList, newTarget);
          audioWindow.__playerAudioContextInstances!.push(context);
          return context;
        },
      });
      if (localStorage.getItem('slskdn.player.collapsed') === null) {
        localStorage.setItem('slskdn.player.collapsed', 'false');
      }
    });
    await login(page, harness.getNode('A').nodeCfg);
  });

  test('keeps browser media metadata and transport actions synchronized', async ({ page }) => {
    await page.addInitScript(() => {
      const mediaWindow = window as Window & {
        __playerMediaActions?: Record<string, MediaSessionActionHandler | null>;
        __playerMediaPosition?: MediaPositionState;
      };
      mediaWindow.__playerMediaActions = {};
      const session = navigator.mediaSession;
      const setAction = session.setActionHandler.bind(session);
      session.setActionHandler = (action, handler) => {
        setAction(action, handler);
        mediaWindow.__playerMediaActions![action] = handler;
      };
      const setPosition = session.setPositionState.bind(session);
      session.setPositionState = (state) => {
        setPosition(state);
        mediaWindow.__playerMediaPosition = state;
      };
    });
    await page.reload();
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles([firstFile, secondFile]);
    const audio = page.locator('audio');
    await expect.poll(() => audio.evaluateAll((elements) => elements.some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    expect(await page.evaluate(() => navigator.mediaSession.metadata?.title)).toBe('Player runtime first');
    expect(await page.evaluate(() => navigator.mediaSession.playbackState)).toBe('playing');
    const invoke = async (action: MediaSessionAction, details: Partial<MediaSessionActionDetails> = {}) => {
      await page.evaluate(({ action, details }) => {
        const handlers = (window as Window & { __playerMediaActions?: Record<string, MediaSessionActionHandler | null> }).__playerMediaActions;
        if (!handlers?.[action]) throw new Error(`Missing Media Session action: ${action}`);
        handlers[action]!({ action, ...details });
      }, { action, details });
    };
    await invoke('pause');
    await expect.poll(() => audio.evaluateAll((elements) => elements.every((element) => element.paused))).toBe(true);
    expect(await page.evaluate(() => navigator.mediaSession.playbackState)).toBe('paused');
    await invoke('seekto', { seekTime: 12 });
    await expect.poll(() => audio.evaluateAll((elements) => Math.max(...elements.map((element) => element.currentTime)))).toBeGreaterThanOrEqual(12);
    await expect.poll(() => page.evaluate(() => (window as Window & { __playerMediaPosition?: MediaPositionState }).__playerMediaPosition?.position)).toBeGreaterThanOrEqual(12);
    await invoke('seekbackward', { seekOffset: 5 });
    await expect.poll(() => audio.evaluateAll((elements) => Math.max(...elements.map((element) => element.currentTime)))).toBe(7);
    await invoke('seekforward', { seekOffset: 3 });
    await expect.poll(() => audio.evaluateAll((elements) => Math.max(...elements.map((element) => element.currentTime)))).toBe(10);
    await invoke('play');
    await expect.poll(() => audio.evaluateAll((elements) => elements.some((element) => !element.paused && element.currentTime > 10))).toBe(true);
    await invoke('nexttrack');
    await expect.poll(() => page.evaluate(() => navigator.mediaSession.metadata?.title)).toBe('Player runtime second');
    await invoke('previoustrack');
    await expect.poll(() => page.evaluate(() => navigator.mediaSession.metadata?.title)).toBe('Player runtime first');
    await invoke('stop');
    await expect.poll(() => audio.evaluateAll((elements) => elements.every((element) => element.paused && !element.getAttribute('src')))).toBe(true);
    expect(await page.evaluate(() => navigator.mediaSession.metadata)).toBeNull();
    expect(await page.evaluate(() => navigator.mediaSession.playbackState)).toBe('none');
    expect(await page.evaluate(() => (window as Window & { __playerMediaPosition?: MediaPositionState }).__playerMediaPosition)).toBeUndefined();
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

  test('saves a queue, reloads its playlist and preserves repeated server entries', async ({ page }) => {
    await page.getByTestId('player-open-file-browser').click();
    const files = page.getByTestId('player-file-browser-modal');
    await files.getByTestId('player-file-browser-search').locator('input').fill('Player runtime first');
    await files.getByRole('button', { name: 'Play Player runtime first.wav', exact: true }).click();
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    await page.getByTestId('player-open-file-browser').click();
    await files.getByTestId('player-file-browser-search').locator('input').fill('Player runtime second');
    await files.getByRole('button', { name: 'Play Player runtime second.wav next', exact: true }).click();
    await files.getByRole('button', { name: 'Close', exact: true }).click();
    await page.getByTestId('player-open-queue').click();
    const queue = page.locator('.player-queue-modal');
    await queue.getByLabel('New playlist name').fill('Player runtime playlist');
    const created = page.waitForResponse((response) => response.request().method() === 'POST' &&
      response.url().endsWith('/api/v0/collections'));
    await queue.getByRole('button', { name: 'Save queue', exact: true }).click();
    const response = await created;
    expect(response.status()).toBe(201);
    const playlist = await response.json();
    await expect(queue).toContainText('Saved 2 tracks to Player runtime playlist.');
    const baseUrl = harness.getNode('A').nodeCfg.baseUrl;
    const headers = { Authorization: `Bearer ${await getAuthToken(page)}` };
    const saved = await page.request.get(`${baseUrl}/api/v0/collections/${playlist.id}/items`, { headers });
    const items = await saved.json();
    expect(items).toHaveLength(2);
    const repeated = await page.request.post(`${baseUrl}/api/v0/collections/${playlist.id}/items`, {
      headers,
      data: { contentId: items[0].contentId, title: items[0].title, fileName: items[0].fileName, mediaKind: 'Audio' },
    });
    expect(repeated.status()).toBe(201);
    await queue.getByLabel('Saved playlist', { exact: true }).selectOption(playlist.id);
    await queue.getByRole('button', { name: 'Load', exact: true }).click();
    await expect(queue).toContainText('Loaded 3 tracks.');
    await expect(queue.locator('.player-queue-manager-list .player-queue-manager-row')).toHaveCount(2);
    await queue.getByRole('button', { name: 'Done', exact: true }).click();
    await expect(page.locator('.player-title')).toHaveText('Player runtime first.wav');
    const seek = page.getByLabel('Seek playback', { exact: true });
    await seek.press('Home');
    for (let second = 0; second < 12; second++) await seek.press('ArrowRight');
    await page.getByTestId('player-toggle-playback').click();
    await page.reload({ waitUntil: 'domcontentloaded' });
    await expect(page.locator('.player-title')).toHaveText('Player runtime first.wav');
    await expect.poll(async () => Number(await page.getByLabel('Seek playback', { exact: true }).inputValue())).toBeGreaterThanOrEqual(12);
    await page.getByTestId('player-toggle-playback').click();
    await page.getByTestId('player-next').click();
    await expect(page.locator('.player-title')).toHaveText('Player runtime second.wav');
    await page.getByTestId('player-previous').click();
    await expect(page.locator('.player-title')).toHaveText('Player runtime first.wav');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
      elements.some((element) => !element.paused && element.currentTime > 0.2 && element.currentTime < 2))).toBe(true);
    await page.getByTestId('player-next').click();
    await expect(page.locator('.player-title')).toHaveText('Player runtime second.wav');
    await page.getByTestId('player-next').click();
    await expect(page.locator('.player-title')).toHaveText('Player runtime first.wav');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
      elements.some((element) => !element.paused && element.currentTime > 0.2 && element.currentTime < 5))).toBe(true);
    await page.reload({ waitUntil: 'domcontentloaded' });
    await expect(page.locator('.player-title')).toHaveText('Player runtime first.wav');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.every((element) => element.paused))).toBe(true);
  });

  test('keeps identical browser-local files as separate queue entries', async ({ page }) => {
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles([firstFile, firstFile]);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
      elements.some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    const firstSource = await page.locator('audio').evaluateAll((elements) => elements.find((element) => !element.paused)!.currentSrc);
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    await page.getByTestId('player-open-queue').click();
    const queue = page.locator('.player-queue-modal');
    await expect(queue.locator('.player-queue-manager-list .player-queue-manager-row')).toHaveCount(1);
    await queue.getByRole('button', { name: 'Done', exact: true }).click();
    await page.getByTestId('player-next').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements, previousSource) =>
      elements.some((element) => !element.paused && element.currentTime > 0.2 && element.currentSrc !== previousSource), firstSource)).toBe(true);
    await expect(page.locator('.player-title')).toHaveText('Player runtime first');
  });

  test('records idle, native playback and paused browser resource use', async ({ page }, testInfo) => {
    const session = await page.context().newCDPSession(page);
    await session.send('Performance.enable');
    const browserSession = await page.context().browser()!.newBrowserCDPSession();
    const metrics = async () => {
      const result = await session.send('Performance.getMetrics');
      return Object.fromEntries(result.metrics.map(({ name, value }) => [name, value]));
    };
    const samples = [];
    for (const state of ['idle', 'playing', 'paused']) {
      if (state === 'playing') {
        await page.getByLabel('Choose audio files', { exact: true }).setInputFiles(firstFile);
        await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
      }
      if (state === 'paused') await page.getByTestId('player-toggle-playback').click();
      const before = await metrics();
      const processesBefore = await browserSession.send('SystemInfo.getProcessInfo');
      await page.waitForTimeout(10_000);
      const after = await metrics();
      const processesAfter = await browserSession.send('SystemInfo.getProcessInfo');
      const cpuById = new Map(processesBefore.processInfo.map((entry) => [entry.id, entry.cpuTime]));
      const sampledProcesses = processesAfter.processInfo.filter((entry) => cpuById.has(entry.id));
      const browserCpuSeconds = sampledProcesses.reduce((total, entry) => total + Math.max(0, entry.cpuTime - cpuById.get(entry.id)!), 0);
      // Linux PSS apportions shared mappings, unlike summing each process's RSS.
      const memoryReadings = process.platform === 'linux' ? await Promise.allSettled(
        processesAfter.processInfo.map(async (entry) => {
          const status = await fs.readFile(`/proc/${entry.id}/smaps_rollup`, 'utf8');
          const pss = status.match(/^Pss:\s+(\d+) kB$/mu);
          if (!pss) throw new Error('PSS unavailable');
          return Number(pss[1]) / 1024;
        }),
      ) : [];
      const measuredMemory = memoryReadings.filter((entry): entry is PromiseFulfilledResult<number> => entry.status === 'fulfilled');
      samples.push({
        state,
        browserPssMiB: measuredMemory.length > 0 ? measuredMemory.reduce((total, entry) => total + entry.value, 0) : null,
        memoryProcessesMeasured: measuredMemory.length,
        memoryProcessesUnavailable: memoryReadings.length - measuredMemory.length,
        memoryMeasurementSupported: process.platform === 'linux',
        seconds: after.Timestamp - before.Timestamp,
        browserCpuPercentOfOneCore: browserCpuSeconds / (after.Timestamp - before.Timestamp) * 100,
        browserProcessesSampled: sampledProcesses.length,
        browserProcessesBefore: processesBefore.processInfo.length,
        browserProcessesAfter: processesAfter.processInfo.length,
        rendererTaskPercent: (after.TaskDuration - before.TaskDuration) / (after.Timestamp - before.Timestamp) * 100,
        scriptPercent: (after.ScriptDuration - before.ScriptDuration) / (after.Timestamp - before.Timestamp) * 100,
        jsHeapMiB: after.JSHeapUsedSize / 1024 / 1024,
        audioContexts: await page.evaluate(() => (window as Window & { __playerAudioContexts?: number }).__playerAudioContexts),
      });
    }
    await session.detach();
    await browserSession.detach();
    expect(samples.every((sample) => sample.browserProcessesSampled > 0)).toBe(true);
    expect(samples.every((sample) => sample.audioContexts === 0)).toBe(true);
    await testInfo.attach('player-native-resources', {
      body: Buffer.from(JSON.stringify(samples, null, 2)),
      contentType: 'application/json',
    });
    await fs.writeFile(testInfo.outputPath('native-resources.json'), JSON.stringify(samples, null, 2));
  });

  test('stops analyzer sampling while paused and resumes it on playback', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('slskdn.player.visualTileMode', 'spectrum');
      const audioWindow = window as Window & { __playerAnalyzerReads?: number };
      audioWindow.__playerAnalyzerReads = 0;
      const read = AnalyserNode.prototype.getByteFrequencyData;
      AnalyserNode.prototype.getByteFrequencyData = function (array) {
        audioWindow.__playerAnalyzerReads! += 1;
        return read.call(this, array);
      };
    });
    await page.reload();
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles(firstFile);
    const reads = () => page.evaluate(() => (window as Window & { __playerAnalyzerReads?: number }).__playerAnalyzerReads!);
    await expect.poll(reads).toBeGreaterThan(5);
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.every((element) => element.paused))).toBe(true);
    await page.waitForTimeout(250);
    const pausedReads = await reads();
    await page.waitForTimeout(750);
    expect(await reads()).toBe(pausedReads);
    await expect.poll(() => page.evaluate(() => (window as Window & { __playerAudioContextInstances?: AudioContext[] }).__playerAudioContextInstances!.every((context) => context.state === 'suspended'))).toBe(true);
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(reads).toBeGreaterThan(pausedReads + 5);
    await page.getByTestId('player-stop').click();
    await page.waitForTimeout(250);
    const stoppedReads = await reads();
    await page.waitForTimeout(750);
    expect(await reads()).toBe(stoppedReads);
    await expect.poll(() => page.evaluate(() => (window as Window & { __playerAudioContextInstances?: AudioContext[] }).__playerAudioContextInstances!.every((context) => context.state === 'suspended'))).toBe(true);
  });

  test('opens a real Picture-in-Picture analyzer and closes it on Stop or hide', async ({ page }, testInfo) => {
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles(firstFile);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    await expect(page.getByTestId('player-document-pip')).toBeEnabled();
    await page.getByTestId('player-document-pip').click();
    const pipExists = () => page.evaluate(() => Boolean((window as Window & { documentPictureInPicture?: { window: Window | null } }).documentPictureInPicture?.window));
    await expect.poll(pipExists).toBe(true);
    await expect.poll(() => page.evaluate(() => {
      const pip = (window as Window & { documentPictureInPicture?: { window: Window | null } }).documentPictureInPicture?.window;
      const canvas = pip?.document.querySelector('canvas');
      if (!canvas) return false;
      const pixels = canvas.getContext('2d')!.getImageData(0, 0, canvas.width, canvas.height).data;
      return pixels.some((value, index) => index % 4 !== 3 && value > 100);
    })).toBe(true);
    const png = await page.evaluate(() => (window as Window & { documentPictureInPicture: { window: Window } }).documentPictureInPicture.window.document.querySelector('canvas')!.toDataURL());
    await fs.writeFile(testInfo.outputPath('picture-in-picture.png'), Buffer.from(png.split(',')[1], 'base64'));
    await page.getByTestId('player-stop').click();
    await expect.poll(pipExists).toBe(false);
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles(firstFile);
    await page.getByTestId('player-document-pip').click();
    await expect.poll(pipExists).toBe(true);
    await page.getByTestId('player-hide').click();
    await expect.poll(pipExists).toBe(false);
  });

  test('suspends the outgoing graph when a crossfade finishes naturally', async ({ page }) => {
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles([firstFile, secondFile]);
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    await page.getByTestId('player-toggle-crossfade').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    const seek = page.getByLabel('Seek playback', { exact: true });
    await seek.press('Home');
    for (let second = 0; second < 34; second++) await seek.press('ArrowRight');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.filter((element) => !element.paused).length)).toBe(2);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.filter((element) => !element.paused).length)).toBe(1);
    await expect.poll(() => page.evaluate(() => (window as Window & { __playerAudioContextInstances?: AudioContext[] }).__playerAudioContextInstances!.map((context) => context.state).sort())).toEqual(['running', 'suspended']);
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
    await expect.poll(() => page.evaluate(() => (window as Window & { __playerAudioContextInstances?: AudioContext[] }).__playerAudioContextInstances!.every((context) => context.state === 'suspended'))).toBe(true);
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.filter((element) => !element.paused).length)).toBe(1);
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
