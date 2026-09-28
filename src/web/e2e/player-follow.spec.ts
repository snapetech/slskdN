// <copyright file="player-follow.spec.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>
import * as fs from 'node:fs/promises';
import * as path from 'node:path';
import { randomUUID } from 'node:crypto';
import { expect, test, type WebSocketRoute } from '@playwright/test';
import { parse } from 'yaml';
import { MultiPeerHarness } from './harness/MultiPeerHarness';
import { login } from './helpers';

function followTone(): Buffer {
  const rate = 22050;
  const samples = rate * 45;
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
  for (let index = 0; index < samples; index++) {
    wave.writeInt16LE(Math.round(Math.sin(index * 2 * Math.PI * 440 / rate) * 1600), 44 + index * 2);
  }
  return wave;
}

test.use({ serviceWorkers: 'block' });

test('follows a real room from routed messaging and catches up after automatic transport recovery', async ({ page, request }, testInfo) => {
  test.setTimeout(120_000);
  const harness = new MultiPeerHarness();
  const node = await harness.startNode('A', [], { noConnect: true });
  let allowHub = true;
  let socket: WebSocketRoute | null = null;
  let connections = 0;
  let joins = 0;
  let negotiations = 0;
  let snapshots = 0;
  page.on('request', (request) => {
    if (request.url().includes('/hub/listening-party/negotiate')) negotiations++;
    if (request.url().includes('/api/v0/listening-party/') && request.method() === 'GET') snapshots++;
    if (request.url().includes('/hub/listening-party') && request.method() === 'POST' && request.postData()?.includes('"target":"JoinParty"')) joins++;
  });
  await page.route('**/hub/listening-party/negotiate*', async (route) => {
    if (allowHub) await route.continue();
    else await route.abort('connectionfailed');
  });
  await page.routeWebSocket('**/hub/listening-party*', async (route) => {
    if (!allowHub) {
      await route.close({ code: 1011, reason: 'Controlled transport interruption' });
      return;
    }
    connections++;
    socket = route;
    const server = route.connectToServer();
    route.onMessage((message) => {
      if (typeof message === 'string' && message.includes('"target":"JoinParty"')) joins++;
      server.send(message);
    });
  });
  try {
    const configuration = parse(await fs.readFile(path.join(node.getAppDir(), 'config', 'slskd.yml'), 'utf8'));
    expect(configuration.feature.Dht).toBe(false);
    expect(configuration.dhtRendezvous).toMatchObject({ enabled: false, lanOnly: true, bootstrapRouters: [] });
    await fs.writeFile(path.join(node.getAppDir(), 'downloads', 'Browser follow tone.wav'), followTone());
    const session = await request.post(`${node.apiUrl}/api/v0/session`, { data: { username: node.nodeCfg.username, password: node.nodeCfg.password } });
    expect(session.ok()).toBe(true);
    const headers = { Authorization: `Bearer ${(await session.json()).token}` };
    const library = await request.get(`${node.apiUrl}/api/v0/library/items/browser?query=Browser%20follow%20tone&kinds=Audio`, { headers });
    expect(library.ok()).toBe(true);
    const contentId = (await library.json()).files[0]?.contentId;
    expect(contentId).toBeTruthy();
    const created = await request.post(`${node.apiUrl}/api/v0/pods`, {
      headers, data: { requestingPeerId: node.nodeCfg.username, pod: { name: 'Browser follow room', visibility: 'Unlisted', isPublic: true, channels: [{ channelId: 'music', name: 'Music' }] } },
    });
    expect(created.ok(), await created.text()).toBe(true);
    const podId = (await created.json()).podId;
    const publish = async (action: string, positionSeconds: number, title = 'Browser follow tone') => {
      const response = await request.post(`${node.apiUrl}/api/v0/listening-party/${encodeURIComponent(podId)}/music`, {
        headers, data: { action, contentId, title, positionSeconds },
      });
      expect(response.ok(), await response.text()).toBe(true);
    };
    await publish('play', 0);
    await page.addInitScript(() => localStorage.setItem('slskdn.player.collapsed', 'false'));
    await login(page, node.nodeCfg);
    await page.goto(`${node.nodeCfg.baseUrl}/pods/${encodeURIComponent(podId)}/channels/music`);
    await expect(page.getByRole('status', { name: 'Listen Along live' })).toBeVisible();
    const follow = page.getByRole('button', { name: 'Follow room broadcast', exact: true });
    await expect(follow).toHaveAttribute('aria-pressed', 'false');
    await follow.click();
    await expect(follow).toHaveAttribute('aria-pressed', 'true');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((audio) => !audio.paused && audio.currentTime > 0.5))).toBe(true);
    await publish('pause', 8);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((audio) => audio.paused && Math.abs(audio.currentTime - 8) < 0.4))).toBe(true);
    await publish('seek', 12);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((audio) => !audio.paused && audio.currentTime >= 12))).toBe(true);

    // Use the real SPA link: a document reload legitimately ends this session.
    const connectionsBeforeNavigation = connections;
    await page.locator('a[href="/downloads"]').first().click();
    await expect(page).toHaveURL(/\/downloads$/);
    await expect(follow).toHaveCount(0);
    await publish('pause', 16);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((audio) => audio.paused && Math.abs(audio.currentTime - 16) < 0.4))).toBe(true);
    await page.goBack();
    await expect(page.getByRole('status', { name: 'Listen Along live' })).toBeVisible();
    await expect(follow).toHaveAttribute('aria-pressed', 'true');
    expect(connections).toBe(connectionsBeforeNavigation);

    // Keep real transport/messages. Only interrupt the socket, then let the
    // production reconnect policy rejoin and fetch the newest backend state.
    const previousJoins = joins;
    const previousNegotiations = negotiations;
    const previousSnapshots = snapshots;
    allowHub = false;
    expect(socket).not.toBeNull();
    await socket!.close({ code: 1011, reason: 'Controlled transport interruption' });
    await expect(page.getByRole('status', { name: 'Listen Along connecting' })).toBeVisible();
    await publish('pause', 20);
    allowHub = true;
    await expect(page.getByRole('status', { name: 'Listen Along live' })).toBeVisible();
    await expect.poll(() => joins).toBeGreaterThan(previousJoins);
    expect(negotiations).toBeGreaterThan(previousNegotiations);
    await expect.poll(() => snapshots).toBeGreaterThan(previousSnapshots);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((audio) => audio.paused && Math.abs(audio.currentTime - 20) < 0.4))).toBe(true);
    await expect(follow).toHaveAttribute('aria-pressed', 'true');
    await testInfo.attach('room-recovery-transport', {
      body: JSON.stringify({ connections, joins, negotiations, snapshots }, null, 2), contentType: 'application/json',
    });

    for (const width of [390, 320]) {
      await page.setViewportSize({ width, height: 844 });
      await expect(follow).toBeVisible();
      const controls = page.locator('.msgv2-room-playback').getByRole('button');
      for (const control of await controls.all()) {
        const bounds = await control.boundingBox();
        expect(bounds).not.toBeNull();
        expect(bounds!.width).toBeGreaterThanOrEqual(44);
        expect(bounds!.height).toBeGreaterThanOrEqual(44);
        expect(bounds!.x).toBeGreaterThanOrEqual(0);
        expect(bounds!.x + bounds!.width).toBeLessThanOrEqual(width);
      }
      const glyphs = await controls.locator('i.icon').evaluateAll((icons) => icons.map((icon) => getComputedStyle(icon, '::before').content));
      expect(glyphs).toHaveLength(5);
      expect(glyphs.every((content) => !['none', 'normal', '""', "''"].includes(content))).toBe(true);
      await page.mouse.move(0, 0);
      await expect(page.locator('.ui.popup.visible')).toHaveCount(0);
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
      await page.screenshot({ path: testInfo.outputPath(`room-follow-${width}.png`), fullPage: true });
    }
    await follow.focus();
    await page.keyboard.press('Space');
    await expect(follow).toHaveAttribute('aria-pressed', 'false');
    await publish('play', 22, 'Keyboard resume');
    await expect(page.locator('.msgv2-room-playback').getByText('Keyboard resume', { exact: true })).toBeVisible();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((audio) => audio.paused && Math.abs(audio.currentTime - 20) < 0.4))).toBe(true);
    await page.keyboard.press('Space');
    await expect(follow).toHaveAttribute('aria-pressed', 'true');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((audio) => !audio.paused && audio.currentTime >= 22))).toBe(true);
    await publish('stop', 0);
    await expect(follow).toHaveAttribute('aria-pressed', 'false');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).every((audio) => audio.paused))).toBe(true);

    await page.setViewportSize({ width: 1280, height: 900 });
    await publish('play', 0, 'Final follow ownership');
    await expect(page.locator('.msgv2-room-playback').getByText('Final follow ownership', { exact: true })).toBeVisible();
    await follow.click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).some((audio) => !audio.paused && audio.currentTime > 0.5))).toBe(true);
    await page.locator('a[href="/downloads"]').first().click();
    await expect(page).toHaveURL(/\/downloads$/);
    await page.getByTestId('player-stop').click();
    await expect(page.locator('.player-title')).toHaveText('Nothing playing');
    await publish('play', 24, 'After local stop');
    await page.goBack();
    await expect(page.locator('.msgv2-room-playback').getByText('After local stop', { exact: true })).toBeVisible();
    await expect(follow).toHaveAttribute('aria-pressed', 'false');
    await expect(page.locator('.player-title')).toHaveText('Nothing playing');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) => (elements as HTMLAudioElement[]).every((audio) => audio.paused))).toBe(true);
    const startup = await fs.readFile(path.join(node.getAppDir(), 'artifacts', 'stdout.log'), 'utf8');
    expect(startup).not.toMatch(/Starting DHT rendezvous service|DHT engine started|Announced overlay port .* to DHT/);
  } finally {
    await page.close();
    await harness.stopAll();
  }
});

