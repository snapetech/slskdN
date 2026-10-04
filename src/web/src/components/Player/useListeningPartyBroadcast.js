// <copyright file="useListeningPartyBroadcast.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as listeningParty from '../../lib/listeningParty';
import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';

const shareable = (current) => current?.contentId && !current.contentId.startsWith('local:') && !current.radioPartyId;
const sameRoom = (a, b) => a?.podId === b?.podId && a?.channelId === b?.channelId;
const HOST_SESSION_RENEWAL_INTERVAL_MS = 5 * 60 * 1000;
const createHostSessionId = () => {
  const bytes = new Uint8Array(16);
  globalThis.crypto.getRandomValues(bytes);
  return [...bytes].map((value) => value.toString(16).padStart(2, '0')).join('');
};
const publicationError = (error) => error?.response?.status === 429
  ? 'Room updates are at capacity. Retry later.'
  : error?.response?.status === 409 && error?.response?.data?.code === 'host_session_replaced'
    ? 'Another browser replaced this host session. Start a new broadcast to host again.'
    : error?.response?.status === 409 && error?.response?.data?.code === 'host_session_expired'
      ? 'This host session expired. Start a new broadcast to host again.'
      : error?.response?.status === 409 && error?.response?.data?.code === 'party_id_in_use'
        ? 'This Party ID is already in use. Start the broadcast again to claim a new ID.'
  : error?.response?.status === 404
    ? 'This room is unavailable. Choose an existing room.'
    : error?.response?.status === 403
      ? 'Room access was revoked. Rejoin before broadcasting.'
      : error?.response?.data?.code === 'room_storage_unavailable'
        ? 'The room update could not be saved. Try again.'
        : error?.response?.data?.code === 'party_directory_unavailable'
          ? 'Party ID ownership could not be checked. Try again.'
        : 'Room broadcast updates failed. Retry or stop the broadcast.';

