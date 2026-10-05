const CACHE_NAME = 'slskdn-shell-retired-v3';
const CACHE_NAME_PREFIX = 'slskdn-shell-';

self.addEventListener('install', (event) => {
  event.waitUntil(caches.delete(CACHE_NAME));
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(
        keys
          .filter((key) => key.startsWith(CACHE_NAME_PREFIX))
          .map((key) => caches.delete(key)),
      ),
    ).then(() => self.registration.unregister()),
  );
  self.clients.claim();
});

self.addEventListener('fetch', (event) => {
  event.respondWith(fetch(event.request));
});
