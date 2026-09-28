import useListeningPartyBroadcast from './useListeningPartyBroadcast';
import useListeningPartyRooms from './useListeningPartyRooms';
import * as nowPlaying from '../../lib/nowPlaying';
import { useExperiencePreference } from '../../lib/experiencePreferences';
import { getSessionStorageItem, setSessionStorageItem } from '../../lib/storage';
import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
} from 'react';

export const PlayerContext = createContext({
  broadcastStatus: null,
  publishBroadcast: () => Promise.resolve(null),
  reportPlaybackEvent: () => {},
  retryBroadcast: () => Promise.resolve(null),
  stopBroadcast: () => Promise.resolve(null),
  clearQueue: () => {},
  clear: () => {},
  current: null,
  followParty: () => {},
  followingParty: null,
  followingPartyStatus: null,
  observePartyRoom: () => () => {},
  retryPartyRoom: () => {},
  getPlaybackPosition: () => 0,
  history: [],
  next: () => {},
  moveQueueItem: () => {},
  pause: () => {},
  playItem: () => {},
  playNext: () => {},
  previous: () => {},
  queue: [],
  queueItems: () => {},
  removeFromQueue: () => {},
  repeatMode: 'off',
  setRepeatMode: () => {},
  setShuffle: () => {},
  shuffle: false,
  setAudioElement: () => {},
  setPauseHandler: () => {},
  setPlaybackPosition: () => {},
  playerVisible: true,
});

const asArray = (value) => (Array.isArray(value) ? value : []);
const normalizePlayerItem = (item, options = {}) => ({
  album: item.album || item.collectionTitle || '',
  artist: item.artist || item.username || 'slskdN',
  artworkUrl: item.artworkUrl || item.coverUrl || item.imageUrl || '',
  confidence: item.confidence || item.matchConfidence || item.score || 0,
  contentId: item.contentId,
  fileName: item.fileName || item.title || item.contentId,
  genre: item.genre || '',
  positionSeconds: Number.isFinite(options.positionSeconds)
    ? Math.max(0, options.positionSeconds)
    : 0,
  startPaused: options.startPaused === true,
  sourceProviders: asArray(item.sourceProviders || item.providers),
  streamUrl: item.streamUrl || options.streamUrl || '',
  radioPartyId: item.radioPartyId || '',
  tags: asArray(item.tags || item.genres),
  title: item.title || item.fileName || item.contentId,
  verified: Boolean(
    item.verified ||
    item.verifiedAt ||
    item.fingerprint?.verifiedAt ||
    item.verification?.verified,
  ),
});
const isRestorableItem = (item) =>
  typeof item?.contentId === 'string' &&
  !item.contentId.startsWith('local:') &&
  !item.streamUrl && !item.radioPartyId;
const sessionKey = 'slskdn.player.session.v1';
const readSession = () => {
  try {
    const value = JSON.parse(getSessionStorageItem(sessionKey, '{}'));
    const queue = asArray(value.queue).filter(isRestorableItem).map((item) => normalizePlayerItem(item, {
      positionSeconds: item.positionSeconds,
      startPaused: item.startPaused,
    }));
    return {
      current: queue[0] || null,
      queue,
      repeatMode: ['off', 'all', 'one'].includes(value.repeatMode) ? value.repeatMode : 'off',
      shuffle: value.shuffle === true,
    };
  } catch {
    return { current: null, queue: [], repeatMode: 'off', shuffle: false };
  }
};

