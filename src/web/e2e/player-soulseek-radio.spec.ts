// <copyright file="player-soulseek-radio.spec.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as fs from 'node:fs/promises';
import * as os from 'node:os';
import * as net from 'node:net';
import * as path from 'node:path';
import { randomBytes } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { expect, test, type APIRequestContext } from '@playwright/test';
import { MultiPeerHarness } from './harness/MultiPeerHarness';
import { makeTone } from './fixtures/player-tone';
import { login } from './helpers';

type TestNode = ReturnType<MultiPeerHarness['getNode']>;
type ShareFile = { filename: string; size: number };
type ShareDirectory = { name: string; files: ShareFile[] };
type TrafficTotals = { soulseekUpload: number; soulseekDownload: number };

const transferFixtureBytes = 16 * 1024 * 1024;
const soulseekSpeedLimitKiB = 128;

async function findFreePort(): Promise<number> {
  return new Promise((resolve, reject) => {
    const server = net.createServer();
    server.once('error', reject);
    server.listen(0, '127.0.0.1', () => {
      const address = server.address();
      if (!address || typeof address === 'string') {
        server.close(() => reject(new Error('Could not allocate a Soulseek test port.')));
        return;
      }

      server.close((error) => error ? reject(error) : resolve(address.port));
    });
  });
}

async function authenticate(request: APIRequestContext, node: TestNode): Promise<Record<string, string>> {
  const response = await request.post(`${node.apiUrl}/api/v0/session`, {
    data: { username: node.nodeCfg.username, password: node.nodeCfg.password },
  });
  const body = await response.text();
  expect(response.ok(), body).toBe(true);
  return { Authorization: `Bearer ${(JSON.parse(body) as { token: string }).token}` };
}

async function browseFile(
  request: APIRequestContext,
  node: TestNode,
  headers: Record<string, string>,
  username: string,
  filename: string,
): Promise<{ filename: string; size: number }> {
  const response = await request.get(`${node.apiUrl}/api/v0/users/${encodeURIComponent(username)}/browse`, { headers });
  const body = await response.text();
  expect(response.ok(), body).toBe(true);
  const directories = (JSON.parse(body) as { directories: ShareDirectory[] }).directories;
  const directory = directories.find((entry) => entry.files.some((file) => file.filename === filename));
  const file = directory?.files.find((entry) => entry.filename === filename);
  expect(file, `Soulseek browse did not return ${filename} from ${username}`).toBeDefined();
  return { filename: `${directory!.name}\\${file!.filename}`, size: file!.size };
}

async function transferBytes(
  request: APIRequestContext,
  node: TestNode,
  headers: Record<string, string>,
  direction: 'downloads' | 'uploads',
  username: string,
): Promise<number> {
  const response = await request.get(
    `${node.apiUrl}/api/v0/transfers/${direction}/${encodeURIComponent(username)}`,
    { headers },
  );
  if (response.status() === 404) return 0;
  const body = await response.text();
  expect(response.ok(), body).toBe(true);
  const directories = (JSON.parse(body) as { directories: Array<{ files: Array<{ bytesTransferred: number }> }> }).directories;
  return directories.flatMap((directory) => directory.files)
    .reduce((total, transfer) => total + Number(transfer.bytesTransferred || 0), 0);
}

function trafficTotals(appDir: string): TrafficTotals {
  const output = execFileSync('python3', ['-c',
    'import sqlite3,sys,json; connection=sqlite3.connect("file:"+sys.argv[1]+"?mode=ro",uri=True); row=connection.execute("SELECT soulseek_upload_bytes, soulseek_download_bytes FROM TrafficStats WHERE key=\'global\'").fetchone(); print(json.dumps(row or [0,0])); connection.close()',
    path.join(appDir, 'hashdb.db')], { encoding: 'utf8', timeout: 5000 });
  const [soulseekUpload, soulseekDownload] = JSON.parse(output) as [number, number];
  return { soulseekUpload, soulseekDownload };
}

async function createRadioRoom(request: APIRequestContext, apiUrl: string, headers: Record<string, string>): Promise<string> {
  const response = await request.post(`${apiUrl}/api/v0/pods`, {
    headers,
    data: { requestingPeerId: 'player-soulseek-radio-fixture', pod: {
      name: 'Soulseek transfer overlap',
      visibility: 'Unlisted',
      channels: [{ channelId: 'music', name: 'Music' }],
    } },
  });
  const body = await response.text();
  expect(response.ok(), body).toBe(true);
  const podId = (JSON.parse(body) as { podId: string }).podId;
  expect(podId).toMatch(/^pod:[a-f0-9]{32}$/u);
  return encodeURIComponent(podId);
}

