import api from './api';

export const getAvailable = async () => {
  const response = (await api.get('/rooms/available')).data;

  if (!Array.isArray(response)) {
    console.warn('got non-array response from rooms API', response);
    return [];
  }

  return response;
};

export const getJoined = async () => {
  const response = (await api.get('/rooms/joined')).data;

  if (!Array.isArray(response)) {
    console.warn('got non-array response from rooms API', response);
    return [];
  }

  return response;
};

export const getActivity = async () => {
  const response = (await api.get('/rooms/activity')).data;

  if (!response || typeof response !== 'object' || Array.isArray(response)) {
    console.warn('got non-object response from room activity API', response);
    return {};
  }

  return Object.fromEntries(
    Object.entries(response)
      .map(([roomName, timestamp]) => [roomName, Number(timestamp)])
      .filter(
        ([roomName, timestamp]) =>
          roomName.length > 0 && Number.isFinite(timestamp) && timestamp > 0,
      ),
  );
};

export const getMessages = async ({ roomName, since = null }) => {
  const query =
    since == null ? '' : `?since=${encodeURIComponent(String(since))}`;
  const response = (
    await api.get(
      `/rooms/joined/${encodeURIComponent(roomName)}/messages${query}`,
    )
  ).data;

  if (!Array.isArray(response)) {
    console.warn('got non-array response from rooms API', response);
    return [];
  }

  return response;
};

export const getUsers = async ({ roomName }) => {
  const response = (
    await api.get(`/rooms/joined/${encodeURIComponent(roomName)}/users`)
  ).data;

  if (!Array.isArray(response)) {
    console.warn('got non-array response from rooms API', response);
    return [];
  }

  return response;
};

export const join = async ({ roomName }) => {
  return api.post('/rooms/joined', JSON.stringify(roomName));
};

export const leave = async ({ roomName }) => {
  return api.delete(`/rooms/joined/${encodeURIComponent(roomName)}`);
};

export const sendMessage = async ({ roomName, message }) => {
  return api.post(
    `/rooms/joined/${encodeURIComponent(roomName)}/messages`,
    JSON.stringify(message),
  );
};

export const setTicker = async ({ roomName, message }) => {
  return api.post(
    `/rooms/joined/${encodeURIComponent(roomName)}/ticker`,
    JSON.stringify(message),
  );
};

export const addRoomMember = async ({ roomName, username }) => {
  return api.post(
    `/rooms/joined/${encodeURIComponent(roomName)}/members`,
    JSON.stringify(username),
  );
};
