import * as optionsApi from './options';
import {
  getLocalStorageItem,
  setLocalStorageItem,
} from './storage';

export const humanChallengeAutoResponseStorageKey =
  'slskdn.humanChallengeAutoResponse.enabled';

export const readStoredHumanChallengeAutoResponse = () =>
  getLocalStorageItem(humanChallengeAutoResponseStorageKey, 'false') === 'true';

export const writeStoredHumanChallengeAutoResponse = (enabled) =>
  setLocalStorageItem(
    humanChallengeAutoResponseStorageKey,
    enabled ? 'true' : 'false',
  );

export const getHumanChallengeAutoResponseEnabled = (options = {}) =>
  Boolean(
    options?.soulseek?.privateMessageAutoResponse?.enabled ??
      options?.Soulseek?.PrivateMessageAutoResponse?.Enabled,
  );

export const canApplyHumanChallengeAutoResponse = (options = {}) =>
  Boolean(options?.remoteConfiguration ?? options?.RemoteConfiguration);

export const applyHumanChallengeAutoResponse = async (enabled) => {
  await optionsApi.applyOverlay({
    soulseek: {
      privateMessageAutoResponse: {
        enabled,
      },
    },
  });
};
