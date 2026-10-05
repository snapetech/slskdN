// <copyright file="registerServiceWorker.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

const loadModule = async (urlBase) => {
  vi.resetModules();
  vi.doMock('./config', () => ({
    urlBase,
  }));

  return import('./registerServiceWorker');
};

describe('cleanUpServiceWorker', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('cleans up only this app worker and cache names after the document is loaded', async () => {
    const appOrigin = window.location.origin;
    const unregisterApp = vi.fn().mockResolvedValue(true);
    const unregisterSibling = vi.fn().mockResolvedValue(true);
    const unregisterWrongScript = vi.fn().mockResolvedValue(true);
    const getRegistrations = vi
      .fn()
      .mockResolvedValue([
        {
          scope: new URL('/system/', appOrigin).href,
          active: { scriptURL: new URL('/system/service-worker.js', appOrigin).href },
          unregister: unregisterApp,
        },
        {
          scope: new URL('/other/', appOrigin).href,
          active: { scriptURL: new URL('/other/service-worker.js', appOrigin).href },
          unregister: unregisterSibling,
        },
        {
          scope: new URL('/system/', appOrigin).href,
          active: { scriptURL: new URL('/foreign-worker.js', appOrigin).href },
          unregister: unregisterWrongScript,
        },
      ]);
    const deleteCache = vi.fn().mockResolvedValue(true);
    const keys = vi
      .fn()
      .mockResolvedValue(['slskdn-shell-v1', 'other-app-cache']);
    Object.defineProperty(document, 'readyState', {
      configurable: true,
      value: 'complete',
    });
    Object.defineProperty(globalThis, 'navigator', {
      configurable: true,
      value: { serviceWorker: { getRegistrations } },
    });
    Object.defineProperty(globalThis, 'caches', {
      configurable: true,
      value: { delete: deleteCache, keys },
    });

    const { cleanUpServiceWorker } = await loadModule('/system');
    await cleanUpServiceWorker();

    expect(unregisterApp).toHaveBeenCalledOnce();
    expect(unregisterSibling).not.toHaveBeenCalled();
    expect(unregisterWrongScript).not.toHaveBeenCalled();
    expect(keys).toHaveBeenCalled();
    expect(deleteCache).toHaveBeenCalledTimes(1);
    expect(deleteCache).toHaveBeenCalledWith('slskdn-shell-v1');
    expect(deleteCache).not.toHaveBeenCalledWith('other-app-cache');
  });

  it('waits for window load when the document is still loading', async () => {
    const addEventListener = vi.spyOn(window, 'addEventListener');
    const getRegistrations = vi.fn().mockResolvedValue([]);
    Object.defineProperty(document, 'readyState', {
      configurable: true,
      value: 'loading',
    });
    Object.defineProperty(globalThis, 'navigator', {
      configurable: true,
      value: { serviceWorker: { getRegistrations } },
    });

    const { cleanUpServiceWorker } = await loadModule('');
    cleanUpServiceWorker();

    const loadCall = addEventListener.mock.calls.find(
      ([eventName]) => eventName === 'load',
    );
    expect(loadCall).toBeDefined();
    const [, cleanUp] = loadCall;
    await cleanUp();

    expect(getRegistrations).toHaveBeenCalledOnce();
    expect(addEventListener).toHaveBeenCalledWith(
      'load',
      expect.any(Function),
      { once: true },
    );

    addEventListener.mockRestore();
  });
});
