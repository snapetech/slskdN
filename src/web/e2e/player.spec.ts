// <copyright file="player.spec.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as fs from 'node:fs/promises';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import * as path from 'node:path';
import { MultiPeerHarness } from './harness/MultiPeerHarness';
import { makeTone } from './fixtures/player-tone';
import { getAuthToken, login } from './helpers';
import { expect, test, type Page } from '@playwright/test';

// Generated PCM exercises the browser decoder without downloaded media,
// personal files, or remote peer requests.
const compressedFormats = [
  { extension: 'flac', label: 'FLAC', encoder: ['-c:a', 'flac'] },
  { extension: 'mp3', label: 'MP3', encoder: ['-c:a', 'libmp3lame', '-q:a', '5'] },
  { extension: 'ogg', label: 'Ogg Vorbis', encoder: ['-c:a', 'libvorbis', '-q:a', '5'] },
];

const seekPlayerTo = async (page: Page, targetSeconds: number) => {
  const seek = page.getByLabel('Seek playback', { exact: true });
  await seek.evaluate((input: HTMLInputElement, target: number) => {
    const setValue = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value')?.set;
    if (!setValue) throw new Error('The seek control does not expose its native value setter.');
    setValue.call(input, String(target));
    input.dispatchEvent(new Event('input', { bubbles: true }));
    input.dispatchEvent(new KeyboardEvent('keyup', { bubbles: true, key: 'ArrowRight' }));
  }, targetSeconds);
  await expect(seek).toHaveValue(String(targetSeconds));
};

const readPlayerTabOrder = async (page: Page) => page.locator('.player-bar').evaluate((player) => {
  const candidates = Array.from(player.querySelectorAll<HTMLElement>([
    'a[href]',
    'button',
    'input',
    'select',
    'textarea',
    '[contenteditable="true"]',
    '[tabindex]',
  ].join(',')));
  return candidates.filter((element) =>
    element.tabIndex >= 0 &&
    !element.matches(':disabled') &&
    element.getClientRects().length > 0 &&
    window.getComputedStyle(element).visibility !== 'hidden')
    .map((element) => element.dataset.testid || element.getAttribute('aria-label') ||
      element.getAttribute('title') || element.textContent?.trim() || element.tagName.toLowerCase());
});

