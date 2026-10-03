// <copyright file="core-soulseek-journeys.spec.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as fs from 'node:fs/promises';
import * as net from 'node:net';
import * as path from 'node:path';
import { MultiPeerHarness } from './harness/MultiPeerHarness';
import { SoulseekPrivateMessageIdProxy } from './harness/SoulseekPrivateMessageIdProxy';
import { login, waitForHealth } from './helpers';
import {
  expect,
  test,
  type APIRequestContext,
  type Page,
} from '@playwright/test';

type TestNode = ReturnType<MultiPeerHarness['getNode']>;

const coverFixturePath = path.resolve(
  process.cwd(),
  '..',
  '..',
  'test-data',
  'slskdn-test-fixtures',
  'music',
  'open_goldberg',
  'cover.jpg',
);
const musicFixtureDirectory = path.dirname(path.dirname(coverFixturePath));

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

async function authenticate(
  request: APIRequestContext,
  node: TestNode,
): Promise<string> {
  const response = await request.post(`${node.apiUrl}/api/v0/session`, {
    data: {
      password: node.nodeCfg.password,
      username: node.nodeCfg.username,
    },
  });
  const body = await response.text();
  expect(response.ok(), body).toBe(true);
  return (JSON.parse(body) as { token: string }).token;
}

async function waitForSoulseekLogin(
  request: APIRequestContext,
  node: TestNode,
  token: string,
): Promise<void> {
  await expect.poll(async () => {
    const response = await request.get(`${node.apiUrl}/api/v0/server`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!response.ok()) return false;
    const state = (await response.json()) as { isLoggedIn?: boolean };
    return state.isLoggedIn === true;
  }, { intervals: [250, 500, 1_000], timeout: 90_000 }).toBe(true);
}

function getLoopbackSoulseekEndpoint(): { address: '127.0.0.1'; port: number } {
  const address = process.env.SLSKDN_E2E_SOULSEEK_ADDRESS;
  const port = Number(process.env.SLSKDN_E2E_SOULSEEK_PORT);
  if (
    address !== '127.0.0.1' ||
    !Number.isInteger(port) ||
    port < 1024 ||
    port > 65535
  ) {
    throw new Error(
      'Refusing to start core journey nodes without a valid loopback Soulseek endpoint.',
    );
  }

  return { address, port };
}

async function openDirectMessage(page: Page, username: string): Promise<void> {
  const section = page
    .locator('.msgv2-tree-section')
    .filter({ hasText: 'Soulseek · DMs' });
  const sectionToggle = section.locator('.msgv2-tree-section-head');
  if (await sectionToggle.getAttribute('aria-expanded') === 'false') {
    await sectionToggle.click();
  }

  await section.getByRole('button', { name: 'Start a direct message' }).click();
  await page.getByPlaceholder('username').fill(username);
  await section.getByRole('button', { name: 'DM', exact: true }).click();
  await expect(page.getByRole('textbox', { name: `Message @${username}` }))
    .toBeVisible();
}

test.skip(
  process.env.RUN_CORE_SOULSEEK_JOURNEYS !== '1',
  'Run with scripts/test-core-soulseek-journeys.sh and the loopback Soulfind fixture.',
);

test.describe.configure({ mode: 'serial' });
test.setTimeout(180_000);
test.use({ serviceWorkers: 'block' });