export const PlayerProvider = ({ children }) => {
  const [initialSession] = useState(readSession);
  const [audioElement, setAudioElement] = useState(null);
  const [playback, setPlayback] = useState(() => ({
    current: initialSession.current,
    history: [],
    queue: initialSession.queue,
  }));
  const { current, history, queue } = playback;
  const [repeatMode, setRepeatMode] = useState(initialSession.repeatMode);
  const [shuffle, setShuffle] = useState(initialSession.shuffle);
  const [followingParty, setFollowingParty] = useState(null);
  const pauseHandlerRef = useRef(null);
  const followPartyRef = useRef(() => {});
  const playerVisible = useExperiencePreference('playerVisible', true);
  const previousPlayerVisible = useRef(playerVisible);
  const playbackPositionRef = useRef(initialSession.current?.positionSeconds || 0);

  const getPlaybackPosition = useCallback(() => playbackPositionRef.current, []);
  const setPlaybackPosition = useCallback((seconds) => {
    if (Number.isFinite(seconds)) playbackPositionRef.current = Math.max(0, seconds);
  }, []);

  useLayoutEffect(() => {
    playbackPositionRef.current = current?.positionSeconds || 0;
  }, [current]);

  useEffect(() => {
    const saveSession = () => {
      setSessionStorageItem(sessionKey, JSON.stringify({
        queue: queue.filter(isRestorableItem).map((item) => item === current
          ? { ...item, positionSeconds: playbackPositionRef.current }
          : item),
        repeatMode,
        shuffle,
      }));
    };
    const handleVisibility = () => {
      if (document.visibilityState === 'hidden') saveSession();
    };
    saveSession();
    window.addEventListener('pagehide', saveSession);
    document.addEventListener('visibilitychange', handleVisibility);
    return () => {
      window.removeEventListener('pagehide', saveSession);
      document.removeEventListener('visibilitychange', handleVisibility);
    };
  }, [current, queue, repeatMode, shuffle]);

  const playItem = useCallback(
    (item, options = {}) => {
      if (!playerVisible || !item?.contentId) return;
      if (!options.fromParty) followPartyRef.current(null);

      const playable = normalizePlayerItem(item, options);

      playbackPositionRef.current = playable.positionSeconds;
      setPlayback((existing) => ({
        current: playable,
        history: options.replaceQueue
          ? []
          : existing.current
            ? [existing.current, ...existing.history]
            : existing.history,
        queue: options.replaceQueue
          ? [playable]
          : [playable, ...existing.queue.slice(existing.current ? 1 : 0)],
      }));

    },
    [playerVisible],
  );

  const setPauseHandler = useCallback((handler) => {
    pauseHandlerRef.current = handler;
  }, []);

  const pause = useCallback(() => {
    if (pauseHandlerRef.current) {
      pauseHandlerRef.current();
      return;
    }
    if (audioElement) {
      audioElement.pause();
    }
  }, [audioElement]);

  const clear = useCallback(() => {
    if (audioElement) {
      audioElement.pause();
      audioElement.removeAttribute('src');
      audioElement.load();
    }

    playbackPositionRef.current = 0;
    setPlayback({ current: null, history: [], queue: [] });
    followPartyRef.current(null);
    nowPlaying.clearNowPlaying().catch(() => {});
  }, [audioElement]);

  useEffect(() => {
    if (previousPlayerVisible.current && !playerVisible) {
      clear();
    }
    previousPlayerVisible.current = playerVisible;
  }, [clear, playerVisible]);

  const clearQueue = useCallback(() => {
    setPlayback((existing) => ({
      ...existing,
      queue: existing.current ? [existing.current] : existing.queue.slice(0, 1),
    }));
  }, []);

  const playNext = useCallback((item) => {
    if (!item?.contentId) return;
    if (!current) {
      playItem(item, { replaceQueue: true });
      return;
    }
    setPlayback((existing) => {
      const upcoming = existing.queue.slice(1);
      const existingIndex = upcoming.findIndex((entry) => entry.contentId === item.contentId);
      const nextItem = existingIndex >= 0
        ? upcoming.splice(existingIndex, 1)[0]
        : normalizePlayerItem(item);
      return {
        ...existing,
        queue: [...existing.queue.slice(0, 1), nextItem, ...upcoming],
      };
    });
  }, [current, playItem]);

  const moveQueueItem = useCallback((fromIndex, toIndex) => {
    setPlayback((existing) => {
      if (fromIndex < 1 || toIndex < 1 || fromIndex >= existing.queue.length || toIndex >= existing.queue.length) return existing;
      const updated = [...existing.queue];
      updated.splice(toIndex, 0, updated.splice(fromIndex, 1)[0]);
      return { ...existing, queue: updated };
    });
  }, []);

  const queueItems = useCallback((items = [], { allowDuplicates = false } = {}) => {
    setPlayback((existing) => {
      if (allowDuplicates) {
        const additions = items
          .filter((item) => item?.contentId)
          .map((item) => normalizePlayerItem(item));
        return additions.length > 0
          ? { ...existing, queue: [...existing.queue, ...additions] }
          : existing;
      }
      const queuedIds = new Set(existing.queue.map((item) => item.contentId));
      const additions = items.filter((item) => {
        if (!item?.contentId || queuedIds.has(item.contentId)) return false;
        queuedIds.add(item.contentId);
        return true;
      }).map((item) => normalizePlayerItem(item));

      return additions.length > 0
        ? { ...existing, queue: [...existing.queue, ...additions] }
        : existing;
    });
  }, []);

  const removeFromQueue = useCallback((queueIndex) => {
    setPlayback((existing) => {
      if (queueIndex < 1 || queueIndex >= existing.queue.length) return existing;
      return {
        ...existing,
        queue: existing.queue.filter((_, index) => index !== queueIndex),
      };
    });
  }, []);

  const { followParty, followingPartyStatus, observePartyRoom, retryPartyRoom } = useListeningPartyRooms({
    clear, current, followingParty, getPlaybackPosition, pause, playItem, playerVisible,
  }, setFollowingParty);
  useLayoutEffect(() => { followPartyRef.current = followParty; }, [followParty]);
  const { broadcastStatus, publishBroadcast, reportPlaybackEvent, retryBroadcast, stopBroadcast } = useListeningPartyBroadcast({
    audioElement, current, followParty, getPlaybackPosition, observePartyRoom, playerVisible,
  });
  const followPartyWithOwnership = useCallback((state) => {
    if (state && broadcastStatus?.active) {
      stopBroadcast().then((stopped) => {
        if (stopped?.action === 'stop') followParty(state);
      }).catch(() => {});
    } else followParty(state);
  }, [broadcastStatus?.active, followParty, stopBroadcast]);

  const next = useCallback(() => {
    const random = Math.random();
    setPlayback((existing) => {
      if (existing.queue.length < 2) {
        if (repeatMode === 'all' && existing.history.length > 0) {
          const [item, ...remaining] = [...existing.history].reverse().concat(existing.queue);
          const nextItem = { ...item, positionSeconds: 0, startPaused: false };
          return { current: nextItem, history: [], queue: [nextItem, ...remaining] };
        }
        return existing;
      }
      const nextIndex = shuffle ? 1 + Math.floor(random * (existing.queue.length - 1)) : 1;
      const nextItem = { ...existing.queue[nextIndex], positionSeconds: 0, startPaused: false };
      const remaining = existing.queue.slice(1).filter((_, index) => index + 1 !== nextIndex);
      return {
        current: nextItem,
        history: existing.current
          ? [existing.current, ...existing.history]
          : existing.history,
        queue: [nextItem, ...remaining],
      };
    });
  }, [repeatMode, shuffle]);

  const previous = useCallback(() => {
    if (history.length === 0) {
      if (audioElement) {
        audioElement.currentTime = 0;
      }
      playbackPositionRef.current = 0;
      return;
    }

    setPlayback((existing) => {
      if (existing.history.length === 0) return existing;
      const [item, ...remainingHistory] = existing.history;
      const previousItem = { ...item, positionSeconds: 0, startPaused: false };
      return {
        current: previousItem,
        history: remainingHistory,
        queue: [previousItem, ...existing.queue],
      };
    });
  }, [audioElement, history]);

  return (
    <PlayerContext.Provider
      value={{
        broadcastStatus,
        publishBroadcast,
        reportPlaybackEvent,
        retryBroadcast,
        stopBroadcast,
        clearQueue,
        clear,
        current,
        followParty: followPartyWithOwnership,
        followingParty,
        followingPartyStatus,
        observePartyRoom,
        retryPartyRoom,
        getPlaybackPosition,
        history,
        moveQueueItem,
        next,
        pause,
        playItem,
        playNext,
        playerVisible,
        previous,
        queue,
        queueItems,
        removeFromQueue,
        repeatMode,
        setRepeatMode,
        setShuffle,
        shuffle,
        setAudioElement,
        setPauseHandler,
        setPlaybackPosition,
      }}
    >
      {children}
    </PlayerContext.Provider>
  );
};

export const usePlayer = () => useContext(PlayerContext);
