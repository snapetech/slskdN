// <copyright file="registerServiceWorker.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import { urlBase } from './config';

const APP_CACHE_NAME_PREFIX = 'slskdn-shell-';

const getServiceWorkerUrl = () => {
  const normalizedBase = urlBase && urlBase !== '/' ? urlBase : '';
  return `${normalizedBase}/service-worker.js`;
};

const getServiceWorkerScope = () => {
  const normalizedBase = urlBase && urlBase !== '/' ? urlBase : '';
  return normalizedBase ? `${normalizedBase}/` : '/';
};

const isAppServiceWorkerRegistration = (registration) => {
  const expectedScope = new URL(getServiceWorkerScope(), window.location.href).href;
  const expectedScriptUrl = new URL(getServiceWorkerUrl(), window.location.href).href;
  const scriptUrls = [
    registration.installing?.scriptURL,
    registration.waiting?.scriptURL,
    registration.active?.scriptURL,
  ];

  return registration.scope === expectedScope && scriptUrls.includes(expectedScriptUrl);
};

export const cleanUpServiceWorker = () => {
  if (
    typeof window === 'undefined' ||
    typeof navigator === 'undefined' ||
    !('serviceWorker' in navigator)
  ) {
    return;
  }

  const cleanUp = async () => {
    try {
      const registrations = await navigator.serviceWorker.getRegistrations?.();
      await Promise.all(
        (registrations || [])
          .filter(isAppServiceWorkerRegistration)
          .map((registration) => registration.unregister()),
      );
      const cacheNames = await globalThis.caches?.keys?.();
      await Promise.all(
        (cacheNames || [])
          .filter((name) => name.startsWith(APP_CACHE_NAME_PREFIX))
          .map((name) => globalThis.caches.delete(name)),
      );
    } catch (error) {
      console.debug('Service worker cleanup failed:', error);
    }
  };

  if (document.readyState === 'complete') {
    return cleanUp();
  }

  window.addEventListener('load', cleanUp, { once: true });
  return undefined;
};

export { getServiceWorkerScope, getServiceWorkerUrl, APP_CACHE_NAME_PREFIX };
