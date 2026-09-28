// <copyright file="player-radio-network.spec.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as fs from 'node:fs/promises';
import * as path from 'node:path';
import { expect, test } from '@playwright/test';
import { MultiPeerHarness } from './harness/MultiPeerHarness';
import { login } from './helpers';

// A long generated WAV keeps the real paced mesh response active during seeks.
function radioTone(seconds = 180): Buffer {
  const rate = 22050;
  const samples = rate * seconds;
  const wave = Buffer.alloc(44 + samples * 2);
  wave.write('RIFF', 0);
  wave.writeUInt32LE(wave.length - 8, 4);
  wave.write('WAVEfmt ', 8);
  wave.writeUInt32LE(16, 16);
  wave.writeUInt16LE(1, 20);
  wave.writeUInt16LE(1, 22);
  wave.writeUInt32LE(rate, 24);
  wave.writeUInt32LE(rate * 2, 28);
  wave.writeUInt16LE(2, 32);
  wave.writeUInt16LE(16, 34);
  wave.write('data', 36);
  wave.writeUInt32LE(samples * 2, 40);
  for (let index = 0; index < samples; index++) wave.writeInt16LE(Math.round(Math.sin(index * 2 * Math.PI * 440 / rate) * 1600), 44 + index * 2);
  return wave;
}

test.describe('listed radio between isolated nodes', () => {
  test.use({ serviceWorkers: 'block' });
  test.setTimeout(120_000);
  let harness: MultiPeerHarness;

  test.beforeAll(async () => {
    harness = new MultiPeerHarness();
    await harness.startNode('A', [], { noConnect: true, radioMesh: true });
    await harness.startNode('B', [], { noConnect: true, radioMesh: true });
    await fs.writeFile(path.join(harness.getNode('A').getAppDir(), 'downloads', 'Radio network tone.wav'), radioTone());
  });

  test.afterAll(async () => {
    if (harness) await harness.stopAll();
  });

  test('plays and seeks the real host snapshot through the listener gateway', async ({ page, request }) => {
    const host = harness.getNode('A');
    const listener = harness.getNode('B');
    const hostSession = await request.post(`${host.apiUrl}/api/v0/session`, { data: { username: host.nodeCfg.username, password: host.nodeCfg.password } });
    expect(hostSession.ok()).toBe(true);
    const hostToken = (await hostSession.json()).token;
    const listenerSession = await request.post(`${listener.apiUrl}/api/v0/session`, { data: { username: listener.nodeCfg.username, password: listener.nodeCfg.password } });
    expect(listenerSession.ok()).toBe(true);
    const listenerToken = (await listenerSession.json()).token;
    const hostHeaders = { Authorization: `Bearer ${hostToken}` };
    const listenerHeaders = { Authorization: `Bearer ${listenerToken}` };

    const connection = await request.post(`${listener.apiUrl}/api/v0/overlay/connect`, {
      headers: listenerHeaders, data: { address: '127.0.0.1', port: host.getOverlayPort() },
    });
    expect(await connection.text()).toContain('"connected":true');
    const library = await request.get(`${host.apiUrl}/api/v0/library/items/browser?query=Radio%20network%20tone&kinds=Audio`, { headers: hostHeaders });
    expect(library.ok()).toBe(true);
    const item = (await library.json()).files[0];
    expect(item.contentId).toBeTruthy();
    const publish = await request.post(`${host.apiUrl}/api/v0/listening-party/radio-pod/radio-room`, {
      headers: hostHeaders,
      data: { partyId: 'network-radio', action: 'play', contentId: item.contentId, title: 'Radio network tone', listed: true, allowMeshStreaming: true, positionSeconds: 0 },
    });
    expect(publish.ok(), await publish.text()).toBe(true);
    const directory = await request.get(`${host.apiUrl}/api/v0/listening-party`, { headers: hostHeaders });
    const parties = await directory.json();
    expect(parties[0].transportUsername).toBe('nodeA');

    const remoteDirectory = await request.get(`${listener.apiUrl}/api/v0/listening-party`, { headers: listenerHeaders });
    expect(remoteDirectory.ok()).toBe(true);
    expect((await remoteDirectory.json()).some((party: { partyId: string }) => party.partyId === 'network-radio')).toBe(true);

    await page.addInitScript(() => localStorage.setItem('slskdn.player.collapsed', 'false'));
    const statuses: number[] = [];
    page.on('response', (response) => {
      if (response.url().includes('/api/v0/mesh-streams/')) {
        statuses.push(response.status());
        console.log('Radio HTTP response', response.status(), response.request().headers().range || 'full');
      }
    });
    await login(page, listener.nodeCfg);
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    await page.getByTestId('player-open-listed-radio').click();
    await page.getByRole('button', { name: 'Play Radio network tone from listed radio' }).click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.some((audio) => !audio.paused && audio.currentTime > 1))).toBe(true);
    const seek = page.getByLabel('Seek playback', { exact: true });
    const bounds = await seek.boundingBox();
    expect(bounds).not.toBeNull();
    const replacementResponse = page.waitForResponse((response) => response.url().includes('/api/v0/mesh-streams/') && response.request().headers().range !== 'bytes=0-');
    await seek.click({ position: { x: bounds!.width * 0.88, y: bounds!.height / 2 } });
    expect((await replacementResponse).status()).toBe(206);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => Math.max(...elements.map((audio) => audio.currentTime)))).toBeGreaterThan(150);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.some((audio) => !audio.paused && !audio.seeking && audio.readyState >= 2 && audio.currentTime > 150))).toBe(true);
    const resumedAt = await page.locator('audio').evaluateAll((elements) => Math.max(...elements.map((audio) => audio.currentTime)));
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => Math.max(...elements.map((audio) => audio.currentTime)))).toBeGreaterThan(resumedAt + 1);
    expect(statuses.filter((status) => status === 206).length).toBeGreaterThanOrEqual(2);
    expect(statuses).not.toContain(429);
    expect(statuses).not.toContain(500);
  });
});
