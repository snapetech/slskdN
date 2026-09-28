// <copyright file="useListeningPartyBroadcast.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as listeningParty from '../../lib/listeningParty';
import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';

const shareable = (current) => current?.contentId && !current.contentId.startsWith('local:') && !current.radioPartyId;
const sameRoom = (a, b) => a?.podId === b?.podId && a?.channelId === b?.channelId;
const publicationError = (error) => error?.response?.status === 429
  ? 'Room updates are at capacity. Retry later.'
  : error?.response?.status === 404
    ? 'This room is unavailable. Choose an existing room.'
    : error?.response?.status === 403
      ? 'Room access was revoked. Rejoin before broadcasting.'
      : error?.response?.data?.code === 'room_storage_unavailable'
        ? 'The room update could not be saved. Try again.'
        : 'Room broadcast updates failed. Retry or stop the broadcast.';

// An explicitly started host session survives route changes. One active request
// and one coalesced update bound publication work; see ADR-0019.
export default function useListeningPartyBroadcast(player) {
  const playerRef = useRef(player);
  const sessionRef = useRef(null);
  const lastSentRef = useRef(null);
  const mountedRef = useRef(true);
  const [broadcastStatus, setBroadcastStatus] = useState(null);
  useLayoutEffect(() => { playerRef.current = player; });

  const status = useCallback((session, changes = {}) => {
    if (mountedRef.current && sessionRef.current === session) {
      setBroadcastStatus({ ...session.config, active: true, error: '', pending: session.inFlight,
        ...changes });
    }
  }, []);
  const release = useCallback((session, error = '') => {
    if (sessionRef.current !== session) return;
    sessionRef.current = null;
    session.abort.abort();
    session.pending?.resolve(null);
    session.pending = null;
    session.releaseRoom?.();
    if (mountedRef.current) setBroadcastStatus(error ? { ...session.config, active: false, error, pending: false } : null);
  }, []);

  const send = useCallback(async function send(session, update) {
    session.inFlight = true;
    session.lastDesired = update.payload;
    status(session);
    try {
      session.abort.signal.throwIfAborted();
      const remaining = lastSentRef.current === null ? 0 : 250 - (performance.now() - lastSentRef.current);
      if (remaining > 0) {
        await new Promise((resolve, reject) => {
          const abort = () => {
            window.clearTimeout(timer);
            reject(new DOMException('Broadcast ended', 'AbortError'));
          };
          const timer = window.setTimeout(() => {
            session.abort.signal.removeEventListener('abort', abort);
            resolve();
          }, remaining);
          session.abort.signal.addEventListener('abort', abort, { once: true });
        });
      }
      if (update.payload.action === 'stop' && !session.partyId) {
        const existing = await listeningParty.getPartyState(session.config.podId, session.config.channelId,
          { signal: session.abort.signal });
        session.partyId = existing?.partyId || '';
      }
      session.abort.signal.throwIfAborted();
      lastSentRef.current = performance.now();
      const state = await listeningParty.publishPartyState(session.config.podId, session.config.channelId,
        { ...update.payload, partyId: session.partyId }, { signal: session.abort.signal });
      if (sessionRef.current !== session) { update.resolve(null); return; }
      session.partyId = state?.partyId || session.partyId;
      session.lastState = state;
      session.error = '';
      update.resolve(state);
      if (update.payload.action === 'stop') release(session);
    } catch (error) {
      update.reject(error);
      if (sessionRef.current !== session) return;
      session.error = publicationError(error);
      session.pending?.reject(error);
      session.pending = null;
      session.stopping = false;
      if ([403, 404].includes(error?.response?.status)) release(session, session.error);
    } finally {
      session.inFlight = false;
      if (sessionRef.current === session) {
        status(session, { error: session.error });
        const pending = session.pending;
        session.pending = null;
        if (pending) send(session, pending);
      }
    }
  }, [release, status]);

  const queue = useCallback((session, payload) => {
    session.lastDesired = payload;
    return new Promise((resolve, reject) => {
      const update = { payload, reject, resolve };
      if (session.inFlight) {
        session.pending?.resolve(null);
        session.pending = update;
      } else send(session, update);
    });
  }, [send]);
  const payload = useCallback((session, action, position) => {
    const current = playerRef.current.current;
    return { action, album: current?.album || '', artist: current?.artist || session.config.user || '',
      allowMeshStreaming: session.config.meshStreaming, contentId: current?.contentId || '',
      hostPeerId: session.config.user || 'local-peer', listed: session.config.globalRadio,
      positionSeconds: action === 'stop' ? 0 : position ?? playerRef.current.getPlaybackPosition(),
      title: current?.title || current?.fileName || '' };
  }, []);
  const stopBroadcast = useCallback(() => {
    const session = sessionRef.current;
    if (!session) return Promise.resolve(null);
    session.stopping = true;
    return queue(session, payload(session, 'stop'));
  }, [payload, queue]);

  const publishBroadcast = useCallback((config, action) => {
    let session = sessionRef.current;
    if (session && !sameRoom(session.config, config)) {
      return Promise.reject(new Error('Stop the active broadcast before starting another room.'));
    }
    if (session?.stopping && action !== 'stop') {
      return Promise.reject(new Error('Wait for the room broadcast to stop before starting again.'));
    }
    const current = playerRef.current.current;
    if (action !== 'stop' && (!shareable(current) || playerRef.current.playerVisible === false)) {
      return Promise.reject(new Error('Choose a server-backed track before broadcasting.'));
    }
    if (!session) {
      if (action !== 'stop') playerRef.current.followParty(null);
      session = { abort: new AbortController(), config, error: '', inFlight: false,
        lastEvent: null, lastState: null, partyId: config.partyId || '', pending: null, stopping: false };
      sessionRef.current = session;
      if (action !== 'stop') {
        session.releaseRoom = playerRef.current.observePartyRoom(config.podId, config.channelId, (room) => {
          if (sessionRef.current !== session) return;
          if (room.error?.includes('revoked') || (room.error && room.error.includes('Close another room'))) {
            release(session, room.error);
          } else if (session.lastState && room.connected && !room.pending && !room.state && !session.stopping) {
            release(session, 'The room broadcast ended. Start a new broadcast to host again.');
          } else if (session.lastState && room.state &&
              (room.state.partyId !== session.partyId || room.state.hostPeerId !== session.lastState.hostPeerId)) {
            release(session, 'Another broadcast replaced this host session.');
          }
        });
        if (sessionRef.current !== session) {
          session.releaseRoom();
          return Promise.reject(new Error('This room is unavailable for broadcasting.'));
        }
      }
    }
    session.config = config;
    session.error = '';
    session.stopping = action === 'stop';
    const actualAction = action === 'play' && playerRef.current.audioElement?.paused ? 'pause' : action;
    return queue(session, payload(session, actualAction));
  }, [payload, queue, release]);

  const reportPlaybackEvent = useCallback((action, position) => {
    const session = sessionRef.current;
    if (!session || session.stopping || session.error) return;
    const current = playerRef.current.current;
    if (!shareable(current)) {
      stopBroadcast().catch(() => {});
      return;
    }
    const key = JSON.stringify([current.contentId, current.streamUrl, action, action === 'pause' ? position : null]);
    // Position ticks never arrive here. Seeks may repeat with new positions;
    // duplicate play/pause events from remounts need no additional publication.
    if (action !== 'seek' && key === session.lastEvent) return;
    session.lastEvent = key;
    return queue(session, payload(session, action, position)).catch(() => {});
  }, [payload, queue, stopBroadcast]);
  const retryBroadcast = useCallback(() => {
    const session = sessionRef.current;
    if (!session) return Promise.resolve(null);
    session.error = '';
    return session.lastDesired?.action === 'stop' ? stopBroadcast()
      : queue(session, payload(session, playerRef.current.audioElement?.paused ? 'pause' : 'play'));
  }, [payload, queue, stopBroadcast]);

  useEffect(() => {
    const session = sessionRef.current;
    if (session && (!shareable(player.current) || player.playerVisible === false)) {
      stopBroadcast().catch(() => {});
    }
  }, [player.current, player.playerVisible, stopBroadcast]);
  useEffect(() => {
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
      const session = sessionRef.current;
      if (session) {
        session.abort.abort();
        release(session);
      }
    };
  }, [release]);
  return { broadcastStatus, publishBroadcast, reportPlaybackEvent, retryBroadcast, stopBroadcast };
}
