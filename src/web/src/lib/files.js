import api from './api';

export const encodePathSegment = (value = '') => {
  const bytes = new TextEncoder().encode(value);
  const binary = Array.from(bytes)
    .map((byte) => String.fromCharCode(byte))
    .join('');

  return encodeURIComponent(btoa(binary));
};

export const list = async ({ root, subdirectory = '' }) => {
  const response = (
    await api.get(`/files/${root}/directories/${encodePathSegment(subdirectory)}`)
  ).data;

  return response;
};

export const deleteDirectory = async ({ root, path }) => {
  const response = await api.delete(`/files/${root}/directories/${encodePathSegment(path)}`);

  return response;
};

export const deleteFile = async ({ root, path }) => {
  const response = await api.delete(`/files/${root}/files/${encodePathSegment(path)}`);

  return response;
};
