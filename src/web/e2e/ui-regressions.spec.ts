import { NODES, shouldLaunchNodes } from './env';
import { clickNav, getAuthToken, login, waitForHealth } from './helpers';
import { MultiPeerHarness } from './harness/MultiPeerHarness';
import { expect, test } from '@playwright/test';

test.use({ serviceWorkers: 'block' });

test.describe('UI regression coverage', () => {
  let harness: MultiPeerHarness | null = null;

  test.beforeAll(async () => {
    if (shouldLaunchNodes()) {
      harness = new MultiPeerHarness();
      await harness.startNode('A', 'test-data/slskdn-test-fixtures/music', {
        noConnect: true,
      });
    }
  });

  test.afterAll(async () => {
    if (harness) {
      await harness.stopAll();
    }
  });

  const getNode = () => (harness ? harness.getNode('A').nodeCfg : NODES.A);

  test('keeps grouped navigation and System section tabs visible on dark theme', async ({
    page,
    request,
  }) => {
    const node = getNode();
    await waitForHealth(request, node.baseUrl);
    await login(page, node);

    await page.goto(`${node.baseUrl}/searches`, {
      waitUntil: 'domcontentloaded',
    });

    const navigation = page.locator('.navigation');
    await expect(navigation).toBeVisible();
    await expect(navigation).toHaveCSS('overflow', 'visible');

    const discoverMenu = page.getByTestId('nav-group-discover');
    await discoverMenu.click();
    await expect(page.locator('.navigation-dropdown-popup')).toBeVisible();
    await expect(page.getByTestId('nav-wishlist')).toBeVisible();

    await clickNav(page, 'nav-wishlist');
    await expect(page).toHaveURL(/\/wishlist$/);

    await page.goto(`${node.baseUrl}/system/info`, {
      waitUntil: 'domcontentloaded',
    });
    const sectionMenu = page.locator('.system-section-menu');
    await expect(sectionMenu).toBeVisible();
    const sectionColors = await sectionMenu.locator(':scope > .item').evaluateAll(
      (items) => items.map((item) => getComputedStyle(item).color),
    );
    expect(sectionColors.length).toBe(6);
    expect(sectionColors.every((color) => !/^rgb\(0, 0, 0\)$/u.test(color))).toBe(true);
  });

  test('persists transfer view preferences and keeps row cells aligned with headers', async ({
    page,
    request,
  }) => {
    const node = getNode();
    await waitForHealth(request, node.baseUrl);
    await login(page, node);

    await page.evaluate(() => {
      const columns = {
        order: ['peer', 'name', 'size'],
        visible: {
          actions: false,
          album: false,
          artist: false,
          bitrate: false,
          completed: false,
          directory: false,
          elapsed: false,
          eta: false,
          extension: false,
          length: false,
          local: false,
          name: true,
          peer: true,
          progress: false,
          remaining: false,
          samplerate: false,
          size: true,
          speed: false,
          started: false,
          state: false,
          title: false,
          track: false,
          year: false,
        },
        widths: { name: 200, peer: 90, size: 120 },
      };
      localStorage.setItem('slskdn-transfer-columns-download', JSON.stringify(columns));
    });

    await page.route(/\/api\/v0\/transfers\/changes(?:\?|$)/u, async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        json: {
          counts: { download: 1, upload: 0 },
          cursor: 1,
          transfers: [{
            bytesTransferred: 5,
            direction: 'Download',
            filename: 'Artist/Track.flac',
            id: 'ui-regression-transfer',
            percentComplete: 50,
            size: 10,
            state: 'InProgress',
            username: 'peer-one',
          }],
        },
      });
    });
    await page.route('**/api/v0/transfers/downloads/accelerated', async (route) => {
      await route.fulfill({ contentType: 'application/json', json: { enabled: false } });
    });
    await page.route('**/api/v0/autoreplace', async (route) => {
      await route.fulfill({ contentType: 'application/json', json: { enabled: false } });
    });

    await page.goto(`${node.baseUrl}/downloads`, {
      waitUntil: 'domcontentloaded',
    });

    const headers = page.locator('.transfer-header-cell');
    await expect(headers).toHaveCount(3);
    expect(await headers.evaluateAll((cells) => cells.map((cell) => cell.dataset.colkey)))
      .toEqual(['peer', 'name', 'size']);

    const rowCells = page.locator('.transfer-row:not(.transfer-header-row) [data-colkey]');
    await expect(rowCells).toHaveCount(3);
    expect(await rowCells.evaluateAll((cells) => cells.map((cell) => cell.dataset.colkey)))
      .toEqual(['peer', 'name', 'size']);

    const hideCompleted = page.locator('.hide-completed-toggle input');
    await expect(hideCompleted).toBeChecked();
    await page.locator('.hide-completed-toggle label').click();
    await page.locator('.transfer-header-cell[data-colkey="size"]').click();

    await page.reload({ waitUntil: 'domcontentloaded' });
    await expect(page.locator('.transfer-header-cell[data-colkey="size"]')).toHaveAttribute(
      'aria-sort',
      'ascending',
    );
    await expect(page.locator('.hide-completed-toggle input')).not.toBeChecked();
  });

  test('persists and retrieves blocked users through the authenticated API', async ({
    page,
    request,
  }) => {
    const node = getNode();
    await waitForHealth(request, node.baseUrl);
    await login(page, node);

    const token = await getAuthToken(page);
    const headers = { Authorization: `Bearer ${token}` };
    const username = `playwright-regression-${Date.now()}`;
    const encodedUsername = encodeURIComponent(username);

    try {
      const initial = await request.get(`${node.baseUrl}/api/v0/users/blocks`, {
        failOnStatusCode: false,
        headers,
      });
      expect(initial.status()).toBe(200);

      const blocked = await request.put(
        `${node.baseUrl}/api/v0/users/blocks/${encodedUsername}`,
        { failOnStatusCode: false, headers },
      );
      expect(blocked.status()).toBe(200);
      expect((await blocked.json()).username).toBe(username);

      const persisted = await request.get(`${node.baseUrl}/api/v0/users/blocks`, {
        failOnStatusCode: false,
        headers,
      });
      expect(persisted.status()).toBe(200);
      expect((await persisted.json()).some((entry: { username: string }) => entry.username === username)).toBe(true);
    } finally {
      const removed = await request.delete(
        `${node.baseUrl}/api/v0/users/blocks/${encodedUsername}`,
        { failOnStatusCode: false, headers },
      );
      expect([204, 404]).toContain(removed.status());
    }
  });
});
