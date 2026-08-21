import { getLocalStorageItem } from './storage';
import { useEffect, useState } from 'react';

export const EXPERIENCE_PREFERENCES_STORAGE_KEY =
  'slskdn:experience-preferences:v1';
export const EXPERIENCE_PREFERENCES_CHANGED_EVENT =
  'slskdn:experience-preferences-changed';

export const readExperiencePreference = (key, fallback) => {
  const stored = getLocalStorageItem(EXPERIENCE_PREFERENCES_STORAGE_KEY);
  if (!stored) return fallback;

  try {
    const parsed = JSON.parse(stored);
    const value = parsed?.[key];
    return typeof value === typeof fallback ? value : fallback;
  } catch {
    return fallback;
  }
};

export const notifyExperiencePreferencesChanged = () => {
  if (typeof window === 'undefined') return;
  window.dispatchEvent(new window.Event(EXPERIENCE_PREFERENCES_CHANGED_EVENT));
};

export const subscribeToExperiencePreferences = (callback) => {
  if (typeof window === 'undefined') return () => {};

  const handleStorageChange = (event) => {
    if (event.key === EXPERIENCE_PREFERENCES_STORAGE_KEY) {
      callback();
    }
  };

  window.addEventListener('storage', handleStorageChange);
  window.addEventListener(EXPERIENCE_PREFERENCES_CHANGED_EVENT, callback);

  return () => {
    window.removeEventListener('storage', handleStorageChange);
    window.removeEventListener(EXPERIENCE_PREFERENCES_CHANGED_EVENT, callback);
  };
};

export const useExperiencePreference = (key, fallback) => {
  const [value, setValue] = useState(() =>
    readExperiencePreference(key, fallback),
  );

  useEffect(() => {
    const refresh = () => setValue(readExperiencePreference(key, fallback));
    return subscribeToExperiencePreferences(refresh);
  }, [fallback, key]);

  return value;
};
