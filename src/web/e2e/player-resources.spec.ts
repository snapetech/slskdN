// <copyright file="player-resources.spec.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>
import * as fs from 'node:fs/promises';
import * as os from 'node:os';
import * as path from 'node:path';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { expect, test } from '@playwright/test';
import { MultiPeerHarness } from './harness/MultiPeerHarness';
import { collectBrowserProcessSnapshot, compareBrowserProcessSnapshots, summarizePssByType } from './harness/browser-process-resources';
import { makeTone } from './fixtures/player-tone';
import { login } from './helpers';

// Video and trace are worker options; isolate measurement from functional QA.
test.use({ serviceWorkers: 'block', video: 'off', trace: 'off' });
const harness = new MultiPeerHarness();
const resourceSessionJwtTtlMilliseconds = 14_400_000;
const hiddenPlayerControl = process.env.SLSKDN_PLAYER_RESOURCE_PROFILE === 'hidden-app';
let fixtureDirectory: string;
test.beforeAll(async () => {
  fixtureDirectory = await fs.mkdtemp(path.join(os.tmpdir(), 'player-resource-'));
  await harness.startNode('A', [], { noConnect: true, jwtTtlMilliseconds: resourceSessionJwtTtlMilliseconds });
});
test.afterAll(async () => {
  try { await harness.stopAll(); }
  finally { if (fixtureDirectory) await fs.rm(fixtureDirectory, { recursive: true, force: true }); }
});
test.beforeEach(async ({ page }) => {
  await page.addInitScript((hidePlayer) => {
    const audioWindow = window as Window & {
      __playerAudioContextRefs?: WeakRef<AudioContext>[];
      __playerAudioContexts?: number;
      __playerAudioContextCloses?: number;
      __playerAnalyzerReads?: { frequency: number; timeDomain: number };
      __playerPendingAnimationFrames?: Set<number>;
      __playerWebGlContextsCreated?: number;
      __playerWebGlContextsLost?: number;
      __playerWebGlContextAttributes?: Array<Record<string, boolean | null>>;
    };
    audioWindow.__playerAudioContexts = 0;
    audioWindow.__playerAudioContextCloses = 0;
    audioWindow.__playerAudioContextRefs = [];
    audioWindow.__playerAnalyzerReads = { frequency: 0, timeDomain: 0 };
    audioWindow.__playerWebGlContextsCreated = 0;
    audioWindow.__playerWebGlContextsLost = 0;
    audioWindow.__playerWebGlContextAttributes = [];
    const webGlContexts = new WeakSet<object>();
    const getContext = HTMLCanvasElement.prototype.getContext;
    HTMLCanvasElement.prototype.getContext = function trackedGetContext(
      this: HTMLCanvasElement,
      contextType: string,
      ...options: unknown[]
    ) {
      const context = Reflect.apply(getContext, this, [contextType, ...options]) as RenderingContext | null;
      if (context && ['webgl', 'webgl2', 'experimental-webgl'].includes(contextType) && !webGlContexts.has(context)) {
        webGlContexts.add(context);
        audioWindow.__playerWebGlContextsCreated! += 1;
        const attributes = (context as WebGLRenderingContext | WebGL2RenderingContext).getContextAttributes();
        if (attributes) {
          audioWindow.__playerWebGlContextAttributes!.push({
            alpha: attributes.alpha ?? null,
            antialias: attributes.antialias ?? null,
            depth: attributes.depth ?? null,
            premultipliedAlpha: attributes.premultipliedAlpha ?? null,
            stencil: attributes.stencil ?? null,
          });
        }
        this.addEventListener('webglcontextlost', () => {
          audioWindow.__playerWebGlContextsLost! += 1;
        }, { once: true });
      }
      return context;
    } as typeof HTMLCanvasElement.prototype.getContext;
    const pendingFrames = new Set<number>();
    audioWindow.__playerPendingAnimationFrames = pendingFrames;
    const requestFrame = window.requestAnimationFrame.bind(window);
    const cancelFrame = window.cancelAnimationFrame.bind(window);
    window.requestAnimationFrame = (callback) => {
      let frame = 0;
      frame = requestFrame((timestamp) => {
        pendingFrames.delete(frame);
        callback(timestamp);
      });
      pendingFrames.add(frame);
      return frame;
    };
    window.cancelAnimationFrame = (frame) => {
      pendingFrames.delete(frame);
      cancelFrame(frame);
    };
    const original = window.AudioContext;
    window.AudioContext = new Proxy(original, {
      construct(target, argumentsList, newTarget) {
        audioWindow.__playerAudioContexts! += 1;
        const context = Reflect.construct(target, argumentsList, newTarget);
        audioWindow.__playerAudioContextRefs!.push(new WeakRef(context));
        const close = context.close.bind(context);
        Object.defineProperty(context, 'close', {
          configurable: true,
          value: () => {
            audioWindow.__playerAudioContextCloses! += 1;
            return close();
          },
        });
        const createAnalyser = context.createAnalyser.bind(context);
        Object.defineProperty(context, 'createAnalyser', {
          configurable: true,
          value: () => {
            const analyser = createAnalyser();
            const readFrequency = analyser.getByteFrequencyData.bind(analyser);
            const readTimeDomain = analyser.getByteTimeDomainData.bind(analyser);
            analyser.getByteFrequencyData = (data: Uint8Array<ArrayBuffer>) => {
              audioWindow.__playerAnalyzerReads!.frequency += 1;
              readFrequency(data);
            };
            analyser.getByteTimeDomainData = (data: Uint8Array<ArrayBuffer>) => {
              audioWindow.__playerAnalyzerReads!.timeDomain += 1;
              readTimeDomain(data);
            };
            return analyser;
          },
        });
        return context;
      },
    });
    localStorage.setItem('slskdn.player.collapsed', 'false');
    if (hidePlayer) localStorage.setItem('slskdn:experience-preferences:v1', JSON.stringify({ playerVisible: false }));
  }, hiddenPlayerControl);
  await login(page, harness.getNode('A').nodeCfg);
});

