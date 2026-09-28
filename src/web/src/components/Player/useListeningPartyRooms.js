// <copyright file="useListeningPartyRooms.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import { createListeningPartyHubConnection } from '../../lib/hubFactory';
import * as listeningParty from '../../lib/listeningParty';
import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';

const roomKey = (room) => JSON.stringify([room.podId, room.channelId]);
const eventKey = (state) => JSON.stringify([
  state.partyId, state.sequence, state.serverTimeUnixMs, state.action,
  state.contentId, state.positionSeconds, state.streamUrl,
]);
const initialRoom = { connected: false, error: '', pending: true, state: null };

const applyPartyState = (state, player) => {
  const item = {
    album: state.album,
    artist: state.artist || state.hostPeerId,
    contentId: state.contentId,
    streamUrl: state.streamUrl,
    title: state.title || state.contentId,
  };
  const positionSeconds = Number.isFinite(state.positionSeconds) ? Math.max(0, state.positionSeconds) : 0;
  if (state.action === 'play' || state.action === 'seek') {
    const elapsed = state.action === 'play' && state.serverTimeUnixMs > 0
      ? Math.max(0, (Date.now() - state.serverTimeUnixMs) / 1000) : 0;
    player.playItem(item, { fromParty: true, positionSeconds: positionSeconds + elapsed, replaceQueue: true });
  } else if (state.action === 'pause') {
    player.pause();
    if (player.current?.contentId !== state.contentId || Math.abs(player.getPlaybackPosition() - positionSeconds) > 0.05) {
      player.playItem(item, { fromParty: true, positionSeconds, replaceQueue: true, startPaused: true });
    }
  }
};

