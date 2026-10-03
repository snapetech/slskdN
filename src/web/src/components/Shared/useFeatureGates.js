import * as slskdn from '../../lib/slskdn';
import { useSyncExternalStore } from 'react';

const REFRESH_INTERVAL_MS = 60_000;
const initialState = {
  featureGates: {},
  ready: false,
};
const subscribers = new Set();

let state = initialState;
let refreshInFlight = null;
let refreshInterval = null;

const isObject = (value) => value && typeof value === 'object' && !Array.isArray(value);

const gatesAreEqual = (left, right) => {
  const leftKeys = Object.keys(left);
  const rightKeys = Object.keys(right);
  return (
    leftKeys.length === rightKeys.length &&
    leftKeys.every((key) => {
      const leftGate = left[key];
      const rightGate = right[key];
      return (
        rightGate &&
        leftGate?.enabled === rightGate.enabled &&
        leftGate?.status === rightGate.status &&
        leftGate?.message === rightGate.message
      );
    })
  );
};

const notifySubscribers = () => {
  subscribers.forEach((subscriber) => subscriber());
};

const refresh = () => {
  if (document.hidden || refreshInFlight) return refreshInFlight;

  refreshInFlight = slskdn.getCapabilities()
    .then((capabilities) => {
      const featureGates = isObject(capabilities?.featureGates)
        ? capabilities.featureGates
        : {};

      if (!state.ready || !gatesAreEqual(state.featureGates, featureGates)) {
        state = { featureGates, ready: true };
        notifySubscribers();
      }
    })
    .finally(() => {
      refreshInFlight = null;
    });

  return refreshInFlight;
};

const handleVisibilityChange = () => {
  if (!document.hidden) refresh();
};

const subscribe = (subscriber) => {
  subscribers.add(subscriber);
  if (subscribers.size === 1) {
    state = { ...state, ready: false };
    refresh();
    refreshInterval = window.setInterval(refresh, REFRESH_INTERVAL_MS);
    document.addEventListener('visibilitychange', handleVisibilityChange);
  }

  return () => {
    subscribers.delete(subscriber);
    if (subscribers.size === 0) {
      window.clearInterval(refreshInterval);
      refreshInterval = null;
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      state = { ...state, ready: false };
    }
  };
};

const getSnapshot = () => state;

const useFeatureGates = () =>
  useSyncExternalStore(subscribe, getSnapshot, getSnapshot);

export const isFeatureEnabled = (featureGates, featureId) =>
  featureGates?.[featureId]?.enabled !== false;

export default useFeatureGates;
