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
}) => {
  return (
    await api.post('/wishlist', {
      autoDownload,
      enabled,
      filter,
      maxResults,
      searchText,
    })
  ).data;
};

export const update = async (
  id,
  { searchText, filter, enabled, autoDownload, maxResults },
) => {
  return (
    await api.put(`/wishlist/${encodeURIComponent(id)}`, {
      autoDownload,
      enabled,
      filter,
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
