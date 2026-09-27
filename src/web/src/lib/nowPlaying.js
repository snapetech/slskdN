import api from './api';

let pendingWrite = Promise.resolve();

const writeInOrder = (write) => {
  const result = pendingWrite.then(write);
  pendingWrite = result.catch(() => {});
  return result;
};

export const getNowPlaying = async () => {
  return (await api.get('/nowplaying')).data;
};

export const setNowPlaying = ({ artist, title, album } = {}) =>
  writeInOrder(() => api.put('/nowplaying', { artist, title, album }));

export const clearNowPlaying = () =>
  writeInOrder(() => api.delete('/nowplaying'));
