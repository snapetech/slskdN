import * as nowPlaying from '../../lib/nowPlaying';
import { useExperiencePreference } from '../../lib/experiencePreferences';
import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
} from 'react';

export const PlayerContext = createContext({
  clearQueue: () => {},
  clear: () => {},
  current: null,
  followParty: () => {},
  followingParty: null,
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
  playerVisible: true,
});

const asArray = (value) => (Array.isArray(value) ? value : []);
const sessionKey = 'slskdn.player.session.v1';
const readSession = () => {
  try {
    const value = JSON.parse(window.sessionStorage.getItem(sessionKey) || '{}');
    const queue = asArray(value.queue).filter((item) =>
      typeof item?.contentId === 'string' && !item.contentId.startsWith('local:'));
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
  const [current, setCurrent] = useState(initialSession.current);
  const [history, setHistory] = useState([]);
  const [queue, setQueue] = useState(initialSession.queue);
  const [repeatMode, setRepeatMode] = useState(initialSession.repeatMode);
  const [shuffle, setShuffle] = useState(initialSession.shuffle);
  const [followingParty, setFollowingParty] = useState(null);
  const playerVisible = useExperiencePreference('playerVisible', true);
  const previousPlayerVisible = useRef(playerVisible);

  useEffect(() => {
    window.sessionStorage.setItem(sessionKey, JSON.stringify({
      queue: queue.filter((item) => !item.contentId.startsWith('local:')),
      repeatMode,
      shuffle,
    }));
  }, [queue, repeatMode, shuffle]);

  const playItem = useCallback(
    (item, options = {}) => {
      if (!playerVisible || !item?.contentId) return;

      const playable = {
        album: item.album || item.collectionTitle || '',
        artist: item.artist || item.username || 'slskdN',
        artworkUrl: item.artworkUrl || item.coverUrl || item.imageUrl || '',
        confidence: item.confidence || item.matchConfidence || item.score || 0,
        contentId: item.contentId,
        fileName: item.fileName || item.title || item.contentId,
        genre: item.genre || '',
        positionSeconds: options.positionSeconds || 0,
        sourceProviders: asArray(item.sourceProviders || item.providers),
        streamUrl: item.streamUrl || options.streamUrl || '',
        tags: asArray(item.tags || item.genres),
        title: item.title || item.fileName || item.contentId,
        verified: Boolean(
          item.verified ||
          item.verifiedAt ||
          item.fingerprint?.verifiedAt ||
          item.verification?.verified,
        ),
      };

      setHistory((existing) => (options.replaceQueue ? [] : current ? [current, ...existing] : existing));
      setCurrent(playable);
      setQueue((existing) =>
        options.replaceQueue ? [playable] : [playable, ...existing.slice(current ? 1 : 0)],
      );

    },
    [current, playerVisible],
  );

  const pause = useCallback(() => {
    if (audioElement) {
      audioElement.pause();
    }
  }, [audioElement]);

  const clear = useCallback(async () => {
    if (audioElement) {
      audioElement.pause();
      audioElement.removeAttribute('src');
      audioElement.load();
    }

    setCurrent(null);
    setHistory([]);
    setQueue([]);
    await nowPlaying.clearNowPlaying();
  }, [audioElement]);

  useEffect(() => {
    if (previousPlayerVisible.current && !playerVisible) {
      clear();
    }
    previousPlayerVisible.current = playerVisible;
  }, [clear, playerVisible]);

  const clearQueue = useCallback(() => {
    setQueue((existing) => (current ? [current] : existing.slice(0, 1)));
    setHistory([]);
  }, [current]);

  const playNext = useCallback((item) => {
    if (!item?.contentId) return;
    setQueue((existing) => [
      ...existing.slice(0, 1),
      item,
      ...existing.slice(1).filter((entry) => entry.contentId !== item.contentId),
    ]);
  }, []);

  const moveQueueItem = useCallback((fromIndex, toIndex) => {
    setQueue((existing) => {
      if (fromIndex < 1 || toIndex < 1 || fromIndex >= existing.length || toIndex >= existing.length) return existing;
      const updated = [...existing];
      updated.splice(toIndex, 0, updated.splice(fromIndex, 1)[0]);
      return updated;
    });
  }, []);

  const queueItems = useCallback((items = []) => {
    setQueue((existing) => {
      const queuedIds = new Set(existing.map((item) => item.contentId));
      const additions = items.filter((item) => {
        if (!item?.contentId || queuedIds.has(item.contentId)) return false;
        queuedIds.add(item.contentId);
        return true;
      });

      return additions.length > 0 ? [...existing, ...additions] : existing;
    });
  }, []);

  const removeFromQueue = useCallback((contentId) => {
    setQueue((existing) =>
      existing.filter((item, index) => index === 0 || item.contentId !== contentId),
    );
  }, []);

  const followParty = useCallback((partyState) => {
    setFollowingParty(partyState);
  }, []);

  const next = useCallback(() => {
    setQueue((existing) => {
      if (existing.length < 2) {
        if (repeatMode === 'all' && history.length > 0) {
          const [nextItem, ...remaining] = [...history].reverse().concat(existing);
          setCurrent(nextItem);
          setHistory([]);
          return [nextItem, ...remaining];
        }
        return existing;
      }
      const nextIndex = shuffle ? 1 + Math.floor(Math.random() * (existing.length - 1)) : 1;
      const nextItem = existing[nextIndex];
      const remaining = existing.slice(1).filter((_, index) => index + 1 !== nextIndex);
      setHistory((previousHistory) =>
        current ? [current, ...previousHistory] : previousHistory,
      );
      setCurrent(nextItem);
      return [nextItem, ...remaining];
    });
  }, [current, history, repeatMode, shuffle]);

  const previous = useCallback(() => {
    if (history.length === 0) {
      if (audioElement) {
        audioElement.currentTime = 0;
      }
      return;
    }

    const [previousItem, ...remainingHistory] = history;
    setHistory(remainingHistory);
    setCurrent(previousItem);
    setQueue((existing) => [previousItem, ...existing]);
  }, [audioElement, history]);

  return (
    <PlayerContext.Provider
      value={{
        clearQueue,
        clear,
        current,
        followParty,
        followingParty,
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
      }}
    >
      {children}
    </PlayerContext.Provider>
  );
};

export const usePlayer = () => useContext(PlayerContext);
