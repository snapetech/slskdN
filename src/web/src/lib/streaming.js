import api from './api';
import { urlBase } from '../config';

export const createStreamTicket = async (contentId) => {
  const response = await api.post(
    `/streams/${encodeURIComponent(contentId)}/ticket`,
  );
  return response.data?.ticket || '';
};

export const buildTicketedStreamUrl = (contentId, ticket) =>
  `${urlBase}/api/v0/streams/${encodeURIComponent(contentId)}?ticket=${encodeURIComponent(ticket)}`;

// Exchanges a share token for a short-lived, content-bound stream ticket. The share token is sent in
// the X-Share-Token header (never the URL) so it stays out of browser history, proxy logs, and access
// logs; the returned opaque ticket is safe to place in the stream URL.
export const createShareStreamTicket = async (contentId, shareToken) => {
  const response = await api.post(
    `/streams/${encodeURIComponent(contentId)}/share-ticket`,
    undefined,
    { headers: { 'X-Share-Token': shareToken } },
  );
  return response.data?.ticket || '';
};

export const buildDirectStreamUrl = (contentId) =>
  `${urlBase}/api/v0/streams/${encodeURIComponent(contentId)}`;

export const createPeerStreamTicket = async ({ username, filename, size }) => {
  const response = await api.post('/peer-streams/tickets', {
    username,
    filename,
    size,
  });
  return response.data || {};
};

export const buildPeerStreamUrl = (streamUrl) => {
  if (!streamUrl) return '';
  if (/^https?:\/\//i.test(streamUrl)) return streamUrl;
  return `${urlBase}${streamUrl}`;
};
