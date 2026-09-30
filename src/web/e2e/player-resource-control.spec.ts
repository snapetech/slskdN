// <copyright file="player-resource-control.spec.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as fs from 'node:fs/promises';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { expect, test } from '@playwright/test';
import {
  collectBrowserProcessSnapshot,
  compareBrowserProcessSnapshots,
  summarizePssByType,
} from './harness/browser-process-resources';

test.use({ serviceWorkers: 'block', video: 'off', trace: 'off' });
test.skip(process.env.RUN_PLAYER_RESOURCE_CONTROL !== '1', 'Run through test:player:resource-control.');
test.skip(process.platform !== 'linux', 'The blank-page control requires Linux process PSS.');

test('records a blank-page browser resource control @player-resource-control', async ({ page }, testInfo) => {
  const windowSeconds = Number(process.env.SLSKDN_PLAYER_RESOURCE_CONTROL_WINDOW_SECONDS || 60);
  const windows = Number(process.env.SLSKDN_PLAYER_RESOURCE_CONTROL_WINDOWS || 40);
  expect(Number.isInteger(windowSeconds) && windowSeconds >= 10 && windowSeconds <= 60).toBe(true);
  expect(Number.isInteger(windows) && windows >= 1 && windows <= 60).toBe(true);
  const warmupSeconds = 15;
  test.setTimeout(90_000 + (warmupSeconds + windows * windowSeconds) * 1_000);

  await page.setContent('<!doctype html><html><head><title>Blank browser resource control</title></head><body></body></html>');
  const pageSession = await page.context().newCDPSession(page);
  await pageSession.send('Performance.enable');
  const browserSession = await page.context().browser()!.newBrowserCDPSession();
  const browserVersion = await browserSession.send('Browser.getVersion');
  const initialProcesses = await browserSession.send('SystemInfo.getProcessInfo');
  const root = initialProcesses.processInfo.find((entry) => entry.type === 'browser');
  expect(root).toBeDefined();
  const clockTicksPerSecond = Number((await promisify(execFile)('getconf', ['CLK_TCK'], { timeout: 5_000 })).stdout.trim());
  expect(Number.isSafeInteger(clockTicksPerSecond) && clockTicksPerSecond > 0).toBe(true);
  const metrics = async () => {
    const result = await pageSession.send('Performance.getMetrics');
    return Object.fromEntries(result.metrics.map(({ name, value }) => [name, value]));
  };
  const samples = [];

  try {
    await page.waitForTimeout(warmupSeconds * 1_000);
    for (let window = 1; window <= windows; window++) {
      const before = await metrics();
      const processesBefore = await browserSession.send('SystemInfo.getProcessInfo');
      const osBefore = await collectBrowserProcessSnapshot(root!.id);
      await page.waitForTimeout(windowSeconds * 1_000);
      const after = await metrics();
      const processesAfter = await browserSession.send('SystemInfo.getProcessInfo');
      const osAfter = await collectBrowserProcessSnapshot(root!.id);
      const cpuById = new Map(processesBefore.processInfo.map((entry) => [entry.id, entry.cpuTime]));
      const afterIds = new Set(processesAfter.processInfo.map((entry) => entry.id));
      const sampledProcesses = processesAfter.processInfo.filter((entry) => cpuById.has(entry.id));
      const browserCpuSeconds = sampledProcesses.reduce((total, entry) =>
        total + Math.max(0, entry.cpuTime - cpuById.get(entry.id)!), 0);
      const memoryReadings = await Promise.allSettled(processesAfter.processInfo.map(async (entry) => {
        const status = await fs.readFile(`/proc/${entry.id}/smaps_rollup`, 'utf8');
        const pss = status.match(/^Pss:\s+(\d+) kB$/mu);
        if (!pss) throw new Error('PSS unavailable');
        return Number(pss[1]) / 1024;
      }));
      const measuredMemory = memoryReadings.filter((entry): entry is PromiseFulfilledResult<number> => entry.status === 'fulfilled');
      const processTypeByPid = new Map(processesAfter.processInfo.map((entry) => [Number(entry.id), entry.type]));
      const pssByType = summarizePssByType(osAfter.processes.map((entry) => ({
        type: processTypeByPid.get(entry.pid) || 'not-reported-by-cdp',
        pssMiB: entry.pssMiB,
      })));
      const elapsedSeconds = after.Timestamp - before.Timestamp;
      const sample = {
        state: 'idle', window, profile: 'blank-page-control', warmupSeconds,
        browserVersion: browserVersion.product, platform: process.platform,
        browserProcessScope: 'cdp-reported',
        osProcessTree: {
          scope: 'linux-owned-process-tree', clockTicksPerSecond,
          ...compareBrowserProcessSnapshots(osBefore, osAfter, clockTicksPerSecond), pssByType,
        },
        browserPssMiB: measuredMemory.length ? measuredMemory.reduce((total, entry) => total + entry.value, 0) : null,
        browserPssByType: summarizePssByType(processesAfter.processInfo.map((entry, index) => ({
          type: entry.type,
          pssMiB: memoryReadings[index]?.status === 'fulfilled' ? memoryReadings[index].value : null,
        }))),
        memoryProcessesMeasured: measuredMemory.length,
        memoryProcessesUnavailable: memoryReadings.length - measuredMemory.length,
        seconds: elapsedSeconds,
        browserCpuPercentOfOneCore: browserCpuSeconds / elapsedSeconds * 100,
        browserProcessesSampled: sampledProcesses.length,
        browserProcessesBefore: processesBefore.processInfo.length,
        browserProcessesAfter: processesAfter.processInfo.length,
        browserProcessesAdded: processesAfter.processInfo.filter((entry) => !cpuById.has(entry.id)).length,
        browserProcessesRemoved: processesBefore.processInfo.filter((entry) => !afterIds.has(entry.id)).length,
        rendererTaskPercent: (after.TaskDuration - before.TaskDuration) / elapsedSeconds * 100,
        scriptPercent: (after.ScriptDuration - before.ScriptDuration) / elapsedSeconds * 100,
        jsHeapMiB: after.JSHeapUsedSize / 1024 / 1024,
        domNodes: after.Nodes ?? null, documents: after.Documents ?? null,
        eventListeners: after.JSEventListeners ?? null,
      };
      samples.push(sample);
      await fs.writeFile(testInfo.outputPath('native-resources.json'), JSON.stringify(samples, null, 2));
      expect(sample.osProcessTree.memoryProcessesUnavailable).toBe(0);
      expect(sample.browserProcessesSampled).toBeGreaterThan(0);
      console.log(`Blank resource control window: ${window}/${windows} (${windowSeconds}s)`);
    }
    await testInfo.attach('blank-browser-resource-control', {
      body: Buffer.from(JSON.stringify(samples, null, 2)),
      contentType: 'application/json',
    });
  } finally {
    await pageSession.detach();
    await browserSession.detach();
  }
});
