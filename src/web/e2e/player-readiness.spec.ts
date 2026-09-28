// <copyright file="player-readiness.spec.ts" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>
import { expect, test } from '@playwright/test';
import { MultiPeerHarness } from './harness/MultiPeerHarness';
import { login } from './helpers';

test.use({ serviceWorkers: 'block' });
test('authenticates the player while unrelated network work remains pending', async ({ page }) => {
  const harness = new MultiPeerHarness();
  let releaseRequest!: () => void;
  const pending = new Promise<void>((resolve) => { releaseRequest = resolve; });
  let requestStarted = false;
  await page.route('**/controlled-pending-readiness', async (route) => {
    requestStarted = true;
    await pending;
    await route.fulfill({ status: 204 });
  });
  await page.addInitScript(() => {
    void fetch('/controlled-pending-readiness');
  });
  try {
    const node = await harness.startNode('A', [], { noConnect: true });
    await login(page, node.nodeCfg);
    expect(requestStarted).toBe(true);
    await expect(page.getByTestId('nav-system')).toBeVisible();
  } finally {
    releaseRequest();
    await page.unrouteAll({ behavior: 'wait' });
    await page.close();
    await harness.stopAll();
  }
});
