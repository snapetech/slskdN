#!/usr/bin/env node

import fs from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';
import { chromium } from '../src/web/node_modules/playwright/index.mjs';

const baseUrl = process.env.SLSKDN_AUDIT_URL;
const username = process.env.SLSKDN_AUDIT_USERNAME;
const password = process.env.SLSKDN_AUDIT_PASSWORD;
const outputDirectory = process.env.SLSKDN_AUDIT_OUTPUT ?? '/tmp/slskdn-live-ui-audit';

if (!baseUrl || !username || !password) {
  throw new Error(
    'Set SLSKDN_AUDIT_URL, SLSKDN_AUDIT_USERNAME, and SLSKDN_AUDIT_PASSWORD.',
  );
}

const applicationRoutes = [
  '/',
  '/searches',
  '/collections',
  '/solid',
  '/discovery-graph',
  '/playlist-intake',
  '/wishlist',
  '/lidarr',
  '/browse',
  '/users',
  '/contacts',
  '/sharegroups',
  '/shared',
  '/chat',
  '/pods',
  '/rooms',
  '/messages',
  '/uploads',
  '/downloads',
];

const systemTabs = [
  'info',
  'network',
  'mesh',
  'bridge',
  'mediacore',
  'security',
  'policies',
  'experience',
  'integrations',
  'options',
  'shares',
  'jobs',
  'automations',
  'source-providers',
  'swarm-analytics',
  'library-health',
  'quarantine-jury',
  'files',
  'data',
  'events',
  'logs',
  'metrics',
];

const requestedRoutes = [
  ...applicationRoutes,
  ...systemTabs.map((tab) => `/system/${tab}`),
];