const assertPlayerTabSequence = async (page: Page, expected: string[]) => {
  await page.locator('.player-bar').evaluate((player) => {
    const first = Array.from(player.querySelectorAll<HTMLElement>([
      'a[href]',
      'button',
      'input',
      'select',
      'textarea',
      '[contenteditable="true"]',
      '[tabindex]',
    ].join(','))).find((element) =>
      element.tabIndex >= 0 &&
      !element.matches(':disabled') &&
      element.getClientRects().length > 0 &&
      window.getComputedStyle(element).visibility !== 'hidden');
    if (!first) throw new Error('The player has no visible keyboard stops.');
    first.focus();
  });
  for (const [index, expectedStop] of expected.entries()) {
    if (index > 0) await page.keyboard.press('Tab');
    await expect.poll(() => page.locator('.player-bar').evaluate((player) => {
      const active = document.activeElement;
      if (!(active instanceof HTMLElement) || !player.contains(active)) return null;
      return active.dataset.testid || active.getAttribute('aria-label') || active.getAttribute('title') ||
        active.textContent?.trim() || active.tagName.toLowerCase();
    })).toBe(expectedStop);
  }
};

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
    for (const format of compressedFormats) {
      const outputPath = path.join(fixtureDirectory, `Player format ${format.label} runtime.${format.extension}`);
      await promisify(execFile)('ffmpeg', [
        '-hide_banner', '-loglevel', 'error', '-i', firstFile,
        ...format.encoder,
        outputPath,
      ]);
    }
    harness = new MultiPeerHarness();
    await harness.startNode('A', 'test-data/slskdn-test-fixtures/music', { noConnect: true });
    await fs.writeFile(path.join(harness.getNode('A').getAppDir(), 'downloads', 'Downloaded runtime.wav'), makeTone(42));
    await fs.writeFile(path.join(harness.getNode('A').getAppDir(), 'downloads', 'Downloaded runtime second.wav'), makeTone(43));
  });

  test.afterAll(async () => {
    if (harness) await harness.stopAll();
    if (fixtureDirectory) await fs.rm(fixtureDirectory, { recursive: true, force: true });
  });

  test.afterEach(async ({ page }) => {
    if (harness?.getNodeNames().includes('B')) {
      await page.goto('about:blank');
      await harness.stopNode('B');
    }
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

  test('opens listed radio without a track and recovers directory and stream failures', async ({ page }) => {
    let directoryFails = true;
    let streamFails = true;
    let directoryRequests = 0;
    let playbackInfoRequests = 0;
    page.on('request', (request) => { if (request.url().includes('/playback-info')) playbackInfoRequests += 1; });
    await page.route(/\/api\/v0\/listening-party(?:\?.*)?$/, async (route) => {
      directoryRequests += 1;
      await route.fulfill(directoryFails ? { status: 503, body: 'Unavailable' } : {
        contentType: 'application/json',
        body: JSON.stringify([
          { partyId: 'controlled-radio', contentId: 'radio:controlled', title: 'Controlled radio', hostPeerId: 'Test host', allowMeshStreaming: true, streamPath: '/controlled-radio.wav', transportUsername: 'controlled-host', streamTicket: 'controlled-capability', action: 'play', positionSeconds: 0 },
          { partyId: 'metadata-radio', contentId: 'radio:metadata', title: 'Metadata radio', hostPeerId: 'Test host', allowMeshStreaming: false },
        ]),
      });
    });
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    await page.route('**/api/v0/listed-radio/controlled-radio/tickets', (route) => route.fulfill({
      json: { streamUrl: '/api/v0/mesh-streams/controlled-radio' },
    }));
    await page.route('**/api/v0/mesh-streams/controlled-radio', (route) => route.fulfill({
      status: streamFails ? 503 : 200,
      contentType: 'audio/wav',
      body: makeTone(),
    }));
    await page.getByTestId('player-open-listed-radio').click();
    await expect(page.getByText('Listed radio could not load. Refresh to try again.', { exact: true })).toBeVisible();
    directoryFails = false;
    await page.getByRole('button', { name: 'Refresh listed radio' }).click();
    await expect(page.getByRole('button', { name: 'Play Metadata radio from listed radio' })).toBeDisabled();
    await page.getByRole('button', { name: 'Play Controlled radio from listed radio' }).click();
    await expect(page.locator('.player-title')).toHaveText('Controlled radio');
    await expect(page.getByText('This audio could not be decoded or streamed.', { exact: true })).toBeVisible();
    streamFails = false;
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    expect(directoryRequests).toBe(2);
    expect(playbackInfoRequests).toBe(0);
    await page.reload();
    await expect(page.locator('.player-title')).toHaveText('Nothing playing');
    expect(directoryRequests).toBe(2);
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
    await expect.poll(() => audio.evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    expect(await page.evaluate(() => navigator.mediaSession.metadata?.title)).toBe('Player runtime first');
    expect(await page.evaluate(() => navigator.mediaSession.playbackState)).toBe('playing');
    const supportsTransportActions = await page.evaluate(() => {
      const handlers = (window as Window & { __playerMediaActions?: Record<string, MediaSessionActionHandler | null> }).__playerMediaActions;
      return Boolean(handlers?.pause && handlers.play && handlers.stop);
    });
    test.skip(!supportsTransportActions, 'This browser exposes Media Session state but not transport action handlers.');
    const invoke = async (action: MediaSessionAction, details: Partial<MediaSessionActionDetails> = {}) => {
      await page.evaluate(({ action, details }) => {
        const handlers = (window as Window & { __playerMediaActions?: Record<string, MediaSessionActionHandler | null> }).__playerMediaActions;
        if (!handlers?.[action]) throw new Error(`Missing Media Session action: ${action}`);
        handlers[action]!({ action, ...details });
      }, { action, details });
    };
    await invoke('pause');
    await expect.poll(() => audio.evaluateAll((elements) => (elements as HTMLAudioElement[]).every((element) => element.paused))).toBe(true);
    expect(await page.evaluate(() => navigator.mediaSession.playbackState)).toBe('paused');
    await invoke('seekto', { seekTime: 12 });
    await expect.poll(() => audio.evaluateAll((elements) => Math.max(...(elements as HTMLAudioElement[]).map((element) => element.currentTime)))).toBeGreaterThanOrEqual(12);
    await expect.poll(() => page.evaluate(() => (window as Window & { __playerMediaPosition?: MediaPositionState }).__playerMediaPosition?.position)).toBeGreaterThanOrEqual(12);
    await invoke('seekbackward', { seekOffset: 5 });
    await expect.poll(() => audio.evaluateAll((elements) => Math.max(...(elements as HTMLAudioElement[]).map((element) => element.currentTime)))).toBe(7);
    await invoke('seekforward', { seekOffset: 3 });
    await expect.poll(() => audio.evaluateAll((elements) => Math.max(...(elements as HTMLAudioElement[]).map((element) => element.currentTime)))).toBe(10);
    await invoke('play');
    await expect.poll(() => audio.evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 10))).toBe(true);
    await invoke('nexttrack');
    await expect.poll(() => page.evaluate(() => navigator.mediaSession.metadata?.title)).toBe('Player runtime second');
    await invoke('previoustrack');
    await expect.poll(() => page.evaluate(() => navigator.mediaSession.metadata?.title)).toBe('Player runtime first');
    await invoke('stop');
    await expect.poll(() => audio.evaluateAll((elements) => (elements as HTMLAudioElement[]).every((element) => element.paused && !element.getAttribute('src')))).toBe(true);
    expect(await page.evaluate(() => navigator.mediaSession.metadata)).toBeNull();
    expect(await page.evaluate(() => navigator.mediaSession.playbackState)).toBe('none');
    expect(await page.evaluate(() => (window as Window & { __playerMediaPosition?: MediaPositionState }).__playerMediaPosition)).toBeUndefined();
  });

  test('plays local PCM, pauses, seeks, remounts and advances the queue', async ({ page }) => {
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles([firstFile, secondFile]);
    const activeAudio = page.locator('audio');
    await expect.poll(() => activeAudio.evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(() => activeAudio.evaluateAll((elements) => (elements as HTMLAudioElement[]).every((element) => element.paused))).toBe(true);
    await page.getByLabel('Seek playback', { exact: true }).press('Home');
    for (let second = 0; second < 12; second++) {
      await page.getByLabel('Seek playback', { exact: true }).press('ArrowRight');
    }
    await expect.poll(() => activeAudio.evaluateAll((elements) => Math.max(...(elements as HTMLAudioElement[]).map((element) => element.currentTime)))).toBeGreaterThanOrEqual(12);
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(() => activeAudio.evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 12.2))).toBe(true);
    await page.getByTestId('player-collapse').click();
    await expect.poll(() => activeAudio.evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 12))).toBe(true);
    await page.getByRole('button', { name: 'Next local track', exact: true }).click();
    await expect(page.locator('.player-title')).toHaveText('Player runtime second');
    expect(await page.evaluate(() => (window as Window & { __playerAudioContexts?: number }).__playerAudioContexts)).toBe(0);
    await expect.poll(() => activeAudio.evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2 && element.currentTime < 5))).toBe(true);
  });

  for (const selection of ['direct', 'queue']) {
    test(`switches between unindexed downloads through ${selection} without a scan cooldown failure`, async ({ page }) => {
      await page.getByTestId('player-open-file-browser').click();
      const modal = page.getByTestId('player-file-browser-modal');
      await modal.getByTestId('player-file-browser-search').locator('input').fill('Downloaded runtime');
      await modal.getByRole('button', { name: 'Play Downloaded runtime.wav', exact: true }).click();
      await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
        (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
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
        (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2)), { timeout: 3_000 }).toBe(true);
    });
  }

  test('plays server AIFF natively or transcodes as needed, then seeks absolutely', async ({ page }) => {
    await page.getByTestId('player-open-file-browser').click();
    const modal = page.getByTestId('player-file-browser-modal');
    await modal.getByTestId('player-file-browser-search').locator('input').fill('Player decoded runtime');
    await modal.getByRole('button', { name: 'Play Player decoded runtime.aiff', exact: true }).click();
    const audio = page.locator('audio');
    const decodeButton = page.getByRole('button', { name: 'Decode for playback', exact: true });
    await expect.poll(async () => (await decodeButton.count()) > 0 || await audio.evaluateAll((elements) =>
      (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    const usesTranscode = await decodeButton.count() > 0;
    if (usesTranscode) {
      await decodeButton.click();
      await expect.poll(() => audio.evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) =>
        !element.paused && element.currentTime > 0.2 && element.currentSrc.includes('/transcoded')))).toBe(true);
    } else {
      await expect.poll(() => audio.evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) =>
        !element.paused && element.currentTime > 0.2))).toBe(true);
    }
    await page.getByTestId('player-toggle-playback').click();
    const seek = page.getByLabel('Seek playback', { exact: true });
    await seek.press('Home');
    for (let second = 0; second < 12; second++) await seek.press('ArrowRight');
    if (usesTranscode) {
      await expect.poll(() => audio.evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) =>
        element.currentSrc.includes('startSeconds=12')))).toBe(true);
    } else {
      await expect.poll(() => audio.evaluateAll((elements) => Math.max(...(elements as HTMLAudioElement[]).map((element) => element.currentTime)))).toBeGreaterThanOrEqual(12);
    }
    await expect.poll(() => audio.evaluateAll((elements) => (elements as HTMLAudioElement[]).every((element) => element.paused))).toBe(true);
    await expect(seek).toHaveValue('12');
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(() => audio.evaluateAll((elements, transcodeMode: boolean) => (elements as HTMLAudioElement[]).some((element) =>
      !element.paused && element.currentTime > (transcodeMode ? 0.2 : 12.2) &&
      (!transcodeMode || element.currentSrc.includes('startSeconds=12'))), usesTranscode)).toBe(true);
    await seek.press('Home');
    for (let second = 0; second < 5; second++) await seek.press('ArrowRight');
    if (usesTranscode) {
      await expect.poll(() => audio.evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) =>
        !element.paused && element.currentTime > 0.2 && element.currentSrc.includes('startSeconds=5')))).toBe(true);
    } else {
      await expect.poll(() => audio.evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) =>
        !element.paused && element.currentTime >= 5 && element.currentTime < 7))).toBe(true);
    }
  });

  for (const format of compressedFormats) {
    test(`plays server ${format.label} natively or through on-demand decoding`, async ({ page }) => {
      await page.getByTestId('player-open-file-browser').click();
      const modal = page.getByTestId('player-file-browser-modal');
      await modal.getByTestId('player-file-browser-search').locator('input').fill(`Player format ${format.label} runtime`);
      await modal.getByRole('button', {
        name: `Play Player format ${format.label} runtime.${format.extension}`,
        exact: true,
      }).click();

      const audio = page.locator('audio');
      const decodeButton = page.getByRole('button', { name: 'Decode for playback', exact: true });
      await expect.poll(async () => (await decodeButton.count()) > 0 || await audio.evaluateAll((elements) =>
        (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
      if (await decodeButton.count() > 0) {
        await decodeButton.click();
        await expect.poll(() => audio.evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) =>
          !element.paused && element.currentTime > 0.2 && element.currentSrc.includes('/transcoded')))).toBe(true);
      } else {
        await expect.poll(() => audio.evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) =>
          !element.paused && element.currentTime > 0.2 && !element.currentSrc.includes('/transcoded')))).toBe(true);
      }
    });

    test(`recovers ${format.label} playback after a temporary transcode failure`, async ({ page }) => {
      let nativeStreamFailures = 0;
      let transcodeRequests = 0;
      await page.route((url) => url.pathname.startsWith('/api/v0/streams/') &&
        url.pathname.split('/').length === 5, async (route) => {
        if (route.request().method() !== 'GET') {
          await route.continue();
          return;
        }
        nativeStreamFailures += 1;
        await route.fulfill({ status: 415, contentType: 'text/plain', body: 'Injected native-format rejection.' });
      });
      await page.route((url) => url.pathname.startsWith('/api/v0/streams/') &&
        url.pathname.endsWith('/transcoded'), async (route) => {
        transcodeRequests += 1;
        if (transcodeRequests === 1) {
          await route.fulfill({ status: 503, contentType: 'text/plain', body: 'Injected temporary transcode failure.' });
          return;
        }
        await route.continue();
      });

      await page.getByTestId('player-open-file-browser').click();
      const modal = page.getByTestId('player-file-browser-modal');
      await modal.getByTestId('player-file-browser-search').locator('input').fill(`Player format ${format.label} runtime`);
      await modal.getByRole('button', {
        name: `Play Player format ${format.label} runtime.${format.extension}`,
        exact: true,
      }).click();

      const audio = page.locator('audio').first();
      await expect(page.getByRole('button', { name: 'Decode for playback', exact: true })).toBeVisible();
      expect(nativeStreamFailures).toBe(1);
      await audio.evaluate((element) => {
        const playerWindow = window as Window & { __playerDecodeErrorCount?: number };
        playerWindow.__playerDecodeErrorCount = 0;
        element.addEventListener('error', () => { playerWindow.__playerDecodeErrorCount! += 1; });
      });

      const failedTranscode = page.waitForResponse((response) => {
        const path = new URL(response.url()).pathname;
        return path.startsWith('/api/v0/streams/') && path.endsWith('/transcoded') && response.status() === 503;
      });
      await page.getByRole('button', { name: 'Decode for playback', exact: true }).click();
      await failedTranscode;
      await expect.poll(() => page.evaluate(() =>
        (window as Window & { __playerDecodeErrorCount?: number }).__playerDecodeErrorCount)).toBeGreaterThan(0);
      await expect.poll(() => audio.evaluate((element: HTMLAudioElement) =>
        element.paused && Boolean(element.error) && element.currentSrc.includes('/transcoded'))).toBe(true);

      const successfulRetry = page.waitForResponse((response) => {
        const path = new URL(response.url()).pathname;
        return path.startsWith('/api/v0/streams/') && path.endsWith('/transcoded') && response.status() === 200;
      });
      await page.getByTestId('player-toggle-playback').click();
      const retriedStream = await successfulRetry;
      expect(retriedStream.headers()['content-type']).toContain('audio/mpeg');
      await expect.poll(() => audio.evaluate((element: HTMLAudioElement) =>
        !element.paused && element.currentTime > 0.2 && element.currentSrc.includes('/transcoded'))).toBe(true);
      expect(nativeStreamFailures).toBe(1);
      expect(transcodeRequests).toBe(2);
    });
  }

  test('recovers playback after the server cannot start FFmpeg', async ({ page }) => {
    const missingFfmpegPath = path.join(fixtureDirectory, 'ffmpeg-unavailable');
    await harness.startNode('B', 'test-data/slskdn-test-fixtures/music', {
      noConnect: true,
      ffmpegPath: missingFfmpegPath,
    });
    const node = harness.getNode('B');
    await login(page, node.nodeCfg);

    let nativeStreamFailures = 0;
    let transcodeRequests = 0;
    page.on('request', (request) => {
      if (new URL(request.url()).pathname.endsWith('/transcoded')) transcodeRequests += 1;
    });
    await page.route((url) => url.pathname.startsWith('/api/v0/streams/') &&
      url.pathname.split('/').length === 5, async (route) => {
      if (route.request().method() !== 'GET') {
        await route.continue();
        return;
      }
      nativeStreamFailures += 1;
      await route.fulfill({ status: 415, contentType: 'text/plain', body: 'Injected native-format rejection.' });
    });

    await page.getByTestId('player-open-file-browser').click();
    const modal = page.getByTestId('player-file-browser-modal');
    await modal.getByTestId('player-file-browser-search').locator('input').fill('Player format FLAC runtime');
    await modal.getByRole('button', {
      name: 'Play Player format FLAC runtime.flac',
      exact: true,
    }).click();
    await expect(page.getByRole('button', { name: 'Decode for playback', exact: true })).toBeVisible();
    expect(nativeStreamFailures).toBe(1);

    const failedTranscode = page.waitForResponse((response) => {
      const url = new URL(response.url());
      return url.pathname.startsWith('/api/v0/streams/') && url.pathname.endsWith('/transcoded');
    });
    await page.getByRole('button', { name: 'Decode for playback', exact: true }).click();
    const failure = await failedTranscode;
    expect(failure.status()).toBe(503);
    expect(await failure.text()).toContain('FFmpeg is unavailable on this server.');
    await expect(page.getByText('The server could not decode this audio. Press Play to retry.', { exact: true })).toBeVisible();

    const configPath = path.join(node.getAppDir(), 'config', 'slskd.yml');
    const config = await fs.readFile(configPath, 'utf8');
    const restoredConfig = config.replace(/^    ffmpegPath: .*$/mu, '    ffmpegPath: "ffmpeg"');
    expect(restoredConfig).not.toBe(config);
    await fs.writeFile(configPath, restoredConfig, 'utf8');
    await page.waitForTimeout(500);

    const recoveredTranscode = page.waitForResponse((response) => {
      const url = new URL(response.url());
      return url.pathname.startsWith('/api/v0/streams/') && url.pathname.endsWith('/transcoded') && response.status() === 200;
    });
    await page.getByTestId('player-toggle-playback').click();
    const recovered = await recoveredTranscode;
    expect(recovered.headers()['content-type']).toContain('audio/mpeg');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((audio) =>
      !audio.paused && audio.currentTime > 0.2 && audio.currentSrc.includes('/transcoded')))).toBe(true);
    expect(nativeStreamFailures).toBe(1);
    expect(transcodeRequests).toBe(2);
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
      (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2 && element.currentTime < 2))).toBe(true);
    await page.getByTestId('player-next').click();
    await expect(page.locator('.player-title')).toHaveText('Player runtime second.wav');
    await page.getByTestId('player-next').click();
    await expect(page.locator('.player-title')).toHaveText('Player runtime first.wav');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
      (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2 && element.currentTime < 5))).toBe(true);
    await page.reload({ waitUntil: 'domcontentloaded' });
    await expect(page.locator('.player-title')).toHaveText('Player runtime first.wav');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).every((element) => element.paused))).toBe(true);
  });

  test('keeps identical browser-local files as separate queue entries', async ({ page }) => {
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles([firstFile, firstFile]);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
      (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    const firstSource = await page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).find((element) => !element.paused)!.currentSrc);
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    await page.getByTestId('player-open-queue').click();
    const queue = page.locator('.player-queue-modal');
    await expect(queue.locator('.player-queue-manager-list .player-queue-manager-row')).toHaveCount(1);
    await queue.getByRole('button', { name: 'Done', exact: true }).click();
    await page.getByTestId('player-next').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements, previousSource) =>
      (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2 && element.currentSrc !== previousSource), firstSource)).toBe(true);
    await expect(page.locator('.player-title')).toHaveText('Player runtime first');
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
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).every((element) => element.paused))).toBe(true);
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
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    const pipButton = page.getByTestId('player-document-pip');
    const supportsDocumentPictureInPicture = await page.evaluate(() =>
      typeof (window as Window & { documentPictureInPicture?: { requestWindow?: unknown } })
        .documentPictureInPicture?.requestWindow === 'function');
    if (!supportsDocumentPictureInPicture) {
      await expect(pipButton).toBeDisabled();
      test.skip(true, 'Document Picture-in-Picture is not supported by this browser.');
    }
    await expect(pipButton).toBeEnabled();
    await pipButton.click();
    const pipExists = () => page.evaluate(() => Boolean((window as Window & { documentPictureInPicture?: { window: Window | null } }).documentPictureInPicture?.window));
    await expect.poll(pipExists).toBe(true);
    await expect.poll(() => page.evaluate(() => {
      const pip = (window as Window & { documentPictureInPicture?: { window: Window | null } }).documentPictureInPicture?.window;
      const canvas = pip?.document.querySelector('canvas');
      if (!canvas) return false;
      const pixels = canvas.getContext('2d')!.getImageData(0, 0, canvas.width, canvas.height).data;
      return pixels.some((value, index) => index % 4 !== 3 && value > 100);
    })).toBe(true);
    const png = await page.evaluate(() => (window as Window & { documentPictureInPicture?: { window: Window } }).documentPictureInPicture!.window.document.querySelector('canvas')!.toDataURL());
    await fs.writeFile(testInfo.outputPath('picture-in-picture.png'), Buffer.from(png.split(',')[1], 'base64'));
    await page.getByTestId('player-stop').click();
    await expect.poll(pipExists).toBe(false);
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles(firstFile);
    await page.getByTestId('player-document-pip').click();
    await expect.poll(pipExists).toBe(true);
    await page.getByTestId('player-hide').click();
    await expect.poll(pipExists).toBe(false);
  });

  test('suspends processing after rejected initial Play and resumes on explicit retry', async ({ page }) => {
    await page.evaluate(() => localStorage.setItem('slskdn.player.crossfadeEnabled', 'true'));
    await page.reload();
    await page.evaluate(() => {
      const play = HTMLMediaElement.prototype.play;
      HTMLMediaElement.prototype.play = function () {
        HTMLMediaElement.prototype.play = play;
        return Promise.reject(new DOMException('Injected denied start', 'NotAllowedError'));
      };
    });
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles(firstFile);
    await expect(page.getByText('Playback could not start. Check the file or try again.', { exact: true })).toBeVisible();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).every((audio) => audio.paused))).toBe(true);
    await expect.poll(() => page.evaluate(() => (window as Window & { __playerAudioContextInstances?: AudioContext[] }).__playerAudioContextInstances!.map((context) => context.state))).toEqual(['suspended']);
    await page.getByTestId('player-toggle-playback').click();
    await expect(page.getByText('Playback could not start. Check the file or try again.', { exact: true })).not.toBeVisible();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((audio) => !audio.paused && audio.currentTime > 0.2))).toBe(true);
    await expect.poll(() => page.evaluate(() => (window as Window & { __playerAudioContextInstances?: AudioContext[] }).__playerAudioContextInstances!.map((context) => context.state))).toEqual(['running']);
  });

  test('keeps the graph running when newer Play supersedes pending Pause suspension', async ({ page }) => {
    await page.reload();
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    const enableCrossfade = page.getByRole('button', { name: 'Enable crossfade', exact: true });
    if (await enableCrossfade.count()) await enableCrossfade.click();
    await expect(page.getByRole('button', { name: 'Disable crossfade', exact: true })).toBeVisible();
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles(firstFile);
    await expect.poll(() => page.evaluate(() => (window as Window & { __playerAudioContextInstances?: AudioContext[] }).__playerAudioContextInstances!.some((context) => context.state === 'running'))).toBe(true);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((audio) => !audio.paused))).toBe(true);
    await page.evaluate(() => {
      const audioWindow = window as Window & { __playerAudioContextInstances?: AudioContext[]; __playerSuspendRequested?: boolean; __releasePlayerSuspend?: () => void };
      const context = audioWindow.__playerAudioContextInstances!.find((candidate) => candidate.state === 'running')!;
      const suspend = context.suspend.bind(context);
      let holdNextSuspend = true;
      context.suspend = () => {
        if (!holdNextSuspend) return suspend();
        holdNextSuspend = false;
        let releaseSuspend;
        const held = new Promise<void>((resolve) => { releaseSuspend = resolve; });
        audioWindow.__playerSuspendRequested = true;
        audioWindow.__releasePlayerSuspend = () => {
          releaseSuspend!();
          audioWindow.__releasePlayerSuspend = undefined;
        };
        return held.then(() => suspend());
      };
    });
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).every((audio) => audio.paused))).toBe(true);
    await expect.poll(() => page.evaluate(() => (window as Window & { __playerSuspendRequested?: boolean }).__playerSuspendRequested)).toBe(true);
    await page.getByTestId('player-toggle-playback').click();
    await expect(page.locator('.player-now-playing .player-eyebrow')).toHaveText('Loading');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).every((audio) => audio.paused))).toBe(true);
    await page.evaluate(() => (window as Window & { __releasePlayerSuspend?: () => void }).__releasePlayerSuspend!());
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((audio) => !audio.paused && audio.currentTime > 0.2))).toBe(true);
    await expect.poll(() => page.evaluate(() => (window as Window & { __playerAudioContextInstances?: AudioContext[] }).__playerAudioContextInstances!.some((context) => context.state === 'running'))).toBe(true);
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(() => page.evaluate(() => (window as Window & { __playerAudioContextInstances?: AudioContext[] }).__playerAudioContextInstances!.every((context) => context.state === 'suspended'))).toBe(true);
  });

  test('suspends the outgoing graph when a crossfade finishes naturally', async ({ page }) => {
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles([firstFile, secondFile]);
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    await page.getByTestId('player-toggle-crossfade').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    await seekPlayerTo(page, 34);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).filter((element) => !element.paused).length)).toBe(2);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).filter((element) => !element.paused).length)).toBe(1);
    await expect.poll(() => page.evaluate(() => (window as Window & { __playerAudioContextInstances?: AudioContext[] }).__playerAudioContextInstances!.map((context) => context.state).sort())).toEqual(['running', 'suspended']);
  });

  test('crossfades two local streams and pauses both on transport Pause', async ({ page }) => {
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles([firstFile, secondFile]);
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    await page.getByTestId('player-toggle-crossfade').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    await seekPlayerTo(page, 34);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).filter((element) => !element.paused).length)).toBe(2);
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).every((element) => element.paused))).toBe(true);
    await expect.poll(() => page.evaluate(() => (window as Window & { __playerAudioContextInstances?: AudioContext[] }).__playerAudioContextInstances!.every((context) => context.state === 'suspended'))).toBe(true);
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).filter((element) => !element.paused).length)).toBe(1);
  });

  test('controls playback and seeking with the keyboard in expanded and compact modes', async ({ page }) => {
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles(firstFile);
    const audioState = () => page.evaluate(() => {
      const element = document.querySelector<HTMLAudioElement>('audio');
      return element ? { currentTime: element.currentTime, paused: element.paused } : null;
    });
    await expect.poll(async () => {
      const state = await audioState();
      return state !== null && !state.paused && state.currentTime > 0.2;
    }).toBe(true);
    const playbackAnnouncement = page.getByTestId('player-playback-announcement');
    await expect(playbackAnnouncement).toHaveText(/Now playing: .+/u);

    const play = page.getByTestId('player-toggle-playback');
    await expect(play).toHaveAccessibleName('Pause local playback');
    await play.focus();
    await page.keyboard.press('Space');
    await expect(play).toHaveAccessibleName('Resume local playback');
    await expect.poll(async () => (await audioState())?.paused).toBe(true);
    await expect(playbackAnnouncement).toHaveText(/Paused: .+/u);

    const seek = page.getByLabel('Seek playback', { exact: true });
    await expect(seek).toBeEnabled();
    await seek.focus();
    await page.keyboard.press('Home');
    await page.keyboard.press('ArrowRight');
    await expect(seek).toHaveValue('1');
    await expect.poll(async () => (await audioState())?.currentTime).toBe(1);
    await expect.poll(async () => (await audioState())?.paused).toBe(true);

    await play.focus();
    await page.keyboard.press('Enter');
    await expect.poll(async () => {
      const state = await audioState();
      return state !== null && !state.paused && state.currentTime > 1.2;
    }).toBe(true);
    await expect(playbackAnnouncement).toHaveText(/Now playing: .+/u);

    const collapse = page.getByTestId('player-collapse');
    await collapse.focus();
    await page.keyboard.press('Enter');
    const compactPlay = page.getByTestId('player-collapsed-toggle-playback');
    await expect(compactPlay).toBeVisible();
    await compactPlay.focus();
    await page.keyboard.press('Space');
    await expect(compactPlay).toHaveAccessibleName('Resume local playback');
    await expect.poll(async () => (await audioState())?.paused).toBe(true);
    await page.getByTestId('player-expand').click();
    await page.getByTestId('player-stop').click();
    await expect(playbackAnnouncement).toHaveText('Playback stopped.');
  });

  test('keeps the visible player controls in keyboard Tab order across modes', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 900 });
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles([firstFile, secondFile]);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
      (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);

    const expandedOrder = await readPlayerTabOrder(page);
    expect(expandedOrder).toContain('player-visual-tile');
    expect(expandedOrder).toContain('player-analyzer-tile');
    expect(expandedOrder.indexOf('Seek playback')).toBeLessThan(expandedOrder.indexOf('player-previous'));
    expect(expandedOrder.indexOf('player-previous')).toBeLessThan(expandedOrder.indexOf('player-rewind'));
    expect(expandedOrder.indexOf('player-rewind')).toBeLessThan(expandedOrder.indexOf('player-toggle-playback'));
    expect(expandedOrder.indexOf('player-toggle-playback')).toBeLessThan(expandedOrder.indexOf('player-fast-forward'));
    expect(expandedOrder.indexOf('player-fast-forward')).toBeLessThan(expandedOrder.indexOf('player-next'));
    expect(expandedOrder.indexOf('player-next')).toBeLessThan(expandedOrder.indexOf('player-stop'));
    expect(expandedOrder.indexOf('player-stop')).toBeLessThan(expandedOrder.indexOf('Playback speed'));
    await assertPlayerTabSequence(page, expandedOrder);

    const canEnumerateOutputs = await page.evaluate(() => {
      const audioContext = window.AudioContext ||
        (window as Window & { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
      const mediaDevices = Reflect.get(navigator, 'mediaDevices');
      return Boolean(mediaDevices && Reflect.get(mediaDevices, 'enumerateDevices') && audioContext &&
        Reflect.get(audioContext.prototype, 'setSinkId'));
    });
    if (canEnumerateOutputs) {
      await page.evaluate(() => Object.defineProperty(navigator.mediaDevices, 'enumerateDevices', {
        configurable: true,
        value: async () => [{
          deviceId: 'keyboard-order-test-output',
          groupId: 'keyboard-order-test',
          kind: 'audiooutput',
          label: 'Keyboard order test output',
        }],
      }));
    }
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    if (canEnumerateOutputs) await expect(page.getByLabel('Audio output device')).toHaveCount(1);
    const expandedToolsOrder = await readPlayerTabOrder(page);
    for (const testId of [
      'player-toggle-visualizer',
      'player-toggle-eq',
      'player-toggle-lyrics',
      'player-open-listed-radio',
      'player-open-radio',
      'player-open-listening-stats',
      'player-open-discovery-shelf',
      'player-toggle-karaoke',
      'player-toggle-crossfade',
      'player-open-integrations',
    ]) expect(expandedToolsOrder).toContain(testId);
    await assertPlayerTabSequence(page, expandedToolsOrder);

    await page.getByTestId('player-collapse').click();
    const compactOrder = await readPlayerTabOrder(page);
    expect(compactOrder).toEqual([
      'Seek playback',
      'Previous local track',
      'player-collapsed-toggle-playback',
      'Next local track',
      'Open playback queue',
      'player-expand',
      'player-hide',
      'player-collapsed-toggle-mute',
      'Playback volume',
    ]);
    await assertPlayerTabSequence(page, compactOrder);
  });

  test('contains keyboard focus in the queue dialog and restores its opener', async ({ page }) => {
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles([firstFile, secondFile]);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
      (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);

    const trigger = page.getByTestId('player-open-queue');
    await trigger.focus();
    await page.keyboard.press('Enter');

    const dialog = page.getByRole('dialog', { name: 'Playback Queue', exact: true });
    await expect(dialog).toBeVisible();
    const focusIsInDialog = () => page.evaluate(() => {
      const modal = document.querySelector('.player-queue-modal');
      return Boolean(modal && document.activeElement && modal.contains(document.activeElement));
    });
    const focusState = () => page.evaluate(() => {
      const modal = document.querySelector('.player-queue-modal');
      if (!modal) return { activeIndex: -1, count: 0 };

      const focusables = Array.from(modal.querySelectorAll<HTMLElement>([
        'a[href]',
        'button:not([disabled])',
        'input:not([disabled]):not([type="hidden"])',
        'select:not([disabled])',
        'textarea:not([disabled])',
        '[contenteditable="true"]',
        '[tabindex]:not([tabindex="-1"])',
      ].join(','))).filter((element) =>
        element.tabIndex >= 0 &&
        !element.closest('[aria-hidden="true"]') &&
        element.getClientRects().length > 0 &&
        window.getComputedStyle(element).visibility !== 'hidden');

      const activeElement = document.activeElement;
      return {
        activeIndex: activeElement instanceof HTMLElement ? focusables.indexOf(activeElement) : -1,
        count: focusables.length,
      };
    });
    await expect.poll(focusIsInDialog).toBe(true);
    await expect.poll(async () => (await focusState()).activeIndex).toBe(0);
    const { count } = await focusState();
    expect(count).toBeGreaterThan(1);

    await page.keyboard.press('Shift+Tab');
    await expect.poll(focusIsInDialog).toBe(true);
    await expect.poll(async () => (await focusState()).activeIndex).toBe(count - 1);
    await page.keyboard.press('Tab');
    await expect.poll(async () => (await focusState()).activeIndex).toBe(0);

    await page.keyboard.press('Escape');
    await expect(dialog).toBeHidden();
    await expect(trigger).toBeFocused();
  });

  test('keeps the keyboard focus ring visible on dark player surfaces in light theme', async ({ page }) => {
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles(firstFile);
    await expect.poll(() => page.locator('audio').evaluateAll(
      (elements) => (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2),
    )).toBe(true);

    await page.getByTestId('theme-menu').click();
    await page.getByTestId('theme-option-light').click();
    await expect.poll(() => page.evaluate(() => document.documentElement.classList.contains('light'))).toBe(true);

    const trigger = page.getByTestId('player-open-queue');
    await expect(trigger).toBeEnabled();
    await trigger.focus();
    await page.keyboard.press('Tab');
    await page.keyboard.press('Shift+Tab');
    await expect(trigger).toBeFocused();
    await expect.poll(() => trigger.evaluate((element) => element.matches(':focus-visible'))).toBe(true);
    await page.keyboard.press('Enter');

    const dialog = page.getByRole('dialog', { name: 'Playback Queue', exact: true });
    await expect(dialog).toBeVisible();
    const initialFocus = page.getByRole('textbox', { name: 'New playlist name', exact: true });
    await expect(initialFocus).toBeFocused();
    await expect.poll(() => initialFocus.evaluate((element) => element.matches(':focus-visible'))).toBe(true);
    const focusContrast = await page.evaluate(() => {
      const modal = document.querySelector('.player-queue-modal');
      const focused = document.activeElement;
      if (!modal || !(focused instanceof HTMLElement)) return null;

      const relativeLuminance = (color: string) => {
        const channels = color.match(/[\d.]+/g);
        if (!channels || channels.length < 3) return null;
        const linear = channels.slice(0, 3).map((channel) => {
          const value = Number(channel) / 255;
          return value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
        });
        return linear[0] * 0.2126 + linear[1] * 0.7152 + linear[2] * 0.0722;
      };

      const outlineColor = window.getComputedStyle(focused).outlineColor;
      const surfaceColor = window.getComputedStyle(modal).backgroundColor;
      const outlineLuminance = relativeLuminance(outlineColor);
      const surfaceLuminance = relativeLuminance(surfaceColor);
      if (outlineLuminance === null || surfaceLuminance === null) return null;

      const [lighter, darker] = [outlineLuminance, surfaceLuminance].sort((left, right) => right - left);
      return {
        outlineStyle: window.getComputedStyle(focused).outlineStyle,
        ratio: (lighter + 0.05) / (darker + 0.05),
      };
    });

    expect(focusContrast?.outlineStyle).toBe('solid');
    expect(focusContrast?.ratio).toBeGreaterThanOrEqual(3);

    await page.keyboard.press('Escape');
    await expect(dialog).toBeHidden();
    const rating = page.getByTestId('player-rating-3');
    await rating.focus();
    await page.keyboard.press('Tab');
    await page.keyboard.press('Shift+Tab');
    await expect(rating).toBeFocused();
    await expect.poll(() => rating.evaluate((element) => element.matches(':focus-visible'))).toBe(true);
    const ratingFocus = await rating.evaluate((element) => {
      const display = element.closest('.player-display');
      if (!display) return null;

      const expectedColor = document.createElement('span');
      expectedColor.style.color = window.getComputedStyle(display).getPropertyValue('--slskdn-affordance-outline');
      display.append(expectedColor);
      const expectedOutline = window.getComputedStyle(expectedColor).color;
      expectedColor.remove();

      const style = window.getComputedStyle(element);
      return { expectedOutline, outlineColor: style.outlineColor, outlineStyle: style.outlineStyle };
    });
    expect(ratingFocus?.outlineStyle).toBe('solid');
    expect(ratingFocus?.outlineColor).toBe(ratingFocus?.expectedOutline);

    for (const name of ['Playback volume', 'Playback speed', 'Seek playback']) {
      const control = page.getByLabel(name, { exact: true });
      await control.focus();
      await page.keyboard.press('Tab');
      await page.keyboard.press('Shift+Tab');
      await expect(control).toBeFocused();
      await expect.poll(() => control.evaluate((element) => element.matches(':focus-visible'))).toBe(true);
      if (name === 'Playback volume') {
        await expect(control).toHaveAttribute('aria-valuetext', '100%');
        await page.keyboard.press('ArrowLeft');
        await expect(control).toHaveAttribute('aria-valuetext', '99%');
        await page.keyboard.press('ArrowRight');
        await expect(control).toHaveAttribute('aria-valuetext', '100%');
      }
      const fieldFocus = await control.evaluate((element) => {
        const playerBar = element.closest('.player-bar');
        const surface = element.closest('.player-control-pad') || playerBar;
        if (!playerBar || !surface) return null;

        const expectedColor = document.createElement('span');
        expectedColor.style.color = window.getComputedStyle(playerBar).getPropertyValue('--slskdn-affordance-outline');
        playerBar.append(expectedColor);
        const expectedOutline = window.getComputedStyle(expectedColor).color;
        expectedColor.remove();

        const style = window.getComputedStyle(element);
        const parseColor = (color: string) => {
          const channels = color.match(/[\d.]+/g);
          if (!channels || channels.length < 3) return null;
          const values = channels.slice(0, 3).map((channel) => Number(channel) / 255);
          const linear = values.map((value) => value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4);
          return {
            alpha: channels.length > 3 ? Number(channels[3]) : 1,
            luminance: linear[0] * 0.2126 + linear[1] * 0.7152 + linear[2] * 0.0722,
          };
        };
        const outline = parseColor(style.outlineColor);
        const surfaceColor = parseColor(window.getComputedStyle(surface).backgroundColor);
        if (!outline || !surfaceColor) return null;

        const [lighter, darker] = [outline.luminance, surfaceColor.luminance].sort((left, right) => right - left);
        return {
          expectedOutline,
          outlineAlpha: outline.alpha,
          outlineColor: style.outlineColor,
          outlineStyle: style.outlineStyle,
          outlineWidth: style.outlineWidth,
          ratio: (lighter + 0.05) / (darker + 0.05),
        };
      });
      expect(fieldFocus?.outlineStyle, name).toBe('solid');
      expect(fieldFocus?.outlineWidth, name).toBe('3px');
      expect(fieldFocus?.outlineAlpha, name).toBe(1);
      expect(fieldFocus?.outlineColor, name).toBe(fieldFocus?.expectedOutline);
      expect(fieldFocus?.ratio, name).toBeGreaterThanOrEqual(3);
    }
  });

  test('announces equalizer gain values in decibels during keyboard adjustment', async ({ page }) => {
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    await page.getByRole('button', { name: 'Show equalizer', exact: true }).click();

    const gain = page.getByRole('slider', { name: '31 equalizer gain', exact: true });
    await expect(gain).toHaveAttribute('aria-valuetext', '0 dB');
    await page.getByRole('button', { name: 'Enable equalizer', exact: true }).click();
    await gain.focus();
    await page.keyboard.press('ArrowRight');
    await expect.poll(async () => Number(await gain.inputValue())).not.toBe(0);
    const value = await gain.inputValue();
    await expect(gain).toHaveAttribute('aria-valuetext', `${value} dB`);
  });

  test('keeps compact and expanded controls within desktop and narrow viewports', async ({ page }, testInfo) => {
    const minimumMeasuredTouchSize = 44 - 0.01;
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles(firstFile);
    for (const width of [1440, 768, 390, 320]) {
      await page.setViewportSize({ width, height: 900 });
      await expect(page.getByTestId('player-toggle-playback')).toBeInViewport();
      if (width <= 720) {
        const touchBounds = await page.getByTestId('player-toggle-playback').boundingBox();
        expect(touchBounds!.width).toBeGreaterThanOrEqual(minimumMeasuredTouchSize);
        expect(touchBounds!.height).toBeGreaterThanOrEqual(minimumMeasuredTouchSize);
        const ratingBounds = await page.getByTestId('player-rating-1').boundingBox();
        expect(ratingBounds!.width).toBeGreaterThanOrEqual(minimumMeasuredTouchSize);
        expect(ratingBounds!.height).toBeGreaterThanOrEqual(minimumMeasuredTouchSize);
        await expect(page.locator('.player-rating-summary')).toHaveText('Not rated');
        expect(await page.locator('.player-rating-summary').evaluate((element) => element.scrollWidth <= element.clientWidth)).toBe(true);
        await expect(page.locator('.player-visual-tile-controls')).toBeHidden();
        await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
        const visualControls = page.locator('.player-visual-tile-controls').getByRole('button');
        await expect(visualControls).toHaveCount(7);
        for (const control of await visualControls.all()) {
          await control.scrollIntoViewIfNeeded();
          const controlBounds = await control.boundingBox();
          expect(controlBounds!.width).toBeGreaterThanOrEqual(minimumMeasuredTouchSize);
          expect(controlBounds!.height).toBeGreaterThanOrEqual(minimumMeasuredTouchSize);
          expect(controlBounds!.x).toBeGreaterThanOrEqual(0);
          expect(controlBounds!.x + controlBounds!.width).toBeLessThanOrEqual(width);
        }
        const rating = page.getByTestId('player-rating-3');
        await rating.focus();
        await page.keyboard.press('Tab');
        await page.keyboard.press('Shift+Tab');
        await expect(rating).toBeFocused();
        await page.keyboard.press('Space');
        await expect(rating).toHaveAttribute('aria-pressed', 'true');
        expect(await rating.evaluate((element) => getComputedStyle(element).outlineStyle)).toBe('solid');
        await page.keyboard.press('Space');
        await expect(rating).toHaveAttribute('aria-pressed', 'false');
        await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((audio) => !audio.paused))).toBe(true);
        await page.getByRole('button', { name: 'Hide player tools', exact: true }).click();
        await expect(page.locator('.player-title')).toBeInViewport();
        expect(await page.evaluate(() => document.activeElement?.getAttribute('data-testid'))).toBe('player-toggle-playback');
        await page.getByTestId('player-toggle-playback').scrollIntoViewIfNeeded();
      }
      const bar = page.locator('.player-bar');
      const bounds = await bar.boundingBox();
      expect(bounds).not.toBeNull();
      expect(bounds!.x).toBeGreaterThanOrEqual(0);
      expect(bounds!.x + bounds!.width).toBeLessThanOrEqual(width + 1);
      await page.mouse.move(0, 0);
      await expect(page.locator('.ui.popup.visible')).toHaveCount(0);
      await page.screenshot({ path: testInfo.outputPath(`expanded-${width}.png`) });
      await page.getByTestId('player-collapse').click();
      await expect(page.getByTestId('player-collapsed-toggle-playback')).toBeInViewport();
      await expect(page.getByTestId('player-expand')).toBeInViewport();
      if (width <= 720) {
        for (const control of await page.locator('.player-control-cluster').getByRole('button').all()) {
          const touchBounds = await control.boundingBox();
          expect(touchBounds).not.toBeNull();
          expect(touchBounds!.width).toBeGreaterThanOrEqual(minimumMeasuredTouchSize);
          expect(touchBounds!.height).toBeGreaterThanOrEqual(minimumMeasuredTouchSize);
        }
      }
      if (width <= 420) {
        await expect(page.getByTestId('player-collapsed-toggle-mute')).toBeHidden();
        const titleBounds = await page.locator('.player-title').boundingBox();
        expect(titleBounds!.width).toBeGreaterThanOrEqual(72);
      }
      const overflow = await bar.evaluate((element) => element.scrollWidth - element.clientWidth);
      expect(overflow).toBeLessThanOrEqual(1);
      await page.mouse.move(0, 0);
      await expect(page.locator('.ui.popup.visible')).toHaveCount(0);
      await page.screenshot({ path: testInfo.outputPath(`compact-${width}.png`) });
      await page.getByTestId('player-expand').click();
    }
  });

  test.describe('touchscreen layout', () => {
    test.use({ hasTouch: true });
    test('keeps tablet controls touch-sized independently of viewport width', async ({ page }, testInfo) => {
      const minimumMeasuredTouchSize = 44 - 0.01;
      await page.setViewportSize({ width: 768, height: 1024 });
      expect(await page.evaluate(() => matchMedia('(pointer: coarse)').matches)).toBe(true);
      await page.getByLabel('Choose audio files', { exact: true }).setInputFiles(firstFile);
      for (const testId of ['player-toggle-playback', 'player-rating-1']) {
        const bounds = await page.getByTestId(testId).boundingBox();
        expect(bounds).not.toBeNull();
        expect(bounds!.width).toBeGreaterThanOrEqual(minimumMeasuredTouchSize);
        expect(bounds!.height).toBeGreaterThanOrEqual(minimumMeasuredTouchSize);
      }
      await expect(page.locator('.player-visual-tile-controls')).toBeHidden();
      await expect(page.getByTestId('player-toggle-playback')).toBeInViewport();
      await page.mouse.move(0, 0);
      await page.screenshot({ path: testInfo.outputPath('touchscreen-tablet.png') });
    });
  });

  for (const filename of ['Player runtime first.wav', 'Downloaded runtime.wav']) {
    test(`streams ${filename} and restores its queue without autoplay`, async ({ page }) => {
      await page.getByTestId('player-open-file-browser').click();
      const modal = page.getByTestId('player-file-browser-modal');
      await modal.getByTestId('player-file-browser-search').locator('input').fill(filename.replace('.wav', ''));
      const play = modal.getByRole('button', { name: `Play ${filename}`, exact: true });
      await expect(play).toBeVisible({ timeout: 10_000 });
      await play.click();
      await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2 && element.currentSrc.includes('/streams/')))).toBe(true);
      await page.reload({ waitUntil: 'domcontentloaded' });
      await expect(page.locator('.player-title')).toHaveText(filename);
      await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).every((element) => element.paused))).toBe(true);
      await page.getByTestId('player-toggle-playback').click();
      await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    });
  }
});
