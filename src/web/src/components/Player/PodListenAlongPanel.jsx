import * as listeningParty from '../../lib/listeningParty';
import { usePlayer } from './PlayerContext';
import React, { useCallback, useEffect, useRef, useState } from 'react';
import { Button, Checkbox, Icon, Label, List, Popup, Segment } from 'semantic-ui-react';

const DIRECTORY_POLL_INTERVAL_MS = 60_000;
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

const PodListenAlongPanel = ({ channelId, compact = false, podId, user }) => {
  const player = usePlayer();
  const canBroadcastCurrent = Boolean(
    player.current?.contentId && !player.current.contentId.startsWith('local:') && !player.current.radioPartyId,
  );
  const [room, setRoom] = useState({ connected: false, error: '', pending: true, state: null });
  const { connected, error: connectionError, pending: connectionPending, state: roomState } = room;
  const [directory, setDirectory] = useState([]);
  const following = Boolean(player.followingParty &&
    player.followingParty.podId === podId && player.followingParty.channelId === channelId);
  const [globalRadio, setGlobalRadio] = useState(false);
  const [meshStreaming, setMeshStreaming] = useState(false);
  const [partyState, setPartyState] = useState(null);
  const [publishError, setPublishError] = useState('');
  const directoryFetchInFlightRef = useRef(false);
  const mountedRef = useRef(false);
  const publishRequestRef = useRef(0);

  useEffect(() => {
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
    };
  }, []);

  useEffect(() => {
    if (!podId || !channelId) return undefined;
    publishRequestRef.current += 1;
    setPublishError('');
    return player.observePartyRoom(podId, channelId, setRoom);
  }, [channelId, player.observePartyRoom, podId]);

  useEffect(() => { setPartyState(roomState); }, [roomState]);
  const ownBroadcast = player.broadcastStatus?.podId === podId && player.broadcastStatus?.channelId === channelId;
  useEffect(() => {
    if (ownBroadcast) {
      setGlobalRadio(Boolean(player.broadcastStatus.globalRadio));
      setMeshStreaming(Boolean(player.broadcastStatus.meshStreaming));
    }
  }, [ownBroadcast, player.broadcastStatus?.globalRadio, player.broadcastStatus?.meshStreaming]);

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
    if (action !== 'stop' && !canBroadcastCurrent) return;

    const requestId = ++publishRequestRef.current;
    setPublishError('');
    const request = player.publishBroadcast({ channelId, globalRadio, meshStreaming, podId, user,
      partyId: partyState?.podId === podId && partyState?.channelId === channelId ? partyState.partyId : '',
    }, action);
    request.then((state) => {
      if (!mountedRef.current || requestId !== publishRequestRef.current) return;
      if (state) setPartyState(action === 'stop' ? null : state);
      if (!compact) refreshDirectory();
    }).catch((error) => {
      if (mountedRef.current && requestId === publishRequestRef.current) {
        setPublishError(error?.message === 'Stop the active broadcast before starting another room.'
          ? error.message
          : error?.response?.status === 429
          ? 'Room updates are at capacity. Retry later.'
          : error?.response?.status === 404
            ? 'This room is unavailable. Choose an existing room.'
            : error?.response?.data?.code === 'room_storage_unavailable'
              ? 'The room update could not be saved. Try again.'
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
          onClick={() => player.retryPartyRoom(podId, channelId)}
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
                onClick={() => player.followParty(following ? null : { podId, channelId })}
                size="mini"
                title="Follow room broadcast"
                toggle
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
                toggle
              >
                <Icon name="globe" />
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
                toggle
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
              toggle
              disabled={player.playerVisible === false}
              icon
              onClick={() => player.followParty(following ? null : { podId, channelId })}
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
