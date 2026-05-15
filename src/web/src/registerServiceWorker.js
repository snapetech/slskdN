// <copyright file="registerServiceWorker.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import { urlBase } from './config';

const getServiceWorkerUrl = () => {
  const normalizedBase = urlBase && urlBase !== '/' ? urlBase : '';
  return `${normalizedBase}/service-worker.js`;
};

const getServiceWorkerScope = () => {
  const normalizedBase = urlBase && urlBase !== '/' ? urlBase : '';
  return normalizedBase ? `${normalizedBase}/` : '/';
};

export const registerServiceWorker = () => {
  if (
    typeof window === 'undefined' ||
    typeof navigator === 'undefined' ||
    !('serviceWorker' in navigator)
  ) {
    return;
  }

  const register = async () => {
    try {
      const registrations = await navigator.serviceWorker.getRegistrations?.();
      await Promise.all(
        (registrations || []).map((registration) => registration.unregister()),
      );
      await globalThis.caches?.keys?.().then((keys) =>
        Promise.all(keys.map((key) => globalThis.caches.delete(key))),
      );
      const registration = await navigator.serviceWorker.register(getServiceWorkerUrl(), {
        scope: getServiceWorkerScope(),
      });
      await registration.unregister();
    } catch (error) {
      console.debug('Service worker registration failed:', error);
    }
  };

  if (document.readyState === 'complete') {
    register();
    return;
  }

  window.addEventListener('load', register, { once: true });
};

export { getServiceWorkerScope, getServiceWorkerUrl };
