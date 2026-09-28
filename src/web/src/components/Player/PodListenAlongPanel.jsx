import { createListeningPartyHubConnection } from '../../lib/hubFactory';
import * as listeningParty from '../../lib/listeningParty';
import { usePlayer } from './PlayerContext';
import React, { useCallback, useEffect, useRef, useState } from 'react';
import { Button, Checkbox, Icon, Label, List, Popup, Segment } from 'semantic-ui-react';

const DIRECTORY_POLL_INTERVAL_MS = 60_000;
const partyEventKey = (state) => JSON.stringify([
  state.podId,
  state.channelId,
  state.partyId,
  state.sequence,
  state.serverTimeUnixMs,
  state.action,
  state.contentId,
  state.positionSeconds,
  state.streamUrl,
]);

const sameDirectory = (previous, next) =>
  previous.length === next.length &&
  previous.every(
    (party, index) =>
      party.partyId === next[index]?.partyId &&
      party.podId === next[index]?.podId &&
      party.channelId === next[index]?.channelId &&
      party.hostPeerId === next[index]?.hostPeerId &&
      party.title === next[index]?.title &&
      party.artist === next[index]?.artist &&
      party.album === next[index]?.album &&
      party.contentId === next[index]?.contentId &&
      party.action === next[index]?.action &&
      party.positionSeconds === next[index]?.positionSeconds &&
      party.startedAtUnixMs === next[index]?.startedAtUnixMs &&
      party.allowMeshStreaming === next[index]?.allowMeshStreaming &&
      party.streamPath === next[index]?.streamPath &&
      party.transportUsername === next[index]?.transportUsername &&
      party.streamTicket === next[index]?.streamTicket,
  );

const applyPartyState = (state, player) => {
  if (!state) return;

  if (state.action === 'play' || state.action === 'seek') {
    const elapsed =
      state.action === 'play' && Number.isFinite(state.serverTimeUnixMs) &&
      state.serverTimeUnixMs > 0
        ? Math.max(0, (Date.now() - state.serverTimeUnixMs) / 1000)
        : 0;
    player.playItem(
      {
        album: state.album,
        artist: state.artist || state.hostPeerId,
        contentId: state.contentId,
        streamUrl: state.streamUrl,
        title: state.title || state.contentId,
      },
      {
        fromParty: true,
        positionSeconds: (state.positionSeconds || 0) + elapsed,
        replaceQueue: true,
      },
    );
  } else if (state.action === 'pause') {
    player.pause();
    const positionSeconds = Number.isFinite(state.positionSeconds)
      ? Math.max(0, state.positionSeconds)
      : 0;
    if (player.current?.contentId !== state.contentId ||
        Math.abs(player.getPlaybackPosition() - positionSeconds) > 2) {
      player.playItem(
        {
          album: state.album,
          artist: state.artist || state.hostPeerId,
          contentId: state.contentId,
          streamUrl: state.streamUrl,
          title: state.title || state.contentId,
        },
        {
          fromParty: true,
          positionSeconds,
          replaceQueue: true,
          startPaused: true,
        },
      );
    }
  } else if (state.action === 'stop') {
    player.clear();
  }
};

