// <copyright file="player-radio-network.spec.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as fs from 'node:fs/promises';
import { execFileSync } from 'node:child_process';
import * as path from 'node:path';
import { expect, test, type APIRequestContext } from '@playwright/test';
import { MultiPeerHarness } from './harness/MultiPeerHarness';
import { login } from './helpers';

async function createRadioRoom(request: APIRequestContext, apiUrl: string, headers: Record<string, string>, name: string): Promise<string> {
  const created = await request.post(`${apiUrl}/api/v0/pods`, {
    headers,
    data: { requestingPeerId: 'fixture-owner', pod: { name, visibility: 'Unlisted', channels: [{ channelId: 'music', name: 'Music' }] } },
  });
  expect(created.ok(), await created.text()).toBe(true);
  const podId = (await created.json()).podId;
  expect(podId).toMatch(/^pod:[a-f0-9]{32}$/u);
  return encodeURIComponent(podId);
}

// Read isolated-node counters without introducing a diagnostic production endpoint.
function trafficTotals(appDir: string): number[] {
  const output = execFileSync('python3', ['-c',
    'import sqlite3,sys,json; connection=sqlite3.connect("file:"+sys.argv[1]+"?mode=ro",uri=True); row=connection.execute("SELECT overlay_upload_bytes, overlay_download_bytes FROM TrafficStats WHERE key=\'global\'").fetchone(); print(json.dumps(row or [0,0])); connection.close()',
    path.join(appDir, 'hashdb.db')], { encoding: 'utf8', timeout: 5000 });
  return JSON.parse(output);
}

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
    await fs.writeFile(path.join(harness.getNode('B').getAppDir(), 'downloads', 'Reverse radio tone.wav'), radioTone(5));
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
    const radioPod = await createRadioRoom(request, host.apiUrl, hostHeaders, 'Network radio room');
    const publish = await request.post(`${host.apiUrl}/api/v0/listening-party/${radioPod}/music`, {
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
    const listenerTraffic = trafficTotals(listener.getAppDir());
    const hostTraffic = trafficTotals(host.getAppDir());
    expect(listenerTraffic[1]).toBeGreaterThan(0);
    expect(hostTraffic[0]).toBeGreaterThanOrEqual(listenerTraffic[1]);
    expect(hostTraffic[1]).toBe(0);
    expect(listenerTraffic[0]).toBe(0);

    // A new ticket evaluates current policy; seeks on the admitted ticket
    // retain their admission and must continue to work above.
    const freshTicket = await request.post(`${listener.apiUrl}/api/v0/listed-radio/network-radio/tickets`, {
      headers: listenerHeaders, data: { contentId: item.contentId },
    });
    expect(freshTicket.status()).toBe(429);
    expect((await freshTicket.json()).code).toBe('radio_fairness_limited');

    const revoke = await request.post(`${host.apiUrl}/api/v0/listening-party/${radioPod}/music`, {
      headers: hostHeaders,
      data: { partyId: 'network-radio', action: 'play', contentId: item.contentId, title: 'Radio network tone', listed: true, allowMeshStreaming: false, positionSeconds: 0 },
    });
    expect(revoke.ok()).toBe(true);
    await seek.click({ position: { x: bounds!.width * 0.35, y: bounds!.height / 2 } });
    await page.getByRole('button', { name: 'Retry radio playback', exact: true }).click();
    await expect(page.getByText('The radio host could not provide this snapshot. Retry or refresh listed radio.')).toBeVisible();
    expect(statuses).not.toContain(500);
    await page.getByTestId('player-open-listed-radio').click();
    await page.getByRole('button', { name: 'Refresh listed radio' }).click();
    await expect(page.getByRole('button', { name: 'Play Radio network tone from listed radio' })).toBeDisabled();

    // The original host has only the inbound TLS link. Its directory must still
    // learn a publication sent in the opposite direction without another connect.
    const reverseLibrary = await request.get(`${listener.apiUrl}/api/v0/library/items/browser?query=Reverse%20radio%20tone&kinds=Audio`, { headers: listenerHeaders });
    expect(reverseLibrary.ok()).toBe(true);
    const reverseItem = (await reverseLibrary.json()).files[0];
    expect(reverseItem.contentId).toBeTruthy();
    const reversePod = await createRadioRoom(request, listener.apiUrl, listenerHeaders, 'Reverse radio room');
    const reversePublish = await request.post(`${listener.apiUrl}/api/v0/listening-party/${reversePod}/music`, {
      headers: listenerHeaders,
      data: { partyId: 'reverse-radio', action: 'play', contentId: reverseItem.contentId, title: 'Reverse radio tone', listed: true, allowMeshStreaming: false, positionSeconds: 0 },
    });
    expect(reversePublish.ok(), await reversePublish.text()).toBe(true);
    const hostRefresh = await request.get(`${host.apiUrl}/api/v0/listening-party?refresh=true`, { headers: hostHeaders });
    expect(hostRefresh.ok()).toBe(true);
    expect((await hostRefresh.json()).some((party: { partyId: string; transportUsername: string }) => party.partyId === 'reverse-radio' && party.transportUsername === 'nodeB')).toBe(true);


  });
  test('renews local radio after actual ticket expiry through manual reselection', async ({ page, request }) => {
    test.setTimeout(180_000);
    const host = harness.getNode('A');
    const session = await request.post(`${host.apiUrl}/api/v0/session`, {
      data: { username: host.nodeCfg.username, password: host.nodeCfg.password },
    });
    expect(session.ok()).toBe(true);
    const headers = { Authorization: `Bearer ${(await session.json()).token}` };
    const library = await request.get(`${host.apiUrl}/api/v0/library/items/browser?query=Radio%20network%20tone&kinds=Audio`, { headers });
    expect(library.ok()).toBe(true);
    const item = (await library.json()).files[0];
    const localPod = await createRadioRoom(request, host.apiUrl, headers, 'Local expiry room');
    const publication = await request.post(`${host.apiUrl}/api/v0/listening-party/${localPod}/music`, {
      headers,
      data: { partyId: 'local-expiry', action: 'play', contentId: item.contentId, title: 'Local expiry tone', listed: true, allowMeshStreaming: true, positionSeconds: 0 },
    });
    expect(publication.ok(), await publication.text()).toBe(true);
    await page.addInitScript(() => localStorage.setItem('slskdn.player.collapsed', 'false'));
    await login(page, host.nodeCfg);
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    await page.getByTestId('player-open-listed-radio').click();
    const acquisition = page.waitForResponse((response) => response.url().includes('/listed-radio/local-expiry/tickets'));
    await page.getByRole('button', { name: 'Play Local expiry tone from listed radio' }).click();
    const response = await acquisition;
    expect(response.ok()).toBe(true);
    const initial = await response.json();
    expect(initial.expiresInSeconds).toBe(120);
    const receivedAt = Date.now();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.some((audio) => !audio.paused && audio.currentTime > 1))).toBe(true);
    await page.getByRole('button', { name: 'Pause local playback', exact: true }).click();
    const valid = await request.get(`${host.apiUrl}${initial.streamUrl}`, { headers: { Range: 'bytes=0-1' } });
    expect(valid.status()).toBe(206);

    // Let the production two-minute lifetime elapse. Cached browser audio does
    // not prove a ticket is valid, so inspect a new HTTP request explicitly.
    await new Promise((resolve) => setTimeout(resolve, Math.max(0, receivedAt + 122_000 - Date.now())));
    const expired = await request.get(`${host.apiUrl}${initial.streamUrl}`, { headers: { Range: 'bytes=0-1' } });
    expect(expired.status()).toBe(401);
    await page.getByTestId('player-open-listed-radio').click();
    const renewal = page.waitForResponse((result) => result.url().includes('/listed-radio/local-expiry/tickets'));
    await page.getByRole('button', { name: 'Play Local expiry tone from listed radio' }).click();
    const renewedResponse = await renewal;
    expect(renewedResponse.ok()).toBe(true);
    const renewed = await renewedResponse.json();
    expect(renewed.streamUrl).not.toBe(initial.streamUrl);
    const renewedRange = await request.get(`${host.apiUrl}${renewed.streamUrl}`, { headers: { Range: 'bytes=0-1' } });
    expect(renewedRange.status()).toBe(206);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => elements.some((audio) => !audio.paused && audio.readyState >= 2 && audio.currentTime > 120))).toBe(true);
    const resumedAt = await page.locator('audio').evaluateAll((elements) => Math.max(...elements.map((audio) => audio.currentTime)));
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => Math.max(...elements.map((audio) => audio.currentTime)))).toBeGreaterThan(resumedAt + 1);
  });

});
