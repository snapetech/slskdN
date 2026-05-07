import { NODES, shouldLaunchNodes } from './env';
import { MultiPeerHarness } from './harness/MultiPeerHarness';
import { login, waitForHealth } from './helpers';
import { expect, test } from '@playwright/test';

test.describe('messaging v2 cutover', () => {
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

  test('messages_route_uses_v2_when_legacy_flag_is_off', async ({ page, request }) => {
    const nodeA = harness ? harness.getNode('A').nodeCfg : NODES.A;
    await waitForHealth(request, nodeA.baseUrl);
    await login(page, nodeA);

    await page.evaluate(() => {
      window.localStorage.setItem('slskd-messaging-v2', 'off');
    });

    await page.goto(`${nodeA.baseUrl}/messages`, {
      timeout: 10_000,
      waitUntil: 'domcontentloaded',
    });

    await expect(page.locator('.msgv2')).toBeVisible({ timeout: 20_000 });
    await expect(page.getByText('Channels', { exact: true })).toBeVisible();
    await expect(page.getByText('Soulseek · DMs', { exact: true })).toBeVisible();
    await expect(page.getByText('Workspace')).toHaveCount(0);
  });
});
