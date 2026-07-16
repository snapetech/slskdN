import api from './api';

const STATUS_CACHE_TTL_MS = 15_000;
let statusCache = null;
let statusCacheExpiresAt = 0;
let statusInflight = null;

export const getStatus = () => {
  if (statusCache && statusCacheExpiresAt > Date.now()) {
    return Promise.resolve(statusCache);
  }

  if (statusInflight) {
    return statusInflight;
  }

  statusInflight = api
    .get('/integrations/lidarr/status')
    .then((response) => {
      statusCache = response.data;
      statusCacheExpiresAt = Date.now() + STATUS_CACHE_TTL_MS;
      return statusCache;
    })
    .finally(() => {
      statusInflight = null;
    });

  return statusInflight;
};

export const getSyncStatus = async () =>
  (await api.get('/integrations/lidarr/sync/status')).data;

export const getWantedMissing = async ({ page = 1, pageSize = 50 } = {}) =>
  (await api.get(`/integrations/lidarr/wanted/missing?page=${page}&pageSize=${pageSize}`)).data;

export const syncWanted = async () =>
  (await api.post('/integrations/lidarr/wanted/sync')).data;

export const importCompletedDirectory = async ({ directory }) =>
  (await api.post('/integrations/lidarr/manualimport', { directory })).data;