// The player retains a followed room independently of route-owned observers.
// Each room has one hub, so revocation cannot affect another room. See ADR-0017.
export default function useListeningPartyRooms(player, setFollowingParty) {
  const roomsRef = useRef(new Map());
  const followingRef = useRef(null);
  const [followingPartyStatus, setFollowingPartyStatus] = useState(null);
  const playerRef = useRef(player);
  useLayoutEffect(() => { playerRef.current = player; });

  const disposeRoom = useCallback((room) => {
    room.generation += 1;
    roomsRef.current.delete(room.key);
    room.hub.invoke('LeaveParty', room.podId, room.channelId).catch(() => {});
    room.hub.stop().catch(() => {});
  }, []);

  const followParty = useCallback((state) => {
    const previous = followingRef.current;
    const next = state && playerRef.current.playerVisible !== false
      ? roomsRef.current.get(roomKey(state)) : null;
    followingRef.current = next || null;
    if (previous !== next && previous) {
      previous.lastApplied = null;
      if (previous.observers.size === 0) disposeRoom(previous);
    }
    if (!next || next.revoked) {
      followingRef.current = null;
      setFollowingPartyStatus(null);
      setFollowingParty(null);
      return;
    }
    setFollowingPartyStatus(next.value);
    const currentState = next.value.state;
    setFollowingParty(currentState || { podId: next.podId, channelId: next.channelId });
    if (currentState && next.lastApplied !== eventKey(currentState)) {
      next.lastApplied = eventKey(currentState);
      applyPartyState(currentState, playerRef.current);
    }
  }, [disposeRoom, setFollowingParty]);

  const startRoom = useCallback((room) => {
    const generation = ++room.generation;
    const previousHub = room.hub;
    if (previousHub) previousHub.stop().catch(() => {});
    const hub = createListeningPartyHubConnection();
    room.hub = hub;
    room.revoked = false;
    let eventVersion = 0;
    let snapshotVersion = 0;
    const current = () => roomsRef.current.get(room.key) === room && room.generation === generation;
    const update = (changes) => {
      if (!current()) return;
      room.value = { ...room.value, ...changes };
      if (followingRef.current === room) setFollowingPartyStatus(room.value);
      room.observers.forEach((observer) => observer(room.value));
    };
    const receive = (state) => {
      if (!current() || room.revoked) return;
      if (state && ((state.podId && state.podId !== room.podId) || (state.channelId && state.channelId !== room.channelId))) return;
      const normalized = state && state.action !== 'stop' ? { ...state, podId: room.podId, channelId: room.channelId } : null;
      const previousState = room.value.state;
      update({ error: '', state: normalized });
      if (followingRef.current !== room) return;
      if (!normalized) {
        // Following an empty room waits for its first broadcast; an ended
        // broadcast releases ownership and stops its player source.
        if (state?.action === 'stop' || previousState) {
          followParty(null);
          playerRef.current.clear();
        }
      } else {
        setFollowingParty(normalized);
        if (room.lastApplied !== eventKey(normalized)) {
          room.lastApplied = eventKey(normalized);
          applyPartyState(normalized, playerRef.current);
        }
      }
    };
    const refresh = async () => {
      const requestVersion = ++snapshotVersion;
      const liveVersion = eventVersion;
      try {
        const state = await listeningParty.getPartyState(room.podId, room.channelId);
        if (current() && !room.revoked && requestVersion === snapshotVersion && liveVersion === eventVersion) receive(state);
      } catch {
        if (current() && !room.revoked && requestVersion === snapshotVersion && liveVersion === eventVersion) {
          update({ error: 'Room state could not refresh. Retry the connection to catch up.' });
        }
      }
    };
    const join = async (reconnect) => {
      if (!current() || room.revoked) return;
      try {
        await hub.invoke('JoinParty', room.podId, room.channelId);
        if (current() && !room.revoked) {
          update({ connected: true, pending: false });
          await refresh();
        }
      } catch {
        if (current() && !room.revoked) update({ connected: false, error: reconnect
          ? 'Could not rejoin this room. Retry the connection.'
          : 'Listen-along could not connect. Retry when the connection is available.', pending: false });
      }
    };
    hub.on('partyAccessRevoked', () => {
      if (!current()) return;
      room.revoked = true;
      eventVersion += 1;
      snapshotVersion += 1;
      update({ connected: false, error: 'Room access was revoked. Rejoin after your membership is restored.', pending: false, state: null });
      if (followingRef.current === room) {
        followParty(null);
        playerRef.current.clear();
      }
    });
    hub.on('partyState', (state) => {
      if (!current() || room.revoked) return;
      eventVersion += 1;
      receive(state);
    });
    hub.onreconnecting(() => {
      if (!room.revoked) update({ connected: false, error: '', pending: true });
    });
    hub.onreconnected(() => join(true));
    hub.onclose(() => {
      if (!room.revoked) update({ connected: false, error: 'Listen-along connection closed. Retry to rejoin this room.', pending: false });
    });
    update({ connected: false, error: '', pending: true });
    hub.start().then(() => join(false)).catch(() => {
      if (!room.revoked) update({ connected: false, error: 'Listen-along could not connect. Retry when the connection is available.', pending: false });
    });
    refresh();
  }, [followParty, setFollowingParty]);

  const observePartyRoom = useCallback((podId, channelId, observer) => {
    const key = roomKey({ podId, channelId });
    let room = roomsRef.current.get(key);
    if (!room) {
      // The routed UI observes one room alongside at most one followed room.
      if (roomsRef.current.size >= 2) {
        observer({ ...initialRoom, error: 'Close another room before opening listen-along here.', pending: false });
        return () => {};
      }
      room = { channelId, generation: 0, hub: null, key, lastApplied: null, observers: new Set(), podId, revoked: false, value: initialRoom };
      roomsRef.current.set(key, room);
      room.observers.add(observer);
      startRoom(room);
    } else room.observers.add(observer);
    observer(room.value);
    return () => {
      room.observers.delete(observer);
      if (room.observers.size === 0 && followingRef.current !== room && roomsRef.current.get(key) === room) disposeRoom(room);
    };
  }, [disposeRoom, startRoom]);

  const retryPartyRoom = useCallback((podId, channelId) => {
    const room = roomsRef.current.get(roomKey({ podId, channelId }));
    if (room) startRoom(room);
  }, [startRoom]);

  useEffect(() => () => {
    followingRef.current = null;
    [...roomsRef.current.values()].forEach(disposeRoom);
  }, [disposeRoom]);

  return { followParty, followingPartyStatus, observePartyRoom, retryPartyRoom };
}
