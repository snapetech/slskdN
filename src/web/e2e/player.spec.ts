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
import { expect, test } from '@playwright/test';

// Generated PCM exercises the browser decoder without downloaded media,
// personal files, or remote peer requests.

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

  test('decodes server AIFF and seeks absolutely while paused and playing', async ({ page }) => {
    await page.getByTestId('player-open-file-browser').click();
    const modal = page.getByTestId('player-file-browser-modal');
    await modal.getByTestId('player-file-browser-search').locator('input').fill('Player decoded runtime');
    await modal.getByRole('button', { name: 'Play Player decoded runtime.aiff', exact: true }).click();
    await page.getByRole('button', { name: 'Decode for playback', exact: true }).click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) =>
      !element.paused && element.currentTime > 0.2 && element.currentSrc.includes('/transcoded')))).toBe(true);
    await page.getByTestId('player-toggle-playback').click();
    const seek = page.getByLabel('Seek playback', { exact: true });
    await seek.press('Home');
    for (let second = 0; second < 12; second++) await seek.press('ArrowRight');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) =>
      element.currentSrc.includes('startSeconds=12')))).toBe(true);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).every((element) => element.paused))).toBe(true);
    await expect(seek).toHaveValue('12');
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) =>
      !element.paused && element.currentTime > 0.2 && element.currentSrc.includes('startSeconds=12')))).toBe(true);
    await seek.press('Home');
    for (let second = 0; second < 5; second++) await seek.press('ArrowRight');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) =>
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
    const seek = page.getByLabel('Seek playback', { exact: true });
    await seek.press('Home');
    for (let second = 0; second < 34; second++) await seek.press('ArrowRight');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).filter((element) => !element.paused).length)).toBe(2);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).filter((element) => !element.paused).length)).toBe(1);
    await expect.poll(() => page.evaluate(() => (window as Window & { __playerAudioContextInstances?: AudioContext[] }).__playerAudioContextInstances!.map((context) => context.state).sort())).toEqual(['running', 'suspended']);
  });

  test('crossfades two local streams and pauses both on transport Pause', async ({ page }) => {
    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles([firstFile, secondFile]);
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    await page.getByTestId('player-toggle-crossfade').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((element) => !element.paused && element.currentTime > 0.2))).toBe(true);
    const seek = page.getByLabel('Seek playback', { exact: true });
    await seek.press('Home');
    for (let second = 0; second < 34; second++) await seek.press('ArrowRight');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).filter((element) => !element.paused).length)).toBe(2);
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).every((element) => element.paused))).toBe(true);
    await expect.poll(() => page.evaluate(() => (window as Window & { __playerAudioContextInstances?: AudioContext[] }).__playerAudioContextInstances!.every((context) => context.state === 'suspended'))).toBe(true);
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).filter((element) => !element.paused).length)).toBe(1);
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
