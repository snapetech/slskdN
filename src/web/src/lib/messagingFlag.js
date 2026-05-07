import {
  getLocalStorageItem,
  removeLocalStorageItem,
  setLocalStorageItem,
} from './storage';

export const FLAG_KEY = 'slskd-messaging-v2';

export const isV2Enabled = () => getLocalStorageItem(FLAG_KEY) === 'on';

export const setV2Enabled = (enabled) => {
  if (enabled) {
    setLocalStorageItem(FLAG_KEY, 'on');
  } else {
    removeLocalStorageItem(FLAG_KEY);
  }
  return Boolean(enabled);
};

export const toggleV2Enabled = () => setV2Enabled(!isV2Enabled());
