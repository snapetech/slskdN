import api from './api';
import { rootUrl } from '../config';

export const getPartyDirectory = async ({ refresh = false } = {}) => {
  const { data } = await api.get(refresh ? '/listening-party?refresh=true' : '/listening-party');
  return Array.isArray(data) ? data : [];
};

export const getPartyState = async (podId, channelId, { signal } = {}) => {
  const response = await api.get(
    `/listening-party/${encodeURIComponent(podId)}/${encodeURIComponent(channelId)}`,
    { signal, validateStatus: (status) => status === 200 || status === 204 },
  );

  return response.status === 204 ? null : response.data;
};

export const publishPartyState = async (podId, channelId, event, { signal, hostSessionId, startHostSession } = {}) => {
  const headers = {};
  if (hostSessionId) headers['X-Listen-Along-Host-Session'] = hostSessionId;
  if (startHostSession) headers['X-Listen-Along-Start-Host-Session'] = 'true';
  return (
    await api.post(
      `/listening-party/${encodeURIComponent(podId)}/${encodeURIComponent(channelId)}`,
      event,
      { signal, headers },
    )
  ).data;
};

export const renewHostSession = async (podId, channelId, partyId, { signal, hostSessionId } = {}) => {
  await api.post(
    `/listening-party/${encodeURIComponent(podId)}/${encodeURIComponent(channelId)}/renew`,
    { partyId },
    { signal, headers: { 'X-Listen-Along-Host-Session': hostSessionId } },
  );
};

export const buildRadioStreamUrl = (party) => {
  if (!party?.streamPath) return null;
  return `${rootUrl}${party.streamPath}`;
};


export const createRadioStreamUrl = async (partyId, contentId) => {
  const { data } = await api.post(`/listed-radio/${encodeURIComponent(partyId)}/tickets`, { contentId });
  if (!data?.streamUrl?.startsWith('/api/v0/mesh-streams/') && !data?.streamUrl?.startsWith('/api/v0/listening-party/radio/')) throw new Error('Invalid radio stream ticket');
  return `${rootUrl}${data.streamUrl}`;
};
