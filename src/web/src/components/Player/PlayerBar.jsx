import './Player.css';
import * as collectionsAPI from '../../lib/collections';
import {
  clearDiscoveryShelf,
  exportDiscoveryShelfPolicyReport,
  getDiscoveryShelf,
  getDiscoveryShelfActionLabel,
  getDiscoveryShelfPolicyPreview,
  getDiscoveryShelfSummary,
  removeDiscoveryShelfItem,
  upsertDiscoveryShelfItem,
} from '../../lib/discoveryShelf';
import * as externalVisualizer from '../../lib/externalVisualizer';
import { setExperiencePreference } from '../../lib/experiencePreferences';
import * as listenBrainz from '../../lib/listenBrainz';
import * as nowPlaying from '../../lib/nowPlaying';
import {
  clearListeningHistory,
  exportListeningHistoryCsv,
  exportListeningHistoryJson,
  getListeningRecommendationQueries,
  getListeningRecommendationSeeds,
  getListeningStats,
  importListeningHistory,
  recordLocalPlay,
} from '../../lib/listeningHistory';
import {
  getPlayerRating,
  getPlayerRatingSummary,
  setPlayerRating,
} from '../../lib/playerRatings';
import {
  buildPlayerRadioPlan,
  buildPlayerRadioSearchPath,
  getPlayerRadioQueries,
  getPlayerRadioCopyText,
} from '../../lib/playerRadio';
import {
  buildSimilarQueueCandidates,
  getSimilarQueueSearchQueries,
} from '../../lib/playerAutoQueue';
import { getPlayerShortcutAction } from '../../lib/playerShortcuts';
import { getLocalStorageItem, setLocalStorageItem } from '../../lib/storage';
import * as searches from '../../lib/searches';
import * as streaming from '../../lib/streaming';
import * as wishlistAPI from '../../lib/wishlist';
import Equalizer from './Equalizer';
import LyricsPane from './LyricsPane';
import SpectrumAnalyzer, { getFrequencyBars } from './SpectrumAnalyzer';
import { fadeOutputGain, getExistingAudioGraph, getOrCreateAudioGraph, releaseAudioGraph, resumeAudioGraph, setKaraokeEnabled, setOutputGain } from './audioGraph';
import { usePlayer } from './PlayerContext';
import React, { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Button,
  Checkbox,
  Header,
  Icon,
  Input,
  Label,
  Message,
  Modal,
  Popup,
  Segment,
  Table,
  TextArea,
} from 'semantic-ui-react';

const localMuteStorageKey = 'slskdn.player.localMuted';
const collapsedStorageKey = 'slskdn.player.collapsed';
const visualizerStorageKey = 'slskdn.player.visualizerEnabled';
const visualizerEngineStorageKey = 'slskdn.player.visualizerEngine';
const eqPanelStorageKey = 'slskdn.player.eqPanelOpen';
const lyricsStorageKey = 'slskdn.player.lyricsOpen';
const karaokeStorageKey = 'slskdn.player.karaokeEnabled';
const crossfadeStorageKey = 'slskdn.player.crossfadeEnabled';
const visualTileStorageKey = 'slskdn.player.visualTileMode';
const analyzerModeStorageKey = 'slskdn.player.analyzerMode';
const volumeStorageKey = 'slskdn.player.volume';
const playbackRateStorageKey = 'slskdn.player.rate';
const playerBrowserPageSize = 80;
const Visualizer = React.lazy(() => import('./Visualizer'));

const formatTime = (seconds) => {
  if (!Number.isFinite(seconds) || seconds < 0) return '0:00';
  const whole = Math.floor(seconds);
  return `${Math.floor(whole / 60)}:${String(whole % 60).padStart(2, '0')}`;
};

const PlayerProgress = ({ current, duration, onSeek, position }) => {
  const draftRef = useRef(null);
  const [draft, setDraft] = useState(null);
  const displayedPosition = draft ?? position;
  useEffect(() => {
    draftRef.current = null;
    setDraft(null);
  }, [current]);
  const commitSeek = () => {
    if (draftRef.current === null) return;
    const seconds = draftRef.current;
    draftRef.current = null;
    setDraft(null);
    onSeek(seconds);
  };

  return (
    <div className="player-progress">
      <span aria-hidden="true">{formatTime(displayedPosition)}</span>
      <input
        aria-label="Seek playback"
        disabled={!current || duration <= 0}
        max={duration || 1}
        min="0"
        onBlur={commitSeek}
        onChange={(event) => {
          const seconds = Number(event.target.value);
          draftRef.current = seconds;
          setDraft(seconds);
        }}
        onKeyUp={commitSeek}
        onPointerCancel={() => {
          draftRef.current = null;
          setDraft(null);
        }}
        onPointerUp={commitSeek}
        step="1"
        type="range"
        value={Math.min(displayedPosition, duration || 1)}
      />
      <span aria-hidden="true">-{formatTime(Math.max(0, duration - displayedPosition))}</span>
    </div>
  );
};

const asArray = (value) => (Array.isArray(value) ? value : []);

const readStoredBoolean = (key) => {
  return getLocalStorageItem(key) === 'true';
};

const readStoredVisualizerEngineTileMode = () => {
  const engine = getLocalStorageItem(visualizerEngineStorageKey);
  if (engine === 'native') return 'native-webgl2';
  return ['butterchurn', 'native-webgl2', 'native-webgpu'].includes(engine)
    ? engine
    : 'butterchurn';
};

const readStoredTileMode = () => {
  const mode = getLocalStorageItem(visualTileStorageKey);
  if (['art', 'spectrum', 'scope', 'butterchurn', 'native-webgl2', 'native-webgpu'].includes(mode)) {
    return mode;
  }
  if (mode === 'milkdrop') {
    return readStoredVisualizerEngineTileMode();
  }
  return 'art';
};

const readStoredAnalyzerMode = () => {
  const mode = getLocalStorageItem(analyzerModeStorageKey);
  return ['off', 'spectrum', 'scope'].includes(mode) ? mode : 'off';
};

const setPlayerHeightVariable = (element) => {
  if (!element || typeof document === 'undefined') return;

  const height = Math.ceil(element.getBoundingClientRect().height);
  if (height > 0) {
    document.documentElement.style.setProperty(
      '--slskdn-player-reserved-height',
      `${height}px`,
    );
  }
};

const getExternalVisualizerStatusText = (status, loading) => {
  if (loading) return 'Checking external visualizer launcher...';
  if (!status) return 'Status unavailable.';
  if (!status.enabled) return 'Disabled in slskd.yml.';
  if (!status.configured) return 'No launcher path configured.';
  if (!status.available) return 'Configured launcher path was not found.';
  return 'Ready to launch on the slskdN host.';
};

const getExternalVisualizerError = (error) => {
  const data = error?.response?.data;
  if (typeof data === 'string') return data;
  if (data?.error) return data.error;
  return 'External visualizer did not launch.';
};

const PlayerToolButton = ({
  active = false,
  children = null,
  content,
  disabled = false,
  icon,
  label,
  ...buttonProps
}) => (
  <Popup
    content={content}
    trigger={
      <Button
        {...buttonProps}
        className={[
          'player-tool-button',
          buttonProps.className,
          active ? 'player-tool-button-active' : '',
        ].filter(Boolean).join(' ')}
        disabled={disabled}
        icon={!label}
        size="small"
        type="button"
      >
        <Icon name={icon} />
        {label ? <span>{label}</span> : null}
        {children}
      </Button>
    }
  />
);

const formatPlayerProvider = (provider = '') => {
  const normalized = String(provider).trim();
  if (!normalized) return '';
  if (normalized.toLowerCase() === 'soulseek') return 'Soulseek';
  if (normalized.toLowerCase() === 'mesh') return 'Mesh';
  if (normalized.toLowerCase() === 'pod') return 'Pod';
  return normalized;
};

const getPlayerBadges = (current) => {
  if (!current) return [];

  const providers = (Array.isArray(current.sourceProviders)
    ? current.sourceProviders
    : [])
    .map(formatPlayerProvider)
    .filter(Boolean);
  const badges = providers.slice(0, 2).map((provider) => ({
    color: provider === 'Mesh' || provider === 'Pod' ? 'violet' : 'grey',
    icon: provider === 'Mesh' ? 'share alternate' : 'music',
    key: `source-${provider}`,
    text: provider,
    title: `Playback source: ${provider}`,
  }));

  const confidence = Number(current.confidence || 0);
  if (confidence > 0) {
    badges.push({
      color: confidence >= 0.75 ? 'green' : 'yellow',
      icon: 'crosshairs',
      key: 'confidence',
      text: `${Math.round(confidence * 100)}% match`,
      title: 'Local match confidence for this now-playing item.',
    });
  }

  if (current.verified) {
    badges.push({
      color: 'teal',
      icon: 'check circle',
      key: 'verified',
      text: 'Verified',
      title: 'This item has local verification evidence.',
    });
  }

  return badges;
};

const PlayerRatingControls = ({ current, onChange, rating }) => {
  if (!current) return null;

  const summary = getPlayerRatingSummary(current);

  return (
    <div
      aria-label="Now playing rating"
      className={[
        'player-rating-controls',
        `player-rating-controls-${summary.tone}`,
      ].join(' ')}
      data-testid="player-rating-controls"
      role="group"
    >
      {[1, 2, 3, 4, 5].map((value) => (
        <Popup
          content={
            value === rating
              ? 'Clear this local rating.'
              : `Rate this track ${value} out of 5 for local discovery context.`
          }
          key={value}
          trigger={
            <button
              aria-label={
                value === rating
                  ? `Clear ${value} star rating`
                  : `Rate ${value} stars`
              }
              className={[
                'player-rating-button',
                value <= rating ? 'player-rating-button-active' : '',
              ].filter(Boolean).join(' ')}
              data-testid={`player-rating-${value}`}
              onClick={() => onChange(value === rating ? 0 : value)}
              title={
                value === rating
                  ? 'Clear this local rating.'
                  : `Rate this track ${value} out of 5.`
              }
              type="button"
            >
              <Icon name={value <= rating ? 'star' : 'star outline'} />
            </button>
          }
        />
      ))}
      <span className="player-rating-summary">{summary.label}</span>
    </div>
  );
};

const PlayerRadioModal = ({ current, onClose, onOpenSearch, open }) => {
  const plan = buildPlayerRadioPlan(current);
  const copyText = getPlayerRadioCopyText(plan);
  const [runningSearches, setRunningSearches] = useState(false);
  const [savingWishlist, setSavingWishlist] = useState(false);
  const [status, setStatus] = useState('');

  const copyPlan = () => {
    if (navigator.clipboard && copyText) {
      navigator.clipboard.writeText(copyText).catch(() => {});
    }
  };

  const startRadioSearches = async () => {
    const queries = getPlayerRadioQueries(plan, { limit: 3 });
    if (queries.length === 0) {
      setStatus('No smart-radio queries are ready.');
      return;
    }

    try {
      setRunningSearches(true);
      const count = await searches.createBatch({ queries });
      setStatus(`Started ${count} smart-radio search${count === 1 ? '' : 'es'}.`);
    } catch {
      setStatus('Unable to start smart-radio searches.');
    } finally {
      setRunningSearches(false);
    }
  };

  const addRadioWishlist = async () => {
    const queries = getPlayerRadioQueries(plan, { limit: 4 });
    if (queries.length === 0) {
      setStatus('No smart-radio queries are ready for Wishlist.');
      return;
    }

    try {
      setSavingWishlist(true);
      await queries.reduce(
        (chain, searchText) =>
          chain.then(() =>
            wishlistAPI.create({
              autoDownload: false,
              enabled: true,
              filter: '',
              maxResults: 50,
              searchText,
            }),
          ),
        Promise.resolve(),
      );
      setStatus(`Added ${queries.length} smart-radio seed${queries.length === 1 ? '' : 's'} to Wishlist.`);
    } catch {
      setStatus('Unable to add smart-radio seeds to Wishlist.');
    } finally {
      setSavingWishlist(false);
    }
  };

  return (
    <Modal
      className="player-browser-modal player-radio-modal"
      onClose={onClose}
      open={open}
      size="small"
    >
      <Modal.Header>Smart Radio Seed</Modal.Header>
      <Modal.Content>
        <p className="player-modal-copy">
          Build review-first radio searches from the current track. Nothing is
          searched or queued until you choose a query.
        </p>
        <div className="player-radio-seed" data-testid="player-radio-seed">
          <Icon name="random" />
          <div>
            <strong>{plan.seedLabel}</strong>
            <div>
              {plan.basis.length > 0 ? plan.basis.join(' | ') : 'Pick a track first.'}
            </div>
          </div>
        </div>
        <div className="player-radio-query-list">
          {plan.queries.map((item) => (
            <div className="player-radio-query" key={item.id}>
              <div>
                <Label color="violet" size="mini">
                  {item.reason}
                </Label>
                <code>{item.query}</code>
              </div>
              <Popup
                content="Open this as a normal Search page query. This is the point where network search work can begin."
                trigger={
                  <Button
                    data-testid={`player-radio-search-${item.id}`}
                    onClick={() => onOpenSearch(item.query)}
                    size="mini"
                    type="button"
                  >
                    <Icon name="search" />
                    Search
                  </Button>
                }
              />
            </div>
          ))}
        </div>
        {status ? (
          <Message compact size="mini">
            {status}
          </Message>
        ) : null}
      </Modal.Content>
      <Modal.Actions>
        <Popup
          content="Start up to three live searches from this smart-radio plan. This does not browse peers, queue downloads, or mutate files."
          trigger={
            <Button
              data-testid="player-radio-start-searches"
              disabled={!plan.ready}
              loading={runningSearches}
              onClick={startRadioSearches}
              type="button"
            >
              <Icon name="search" />
              Start Searches
            </Button>
          }
        />
        <Popup
          content="Add smart-radio seeds to Wishlist as enabled manual requests with auto-download off."
          trigger={
            <Button
              data-testid="player-radio-add-wishlist"
              disabled={!plan.ready}
              loading={savingWishlist}
              onClick={addRadioWishlist}
              type="button"
            >
              <Icon name="heart" />
              Add Wishlist
            </Button>
          }
        />
        <Popup
          content="Copy the generated radio search plan as plain text."
          trigger={
            <Button
              data-testid="player-radio-copy"
              disabled={!copyText}
              onClick={copyPlan}
              type="button"
            >
              <Icon name="copy outline" />
              Copy Plan
            </Button>
          }
        />
        <Popup
          content="Close smart radio without starting a search."
          trigger={
            <Button
              data-testid="player-radio-close"
              onClick={onClose}
              primary
              type="button"
            >
              <Icon name="check" />
              Done
            </Button>
          }
        />
      </Modal.Actions>
    </Modal>
  );
};

const getTrackLabel = (item) =>
  item?.title || item?.fileName || item?.contentId || 'Untitled track';

