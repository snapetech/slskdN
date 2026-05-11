import api from './api';

export const getStatus = async () =>
  (await api.get('/integrations/lidarr/status')).data;

export const getSyncStatus = async () =>
  (await api.get('/integrations/lidarr/sync/status')).data;

export const getWantedMissing = async ({ page = 1, pageSize = 50 } = {}) =>
  (await api.get(`/integrations/lidarr/wanted/missing?page=${page}&pageSize=${pageSize}`)).data;

export const syncWanted = async () =>
  (await api.post('/integrations/lidarr/wanted/sync')).data;

export const importCompletedDirectory = async ({ directory }) =>
  (await api.post('/integrations/lidarr/manualimport', { directory })).data;
