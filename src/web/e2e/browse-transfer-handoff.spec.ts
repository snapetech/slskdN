import { NODES, shouldLaunchNodes } from './env';
import { MultiPeerHarness } from './harness/MultiPeerHarness';
import { clickNav, login, waitForHealth } from './helpers';
import { T } from './selectors';
import { expect, test } from '@playwright/test';

test.use({ serviceWorkers: 'block' });

test.describe('browse transfer handoff', () => {
  let harness: MultiPeerHarness | null = null;

  test.beforeAll(async () => {
    if (shouldLaunchNodes()) {
      harness = new MultiPeerHarness();
      await harness.startNode('A', 'test-data/slskdn-test-fixtures/music', {
        noConnect: process.env.SLSKDN_TEST_NO_CONNECT === 'true',
      });
    }
  });

  test.afterAll(async () => {
    if (harness) {
      await harness.stopAll();
    }
  });

  test('downloads_browse_button_opens_populated_user_browse_tab', async ({ page, request }) => {
    const nodeA = harness ? harness.getNode('A').nodeCfg : NODES.A;
    const peer = 'fixturePeer';
    let queuedDestination: string | null = null;
    let transferRequestCount = 0;

    await waitForHealth(request, nodeA.baseUrl);
    await page.route(
      (url) => url.pathname === '/api/v0/transfers/downloads',
      async (route) => {
        await route.fulfill({
          contentType: 'application/json',
          json: [
            {
              directories: [
                {
                  directory: 'fixture-root',
                  files: [
                    {
                      bytesTransferred: 0,
                      direction: 'Download',
                      filename: 'stalled-track.flac',
                      id: 'download-1',
                      percentComplete: 0,
                      size: 1234,
                      state: 'Completed, Errored',
                      username: peer,
                    },
                  ],
                },
              ],
              username: peer,
            },
          ],
        });
      },
    );
    // The TransferManager seeds its store from the flat /transfers endpoint
    // (not the legacy nested /transfers/downloads). Mock that with a flat array.
    await page.route(
      (url) => url.pathname === '/api/v0/transfers',
      async (route) => {
        transferRequestCount += 1;
        await route.fulfill({
          contentType: 'application/json',
          json: [
            {
              attempts: 1,
              bytesTransferred: 0,
              direction: 'Download',
              filename: 'stalled-track.flac',
              id: 'download-1',
              percentComplete: 0,
              size: 1234,
              state: 'Completed, Errored',
              username: peer,
            },
          ],
        });
      },
    );
    await page.route('**/api/v0/transfers/changes**', async (route) => {
      transferRequestCount += 1;
      await route.fulfill({
        contentType: 'application/json',
        json: {
          counts: { download: 1, upload: 0 },
          cursor: 1,
          transfers: [
            {
              attempts: 1,
              bytesTransferred: 0,
              direction: 'Download',
              filename: 'stalled-track.flac',
              id: 'download-1',
              percentComplete: 0,
              size: 1234,
              state: 'Completed, Errored',
              username: peer,
            },
          ],
        },
      });
    });
    await page.route('**/api/v0/transfers/downloads/accelerated', async (route) => {
      await route.fulfill({ contentType: 'application/json', json: { enabled: false } });
    });
    await page.route('**/api/v0/autoreplace', async (route) => {
      await route.fulfill({ contentType: 'application/json', json: { enabled: false } });
    });
    await page.route(`**/api/v0/users/notes/${peer}`, async (route) => {
      await route.fulfill({ contentType: 'application/json', json: null });
    });
    await page.route(`**/api/v0/users/${peer}/info**`, async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        json: {
          averageSpeed: 0,
          uploadSlots: 0,
        },
      });
    });
    await page.route(`**/api/v0/users/${peer}/status`, async (route) => {
      await route.fulfill({ contentType: 'application/json', json: { isOnline: true } });
    });
    await page.route(`**/api/v0/users/${peer}/browse/status`, async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        json: { fileCount: 1, percentComplete: 100 },
      });
    });
    await page.route(`**/api/v0/users/${peer}/browse`, async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        json: {
          directories: [
            {
              fileCount: 1,
              files: [{ filename: 'proof-track.flac', size: 4321 }],
              name: 'fixture-root',
            },
          ],
          lockedDirectories: [],
        },
      });
    });
    await page.route('**/api/v0/destinations', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        json: [
          {
            exists: true,
            isDefault: false,
            name: 'Downloads',
            path: '/downloads',
          },
          {
            exists: true,
            isDefault: true,
            name: 'Music',
            path: '/downloads/music',
          },
        ],
      });
    });
    await page.route('**/api/v0/transfers/**', async (route) => {
      const url = new URL(route.request().url());

      if (url.pathname.endsWith('/negotiate')) {
        await route.continue();
        return;
      }

      if (
        route.request().method() === 'POST'
        && url.pathname === `/api/v0/transfers/downloads/${peer}`
      ) {
        queuedDestination = url.searchParams.get('destination');
        await route.fulfill({
          contentType: 'application/json',
          json: { enqueued: [{}], failed: [] },
          status: 201,
        });
        return;
      }

      if (url.pathname.includes('/transfers/downloads/accelerated')) {
        await route.fulfill({
          contentType: 'application/json',
          json: { enabled: false },
        });
        return;
      }

      if (url.pathname.includes('/transfers/downloads')) {
        transferRequestCount += 1;
        await route.fulfill({
          contentType: 'application/json',
          json: [
            {
              directories: [
                {
                  directory: 'fixture-root',
                  files: [
                    {
                      bytesTransferred: 0,
                      direction: 'Download',
                      filename: 'stalled-track.flac',
                      id: 'download-1',
                      percentComplete: 0,
                      size: 1234,
                      state: 'Completed, Errored',
                      username: peer,
                    },
                  ],
                },
              ],
              username: peer,
            },
          ],
        });
        return;
      }

      await route.fallback();
    });

    await login(page, nodeA);
    await page.setViewportSize({ height: 800, width: 320 });
    const navigationWidths = await page.evaluate(() => {
      const navigation = document.querySelector('.navigation');
      const primary = navigation?.querySelector('.navigation-primary');
      const utilities = navigation?.querySelector('.right.ui.inverted.menu');
      return {
        navigation: navigation?.clientWidth ?? 0,
        primary: primary?.clientWidth ?? 0,
        utilities: utilities?.clientWidth ?? 0,
        utilitiesContent: utilities?.scrollWidth ?? 0,
      };
    });
    expect(navigationWidths.primary).toBe(navigationWidths.navigation);
    expect(navigationWidths.utilities).toBe(navigationWidths.navigation);
    expect(navigationWidths.utilitiesContent).toBeLessThanOrEqual(
      navigationWidths.utilities,
    );
    const utilityItemBounds = await page.evaluate(() => {
      const utilities = document.querySelector('.navigation .right.ui.inverted.menu');
      return Array.from(utilities?.children ?? []).map((item) => {
        const rect = item.getBoundingClientRect();
        return { left: rect.left, right: rect.right };
      });
    });
    expect(utilityItemBounds.every(({ left, right }) =>
      left >= 0 && right <= 320)).toBe(true);
    expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth))
      .toBe(false);
    for (const testId of ['nav-search', 'nav-downloads', 'nav-uploads', 'nav-messages', 'nav-system']) {
      await expect(page.getByTestId(testId)).toBeInViewport();
    }
    await page.locator('.navigation-primary').evaluate((navigation) => {
      navigation.scrollLeft = navigation.scrollWidth;
    });
    for (const testId of ['nav-group-network', 'nav-group-sharing']) {
      await expect(page.getByTestId(testId)).toBeInViewport();
    }
    for (const [index, group] of [
      {
        links: ['Users', 'Contacts', 'Solid'],
        testId: 'nav-group-network',
      },
      {
        links: ['Collections', 'Share Groups', 'Shared with Me', 'Browse'],
        testId: 'nav-group-sharing',
      },
    ].entries()) {
      await page.getByTestId(group.testId).click();
      const groupPopup = page.locator('.navigation-dropdown-popup');
      await expect(groupPopup).toBeVisible();
      const popupBounds = await groupPopup.boundingBox();
      expect(popupBounds).not.toBeNull();
      expect(popupBounds?.x ?? -1).toBeGreaterThanOrEqual(0);
      expect((popupBounds?.x ?? 0) + (popupBounds?.width ?? 0)).toBeLessThanOrEqual(320);
      for (const linkName of group.links) {
        await expect(
          groupPopup.getByRole('link', { exact: true, name: linkName }),
        ).toBeVisible();
      }
      if (index === 0) {
        await page.locator('.navigation-primary').evaluate((navigation) => {
          navigation.scrollLeft = 0;
        });
        await page.getByTestId('nav-search').click();
        await expect(page).toHaveURL(/\/searches$/);
        await page.locator('.navigation-primary').evaluate((navigation) => {
          navigation.scrollLeft = navigation.scrollWidth;
        });
      }
    }
    await page.getByRole('link', { exact: true, name: 'System' }).focus();
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/system$/);

    await page.evaluate(() => {
      window.localStorage.removeItem('slskd-browse-tabs');
    });

    await page.goto(`${nodeA.baseUrl}/downloads`, {
      timeout: 10_000,
      waitUntil: 'domcontentloaded',
    });
    await clickNav(page, T.navDownloads);

    const browseButton = page.getByRole('button', {
      name: `Browse ${peer} files`,
    });
    await expect(
      browseButton,
      `expected mocked Downloads transfer row; transfer requests mocked: ${transferRequestCount}`,
    ).toBeVisible({ timeout: 15_000 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth))
      .toBe(false);
    await browseButton.click();

    await expect(page).toHaveURL(/\/browse\?user=fixturePeer/);
    await expect(page.getByText(peer, { exact: true })).toBeVisible({
      timeout: 15_000,
    });
    await expect(page.getByTestId(T.browseContent)).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth))
      .toBe(false);
    await expect(page.getByText('1 directories, 1 files')).toBeVisible({
      timeout: 15_000,
    });
    await expect(page.getByText('fixture-root')).toBeVisible();
    await expect(page.getByText('No user share to display')).toHaveCount(0);

    const destination = page.getByRole('listbox', {
      name: 'Download destination',
    });
    await expect(destination).toContainText('Music (default)');
    await destination.click();
    await page.getByRole('option', { name: /Downloads/ }).click();
    await expect(destination).toContainText('Downloads');
    await expect.poll(() => page.evaluate(() =>
      window.localStorage.getItem('slskd-download-destination')))
      .toBe('/downloads');

    await page.getByRole('button', { name: 'fixture-root' }).click();
    const selectedDirectory = page.locator('.browse-selected-directory-card');
    await expect(selectedDirectory.getByText('proof-track.flac')).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth))
      .toBe(false);
    const fileSelection = selectedDirectory.getByRole('checkbox', {
      name: 'proof-track.flac',
    });
    await fileSelection.focus();
    await page.keyboard.press('Space');
    await expect(fileSelection).toBeChecked();
    await selectedDirectory.getByRole('button', { name: /Download/ }).click();
    await expect.poll(() => queuedDestination).toBe('/downloads');
  });
});
