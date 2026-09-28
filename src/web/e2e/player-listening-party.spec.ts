// <copyright file="player-listening-party.spec.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>
import { expect, test } from '@playwright/test';
import { HttpTransportType, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { MultiPeerHarness } from './harness/MultiPeerHarness';

test('reauthorizes two live room participants across leave, disconnect and revocation', async ({ request }) => {
  test.setTimeout(120_000);
  const harness = new MultiPeerHarness();
  const node = await harness.startNode('A', [], { noConnect: true, listenAlongMembers: true });
  const build = (key: string) => new HubConnectionBuilder().withUrl(`${node.apiUrl}/hub/listening-party`, {
    headers: { 'X-API-Key': key }, transport: HttpTransportType.LongPolling,
  }).configureLogging(LogLevel.Error).build();
  const listener = build('room-listener-fixture-key');
  const observer = build('room-observer-fixture-key');
  const events: string[] = [];
  const observerEvents: string[] = [];
  let revoked = 0;
  listener.on('partyState', (state: { title: string }) => { events.push(state.title); });
  listener.on('partyAccessRevoked', () => { revoked++; });
  observer.on('partyState', (state: { title: string }) => { observerEvents.push(state.title); });
  try {
    const session = await request.post(`${node.apiUrl}/api/v0/session`, { data: { username: node.nodeCfg.username, password: node.nodeCfg.password } });
    expect(session.ok()).toBe(true);
    const admin = { Authorization: `Bearer ${(await session.json()).token}` };
    const created = await request.post(`${node.apiUrl}/api/v0/pods`, {
      headers: admin, data: { requestingPeerId: node.nodeCfg.username, pod: { name: 'Player runtime room', isPublic: true, channels: [{ channelId: 'music', name: 'Music' }] } },
    });
    expect(created.ok(), await created.text()).toBe(true);
    const podId = (await created.json()).podId;
    const invalidRoom = await request.post(`${node.apiUrl}/api/v0/listening-party/${encodeURIComponent(podId)}/missing`, {
      headers: admin, data: { action: 'play', contentId: 'fixture-track', title: 'Rejected broadcast', listed: true },
    });
    expect(invalidRoom.status()).toBe(404);
    const initialSnapshot = await request.get(`${node.apiUrl}/api/v0/listening-party/${encodeURIComponent(podId)}/music`, { headers: admin });
    expect(initialSnapshot.status()).toBe(204);
    for (const name of ['roomlistener', 'roomobserver']) {
      const joined = await request.post(`${node.apiUrl}/api/v0/pods/${encodeURIComponent(podId)}/join`, {
        headers: { 'X-API-Key': name === 'roomlistener' ? 'room-listener-fixture-key' : 'room-observer-fixture-key' }, data: { peerId: name },
      });
      expect(joined.ok(), await joined.text()).toBe(true);
    }
    await listener.start();
    await observer.start();
    await listener.invoke('JoinParty', podId, 'music');
    await observer.invoke('JoinParty', podId, 'music');
    const publish = async (title: string) => {
      const response = await request.post(`${node.apiUrl}/api/v0/listening-party/${encodeURIComponent(podId)}/music`, {
        headers: admin, data: { action: 'play', contentId: 'fixture-track', title, positionSeconds: 0 },
      });
      expect(response.ok(), await response.text()).toBe(true);
      await expect.poll(() => observerEvents.includes(title)).toBe(true);
    };
    await publish('First broadcast');
    const stored = await request.get(`${node.apiUrl}/api/v0/podcore/messages/${encodeURIComponent(podId)}/music/count`, { headers: admin });
    expect(stored.ok()).toBe(true);
    expect(await stored.json()).toBeGreaterThan(0);
    await expect.poll(() => events.includes('First broadcast')).toBe(true);
    await listener.invoke('LeaveParty', podId, 'music');
    await publish('After leave');
    expect(events).not.toContain('After leave');
    await listener.invoke('JoinParty', podId, 'music');
    await publish('After rejoin');
    await expect.poll(() => events.includes('After rejoin')).toBe(true);
    await listener.stop();
    await publish('While disconnected');
    expect(events).not.toContain('While disconnected');
    await listener.start();
    await listener.invoke('JoinParty', podId, 'music');
    const snapshot = await request.get(`${node.apiUrl}/api/v0/listening-party/${encodeURIComponent(podId)}/music`, { headers: { 'X-API-Key': 'room-listener-fixture-key' } });
    expect(snapshot.ok()).toBe(true);
    expect((await snapshot.json()).title).toBe('While disconnected');
    await publish('After reconnect');
    await expect.poll(() => events.includes('After reconnect')).toBe(true);
    const currentMembers = await request.get(`${node.apiUrl}/api/v0/pods/${encodeURIComponent(podId)}/members`, { headers: admin });
    expect((await currentMembers.json()).some((member: { peerId: string }) => member.peerId === 'roomlistener')).toBe(true);
    const ban = await request.post(`${node.apiUrl}/api/v0/pods/${encodeURIComponent(podId)}/ban`, { headers: admin, data: { peerId: 'roomlistener' } });
    expect(ban.ok(), await ban.text()).toBe(true);
    await publish('Private after ban');
    await expect.poll(() => revoked).toBe(1);
    expect(events).not.toContain('Private after ban');
    await expect(listener.invoke('JoinParty', podId, 'music')).rejects.toThrow();
    const deniedSnapshot = await request.get(`${node.apiUrl}/api/v0/listening-party/${encodeURIComponent(podId)}/music`, { headers: { 'X-API-Key': 'room-listener-fixture-key' } });
    expect(deniedSnapshot.status()).toBe(403);
    await publish('Later private broadcast');
    expect(events).not.toContain('Later private broadcast');
    expect(revoked).toBe(1);
  } finally {
    await listener.stop();
    await observer.stop();
    await harness.stopAll();
  }
});