const PodListenAlongPanel = ({ channelId, compact = false, podId, user }) => {
  const player = usePlayer();
  const canBroadcastCurrent = Boolean(
    player.current?.contentId && !player.current.contentId.startsWith('local:'),
  );
  const [connected, setConnected] = useState(false);
  const [connectionPending, setConnectionPending] = useState(false);
  const [connectionError, setConnectionError] = useState('');
  const [connectionAttempt, setConnectionAttempt] = useState(0);
  const [directory, setDirectory] = useState([]);
  const [following, setFollowing] = useState(false);
  const [globalRadio, setGlobalRadio] = useState(false);
  const [meshStreaming, setMeshStreaming] = useState(false);
  const [partyState, setPartyState] = useState(null);
  const [publishError, setPublishError] = useState('');
  const directoryFetchInFlightRef = useRef(false);
  const mountedRef = useRef(false);
  const publishedPartyIdsRef = useRef(new Map());
  const publishChainRef = useRef(Promise.resolve());
  const publishRequestRef = useRef(0);
  const followingRef = useRef(false);
  const lastAppliedPartyRef = useRef(null);
  const followedPartyRef = useRef(player.followingParty);
  const playerRef = useRef(player);

  useEffect(() => {
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
    };
  }, []);

  useEffect(() => {
    followingRef.current = following;
  }, [following]);

  useEffect(() => {
    if (followedPartyRef.current && !player.followingParty && followingRef.current) {
      followingRef.current = false;
      lastAppliedPartyRef.current = null;
      setFollowing(false);
    }
    followedPartyRef.current = player.followingParty;
  }, [player.followingParty]);

  useEffect(() => {
    if (player.playerVisible === false && followingRef.current) {
      followingRef.current = false;
      lastAppliedPartyRef.current = null;
      setFollowing(false);
      player.followParty(null);
    }
  }, [player.followParty, player.playerVisible]);

  useEffect(() => {
    playerRef.current = player;
  }, [player]);

  useEffect(() => {
    if (!podId || !channelId) return undefined;

    publishRequestRef.current += 1;
    lastAppliedPartyRef.current = null;
    setConnected(false);
    setConnectionPending(true);
    setConnectionError('');
    setPartyState(null);
    setPublishError('');
    let disposed = false;
    let accessRevoked = false;
    let revocationVersion = 0;
    let hubVersion = 0;
    let snapshotVersion = 0;
    const hub = createListeningPartyHubConnection();

    const receiveState = (state) => {
      setConnectionError('');
      setPartyState(state?.action === 'stop' ? null : state);
      if (followingRef.current) {
        if (!state || state.action === 'stop') {
          setFollowing(false);
          followingRef.current = false;
          lastAppliedPartyRef.current = null;
          playerRef.current.followParty(null);
          playerRef.current.clear();
        } else if (lastAppliedPartyRef.current !== partyEventKey(state)) {
          lastAppliedPartyRef.current = partyEventKey(state);
          playerRef.current.followParty(state);
          applyPartyState(state, playerRef.current);
        }
      }
    };
    const refreshState = async () => {
      const requestVersion = ++snapshotVersion;
      const eventVersion = hubVersion;
      try {
        const state = await listeningParty.getPartyState(podId, channelId);
        if (!disposed && !accessRevoked && requestVersion === snapshotVersion && eventVersion === hubVersion) {
          receiveState(state);
        }
      } catch {
        if (!disposed && !accessRevoked && requestVersion === snapshotVersion && eventVersion === hubVersion) {
          setConnectionError('Room state could not refresh. Retry the connection to catch up.');
        }
      }
    };

    hub.on('partyAccessRevoked', () => {
      if (disposed) return;
      accessRevoked = true;
      revocationVersion += 1;
      hubVersion += 1;
      snapshotVersion += 1;
      followingRef.current = false;
      lastAppliedPartyRef.current = null;
      setFollowing(false);
      setPartyState(null);
      setConnected(false);
      setConnectionPending(false);
      setConnectionError('Room access was revoked. Rejoin after your membership is restored.');
      playerRef.current.followParty(null);
    });
    hub.on('partyState', (state) => {
      if (disposed || accessRevoked) return;
      hubVersion += 1;
      receiveState(state);
    });
    hub.onreconnecting(() => {
      if (!disposed) {
        setConnected(false);
        setConnectionPending(true);
        if (!accessRevoked) setConnectionError('');
      }
    });
    hub.onreconnected(async () => {
      if (disposed) return;
      try {
        const version = revocationVersion;
        await hub.invoke('JoinParty', podId, channelId);
        if (!disposed && version === revocationVersion) {
          accessRevoked = false;
          setConnected(true);
          setConnectionPending(false);
          refreshState();
        }
      } catch {
        if (!disposed) {
          setConnected(false);
          setConnectionPending(false);
          if (!accessRevoked) setConnectionError('Could not rejoin this room. Retry the connection.');
        }
      }
    });
    hub.onclose(() => {
      if (!disposed) {
        setConnected(false);
        setConnectionPending(false);
        if (!accessRevoked) setConnectionError('Listen-along connection closed. Retry to rejoin this room.');
      }
    });

    hub
      .start()
      .then(async () => {
        if (disposed) return;
        const version = revocationVersion;
        await hub.invoke('JoinParty', podId, channelId);
        if (!disposed && version === revocationVersion) {
          accessRevoked = false;
          setConnected(true);
          setConnectionPending(false);
          refreshState();
        }
      })
      .catch(() => {
        if (!disposed) {
          setConnected(false);
          setConnectionPending(false);
          if (!accessRevoked) setConnectionError('Listen-along could not connect. Retry when the connection is available.');
        }
      });

    refreshState();

    return () => {
      disposed = true;
      hub.invoke('LeaveParty', podId, channelId).catch(() => {});
      hub.stop().catch(() => {});
    };
  }, [channelId, connectionAttempt, podId]);

  const refreshDirectory = useCallback(async () => {
    if (compact || document.hidden || directoryFetchInFlightRef.current) return;

    directoryFetchInFlightRef.current = true;
    try {
      const next = await listeningParty.getPartyDirectory();
      if (!mountedRef.current || document.hidden) return;
      setDirectory((previous) =>
        sameDirectory(previous, next) ? previous : next,
      );
    } catch {
      // Retain the last successful directory during transient failures.
    } finally {
      directoryFetchInFlightRef.current = false;
    }
  }, [compact]);

  useEffect(() => {
    if (compact) return undefined;

    let interval = null;
    const stopPolling = () => {
      if (interval) {
        window.clearInterval(interval);
        interval = null;
      }
    };
    const startPolling = () => {
      if (document.hidden || interval) return;
      refreshDirectory();
      interval = window.setInterval(
        refreshDirectory,
        DIRECTORY_POLL_INTERVAL_MS,
      );
    };
    const handleVisibilityChange = () => {
      if (document.hidden) {
        stopPolling();
      } else {
        startPolling();
      }
    };

    startPolling();
    document.addEventListener('visibilitychange', handleVisibilityChange);
    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      stopPolling();
    };
  }, [compact, refreshDirectory]);

  const publish = (action) => {
    const current = player.current;
    if (action !== 'stop' && !canBroadcastCurrent) return;

    const requestId = ++publishRequestRef.current;
    const roomKey = JSON.stringify([podId, channelId]);
    const existingPartyId = publishedPartyIdsRef.current.get(roomKey) ||
      (partyState?.podId === podId && partyState?.channelId === channelId
        ? partyState.partyId
        : '');
    const payload = {
      action,
      album: current?.album || '',
      allowMeshStreaming: meshStreaming,
      artist: current?.artist || user || '',
      contentId: current?.contentId || '',
      hostPeerId: user || 'local-peer',
      listed: globalRadio,
      partyId: existingPartyId,
      positionSeconds: action === 'stop' ? 0 : player.getPlaybackPosition(),
      title: current?.title || current?.fileName || '',
    };
    setPublishError('');
    const request = publishChainRef.current.then(async () => {
      const state = await listeningParty.publishPartyState(podId, channelId, {
        ...payload,
        partyId: action === 'stop'
          ? publishedPartyIdsRef.current.get(roomKey) || payload.partyId
          : payload.partyId,
      });
      if (action === 'stop') publishedPartyIdsRef.current.delete(roomKey);
      else publishedPartyIdsRef.current.set(roomKey, state?.partyId || payload.partyId);
      return state;
    });
    publishChainRef.current = request.catch(() => {});
    request.then((state) => {
      if (!mountedRef.current || requestId !== publishRequestRef.current) return;
      setPartyState(action === 'stop' ? null : state);
      if (!compact) refreshDirectory();
    }).catch((error) => {
      if (mountedRef.current && requestId === publishRequestRef.current) {
        setPublishError(error?.response?.status === 429
          ? 'Room updates are at capacity. Retry later.'
          : action === 'stop'
          ? 'Could not stop the room broadcast. Try again.'
          : 'Could not start the room broadcast. Try again.');
      }
    });
  };

  const joinListedParty = (party) => {
    const streamUrl = listeningParty.buildRadioStreamUrl(party);
    const elapsed = party.action === 'play' && Number.isFinite(party.startedAtUnixMs) &&
      party.startedAtUnixMs > 0
      ? Math.max(0, (Date.now() - party.startedAtUnixMs) / 1000)
      : 0;
    const positionSeconds = Number.isFinite(party.positionSeconds)
      ? Math.max(0, party.positionSeconds)
      : 0;
    setFollowing(false);
    followingRef.current = false;
    lastAppliedPartyRef.current = null;
    player.followParty(null);
    player.playItem(
      {
        album: party.album,
        artist: party.artist || party.hostPeerId,
        contentId: party.contentId,
        streamUrl,
        radioPartyId: party.transportUsername && party.streamTicket ? party.partyId : null,
        title: party.title || party.contentId,
      },
      {
        replaceQueue: true,
        positionSeconds: positionSeconds + elapsed,
        streamUrl,
        startPaused: party.action === 'pause',
      },
    );
  };

  const retryConnection = !connected || connectionError ? (
    <Popup
      content="Reconnect to this room and refresh the host's current state after a connection or refresh failure."
      trigger={
        <Button
          aria-label="Retry listen-along connection"
          disabled={connectionPending || !podId || !channelId}
          icon="refresh"
          loading={connectionPending}
          onClick={() => setConnectionAttempt((attempt) => attempt + 1)}
          size="mini"
        />
      }
    />
  ) : null;

  if (compact) {
    return (
      <Segment className="pod-listen-along pod-listen-along-compact">
        <div className="pod-listen-along-compact-status">
          <Popup
            content={
              partyState
                ? `${partyState.hostPeerId} ${partyState.action}: ${partyState.title || partyState.contentId}`
                : 'No active room broadcast'
            }
            trigger={
              <span
                aria-label={connected ? 'Listen Along live' : connectionPending ? 'Listen Along connecting' : 'Listen Along offline'}
                className={`pod-listen-along-orb ${connected ? 'pod-listen-along-orb-live' : ''}`}
                role="status"
                title={connected ? 'Listen Along live' : connectionPending ? 'Listen Along connecting' : 'Listen Along offline'}
              />
            }
          />
          <span className="pod-listen-along-compact-copy">
            {partyState ? partyState.title || partyState.contentId : 'Room broadcast'}
          </span>
        </div>
        <div className="pod-listen-along-compact-actions">
          {retryConnection}
          <Popup
            content={player.playerVisible === false
              ? 'Show the browser player before following this room.'
              : "Follow this room's broadcast using your own stream access."}
            trigger={
              <Button
                active={following}
                aria-label="Follow room broadcast"
                disabled={player.playerVisible === false}
                icon
                onClick={() => {
                  const next = !following;
                  setFollowing(next);
                  followingRef.current = next;
                  if (next && partyState) {
                    lastAppliedPartyRef.current = partyEventKey(partyState);
                    player.followParty(partyState);
                    applyPartyState(partyState, player);
                  } else {
                    lastAppliedPartyRef.current = null;
                    player.followParty(null);
                  }
                }}
                size="mini"
                title="Follow room broadcast"
              >
                <Icon name={following ? 'volume up' : 'volume off'} />
              </Button>
            }
          />
          <Popup
            content="Broadcast the current server-backed track to this room. Browser-only files cannot be streamed to other listeners."
            trigger={
              <Button
                aria-label="Broadcast current track to room"
                disabled={!canBroadcastCurrent}
                icon
                onClick={() => publish('play')}
                size="mini"
                title="Broadcast current track to room"
              >
                <Icon name="bullhorn" />
              </Button>
            }
          />
          <Popup
            content="List this room broadcast in the mesh radio directory."
            trigger={
              <Button
                active={globalRadio}
                aria-label="List room broadcast in mesh directory"
                icon
                onClick={() => setGlobalRadio((value) => !value)}
                size="mini"
                title="List room broadcast in mesh directory"
              >
                <Icon name="broadcast tower" />
              </Button>
            }
          />
          <Popup
            content="Allow directory listeners to stream the current track from this node."
            trigger={
              <Button
                active={meshStreaming}
                aria-label="Allow mesh streaming for broadcast"
                disabled={!globalRadio}
                icon
                onClick={() => setMeshStreaming((value) => !value)}
                size="mini"
                title="Allow mesh streaming for broadcast"
              >
                <Icon name="wifi" />
              </Button>
            }
          />
          <Popup
            content="Stop broadcasting listen-along metadata for this room."
            trigger={
              <Button
                aria-label="Stop room broadcast"
                icon
                onClick={() => publish('stop')}
                size="mini"
                title="Stop room broadcast"
              >
                <Icon name="stop" />
              </Button>
            }
          />
        </div>
        {connectionError ? <div className="pod-listen-along-error" role="alert">{connectionError}</div> : null}
        {publishError ? <div className="pod-listen-along-error" role="alert">{publishError}</div> : null}
      </Segment>
    );
  }

  return (
    <Segment className="pod-listen-along">
      <div className="pod-listen-along-main">
        <div>
          <strong>Listen Along</strong>
          <div className="pod-listen-along-state">
            {partyState
              ? `${partyState.hostPeerId} ${partyState.action}: ${partyState.title || partyState.contentId}`
              : 'No active party'}
          </div>
        </div>
        <Label color={connected ? 'green' : 'grey'}>
          {connected ? 'Live' : connectionPending ? 'Connecting' : 'Offline'}
        </Label>
        {retryConnection}
      </div>
      <div className="pod-listen-along-toggles">
        <Popup
          content="List this party in the slskdN mesh radio directory so nearby mesh members can discover it."
          trigger={
            <Checkbox
              checked={globalRadio}
              label="List globally"
              onChange={(event, data) => setGlobalRadio(data.checked)}
              toggle
            />
          }
        />
        <Popup
          content="Allow listeners who join from the directory to stream this party's current track from this slskdN node."
          trigger={
            <Checkbox
              checked={meshStreaming}
              disabled={!globalRadio}
              label="Mesh streaming"
              onChange={(event, data) => setMeshStreaming(data.checked)}
              toggle
            />
          }
        />
      </div>
      <Button.Group size="small">
        <Popup
          content={player.playerVisible === false
            ? 'Show the browser player before following this pod.'
            : "Follow this pod's host playback using your own stream access."}
          trigger={
            <Button
              active={following}
              aria-label="Follow pod broadcast"
              disabled={player.playerVisible === false}
              icon
              onClick={() => {
                const next = !following;
                setFollowing(next);
                followingRef.current = next;
                if (next && partyState) {
                  lastAppliedPartyRef.current = partyEventKey(partyState);
                  player.followParty(partyState);
                  applyPartyState(partyState, player);
                } else {
                  lastAppliedPartyRef.current = null;
                  player.followParty(null);
                }
              }}
            >
              <Icon name={following ? 'volume up' : 'volume off'} />
            </Button>
          }
        />
        <Popup
          content="Publish the current server-backed track as the pod listen-along host. Browser-only files cannot be streamed to other listeners."
          trigger={
            <Button
              aria-label="Broadcast current track to pod"
              disabled={!canBroadcastCurrent}
              icon
              onClick={() => publish('play')}
            >
              <Icon name="bullhorn" />
            </Button>
          }
        />
        <Popup
          content="Stop hosting listen-along metadata for this pod."
          trigger={
            <Button
              aria-label="Stop pod broadcast"
              icon
              onClick={() => publish('stop')}
            >
              <Icon name="stop" />
            </Button>
          }
        />
      </Button.Group>
      {connectionError ? <div className="pod-listen-along-error" role="alert">{connectionError}</div> : null}
      {publishError ? <div className="pod-listen-along-error" role="alert">{publishError}</div> : null}
      {directory.length > 0 && (
        <div className="pod-listen-along-directory">
          <strong>Listed radio</strong>
          <List divided relaxed>
            {directory.slice(0, 6).map((party) => (
              <List.Item key={party.partyId}>
                <List.Content floated="right">
                  <Popup
                    content="Play this listed radio snapshot through the host's stream endpoint. Rejoin for later track changes."
                    trigger={
                      <Button
                        aria-label={`Play ${party.title || party.contentId} from listed radio`}
                        disabled={!party.allowMeshStreaming || !party.transportUsername || !party.streamTicket}
                        icon
                        onClick={() => joinListedParty(party)}
                        size="mini"
                      >
                        <Icon name="play" />
                      </Button>
                    }
                  />
                </List.Content>
                <List.Content>
                  <List.Header>{party.title || party.contentId}</List.Header>
                  <List.Description>
                    {party.hostPeerId}
                    {party.allowMeshStreaming ? ' | streamable' : ' | metadata only'}
                  </List.Description>
                </List.Content>
              </List.Item>
            ))}
          </List>
        </div>
      )}
    </Segment>
  );
};

export default PodListenAlongPanel;