test('follows a live room snapshot delivered between authenticated mesh members', async ({ page, request }, testInfo) => {
  test.setTimeout(120_000);
  const harness = new MultiPeerHarness();
  const sharedMedia = path.resolve(
    process.cwd(),
    '..',
    '..',
    'test-data',
    'slskdn-test-fixtures',
    'music',
    `cross-node-${randomUUID()}`,
  );
  const tone = followTone();
  const encodedRoom = (podId: string) => encodeURIComponent(podId);
  const memberKey = (peerId: string) => `listen-${peerId}-fixture-key`;
  const hostPeerId = 'nodea';
  const listenerPeerId = 'nodeb';
  try {
    await fs.mkdir(sharedMedia, { recursive: true });
    await fs.writeFile(path.join(sharedMedia, 'Cross-node follow tone.wav'), tone);
    const host = await harness.startNode('A', ['test-data/slskdn-test-fixtures/music', sharedMedia], {
      noConnect: true,
      radioMesh: true,
      listenAlongPeers: [hostPeerId, listenerPeerId],
    });
    const listener = await harness.startNode('B', ['test-data/slskdn-test-fixtures/music', sharedMedia], {
      noConnect: true,
      radioMesh: true,
      listenAlongPeers: [hostPeerId, listenerPeerId],
    });
    const hostPeerHeaders = { 'X-API-Key': memberKey(hostPeerId) };
    const hostSession = await request.post(`${host.apiUrl}/api/v0/session`, {
      data: { username: host.nodeCfg.username, password: host.nodeCfg.password },
    });
    expect(hostSession.ok()).toBe(true);
    const hostHeaders = { Authorization: `Bearer ${(await hostSession.json()).token}` };
    const listenerSession = await request.post(`${listener.apiUrl}/api/v0/session`, {
      data: { username: listener.nodeCfg.username, password: listener.nodeCfg.password },
    });
    expect(listenerSession.ok()).toBe(true);
    const listenerHeaders = { Authorization: `Bearer ${(await listenerSession.json()).token}` };

    const connected = await request.post(`${listener.apiUrl}/api/v0/overlay/connect`, {
      headers: listenerHeaders,
      data: { address: '127.0.0.1', port: host.getOverlayPort() },
    });
    expect(connected.ok(), await connected.text()).toBe(true);
    expect(await connected.text()).toContain('"connected":true');

    const readToneContentId = async (apiUrl: string, headers: Record<string, string>) => {
      const response = await request.get(`${apiUrl}/api/v0/library/items?query=Cross-node%20follow%20tone&kinds=Audio&limit=10`, { headers });
      if (!response.ok()) return '';
      const library = await response.json() as { items: Array<{ fileName: string; contentId: string }> };
      return library.items.find((file) =>
        file.fileName === 'Cross-node follow tone.wav')?.contentId ?? '';
    };
    let hostContentId = '';
    await expect.poll(async () => {
      hostContentId = await readToneContentId(host.apiUrl, hostHeaders);
      return hostContentId;
    }).not.toBe('');
    expect(hostContentId).toMatch(/^sha256:[0-9a-f]{64}$/iu);
    let listenerContentId = '';
    await expect.poll(async () => {
      listenerContentId = await readToneContentId(listener.apiUrl, listenerHeaders);
      return listenerContentId;
    }).not.toBe('');
    expect(listenerContentId).toMatch(/^sha256:[0-9a-f]{64}$/iu);
    expect(listenerContentId).toBe(hostContentId);

    const created = await request.post(`${host.apiUrl}/api/v0/pods`, {
      headers: hostPeerHeaders,
      data: {
        requestingPeerId: hostPeerId,
        pod: { name: 'Cross-node follow room', visibility: 'Unlisted', isPublic: true, channels: [{ channelId: 'music', name: 'Music' }] },
      },
    });
    expect(created.ok(), await created.text()).toBe(true);
    const podId = (await created.json()).podId as string;
    const replica = await request.post(`${listener.apiUrl}/api/v0/pods`, {
      headers: listenerHeaders,
      data: {
        requestingPeerId: listener.nodeCfg.username,
        pod: { podId, name: 'Cross-node follow room replica', visibility: 'Unlisted', isPublic: true, channels: [{ channelId: 'music', name: 'Music' }] },
      },
    });
    expect(replica.ok(), await replica.text()).toBe(true);
    for (const [node, ownerHeaders, peerId] of [
      [host, hostPeerHeaders, listenerPeerId],
      [listener, listenerHeaders, hostPeerId],
    ] as const) {
      const joined = await request.post(`${node.apiUrl}/api/v0/pods/${encodedRoom(podId)}/join`, {
        headers: { 'X-API-Key': memberKey(peerId) },
        data: { peerId },
      });
      expect(joined.ok(), await joined.text()).toBe(true);

      const membersResponse = await request.get(`${node.apiUrl}/api/v0/pods/${encodedRoom(podId)}/members`, {
        headers: ownerHeaders,
      });
      expect(membersResponse.ok(), await membersResponse.text()).toBe(true);
      const members = await membersResponse.json() as Array<{ peerId: string }>;
      expect(members.map((member) => member.peerId.toLowerCase())).toContain(peerId.toLowerCase());
    }
    for (const [node, headers, peerId] of [
      [host, hostPeerHeaders, listenerPeerId],
      [listener, listenerHeaders, hostPeerId],
    ] as const) {
      const members = await request.get(`${node.apiUrl}/api/v0/pods/${encodedRoom(podId)}/members`, { headers });
      expect(members.ok(), await members.text()).toBe(true);
      expect((await members.json()).some((member: { peerId: string; isBanned: boolean }) =>
        member.peerId.toLowerCase() === peerId.toLowerCase() && !member.isBanned)).toBe(true);
    }

    const publish = async (action: string, positionSeconds: number, title: string) => {
      const response = await request.post(`${host.apiUrl}/api/v0/listening-party/${encodedRoom(podId)}/music`, {
        headers: hostPeerHeaders,
        data: { action, contentId: hostContentId, title, positionSeconds },
      });
      expect(response.ok(), await response.text()).toBe(true);
    };
    const readListenerState = () => request.get(
      `${listener.apiUrl}/api/v0/listening-party/${encodedRoom(podId)}/music`, { headers: listenerHeaders },
    );
    await publish('play', 0, 'Cross-node opening');
    const initialState = await readListenerState();
    if (initialState.status() !== 200) {
      const logs = await Promise.all([host, listener].map(async (node) => {
        const stdout = await fs.readFile(path.join(node.getAppDir(), 'artifacts', 'stdout.log'), 'utf8')
          .catch((error: unknown) => error instanceof Error ? error.message : String(error));
        const relevant = stdout.split('\n').filter((line) =>
          /listen-along|ApplyListenAlong|PodsMeshService|PodMessageRouter|ServiceClient|ServiceRouter/iu.test(line));
        return `=== ${node.nodeCfg.username} ===\n${relevant.slice(-80).join('\n')}`;
      }));
      await testInfo.attach('cross-node-listen-along-diagnostics', {
        body: logs.join('\n\n'),
        contentType: 'text/plain',
      });
    }
    const initialStateBody = await initialState.text();
    expect(initialState.status(), initialStateBody).toBe(200);
    expect(JSON.parse(initialStateBody)).toMatchObject({
      podId,
      channelId: 'music',
      hostPeerId,
      action: 'play',
      title: 'Cross-node opening',
    });

    await page.addInitScript(() => localStorage.setItem('slskdn.player.collapsed', 'false'));
    await login(page, listener.nodeCfg);
    await page.goto(`${listener.nodeCfg.baseUrl}/pods/${encodedRoom(podId)}/channels/music`);
    await expect(page.getByRole('status', { name: 'Listen Along live' })).toBeVisible();
    const follow = page.getByRole('button', { name: 'Follow room broadcast', exact: true });
    await follow.click();
    await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
      (elements as HTMLAudioElement[]).some((audio) => !audio.paused && audio.currentTime > 0.5))).toBe(true);

    await publish('pause', 8, 'Cross-node pause');
    await expect.poll(async () => {
      const response = await readListenerState();
      if (response.status() !== 200) return '';
      const state = await response.json();
      return `${state.action}:${state.title}`;
    }).toBe('pause:Cross-node pause');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
      (elements as HTMLAudioElement[]).some((audio) => audio.paused && Math.abs(audio.currentTime - 8) < 0.4))).toBe(true);

    await publish('seek', 12, 'Cross-node seek');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
      (elements as HTMLAudioElement[]).some((audio) => !audio.paused && audio.currentTime >= 12))).toBe(true);
    await publish('stop', 0, 'Cross-node stop');
    await expect.poll(async () => (await readListenerState()).status()).toBe(204);
    await expect(follow).toHaveAttribute('aria-pressed', 'false');
    await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
      (elements as HTMLAudioElement[]).every((audio) => audio.paused))).toBe(true);

    const ban = await request.post(`${host.apiUrl}/api/v0/pods/${encodedRoom(podId)}/ban`, {
      headers: hostPeerHeaders,
      data: { peerId: listenerPeerId },
    });
    expect(ban.ok(), await ban.text()).toBe(true);
    await publish('play', 15, 'Must not reach banned member');
    expect((await readListenerState()).status()).toBe(204);
  } finally {
    await page.close();
    await harness.stopAll();
    await fs.rm(sharedMedia, { recursive: true, force: true });
  }
});
