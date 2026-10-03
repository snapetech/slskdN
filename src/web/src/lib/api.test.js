// <copyright file="api.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api, { getCsrfTokenFromCookieString } from './api';
import { tokenPassthroughValue } from '../config';
import { getToken, setToken } from './token';

const getResponseErrorHandler = () =>
  api.interceptors.response.handlers.find(({ rejected }) => rejected)?.rejected;

const unauthorizedError = (url) => ({
  response: { status: 401, config: { url }, headers: {} },
});

const stubWindowReload = () => {
  const { localStorage, sessionStorage } = window;
  const reload = vi.fn();
  vi.stubGlobal('window', {
    localStorage,
    sessionStorage,
    location: { reload },
  });
  return reload;
};

describe('api csrf token selection', () => {
  afterEach(() => {
    delete window.port;
    vi.unstubAllGlobals();
  });

  it('prefers the current port scoped csrf token', () => {
    const token = getCsrfTokenFromCookieString(
      'XSRF-TOKEN-5031=https-token; XSRF-TOKEN-5030=http-token',
      '5030',
    );

    expect(token).toBe('http-token');
  });

  it('falls back to the legacy csrf token name', () => {
    const token = getCsrfTokenFromCookieString('XSRF-TOKEN=legacy-token', '');

    expect(token).toBe('legacy-token');
  });

  it('ignores the antiforgery cookie token name', () => {
    const token = getCsrfTokenFromCookieString(
      'XSRF-COOKIE-5030=cookie-token; XSRF-TOKEN-5030=request-token',
      '5030',
    );

    expect(token).toBe('request-token');
  });

  it('falls back to the only port scoped token when the browser url has no port', () => {
    const token = getCsrfTokenFromCookieString(
      'XSRF-TOKEN-5030=request-token',
      '',
    );

    expect(token).toBe('request-token');
  });

  it('uses the injected backend port by default before the browser url port', () => {
    window.port = '5030';

    const token = getCsrfTokenFromCookieString(
      'XSRF-TOKEN-5030=request-token; XSRF-TOKEN-443=proxy-token',
    );

    expect(token).toBe('request-token');
  });

  it.each([
    ['telemetry KPIs', '/telemetry/metrics/kpi'],
    ['unacknowledged conversations', '/conversations/activity/unacknowledged'],
    ['room activity', '/rooms/activity'],
  ])(
    'preserves passthrough state without reloading after a 401 from %s',
    async (_, url) => {
      const reload = stubWindowReload();
      setToken(sessionStorage, tokenPassthroughValue);

      const error = unauthorizedError(url);
      await expect(getResponseErrorHandler()(error)).rejects.toBe(error);

      expect(getToken()).toBe(tokenPassthroughValue);
      expect(reload).not.toHaveBeenCalled();
    },
  );

  it('clears an authenticated session and reloads after a 401', async () => {
    const reload = stubWindowReload();
    setToken(sessionStorage, 'jwt-token');

    const error = unauthorizedError('/telemetry/metrics/kpi');
    await expect(getResponseErrorHandler()(error)).rejects.toBe(error);

    expect(getToken()).toBeNull();
    expect(reload).toHaveBeenCalledTimes(1);
  });
});
