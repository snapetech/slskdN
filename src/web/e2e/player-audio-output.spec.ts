// <copyright file="player-audio-output.spec.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as fs from 'node:fs/promises';
import * as os from 'node:os';
import * as path from 'node:path';
import { expect, test } from '@playwright/test';
import { MultiPeerHarness } from './harness/MultiPeerHarness';
import { makeTone } from './fixtures/player-tone';
import { login } from './helpers';

test.use({ serviceWorkers: 'block', video: 'off', trace: 'off' });
test.setTimeout(90_000);

test.describe('isolated player audio output', () => {
  test.skip(
    process.env.SLSKDN_PLAYER_AUDIO_OUTPUT !== '1',
    'Run through test:player:audio-output with unmuted Chromium and a virtual sink.',
  );

  const harness = new MultiPeerHarness();
  let fixtureDirectory: string;
  let tonePath: string;

  test.beforeAll(async () => {
    fixtureDirectory = await fs.mkdtemp(path.join(os.tmpdir(), 'player-audio-output-'));
    tonePath = path.join(fixtureDirectory, 'Output verification.wav');
    await fs.writeFile(tonePath, makeTone(20));
    await harness.startNode('A', [], { noConnect: true });
  });

  test.afterAll(async () => {
    try { await harness.stopAll(); }
    finally { if (fixtureDirectory) await fs.rm(fixtureDirectory, { recursive: true, force: true }); }
  });

  test.beforeEach(async ({ browser, page }) => {
    const browserSession = await browser.newBrowserCDPSession();
    try {
      const { targetInfos } = await browserSession.send('Target.getTargets');
      const pageTarget = targetInfos.find((target) => target.type === 'page' && target.browserContextId);
      expect(pageTarget?.browserContextId, 'Playwright must expose its isolated browser context to CDP.').toBeTruthy();
      await browserSession.send('Browser.grantPermissions', {
        browserContextId: pageTarget!.browserContextId!,
        origin: new URL(harness.getNode('A').nodeCfg.baseUrl).origin,
        permissions: ['speakerSelection'],
      });
    } finally {
      await browserSession.detach();
    }
    await page.addInitScript(() => localStorage.setItem('slskdn.player.collapsed', 'false'));
    await login(page, harness.getNode('A').nodeCfg);
  });

  test('routes generated PCM through the player output selector to an isolated virtual sink', async ({ page }) => {
    const expectedOutputLabel = process.env.SLSKDN_PLAYER_AUDIO_SINK_LABEL;
    expect(expectedOutputLabel).toBeTruthy();
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    const output = page.getByLabel('Audio output device', { exact: true });
    await expect(output).toBeVisible();
    const outputDevices = await output.locator('option').evaluateAll((elements) =>
      elements.map((element) => ({ value: (element as HTMLOptionElement).value, label: element.textContent || '' })));
    const testSink = outputDevices.find((device) =>
      device.value !== 'default' && device.label.includes(expectedOutputLabel!));
    const selectableDeviceCount = outputDevices.filter((device) => device.value !== 'default').length;
    expect(testSink,
      `The virtual ${expectedOutputLabel} sink was not exposed among ${selectableDeviceCount} selectable browser outputs.`)
      .toBeDefined();
    await output.selectOption(testSink!.value);
    await expect(output).toHaveValue(testSink!.value);

    await page.getByLabel('Choose audio files', { exact: true }).setInputFiles(tonePath);
    const audio = page.locator('audio').first();
    await expect.poll(() => audio.evaluate((element) => {
      if (!(element instanceof HTMLAudioElement)) return false;
      return !element.paused && element.currentTime > 6;
    }), { timeout: 30_000 }).toBe(true);

    // The runner records the sink selected in the PlayerBar through a
    // temporary PulseAudio null sink. Holding playback here gives the external
    // recorder clean PCM windows without routing test audio to a physical output.
    await page.waitForTimeout(5_000);
  });
});
