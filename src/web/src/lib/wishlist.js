import api from './api';

export const getAll = async () => {
  const data = (await api.get('/wishlist')).data;
  return Array.isArray(data) ? data : [];
};

export const get = async (id) => {
  return (await api.get(`/wishlist/${encodeURIComponent(id)}`)).data;
};

export const create = async ({
  searchText,
  filter,
  enabled,
  autoDownload,
  maxResults,
  maxDownloads,
}) => {
  return (
    await api.post('/wishlist', {
      autoDownload,
      enabled,
      filter,
      maxDownloads,
      maxResults,
      searchText,
    })
  ).data;
};

export const update = async (
  id,
  { searchText, filter, enabled, autoDownload, maxResults, maxDownloads },
) => {
  return (
    await api.put(`/wishlist/${encodeURIComponent(id)}`, {
      autoDownload,
      enabled,
      filter,
      maxDownloads,
      maxResults,
      searchText,
    })
  ).data;
};

export const remove = async (id) => {
  await api.delete(`/wishlist/${encodeURIComponent(id)}`);
};

export const runSearch = async (id) => {
  return (await api.post(`/wishlist/${encodeURIComponent(id)}/search`)).data;
};

export const importCsv = async ({
  csvText,
  filter,
  enabled,
  autoDownload,
  maxResults,
  includeAlbum,
}) => {
  return (
    await api.post('/wishlist/import/csv', {
      autoDownload,
      csvText,
      enabled,
      filter,
      includeAlbum,
      maxResults,
    })
  ).data;
};

export const getSearches = async (id, limit = 50) => {
  const data = (
    await api.get(`/wishlist/${encodeURIComponent(id)}/searches?limit=${limit}`)
  ).data;
  return Array.isArray(data) ? data : [];
};

export const markViewed = async (id) => {
  return api.post(`/wishlist/${encodeURIComponent(id)}/mark-viewed`);
};

export const markAllViewed = async () => {
  return api.post('/wishlist/mark-all-viewed');
};

export const getIgnoredResults = async (id) => {
  const data = (
    await api.get(`/wishlist/${encodeURIComponent(id)}/ignored-results`)
  ).data;
  return Array.isArray(data) ? data : [];
};

export const ignoreResult = async (id, { username, directory }) => {
  return (
    await api.post(`/wishlist/${encodeURIComponent(id)}/ignored-results`, {
      directory,
      username,
    })
  ).data;
};

export const removeIgnoredResult = async (id, ignoredResultId) => {
  await api.delete(
    `/wishlist/${encodeURIComponent(id)}/ignored-results/${encodeURIComponent(ignoredResultId)}`,
  );
};