test('records player and application browser resource use', async ({ page, request }, testInfo) => {
  expect(page.video()).toBeNull();
  const windowSeconds = Number(process.env.SLSKDN_PLAYER_RESOURCE_WINDOW_SECONDS || 10);
  const windows = Number(process.env.SLSKDN_PLAYER_RESOURCE_WINDOWS || 1);
  expect(Number.isInteger(windowSeconds) && windowSeconds >= 10 && windowSeconds <= 60).toBe(true);
  expect(Number.isInteger(windows) && windows >= 1 && windows <= 60).toBe(true);
  const sustained = windowSeconds * windows > 10;
  const warmupSeconds = sustained ? 15 : 0;
  const states = hiddenPlayerControl ? ['idle'] as const : ['idle', 'playing', 'paused'] as const;
  const samplingDurationSeconds = states.length * (warmupSeconds + windows * windowSeconds);
  test.setTimeout(90_000 + states.length * (warmupSeconds + windows * windowSeconds) * 1_000);
  if (hiddenPlayerControl) await expect(page.locator('.player-bar-hidden')).toBeVisible();
  const node = harness.getNode('A');
  const authenticationSessionResponse = await request.post(`${node.apiUrl}/api/v0/session`, {
    data: { username: node.nodeCfg.username, password: node.nodeCfg.password },
  });
  const authenticationSessionBody = await authenticationSessionResponse.text();
  expect(authenticationSessionResponse.ok(), authenticationSessionBody).toBe(true);
  const authenticationSession = JSON.parse(authenticationSessionBody) as { expires: number; issued: number };
  expect(authenticationSession.expires - authenticationSession.issued).toBeGreaterThan(samplingDurationSeconds + 1_800);
  const inputSeconds = hiddenPlayerControl ? null : sustained ? warmupSeconds + windows * windowSeconds + 60 : 40;
  const inputPath = path.join(fixtureDirectory, 'Native playback.wav');
  if (inputSeconds !== null) await fs.writeFile(inputPath, makeTone(inputSeconds));
  const inputBytes = hiddenPlayerControl ? 0 : (await fs.stat(inputPath)).size;
  const session = await page.context().newCDPSession(page);
  await session.send('Performance.enable');
  const browserSession = await page.context().browser()!.newBrowserCDPSession();
  const browserVersion = await browserSession.send('Browser.getVersion');
  const processes = await browserSession.send('SystemInfo.getProcessInfo');
  const root = processes.processInfo.find((entry) => entry.type === 'browser');
  expect(root).toBeDefined();
  const clockTicksPerSecond = process.platform === 'linux'
    ? Number((await promisify(execFile)('getconf', ['CLK_TCK'], { timeout: 5000 })).stdout.trim())
    : null;
  expect(clockTicksPerSecond === null || (Number.isSafeInteger(clockTicksPerSecond) && clockTicksPerSecond > 0)).toBe(true);
  const metrics = async () => {
    const result = await session.send('Performance.getMetrics');
    return Object.fromEntries(result.metrics.map(({ name, value }) => [name, value]));
  };
  const playback = () => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[])
    .filter((audio) => audio.currentSrc)
    .map((audio) => ({ paused: audio.paused, ended: audio.ended, seconds: audio.currentTime })));
  const samples = [];
  try {
    for (const state of states) {
      if (state === 'playing') {
        await page.getByLabel('Choose audio files', { exact: true }).setInputFiles(inputPath);
        await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
      }
      if (state === 'paused') await page.getByTestId('player-toggle-playback').click();
      if (warmupSeconds) await page.waitForTimeout(warmupSeconds * 1_000);
      for (let sampleIndex = 0; sampleIndex < windows; sampleIndex++) {
        const playbackBefore = await playback();
        const before = await metrics();
        const processesBefore = await browserSession.send('SystemInfo.getProcessInfo');
        const osBefore = clockTicksPerSecond === null ? null : await collectBrowserProcessSnapshot(root!.id);
        await page.waitForTimeout(windowSeconds * 1_000);
        const after = await metrics();
        const processesAfter = await browserSession.send('SystemInfo.getProcessInfo');
        const osAfter = clockTicksPerSecond === null ? null : await collectBrowserProcessSnapshot(root!.id);
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
        const processTypeByPid = new Map(processesAfter.processInfo.map((entry) => [Number(entry.id), entry.type]));
        const browserPssByType = summarizePssByType(processesAfter.processInfo.map((entry, index) => {
          const reading = memoryReadings[index];
          return {
            type: entry.type,
            pssMiB: reading?.status === 'fulfilled' ? reading.value : null,
          };
        }));
        const ownedTreePssByType = osAfter ? summarizePssByType(osAfter.processes.map((entry) => ({
          type: processTypeByPid.get(entry.pid) || 'not-reported-by-cdp',
          pssMiB: entry.pssMiB,
        }))) : null;
        samples.push({
          state, window: sampleIndex + 1, warmupSeconds,
          profile: hiddenPlayerControl ? 'hidden-app-control' : 'player-visible',
          browserVersion: browserVersion.product, platform: process.platform,
          inputKind: hiddenPlayerControl ? 'none' : 'disk-file', inputBytes, inputSeconds,
          browserProcessScope: 'cdp-reported',
          osProcessTree: osBefore && osAfter && clockTicksPerSecond !== null
            ? { scope: 'linux-owned-process-tree', clockTicksPerSecond,
              ...compareBrowserProcessSnapshots(osBefore, osAfter, clockTicksPerSecond),
              pssByType: ownedTreePssByType } : null,
          playbackBefore, playbackAfter,
          browserPssMiB: measuredMemory.length > 0 ? measuredMemory.reduce((total, entry) => total + entry.value, 0) : null,
          browserPssByType,
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
          domNodes: after.Nodes ?? null, documents: after.Documents ?? null,
          eventListeners: after.JSEventListeners ?? null,
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

test('soaks repeated queue, analyzer, output and floating-player cycles @player-resource-cycles', async ({ page }, testInfo) => {
  test.skip(process.env.SLSKDN_PLAYER_RESOURCE_CYCLES !== '1', 'Run through test:player:resource-cycles.');
  const cycleCount = Number(process.env.SLSKDN_PLAYER_RESOURCE_CYCLE_COUNT || 50);
  const settleSeconds = Number(process.env.SLSKDN_PLAYER_RESOURCE_CYCLE_SETTLE_SECONDS || 30);
  const trackCount = Number(process.env.SLSKDN_PLAYER_RESOURCE_CYCLE_TRACK_COUNT || 3);
  const profile = process.env.SLSKDN_PLAYER_RESOURCE_CYCLE_PROFILE || 'full';
  const profiles = ['full', 'queue', 'analyzer', 'output', 'pip', 'navigation', 'visualizer'];
  expect(Number.isInteger(cycleCount) && cycleCount >= 5 && cycleCount <= 100).toBe(true);
  expect(Number.isInteger(settleSeconds) && settleSeconds >= 10 && settleSeconds <= 60).toBe(true);
  expect(Number.isInteger(trackCount) && trackCount >= 2 && trackCount <= 10).toBe(true);
  expect(profiles).toContain(profile);
  test.setTimeout(30_000 + cycleCount * 15_000 + settleSeconds * 1_000);

  const files = Array.from({ length: trackCount }, (_, index) =>
    path.join(fixtureDirectory, `Resource cycle ${index + 1}.wav`));
  await Promise.all(files.map((file) => fs.writeFile(file, makeTone(40))));
  await page.addInitScript(() => {
    localStorage.setItem('slskdn.player.analyzerMode.v2', 'off');
    localStorage.setItem('slskdn.player.visualizerEnabled', 'false');
    const audioContext = window.AudioContext ||
      (window as Window & { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!audioContext) throw new Error('AudioContext is unavailable for the cycle soak.');
    const audioContextPrototype = audioContext.prototype as unknown as {
      setSinkId?: (deviceId: string) => Promise<void>;
    };
    const windowState = window as Window & { __playerOutputSelections?: string[] };
    windowState.__playerOutputSelections = [];
    Object.defineProperty(audioContextPrototype, 'setSinkId', {
      configurable: true,
      value: async function setSinkId(deviceId: string) {
        windowState.__playerOutputSelections!.push(deviceId);
        (this as AudioContext & { __resourceTestSinkId?: string }).__resourceTestSinkId = deviceId;
      },
    });
    if (!navigator.mediaDevices) throw new Error('MediaDevices is unavailable for the cycle soak.');
    Object.defineProperty(navigator.mediaDevices, 'enumerateDevices', {
      configurable: true,
      value: async () => [
        { deviceId: 'resource-output-a', groupId: 'resource-soak', kind: 'audiooutput', label: 'Resource output A' },
        { deviceId: 'resource-output-b', groupId: 'resource-soak', kind: 'audiooutput', label: 'Resource output B' },
      ],
    });
  });
  await page.reload({ waitUntil: 'domcontentloaded' });
  await page.getByLabel('Choose audio files', { exact: true }).setInputFiles(files);
  await expect(page.locator('.player-title')).toHaveText('Resource cycle 1');
  await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
    (elements as HTMLAudioElement[]).some((audio) => !audio.paused && audio.currentTime > 0.2))).toBe(true);
  await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
  const output = page.getByLabel('Audio output device', { exact: true });
  await expect(output).toHaveValue('default');

  const pipAvailable = await page.evaluate(() => typeof (window as Window & {
    documentPictureInPicture?: { requestWindow?: unknown };
  }).documentPictureInPicture?.requestWindow === 'function');
  expect(pipAvailable, 'Chromium Document Picture-in-Picture is required for the complete cycle soak.').toBe(true);

  const session = await page.context().newCDPSession(page);
  await session.send('Performance.enable');
  const actionMetrics: Array<Record<string, unknown>> = [];
  const actionReportPath = testInfo.outputPath('player-resource-cycle-actions.json');
  const inspectListenerTargets = async () => {
    const targets = [
      ['window', 'window'],
      ['document', 'document'],
      ['body', 'document.body'],
      ['root', 'document.querySelector("#root")'],
      ['player', 'document.querySelector(".player-bar")'],
      ['audio-primary', 'document.querySelectorAll("audio")[0]'],
      ['audio-secondary', 'document.querySelectorAll("audio")[1]'],
    ] as const;
    const results: Record<string, Record<string, number> | null> = {};
    for (const [name, expression] of targets) {
      const evaluated = await session.send('Runtime.evaluate', { expression });
      const objectId = evaluated.result.objectId;
      if (!objectId) {
        results[name] = null;
        continue;
      }
      try {
        const { listeners } = await session.send('DOMDebugger.getEventListeners', { objectId });
        results[name] = listeners.reduce((counts, listener) => {
          counts[listener.type] = (counts[listener.type] || 0) + 1;
          return counts;
        }, {} as Record<string, number>);
      } finally {
        await session.send('Runtime.releaseObject', { objectId });
      }
    }
    return results;
  };
  const captureActionMetrics = async (cycleIndex: number, step: string) => {
    const result = await session.send('Performance.getMetrics');
    const metrics = Object.fromEntries(result.metrics.map(({ name, value }) => [name, value]));
    const listenerTargets = step === 'track-restored' &&
      [0, 1, 5, 10, cycleCount].includes(cycleIndex)
      ? await inspectListenerTargets()
      : undefined;
    actionMetrics.push({
      cycle: cycleIndex,
      step,
      timestamp: metrics.Timestamp,
      domNodes: metrics.Nodes ?? null,
      documents: metrics.Documents ?? null,
      eventListeners: metrics.JSEventListeners ?? null,
      jsHeapMiB: typeof metrics.JSHeapUsedSize === 'number' ? metrics.JSHeapUsedSize / 1024 / 1024 : null,
      listenerTargets,
    });
    await fs.writeFile(actionReportPath, JSON.stringify(actionMetrics, null, 2));
  };

  const cycle = async (cycleIndex: number) => {
    const currentTitle = profile === 'queue' ? null : await page.locator('.player-title').textContent();
    if (profile === 'full' || profile === 'queue') {
      if (profile === 'queue' && process.env.SLSKDN_PLAYER_RESOURCE_DIRECT_QUEUE === '1') {
        await page.evaluate(async () => {
          const waitForQueue = (open: boolean) => new Promise<void>((resolve) => {
            const isOpen = () => Boolean(document.querySelector('.player-queue-modal'));
            if (isOpen() === open) {
              resolve();
              return;
            }
            const observer = new MutationObserver(() => {
              if (isOpen() === open) {
                observer.disconnect();
                resolve();
              }
            });
            observer.observe(document.body, { childList: true, subtree: true });
          });
          const opened = waitForQueue(true);
          document.querySelector<HTMLButtonElement>('[data-testid="player-open-queue"]')!.click();
          await opened;
          const closed = waitForQueue(false);
          document.dispatchEvent(new KeyboardEvent('keydown', {
            bubbles: true,
            cancelable: true,
            key: 'Escape',
          }));
          await closed;
        });
      } else {
        await page.getByTestId('player-open-queue').click();
        await expect(page.locator('.player-queue-modal')).toBeVisible();
        await page.keyboard.press('Escape');
        await expect(page.locator('.player-queue-modal')).toHaveCount(0);
      }
      await captureActionMetrics(cycleIndex, 'queue-closed');
    }

    if (profile === 'full' || profile === 'analyzer') {
      const analyzer = page.getByTestId('player-analyzer-tile');
      await analyzer.click();
      await expect(page.locator('.player-spectrum')).toBeVisible();
      await expect(analyzer).toContainText('Spectrum bars');
      await analyzer.click();
      await expect(page.locator('.player-spectrum')).toBeVisible();
      await expect(analyzer).toContainText('Signal scope');
      await analyzer.click();
      await expect(page.locator('.player-spectrum')).toHaveCount(0);
      await expect(analyzer).toContainText('Analyzer off');
      await captureActionMetrics(cycleIndex, 'analyzer-off');
    }

    if (profile === 'full' || profile === 'output') {
      for (const deviceId of ['resource-output-a', 'resource-output-b', 'default']) {
        await output.selectOption(deviceId);
        await expect(output).toBeEnabled();
        await expect(output).toHaveValue(deviceId);
      }
      await captureActionMetrics(cycleIndex, 'output-routed');
    }

    if (profile === 'full' || profile === 'pip') {
      await page.getByTestId('player-document-pip').click();
      const hasPictureInPicture = () => page.evaluate(() => Boolean((window as Window & {
        documentPictureInPicture?: { window: Window | null };
      }).documentPictureInPicture?.window));
      await expect.poll(hasPictureInPicture).toBe(true);
      await page.evaluate(() => (window as Window & {
        documentPictureInPicture?: { window: Window | null };
      }).documentPictureInPicture?.window?.close());
      await expect.poll(hasPictureInPicture).toBe(false);
      await captureActionMetrics(cycleIndex, 'pip-closed');
    }

    if (profile === 'full' || profile === 'navigation') {
      await page.getByTestId('player-next').click();
      await expect(page.locator('.player-title')).not.toHaveText(currentTitle || '');
      await page.getByTestId('player-previous').click();
      await expect(page.locator('.player-title')).toHaveText(currentTitle || '');
      await captureActionMetrics(cycleIndex, 'track-restored');
    }

    if (profile === 'full' || profile === 'visualizer') {
      await page.getByTestId('player-visual-tile-mode-butterchurn').click();
      const visualizer = page.getByTestId('player-visualizer');
      await expect(visualizer).toBeVisible();
      await expect(visualizer.locator('canvas')).toBeVisible();
      const lostContextsBeforeDispose = await page.evaluate(() => (window as Window & {
        __playerWebGlContextsLost?: number;
      }).__playerWebGlContextsLost ?? 0);
      await page.getByRole('button', { name: 'Hide MilkDrop visualizer', exact: true }).click();
      await expect(visualizer).toHaveCount(0);
      await expect(page.getByTestId('player-album-art')).toBeVisible();
      await expect.poll(() => page.evaluate(() => (window as Window & {
        __playerWebGlContextsLost?: number;
      }).__playerWebGlContextsLost ?? 0)).toBe(lostContextsBeforeDispose + 1);
      await captureActionMetrics(cycleIndex, 'visualizer-disposed');
    }
  };

  await cycle(0); // Warm lazy UI, graph creation and output discovery before sampling.
  const browserSession = await page.context().browser()!.newBrowserCDPSession();
  const browserVersion = await browserSession.send('Browser.getVersion');
  const processes = await browserSession.send('SystemInfo.getProcessInfo');
  const root = processes.processInfo.find((entry) => entry.type === 'browser');
  expect(root).toBeDefined();
  const samples: Array<Record<string, unknown>> = [];
  const reportPath = testInfo.outputPath('player-resource-cycles.json');
  const inspectDetachedElements = async () => {
    const tags = ['a', 'audio', 'button', 'canvas', 'div', 'img', 'input', 'li', 'p', 'path', 'span', 'svg'];
    const results: Record<string, {
      total: number;
      detached: number;
      queueModalDetached?: number;
      examples: Record<string, number>;
    } | null> = {};
    for (const tag of tags) {
      const prototype = await session.send('Runtime.evaluate', {
        expression: `Object.getPrototypeOf(document.createElement(${JSON.stringify(tag)}))`,
      });
      const prototypeObjectId = prototype.result.objectId;
      if (!prototypeObjectId) {
        results[tag] = null;
        continue;
      }
      let objectsObjectId: string | undefined;
      try {
        const queried = await session.send('Runtime.queryObjects', { prototypeObjectId });
        objectsObjectId = queried.objects.objectId;
        const inspected = await session.send('Runtime.callFunctionOn', {
          objectId: objectsObjectId,
          functionDeclaration: `function () {
            const examples = {};
            let detached = 0;
            let queueModalDetached = 0;
            for (let index = 0; index < this.length; index += 1) {
              const element = this[index];
              if (!element || element.isConnected) continue;
              detached += 1;
              const classes = typeof element.className === 'string'
                ? element.className.split(/\\s+/u).filter(Boolean).slice(0, 3).join('.')
                : '';
              const label = element.tagName.toLowerCase() + (classes ? '.' + classes : '');
              examples[label] = (examples[label] || 0) + 1;
              if (element.classList.contains('player-queue-modal')) queueModalDetached += 1;
            }
            return { total: this.length, detached, queueModalDetached, examples };
          }`,
          returnByValue: true,
        });
        results[tag] = inspected.result.value;
      } finally {
        if (objectsObjectId) await session.send('Runtime.releaseObject', { objectId: objectsObjectId });
        await session.send('Runtime.releaseObject', { objectId: prototypeObjectId });
      }
    }
    return results;
  };
  const capture = async (phase: string) => {
    const [metricResult, processTree, processInfo, telemetry] = await Promise.all([
      session.send('Performance.getMetrics'),
      process.platform === 'linux' ? collectBrowserProcessSnapshot(root!.id) : Promise.resolve(null),
      browserSession.send('SystemInfo.getProcessInfo'),
      page.evaluate(() => {
        const state = window as Window & {
          __playerAudioContexts?: number;
          __playerAudioContextCloses?: number;
          __playerAudioContextRefs?: WeakRef<AudioContext>[];
          __playerAnalyzerReads?: { frequency: number; timeDomain: number };
          __playerPendingAnimationFrames?: Set<number>;
          __playerOutputSelections?: string[];
          __playerWebGlContextsCreated?: number;
          __playerWebGlContextsLost?: number;
          __playerWebGlContextAttributes?: Array<Record<string, boolean | null>>;
        };
        const audioContexts = (state.__playerAudioContextRefs || [])
          .map((reference) => reference.deref())
          .filter((context): context is AudioContext => Boolean(context));
        return {
          audioContextsCreated: state.__playerAudioContexts || 0,
          audioContextsClosed: state.__playerAudioContextCloses || 0,
          liveAudioContextStates: audioContexts.map((context) => context.state),
          analyzerReads: state.__playerAnalyzerReads,
          webGlContextsCreated: state.__playerWebGlContextsCreated || 0,
          webGlContextsLost: state.__playerWebGlContextsLost || 0,
          webGlContextAttributes: state.__playerWebGlContextAttributes || [],
          outputSelections: state.__playerOutputSelections?.length ?? 0,
          pendingAnimationFrames: state.__playerPendingAnimationFrames?.size ?? null,
          pictureInPictureOpen: Boolean((window as Window & {
            documentPictureInPicture?: { window: Window | null };
          }).documentPictureInPicture?.window),
          audio: Array.from(document.querySelectorAll('audio')).map((element) => ({
            paused: (element as HTMLAudioElement).paused,
            currentTime: (element as HTMLAudioElement).currentTime,
            source: Boolean((element as HTMLAudioElement).currentSrc),
          })),
          connectedDomNodes: document.querySelectorAll('*').length,
          playerDomNodes: document.querySelector('.player-bar')?.querySelectorAll('*').length ?? null,
          queueDialogCount: document.querySelectorAll('.player-queue-modal').length,
          pictureInPictureButtonCount: document.querySelectorAll('[data-testid="player-document-pip"]').length,
        };
      }),
    ]);
    const metrics = Object.fromEntries(metricResult.metrics.map(({ name, value }) => [name, value]));
    const detachedDom = phase === 'forced-gc' ? await inspectDetachedElements() : undefined;
    const measured = processTree?.processes.filter((entry) => entry.pssMiB !== null) || [];
    const processTypeByPid = new Map(processInfo.processInfo.map((entry) => [Number(entry.id), entry.type]));
    const pssByType = summarizePssByType(measured.map((entry) => ({
      type: processTypeByPid.get(entry.pid) || 'not-reported-by-cdp', pssMiB: entry.pssMiB,
    })));
    samples.push({
      phase,
      profile,
      trackCount,
      timestamp: new Date().toISOString(),
      browserVersion: browserVersion.product,
      platform: process.platform,
      browserProcessScope: process.platform === 'linux' ? 'linux-owned-process-tree' : null,
      processTree: processTree ? {
        processCount: processTree.processes.length,
        pssMiB: measured.length ? measured.reduce((total, entry) => total + entry.pssMiB!, 0) : null,
        pssByType,
        memoryProcessesMeasured: measured.length,
        memoryProcessesUnavailable: processTree.processes.length - measured.length,
        enumerationReadsUnavailable: processTree.enumerationReadsUnavailable,
      } : null,
      metrics: {
        timestamp: metrics.Timestamp,
        jsHeapMiB: typeof metrics.JSHeapUsedSize === 'number' ? metrics.JSHeapUsedSize / 1024 / 1024 : null,
        domNodes: metrics.Nodes ?? null,
        documents: metrics.Documents ?? null,
        eventListeners: metrics.JSEventListeners ?? null,
      },
      telemetry: { ...telemetry, detachedDom },
    });
    await fs.writeFile(reportPath, JSON.stringify(samples, null, 2));
    console.log(`Player resource cycle sample: ${phase}`);
  };

  try {
    await capture('post-warmup');
    for (let index = 1; index <= cycleCount; index += 1) {
      await cycle(index);
      if (index % 5 === 0 || index === cycleCount) await capture(`cycle-${index}`);
    }

    await page.waitForTimeout(250);
    const analyzerReadsAtStop = await page.evaluate(() => {
      const state = window as Window & { __playerAnalyzerReads?: { frequency: number; timeDomain: number } };
      return { ...state.__playerAnalyzerReads };
    });
    await page.getByTestId('player-stop').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
      (elements as HTMLAudioElement[]).every((audio) => audio.paused))).toBe(true);
    await expect.poll(() => page.evaluate(() => {
      const state = window as Window & { __playerAudioContextRefs?: WeakRef<AudioContext>[] };
      return (state.__playerAudioContextRefs || []).map((reference) => reference.deref()?.state)
        .filter((state): state is AudioContextState => Boolean(state))
        .every((state) => state !== 'running');
    })).toBe(true);
    await capture('stopped');
    await page.waitForTimeout(settleSeconds * 1_000); // Natural GC remains enabled; do not force collection.
    const analyzerReadsAfterSettle = await page.evaluate(() => {
      const state = window as Window & { __playerAnalyzerReads?: { frequency: number; timeDomain: number } };
      return { ...state.__playerAnalyzerReads };
    });
    expect(analyzerReadsAfterSettle).toEqual(analyzerReadsAtStop);
    await capture('natural-settle');
    if (process.env.SLSKDN_PLAYER_RESOURCE_CYCLE_FORCE_GC === '1') {
      await session.send('HeapProfiler.enable');
      try {
        await session.send('HeapProfiler.collectGarbage');
        await page.waitForTimeout(1000);
        await capture('forced-gc');
        if (profile === 'full' || profile === 'queue') {
          const forcedGc = samples.find((sample) => sample.phase === 'forced-gc')!;
          const detachedDom = (forcedGc.telemetry as {
            detachedDom?: Record<string, {
              queueModalDetached?: number;
            } | null>;
          }).detachedDom;
          expect(detachedDom?.div?.queueModalDetached ?? 0,
            'Closed queue modals should not remain reachable after collection.').toBe(0);
        }
      } finally {
        await session.send('HeapProfiler.disable');
      }
    }

    expect(samples.every((sample) => {
      const value = sample.telemetry as { pictureInPictureOpen: boolean };
      return !value.pictureInPictureOpen;
    })).toBe(true);
    const stopped = samples.find((sample) => sample.phase === 'stopped')!.telemetry as {
      liveAudioContextStates: AudioContextState[];
      audioContextsCreated: number;
      analyzerReads: { frequency: number; timeDomain: number };
      webGlContextsCreated: number;
      webGlContextsLost: number;
      webGlContextAttributes: Array<Record<string, boolean | null>>;
    };
    const expectsAudioContext = ['full', 'analyzer', 'output', 'pip', 'visualizer'].includes(profile);
    if (expectsAudioContext) expect(stopped.audioContextsCreated).toBeGreaterThan(0);
    expect(stopped.liveAudioContextStates.filter((state) => state !== 'closed').length).toBeLessThanOrEqual(2);
    expect(stopped.liveAudioContextStates).not.toContain('running');
    if (profile === 'full' || profile === 'visualizer') {
      expect(stopped.webGlContextsCreated).toBeGreaterThanOrEqual(cycleCount + 1);
      expect(stopped.webGlContextsLost).toBeGreaterThanOrEqual(cycleCount + 1);
      expect(stopped.webGlContextAttributes).toHaveLength(stopped.webGlContextsCreated);
      expect(stopped.webGlContextAttributes.every((attributes) =>
        attributes.alpha === false && attributes.antialias === false && attributes.depth === false &&
        attributes.premultipliedAlpha === false && attributes.stencil === false)).toBe(true);
    }
    if (profile === 'full' || profile === 'analyzer' || profile === 'pip') {
      expect(stopped.analyzerReads?.frequency).toBeGreaterThan(0);
      if (profile === 'full' || profile === 'analyzer') {
        expect(stopped.analyzerReads?.timeDomain).toBeGreaterThan(0);
      }
    }
    if (profile === 'full' || profile === 'output') {
      expect((samples[0].telemetry as { outputSelections: number }).outputSelections).toBeGreaterThanOrEqual(3);
      expect((samples.find((sample) => sample.phase === `cycle-${cycleCount}`)!.telemetry as {
        outputSelections: number;
      }).outputSelections).toBeGreaterThanOrEqual(3 * (cycleCount + 1));
    }
    await testInfo.attach('player-resource-cycles', {
      body: Buffer.from(JSON.stringify(samples, null, 2)),
      contentType: 'application/json',
    });
    await testInfo.attach('player-resource-cycle-actions', {
      body: Buffer.from(JSON.stringify(actionMetrics, null, 2)),
      contentType: 'application/json',
    });
  } finally {
    await session.detach();
    await browserSession.detach();
  }
});