test.describe('player radio during local Soulseek transfers', () => {
  test.skip(process.env.RUN_PLAYER_SOULSEEK_RADIO !== '1', 'Run through test:player:soulseek-radio with the loopback Soulfind fixture.');
  test.use({ serviceWorkers: 'block' });
  test.setTimeout(360_000);

  const harness = new MultiPeerHarness();
  let fixtureDirectory: string;
  let hostShareDirectory: string;
  let listenerShareDirectory: string;
  let host: TestNode;
  let listener: TestNode;
  let hostSoulseekPort: number;
  let listenerSoulseekPort: number;

  test.beforeAll(async () => {
    fixtureDirectory = await fs.mkdtemp(path.join(os.tmpdir(), 'player-soulseek-radio-'));
    hostShareDirectory = path.join(fixtureDirectory, 'host-shares');
    listenerShareDirectory = path.join(fixtureDirectory, 'listener-shares');
    hostSoulseekPort = await findFreePort();
    listenerSoulseekPort = await findFreePort();
    while (listenerSoulseekPort === hostSoulseekPort) listenerSoulseekPort = await findFreePort();
    await Promise.all([
      ...['book', 'music', 'movie', 'tv'].map((directory) =>
        fs.mkdir(path.join(fixtureDirectory, directory), { recursive: true })),
      fs.mkdir(path.join(fixtureDirectory, 'meta'), { recursive: true }),
      fs.mkdir(hostShareDirectory, { recursive: true }),
      fs.mkdir(listenerShareDirectory, { recursive: true }),
    ]);
    await fs.copyFile(
      path.resolve(process.cwd(), '..', '..', 'test-data', 'slskdn-test-fixtures', 'meta', 'manifest.json'),
      path.join(fixtureDirectory, 'meta', 'manifest.json'),
    );
    await Promise.all([
      fs.writeFile(path.join(hostShareDirectory, 'Host transfer.bin'), randomBytes(transferFixtureBytes)),
      fs.writeFile(path.join(listenerShareDirectory, 'Listener transfer.bin'), randomBytes(transferFixtureBytes)),
      fs.writeFile(path.join(hostShareDirectory, 'Radio overlap.wav'), makeTone(360)),
      fs.writeFile(path.join(listenerShareDirectory, 'Reverse radio overlap.wav'), makeTone(360)),
    ]);
    host = await harness.startNode('A', hostShareDirectory, {
      noConnect: false,
      radioMesh: true,
      soulseekEndpointOverrides: { nodeB: listenerSoulseekPort },
      soulseekListenPort: hostSoulseekPort,
    });
    listener = await harness.startNode('B', listenerShareDirectory, {
      noConnect: false,
      radioMesh: true,
      soulseekEndpointOverrides: { nodeA: hostSoulseekPort },
      soulseekListenPort: listenerSoulseekPort,
    });
  });

  test.afterAll(async () => {
    try { await harness.stopAll(); }
    finally { if (fixtureDirectory) await fs.rm(fixtureDirectory, { recursive: true, force: true }); }
  });

  test('keeps radio playback and bidirectional transfers progressing under constrained bandwidth', async ({ page, request }) => {
    const [hostHeaders, listenerHeaders] = await Promise.all([
      authenticate(request, host),
      authenticate(request, listener),
    ]);

    await expect.poll(async () => {
      const [hostStatus, listenerStatus] = await Promise.all([
        request.get(`${host.apiUrl}/api/v0/server`, { headers: hostHeaders }),
        request.get(`${listener.apiUrl}/api/v0/server`, { headers: listenerHeaders }),
      ]);
      if (!hostStatus.ok() || !listenerStatus.ok()) return false;
      const [hostState, listenerState] = await Promise.all([hostStatus.json(), listenerStatus.json()]);
      return hostState.isLoggedIn === true && listenerState.isLoggedIn === true;
    }, { timeout: 90_000 }).toBe(true);

    for (const [node, headers] of [[host, hostHeaders], [listener, listenerHeaders]] as const) {
      const scan = await request.put(`${node.apiUrl}/api/v0/shares`, { headers });
      expect(scan.ok(), await scan.text()).toBe(true);
    }

    const connection = await request.post(`${listener.apiUrl}/api/v0/overlay/connect`, {
      headers: listenerHeaders,
      data: { address: '127.0.0.1', port: host.getOverlayPort() },
    });
    const connectionBody = await connection.text();
    expect(connection.ok(), connectionBody).toBe(true);
    expect(connectionBody).toContain('"connected":true');

    const [listenerTransferFile, hostTransferFile] = await Promise.all([
      browseFile(request, host, hostHeaders, listener.nodeCfg.username, 'Listener transfer.bin'),
      browseFile(request, listener, listenerHeaders, host.nodeCfg.username, 'Host transfer.bin'),
    ]);
    const [hostToListener, listenerToHost] = await Promise.all([
      request.post(`${host.apiUrl}/api/v0/transfers/downloads/${encodeURIComponent(listener.nodeCfg.username)}`, {
        headers: hostHeaders, data: [listenerTransferFile],
      }),
      request.post(`${listener.apiUrl}/api/v0/transfers/downloads/${encodeURIComponent(host.nodeCfg.username)}`, {
        headers: listenerHeaders, data: [hostTransferFile],
      }),
    ]);
    expect(hostToListener.ok(), await hostToListener.text()).toBe(true);
    expect(listenerToHost.ok(), await listenerToHost.text()).toBe(true);

    const transfers = async () => {
      const [hostDownloads, hostUploads, listenerDownloads, listenerUploads] = await Promise.all([
        transferBytes(request, host, hostHeaders, 'downloads', listener.nodeCfg.username),
        transferBytes(request, host, hostHeaders, 'uploads', listener.nodeCfg.username),
        transferBytes(request, listener, listenerHeaders, 'downloads', host.nodeCfg.username),
        transferBytes(request, listener, listenerHeaders, 'uploads', host.nodeCfg.username),
      ]);
      return { hostDownloads, hostUploads, listenerDownloads, listenerUploads };
    };
    await expect.poll(async () => {
      const progress = await transfers();
      return Object.values(progress).every((bytes) => bytes >= 64 * 1024);
    }, { timeout: 45_000 }).toBe(true);

    const libraryResponse = await request.get(
      `${host.apiUrl}/api/v0/library/items/browser?query=Radio%20overlap&kinds=Audio`, { headers: hostHeaders });
    const libraryBody = await libraryResponse.text();
    expect(libraryResponse.ok(), libraryBody).toBe(true);
    const radioItem = (JSON.parse(libraryBody) as { files: Array<{ contentId: string }> }).files[0];
    expect(radioItem?.contentId).toBeTruthy();

    const reverseLibraryResponse = await request.get(
      `${listener.apiUrl}/api/v0/library/items/browser?query=Reverse%20radio%20overlap&kinds=Audio`, { headers: listenerHeaders });
    const reverseLibraryBody = await reverseLibraryResponse.text();
    expect(reverseLibraryResponse.ok(), reverseLibraryBody).toBe(true);
    const reverseRadioItem = (JSON.parse(reverseLibraryBody) as { files: Array<{ contentId: string }> }).files[0];
    expect(reverseRadioItem?.contentId).toBeTruthy();

    const podId = await createRadioRoom(request, host.apiUrl, hostHeaders);
    const partyId = `soulseek-radio-${Date.now()}`;
    const radioTitle = 'Radio overlap';
    const published = await request.post(`${host.apiUrl}/api/v0/listening-party/${podId}/music`, {
      headers: hostHeaders,
      data: {
        partyId, action: 'play', contentId: radioItem!.contentId, title: radioTitle,
        listed: true, allowMeshStreaming: true, positionSeconds: 0,
      },
    });
    expect(published.ok(), await published.text()).toBe(true);
    await expect.poll(async () => {
      const directory = await request.get(`${listener.apiUrl}/api/v0/listening-party?refresh=true`, { headers: listenerHeaders });
      if (!directory.ok()) return null;
      return (await directory.json()).find((party: { partyId: string }) => party.partyId === partyId) ?? null;
    }, { timeout: 30_000 }).toMatchObject({ contentId: radioItem!.contentId, title: radioTitle });

    await page.addInitScript(() => localStorage.setItem('slskdn.player.collapsed', 'false'));
    await login(page, listener.nodeCfg);
    const browserDebug = await page.context().newCDPSession(page);
    await browserDebug.send('Network.enable');
    await browserDebug.send('Network.emulateNetworkConditionsByRule', {
      matchedNetworkConditions: [{
        urlPattern: new URL('/api/v0/mesh-streams/*', listener.nodeCfg.baseUrl).href,
        latency: 40,
        downloadThroughput: 256 * 1024,
        uploadThroughput: 64 * 1024,
      }],
    });
    await page.getByRole('button', { name: 'Show player tools', exact: true }).click();
    await page.getByTestId('player-open-listed-radio').click();
    await expect(page.getByRole('button', { name: `Play ${radioTitle} from listed radio`, exact: true })).toBeVisible();

    const ticketAdmissions: string[] = [];
    page.on('request', (current) => {
      const url = new URL(current.url());
      if (url.pathname.startsWith('/api/v0/listed-radio/') && url.pathname.endsWith('/tickets')) {
        ticketAdmissions.push(current.method());
      }
    });
    const progressBeforeRadio = await transfers();
    const firstAdmission = page.waitForResponse((current) =>
      current.url().includes('/api/v0/listed-radio/') && current.request().method() === 'POST');
    await page.getByRole('button', { name: `Play ${radioTitle} from listed radio`, exact: true }).click();
    expect((await firstAdmission).ok()).toBe(true);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
      (elements as HTMLAudioElement[]).some((audio) => !audio.paused && audio.currentTime > 1))).toBe(true);

    const seek = page.getByLabel('Seek playback', { exact: true });
    const seekBounds = await seek.boundingBox();
    expect(seekBounds).not.toBeNull();
    const seekResponse = page.waitForResponse((current) =>
      current.url().includes('/api/v0/mesh-streams/') && current.status() === 206 && Boolean(current.request().headers().range));
    await seek.click({ position: { x: seekBounds!.width * 0.2, y: seekBounds!.height / 2 } });
    expect((await seekResponse).status()).toBe(206);
    await expect.poll(() => page.locator('audio').evaluateAll((elements) =>
      (elements as HTMLAudioElement[]).some((audio) => !audio.paused && !audio.seeking && audio.currentTime > 40))).toBe(true);

    const progressAfterFirstAdmission = await transfers();
    expect(progressAfterFirstAdmission.hostDownloads).toBeGreaterThan(progressBeforeRadio.hostDownloads);
    expect(progressAfterFirstAdmission.hostUploads).toBeGreaterThan(progressBeforeRadio.hostUploads);
    expect(progressAfterFirstAdmission.listenerDownloads).toBeGreaterThan(progressBeforeRadio.listenerDownloads);
    expect(progressAfterFirstAdmission.listenerUploads).toBeGreaterThan(progressBeforeRadio.listenerUploads);

    const retryAdmission = await request.post(
      `${listener.apiUrl}/api/v0/listed-radio/${encodeURIComponent(partyId)}/tickets`, {
        headers: listenerHeaders,
        data: { contentId: radioItem!.contentId },
      });
    const retryBody = await retryAdmission.text();
    expect(retryAdmission.status(), retryBody).toBe(429);
    expect(JSON.parse(retryBody)).toMatchObject({ code: 'radio_fairness_limited' });
    expect(ticketAdmissions).toEqual(['POST']);

    const reversePodId = await createRadioRoom(request, listener.apiUrl, listenerHeaders);
    const reversePartyId = `soulseek-radio-reverse-${Date.now()}`;
    const reversePublication = await request.post(`${listener.apiUrl}/api/v0/listening-party/${reversePodId}/music`, {
      headers: listenerHeaders,
      data: {
        partyId: reversePartyId, action: 'play', contentId: reverseRadioItem!.contentId,
        title: 'Reverse radio overlap', listed: true, allowMeshStreaming: true, positionSeconds: 0,
      },
    });
    expect(reversePublication.ok(), await reversePublication.text()).toBe(true);
    const hostDirectoryRefresh = await request.get(`${host.apiUrl}/api/v0/listening-party?refresh=true`, { headers: hostHeaders });
    const hostDirectoryBody = await hostDirectoryRefresh.text();
    expect(hostDirectoryRefresh.ok(), hostDirectoryBody).toBe(true);
    expect((JSON.parse(hostDirectoryBody) as Array<{ partyId: string }>).some((party) => party.partyId === reversePartyId)).toBe(true);

    const reverseTicketResponse = await request.post(
      `${host.apiUrl}/api/v0/listed-radio/${encodeURIComponent(reversePartyId)}/tickets`, {
        headers: hostHeaders, data: { contentId: reverseRadioItem!.contentId },
      });
    const reverseTicketBody = await reverseTicketResponse.text();
    expect(reverseTicketResponse.ok(), reverseTicketBody).toBe(true);
    const reverseTicket = JSON.parse(reverseTicketBody) as { streamUrl: string };
    const reverseRadioSize = (await fs.stat(path.join(listenerShareDirectory, 'Reverse radio overlap.wav'))).size;
    const reverseStreamResponse = await request.get(new URL(reverseTicket.streamUrl, host.nodeCfg.baseUrl).href, {
      headers: { Range: `bytes=0-${reverseRadioSize - 1}` },
    });
    const reverseStreamBody = await reverseStreamResponse.body();
    expect(reverseStreamResponse.status()).toBe(206);
    expect(reverseStreamBody.byteLength).toBe(reverseRadioSize);

    const balancedAdmission = await request.post(
      `${listener.apiUrl}/api/v0/listed-radio/${encodeURIComponent(partyId)}/tickets`, {
        headers: listenerHeaders, data: { contentId: radioItem!.contentId },
      });
    const balancedAdmissionBody = await balancedAdmission.text();
    expect(balancedAdmission.status(), balancedAdmissionBody).toBe(200);
    const balancedTicket = JSON.parse(balancedAdmissionBody) as { streamUrl: string };
    const balancedStreamResponse = await request.get(new URL(balancedTicket.streamUrl, listener.nodeCfg.baseUrl).href, {
      headers: { Range: 'bytes=0-65535' },
    });
    expect(balancedStreamResponse.status()).toBe(206);
    expect((await balancedStreamResponse.body()).byteLength).toBe(65_536);
    const transferProgressDuringBalancedAdmission = await transfers();
    expect(Object.values(transferProgressDuringBalancedAdmission).every((bytes) => bytes < transferFixtureBytes)).toBe(true);

    const radioPositionBeforeTransfersFinish = await page.locator('audio').evaluateAll((elements) =>
      Math.max(...(elements as HTMLAudioElement[]).map((audio) => audio.currentTime)));
    await expect.poll(async () => {
      const progress = await transfers();
      return Object.values(progress).every((bytes) => bytes >= transferFixtureBytes);
    }, { timeout: 180_000, intervals: [1_000, 2_000, 5_000] }).toBe(true);
    const radioAtTransferCompletion = await page.locator('audio').evaluateAll((elements) => {
      const active = (elements as HTMLAudioElement[]).find((audio) => audio.currentSrc);
      return active ? { paused: active.paused, ended: active.ended, currentTime: active.currentTime } : null;
    });
    expect(radioAtTransferCompletion?.paused).toBe(false);
    expect(radioAtTransferCompletion?.ended).toBe(false);
    expect(radioAtTransferCompletion!.currentTime).toBeGreaterThan(radioPositionBeforeTransfersFinish + 60);

    await expect.poll(() => {
      const [hostTotals, listenerTotals] = [trafficTotals(host.getAppDir()), trafficTotals(listener.getAppDir())];
      return [
        hostTotals.soulseekDownload > 0,
        hostTotals.soulseekUpload > 0,
        listenerTotals.soulseekDownload > 0,
        listenerTotals.soulseekUpload > 0,
      ];
    }, { timeout: 15_000, intervals: [250, 500, 1_000] }).toEqual([true, true, true, true]);
    const [hostTrafficAfter, listenerTrafficAfter] = [trafficTotals(host.getAppDir()), trafficTotals(listener.getAppDir())];
    expect(hostTrafficAfter.soulseekDownload).toBeGreaterThan(0);
    expect(hostTrafficAfter.soulseekUpload).toBeGreaterThan(0);
    expect(listenerTrafficAfter.soulseekDownload).toBeGreaterThan(0);
    expect(listenerTrafficAfter.soulseekUpload).toBeGreaterThan(0);
    await browserDebug.detach();
    console.log(JSON.stringify({
      soulseekSpeedLimitKiB,
      transfers: await transfers(),
      hostTrafficAfter,
      listenerTrafficAfter,
      ticketAdmissions: ticketAdmissions.length,
    }));
  });
});