const PlayerQueueModal = ({
  current,
  history,
  onClearQueue,
  onAutoQueueSimilar,
  onClose,
  onLoadPlaylist,
  onMove,
  onNext,
  onPrevious,
  onRemove,
  open,
  queue,
}) => {
  const upcoming = queue.slice(1);
  const [handoffStatus, setHandoffStatus] = useState('');
  const [searchingSimilar, setSearchingSimilar] = useState(false);
  const [savingSimilarWishlist, setSavingSimilarWishlist] = useState(false);
  const [playlists, setPlaylists] = useState([]);
  const [playlistName, setPlaylistName] = useState('');
  const [selectedPlaylist, setSelectedPlaylist] = useState('');
  const [playlistBusy, setPlaylistBusy] = useState(false);
  const similarCandidates = buildSimilarQueueCandidates({
    current,
    history,
    queue,
  });

  useEffect(() => {
    if (!open) return undefined;
    let cancelled = false;
    collectionsAPI.getCollections().then((response) => {
      if (!cancelled) setPlaylists(asArray(response.data).filter((entry) => entry.type === 'Playlist'));
    }).catch(() => {
      if (!cancelled) setPlaylists([]);
    });
    return () => { cancelled = true; };
  }, [open]);

  const savePlaylist = async () => {
    const name = playlistName.trim();
    const items = queue.filter((item) => !item.contentId.startsWith('local:'));
    if (!name || items.length === 0) return;
    setPlaylistBusy(true);
    let createdPlaylistId = null;
    try {
      const response = await collectionsAPI.createCollection({ title: name, type: 'Playlist' });
      createdPlaylistId = response.data.id;
      for (const item of items) {
        await collectionsAPI.addCollectionItem(createdPlaylistId, {
          album: item.album,
          artist: item.artist,
          contentId: item.contentId,
          fileName: item.fileName,
          mediaKind: 'Audio',
          title: item.title,
        });
      }
      setPlaylists((existing) => [...existing, response.data]);
      setPlaylistName('');
      setHandoffStatus(`Saved ${items.length} tracks to ${name}.`);
    } catch {
      if (createdPlaylistId) {
        try {
          await collectionsAPI.deleteCollection(createdPlaylistId);
        } catch {
          setHandoffStatus('The playlist is incomplete and could not be removed. Review it in Collections.');
          return;
        }
      }
      setHandoffStatus('Could not save the playlist.');
    } finally {
      setPlaylistBusy(false);
    }
  };

  const loadPlaylist = async () => {
    if (!selectedPlaylist) return;
    setPlaylistBusy(true);
    try {
      const response = await collectionsAPI.getCollectionItems(selectedPlaylist);
      const items = asArray(response.data).filter((item) => item.contentId);
      if (items.length === 0) {
        setHandoffStatus('This playlist has no playable tracks.');
      } else {
        onLoadPlaylist(items);
        setHandoffStatus(`Loaded ${items.length} tracks.`);
      }
    } catch {
      setHandoffStatus('Could not load the playlist.');
    } finally {
      setPlaylistBusy(false);
    }
  };

  const startSimilarSearches = async () => {
    const queries = getSimilarQueueSearchQueries(similarCandidates, { limit: 3 });
    if (queries.length === 0) {
      setHandoffStatus('No similar queue candidates are ready to search.');
      return;
    }

    try {
      setSearchingSimilar(true);
      const count = await searches.createBatch({ queries });
      setHandoffStatus(`Started ${count} similar-track search${count === 1 ? '' : 'es'}.`);
    } catch {
      setHandoffStatus('Unable to start similar-track searches.');
    } finally {
      setSearchingSimilar(false);
    }
  };

  const addSimilarWishlist = async () => {
    const queries = getSimilarQueueSearchQueries(similarCandidates, { limit: 5 });
    if (queries.length === 0) {
      setHandoffStatus('No similar queue candidates are ready for Wishlist.');
      return;
    }

    try {
      setSavingSimilarWishlist(true);
      await queries.reduce(
        (chain, searchText) =>
          chain.then(() =>
            wishlistAPI.create({
              autoDownload: false,
              enabled: true,
              filter: '',
              maxResults: 50,
              searchText,
            }),
          ),
        Promise.resolve(),
      );
      setHandoffStatus(`Added ${queries.length} similar-track seed${queries.length === 1 ? '' : 's'} to Wishlist.`);
    } catch {
      setHandoffStatus('Unable to add similar-track seeds to Wishlist.');
    } finally {
      setSavingSimilarWishlist(false);
    }
  };

  return (
    <Modal
      className="player-browser-modal player-queue-modal"
      onClose={onClose}
      open={open}
      size="small"
    >
      <Modal.Header>Playback Queue</Modal.Header>
      <Modal.Content>
        <div className="player-queue-manager">
          <section className="player-playlist-actions">
            <div className="player-panel-title">Playlists</div>
            <Input
              aria-label="New playlist name"
              onChange={(event) => setPlaylistName(event.target.value)}
              placeholder="Name this queue"
              size="small"
              value={playlistName}
            />
            <Popup content="Save the current server library tracks as a new Collection playlist." trigger={
              <Button disabled={!playlistName.trim() || playlistBusy || queue.every((item) => item.contentId.startsWith('local:'))} onClick={savePlaylist} size="small" type="button">Save queue</Button>
            } />
            <select aria-label="Saved playlist" onChange={(event) => setSelectedPlaylist(event.target.value)} value={selectedPlaylist}>
              <option value="">Choose playlist</option>
              {playlists.map((playlist) => <option key={playlist.id} value={playlist.id}>{playlist.title}</option>)}
            </select>
            <Popup content="Replace the immediate queue with tracks from this saved playlist." trigger={
              <Button disabled={!selectedPlaylist || playlistBusy} onClick={loadPlaylist} size="small" type="button">Load</Button>
            } />
          </section>
          <section>
            <div className="player-panel-title">Now Playing</div>
            <div className="player-queue-manager-row player-queue-manager-current">
              <Icon name="play circle outline" />
              <div>
                <strong>{getTrackLabel(current)}</strong>
                <span>{current?.artist || 'No active playback'}</span>
              </div>
            </div>
          </section>
          <section>
            <div className="player-queue-manager-heading">
              <div className="player-panel-title">Upcoming</div>
              <div className="player-queue-manager-actions">
                <Popup
                  content="Add similar recent session tracks to the upcoming queue. This only uses tracks already known to this browser session."
                  trigger={
                    <Button
                      data-testid="player-auto-queue-similar"
                      disabled={similarCandidates.length === 0}
                      onClick={() =>
                        onAutoQueueSimilar(
                          similarCandidates.map((candidate) => candidate.item),
                        )
                      }
                      size="mini"
                      type="button"
                    >
                      <Icon name="magic" />
                      Auto-fill Similar
                    </Button>
                  }
                />
                <Popup
                  content="Start up to three searches from similar recent session tracks. This starts search jobs only."
                  trigger={
                    <Button
                      data-testid="player-search-similar-candidates"
                      disabled={similarCandidates.length === 0}
                      loading={searchingSimilar}
                      onClick={startSimilarSearches}
                      size="mini"
                      type="button"
                    >
                      <Icon name="search" />
                      Search Similar
                    </Button>
                  }
                />
                <Popup
                  content="Add similar recent session tracks to Wishlist as manual requests with auto-download off."
                  trigger={
                    <Button
                      data-testid="player-wishlist-similar-candidates"
                      disabled={similarCandidates.length === 0}
                      loading={savingSimilarWishlist}
                      onClick={addSimilarWishlist}
                      size="mini"
                      type="button"
                    >
                      <Icon name="heart" />
                      Wishlist Similar
                    </Button>
                  }
                />
                <Popup
                  content="Remove every upcoming item while keeping the current track playing."
                  trigger={
                    <Button
                      data-testid="player-clear-upcoming"
                      disabled={upcoming.length === 0}
                      onClick={onClearQueue}
                      size="mini"
                      type="button"
                    >
                      <Icon name="trash alternate outline" />
                      Clear Upcoming
                    </Button>
                  }
                />
              </div>
            </div>
            {upcoming.length > 0 ? (
              <div className="player-queue-manager-list">
                {upcoming.map((item, index) => (
                  <div
                    className="player-queue-manager-row"
                    data-testid={`player-queue-row-${item.contentId}`}
                    key={`${item.contentId}-${index}`}
                  >
                    <span className="player-queue-manager-index">{index + 1}</span>
                    <div>
                      <strong>{getTrackLabel(item)}</strong>
                      <span>{item.artist || item.album || item.contentId}</span>
                    </div>
                    <Popup content="Move this track earlier in the queue." trigger={
                      <Button aria-label={`Move ${getTrackLabel(item)} up`} disabled={index === 0} icon onClick={() => onMove(index + 1, index)} size="mini" type="button"><Icon name="arrow up" /></Button>
                    } />
                    <Popup content="Move this track later in the queue." trigger={
                      <Button aria-label={`Move ${getTrackLabel(item)} down`} disabled={index === upcoming.length - 1} icon onClick={() => onMove(index + 1, index + 2)} size="mini" type="button"><Icon name="arrow down" /></Button>
                    } />
                    <Popup
                      content="Remove this upcoming item from the local playback queue."
                      trigger={
                        <Button
                          aria-label={`Remove ${getTrackLabel(item)} from queue`}
                          data-testid={`player-remove-queue-${item.contentId}`}
                          icon
                          onClick={() => onRemove(item.contentId)}
                          size="mini"
                          type="button"
                        >
                          <Icon name="close" />
                        </Button>
                      }
                    />
                  </div>
                ))}
              </div>
            ) : (
              <div className="player-queue-manager-empty">
                No upcoming tracks.
              </div>
            )}
          </section>
          {handoffStatus ? (
            <Message compact size="mini">
              {handoffStatus}
            </Message>
          ) : null}
          <section>
            <div className="player-panel-title">Recent</div>
            {history.length > 0 ? (
              <div className="player-queue-manager-list">
                {history.slice(0, 5).map((item, index) => (
                  <div
                    className="player-queue-manager-row"
                    key={`${item.contentId}-${index}`}
                  >
                    <Icon name="history" />
                    <div>
                      <strong>{getTrackLabel(item)}</strong>
                      <span>{item.artist || item.album || item.contentId}</span>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="player-queue-manager-empty">
                No recent tracks in this session.
              </div>
            )}
          </section>
        </div>
      </Modal.Content>
      <Modal.Actions>
        <Popup
          content="Jump back to the previous session track, or restart the current track if there is no history."
          trigger={
            <Button
              data-testid="player-queue-previous"
              disabled={!current}
              onClick={onPrevious}
              type="button"
            >
              <Icon name="step backward" />
              Previous
            </Button>
          }
        />
        <Popup
          content="Advance to the next queued track."
          trigger={
            <Button
              data-testid="player-queue-next"
              disabled={queue.length < 2}
              onClick={onNext}
              type="button"
            >
              <Icon name="step forward" />
              Next
            </Button>
          }
        />
        <Popup
          content="Close the queue manager."
          trigger={
            <Button
              data-testid="player-queue-close"
              onClick={onClose}
              primary
              type="button"
            >
              <Icon name="check" />
              Done
            </Button>
          }
        />
      </Modal.Actions>
    </Modal>
  );
};

const PlayerDiscoveryShelfModal = ({ onClose, open }) => {
  const [expiryDays, setExpiryDays] = useState(14);
  const [items, setItems] = useState(() => getDiscoveryShelf());
  const [message, setMessage] = useState('');
  const [requireConsensus, setRequireConsensus] = useState(true);
  const summary = getDiscoveryShelfSummary();
  const policyPreview = getDiscoveryShelfPolicyPreview({
    expiryDays,
    items,
    requireConsensus,
  });

  const refreshShelf = () => {
    setItems(getDiscoveryShelf());
  };

  const previewAction = (item) => {
    setMessage(
      `${getDiscoveryShelfActionLabel(item.action)} prepared for ${item.title}. No files were moved or deleted.`,
    );
  };

  const removeItem = (key) => {
    removeDiscoveryShelfItem(key);
    refreshShelf();
  };

  const clearShelf = () => {
    clearDiscoveryShelf();
    refreshShelf();
    setMessage('Discovery shelf cleared from this browser.');
  };

  const copyPolicyReport = () => {
    const report = exportDiscoveryShelfPolicyReport({
      expiryDays,
      items,
      requireConsensus,
    });

    if (navigator.clipboard?.writeText) {
      navigator.clipboard.writeText(report).catch(() => {});
    }

    setMessage(`Policy report prepared for ${items.length} shelf items.`);
  };

  return (
    <Modal
      className="player-browser-modal player-discovery-shelf-modal"
      onClose={onClose}
      open={open}
      size="small"
    >
      <Modal.Header>Discovery Shelf</Modal.Header>
      <Modal.Content>
        <div className="player-shelf-summary" data-testid="player-shelf-summary">
          <div>
            <strong>{summary.total}</strong>
            <span>local review items</span>
          </div>
          <div>
            <strong>{summary['promote-preview']}</strong>
            <span>promote previews</span>
          </div>
          <div>
            <strong>{summary['archive-preview']}</strong>
            <span>archive previews</span>
          </div>
          <div>
            <strong>{summary['expiry-watch']}</strong>
            <span>expiry watch</span>
          </div>
        </div>
        {message ? (
          <Message compact size="mini">
            {message}
          </Message>
        ) : null}
        <section className="player-shelf-policy">
          <div className="player-panel-title">Policy Preview</div>
          <div className="player-shelf-policy-controls">
            <label htmlFor="player-shelf-expiry-days">Expire unrated after</label>
            <Input
              aria-label="Discovery shelf expiry days"
              data-testid="player-shelf-expiry-days"
              id="player-shelf-expiry-days"
              min="1"
              onChange={(event) => setExpiryDays(event.target.value)}
              size="mini"
              type="number"
              value={expiryDays}
            />
            <Popup
              content="Require shared-library consensus before any destructive archive or expiry action can be applied later."
              trigger={
                <Checkbox
                  checked={requireConsensus}
                  data-testid="player-shelf-require-consensus"
                  label="Consensus for destructive actions"
                  onChange={(_, data) => setRequireConsensus(Boolean(data.checked))}
                  toggle
                />
              }
            />
          </div>
          <div className="player-shelf-policy-preview" data-testid="player-shelf-policy-preview">
            <span>{policyPreview.promote} promote</span>
            <span>{policyPreview.archive} archive</span>
            <span>{policyPreview.expire} expire</span>
            <span>{policyPreview.review} review</span>
            <span>{policyPreview.blockedByConsensus} consensus gated</span>
          </div>
          <Popup
            content="Copy a text report of the current shelf policy preview for review. This does not apply any action."
            trigger={
              <Button
                data-testid="player-shelf-copy-policy-report"
                disabled={items.length === 0}
                onClick={copyPolicyReport}
                size="mini"
                type="button"
              >
                <Icon name="copy" />
                Copy Report
              </Button>
            }
          />
        </section>
        <div className="player-shelf-list">
          {items.length > 0 ? items.map((item) => (
            <div
              className="player-shelf-row"
              data-testid={`player-shelf-row-${item.key}`}
              key={item.key}
            >
              <div className="player-shelf-rating">{item.rating || '-'}</div>
              <div className="player-shelf-track">
                <strong>{item.title}</strong>
                <span>
                  {[item.artist, item.album].filter(Boolean).join(' - ') || 'Local discovery item'}
                </span>
              </div>
              <Label size="mini">
                {getDiscoveryShelfActionLabel(item.action)}
              </Label>
              <Popup
                content="Preview the shelf action. This does not move, delete, share, download, or publish anything."
                trigger={
                  <Button
                    data-testid={`player-shelf-preview-${item.key}`}
                    icon
                    onClick={() => previewAction(item)}
                    size="mini"
                    type="button"
                  >
                    <Icon name="eye" />
                  </Button>
                }
              />
              <Popup
                content="Remove this local review item from the browser-only shelf."
                trigger={
                  <Button
                    data-testid={`player-shelf-remove-${item.key}`}
                    icon
                    onClick={() => removeItem(item.key)}
                    size="mini"
                    type="button"
                  >
                    <Icon name="trash alternate outline" />
                  </Button>
                }
              />
            </div>
          )) : (
            <div className="player-queue-manager-empty">
              Rate tracks in the player to build a local discovery review shelf.
            </div>
          )}
        </div>
      </Modal.Content>
      <Modal.Actions>
        <Popup
          content="Clear browser-local discovery shelf review items. This does not affect files or ratings."
          trigger={
            <Button
              data-testid="player-clear-discovery-shelf"
              disabled={items.length === 0}
              onClick={clearShelf}
              type="button"
            >
              <Icon name="trash" />
              Clear Shelf
            </Button>
          }
        />
        <Popup
          content="Close the local discovery shelf."
          trigger={
            <Button
              data-testid="player-close-discovery-shelf"
              onClick={onClose}
              primary
              type="button"
            >
              <Icon name="check" />
              Done
            </Button>
          }
        />
      </Modal.Actions>
    </Modal>
  );
};

const PlayerStatsModal = ({ onClose, onOpenSearch, open }) => {
  const fileInputRef = useRef(null);
  const [rangeDays, setRangeDays] = useState(30);
  const [importText, setImportText] = useState('');
  const [importStatus, setImportStatus] = useState(null);
  const [runningSeedSearches, setRunningSeedSearches] = useState(false);
  const [scrobblingRecent, setScrobblingRecent] = useState(false);
  const [savingSeedWishlist, setSavingSeedWishlist] = useState(false);
  const [stats, setStats] = useState(() =>
    getListeningStats({ rangeDays: 30 }),
  );
  const recommendationSeeds = getListeningRecommendationSeeds(stats);
  const refreshStats = useCallback((nextRangeDays = rangeDays) => {
    setStats(getListeningStats({ rangeDays: nextRangeDays }));
  }, [rangeDays]);

  const clearStats = () => {
    clearListeningHistory();
    setImportStatus(null);
    refreshStats();
  };

  const updateRange = (nextRangeDays) => {
    setRangeDays(nextRangeDays);
    refreshStats(nextRangeDays);
  };

  const importHistory = () => {
    const result = importListeningHistory(importText);
    setImportStatus(
      `${result.imported} imported, ${result.skipped} skipped as duplicates or incomplete rows.`,
    );
    setImportText('');
    refreshStats();
  };

  const copyHistory = (format) => {
    const content = format === 'csv'
      ? exportListeningHistoryCsv()
      : exportListeningHistoryJson();

    if (navigator.clipboard?.writeText) {
      navigator.clipboard.writeText(content).catch(() => {});
    }

    setImportStatus(`Prepared ${format.toUpperCase()} export for ${stats.history.length} plays.`);
  };

  const startSeedSearches = async () => {
    const queries = getListeningRecommendationQueries(stats, { limit: 3 });
    if (queries.length === 0) {
      setImportStatus('No listening seeds are ready to search.');
      return;
    }

    try {
      setRunningSeedSearches(true);
      const count = await searches.createBatch({ queries });
      setImportStatus(`Started ${count} bounded listening seed search${count === 1 ? '' : 'es'}.`);
    } catch {
      setImportStatus('Unable to start listening seed searches.');
    } finally {
      setRunningSeedSearches(false);
    }
  };

  const addSeedsToWishlist = async () => {
    const queries = getListeningRecommendationQueries(stats, { limit: 5 });
    if (queries.length === 0) {
      setImportStatus('No listening seeds are ready for Wishlist.');
      return;
    }

    try {
      setSavingSeedWishlist(true);
      await queries.reduce(
        (chain, searchText) =>
          chain.then(() =>
            wishlistAPI.create({
              autoDownload: false,
              enabled: true,
              filter: '',
              maxResults: 50,
              searchText,
            }),
          ),
        Promise.resolve(),
      );
      setImportStatus(`Added ${queries.length} listening seed${queries.length === 1 ? '' : 's'} to Wishlist for manual acquisition.`);
    } catch {
      setImportStatus('Unable to add listening seeds to Wishlist.');
    } finally {
      setSavingSeedWishlist(false);
    }
  };

  const scrobbleRecentHistory = async () => {
    try {
      setScrobblingRecent(true);
      const result = await listenBrainz.submitListeningHistory(stats.history, {
        limit: 10,
      });
      setImportStatus(
        result.submitted > 0
          ? `Submitted ${result.submitted} recent listen${result.submitted === 1 ? '' : 's'} to ListenBrainz.`
          : 'No ListenBrainz token or eligible recent listens are available.',
      );
    } catch {
      setImportStatus('Unable to submit recent listens to ListenBrainz.');
    } finally {
      setScrobblingRecent(false);
    }
  };

  const readImportFile = (event) => {
    const file = event.target.files?.[0];
    if (!file) return;

    file.text().then((content) => {
      setImportText(content);
      setImportStatus(`Loaded ${file.name} for review.`);
    }).catch(() => {
      setImportStatus(`Could not read ${file.name}.`);
    });
    event.target.value = '';
  };

  const renderList = (items, emptyText) => (
    items.length > 0 ? (
      <div className="player-stats-list">
        {items.map((item, index) => (
          <div className="player-stats-row" key={`${item.label || item.title}-${index}`}>
            <span>{index + 1}</span>
            <strong>{item.label || item.title}</strong>
            <em>{item.plays ? `${item.plays} plays` : item.artist || item.album || ''}</em>
          </div>
        ))}
      </div>
    ) : (
      <div className="player-queue-manager-empty">{emptyText}</div>
    )
  );

  return (
    <Modal
      className="player-browser-modal player-stats-modal"
      onClose={onClose}
      open={open}
      size="small"
    >
      <Modal.Header>Listening Stats</Modal.Header>
      <Modal.Content>
        <div className="player-stats-summary" data-testid="player-stats-summary">
          <Icon name="bar chart" />
          <div>
            <strong>{stats.totalPlays}</strong>
            <span>
              local plays recorded in this browser
              {rangeDays ? ` over ${rangeDays} days` : ' overall'}
            </span>
          </div>
        </div>
        <div className="player-stats-ranges" role="group" aria-label="Listening stats range">
          {[
            { label: '7D', value: 7 },
            { label: '30D', value: 30 },
            { label: '90D', value: 90 },
            { label: 'All', value: null },
          ].map((range) => (
            <Button
              active={rangeDays === range.value}
              data-testid={`player-stats-range-${range.label}`}
              key={range.label}
              onClick={() => updateRange(range.value)}
              size="mini"
              type="button"
            >
              {range.label}
            </Button>
          ))}
        </div>
        <div className="player-stats-grid">
          <section>
            <div className="player-panel-title">Top Artists</div>
            {renderList(stats.topArtists, 'No artist plays recorded yet.')}
          </section>
          <section>
            <div className="player-panel-title">Top Tracks</div>
            {renderList(stats.topTracks, 'No track plays recorded yet.')}
          </section>
          <section>
            <div className="player-panel-title">Top Genres</div>
            {renderList(stats.topGenres, 'No genre metadata recorded yet.')}
          </section>
          <section>
            <div className="player-panel-title">Recent</div>
            {renderList(stats.recent, 'No recent plays recorded yet.')}
          </section>
          <section>
            <div className="player-panel-title">Forgotten Favorites</div>
            {renderList(
              stats.forgottenFavorites,
              'No older repeat plays outside this range yet.',
            )}
          </section>
        </div>
        <section className="player-stats-recommendations">
          <div className="player-panel-title">Recommendation Seeds</div>
          {recommendationSeeds.length > 0 ? (
            <>
              <div className="player-stats-seed-list">
                {recommendationSeeds.map((seed) => (
                  <div className="player-stats-seed-row" key={`${seed.type}-${seed.query}`}>
                    <div>
                      <strong>{seed.label}</strong>
                      <span>{seed.type} - {seed.basis}</span>
                    </div>
                    <Popup
                      content="Open this local listening seed as a normal Search page query. Network search starts only after you choose to search."
                      trigger={
                        <Button
                          aria-label={`Search ${seed.label}`}
                          data-testid={`player-stats-search-seed-${seed.query}`}
                          icon
                          onClick={() => onOpenSearch(seed.query)}
                          size="mini"
                          type="button"
                        >
                          <Icon name="search" />
                        </Button>
                      }
                    />
                  </div>
                ))}
              </div>
              <Popup
                content="Start up to three live searches from the strongest listening seeds. This only starts searches; it does not browse peers, queue downloads, or mutate files."
                trigger={
                  <Button
                    data-testid="player-stats-start-seed-searches"
                    loading={runningSeedSearches}
                    onClick={startSeedSearches}
                    size="mini"
                    type="button"
                  >
                    <Icon name="search" />
                    Start Searches
                  </Button>
                }
              />
              <Popup
                content="Add up to five listening seeds to Wishlist as enabled manual-acquisition requests. Auto-download stays off."
                trigger={
                  <Button
                    data-testid="player-stats-add-seeds-to-wishlist"
                    loading={savingSeedWishlist}
                    onClick={addSeedsToWishlist}
                    size="mini"
                    type="button"
                  >
                    <Icon name="heart" />
                    Add Wishlist
                  </Button>
                }
              />
            </>
          ) : (
            <div className="player-queue-manager-empty">
              Play more tracks locally to build recommendation seeds.
            </div>
          )}
        </section>
        <section className="player-stats-import">
          <div className="player-panel-title">Media Server Import</div>
          <TextArea
            aria-label="Paste exported media server play history"
            data-testid="player-listening-history-import-text"
            onChange={(event) => setImportText(event.target.value)}
            placeholder="Paste Plex, Jellyfin, Navidrome, or generic CSV/JSON play history here for local import."
            rows={4}
            value={importText}
          />
          {importStatus ? (
            <Message compact size="mini">
              {importStatus}
            </Message>
          ) : null}
          <div className="player-stats-import-actions">
            <input
              accept=".csv,.json,.txt"
              aria-label="Choose media server history file"
              data-testid="player-listening-history-file"
              onChange={readImportFile}
              ref={fileInputRef}
              type="file"
            />
            <Popup
              content="Choose a local CSV or JSON export from Plex, Jellyfin, Navidrome, or another media server. The file is read in this browser only."
              trigger={
                <Button
                  data-testid="player-listening-history-choose-file"
                  onClick={() => fileInputRef.current?.click()}
                  size="mini"
                  type="button"
                >
                  <Icon name="folder open" />
                  Choose File
                </Button>
              }
            />
            <Popup
              content="Import the pasted or chosen play history into browser-local listening stats with duplicate suppression."
              trigger={
                <Button
                  data-testid="player-listening-history-import"
                  disabled={!importText.trim()}
                  onClick={importHistory}
                  primary
                  size="mini"
                  type="button"
                >
                  <Icon name="upload" />
                  Import
                </Button>
              }
            />
            <Popup
              content="Copy the browser-local listening history as JSON for backup or review."
              trigger={
                <Button
                  data-testid="player-listening-history-export-json"
                  disabled={stats.history.length === 0}
                  onClick={() => copyHistory('json')}
                  size="mini"
                  type="button"
                >
                  <Icon name="copy" />
                  JSON
                </Button>
              }
            />
            <Popup
              content="Copy the browser-local listening history as CSV for media-server or spreadsheet review."
              trigger={
                <Button
                  data-testid="player-listening-history-export-csv"
                  disabled={stats.history.length === 0}
                  onClick={() => copyHistory('csv')}
                  size="mini"
                  type="button"
                >
                  <Icon name="table" />
                  CSV
                </Button>
              }
            />
            <Popup
              content="Submit up to ten recent browser-local plays to ListenBrainz using the saved token. This does not search, browse peers, download, or mutate files."
              trigger={
                <Button
                  data-testid="player-listening-history-scrobble-recent"
                  disabled={stats.history.length === 0}
                  loading={scrobblingRecent}
                  onClick={scrobbleRecentHistory}
                  size="mini"
                  type="button"
                >
                  <Icon name="send" />
                  Scrobble Recent
                </Button>
              }
            />
          </div>
        </section>
      </Modal.Content>
      <Modal.Actions>
        <Popup
          content="Clear only the browser-local listening history used for this stats view."
          trigger={
            <Button
              data-testid="player-clear-listening-history"
              disabled={stats.totalPlays === 0}
              onClick={clearStats}
              type="button"
            >
              <Icon name="trash alternate outline" />
              Clear Local History
            </Button>
          }
        />
        <Popup
          content="Close listening stats."
          trigger={
            <Button
              data-testid="player-close-listening-stats"
              onClick={onClose}
              primary
              type="button"
            >
              <Icon name="check" />
              Done
            </Button>
          }
        />
      </Modal.Actions>
    </Modal>
  );
};

const PlayerLauncher = ({ compact = false, onPlayItem, onPlayNext }) => {
  const navigate = useNavigate();
  const [collections, setCollections] = useState([]);
  const [collectionsLoading, setCollectionsLoading] = useState(false);
  const [collectionsOpen, setCollectionsOpen] = useState(false);
  const [selectedCollection, setSelectedCollection] = useState(null);
  const [collectionItems, setCollectionItems] = useState([]);
  const [collectionItemsLoading, setCollectionItemsLoading] = useState(false);
  const collectionRequestRef = useRef(0);
  const [items, setItems] = useState([]);
  const [browserDirectories, setBrowserDirectories] = useState([]);
  const [browserBreadcrumbs, setBrowserBreadcrumbs] = useState([]);
  const [browserHasMore, setBrowserHasMore] = useState(false);
  const [browserOffset, setBrowserOffset] = useState(0);
  const [browserPath, setBrowserPath] = useState('');
  const [browserStats, setBrowserStats] = useState({
    duplicatesRemoved: 0,
    totalDirectories: 0,
    totalFiles: 0,
  });
  const [filesOpen, setFilesOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [itemsLoading, setItemsLoading] = useState(false);

  useEffect(() => {
    if (!collectionsOpen) return undefined;
    let canceled = false;
    collectionsAPI
      .getCollections()
      .then((response) => {
        if (!canceled) setCollections(asArray(response.data));
      })
      .catch(() => {
        if (!canceled) setCollections([]);
      })
      .finally(() => {
        if (!canceled) setCollectionsLoading(false);
      });

    return () => {
      canceled = true;
    };
  }, [collectionsOpen]);

  useEffect(() => {
    if (!filesOpen) {
      setItemsLoading(false);
      return undefined;
    }

    if (query && query.length < 2) {
      setItemsLoading(false);
      setItems([]);
      setBrowserDirectories([]);
      setBrowserBreadcrumbs([]);
      setBrowserHasMore(false);
      setBrowserStats({ duplicatesRemoved: 0, totalDirectories: 0, totalFiles: 0 });
      return undefined;
    }

    let canceled = false;
    const timeoutId = window.setTimeout(() => {
      setItemsLoading(true);
      collectionsAPI
        .browseLibraryItems({
          kinds: 'Audio',
          limit: playerBrowserPageSize,
          offset: browserOffset,
          path: browserPath,
          query,
        })
        .then((response) => {
          if (!canceled) {
            setItems(asArray(response.data?.files));
            setBrowserDirectories(asArray(response.data?.directories));
            setBrowserBreadcrumbs(asArray(response.data?.breadcrumbs));
            setBrowserHasMore(Boolean(response.data?.hasMore));
            setBrowserStats({
              duplicatesRemoved: response.data?.duplicatesRemoved || 0,
              totalDirectories: response.data?.totalDirectories || 0,
              totalFiles: response.data?.totalFiles || 0,
            });
          }
        })
        .catch(() => {
          if (!canceled) {
            setItems([]);
            setBrowserDirectories([]);
            setBrowserBreadcrumbs([]);
            setBrowserHasMore(false);
            setBrowserStats({ duplicatesRemoved: 0, totalDirectories: 0, totalFiles: 0 });
          }
        })
        .finally(() => {
          if (!canceled) setItemsLoading(false);
        });
    }, query ? 200 : 0);

    return () => {
      canceled = true;
      window.clearTimeout(timeoutId);
    };
  }, [browserOffset, browserPath, filesOpen, query]);

  const selectCollection = (collection) => {
    const requestId = ++collectionRequestRef.current;
    setSelectedCollection(collection);
    setCollectionItems([]);
    setCollectionItemsLoading(true);
    collectionsAPI
      .getCollectionItems(collection.id)
      .then((response) => {
        if (requestId === collectionRequestRef.current) {
          setCollectionItems(asArray(response.data));
        }
      })
      .catch(() => {
        if (requestId === collectionRequestRef.current) setCollectionItems([]);
      })
      .finally(() => {
        if (requestId === collectionRequestRef.current) setCollectionItemsLoading(false);
      });
  };

  const playAndClose = (item) => {
    onPlayItem(item);
    setFilesOpen(false);
    setCollectionsOpen(false);
  };

  const openFileBrowser = () => {
    setBrowserOffset(0);
    setBrowserPath('');
    setFilesOpen(true);
    setQuery('');
  };

  const openBrowserPath = (path) => {
    setBrowserOffset(0);
    setBrowserPath(path || '');
    setQuery('');
  };

  const updateBrowserQuery = (value) => {
    setBrowserOffset(0);
    setQuery(value || '');
  };

  const shownFileCount = Math.min(
    browserOffset + items.length,
    browserStats.totalFiles,
  );

  return (
    <div className="player-launcher">
      <Popup
        content="Browse your collections and play an item from a playlist or share list."
        trigger={
          <Button
            aria-label="Open collections browser"
            className="player-library-button"
            compact
            data-testid="player-open-collections-browser"
            icon
            labelPosition={compact ? undefined : 'left'}
            onClick={() => {
              setCollectionsLoading(true);
              setCollectionsOpen(true);
            }}
            size="small"
            title="Open collections browser"
          >
            <Icon name="list" />
            {compact ? null : 'Collections'}
          </Button>
        }
      />
      <Popup
        content="Browse shared and downloaded local audio that slskdN can stream in this browser."
        trigger={
          <Button
            aria-label="Open local audio file browser"
            className="player-library-button"
            compact
            data-testid="player-open-file-browser"
            icon
            labelPosition={compact ? undefined : 'left'}
            onClick={openFileBrowser}
            size="small"
            title="Open local audio file browser"
          >
            <Icon name="folder open" />
            {compact ? null : 'Files'}
          </Button>
        }
      />

      <Modal
        className="player-browser-modal"
        data-testid="player-collection-browser-modal"
        onClose={() => setCollectionsOpen(false)}
        open={collectionsOpen}
        size="large"
      >
        <Modal.Header>Choose from Collections</Modal.Header>
        <Modal.Content>
          <div className="player-browser-grid">
            <Segment className="player-browser-panel">
              <Header as="h4">Collections</Header>
              {collectionsLoading ? (
                <Message info>Loading collections...</Message>
              ) : collections.length === 0 ? (
                <Message info>No collections found.</Message>
              ) : (
                <Table compact selectable>
                  <Table.Body>
                    {collections.map((collection) => (
                      <Table.Row
                        active={selectedCollection?.id === collection.id}
                        data-testid={`player-collection-row-${collection.id}`}
                        key={collection.id}
                        onClick={() => selectCollection(collection)}
                      >
                        <Table.Cell>
                          <strong>{collection.title}</strong>
                          <div className="player-picker-meta">
                            {collection.type || 'Playlist'}
                          </div>
                        </Table.Cell>
                      </Table.Row>
                    ))}
                  </Table.Body>
                </Table>
              )}
            </Segment>
            <Segment className="player-browser-panel">
              <Header as="h4">
                {selectedCollection?.title || 'Collection Items'}
              </Header>
              {!selectedCollection ? (
                <Message info>Select a collection to see its tracks.</Message>
              ) : collectionItemsLoading ? (
                <Message info>Loading collection items...</Message>
              ) : collectionItems.length === 0 ? (
                <Message info>No playable items in this collection.</Message>
              ) : (
                <Table compact>
                  <Table.Header>
                    <Table.Row>
                      <Table.HeaderCell>Track</Table.HeaderCell>
                      <Table.HeaderCell collapsing>Action</Table.HeaderCell>
                    </Table.Row>
                  </Table.Header>
                  <Table.Body>
                    {collectionItems.map((item) => (
                      <Table.Row key={item.id || item.contentId}>
                        <Table.Cell>
                          <strong>
                            {item.fileName || item.title || item.contentId}
                          </strong>
                          <div className="player-picker-meta">
                            {item.mediaKind || 'Audio'}
                          </div>
                        </Table.Cell>
                        <Table.Cell collapsing>
                          <Popup
                            content="Play this collection item in the browser player."
                            trigger={
                              <Button
                                data-testid={`player-play-collection-item-${item.contentId}`}
                                icon
                                onClick={() => playAndClose(item)}
                                size="small"
                              >
                                <Icon name="play" />
                              </Button>
                            }
                          />
                          <Popup content="Place this collection item next in the playback queue." trigger={
                            <Button aria-label={`Play ${item.title || item.fileName || 'track'} next`} disabled={!onPlayNext} icon onClick={() => onPlayNext(item)} size="small"><Icon name="level down alternate" /></Button>
                          } />
                        </Table.Cell>
                      </Table.Row>
                    ))}
                  </Table.Body>
                </Table>
              )}
            </Segment>
          </div>
        </Modal.Content>
        <Modal.Actions>
          <Popup
            content="Open the full Collections page to create, edit, or share collections."
            trigger={
              <Button
                data-testid="player-manage-collections"
                onClick={() => {
                  setCollectionsOpen(false);
                  navigate('/collections');
                }}
              >
                <Icon name="external alternate" />
                Manage Collections
              </Button>
            }
          />
          <Popup
            content="Close the collection picker without changing playback."
            trigger={
              <Button onClick={() => setCollectionsOpen(false)}>Close</Button>
            }
          />
        </Modal.Actions>
      </Modal>

      <Modal
        className="player-browser-modal"
        data-testid="player-file-browser-modal"
        onClose={() => setFilesOpen(false)}
        open={filesOpen}
        size="fullscreen"
      >
        <Modal.Header>Browse Local Audio Library</Modal.Header>
        <Modal.Content>
          <div className="player-file-explorer">
            <div className="player-file-explorer-toolbar">
              <Input
                data-testid="player-file-browser-search"
                fluid
                icon="search"
                onChange={(_, { value }) => updateBrowserQuery(value)}
                placeholder="Search all audio by file, artist folder, album folder, or path"
                value={query}
              />
              <div className="player-file-explorer-counts">
                {itemsLoading
                  ? 'Loading...'
                  : `${shownFileCount} of ${browserStats.totalFiles} tracks`}
                {browserStats.duplicatesRemoved > 0
                  ? `, ${browserStats.duplicatesRemoved} duplicates collapsed`
                  : ''}
              </div>
            </div>

            <div className="player-file-explorer-breadcrumbs">
              {(browserBreadcrumbs.length > 0
                ? browserBreadcrumbs
                : [{ name: 'Library', path: '' }]).map((breadcrumb, index) => (
                  <React.Fragment key={breadcrumb.path || 'library'}>
                    {index > 0 ? <Icon name="angle right" /> : null}
                    <Popup
                      content={`Open ${breadcrumb.name} to browse files from that folder.`}
                      trigger={
                        <button
                          className="player-file-breadcrumb"
                          data-testid={`player-file-breadcrumb-${index}`}
                          onClick={() => openBrowserPath(breadcrumb.path)}
                          type="button"
                        >
                          {breadcrumb.name}
                        </button>
                      }
                    />
                  </React.Fragment>
              ))}
            </div>

            <div className="player-file-explorer-body">
              <aside className="player-file-explorer-folders">
                <div className="player-file-explorer-section-title">
                  Folders
                </div>
                {query ? (
                  <Message info compact>
                    Clear search to browse folders.
                  </Message>
                ) : browserDirectories.length === 0 ? (
                  <Message info compact>
                    No child folders here.
                  </Message>
                ) : (
                  browserDirectories.map((directory) => (
                    <Popup
                      content={`Open ${directory.name} to browse its tracks and subfolders.`}
                      key={directory.path}
                      trigger={
                        <button
                          className="player-file-folder-row"
                          data-testid={`player-file-folder-${directory.path}`}
                          onClick={() => openBrowserPath(directory.path)}
                          type="button"
                        >
                          <Icon name="folder" />
                          <span>
                            <strong>{directory.name}</strong>
                            <small>
                              {directory.fileCount} tracks
                              {directory.childDirectoryCount
                                ? `, ${directory.childDirectoryCount} folders`
                                : ''}
                            </small>
                          </span>
                        </button>
                      }
                    />
                  ))
                )}
              </aside>

              <section className="player-file-explorer-files">
                <div className="player-file-explorer-section-title">
                  {query ? 'Search Results' : browserPath || 'Library Root'}
                </div>
                {itemsLoading ? (
                  <Message info>Loading audio files...</Message>
                ) : items.length === 0 ? (
                  <Message info>
                    {query && query.length < 2
                      ? 'Type at least two characters to search.'
                      : 'No local audio files found here.'}
                  </Message>
                ) : (
                  <Table compact selectable>
                    <Table.Header>
                      <Table.Row>
                        <Table.HeaderCell>Track</Table.HeaderCell>
                        <Table.HeaderCell>Location</Table.HeaderCell>
                        <Table.HeaderCell collapsing>Copies</Table.HeaderCell>
                        <Table.HeaderCell collapsing>Action</Table.HeaderCell>
                      </Table.Row>
                    </Table.Header>
                    <Table.Body>
                      {items.map((item) => (
                        <Table.Row
                          data-testid={`player-file-row-${item.contentId}`}
                          key={`${item.contentId}-${item.path}`}
                          onDoubleClick={() => playAndClose(item)}
                        >
                          <Table.Cell>
                            <strong>{item.fileName || item.contentId}</strong>
                            <div className="player-picker-meta">
                              {item.mediaKind || 'Audio'}
                              {item.bytes ? ` - ${Math.round(item.bytes / 1024 / 1024)} MB` : ''}
                            </div>
                          </Table.Cell>
                          <Table.Cell>
                            <span className="player-file-path">{item.path}</span>
                          </Table.Cell>
                          <Table.Cell collapsing>
                            {item.duplicateCount > 1 ? item.duplicateCount : ''}
                          </Table.Cell>
                          <Table.Cell collapsing>
                            <Popup
                              content="Play this local file in the browser player."
                              trigger={
                                <Button
                                  aria-label={`Play ${item.fileName || item.contentId}`}
                                  data-testid={`player-play-file-${item.contentId}`}
                                  icon
                                  onClick={() => playAndClose(item)}
                                  size="small"
                                  title={`Play ${item.fileName || item.contentId}`}
                                >
                                  <Icon name="play" />
                                </Button>
                              }
                            />
                            <Popup content="Place this file next in the playback queue." trigger={
                              <Button aria-label={`Play ${item.fileName || 'file'} next`} disabled={!onPlayNext} icon onClick={() => onPlayNext(item)} size="small"><Icon name="level down alternate" /></Button>
                            } />
                          </Table.Cell>
                        </Table.Row>
                      ))}
                    </Table.Body>
                  </Table>
                )}
                <div className="player-file-explorer-pager">
                  <Popup
                    content="Move to the previous page of files in this folder or search."
                    trigger={
                      <Button
                        disabled={browserOffset === 0 || itemsLoading}
                        onClick={() =>
                          setBrowserOffset(Math.max(0, browserOffset - playerBrowserPageSize))
                        }
                        size="small"
                      >
                        <Icon name="angle left" />
                        Previous
                      </Button>
                    }
                  />
                  <Popup
                    content="Move to the next page of files in this folder or search."
                    trigger={
                      <Button
                        disabled={!browserHasMore || itemsLoading}
                        onClick={() =>
                          setBrowserOffset(browserOffset + playerBrowserPageSize)
                        }
                        size="small"
                      >
                        Next
                        <Icon name="angle right" />
                      </Button>
                    }
                  />
                </div>
              </section>
            </div>
          </div>
        </Modal.Content>
        <Modal.Actions>
          <Popup
            content="Close the local file browser without changing playback."
            trigger={<Button onClick={() => setFilesOpen(false)}>Close</Button>}
          />
        </Modal.Actions>
      </Modal>
    </div>
  );
};

const PlayerVisualTile = ({
  audioElement,
  current,
  mode,
  onModeChange,
  onTileModeChange,
  tileMode,
}) => {
  const tileModes = ['art', 'butterchurn', 'native-webgl2', 'native-webgpu', 'spectrum', 'scope'];
  const visualizerTileModes = ['butterchurn', 'native-webgl2', 'native-webgpu'];
  const tileModeLabels = {
    art: 'album art',
    butterchurn: 'Butterchurn',
    'native-webgl2': 'MilkDrop3 WebGL2',
    'native-webgpu': 'MilkDrop3 WebGPU',
    scope: 'signal scope',
    spectrum: 'spectrum bars',
  };
  const tileModeIcons = {
    butterchurn: 'magic',
    'native-webgl2': 'microchip',
    'native-webgpu': 'bolt',
    scope: 'signal',
    spectrum: 'chart bar',
  };
  const tileRef = useRef(null);
  const [visualizerRevision, setVisualizerRevision] = useState(0);
  const title = current?.title || current?.fileName || 'slskdN';
  const artist = current?.artist || '';
  const initials = (artist || title)
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join('')
    .toUpperCase() || 'N';
  const artworkUrl = current?.artworkUrl;
  const normalizedTileMode = tileModes.includes(tileMode) ? tileMode : 'art';
  const effectiveTileMode = mode === 'off' && visualizerTileModes.includes(normalizedTileMode)
    ? 'art'
    : normalizedTileMode;
  const showingVisualizer = visualizerTileModes.includes(effectiveTileMode);
  const showingAnalyzer = ['spectrum', 'scope'].includes(effectiveTileMode);
  const nextTileMode = tileModes[
    (tileModes.indexOf(effectiveTileMode) + 1) % tileModes.length
  ];
  const visualizerDisplayMode = mode === 'off' ? 'inline' : mode;
  const preferredVisualizerMode = visualizerTileModes.includes(normalizedTileMode)
    ? normalizedTileMode
    : readStoredVisualizerEngineTileMode();
  const setTileMode = (nextMode) => {
    onTileModeChange(nextMode);
    if (visualizerTileModes.includes(nextMode) && mode === 'off') {
      onModeChange('inline');
    }
    if (visualizerTileModes.includes(nextMode)) {
      setVisualizerRevision((revision) => revision + 1);
    }
  };
  const switchTileMode = (event, nextMode) => {
    event.stopPropagation();
    setTileMode(nextMode);
  };
  const showVisualizerWindow = (event) => {
    event.stopPropagation();
    if (!showingVisualizer) {
      onTileModeChange(preferredVisualizerMode);
    }
    onModeChange('fullwindow');
  };
  const showVisualizerFullscreen = async (event) => {
    event.stopPropagation();
    if (!showingVisualizer) {
      onTileModeChange(preferredVisualizerMode);
    }
    if (tileRef.current?.requestFullscreen) {
      try {
        await tileRef.current.requestFullscreen();
      } catch {
        // Keep the visualizer in fullscreen layout even if the browser denies the request.
      }
    }
    onModeChange('fullscreen');
  };
  const handleTileActivate = () => setTileMode(nextTileMode);

  return (
    <div className="player-visual-tile">
      <Popup
        content={
          `Show ${tileModeLabels[nextTileMode]} in this square.`
        }
        trigger={
          <div
            aria-label={
              `Show ${tileModeLabels[nextTileMode]} in player visual tile`
            }
            role="button"
            className="player-visual-stage"
            data-testid="player-visual-tile"
            onKeyDown={(event) => {
              if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                handleTileActivate();
              }
            }}
            onClick={handleTileActivate}
            ref={tileRef}
            tabIndex={0}
          >
            {showingVisualizer ? (
              <React.Suspense fallback={
                <span className="player-visualizer-loading" role="status">Loading visualizer</span>
              }>
                <Visualizer
                  audioElement={audioElement}
                  compactControls
                  engineOverride={normalizedTileMode}
                  key={`${normalizedTileMode}-${visualizerRevision}`}
                  mode={visualizerDisplayMode}
                  onEngineChange={onTileModeChange}
                  onModeChange={onModeChange}
                />
              </React.Suspense>
            ) : showingAnalyzer ? (
              <SpectrumAnalyzer
                audioElement={audioElement}
                className="player-visualizer-fallback"
                mode={normalizedTileMode}
              />
            ) : (
              <span className="player-album-art" data-testid="player-album-art">
                {artworkUrl ? (
                  <img alt="" src={artworkUrl} />
                ) : (
                  <>
                    <span className="player-album-art-glow" />
                    <span className="player-album-art-mark">{initials}</span>
                  </>
                )}
              </span>
            )}
            <span className="player-visual-affordance">
              <Icon name={showingVisualizer ? 'magic' : (showingAnalyzer ? 'chart bar' : 'image outline')} />
            </span>
          </div>
        }
      />
      <div className="player-visual-tile-controls" onClick={(event) => event.stopPropagation()}>
        {['spectrum', 'scope', 'butterchurn', 'native-webgl2', 'native-webgpu'].map((option) => (
          <Popup
            content={`Show ${tileModeLabels[option]}.`}
            key={option}
            trigger={
              <Button
                aria-label={`Show ${tileModeLabels[option]}`}
                active={effectiveTileMode === option}
                data-testid={`player-visual-tile-mode-${option}`}
                icon
                onClick={(event) => switchTileMode(event, option)}
                size="mini"
              >
                <Icon name={tileModeIcons[option]} />
              </Button>
            }
          />
        ))}
        <Popup
          content="Maximize the visualizer to the browser window."
          trigger={
            <Button
              aria-label="Maximize visualizer to browser window"
              data-testid="player-visual-tile-fullwindow"
              icon
              onClick={showVisualizerWindow}
              size="mini"
            >
              <Icon name="expand arrows alternate" />
            </Button>
          }
        />
        <Popup
          content="Maximize the visualizer to fullscreen."
          trigger={
            <Button
              aria-label="Maximize visualizer to fullscreen"
              data-testid="player-visual-tile-fullscreen"
              icon
              onClick={showVisualizerFullscreen}
              size="mini"
            >
              <Icon name="expand" />
            </Button>
          }
        />
      </div>
    </div>
  );
};

const PlayerAnalyzerTile = ({ audioElement, mode, onModeChange }) => {
  const nextMode = { off: 'spectrum', spectrum: 'scope', scope: 'off' }[mode];
  const label = {
    off: 'Analyzer off',
    spectrum: 'Spectrum bars',
    scope: 'Signal scope',
  }[mode];
  const nextLabel = {
    off: 'turn off the analyzer',
    spectrum: 'show spectrum bars',
    scope: 'show signal scope',
  }[nextMode];

  return (
    <Popup
      content={`Click to ${nextLabel}.`}
      trigger={
        <div
          aria-label={`Click to ${nextLabel}`}
          className="player-analyzer-tile"
          data-testid="player-analyzer-tile"
          onClick={() => onModeChange(nextMode)}
          onKeyDown={(event) => {
            if (event.key === 'Enter' || event.key === ' ') {
              event.preventDefault();
              onModeChange(nextMode);
            }
          }}
          role="button"
          tabIndex={0}
        >
          <div className="player-analyzer-label">{label}</div>
          {mode !== 'off' ? (
            <SpectrumAnalyzer
              audioElement={audioElement}
              className="player-spectrum-switchable"
              mode={mode}
            />
          ) : null}
          <span className="player-analyzer-affordance">
            <Icon name={{ off: 'power off', spectrum: 'signal', scope: 'chart bar' }[mode]} />
          </span>
        </div>
      }
    />
  );
};

const PlayerBar = () => {
  const navigate = useNavigate();
  const audioRef = useRef(null);
  const fadeAudioRef = useRef(null);
  const renderedPrimaryAudioRef = useRef(null);
  const renderedSecondaryAudioRef = useRef(null);
  const lastSourceRef = useRef('');
  const playerBarRef = useRef(null);
  const scrobbledRef = useRef('');
  const playedSecondsRef = useRef(0);
  const playingNowSentRef = useRef(false);
  const pipRef = useRef({ data: null, raf: null, timer: null, win: null });
  const fadeTimeoutRef = useRef(null);
  const fadeOutgoingRef = useRef(null);
  const fadeRequestRef = useRef(0);
  const playRequestRef = useRef(0);
  const crossfadeStartedRef = useRef(null);
  const localFileSequenceRef = useRef(0);
  const {
    clearQueue,
    clear,
    current,
    followingParty,
    history,
    moveQueueItem,
    next,
    queue,
    repeatMode,
    previous,
    playNext,
    queueItems,
    removeFromQueue,
    setRepeatMode,
    setShuffle,
    setAudioElement,
    setPlaybackPosition,
    shuffle,
    playItem,
    playerVisible,
  } = usePlayer();
  const [localMuted, setLocalMuted] = useState(() =>
    readStoredBoolean(localMuteStorageKey),
  );
  const [collapsed, setCollapsed] = useState(() => {
    // Auto-hide by default: an unset preference collapses the player so it
    // doesn't dominate the layout before the user has ever played anything.
    const stored = getLocalStorageItem(collapsedStorageKey);
    return stored === null ? true : stored === 'true';
  });
  const [playing, setPlaying] = useState(false);
  const playingRef = useRef(false);
  const [playbackStatus, setPlaybackStatus] = useState('idle');
  const [playbackError, setPlaybackError] = useState('');
  const [position, setPosition] = useState(0);
  const renderedPositionRef = useRef(0);
  const [duration, setDuration] = useState(0);
  const [transcodeMode, setTranscodeMode] = useState(false);
  const [transcodeAvailable, setTranscodeAvailable] = useState(false);
  const [transcodeOffset, setTranscodeOffset] = useState(0);
  const [volume, setVolume] = useState(() => {
    const stored = Number(getLocalStorageItem(volumeStorageKey, '1'));
    return Number.isFinite(stored) ? Math.max(0, Math.min(1, stored)) : 1;
  });
  const [playbackRate, setPlaybackRate] = useState(() => {
    const stored = Number(getLocalStorageItem(playbackRateStorageKey, '1'));
    return [0.75, 1, 1.25, 1.5, 2].includes(stored) ? stored : 1;
  });
  const [advancedOpen, setAdvancedOpen] = useState(false);
  const [outputDevices, setOutputDevices] = useState([]);
  const [outputDeviceId, setOutputDeviceId] = useState('default');
  const outputDeviceIdRef = useRef('default');
  const [outputSwitching, setOutputSwitching] = useState(false);
  const [outputChoosing, setOutputChoosing] = useState(false);
  const outputSwitchingRef = useRef(false);
  const outputSwitchPromiseRef = useRef(Promise.resolve());
  const fileInputRef = useRef(null);
  const localObjectUrlsRef = useRef(new Set());
  const selectedItemRef = useRef(current);
  const autoplayRef = useRef(false);
  const remountPositionRef = useRef(null);
  const transcodeRequestRef = useRef(0);

  const closePictureInPicture = useCallback(() => {
    const { raf, timer, win } = pipRef.current;
    pipRef.current = { data: null, raf: null, timer: null, win: null };
    if (!win) return;
    if (raf !== null) win.cancelAnimationFrame(raf);
    if (timer !== null) win.clearTimeout(timer);
    if (!win.closed) win.close();
  }, []);

  useEffect(() => () => {
    if (fadeTimeoutRef.current) window.clearTimeout(fadeTimeoutRef.current);
    closePictureInPicture();
    localObjectUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
    localObjectUrlsRef.current.clear();
  }, [closePictureInPicture]);

  useEffect(() => {
    if (!playerVisible) closePictureInPicture();
  }, [closePictureInPicture, playerVisible]);

  useEffect(() => {
    const referenced = new Set([...queue, ...history, current].map((item) => item?.streamUrl));
    localObjectUrlsRef.current.forEach((url) => {
      if (referenced.has(url)) return;
      URL.revokeObjectURL(url);
      localObjectUrlsRef.current.delete(url);
    });
  }, [current, history, queue]);
  const [visualizerMode, setVisualizerMode] = useState(() =>
    readStoredBoolean(visualizerStorageKey) ? 'inline' : 'off',
  );
  const [visualTileMode, setVisualTileMode] = useState(readStoredTileMode);
  const [analyzerMode, setAnalyzerMode] = useState(readStoredAnalyzerMode);
  const [eqPanelOpen, setEqPanelOpen] = useState(() =>
    readStoredBoolean(eqPanelStorageKey),
  );
  const [lyricsOpen, setLyricsOpen] = useState(() =>
    readStoredBoolean(lyricsStorageKey),
  );
  const [karaokeEnabled, setKaraokeEnabledState] = useState(() =>
    readStoredBoolean(karaokeStorageKey),
  );
  const [crossfadeEnabled, setCrossfadeEnabled] = useState(() =>
    readStoredBoolean(crossfadeStorageKey),
  );
  const [listenBrainzToken, setListenBrainzTokenState] = useState(() =>
    listenBrainz.getListenBrainzToken(),
  );
  const [integrationsOpen, setIntegrationsOpen] = useState(false);
  const [queueOpen, setQueueOpen] = useState(false);
  const [radioOpen, setRadioOpen] = useState(false);
  const [shelfOpen, setShelfOpen] = useState(false);
  const [statsOpen, setStatsOpen] = useState(false);
  const [externalVisualizerStatus, setExternalVisualizerStatus] = useState(null);
  const [externalVisualizerLoading, setExternalVisualizerLoading] = useState(false);
  const [externalVisualizerLaunching, setExternalVisualizerLaunching] = useState(false);
  const [externalVisualizerMessage, setExternalVisualizerMessage] = useState('');
  const [playerAudioElement, setPlayerAudioElement] = useState(null);
  const [playerRating, setPlayerRatingState] = useState(0);
  const [source, setSource] = useState('');

  const refreshExternalVisualizerStatus = useCallback(() => {
    setExternalVisualizerLoading(true);
    setExternalVisualizerMessage('');

    return externalVisualizer.getExternalVisualizerStatus()
      .then((status) => {
        setExternalVisualizerStatus(status);
        return status;
      })
      .catch(() => {
        setExternalVisualizerStatus(null);
        setExternalVisualizerMessage('External visualizer status is unavailable.');
      })
      .finally(() => {
        setExternalVisualizerLoading(false);
      });
  }, []);

  const launchExternalVisualizer = useCallback(() => {
    setExternalVisualizerLaunching(true);
    setExternalVisualizerMessage('');

    externalVisualizer.launchExternalVisualizer()
      .then((result) => {
        const name = result?.name || externalVisualizerStatus?.name || 'External visualizer';
        setExternalVisualizerMessage(
          result?.started ? `${name} launched.` : result?.error || 'External visualizer did not launch.',
        );
      })
      .catch((error) => {
        setExternalVisualizerMessage(getExternalVisualizerError(error));
      })
      .finally(() => {
        setExternalVisualizerLaunching(false);
      });
  }, [externalVisualizerStatus]);

  const bindAudioElement = useCallback((element) => {
    if (!element && audioRef.current) {
      remountPositionRef.current = audioRef.current.currentTime;
    }
    if (!element && renderedPrimaryAudioRef.current) {
      releaseAudioGraph(renderedPrimaryAudioRef.current);
    }
    renderedPrimaryAudioRef.current = element;
    if (element && audioRef.current !== element) {
      lastSourceRef.current = '';
      autoplayRef.current = playingRef.current;
    }
    audioRef.current = element;
    setPlayerAudioElement(element);
    setAudioElement(element);
  }, [setAudioElement]);

  const bindFadeAudioElement = useCallback((element) => {
    if (!element && renderedSecondaryAudioRef.current) {
      releaseAudioGraph(renderedSecondaryAudioRef.current);
    }
    renderedSecondaryAudioRef.current = element;
    fadeAudioRef.current = element;
  }, []);

  useLayoutEffect(() => {
    const element = playerBarRef.current;
    if (!element) return undefined;

    setPlayerHeightVariable(element);
    if (typeof window.ResizeObserver !== 'function') {
      return undefined;
    }

    const resizeObserver = new window.ResizeObserver(() =>
      setPlayerHeightVariable(element));
    resizeObserver.observe(element);

    return () => resizeObserver.disconnect();
  }, [collapsed, current, eqPanelOpen, lyricsOpen, playerVisible]);

  const playAudio = useCallback(async () => {
    const element = audioRef.current;
    if (!element) return;
    const request = ++playRequestRef.current;
    try {
      await outputSwitchPromiseRef.current;
      if (request !== playRequestRef.current || audioRef.current !== element) return;
      const selectedSinkId = outputDeviceIdRef.current === 'default'
        ? ''
        : outputDeviceIdRef.current;
      const graph = selectedSinkId || crossfadeEnabled
        ? getOrCreateAudioGraph(element)
        : getExistingAudioGraph(element);
      if (selectedSinkId) {
        await graph.ctx.setSinkId(selectedSinkId);
        const latestSinkId = outputDeviceIdRef.current === 'default'
          ? ''
          : outputDeviceIdRef.current;
        if (latestSinkId !== selectedSinkId) await graph.ctx.setSinkId(latestSinkId);
      }
      await resumeAudioGraph(element, false);
      if (request !== playRequestRef.current || audioRef.current !== element) return;
      await element.play();
    } catch (error) {
      if (request === playRequestRef.current && audioRef.current === element) throw error;
    }
  }, [crossfadeEnabled]);

  const stopOutgoingFade = useCallback(() => {
    fadeRequestRef.current += 1;
    if (fadeTimeoutRef.current) {
      window.clearTimeout(fadeTimeoutRef.current);
      fadeTimeoutRef.current = null;
    }
    const outgoing = fadeOutgoingRef.current;
    fadeOutgoingRef.current = null;
    if (!outgoing) return;
    outgoing.pause();
    outgoing.removeAttribute('src');
    outgoing.load();
    if (audioRef.current) {
      setOutputGain(outgoing, 1);
      setOutputGain(audioRef.current, 1);
    }
  }, []);

  useEffect(() => () => stopOutgoingFade(), [stopOutgoingFade]);

  const pausePlayback = useCallback(() => {
    playRequestRef.current += 1;
    stopOutgoingFade();
    audioRef.current?.pause();
    playingRef.current = false;
    setPlaying(false);
    setPlaybackStatus((status) => status === 'ended' || status === 'error' ? status : 'paused');
    nowPlaying.clearNowPlaying().catch(() => {});
  }, [stopOutgoingFade]);

  const tryPlay = useCallback(() => {
    setPlaybackError('');
    setPlaybackStatus('loading');
    playAudio().catch(() => {
      setPlaybackStatus('error');
      setPlaybackError('Playback could not start. Check the file or try again.');
    });
  }, [playAudio]);

  const startTranscode = useCallback(async (seconds = 0, autoPlay = true) => {
    if (!current?.contentId || current.contentId.startsWith('local:')) return;
    const requestId = ++transcodeRequestRef.current;
    setPlaybackError('');
    setPlaybackStatus(autoPlay ? 'loading' : 'paused');
    try {
      const ticket = await streaming.createStreamTicket(current.contentId);
      const response = await streaming.getPlaybackInfo(current.contentId);
      if (requestId !== transcodeRequestRef.current) return;
      setDuration(Number(response.data?.durationSeconds) || 0);
      setTranscodeMode(true);
      setTranscodeOffset(seconds);
      setPlaybackPosition(seconds);
      renderedPositionRef.current = seconds;
      setPosition(seconds);
      autoplayRef.current = autoPlay;
      setSource(streaming.buildTranscodedStreamUrl(current.contentId, ticket, seconds));
    } catch {
      if (requestId !== transcodeRequestRef.current) return;
      setPlaybackStatus('error');
      setPlaybackError('Decoding could not start. The server may be busy or FFmpeg may be unavailable.');
    }
  }, [current, setPlaybackPosition]);

  const seekTo = useCallback((seconds) => {
    if (!audioRef.current || !Number.isFinite(seconds)) return;
    const target = Math.max(0, Math.min(duration > 0 ? duration : Number.MAX_SAFE_INTEGER, seconds));
    stopOutgoingFade();
    crossfadeStartedRef.current = null;
    if (transcodeMode) {
      startTranscode(target, playingRef.current);
      return;
    }
    audioRef.current.currentTime = target;
    setPlaybackPosition(target);
    renderedPositionRef.current = target;
    setPosition(target);
  }, [duration, setPlaybackPosition, startTranscode, stopOutgoingFade, transcodeMode]);

  const seekBy = useCallback((seconds) => {
    if (!audioRef.current) return;
    seekTo(transcodeOffset + audioRef.current.currentTime + seconds);
  }, [seekTo, transcodeOffset]);

  const previousTrack = useCallback(() => {
    if (history.length === 0) {
      seekTo(0);
      return;
    }
    previous();
  }, [history.length, previous, seekTo]);

  useEffect(() => {
    if (!playerAudioElement) return;
    const safeVolume = Number.isFinite(volume) ? Math.max(0, Math.min(1, volume)) : 1;
    [audioRef.current, fadeAudioRef.current].filter(Boolean).forEach((element) => {
      element.volume = safeVolume;
    });
    setLocalStorageItem(volumeStorageKey, String(safeVolume));
  }, [playerAudioElement, volume]);

  useEffect(() => {
    if (!playerAudioElement) return;
    const safeRate = [0.75, 1, 1.25, 1.5, 2].includes(playbackRate) ? playbackRate : 1;
    [audioRef.current, fadeAudioRef.current].filter(Boolean).forEach((element) => {
      element.playbackRate = safeRate;
    });
    setLocalStorageItem(playbackRateStorageKey, String(safeRate));
  }, [playbackRate, playerAudioElement]);

  useEffect(() => {
    const AudioCtx = window.AudioContext || window.webkitAudioContext;
    if (!advancedOpen || !navigator.mediaDevices?.enumerateDevices || !AudioCtx?.prototype?.setSinkId) return undefined;
    let cancelled = false;
    const refreshOutputs = () => {
      navigator.mediaDevices.enumerateDevices().then((devices) => {
        if (!cancelled) setOutputDevices(devices.filter((device) =>
          device.kind === 'audiooutput' && device.deviceId !== 'default'));
      }).catch(() => {
        if (!cancelled) setOutputDevices([]);
      });
    };
    refreshOutputs();
    navigator.mediaDevices.addEventListener?.('devicechange', refreshOutputs);
    return () => {
      cancelled = true;
      navigator.mediaDevices.removeEventListener?.('devicechange', refreshOutputs);
    };
  }, [advancedOpen]);

  useEffect(() => {
    if (!playerAudioElement) return;
    [audioRef.current, fadeAudioRef.current].filter(Boolean).forEach((element) => {
      element.muted = localMuted;
    });
    setLocalStorageItem(localMuteStorageKey, localMuted ? 'true' : 'false');
  }, [localMuted, playerAudioElement]);

  useEffect(() => {
    setLocalStorageItem(collapsedStorageKey, collapsed ? 'true' : 'false');
  }, [collapsed]);

  useEffect(() => {
    if (!crossfadeEnabled) stopOutgoingFade();
  }, [crossfadeEnabled, stopOutgoingFade]);

  useEffect(() => {
    if (!crossfadeEnabled || !audioRef.current || audioRef.current.paused) return;
    resumeAudioGraph(audioRef.current).catch(() => {
      setPlaybackError('Could not prepare crossfade audio.');
    });
  }, [crossfadeEnabled]);

  useEffect(() => {
    document.documentElement.classList.toggle('player-collapsed', collapsed);
    return () => {
      document.documentElement.classList.remove('player-collapsed');
    };
  }, [collapsed]);

  useEffect(() => {
    setLocalStorageItem(
      visualizerStorageKey,
      visualizerMode !== 'off' ? 'true' : 'false',
    );
  }, [visualizerMode]);

  useEffect(() => {
    setLocalStorageItem(visualTileStorageKey, visualTileMode);
  }, [visualTileMode]);

  useEffect(() => {
    setLocalStorageItem(analyzerModeStorageKey, analyzerMode);
  }, [analyzerMode]);

  useEffect(() => {
    if (integrationsOpen) {
      refreshExternalVisualizerStatus();
    }
  }, [integrationsOpen, refreshExternalVisualizerStatus]);

  useEffect(() => {
    setLocalStorageItem(eqPanelStorageKey, eqPanelOpen ? 'true' : 'false');
  }, [eqPanelOpen]);

  useEffect(() => {
    setLocalStorageItem(lyricsStorageKey, lyricsOpen ? 'true' : 'false');
  }, [lyricsOpen]);

  useEffect(() => {
    setLocalStorageItem(
      karaokeStorageKey,
      karaokeEnabled ? 'true' : 'false',
    );
    if (playerAudioElement) {
      [playerAudioElement, fadeOutgoingRef.current].filter(Boolean).forEach((element) => {
        setKaraokeEnabled(element, karaokeEnabled);
        if (!element.paused) {
          resumeAudioGraph(element, false).catch(() => {
            setPlaybackError('Could not enable vocal reduction for this track.');
          });
        }
      });
    }
  }, [karaokeEnabled, playerAudioElement]);

  useEffect(() => {
    setLocalStorageItem(
      crossfadeStorageKey,
      crossfadeEnabled ? 'true' : 'false',
    );
  }, [crossfadeEnabled]);

  const toggleVisualizer = () => {
    setVisualizerMode((mode) => {
      if (mode === 'off') {
        setVisualTileMode(readStoredVisualizerEngineTileMode());
        return 'inline';
      }
      return 'off';
    });
  };

  useEffect(() => {
    setPlayerRatingState(getPlayerRating(current));
  }, [current]);

  const updatePlayerRating = (rating) => {
    const nextRating = setPlayerRating(current, rating);
    setPlayerRatingState(nextRating);
    upsertDiscoveryShelfItem(current, nextRating);
  };

  const openRadioSearch = (query) => {
    setRadioOpen(false);
    navigate(buildPlayerRadioSearchPath(query));
  };

  const togglePlayback = useCallback(() => {
    if (!audioRef.current || !current) return;
    if (playing) {
      pausePlayback();
    } else {
      tryPlay();
    }
  }, [current, pausePlayback, playing, tryPlay]);

  useEffect(() => {
    let cancelled = false;

    if (!current) {
      selectedItemRef.current = null;
      transcodeRequestRef.current += 1;
      playRequestRef.current += 1;
      stopOutgoingFade();
      setSource('');
      setPlaybackStatus('idle');
      localObjectUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
      localObjectUrlsRef.current.clear();
      return undefined;
    }

    if (selectedItemRef.current !== current) {
      selectedItemRef.current = current;
      transcodeRequestRef.current += 1;
      playRequestRef.current += 1;
      autoplayRef.current = !current.startPaused;
      remountPositionRef.current = null;
      setPlaybackStatus('loading');
      setTranscodeMode(false);
      setTranscodeAvailable(false);
      setTranscodeOffset(0);
      setDuration(0);
      const startingPosition = current.positionSeconds || 0;
      setPlaybackPosition(startingPosition);
      renderedPositionRef.current = startingPosition;
      setPosition(startingPosition);
    }
    setSource('');

    if (current.streamUrl) {
      setSource(current.streamUrl);
      return undefined;
    }

    if (!current.contentId) {
      setSource('');
      return undefined;
    }

    streaming
      .createStreamTicket(current.contentId)
      .then((ticket) => {
        if (!cancelled) {
          setSource(ticket
            ? streaming.buildTicketedStreamUrl(current.contentId, ticket)
            : streaming.buildDirectStreamUrl(current.contentId));
        }
      })
      .catch(() => {
        if (!cancelled) setSource(streaming.buildDirectStreamUrl(current.contentId));
      });

    return () => {
      cancelled = true;
    };
  }, [current, setPlaybackPosition, stopOutgoingFade]);

  useEffect(() => {
    const handleKeyDown = (event) => {
      const action = getPlayerShortcutAction(event);
      if (!action || !current) return;

      event.preventDefault();

      if (action === 'togglePlayback') {
        togglePlayback();
      } else if (action === 'seekBackward') {
        seekBy(-15);
      } else if (action === 'seekForward') {
        seekBy(30);
      } else if (action === 'previous') {
        previousTrack();
      } else if (action === 'next') {
        next();
      } else if (action === 'toggleMute') {
        setLocalMuted((muted) => !muted);
      } else if (action === 'toggleEqualizer') {
        setEqPanelOpen((open) => !open);
      } else if (action === 'toggleLyrics') {
        setLyricsOpen((open) => !open);
      } else if (action === 'toggleVisualizer') {
        toggleVisualizer();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [
    current,
    next,
    previousTrack,
    seekBy,
    togglePlayback,
    toggleVisualizer,
  ]);

  useEffect(() => {
    if (!audioRef.current || !source) return;
    const previousSource = lastSourceRef.current;
    if (previousSource === source) return;
    playRequestRef.current += 1;
    stopOutgoingFade();
    const active = audioRef.current;
    const standby = fadeAudioRef.current;
    if (crossfadeEnabled && (window.AudioContext || window.webkitAudioContext) &&
        !transcodeMode && autoplayRef.current &&
        previousSource && !active.paused && standby) {
      fadeOutgoingRef.current = active;
      const fadeRequest = ++fadeRequestRef.current;
      standby.src = source;
      standby.muted = localMuted;
      standby.volume = volume;
      standby.playbackRate = playbackRate;
      standby.load();
      setOutputGain(standby, 0);
      audioRef.current = standby;
      fadeAudioRef.current = active;
      setAudioElement(standby);
      setPlayerAudioElement(standby);
      autoplayRef.current = false;
      playAudio().then(() => {
        if (fadeRequest !== fadeRequestRef.current || audioRef.current !== standby) return;
        const remainingMediaSeconds = Number.isFinite(active.duration)
          ? Math.max(0, active.duration - active.currentTime)
          : 5;
        const fadeDurationSeconds = Math.max(0.1, Math.min(5, remainingMediaSeconds) / playbackRate);
        fadeOutputGain(active, 1, 0, fadeDurationSeconds);
        fadeOutputGain(standby, 0, 1, fadeDurationSeconds);
        fadeTimeoutRef.current = window.setTimeout(() => {
          if (fadeRequest !== fadeRequestRef.current) return;
          active.pause();
          active.removeAttribute('src');
          active.load();
          setOutputGain(active, 1);
          fadeOutgoingRef.current = null;
          fadeTimeoutRef.current = null;
        }, fadeDurationSeconds * 1000 + 200);
      }).catch(() => {
        if (fadeRequest !== fadeRequestRef.current) return;
        stopOutgoingFade();
        audioRef.current = active;
        fadeAudioRef.current = standby;
        setAudioElement(active);
        setPlayerAudioElement(active);
        standby.removeAttribute('src');
        standby.load();
        setPlaybackStatus('error');
        setPlaybackError('The next track could not start.');
      });
    } else {
      if (standby) standby.pause();
      active.src = source;
      setOutputGain(active, 1);
      active.load();
      if (autoplayRef.current) {
        autoplayRef.current = false;
        tryPlay();
      } else {
        setPlaybackStatus('paused');
      }
    }
    lastSourceRef.current = source;
  }, [crossfadeEnabled, localMuted, playbackRate, playAudio, playerAudioElement, setAudioElement, source, stopOutgoingFade, transcodeMode, tryPlay, volume]);

  useEffect(() => {
    scrobbledRef.current = '';
    playedSecondsRef.current = 0;
    playingNowSentRef.current = false;
  }, [current]);

  useEffect(() => {
    const audioElement = playerAudioElement;
    if (!audioElement || !current) return undefined;

    let lastPosition = audioElement.currentTime;
    const resetPosition = () => { lastPosition = audioElement.currentTime; };
    const handleTimeUpdate = () => {
      const mediaPosition = audioElement.currentTime;
      if (!audioElement.paused && !audioElement.seeking) {
        playedSecondsRef.current += Math.max(0, mediaPosition - lastPosition);
      }
      lastPosition = mediaPosition;
      const trackDuration = transcodeMode ? duration : audioElement.duration;
      const scrobbleDuration = Number.isFinite(trackDuration)
        ? trackDuration
        : 0;
      const threshold = scrobbleDuration > 0
        ? Math.min(scrobbleDuration / 2, 240)
        : 240;
      const scrobbleKey = `${current.contentId}:${current.title}`;

      if (playedSecondsRef.current >= threshold && scrobbledRef.current !== scrobbleKey) {
        scrobbledRef.current = scrobbleKey;
        recordLocalPlay(current);
        listenBrainz.submitListen('single', current).catch(() => {});
      }
    };

    audioElement.addEventListener('play', resetPosition);
    audioElement.addEventListener('seeking', resetPosition);
    audioElement.addEventListener('seeked', resetPosition);
    audioElement.addEventListener('timeupdate', handleTimeUpdate);
    return () => {
      audioElement.removeEventListener('play', resetPosition);
      audioElement.removeEventListener('seeking', resetPosition);
      audioElement.removeEventListener('seeked', resetPosition);
      audioElement.removeEventListener('timeupdate', handleTimeUpdate);
    };
  }, [current, duration, playerAudioElement, transcodeMode]);

  const openPictureInPicture = async () => {
    if (!audioRef.current || !window.documentPictureInPicture) return;

    closePictureInPicture();
    const graph = await resumeAudioGraph(audioRef.current);
    if (!graph) return;

    const pipWindow = await window.documentPictureInPicture.requestWindow({
      height: 220,
      width: 360,
    });
    pipWindow.document.body.style.margin = '0';
    pipWindow.document.body.style.background = '#050608';
    const canvas = pipWindow.document.createElement('canvas');
    canvas.style.height = '100%';
    canvas.style.width = '100%';
    pipWindow.document.body.appendChild(canvas);
    pipRef.current.win = pipWindow;
    pipWindow.addEventListener('pagehide', () => {
      if (pipRef.current.win === pipWindow) closePictureInPicture();
    }, { once: true });

    const draw = () => {
      if (pipRef.current.win !== pipWindow) return;
      if (pipWindow.closed) {
        closePictureInPicture();
        return;
      }
      pipRef.current.raf = null;
      pipRef.current.timer = null;
      const width = Math.max(1, pipWindow.innerWidth);
      const height = Math.max(1, pipWindow.innerHeight);
      if (canvas.width !== width) canvas.width = width;
      if (canvas.height !== height) canvas.height = height;
      const ctx = canvas.getContext('2d');
      ctx.fillStyle = '#050608';
      ctx.fillRect(0, 0, width, height);
      const activeGraph = audioRef.current && playingRef.current
        ? getOrCreateAudioGraph(audioRef.current)
        : null;
      if (activeGraph) {
        if (pipRef.current.data?.length !== activeGraph.analyser.frequencyBinCount) {
          pipRef.current.data = new Uint8Array(activeGraph.analyser.frequencyBinCount);
        }
        const data = pipRef.current.data;
        activeGraph.analyser.getByteFrequencyData(data);
        const barCount = Math.min(72, Math.max(16, Math.floor(width / 7)));
        const bars = getFrequencyBars(data, barCount);
        const barWidth = width / bars.length;
        bars.forEach((value, index) => {
          const barHeight = (value / 255) * height;
          ctx.fillStyle = `hsl(${264 + (index / bars.length) * 24}, 72%, 68%)`;
          ctx.fillRect(
            index * barWidth,
            height - barHeight,
            Math.max(1, barWidth - 1),
            barHeight,
          );
        });
      }
      pipRef.current.timer = pipWindow.setTimeout(() => {
        if (pipRef.current.win === pipWindow) {
          pipRef.current.raf = pipWindow.requestAnimationFrame(draw);
        }
      }, playingRef.current ? 33 : 250);
    };

    draw();
  };

  useEffect(() => {
    if (!('mediaSession' in navigator) || !window.MediaMetadata) {
      return undefined;
    }
    if (!current) {
      navigator.mediaSession.metadata = null;
      return undefined;
    }

    navigator.mediaSession.metadata = new window.MediaMetadata({
      album: current.album || '',
      artist: current.artist || '',
      artwork: current.artworkUrl ? [{ src: current.artworkUrl }] : [],
      title: current.title || current.fileName || current.contentId,
    });

    const handlers = {
      nexttrack: next,
      pause: pausePlayback,
      play: tryPlay,
      previoustrack: previousTrack,
      seekbackward: (details) => seekBy(-(details?.seekOffset || 15)),
      seekforward: (details) => seekBy(details?.seekOffset || 30),
      seekto: (details) => seekTo(details?.seekTime),
    };

    Object.entries(handlers).forEach(([action, handler]) => {
      try {
        navigator.mediaSession.setActionHandler(action, handler);
      } catch {
        // Some browsers expose a partial Media Session implementation.
      }
    });

    return () => {
      Object.keys(handlers).forEach((action) => {
        try {
          navigator.mediaSession.setActionHandler(action, null);
        } catch {
          // Some browsers expose a partial Media Session implementation.
        }
      });
    };
  }, [current, next, pausePlayback, previousTrack, seekBy, seekTo, tryPlay]);

  useEffect(() => {
    if (!('mediaSession' in navigator)) return;
    navigator.mediaSession.playbackState = current ? (playing ? 'playing' : 'paused') : 'none';
    if (current && duration > 0 && navigator.mediaSession.setPositionState) {
      navigator.mediaSession.setPositionState({
        duration,
        playbackRate,
        position: Math.min(position, duration),
      });
    }
  }, [current, duration, playbackRate, playing, position]);

  const switchOutputDevice = async (deviceId) => {
    if (outputSwitchingRef.current) return;
    outputSwitchingRef.current = true;
    setOutputSwitching(true);
    const sinkId = deviceId === 'default' ? '' : deviceId;
    const previousSinkId = outputDeviceIdRef.current === 'default'
      ? ''
      : outputDeviceIdRef.current;
    const switching = (async () => {
      const elements = [audioRef.current, fadeOutgoingRef.current].filter(Boolean);
      const contexts = elements
        .map((element) => (sinkId
          ? getOrCreateAudioGraph(element)
          : getExistingAudioGraph(element))?.ctx)
        .filter(Boolean);
      if (contexts.some((context) => !context.setSinkId)) {
        throw new Error('Web Audio output selection is unavailable.');
      }
      const results = await Promise.allSettled(
        contexts.map((context) => context.setSinkId(sinkId)),
      );
      if (results.some((result) => result.status === 'rejected')) {
        await Promise.allSettled(
          contexts.map((context) => context.setSinkId(previousSinkId)),
        );
        await Promise.allSettled(
          elements.filter((element) => !element.paused)
            .map((element) => resumeAudioGraph(element, false)),
        );
        throw new Error('Audio output switch failed.');
      }
      outputDeviceIdRef.current = deviceId;
      setOutputDeviceId(deviceId);
      const resumeResults = await Promise.allSettled(
        elements.filter((element) => !element.paused)
          .map((element) => resumeAudioGraph(element, false)),
      );
      if (resumeResults.some((result) => result.status === 'rejected')) {
        setPlaybackError('Audio output changed, but playback could not resume.');
      }
    })();
    outputSwitchPromiseRef.current = switching.catch(() => {});
    try {
      await switching;
    } catch {
      setPlaybackError('Could not switch audio output device.');
    } finally {
      outputSwitchingRef.current = false;
      setOutputSwitching(false);
    }
  };

  const chooseOutputDevice = async () => {
    if (outputChoosing || outputSwitchingRef.current) return;
    setOutputChoosing(true);
    try {
      const selection = navigator.mediaDevices.selectAudioOutput();
      const device = await selection;
      setOutputDevices((devices) => [
        ...devices.filter((listed) => listed.deviceId !== device.deviceId),
        device,
      ]);
      await switchOutputDevice(device.deviceId);
    } catch (error) {
      if (error?.name !== 'AbortError') {
        setPlaybackError('Could not choose an audio output device.');
      }
    } finally {
      setOutputChoosing(false);
    }
  };

  if (!playerVisible) {
    return (
      <div
        className="player-bar player-bar-hidden player-bar-modern"
        ref={playerBarRef}
      >
        <div className="player-hidden-label">
          <Icon name="music" />
          Player hidden
        </div>
        <PlayerToolButton
          content="Show the browser player again at this location."
          aria-label="Show player"
          data-testid="player-show"
          icon="eye"
          onClick={() => setExperiencePreference('playerVisible', true)}
        />
      </div>
    );
  }

  const handleEnded = () => {
    if (crossfadeStartedRef.current === current) {
      setPlaybackStatus('loading');
      return;
    }
    if (repeatMode === 'one' || (repeatMode === 'all' && queue.length === 1 && history.length === 0)) {
      playingNowSentRef.current = false;
      scrobbledRef.current = '';
      playedSecondsRef.current = 0;
      if (transcodeMode) startTranscode(0);
      else {
        audioRef.current.currentTime = 0;
        setPlaybackPosition(0);
        tryPlay();
      }
    } else if (queue.length > 1 || (repeatMode === 'all' && history.length > 0)) {
      next();
    } else {
      playingRef.current = false;
      setPlaying(false);
      setPlaybackStatus('ended');
      nowPlaying.clearNowPlaying().catch(() => {});
    }
  };

  const handleLocalFiles = (event) => {
    const files = Array.from(event.target.files || []);
    if (files.length === 0) return;
    const [file, ...remaining] = files;
    const streamUrl = URL.createObjectURL(file);
    localObjectUrlsRef.current.add(streamUrl);
    const makeItem = (selectedFile, url) => ({
      artist: 'Local file',
      contentId: `local:${Date.now()}:${++localFileSequenceRef.current}`,
      fileName: selectedFile.name,
      streamUrl: url,
      title: selectedFile.name.replace(/\.[^.]+$/u, ''),
    });
    playItem(makeItem(file, streamUrl), { replaceQueue: true });
    queueItems(remaining.map((selectedFile) => {
      const url = URL.createObjectURL(selectedFile);
      localObjectUrlsRef.current.add(url);
      return makeItem(selectedFile, url);
    }));
    event.target.value = '';
  };

  const loadPlaylist = (items) => {
    const [first, ...remaining] = items;
    playItem(first, { replaceQueue: true });
    queueItems(remaining);
  };

  const audioHandlers = {
    onEnded: (event) => {
      if (event.currentTarget === audioRef.current) handleEnded();
    },
    onError: (event) => {
      if (event.currentTarget !== audioRef.current || !current) return;
      const failedElement = event.currentTarget;
      const failedSource = failedElement.currentSrc || failedElement.src;
      playRequestRef.current += 1;
      stopOutgoingFade();
      playingRef.current = false;
      setPlaying(false);
      setPlaybackStatus('error');
      setPlaybackError('This audio could not be decoded or streamed.');
      if (!transcodeMode && !current.contentId.startsWith('local:')) {
        streaming.getPlaybackInfo(current.contentId).then((response) => {
          if (selectedItemRef.current !== current || audioRef.current !== failedElement ||
              (failedElement.currentSrc || failedElement.src) !== failedSource) return;
          setTranscodeAvailable(true);
          setDuration(Number(response.data?.durationSeconds) || 0);
        }).catch(() => {});
      }
      nowPlaying.clearNowPlaying().catch(() => {});
    },
    onLoadedMetadata: (event) => {
      if (event.currentTarget !== audioRef.current) return;
      if (!transcodeMode) setDuration(Number.isFinite(event.currentTarget.duration) ? event.currentTarget.duration : 0);
      if (remountPositionRef.current !== null) {
        event.currentTarget.currentTime = remountPositionRef.current;
        setPlaybackPosition(transcodeOffset + remountPositionRef.current);
        remountPositionRef.current = null;
      } else if (!transcodeMode && current?.positionSeconds > 0) {
        event.currentTarget.currentTime = current.positionSeconds;
        setPlaybackPosition(current.positionSeconds);
      }
    },
    onPause: (event) => {
      if (event.currentTarget !== audioRef.current) return;
      stopOutgoingFade();
      playingRef.current = false;
      setPlaying(false);
      setPlaybackStatus((status) => status === 'ended' || status === 'error' ? status : 'paused');
      nowPlaying.clearNowPlaying().catch(() => {});
    },
    onPlay: (event) => {
      if (event.currentTarget !== audioRef.current) return;
      playingRef.current = true;
      setPlaying(true);
      setPlaybackStatus('playing');
      if (current?.artist && current?.title) {
        nowPlaying.setNowPlaying({ album: current.album, artist: current.artist, title: current.title }).catch(() => {});
        if (!playingNowSentRef.current && listenBrainzToken) {
          playingNowSentRef.current = true;
          listenBrainz.submitListen('playing_now', current).catch(() => {});
        }
      }
    },
    onPlaying: (event) => {
      if (event.currentTarget !== audioRef.current) return;
      playingRef.current = true;
      setPlaying(true);
      setPlaybackStatus('playing');
    },
    onTimeUpdate: (event) => {
      if (event.currentTarget !== audioRef.current) return;
      const nextPosition = transcodeOffset + event.currentTarget.currentTime;
      setPlaybackPosition(nextPosition);
      if (Math.floor(nextPosition) !== Math.floor(renderedPositionRef.current)) {
        renderedPositionRef.current = nextPosition;
        setPosition(nextPosition);
      }
      if (crossfadeEnabled && !transcodeMode && repeatMode !== 'one' &&
          (queue.length > 1 || (repeatMode === 'all' && history.length > 0)) &&
          Number.isFinite(event.currentTarget.duration) && event.currentTarget.duration > 6 &&
          event.currentTarget.currentTime >= event.currentTarget.duration - 5 &&
          crossfadeStartedRef.current !== current) {
        crossfadeStartedRef.current = current;
        next();
      }
    },
    onWaiting: (event) => {
      if (event.currentTarget === audioRef.current && playingRef.current) {
        setPlaybackStatus('buffering');
      }
    },
  };
  const audio = (
    <>
      <audio {...audioHandlers} playsInline preload="metadata" ref={bindAudioElement} />
      <audio {...audioHandlers} playsInline preload="metadata" ref={bindFadeAudioElement} />
    </>
  );
  const playerBadges = getPlayerBadges(current);

  if (collapsed) {
    return (
      <div
        className="player-bar player-bar-collapsed player-bar-modern"
        ref={playerBarRef}
      >
        {audio}
        <div className="player-track player-track-lcd">
          <Icon name="music" />
          <div>
            <div className="player-title">
              {current?.title || 'Player'}
            </div>
            <div className="player-subtitle">
              {current?.artist || 'Ready'}
            </div>
          </div>
        </div>
        <PlayerProgress current={current} duration={duration} onSeek={seekTo} position={position} />
        <div className="player-controls player-control-cluster">
          <PlayerToolButton
            content="Play the previous track or restart this one."
            aria-label="Previous local track"
            disabled={!current}
            icon="step backward"
            onClick={previousTrack}
          />
          <PlayerToolButton
            content={playing ? 'Pause playback.' : 'Play the selected track.'}
            aria-label={playing ? 'Pause local playback' : 'Resume local playback'}
            data-testid="player-collapsed-toggle-playback"
            disabled={!current}
            icon={playing ? 'pause' : 'play'}
            onClick={togglePlayback}
          />
          <PlayerToolButton
            content="Play the next queued track."
            aria-label="Next local track"
            disabled={queue.length < 2}
            icon="step forward"
            onClick={next}
          />
          <PlayerToolButton
            content="Open the playback queue."
            className="player-compact-secondary"
            aria-label="Open playback queue"
            disabled={!current}
            icon="list ol"
            onClick={() => setQueueOpen(true)}
          />
          <PlayerToolButton
            content="Expand the player drawer."
            aria-label="Expand player"
            data-testid="player-expand"
            icon="angle up"
            onClick={() => setCollapsed(false)}
          />
          <PlayerToolButton
            content="Stop playback and hide the player. Use Show player to restore it."
            className="player-compact-secondary"
            aria-label="Hide player"
            data-testid="player-hide"
            icon="eye slash"
            onClick={() => setExperiencePreference('playerVisible', false)}
          />
          <PlayerToolButton
            content={
              localMuted
                ? 'Unmute playback on this device without changing the stream.'
                : 'Mute playback on this device without changing the stream.'
            }
            className="player-compact-secondary"
            aria-label={localMuted ? 'Unmute local playback' : 'Mute local playback'}
            data-testid="player-collapsed-toggle-mute"
            disabled={!current}
            icon={localMuted ? 'volume off' : 'volume up'}
            onClick={() => setLocalMuted((muted) => !muted)}
          />
          <input
            aria-label="Playback volume"
            className="player-volume player-compact-secondary"
            max="1"
            min="0"
            onChange={(event) => setVolume(Number(event.target.value))}
            step="0.01"
            type="range"
            value={volume}
          />
        </div>
        {queueOpen ? <PlayerQueueModal
          current={current}
          history={history}
          onAutoQueueSimilar={queueItems}
          onClearQueue={clearQueue}
          onClose={() => setQueueOpen(false)}
          onLoadPlaylist={loadPlaylist}
          onMove={moveQueueItem}
          onNext={next}
          onPrevious={previousTrack}
          onRemove={removeFromQueue}
          open={queueOpen}
          queue={queue}
        /> : null}
      </div>
    );
  }

  return (
    <div
      className="player-bar player-bar-modern"
      ref={playerBarRef}
    >
      {audio}
      <div className="player-main-deck">
        <div className="player-display">
          <PlayerVisualTile
            audioElement={playing ? playerAudioElement : null}
            current={current}
            mode={visualizerMode}
            onModeChange={setVisualizerMode}
            onTileModeChange={setVisualTileMode}
            tileMode={visualTileMode}
          />
          <div className="player-now-playing">
            <div className="player-track">
              <div>
                <div className="player-eyebrow">
                  {current ? ({
                    buffering: 'Buffering',
                    ended: 'Finished',
                    error: 'Playback error',
                    loading: 'Loading',
                    paused: 'Paused',
                    playing: 'Now playing',
                  }[playbackStatus] || 'Ready') : 'Ready'}
                </div>
                <div className="player-title">
                  {current?.title || 'Nothing playing'}
                </div>
                <div className="player-subtitle">
                  {current?.artist || 'Pick a collection or local audio file'}
                  {current?.album ? ` | ${current.album}` : ''}
                  {followingParty ? ` | Following ${followingParty.hostPeerId}` : ''}
                </div>
                {current ? (
                  <div className="player-now-playing-meta">
                    <div className="player-now-playing-badges">
                      {playerBadges.map((badge) => (
                        <Label
                          className="player-now-playing-badge"
                          color={badge.color}
                          data-testid={`player-badge-${badge.key}`}
                          key={badge.key}
                          size="mini"
                          title={badge.title}
                        >
                          <Icon name={badge.icon} />
                          {badge.text}
                        </Label>
                      ))}
                    </div>
                    <PlayerRatingControls
                      current={current}
                      onChange={updatePlayerRating}
                      rating={playerRating}
                    />
                  </div>
                ) : null}
              </div>
            </div>
            <div className="player-display-analyzers">
              <PlayerAnalyzerTile
                audioElement={playing ? playerAudioElement : null}
                mode={analyzerMode}
                onModeChange={setAnalyzerMode}
              />
            </div>
            <PlayerProgress current={current} duration={duration} onSeek={seekTo} position={position} />
            {playbackError ? <Message negative size="mini">{playbackError}</Message> : null}
            {transcodeAvailable && !transcodeMode ? (
              <Popup content="Decode this server library file to MP3 for this playback only. This uses server CPU until playback stops." trigger={
                <Button onClick={() => startTranscode(0)} size="mini" type="button">Decode for playback</Button>
              } />
            ) : null}
          </div>
        </div>

        <div className="player-control-pad">
          <input
            accept="audio/*"
            aria-label="Choose audio files"
            multiple
            onChange={handleLocalFiles}
            ref={fileInputRef}
            style={{ display: 'none' }}
            type="file"
          />
          <div className="player-control-row player-control-row-transport">
            <PlayerToolButton
              content="Go to the previous queue item, or restart the current stream."
              aria-label="Previous local track"
              data-testid="player-previous"
              disabled={!current}
              icon="step backward"
              onClick={previousTrack}
            />
            <PlayerToolButton
              content="Rewind local playback by 15 seconds."
              aria-label="Rewind local playback"
              data-testid="player-rewind"
              disabled={!current}
              icon="backward"
              onClick={() => seekBy(-15)}
            />
            <PlayerToolButton
              content={playing ? 'Pause the current stream.' : 'Resume the current stream.'}
              aria-label={playing ? 'Pause local playback' : 'Resume local playback'}
              className="player-play-button"
              data-testid="player-toggle-playback"
              disabled={!current}
              icon={playing ? 'pause' : 'play'}
              onClick={togglePlayback}
            />
            <PlayerToolButton
              content="Fast-forward local playback by 30 seconds."
              aria-label="Fast-forward local playback"
              data-testid="player-fast-forward"
              disabled={!current}
              icon="forward"
              onClick={() => seekBy(30)}
            />
            <PlayerToolButton
              content="Play the next queue item."
              aria-label="Next local track"
              data-testid="player-next"
              disabled={!current || queue.length < 2}
              icon="step forward"
              onClick={next}
            />
            <PlayerToolButton
              content="Stop playback and clear your now-playing profile status."
              aria-label="Stop local playback"
              data-testid="player-stop"
              disabled={!current}
              icon="stop"
              onClick={clear}
            />
            <PlayerToolButton
              active={shuffle}
              aria-label={shuffle ? 'Disable shuffle' : 'Enable shuffle'}
              content={shuffle ? 'Play upcoming tracks in queue order.' : 'Choose upcoming tracks at random.'}
              icon="shuffle"
              onClick={() => setShuffle((enabled) => !enabled)}
            />
            <PlayerToolButton
              active={repeatMode !== 'off'}
              aria-label={`Repeat ${repeatMode}`}
              content={`Repeat: ${repeatMode}. Click to cycle off, all tracks, and one track.`}
              icon="repeat"
              onClick={() => setRepeatMode((mode) => mode === 'off' ? 'all' : mode === 'all' ? 'one' : 'off')}
            />
            <select
              aria-label="Playback speed"
              onChange={(event) => setPlaybackRate(Number(event.target.value))}
              value={playbackRate}
            >
              {[0.75, 1, 1.25, 1.5, 2].map((rate) => <option key={rate} value={rate}>{rate}×</option>)}
            </select>
          </div>
          <div className="player-control-row">
            <PlayerLauncher
              compact
              onPlayItem={(item) => playItem(item, { replaceQueue: true })}
              onPlayNext={playNext}
            />
            <PlayerToolButton
              aria-label="Open audio files from this device"
              content="Play files from this device in this browser session. They are not uploaded to the server."
              icon="folder open"
              onClick={() => fileInputRef.current?.click()}
            />
            <PlayerToolButton
              active={queueOpen}
              content="Open the playback queue manager with current, upcoming, and recent session tracks."
              aria-label="Open playback queue"
              data-testid="player-open-queue"
              disabled={!current}
              icon="list ol"
              onClick={() => setQueueOpen(true)}
            />
            <PlayerToolButton
              active={localMuted}
              content={
                localMuted
                  ? 'Unmute playback on this device without changing the stream.'
                  : 'Mute playback on this device without changing the stream.'
              }
              aria-label={localMuted ? 'Unmute local playback' : 'Mute local playback'}
              data-testid="player-toggle-mute"
              disabled={!current}
              icon={localMuted ? 'volume off' : 'volume up'}
              onClick={() => setLocalMuted((muted) => !muted)}
            />
            <input
              aria-label="Playback volume"
              className="player-volume"
              max="1"
              min="0"
              onChange={(event) => setVolume(Number(event.target.value))}
              step="0.01"
              type="range"
              value={volume}
            />
            <PlayerToolButton
              active={advancedOpen}
              aria-label={advancedOpen ? 'Hide player tools' : 'Show player tools'}
              content={advancedOpen ? 'Hide audio, discovery, and visual tools.' : 'Show audio, discovery, and visual tools.'}
              icon="ellipsis horizontal"
              onClick={() => setAdvancedOpen((open) => !open)}
            />
            <PlayerToolButton
              content="Stop playback and hide the player. Use Show player to restore it."
              aria-label="Hide player"
              data-testid="player-hide"
              icon="eye slash"
              onClick={() => setExperiencePreference('playerVisible', false)}
            />
            {advancedOpen ? <>
            <PlayerToolButton
              active={visualizerMode !== 'off'}
              content={
                visualizerMode === 'off'
                  ? 'Show the MilkDrop visualizer.'
                  : 'Hide the MilkDrop visualizer.'
              }
              aria-label={
                visualizerMode === 'off'
                  ? 'Show MilkDrop visualizer'
                  : 'Hide MilkDrop visualizer'
              }
              data-testid="player-toggle-visualizer"
              icon="eye"
              onClick={toggleVisualizer}
            />
            <PlayerToolButton
              active={eqPanelOpen}
              content={
                eqPanelOpen
                  ? 'Hide the equalizer panel.'
                  : 'Show the equalizer sliders and presets.'
              }
              aria-label={eqPanelOpen ? 'Hide equalizer' : 'Show equalizer'}
              data-testid="player-toggle-eq"
              icon="sliders horizontal"
              onClick={() => setEqPanelOpen((open) => !open)}
            />
            <PlayerToolButton
              active={lyricsOpen}
              content={
                lyricsOpen
                  ? 'Hide synced lyrics for the current track.'
                  : 'Fetch synced lyrics for the current artist and title from LRCLIB.'
              }
              aria-label={lyricsOpen ? 'Hide lyrics' : 'Show lyrics'}
              data-testid="player-toggle-lyrics"
              disabled={!current}
              icon="align left"
              onClick={() => setLyricsOpen((open) => !open)}
            />
            <PlayerToolButton
              content="Build smart-radio search seeds from the current track without starting network work yet."
              aria-label="Open smart radio seeds"
              data-testid="player-open-radio"
              disabled={!current}
              icon="random"
              onClick={() => setRadioOpen(true)}
            />
            <PlayerToolButton
              content="Show local listening stats recorded in this browser."
              aria-label="Open listening stats"
              data-testid="player-open-listening-stats"
              icon="bar chart"
              onClick={() => setStatsOpen(true)}
            />
            <PlayerToolButton
              active={shelfOpen}
              content="Open the browser-local discovery shelf built from player ratings."
              aria-label="Open discovery shelf"
              data-testid="player-open-discovery-shelf"
              icon="bookmark"
              onClick={() => setShelfOpen(true)}
            />
            </> : null}
          </div>
          {advancedOpen ? <div className="player-control-row">
            {outputDevices.length > 0 || outputDeviceId !== 'default' ? (
              <select
                aria-label="Audio output device"
                disabled={outputChoosing || outputSwitching}
                onChange={(event) => switchOutputDevice(event.target.value)}
                value={outputDeviceId}
              >
                <option value="default">System default</option>
                {outputDeviceId !== 'default' &&
                  !outputDevices.some((device) => device.deviceId === outputDeviceId) ? (
                    <option value={outputDeviceId}>Previously selected output</option>
                  ) : null}
                {outputDevices.map((device, index) => (
                  <option key={device.deviceId || index} value={device.deviceId}>
                    {device.label || `Audio output ${index + 1}`}
                  </option>
                ))}
              </select>
            ) : null}
            {(window.AudioContext || window.webkitAudioContext)?.prototype?.setSinkId &&
              navigator.mediaDevices?.selectAudioOutput ? (
                <PlayerToolButton
                  content="Choose a speaker or headset and allow the browser to play through it."
                  aria-label="Choose audio output"
                  disabled={outputChoosing || outputSwitching}
                  icon="headphones"
                  onClick={chooseOutputDevice}
                />
              ) : null}
            <PlayerToolButton
              content="Collapse the player into a small drawer bar above the footer."
              aria-label="Collapse player"
              data-testid="player-collapse"
              icon="angle down"
              onClick={() => setCollapsed(true)}
            />
            <PlayerToolButton
              active={karaokeEnabled}
              content={
                karaokeEnabled
                  ? 'Turn off center-channel vocal reduction.'
                  : 'Try center-channel vocal reduction for karaoke-style playback.'
              }
              aria-label={karaokeEnabled ? 'Disable karaoke mode' : 'Enable karaoke mode'}
              data-testid="player-toggle-karaoke"
              disabled={!current}
              icon="microphone slash"
              onClick={() => setKaraokeEnabledState((enabled) => !enabled)}
            />
            <PlayerToolButton
              active={crossfadeEnabled}
              content={
                crossfadeEnabled
                  ? 'Disable the five-second fade between queue items.'
                  : 'Enable a five-second fade between queue items.'
              }
              aria-label={crossfadeEnabled ? 'Disable crossfade' : 'Enable crossfade'}
              data-testid="player-toggle-crossfade"
              icon="exchange"
              onClick={() => setCrossfadeEnabled((enabled) => !enabled)}
            />
            <PlayerToolButton
              content="Open a tiny always-on-top spectrum window when this browser supports Document Picture-in-Picture."
              aria-label="Open visualizer picture in picture"
              data-testid="player-document-pip"
              disabled={!current || !window.documentPictureInPicture}
              icon="window restore"
              onClick={openPictureInPicture}
            />
            <PlayerToolButton
              active={listenBrainzToken.length > 0}
              content="Configure ListenBrainz scrobbling for this browser."
              aria-label="Configure ListenBrainz scrobbling"
              data-testid="player-open-integrations"
              icon="cloud upload"
              onClick={() => setIntegrationsOpen(true)}
            />
          </div> : null}
        </div>
      </div>

      <div className="player-expanded-panels">
        <div className="player-panel player-panel-eq" hidden={!eqPanelOpen}>
          <Equalizer
            audioElement={current ? playerAudioElement : null}
            fadeAudioElement={current ? fadeOutgoingRef.current : null}
            onAudioError={setPlaybackError}
          />
        </div>
        <LyricsPane
          audioElement={playerAudioElement}
          current={current}
          visible={lyricsOpen}
        />
      </div>

      <Modal
        className="player-browser-modal player-integrations-modal"
        onClose={() => setIntegrationsOpen(false)}
        open={integrationsOpen}
        size="tiny"
      >
        <Modal.Header>Player Integrations</Modal.Header>
        <Modal.Content>
          <p className="player-modal-copy">
            ListenBrainz submissions are opt-in and the token is kept for this browser session.
          </p>
          <Input
            aria-label="ListenBrainz user token"
            action={
              <Button
                aria-label="Clear ListenBrainz token"
                data-testid="player-clear-listenbrainz-token"
                icon
                onClick={() => {
                  setListenBrainzTokenState('');
                  listenBrainz.setListenBrainzToken('');
                }}
                type="button"
              >
                <Icon name="trash alternate outline" />
              </Button>
            }
            data-testid="player-listenbrainz-token"
            fluid
            icon="cloud upload"
            onChange={(event) => {
              setListenBrainzTokenState(event.target.value);
              listenBrainz.setListenBrainzToken(event.target.value);
            }}
            placeholder="ListenBrainz token"
            size="mini"
            type="password"
            value={listenBrainzToken}
          />
          <div
            className="player-token-save-state"
            data-testid="player-listenbrainz-save-state"
          >
            <Icon name="check circle outline" />
            Token changes are saved automatically for this session.
          </div>
          <div
            className="player-external-visualizer"
            data-testid="player-external-visualizer"
          >
            <div className="player-panel-title">External Visualizer</div>
            <div className="player-external-visualizer-summary">
              <Icon
                name={externalVisualizerStatus?.enabled ? 'desktop' : 'ban'}
              />
              <div>
                <div className="player-external-visualizer-name">
                  {externalVisualizerStatus?.name || 'MilkDrop3'}
                </div>
                <div className="player-external-visualizer-status">
                  {getExternalVisualizerStatusText(
                    externalVisualizerStatus,
                    externalVisualizerLoading,
                  )}
                </div>
                {externalVisualizerStatus?.path ? (
                  <div
                    className="player-external-visualizer-path"
                    title={externalVisualizerStatus.path}
                  >
                    {externalVisualizerStatus.path}
                  </div>
                ) : null}
              </div>
            </div>
            {externalVisualizerMessage ? (
              <Message
                className="player-external-visualizer-message"
                compact
                data-testid="player-external-visualizer-message"
                info={externalVisualizerStatus?.available}
                size="tiny"
                warning={!externalVisualizerStatus?.available}
              >
                {externalVisualizerMessage}
              </Message>
            ) : null}
            <div className="player-external-visualizer-actions">
              <Popup
                content="Start the configured external visualizer on the slskdN host. Use this for MilkDrop3 or another local visualizer that captures system audio."
                trigger={
                  <Button
                    data-testid="player-launch-external-visualizer"
                    disabled={
                      externalVisualizerLaunching ||
                      !externalVisualizerStatus?.enabled ||
                      !externalVisualizerStatus?.available
                    }
                    loading={externalVisualizerLaunching}
                    onClick={launchExternalVisualizer}
                    size="mini"
                    type="button"
                  >
                    <Icon name="external alternate" />
                    Launch
                  </Button>
                }
              />
              <Popup
                content="Refresh the configured external visualizer path and readiness from the server."
                trigger={
                  <Button
                    data-testid="player-refresh-external-visualizer"
                    disabled={externalVisualizerLoading}
                    loading={externalVisualizerLoading}
                    onClick={refreshExternalVisualizerStatus}
                    size="mini"
                    type="button"
                  >
                    <Icon name="refresh" />
                    Refresh
                  </Button>
                }
              />
            </div>
          </div>
        </Modal.Content>
        <Modal.Actions>
          <Popup
            content="Close settings. ListenBrainz token changes have already been saved."
            trigger={
              <Button
                data-testid="player-close-integrations"
                onClick={() => setIntegrationsOpen(false)}
                primary
              >
                <Icon name="check" />
                Done
              </Button>
            }
          />
        </Modal.Actions>
      </Modal>
      {radioOpen ? <PlayerRadioModal
        current={current}
        onClose={() => setRadioOpen(false)}
        onOpenSearch={openRadioSearch}
        open={radioOpen}
      /> : null}
      {queueOpen ? <PlayerQueueModal
        current={current}
        history={history}
        onAutoQueueSimilar={queueItems}
        onClearQueue={clearQueue}
        onClose={() => setQueueOpen(false)}
        onLoadPlaylist={loadPlaylist}
        onMove={moveQueueItem}
        onNext={next}
        onPrevious={previousTrack}
        onRemove={removeFromQueue}
        open={queueOpen}
        queue={queue}
      /> : null}
      {shelfOpen ? <PlayerDiscoveryShelfModal
        onClose={() => setShelfOpen(false)}
        open={shelfOpen}
      /> : null}
      {statsOpen ? <PlayerStatsModal
        onClose={() => setStatsOpen(false)}
        onOpenSearch={(query) => {
          setStatsOpen(false);
          openRadioSearch(query);
        }}
        open={statsOpen}
      /> : null}
      {current && queue.length > 1 ? (
        <div className="player-queue">
          {queue.slice(1, 4).map((item) => (
            <Popup
              content={`Remove ${item.title || item.fileName || item.contentId} from upcoming playback without stopping the current track.`}
              key={item.contentId}
              trigger={
                <button
                  aria-label={`Remove ${item.title || item.fileName || item.contentId} from queue`}
                  className="player-queue-item"
                  onClick={() => removeFromQueue(item.contentId)}
                  type="button"
                >
                  {item.title || item.fileName || item.contentId}
                </button>
              }
            />
          ))}
        </div>
      ) : null}
    </div>
  );
};

export default PlayerBar;
