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

describe('registerServiceWorker', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('removes stale workers before registering the retired worker immediately when the document is already loaded', async () => {
    const unregisterOld = vi.fn().mockResolvedValue(true);
    const unregisterNew = vi.fn().mockResolvedValue(true);
    const register = vi.fn().mockResolvedValue({ unregister: unregisterNew });
    const getRegistrations = vi
      .fn()
      .mockResolvedValue([{ unregister: unregisterOld }]);
    const deleteCache = vi.fn().mockResolvedValue(true);
    const keys = vi.fn().mockResolvedValue(['old-cache']);
    Object.defineProperty(document, 'readyState', {
      configurable: true,
      value: 'complete',
    });
    Object.defineProperty(globalThis, 'navigator', {
      configurable: true,
      value: { serviceWorker: { getRegistrations, register } },
    });
    Object.defineProperty(globalThis, 'caches', {
      configurable: true,
      value: { delete: deleteCache, keys },
    });

    const { registerServiceWorker } = await loadModule('/system');
    registerServiceWorker();

    await vi.waitFor(() =>
      expect(register).toHaveBeenCalledWith('/system/service-worker.js', {
        scope: '/system/',
      }),
    );
    await vi.waitFor(() => expect(unregisterOld).toHaveBeenCalled());
    expect(keys).toHaveBeenCalled();
    expect(deleteCache).toHaveBeenCalledWith('old-cache');
    expect(unregisterNew).toHaveBeenCalled();
  });

  it('waits for window load when the document is still loading', async () => {
    const addEventListener = vi.spyOn(window, 'addEventListener');
    const register = vi.fn().mockResolvedValue({});
    Object.defineProperty(document, 'readyState', {
      configurable: true,
      value: 'loading',
    });
    Object.defineProperty(globalThis, 'navigator', {
      configurable: true,
      value: { serviceWorker: { register } },
    });

    const { registerServiceWorker } = await loadModule('');
    registerServiceWorker();

    expect(addEventListener).toHaveBeenCalledWith(
      'load',
      expect.any(Function),
      { once: true },
    );

    addEventListener.mockRestore();
  });
});
