import api from './api';

export const list = async ({ offset, limit }) => {
  const response = await api.get(`/events?offset=${offset}&limit=${limit}`);

  const events = Array.isArray(response.data) ? response.data : [];
  const totalCount = response.headers['x-total-count'];

  return { events, totalCount };
};

export const raiseEvent = async ({ type, disambiguator = '' }) => {
  return api.post(
    `/events/${encodeURIComponent(type)}`,
    JSON.stringify(disambiguator),
  );
};
