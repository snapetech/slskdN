import {
  EXPERIENCE_PREFERENCES_STORAGE_KEY,
  notifyExperiencePreferencesChanged,
  readExperiencePreference,
  setExperiencePreference,
  subscribeToExperiencePreferences,
} from './experiencePreferences';
import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('experiencePreferences', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('reads a persisted boolean and falls back for malformed values', () => {
    localStorage.setItem(
      EXPERIENCE_PREFERENCES_STORAGE_KEY,
      JSON.stringify({
        playerVisible: false,
        searchAlbumCandidatesVisible: 'false',
      }),
    );

    expect(readExperiencePreference('playerVisible', true)).toBe(false);
    expect(readExperiencePreference('searchAlbumCandidatesVisible', true)).toBe(
      true,
    );
    expect(readExperiencePreference('missing', true)).toBe(true);
  });

  it('notifies same-tab subscribers and supports cleanup', () => {
    const callback = vi.fn();
    const unsubscribe = subscribeToExperiencePreferences(callback);

    notifyExperiencePreferencesChanged();
    expect(callback).toHaveBeenCalledTimes(1);

    unsubscribe();
    notifyExperiencePreferencesChanged();
    expect(callback).toHaveBeenCalledTimes(1);
  });

  it('updates one preference without discarding the other browser choices', () => {
    localStorage.setItem(
      EXPERIENCE_PREFERENCES_STORAGE_KEY,
      JSON.stringify({ searchAlbumCandidatesVisible: false }),
    );

    expect(setExperiencePreference('playerVisible', false)).toBe(true);
    expect(JSON.parse(localStorage.getItem(EXPERIENCE_PREFERENCES_STORAGE_KEY))).toEqual({
      playerVisible: false,
      searchAlbumCandidatesVisible: false,
    });
  });
});
