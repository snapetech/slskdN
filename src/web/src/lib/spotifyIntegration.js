import api from './api';

export const getSpotifyStatus = async () =>
  (await api.get('/integrations/spotify/status')).data;

export const startSpotifyAuthorization = async () => {
  const data = (await api.post('/integrations/spotify/authorize')).data;
  const authorizationUrl = data?.authorizationUrl || data?.authorization_url;

  if (authorizationUrl) {
    window.location.assign(authorizationUrl);
  }

  return data;
};

export const disconnectSpotify = async () => {
  await api.delete('/integrations/spotify');
};
