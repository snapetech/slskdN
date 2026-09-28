// <copyright file="player-resources.spec.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>
import * as fs from 'node:fs/promises';
import { expect, test } from '@playwright/test';
import { MultiPeerHarness } from './harness/MultiPeerHarness';
import { makeTone } from './fixtures/player-tone';
import { login } from './helpers';

// Video and trace are worker options; isolate measurement from functional QA.
test.use({ serviceWorkers: 'block', video: 'off', trace: 'off' });
const harness = new MultiPeerHarness();
test.beforeAll(async () => { await harness.startNode('A', [], { noConnect: true }); });
test.afterAll(async () => { await harness.stopAll(); });
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
    localStorage.setItem('slskdn.player.collapsed', 'false');
  });
  await login(page, harness.getNode('A').nodeCfg);
});

test('records idle, native playback and paused browser resource use', async ({ page }, testInfo) => {
  expect(page.video()).toBeNull();
  const windowSeconds = Number(process.env.SLSKDN_PLAYER_RESOURCE_WINDOW_SECONDS || 10);
  const windows = Number(process.env.SLSKDN_PLAYER_RESOURCE_WINDOWS || 1);
  expect(Number.isInteger(windowSeconds) && windowSeconds >= 10 && windowSeconds <= 60).toBe(true);
  expect(Number.isInteger(windows) && windows >= 1 && windows <= 10).toBe(true);
  const sustained = windowSeconds * windows > 10;
  const warmupSeconds = sustained ? 15 : 0;
  test.setTimeout(90_000 + 3 * (warmupSeconds + windows * windowSeconds) * 1_000);
  const session = await page.context().newCDPSession(page);
  await session.send('Performance.enable');
  const browserSession = await page.context().browser()!.newBrowserCDPSession();
  const metrics = async () => {
    const result = await session.send('Performance.getMetrics');
    return Object.fromEntries(result.metrics.map(({ name, value }) => [name, value]));
  };
  const playback = () => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[])
    .filter((audio) => audio.currentSrc)
    .map((audio) => ({ paused: audio.paused, ended: audio.ended, seconds: audio.currentTime })));
  const samples = [];
  try {
    for (const state of ['idle', 'playing', 'paused']) {
      if (state === 'playing') {
        const input = { name: 'Native playback.wav', mimeType: 'audio/wav',
          buffer: makeTone(sustained ? warmupSeconds + windows * windowSeconds + 60 : 40) };
        await page.getByLabel('Choose audio files', { exact: true }).setInputFiles(input);
        await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
      }
      if (state === 'paused') await page.getByTestId('player-toggle-playback').click();
      if (warmupSeconds) await page.waitForTimeout(warmupSeconds * 1_000);
      for (let sampleIndex = 0; sampleIndex < windows; sampleIndex++) {
        const playbackBefore = await playback();
        const before = await metrics();
        const processesBefore = await browserSession.send('SystemInfo.getProcessInfo');
        await page.waitForTimeout(windowSeconds * 1_000);
        const after = await metrics();
        const processesAfter = await browserSession.send('SystemInfo.getProcessInfo');
        const playbackAfter = await playback();
        const cpuById = new Map(processesBefore.processInfo.map((entry) => [entry.id, entry.cpuTime]));
        const afterIds = new Set(processesAfter.processInfo.map((entry) => entry.id));
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
          state, window: sampleIndex + 1, warmupSeconds,
          playbackBefore, playbackAfter,
          browserPssMiB: measuredMemory.length > 0 ? measuredMemory.reduce((total, entry) => total + entry.value, 0) : null,
          memoryProcessesMeasured: measuredMemory.length,
          memoryProcessesUnavailable: memoryReadings.length - measuredMemory.length,
          memoryMeasurementSupported: process.platform === 'linux',
          seconds: after.Timestamp - before.Timestamp,
          browserCpuPercentOfOneCore: browserCpuSeconds / (after.Timestamp - before.Timestamp) * 100,
          browserProcessesSampled: sampledProcesses.length,
          browserProcessesBefore: processesBefore.processInfo.length,
          browserProcessesAfter: processesAfter.processInfo.length,
          browserProcessesAdded: processesAfter.processInfo.filter((entry) => !cpuById.has(entry.id)).length,
          browserProcessesRemoved: processesBefore.processInfo.filter((entry) => !afterIds.has(entry.id)).length,
          rendererTaskPercent: (after.TaskDuration - before.TaskDuration) / (after.Timestamp - before.Timestamp) * 100,
          scriptPercent: (after.ScriptDuration - before.ScriptDuration) / (after.Timestamp - before.Timestamp) * 100,
          jsHeapMiB: after.JSHeapUsedSize / 1024 / 1024,
          audioContexts: await page.evaluate(() => (window as Window & { __playerAudioContexts?: number }).__playerAudioContexts),
        });
        const expectedPlayback = state === 'playing'
          ? playbackAfter.some((audio) => !audio.paused && !audio.ended && audio.seconds - (playbackBefore[0]?.seconds || 0) >= windowSeconds * 0.9)
          : state === 'paused'
            ? playbackBefore.length > 0 && playbackAfter.length === playbackBefore.length &&
              playbackAfter.every((audio, index) => audio.paused && Math.abs(audio.seconds - playbackBefore[index].seconds) < 0.05)
            : playbackBefore.length === 0 && playbackAfter.length === 0;
        // Persist each window before validation so failures retain diagnostic measurements.
        await fs.writeFile(testInfo.outputPath('native-resources.json'), JSON.stringify(samples, null, 2));
        expect(expectedPlayback).toBe(true);
        console.log(`Native resource window: ${state} ${sampleIndex + 1}/${windows} (${windowSeconds}s)`);
      }
    }
    expect(samples.every((sample) => sample.browserProcessesSampled > 0)).toBe(true);
    expect(samples.every((sample) => sample.audioContexts === 0)).toBe(true);
    await testInfo.attach('player-native-resources', {
      body: Buffer.from(JSON.stringify(samples, null, 2)),
      contentType: 'application/json',
    });
  } finally {
    await session.detach();
    await browserSession.detach();
  }
});