test.describe('core Soulseek user journeys against loopback Soulfind', () => {
  const harness = new MultiPeerHarness();
  let sharedFilePath = '';
  let sharedFilename = '';
  let host: TestNode;
  let client: TestNode;
  let soulseekProxy: SoulseekPrivateMessageIdProxy | null = null;

  test.beforeAll(async () => {
    const soulseekEndpoint = getLoopbackSoulseekEndpoint();
    soulseekProxy = new SoulseekPrivateMessageIdProxy(soulseekEndpoint.port);
    const proxyPort = await soulseekProxy.start();
    sharedFilename = ['slskdn-e2e-journey', process.pid, Date.now()].join('-') + '.jpg';
    sharedFilePath = path.join(musicFixtureDirectory, sharedFilename);
    await fs.copyFile(coverFixturePath, sharedFilePath);

    const hostPort = await findFreePort();
    let clientPort = await findFreePort();
    while (clientPort === hostPort) clientPort = await findFreePort();

    host = await harness.startNode(
      'A',
      'test-data/slskdn-test-fixtures/music',
      {
        noConnect: false,
        radioMesh: true,
        scenePodBridge: false,
        soulseekServerAddress: soulseekEndpoint.address,
        soulseekServerPort: proxyPort,
        soulseekEndpointOverrides: { nodeB: clientPort },
        soulseekListenPort: hostPort,
      },
    );
    client = await harness.startNode('B', [], {
      noConnect: false,
      radioMesh: true,
      scenePodBridge: false,
      soulseekServerAddress: soulseekEndpoint.address,
      soulseekServerPort: proxyPort,
      soulseekEndpointOverrides: { nodeA: hostPort },
      soulseekListenPort: clientPort,
    });
  });

  test.afterAll(async () => {
    try {
      await harness.stopAll();
    } finally {
      try {
        await soulseekProxy?.close();
      } finally {
        if (sharedFilePath) {
          await fs.rm(sharedFilePath, { force: true });
        }
      }
    }
  });

  test('searches a local peer, downloads a selected result, and verifies the file', async ({
    browser,
    request,
  }) => {
    await waitForHealth(request, host.nodeCfg.baseUrl);
    await waitForHealth(request, client.nodeCfg.baseUrl);
    const [hostToken, clientToken] = await Promise.all([
      authenticate(request, host),
      authenticate(request, client),
    ]);
    await Promise.all([
      waitForSoulseekLogin(request, host, hostToken),
      waitForSoulseekLogin(request, client, clientToken),
    ]);

    const scan = await request.put(`${host.apiUrl}/api/v0/shares`, {
      headers: { Authorization: `Bearer ${hostToken}` },
    });
    expect(scan.ok(), await scan.text()).toBe(true);

    const meshConnection = await request.post(
      `${client.apiUrl}/api/v0/overlay/connect`,
      {
        headers: { Authorization: `Bearer ${clientToken}` },
        data: { address: '127.0.0.1', port: host.getOverlayPort() },
      },
    );
    const meshConnectionBody = await meshConnection.text();
    expect(meshConnection.ok(), meshConnectionBody).toBe(true);
    expect(meshConnectionBody).toContain('"connected":true');

    const hostBrowse = await request.get(
      `${client.apiUrl}/api/v0/users/${encodeURIComponent(host.nodeCfg.username)}/browse`,
      { headers: { Authorization: `Bearer ${clientToken}` } },
    );
    const hostBrowseBody = await hostBrowse.text();
    expect(hostBrowse.ok(), hostBrowseBody).toBe(true);
    expect(hostBrowseBody).toContain(sharedFilename);

    const context = await browser.newContext({
      viewport: { height: 800, width: 320 },
    });
    const page = await context.newPage();
    try {
      await login(page, client.nodeCfg);
      await page.goto(`${client.nodeCfg.baseUrl}/searches`, {
        waitUntil: 'domcontentloaded',
      });
      expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth))
        .toBe(false);

      const searchInput = page.getByTestId('search-input');
      await expect(searchInput).toBeEnabled({ timeout: 30_000 });
      await searchInput.fill(sharedFilename);
      const searchCreated = page.waitForResponse((response) =>
        new URL(response.url()).pathname === '/api/v0/searches' &&
        response.request().method() === 'POST',
      );
      await searchInput.press('Enter');
      const searchResponse = await searchCreated;
      expect(searchResponse.ok(), await searchResponse.text()).toBe(true);
      await expect(page).toHaveURL(/\/searches\/[^/]+$/);

      // The default filter deliberately hides small files and sparse folders.
      // Clear it so this small local fixture can exercise the regular result UI.
      await page.getByRole('textbox').fill('');

      const peerResult = page
        .locator('.result-card')
        .filter({ hasText: host.nodeCfg.username });
      await expect(peerResult).toBeVisible({ timeout: 60_000 });
      expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth))
        .toBe(false);
      const fileRow = peerResult
        .locator('.filelist-table tbody tr')
        .filter({ hasText: sharedFilename });
      await expect(fileRow).toBeVisible();
      const fileSelection = fileRow.getByRole('checkbox', {
        name: sharedFilename,
      });
      await fileSelection.focus();
      await page.keyboard.press('Space');
      await expect(fileSelection).toBeChecked();
      await peerResult.getByRole('button', { name: 'Download', exact: true }).click();

      const source = await fs.readFile(
        coverFixturePath,
      );
      let localFilename = '';
      await expect.poll(async () => {
        const response = await request.get(
          `${client.apiUrl}/api/v0/transfers/downloads`,
          { headers: { Authorization: `Bearer ${clientToken}` } },
        );
        if (!response.ok()) return false;
        const transfers = (await response.json()) as Array<{
          directories?: Array<{
            files?: Array<{
              bytesTransferred?: number;
              filename?: string;
              localFilename?: string;
              state?: string;
            }>;
          }>;
          username?: string;
        }>;
        return transfers.some((user) =>
          user.username === host.nodeCfg.username &&
          user.directories?.some((directory) =>
            directory.files?.some((file) => {
              if (
                !file.filename?.includes(sharedFilename) ||
                file.state !== 'Completed, Succeeded' ||
                Number(file.bytesTransferred) < source.length ||
                !file.localFilename
              ) {
                return false;
              }

              localFilename = file.localFilename;
              return true;
            }),
          ),
        );
      }, { intervals: [250, 500, 1_000], timeout: 90_000 }).toBe(true);

      const downloaded = await fs.readFile(localFilename);
      expect(downloaded.equals(source)).toBe(true);
    } finally {
      await context.close();
    }
  });

  test('sends and receives a Soulseek private message in the Messages UI', async ({
    browser,
    request,
  }) => {
    await waitForHealth(request, host.nodeCfg.baseUrl);
    await waitForHealth(request, client.nodeCfg.baseUrl);
    const [hostToken, clientToken] = await Promise.all([
      authenticate(request, host),
      authenticate(request, client),
    ]);
    await Promise.all([
      waitForSoulseekLogin(request, host, hostToken),
      waitForSoulseekLogin(request, client, clientToken),
    ]);

    const hostContext = await browser.newContext({
      viewport: { height: 800, width: 320 },
    });
    const clientContext = await browser.newContext({
      viewport: { height: 800, width: 320 },
    });
    const hostPage = await hostContext.newPage();
    const clientPage = await clientContext.newPage();
    try {
      await Promise.all([
        login(hostPage, host.nodeCfg),
        login(clientPage, client.nodeCfg),
      ]);
      await Promise.all([
        hostPage.goto(`${host.nodeCfg.baseUrl}/messages`, {
          waitUntil: 'domcontentloaded',
        }),
        clientPage.goto(`${client.nodeCfg.baseUrl}/messages`, {
          waitUntil: 'domcontentloaded',
        }),
      ]);

      await Promise.all([
        openDirectMessage(hostPage, client.nodeCfg.username),
        openDirectMessage(clientPage, host.nodeCfg.username),
      ]);
      expect(await hostPage.evaluate(
        () => document.documentElement.scrollWidth > window.innerWidth,
      )).toBe(false);
      expect(await clientPage.evaluate(
        () => document.documentElement.scrollWidth > window.innerWidth,
      )).toBe(false);

      const message = `local journey check ${Date.now()}`;
      await hostPage.getByRole('textbox', {
        name: `Message @${client.nodeCfg.username}`,
      }).fill(message);
      const sent = hostPage.waitForResponse((response) =>
        new URL(response.url()).pathname ===
          `/api/v0/conversations/${client.nodeCfg.username}` &&
        response.request().method() === 'POST',
      );
      await hostPage.getByRole('button', { name: 'Send' }).click();
      const sendResponse = await sent;
      expect(sendResponse.status(), await sendResponse.text()).toBe(201);

      await expect(hostPage.getByText(message, { exact: true }))
        .toBeVisible({ timeout: 15_000 });
      await expect(clientPage.getByText(message, { exact: true }))
        .toBeVisible({ timeout: 15_000 });
    } finally {
      await Promise.all([hostContext.close(), clientContext.close()]);
    }
  });
});
