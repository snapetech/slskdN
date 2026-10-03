import api from './api';
import { urlBase } from '../config';

export const createStreamTicket = async (contentId, signal) => {
  const path = `/streams/${encodeURIComponent(contentId)}/ticket`;
  const response = signal
    ? await api.post(path, undefined, { signal })
    : await api.post(path);
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

export const createRemoteShareStreamUrl = async (
  streamUrl,
  contentId,
  shareToken,
) => {
  if (typeof shareToken !== 'string' || !shareToken.trim()) {
    throw new Error('This share does not include a stream credential.');
  }

  const manifestStreamUrl = new URL(streamUrl, window.location.origin);
  if (
    !['http:', 'https:'].includes(manifestStreamUrl.protocol) ||
    manifestStreamUrl.username ||
    manifestStreamUrl.password
  ) {
    throw new Error('The share owner returned an invalid stream address.');
  }

  const routeMarker = '/api/v0/streams/';
  const routeIndex = manifestStreamUrl.pathname.lastIndexOf(routeMarker);
  if (routeIndex < 0) {
    throw new Error('The share owner returned an unsupported stream address.');
  }

  const pathPrefix = manifestStreamUrl.pathname.slice(0, routeIndex);
  const encodedContentId = encodeURIComponent(contentId);
  const ticketEndpoint = new URL(
    `${pathPrefix}${routeMarker}${encodedContentId}/share-ticket`,
    manifestStreamUrl.origin,
  );

  const response = await fetch(ticketEndpoint.href, {
    cache: 'no-store',
    credentials: 'omit',
    headers: { 'X-Share-Token': shareToken },
    method: 'POST',
    mode: 'cors',
    redirect: 'error',
  });

  if (!response.ok) {
    throw new Error(
      `The share owner returned HTTP ${response.status} while preparing the stream.`,
    );
  }

  let payload;
  try {
    payload = await response.json();
  } catch {
    throw new Error('The share owner returned an invalid stream ticket.');
  }

  const ticket = typeof payload?.ticket === 'string' ? payload.ticket.trim() : '';
  if (!ticket || ticket.length > 2048) {
    throw new Error('The share owner returned an invalid stream ticket.');
  }

  const ticketedStreamUrl = new URL(
    `${pathPrefix}${routeMarker}${encodedContentId}`,
    manifestStreamUrl.origin,
  );
  ticketedStreamUrl.searchParams.set('ticket', ticket);
  return ticketedStreamUrl.href;
};

export const buildDirectStreamUrl = (contentId) =>
  `${urlBase}/api/v0/streams/${encodeURIComponent(contentId)}`;

export const getPlaybackInfo = (contentId, signal) => {
  const path = `/streams/${encodeURIComponent(contentId)}/playback-info`;
  return signal ? api.get(path, { signal }) : api.get(path);
};

export const updatePlayerTags = (contentId, data) =>
  api.put(`/player-tags/${encodeURIComponent(contentId)}`, data);

export const buildTranscodedStreamUrl = (contentId, ticket, startSeconds = 0) =>
  `${urlBase}/api/v0/streams/${encodeURIComponent(contentId)}/transcoded?ticket=${encodeURIComponent(ticket)}&startSeconds=${encodeURIComponent(startSeconds)}`;

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
