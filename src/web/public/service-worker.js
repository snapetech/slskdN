const CACHE_NAME = 'slskdn-shell-retired-v3';

self.addEventListener('install', (event) => {
  event.waitUntil(caches.delete(CACHE_NAME));
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(keys.map((key) => caches.delete(key))),
    ).then(() => self.registration.unregister()),
  );
  self.clients.claim();
});

self.addEventListener('fetch', (event) => {
  event.respondWith(fetch(event.request));
});