// An explicitly started host session survives route changes. One active request
// and one coalesced update bound publication work; see ADR-0019. One five-minute
// timer renews its lease without sending periodic position updates.
// Requested and confirmed sharing settings remain separate; see ADR-0021.
export default function useListeningPartyBroadcast(player) {
  const playerRef = useRef(player);
  const sessionRef = useRef(null);
  const lastSentRef = useRef(null);
  const mountedRef = useRef(true);
  const [broadcastStatus, setBroadcastStatus] = useState(null);
  useLayoutEffect(() => { playerRef.current = player; });

  const status = useCallback((session, changes = {}) => {
    if (mountedRef.current && sessionRef.current === session) {
      const confirmed = session.confirmedConfig;
      setBroadcastStatus({ ...session.config, ...confirmed, active: true, error: '', pending: session.inFlight,
        confirmedSettings: Boolean(confirmed),
        requestedGlobalRadio: session.config.globalRadio, requestedMeshStreaming: session.config.meshStreaming,
        settingsPending: session.inFlight && !session.stopping && (!confirmed || confirmed.globalRadio !== session.config.globalRadio ||
          confirmed.meshStreaming !== session.config.meshStreaming), stopping: session.stopping,
        ...changes });
    }
  }, []);
  const release = useCallback((session, error = '') => {
    if (sessionRef.current !== session) return;
    sessionRef.current = null;
    if (session.renewalTimer !== null) {
      window.clearTimeout(session.renewalTimer);
      session.renewalTimer = null;
    }
    session.abort.abort();
    session.pending?.resolve(null);
    session.pending = null;
    session.releaseRoom?.();
    if (mountedRef.current) setBroadcastStatus(error ? { ...session.config, ...session.confirmedConfig, active: false, error, pending: false } : null);
  }, []);

  const scheduleRenewal = useCallback((session) => {
    if (sessionRef.current !== session || !session.hostSessionId || !session.partyId || session.stopping) return;
    if (session.renewalTimer !== null) window.clearTimeout(session.renewalTimer);
    session.renewalTimer = window.setTimeout(async () => {
      session.renewalTimer = null;
      try {
        session.renewalRequest = listeningParty.renewHostSession(session.config.podId, session.config.channelId, session.partyId,
          { signal: session.abort.signal, hostSessionId: session.hostSessionId });
        await session.renewalRequest;
      } catch (error) {
        if (sessionRef.current !== session) return;
        session.error = publicationError(error);
        if ([403, 404].includes(error?.response?.status) ||
            (error?.response?.status === 409 && !session.stopping)) release(session, session.error);
        else status(session, { error: session.error });
        return;
      } finally {
        session.renewalRequest = null;
      }

      if (sessionRef.current === session) {
        session.error = '';
        status(session);
        session.scheduleRenewal?.();
      }
    }, HOST_SESSION_RENEWAL_INTERVAL_MS);
  }, [release, status]);

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
        { ...update.payload, partyId: session.startPending ? '' : session.partyId,
          clientPositionObservedAtUnixMs: update.positionObservedAtUnixMs }, {
          signal: session.abort.signal,
          hostSessionId: session.hostSessionId,
          startHostSession: session.startPending,
        });
      if (sessionRef.current !== session) { update.resolve(null); return; }
      session.partyId = state?.partyId || session.partyId;
      session.startPending = false;
      session.lastState = state;
      session.confirmedConfig = { globalRadio: state?.listed ?? update.payload.listed,
        meshStreaming: state?.allowMeshStreaming ?? update.payload.allowMeshStreaming };
      session.error = '';
      update.resolve(state);
      scheduleRenewal(session);
      if (update.payload.action === 'stop') release(session);
    } catch (error) {
      update.reject(error);
      if (sessionRef.current !== session) return;
      session.error = publicationError(error);
      session.pending?.reject(error);
      session.pending = null;
      session.stopping = false;
      if ([403, 404, 409].includes(error?.response?.status)) release(session, session.error);
    } finally {
      session.inFlight = false;
      if (sessionRef.current === session) {
        status(session, { error: session.error });
        const pending = session.pending;
        session.pending = null;
        if (pending) send(session, pending);
      }
    }
  }, [release, scheduleRenewal, status]);

  const queue = useCallback((session, payload) => {
    session.lastDesired = payload;
    return new Promise((resolve, reject) => {
      const update = {
        payload,
        positionObservedAtUnixMs: payload.action === 'play' || payload.action === 'seek' ? Date.now() : 0,
        reject,
        resolve,
      };
      if (session.inFlight) {
        session.pending?.resolve(null);
        session.pending = update;
        status(session, { error: session.error });
      } else send(session, update);
    });
  }, [send, status]);
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
    status(session);
    if (session.renewalTimer !== null) {
      window.clearTimeout(session.renewalTimer);
      session.renewalTimer = null;
    }
    return (async () => {
      const renewalRequest = session.renewalRequest;
      if (renewalRequest) {
        try {
          await renewalRequest;
        } catch {
          // The ordered Stop request below reports whether host ownership remains valid.
        }
      }

      if (sessionRef.current !== session) return null;
      return queue(session, payload(session, 'stop'));
    })();
  }, [payload, queue, status]);

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
        hostSessionId: action === 'stop' ? null : createHostSessionId(), lastEvent: null, lastState: null,
        partyId: action === 'stop' ? config.partyId || '' : '', pending: null,
        renewalTimer: null, scheduleRenewal: null, startPending: action !== 'stop', stopping: false };
      if (action !== 'stop') session.scheduleRenewal = () => scheduleRenewal(session);
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
  useEffect(() => {
    const synchronizeWhenVisible = () => {
      if (document.visibilityState !== 'visible') return;
      const session = sessionRef.current;
      const playback = playerRef.current;
      if (!session || session.stopping || session.error || !playback.audioElement || !shareable(playback.current)) return;

      const action = playback.audioElement.paused ? 'pause' : 'play';
      queue(session, payload(session, action, playback.getPlaybackPosition())).catch(() => {});
    };

    document.addEventListener('visibilitychange', synchronizeWhenVisible);
    return () => document.removeEventListener('visibilitychange', synchronizeWhenVisible);
  }, [payload, queue]);
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
  useEffect(() => {
    const stopOnPageHide = () => {
      const session = sessionRef.current;
      if (!session) return;

      if (session.renewalTimer !== null) {
        window.clearTimeout(session.renewalTimer);
        session.renewalTimer = null;
      }

      if (session.hostSessionId && session.partyId && session.lastState) {
        const stopEvent = {
          ...session.lastState,
          action: 'stop',
          positionSeconds: 0,
          listed: false,
          allowMeshStreaming: false,
          album: '',
          artist: '',
          contentId: '',
          title: '',
        };
        try {
          listeningParty.stopPartyStateOnPageHide(
            session.config.podId,
            session.config.channelId,
            stopEvent,
            session.hostSessionId,
          ).catch(() => {});
        } catch {
          // The server lease expiry remains the fallback if keepalive is unavailable.
        }
      }

      sessionRef.current = null;
      session.pending?.resolve(null);
      session.pending = null;
      session.abort.abort();
      session.releaseRoom?.();
      if (mountedRef.current) setBroadcastStatus(null);
    };

    window.addEventListener('pagehide', stopOnPageHide);
    return () => window.removeEventListener('pagehide', stopOnPageHide);
  }, []);
  return { broadcastStatus, publishBroadcast, reportPlaybackEvent, retryBroadcast, stopBroadcast };
}
