// <copyright file="player-host.spec.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>
import * as fs from 'node:fs/promises';
import * as path from 'node:path';
import { expect, test } from '@playwright/test';
import { MultiPeerHarness } from './harness/MultiPeerHarness';
import { login } from './helpers';

function hostTone(): Buffer {
  const rate = 22050;
  const wave = Buffer.alloc(44 + rate * 120 * 2);
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
  wave.writeUInt32LE(wave.length - 44, 40);
  for (let index = 0; index < rate * 120; index++) wave.writeInt16LE(Math.round(Math.sin(index * Math.PI * 880 / rate) * 1200), 44 + index * 2);
  return wave;
}

test.use({ serviceWorkers: 'block' });
test('keeps an explicit host synchronized across navigation, paused seeks, tracks and Stop', async ({ browser, page, request }, testInfo) => {
  test.setTimeout(120_000);
  const harness = new MultiPeerHarness();
  const node = await harness.startNode('A', [], { noConnect: true, radioMesh: true });
  const listenerContext = await browser.newContext({ serviceWorkers: 'block' });
  const listener = await listenerContext.newPage();
  try {
    for (const name of ['Host first.wav', 'Host second.wav']) {
      await fs.writeFile(path.join(node.getAppDir(), 'downloads', name), hostTone());
    }
    const session = await request.post(`${node.apiUrl}/api/v0/session`, { data: { username: node.nodeCfg.username, password: node.nodeCfg.password } });
    expect(session.ok()).toBe(true);
    const headers = { Authorization: `Bearer ${(await session.json()).token}` };
    const created = await request.post(`${node.apiUrl}/api/v0/pods`, {
      headers, data: { requestingPeerId: node.nodeCfg.username, pod: { name: 'Persistent player host', visibility: 'Unlisted', isPublic: true, channels: [{ channelId: 'music', name: 'Music' }] } },
    });
    expect(created.ok(), await created.text()).toBe(true);
    const podId = (await created.json()).podId;
    const roomUrl = `${node.nodeCfg.baseUrl}/pods/${encodeURIComponent(podId)}/channels/music`;
    const stateUrl = `${node.apiUrl}/api/v0/listening-party/${encodeURIComponent(podId)}/music`;
    const snapshot = async () => {
      const response = await request.get(stateUrl, { headers });
      return response.status() === 204 ? null : await response.json();
    };
    for (const target of [page, listener]) {
      await target.addInitScript(() => localStorage.setItem('slskdn.player.collapsed', 'false'));
      await login(target, node.nodeCfg);
    }
    let publications = 0;
    page.on('request', (request) => {
      if (request.method() === 'POST' && request.url().includes('/api/v0/listening-party/')) publications++;
    });
    await page.goto(roomUrl);
    await page.getByTestId('player-open-file-browser').click();
    const modal = page.getByTestId('player-file-browser-modal');
    await modal.getByTestId('player-file-browser-search').locator('input').fill('Host first');
    await modal.getByRole('button', { name: 'Play Host first.wav', exact: true }).click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.some((audio) => !audio.paused && audio.currentTime > 0.5))).toBe(true);
    await page.getByRole('button', { name: 'List room broadcast in mesh directory', exact: true }).click();
    await page.getByRole('button', { name: 'Broadcast current track to room', exact: true }).click();
    await expect.poll(async () => (await snapshot())?.action).toBe('play');
    const partyId = (await snapshot()).partyId;
    await page.getByTestId('player-collapse').click();
    await page.setViewportSize({ width: 320, height: 844 });
    const compactStatus = page.locator('.player-bar-collapsed .player-subtitle');
    await expect(compactStatus).toHaveText('Broadcasting');
    expect(await compactStatus.evaluate((element) => element.scrollWidth <= element.clientWidth)).toBe(true);
    await page.mouse.move(0, 0);
    await expect(page.locator('.ui.popup.visible')).toHaveCount(0);
    await page.screenshot({ path: testInfo.outputPath('host-compact-320.png'), fullPage: true });
    await page.getByTestId('player-expand').click();
    await page.setViewportSize({ width: 1280, height: 900 });
    await listener.goto(roomUrl);
    await listener.getByRole('button', { name: 'Follow room broadcast', exact: true }).click();
    await expect.poll(() => listener.locator('audio').evaluateAll((elements) => elements.some((audio) => !audio.paused && audio.currentTime > 0.5))).toBe(true);
    await page.locator('a[href="/downloads"]').first().click();
    await expect(page).toHaveURL(/\/downloads$/);
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(async () => (await snapshot())?.action).toBe('pause');
    await expect.poll(() => listener.locator('audio').evaluateAll((elements) => elements.every((audio) => audio.paused))).toBe(true);
    const seek = page.getByLabel('Seek playback', { exact: true });
    await seek.press('Home');
    for (let second = 0; second < 14; second++) await seek.press('ArrowRight');
    await expect.poll(async () => {
      const state = await snapshot();
      return state?.action === 'pause' && Math.abs(state.positionSeconds - 14) < 0.4;
    }).toBe(true);
    await expect.poll(() => listener.locator('audio').evaluateAll((elements) => elements.some((audio) => audio.paused && Math.abs(audio.currentTime - 14) < 0.4))).toBe(true);
    await page.getByTestId('player-toggle-playback').click();
    await expect.poll(async () => (await snapshot())?.action).toBe('play');
    await expect.poll(() => listener.locator('audio').evaluateAll((elements) => elements.some((audio) => !audio.paused && audio.currentTime >= 14))).toBe(true);
    const beforeIdle = publications;
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.some((audio) => audio.currentTime >= 18))).toBe(true);
    expect(publications).toBe(beforeIdle);
    await seek.press('Home');
    for (let second = 0; second < 4; second++) await seek.press('ArrowRight');
    await expect.poll(async () => {
      const state = await snapshot();
      return state?.action === 'seek' && Math.abs(state.positionSeconds - 4) < 0.4;
    }).toBe(true);
    await expect.poll(() => listener.locator('audio').evaluateAll((elements) => elements.some((audio) => !audio.paused && audio.currentTime >= 4))).toBe(true);
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    await page.getByTestId('player-open-file-browser').click();
    await modal.getByTestId('player-file-browser-search').locator('input').fill('Host second');
    await modal.getByRole('button', { name: 'Play Host second.wav', exact: true }).click();
    await expect.poll(async () => (await snapshot())?.title).toBe('Host second.wav');
    expect((await snapshot()).partyId).toBe(partyId);
    await expect(listener.locator('.player-title')).toHaveText('Host second.wav');
    await page.getByRole('button', { name: 'Hide player tools', exact: true }).click();
    await page.setViewportSize({ width: 320, height: 844 });
    const title = await page.locator('.player-title').boundingBox();
    const deck = await page.locator('.player-bar').boundingBox();
    expect(title!.y).toBeGreaterThanOrEqual(deck!.y);
    expect(title!.y + title!.height).toBeLessThanOrEqual(deck!.y + deck!.height);
    expect(await page.locator('.player-bar').evaluate((element) => element.scrollTop)).toBe(0);
    const stop = page.getByRole('button', { name: 'Stop active room broadcast', exact: true });
    await expect(stop).toBeVisible();
    const bounds = await stop.boundingBox();
    expect(bounds!.height).toBeGreaterThanOrEqual(44);
    await page.mouse.move(0, 0);
    await expect(page.locator('.ui.popup.visible')).toHaveCount(0);
    await page.screenshot({ path: testInfo.outputPath('host-controls-320.png'), fullPage: true });
    await stop.click();
    await expect.poll(snapshot).toBeNull();
    await expect.poll(() => listener.locator('audio').evaluateAll((elements) => elements.every((audio) => audio.paused))).toBe(true);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.some((audio) => !audio.paused))).toBe(true);
    const directory = async () => {
      const response = await request.get(`${node.apiUrl}/api/v0/listening-party`, { headers });
      expect(response.ok()).toBe(true);
      return response.json();
    };
    expect((await directory()).some((entry: { partyId: string }) => entry.partyId === partyId)).toBe(false);
    await page.goto(roomUrl);
    await page.getByRole('button', { name: 'List room broadcast in mesh directory', exact: true }).click();
    await page.getByRole('button', { name: 'Broadcast current track to room', exact: true }).click();
    await expect.poll(async () => (await snapshot())?.partyId).toBeTruthy();
    const reloadedPartyId = (await snapshot()).partyId;
    expect((await directory()).some((entry: { partyId: string }) => entry.partyId === reloadedPartyId)).toBe(true);
    // Reload ends browser ownership; the panel must still stop the existing
    // server broadcast using its actual identity, including directory cleanup.
    await page.reload();
    await expect(page.getByRole('status', { name: 'Listen Along live' })).toBeVisible();
    await page.getByRole('button', { name: 'Stop room broadcast', exact: true }).click();
    await expect.poll(snapshot).toBeNull();
    expect((await directory()).some((entry: { partyId: string }) => entry.partyId === reloadedPartyId)).toBe(false);
    expect(publications).toBeLessThan(40);
    await testInfo.attach('host-publication-count', { body: JSON.stringify({ publications }), contentType: 'application/json' });
  } finally {
    await listenerContext.close();
    await page.close();
    await harness.stopAll();
  }
});