const now = () => new Date().toISOString();
const slug = (value) =>
  value
    .replace(/^\//u, '')
    .replaceAll('/', '-')
    .replaceAll(/[^a-zA-Z0-9_-]/gu, '_') || 'root';

const unique = (items) => [...new Set(items)];
const routeShape = (pathname) =>
  pathname
    .replace(
      /\/[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}(?=\/|$)/giu,
      '/:id',
    )
    .replace(/\/\d+(?=\/|$)/gu, '/:id');

await fs.mkdir(outputDirectory, { recursive: true });

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({
  ignoreHTTPSErrors: true,
  viewport: { height: 1000, width: 1440 },
});
const page = await context.newPage();

const observations = [];
let activeRoute = 'login';

const observe = (kind, detail, url = page.url()) => {
  observations.push({ detail, kind, route: activeRoute, timestamp: now(), url });
};

page.on('console', (message) => {
  if (message.type() === 'error') {
    observe('console-error', message.text());
  }
});
page.on('pageerror', (error) => observe('page-error', error.message));
page.on('requestfailed', (request) => {
  const failure = request.failure()?.errorText ?? 'unknown failure';
  if (!failure.includes('ERR_ABORTED')) {
    observe('request-failed', `${request.method()} ${request.url()} ${failure}`);
  }
});
page.on('response', (response) => {
  if (response.status() >= 400) {
    observe(
      'bad-response',
      `${response.status()} ${response.request().method()} ${response.url()}`,
      response.url(),
    );
  }
});

await page.goto(baseUrl, { timeout: 30_000, waitUntil: 'networkidle' });
await page.getByTestId('login-username').locator('input').fill(username);
await page.getByTestId('login-password').locator('input').fill(password);
const loginResponse = page.waitForResponse(
  (response) =>
    response.url().includes('/api/v0/session') &&
    response.request().method() === 'POST',
  { timeout: 15_000 },
);
await page.getByTestId('login-submit').click();
const loginStatus = (await loginResponse).status();
if (loginStatus !== 200) {
  throw new Error(`Login failed with HTTP ${loginStatus}.`);
}
await page.waitForURL(/\/searches(?:$|[?#])/u, { timeout: 15_000 });

const routeQueue = [...requestedRoutes];
const queuedRouteShapes = new Set(
  routeQueue.map((route) => routeShape(new URL(route, baseUrl).pathname)),
);
const externalLinks = new Set();
const results = [];

for (let index = 0; index < routeQueue.length; index += 1) {
  const route = routeQueue[index];
  activeRoute = route;
  const observationStart = observations.length;
  const startedAt = now();
  const startedMs = Date.now();
  let navigationError;

  try {
    await page.goto(new URL(route, baseUrl).href, {
      timeout: 30_000,
      waitUntil: 'domcontentloaded',
    });
    await page.waitForLoadState('networkidle', { timeout: 15_000 }).catch(() => {});
    await page.waitForTimeout(750);
  } catch (error) {
    navigationError = error instanceof Error ? error.message : String(error);
    observe('navigation-error', navigationError);
  }

  const loginVisible = await page.getByTestId('login-username').isVisible().catch(() => false);
  const bodyText = await page.locator('body').innerText().catch(() => '');
  const pageLinks = await page.locator('a[href]').evaluateAll((anchors) =>
    anchors.map((anchor) => ({
      href: anchor.href,
      text: (anchor.textContent ?? '').replaceAll(/\s+/gu, ' ').trim(),
    })),
  );

  for (const link of pageLinks) {
    const parsed = new URL(link.href);
    if (parsed.origin === new URL(baseUrl).origin) {
      const discoveredRoute = parsed.pathname;
      const discoveredShape = routeShape(discoveredRoute);
      if (
        !queuedRouteShapes.has(discoveredShape) &&
        !discoveredRoute.startsWith('/api/')
      ) {
        queuedRouteShapes.add(discoveredShape);
        routeQueue.push(discoveredRoute);
      }
    } else if (['http:', 'https:'].includes(parsed.protocol)) {
      externalLinks.add(link.href);
    }
  }

  const controls = await page.locator('button, input, select, textarea, [role="button"]').evaluateAll(
    (elements) =>
      elements
        .filter((element) => {
          const style = window.getComputedStyle(element);
          const bounds = element.getBoundingClientRect();
          return style.visibility !== 'hidden' && style.display !== 'none' && bounds.width > 0 && bounds.height > 0;
        })
        .map((element) => ({
          ariaLabel: element.getAttribute('aria-label') ?? '',
          disabled: element.matches(':disabled, [aria-disabled="true"]'),
          name: element.getAttribute('name') ?? '',
          tag: element.tagName.toLowerCase(),
          testId: element.getAttribute('data-testid') ?? '',
          text: (element.textContent ?? '').replaceAll(/\s+/gu, ' ').trim(),
          title: element.getAttribute('title') ?? '',
          type: element.getAttribute('type') ?? '',
        })),
  );

  const unnamedButtons = controls.filter(
    (control) =>
      control.tag === 'button' &&
      !control.text &&
      !control.ariaLabel &&
      !control.title &&
      !control.testId,
  );
  const missingImageAltCount = await page.locator('img:not([alt])').count();
  const systemTabVisibility = route.startsWith('/system')
    ? await page
        .locator('.system .ui.tabular.menu')
        .evaluate((menu) => {
          const activeItem = menu.querySelector('.active.item');
          if (!activeItem) return { activeVisible: false };
          const activeBounds = activeItem.getBoundingClientRect();
          const menuBounds = menu.getBoundingClientRect();
          return {
            activeText: (activeItem.textContent ?? '').replaceAll(/\s+/gu, ' ').trim(),
            activeVisible:
              activeBounds.left >= menuBounds.left &&
              activeBounds.right <= menuBounds.right,
            clientWidth: menu.clientWidth,
            scrollLeft: menu.scrollLeft,
            scrollWidth: menu.scrollWidth,
          };
        })
        .catch(() => null)
    : null;
  const visibleErrorText = unique(
    await page
      .locator('.ui.error.message:visible, .error-segment:visible')
      .allInnerTexts()
      .catch(() => []),
  ).filter(Boolean);

  const screenshot = path.join(outputDirectory, `${String(index + 1).padStart(2, '0')}-${slug(route)}.png`);
  await page.screenshot({ fullPage: true, path: screenshot });

  results.push({
    bodyPreview: bodyText.replaceAll(/\s+/gu, ' ').slice(0, 300),
    controlCount: controls.length,
    controls,
    durationMs: Date.now() - startedMs,
    finalUrl: page.url(),
    linkCount: pageLinks.length,
    links: pageLinks,
    loginVisible,
    missingImageAltCount,
    navigationError,
    observations: observations.slice(observationStart),
    route,
    screenshot,
    startedAt,
    systemTabVisibility,
    title: await page.title(),
    unnamedButtons,
    visibleErrorText,
  });
}

activeRoute = 'interaction-checks';
const interactionChecks = [];
await page.goto(new URL('/searches', baseUrl).href, { waitUntil: 'networkidle' });
const themeMenu = page.getByTestId('theme-menu');
if (await themeMenu.isVisible().catch(() => false)) {
  try {
    await themeMenu.click({ timeout: 3_000 });
    const options = page.locator('[data-testid^="theme-option-"]');
    const optionCount = await options.count();
    interactionChecks.push({
      check: 'theme menu opens',
      optionCount,
      passed: optionCount >= 3,
    });
    const darkOption = page.getByTestId('theme-option-classic-dark');
    if (await darkOption.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await darkOption.click({ timeout: 3_000 });
      interactionChecks.push({
        check: 'dark theme applies',
        passed: await page.locator('html.dark').count().then((count) => count === 1),
      });
      await themeMenu.click({ timeout: 3_000 });
      const defaultOption = page.getByTestId('theme-option-slskdn');
      if (await defaultOption.isVisible({ timeout: 3_000 }).catch(() => false)) {
        await defaultOption.click({ timeout: 3_000 });
      }
    } else {
      interactionChecks.push({ check: 'dark theme applies', passed: false });
    }
  } catch (error) {
    interactionChecks.push({
      check: 'theme interaction completes',
      error: error instanceof Error ? error.message : String(error),
      passed: false,
    });
  }
}

const externalLinkResults = [];
for (const href of externalLinks) {
  try {
    const response = await context.request.head(href, {
      failOnStatusCode: false,
      timeout: 15_000,
    });
    externalLinkResults.push({ href, status: response.status() });
  } catch (error) {
    externalLinkResults.push({
      error: error instanceof Error ? error.message : String(error),
      href,
    });
  }
}

const report = {
  baseOrigin: new URL(baseUrl).origin,
  completedAt: now(),
  externalLinkResults,
  interactionChecks,
  observations,
  requestedRouteCount: requestedRoutes.length,
  results,
  routeCount: results.length,
};

await fs.writeFile(
  path.join(outputDirectory, 'report.json'),
  `${JSON.stringify(report, null, 2)}\n`,
);

const failingRoutes = results.filter(
  (result) =>
    result.loginVisible ||
    result.missingImageAltCount > 0 ||
    result.navigationError ||
    result.observations.length > 0 ||
    result.systemTabVisibility?.activeVisible === false ||
    result.unnamedButtons.length > 0 ||
    result.visibleErrorText.length > 0,
);
const routeIssueCount = (result) =>
  result.observations.length +
  result.visibleErrorText.length +
  result.unnamedButtons.length +
  result.missingImageAltCount +
  (result.loginVisible ? 1 : 0) +
  (result.navigationError ? 1 : 0) +
  (result.systemTabVisibility?.activeVisible === false ? 1 : 0);
const summary = [
  '# Live UI audit',
  '',
  `- Completed: ${report.completedAt}`,
  `- Requested routes: ${report.requestedRouteCount}`,
  `- Routes visited after link discovery: ${report.routeCount}`,
  `- Routes with issues: ${failingRoutes.length}`,
  `- Browser observations: ${observations.length}`,
  `- External links checked: ${externalLinkResults.length}`,
  `- Interaction checks: ${interactionChecks.filter((check) => check.passed).length}/${interactionChecks.length}`,
  '',
  '## Route results',
  '',
  '| Route | Final URL | Controls | Links | Issues |',
  '| --- | --- | ---: | ---: | ---: |',
  ...results.map(
    (result) =>
      `| ${result.route} | ${new URL(result.finalUrl).pathname} | ${result.controlCount} | ${result.linkCount} | ${routeIssueCount(result)} |`,
  ),
  '',
  '## Observations',
  '',
  ...(observations.length === 0
    ? ['None.']
    : observations.map(
        (observation) =>
          `- ${observation.route}: ${observation.kind}: ${observation.detail}`,
      )),
  '',
];
await fs.writeFile(path.join(outputDirectory, 'summary.md'), `${summary.join('\n')}\n`);

console.log(
  JSON.stringify(
    {
      externalLinks: externalLinkResults.length,
      failingRoutes: failingRoutes.map((result) => result.route),
      interactionChecks,
      observations: observations.length,
      outputDirectory,
      routes: results.length,
    },
    null,
    2,
  ),
);

await browser.close();
