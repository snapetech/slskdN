import PlayerBar from './PlayerBar';
import React from 'react';
import { PlayerProvider, usePlayer } from './PlayerContext';
import { MemoryRouter } from 'react-router-dom';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { vi } from 'vitest';
import * as externalVisualizer from '../../lib/externalVisualizer';
import * as collectionsAPI from '../../lib/collections';
import * as audioGraph from './audioGraph';
import * as streaming from '../../lib/streaming';
import * as nowPlaying from '../../lib/nowPlaying';
import * as searches from '../../lib/searches';
import * as wishlistAPI from '../../lib/wishlist';

vi.mock('../../lib/nowPlaying', () => ({
  clearNowPlaying: vi.fn(() => Promise.resolve()),
  setNowPlaying: vi.fn(() => Promise.resolve()),
}));

vi.mock('../../lib/collections', () => ({
  createCollection: vi.fn(() => Promise.resolve({ data: { id: 'playlist-created', title: 'Saved queue', type: 'Playlist' } })),
  addCollectionItem: vi.fn(() => Promise.resolve({ data: { id: 'playlist-item' } })),
  deleteCollection: vi.fn(() => Promise.resolve({})),
  browseLibraryItems: vi.fn(({ path = '', query = '' } = {}) => {
    if (query) {
      return Promise.resolve({
        data: {
          breadcrumbs: [{ name: 'Library', path: '' }],
          directories: [],
          duplicatesRemoved: 2,
          files: [
            {
              bytes: 5242880,
              contentId: 'sha256:library',
              duplicateCount: 3,
              fileName: 'Library stream.ogg',
              mediaKind: 'Audio',
              path: 'Downloads/Library stream.ogg',
            },
          ],
          hasMore: false,
          totalDirectories: 0,
          totalFiles: 1,
        },
      });
    }

    if (path === 'Downloads') {
      return Promise.resolve({
        data: {
          breadcrumbs: [
            { name: 'Library', path: '' },
            { name: 'Downloads', path: 'Downloads' },
          ],
          directories: [],
          duplicatesRemoved: 0,
          files: [
            {
              bytes: 5242880,
              contentId: 'sha256:library',
              duplicateCount: 1,
              fileName: 'Library stream.ogg',
              mediaKind: 'Audio',
              path: 'Downloads/Library stream.ogg',
            },
          ],
          hasMore: false,
          totalDirectories: 0,
          totalFiles: 1,
        },
      });
    }

    return Promise.resolve({
      data: {
        breadcrumbs: [{ name: 'Library', path: '' }],
        directories: [
          {
            childDirectoryCount: 2,
            fileCount: 1,
            name: 'Downloads',
            path: 'Downloads',
          },
        ],
        duplicatesRemoved: 0,
        files: [],
        hasMore: false,
        totalDirectories: 1,
        totalFiles: 0,
      },
    });
  }),
  getCollectionItems: vi.fn(() =>
    Promise.resolve({
      data: [
        {
          contentId: 'sha256:collection',
          fileName: 'Collection stream.ogg',
          id: 'collection-item-1',
          mediaKind: 'Audio',
        },
      ],
    }),
  ),
  getCollections: vi.fn(() =>
    Promise.resolve({ data: [{ id: 'collection-1', title: 'Favorites' }] }),
  ),
  searchLibraryItems: vi.fn(() =>
    Promise.resolve({
      data: {
        items: [
          {
            contentId: 'sha256:library',
            fileName: 'Library stream.ogg',
            mediaKind: 'Audio',
            path: '/downloads/Library stream.ogg',
          },
        ],
      },
    }),
  ),
}));

vi.mock('../../lib/externalVisualizer', () => ({
  getExternalVisualizerStatus: vi.fn(() =>
    Promise.resolve({
      arguments: [],
      available: true,
      configured: true,
      enabled: true,
      name: 'MilkDrop3',
      path: '/opt/MilkDrop3/MilkDrop 3.exe',
      resolvedPath: '/opt/MilkDrop3/MilkDrop 3.exe',
      workingDirectory: '/opt/MilkDrop3',
    }),
  ),
  launchExternalVisualizer: vi.fn(() =>
    Promise.resolve({
      error: null,
      name: 'MilkDrop3',
      processId: 1234,
      started: true,
    }),
  ),
}));

vi.mock('../../lib/streaming', () => ({
  buildDirectStreamUrl: vi.fn((contentId) =>
    `/api/v0/streams/${encodeURIComponent(contentId)}`,
  ),
  buildTranscodedStreamUrl: vi.fn((contentId, ticket, startSeconds) =>
    `/api/v0/streams/${encodeURIComponent(contentId)}/transcoded?ticket=${ticket}&startSeconds=${startSeconds}`,
  ),
  buildTicketedStreamUrl: vi.fn((contentId, ticket) =>
    `/api/v0/streams/${encodeURIComponent(contentId)}?ticket=${ticket}`,
  ),
  createStreamTicket: vi.fn(() => Promise.resolve('ticket-1')),
  getPlaybackInfo: vi.fn(() => Promise.resolve({ data: { durationSeconds: 120 } })),
}));

vi.mock('../../lib/searches', () => ({
  createBatch: vi.fn(),
}));

vi.mock('../../lib/wishlist', () => ({
  create: vi.fn(),
}));

const TestHarness = () => {
  const { playItem, playNext, queueItems } = usePlayer();

  return (
    <>
      <button onClick={() => queueItems([{ contentId: 'sha256:filename', fileName: 'Filename only.wav' }])} type="button">Queue filename</button>
      <button onClick={() => playNext({ contentId: 'sha256:filename', fileName: 'Filename only.wav' })} type="button">Play filename next</button>
      <button
        onClick={() =>
          playItem({
            album: 'Fixture Album',
            contentId: 'sha256:test',
            confidence: 0.91,
            fileName: 'Local stream.ogg',
            genre: 'Fixture Genre',
            sourceProviders: ['mesh', 'soulseek'],
            title: 'Local stream',
            verified: true,
          })
        }
        type="button"
      >
        Play fixture
      </button>
      <button
        onClick={() =>
          playItem({
            contentId: 'sha256:malformed',
            fileName: 'Malformed stream.ogg',
            sourceProviders: { 0: 'mesh', length: 1 },
            tags: { 0: 'tag', length: 1 },
            title: 'Malformed stream',
          })
        }
        type="button"
      >
        Play malformed fixture
      </button>
      <button
        onClick={() =>
          playItem({
            contentId: 'sha256:second',
            fileName: 'Second stream.ogg',
            title: 'Second stream',
          })
        }
        type="button"
      >
        Play second fixture
      </button>
      <button
        onClick={() =>
          playItem({
            contentId: 'sha256:third',
            fileName: 'Third stream.ogg',
            title: 'Third stream',
          })
        }
        type="button"
      >
        Play third fixture
      </button>
      <button
        onClick={() => queueItems([
          { contentId: 'sha256:second', title: 'Second stream', artist: 'slskdN' },
          { contentId: 'sha256:test', title: 'Local stream', artist: 'slskdN' },
        ])}
        type="button"
      >
        Queue prior fixtures
      </button>
      <PlayerBar />
    </>
  );
};

const renderPlayer = () => {
  const result = render(
    <MemoryRouter>
      <PlayerProvider>
        <TestHarness />
      </PlayerProvider>
    </MemoryRouter>,
  );
  const more = screen.queryByRole('button', { name: 'Show player tools' });
  if (more) fireEvent.click(more);
  return result;
};

describe('PlayerBar', () => {
  const originalMediaSession = Object.getOwnPropertyDescriptor(navigator, 'mediaSession');
  beforeEach(() => {
    vi.clearAllMocks();
    window.localStorage.clear();
    window.sessionStorage.clear();
    // These tests exercise the expanded player UI; the collapsed-by-default
    // behavior itself is covered separately below.
    window.localStorage.setItem('slskdn.player.collapsed', 'false');
    vi.stubGlobal('fetch', vi.fn(() => Promise.resolve({ ok: true })));
    searches.createBatch.mockResolvedValue(3);
    wishlistAPI.create.mockResolvedValue({ id: 'wishlist-seed' });
    HTMLMediaElement.prototype.load = vi.fn();
    HTMLMediaElement.prototype.play = vi.fn(() => Promise.resolve());
    HTMLMediaElement.prototype.pause = vi.fn();
  });

  afterEach(() => {
    if (originalMediaSession) Object.defineProperty(navigator, 'mediaSession', originalMediaSession);
    else delete navigator.mediaSession;
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('collapses by default when no preference has ever been stored', () => {
    window.localStorage.removeItem('slskdn.player.collapsed');
    renderPlayer();

    expect(document.querySelector('.player-bar-collapsed')).not.toBeNull();
    expect(screen.getByTestId('player-expand')).toBeInTheDocument();
  });

  it('stays expanded once the user has explicitly expanded it before', () => {
    window.localStorage.setItem('slskdn.player.collapsed', 'false');
    renderPlayer();

    expect(document.querySelector('.player-bar-collapsed')).toBeNull();
  });

  it('keeps Collapse available when optional player tools are closed', () => {
    renderPlayer();
    fireEvent.click(screen.getByRole('button', { name: 'Hide player tools' }));
    expect(screen.getByTestId('player-collapse')).toBeVisible();
    fireEvent.click(screen.getByTestId('player-collapse'));
    expect(screen.getByTestId('player-expand')).toBeVisible();
  });

  it('seeks and adjusts volume in the compact bar', async () => {
    window.localStorage.removeItem('slskdn.player.collapsed');
    renderPlayer();
    fireEvent.click(screen.getByText('Play fixture'));
    const audio = document.querySelector('audio');
    await waitFor(() => expect(audio.getAttribute('src')).toContain('sha256%3Atest'));
    Object.defineProperty(audio, 'duration', { configurable: true, value: 120 });
    fireEvent.loadedMetadata(audio);
    fireEvent.change(screen.getByLabelText('Seek playback'), { target: { value: '42' } });
    fireEvent.pointerUp(screen.getByLabelText('Seek playback'));
    fireEvent.change(screen.getByLabelText('Playback volume'), { target: { value: '0.35' } });
    expect(audio.currentTime).toBe(42);
    expect(audio.volume).toBe(0.35);
  });

  it('publishes Now Playing only after playback starts and clears it at the final end', async () => {
    renderPlayer();
    fireEvent.click(screen.getByText('Play fixture'));
    const audio = document.querySelector('audio');
    await waitFor(() => expect(audio.getAttribute('src')).toContain('sha256%3Atest'));
    expect(nowPlaying.setNowPlaying).not.toHaveBeenCalled();
    fireEvent.play(audio);
    expect(nowPlaying.setNowPlaying).toHaveBeenCalledWith(expect.objectContaining({ title: 'Local stream' }));
    fireEvent.ended(audio);
    expect(nowPlaying.clearNowPlaying).toHaveBeenCalled();
  });

  it.each(['Queue filename', 'Play filename next'])('normalizes filename-only metadata through %s', async (action) => {
    renderPlayer();
    fireEvent.click(screen.getByText('Play fixture'));
    fireEvent.click(screen.getByText(action));
    fireEvent.click(screen.getByTestId('player-next'));
    await waitFor(() => expect(document.querySelector('.player-title')).toHaveTextContent('Filename only.wav'));
    expect(document.querySelector('.player-title')).not.toHaveTextContent('Nothing playing');
  });

  it('normalizes a restored filename-only queue without losing its paused position', async () => {
    sessionStorage.setItem('slskdn.player.session.v1', JSON.stringify({
      queue: [{ contentId: 'sha256:restored', fileName: 'Restored filename.wav', positionSeconds: 12, startPaused: true }],
    }));
    renderPlayer();
    const audio = document.querySelector('audio');
    await waitFor(() => expect(audio.getAttribute('src')).toContain('sha256%3Arestored'));
    expect(document.querySelector('.player-title')).toHaveTextContent('Restored filename.wav');
    Object.defineProperty(audio, 'duration', { configurable: true, value: 120 });
    fireEvent.loadedMetadata(audio);
    expect(audio.currentTime).toBe(12);
    expect(HTMLMediaElement.prototype.play).not.toHaveBeenCalled();
  });

  it('checkpoints playback on page hide without progress-driven session writes', async () => {
    const storage = vi.spyOn(Storage.prototype, 'setItem');
    renderPlayer();
    fireEvent.click(screen.getByText('Play fixture'));
    const audio = document.querySelector('audio');
    await waitFor(() => expect(audio.getAttribute('src')).toContain('sha256%3Atest'));
    const writes = () => storage.mock.calls.filter(([key]) => key === 'slskdn.player.session.v1').length;
    const before = writes();
    for (let second = 1; second <= 12; second++) {
      audio.currentTime = second;
      fireEvent.timeUpdate(audio);
    }
    expect(writes()).toBe(before);
    fireEvent(window, new Event('pagehide'));
    expect(JSON.parse(sessionStorage.getItem('slskdn.player.session.v1')).queue[0].positionSeconds).toBe(12);
    expect(writes()).toBe(before + 1);
    storage.mockRestore();
  });

  it.each([false, true])('preserves a newly saved playlist when the initial list settles late (failure: %s)', async (failure) => {
    let finishList;
    let failList;
    collectionsAPI.getCollections.mockImplementationOnce(() => new Promise((resolve, reject) => { finishList = resolve; failList = reject; }));
    renderPlayer();
    fireEvent.click(screen.getByText('Play fixture'));
    fireEvent.click(screen.getByTestId('player-open-queue'));
    fireEvent.change(screen.getByLabelText('New playlist name'), { target: { value: 'Saved queue' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save queue' }));
    expect(await screen.findByText('Saved 1 track to Saved queue.')).toBeInTheDocument();
    if (failure) {
      await act(async () => failList(new Error('List unavailable')));
    } else {
      await act(async () => finishList({ data: [{ id: 'older-playlist', title: 'Older playlist', type: 'Playlist' }] }));
    }
    const selected = screen.getByLabelText('Saved playlist');
    expect(selected.querySelector('option[value="playlist-created"]')).toHaveTextContent('Saved queue');
    expect(screen.getByRole('status')).toHaveTextContent(failure ? 'Could not retrieve saved playlists.' : 'Saved 1 track to Saved queue.');
    expect(Array.from(selected.options).map((option) => option.value)).toEqual(failure
      ? ['', 'playlist-created']
      : ['', 'older-playlist', 'playlist-created']);
  });

  it('shows playlist operation progress and keeps fields stable until completion', async () => {
    let finishSave;
    collectionsAPI.createCollection.mockImplementationOnce(() => new Promise((resolve) => { finishSave = resolve; }));
    renderPlayer();
    fireEvent.click(screen.getByText('Play fixture'));
    fireEvent.click(screen.getByTestId('player-open-queue'));
    const name = screen.getByLabelText('New playlist name');
    const selected = screen.getByLabelText('Saved playlist');
    fireEvent.change(name, { target: { value: 'Saved queue' } });
    const save = screen.getByRole('button', { name: 'Save queue' });
    fireEvent.click(save);
    expect(save).toHaveAttribute('aria-busy', 'true');
    expect(name).toBeDisabled();
    expect(selected).toBeDisabled();
    await act(async () => finishSave({ data: { id: 'playlist-created', title: 'Saved queue', type: 'Playlist' } }));
    expect(await screen.findByText('Saved 1 track to Saved queue.')).toBeInTheDocument();
    expect(selected).toBeEnabled();
    fireEvent.change(selected, { target: { value: 'playlist-created' } });
    let finishLoad;
    collectionsAPI.getCollectionItems.mockImplementationOnce(() => new Promise((resolve) => { finishLoad = resolve; }));
    const load = screen.getByRole('button', { name: 'Load', exact: true });
    fireEvent.click(load);
    expect(load).toHaveAttribute('aria-busy', 'true');
    expect(selected).toBeDisabled();
    await act(async () => finishLoad({ data: [{ contentId: 'sha256:loaded', fileName: 'Loaded filename.wav' }] }));
    await waitFor(() => expect(document.querySelector('.player-title')).toHaveTextContent('Loaded filename.wav'));
    expect(load).toHaveAttribute('aria-busy', 'false');
    expect(selected).toBeEnabled();
  });

  it('offers on-demand decoding after a server audio error', async () => {
    renderPlayer();
    fireEvent.click(screen.getByText('Play fixture'));
    const audio = document.querySelector('audio');
    await waitFor(() => expect(audio.getAttribute('src')).toContain('sha256%3Atest'));
    fireEvent.error(audio);
    fireEvent.click(await screen.findByText('Decode for playback'));
    await waitFor(() => expect(audio.getAttribute('src')).toContain('/transcoded?'));
  });

  it('aborts the prior decode stream and coalesces seek setup into the final position', async () => {
    renderPlayer();
    fireEvent.click(screen.getByText('Play fixture'));
    const audio = document.querySelector('audio');
    await waitFor(() => expect(audio.getAttribute('src')).toContain('sha256%3Atest'));
    fireEvent.error(audio);
    fireEvent.click(await screen.findByText('Decode for playback'));
    await waitFor(() => expect(audio.getAttribute('src')).toContain('/transcoded?'));
    streaming.createStreamTicket.mockClear();
    HTMLMediaElement.prototype.load.mockClear();
    fireEvent.click(screen.getByTestId('player-fast-forward'));
    fireEvent.click(screen.getByTestId('player-fast-forward'));
    expect(audio.getAttribute('src')).toBeNull();
    expect(HTMLMediaElement.prototype.load).toHaveBeenCalled();
    expect(streaming.createStreamTicket).not.toHaveBeenCalled();
    await waitFor(() => expect(audio.getAttribute('src')).toContain('startSeconds=60'));
    expect(streaming.createStreamTicket).toHaveBeenCalledTimes(1);
  });

  it('cancels delayed decoded setup when the player unmounts', async () => {
    const { unmount } = renderPlayer();
    fireEvent.click(screen.getByText('Play fixture'));
    const audio = document.querySelector('audio');
    await waitFor(() => expect(audio.getAttribute('src')).toContain('sha256%3Atest'));
    fireEvent.error(audio);
    fireEvent.click(await screen.findByText('Decode for playback'));
    await waitFor(() => expect(audio.getAttribute('src')).toContain('/transcoded?'));
    streaming.createStreamTicket.mockClear();
    streaming.getPlaybackInfo.mockClear();
    fireEvent.click(screen.getByTestId('player-fast-forward'));
    unmount();
    await act(async () => new Promise((resolve) => window.setTimeout(resolve, 200)));
    expect(streaming.createStreamTicket).not.toHaveBeenCalled();
    expect(streaming.getPlaybackInfo).not.toHaveBeenCalled();
  });

  it('accumulates decoded seeks while setup is pending and preserves Play intent', async () => {
    renderPlayer();
    fireEvent.click(screen.getByText('Play fixture'));
    const audio = document.querySelector('audio');
    await waitFor(() => expect(audio.getAttribute('src')).toContain('sha256%3Atest'));
    fireEvent.error(audio);
    const decode = await screen.findByText('Decode for playback');
    let finishInitialSetup;
    streaming.getPlaybackInfo.mockImplementationOnce(() => new Promise((resolve) => {
      finishInitialSetup = resolve;
    }));
    fireEvent.click(decode);
    await waitFor(() => expect(finishInitialSetup).toBeDefined());
    HTMLMediaElement.prototype.play.mockClear();
    fireEvent.click(screen.getByTestId('player-fast-forward'));
    fireEvent.click(screen.getByTestId('player-fast-forward'));
    await waitFor(() => expect(audio.getAttribute('src')).toContain('startSeconds=60'));
    await waitFor(() => expect(HTMLMediaElement.prototype.play).toHaveBeenCalled());
    await act(async () => finishInitialSetup({ data: { durationSeconds: 120 } }));
    await waitFor(() => expect(audio.getAttribute('src')).toContain('startSeconds=60'));
    expect(screen.getByLabelText('Seek playback')).toHaveAttribute('aria-valuetext', '1:00 of 2:00');
  });

  it('keeps pending decoded seeks paused after transport Pause', async () => {
    renderPlayer();
    fireEvent.click(screen.getByText('Play fixture'));
    const audio = document.querySelector('audio');
    await waitFor(() => expect(audio.getAttribute('src')).toContain('sha256%3Atest'));
    fireEvent.error(audio);
    const decode = await screen.findByText('Decode for playback');
    let finishInitialSetup;
    streaming.getPlaybackInfo.mockImplementationOnce(() => new Promise((resolve) => {
      finishInitialSetup = resolve;
    }));
    fireEvent.click(decode);
    await waitFor(() => expect(finishInitialSetup).toBeDefined());
    fireEvent.click(screen.getByTestId('player-toggle-playback'));
    HTMLMediaElement.prototype.play.mockClear();
    fireEvent.click(screen.getByTestId('player-fast-forward'));
    fireEvent.click(screen.getByTestId('player-fast-forward'));
    await waitFor(() => expect(audio.getAttribute('src')).toContain('startSeconds=60'));
    expect(HTMLMediaElement.prototype.play).not.toHaveBeenCalled();
    expect(screen.getByTestId('player-toggle-playback')).toHaveAccessibleName('Resume local playback');
    await act(async () => finishInitialSetup({ data: { durationSeconds: 120 } }));
  });

  it('preserves native pending seek and autoplay across layout remounts', async () => {
    renderPlayer();
    let finishTicket;
    streaming.createStreamTicket.mockImplementationOnce(() => new Promise((resolve) => {
      finishTicket = resolve;
    }));
    fireEvent.click(screen.getByText('Play fixture'));
    await waitFor(() => expect(finishTicket).toBeDefined());
    fireEvent.click(screen.getByTestId('player-fast-forward'));
    fireEvent.click(screen.getByTestId('player-fast-forward'));
    fireEvent.click(screen.getByTestId('player-collapse'));
    HTMLMediaElement.prototype.play.mockClear();
    await act(async () => finishTicket('pending-ticket'));
    const audio = document.querySelector('audio');
    await waitFor(() => expect(audio.getAttribute('src')).toContain('pending-ticket'));
    await waitFor(() => expect(HTMLMediaElement.prototype.play).toHaveBeenCalled());
    Object.defineProperty(audio, 'duration', { configurable: true, value: 120 });
    fireEvent.loadedMetadata(audio);
    expect(audio.currentTime).toBe(60);
  });

  it('retries failed decoded setup instead of waiting for an absent source', async () => {
    renderPlayer();
    fireEvent.click(screen.getByText('Play fixture'));
    const audio = document.querySelector('audio');
    await waitFor(() => expect(audio.getAttribute('src')).toContain('sha256%3Atest'));
    fireEvent.error(audio);
    const decode = await screen.findByText('Decode for playback');
    streaming.createStreamTicket.mockRejectedValueOnce(new Error('busy'));
    fireEvent.click(decode);
    await screen.findByText(/Decoding could not start/u);
    fireEvent.click(screen.getByTestId('player-toggle-playback'));
    await waitFor(() => expect(audio.getAttribute('src')).toContain('/transcoded?'));
  });

  it('refreshes failed native server media tickets and restores its position', async () => {
    renderPlayer();
    fireEvent.click(screen.getByText('Play fixture'));
    const audio = document.querySelector('audio');
    await waitFor(() => expect(audio.getAttribute('src')).toContain('ticket-1'));
    audio.currentTime = 37;
    Object.defineProperty(audio, 'error', { configurable: true, value: { code: 2 } });
    HTMLMediaElement.prototype.load.mockImplementation(() => {
      Object.defineProperty(audio, 'error', { configurable: true, value: null });
    });
    fireEvent.error(audio);
    streaming.createStreamTicket.mockResolvedValueOnce('fresh-ticket');
    fireEvent.click(screen.getByTestId('player-toggle-playback'));
    await waitFor(() => expect(audio.getAttribute('src')).toContain('fresh-ticket'));
    Object.defineProperty(audio, 'duration', { configurable: true, value: 120 });
    fireEvent.loadedMetadata(audio);
    expect(audio.currentTime).toBe(37);
  });

  it('closes a stale Picture-in-Picture window after the player is hidden', async () => {
    vi.spyOn(audioGraph, 'resumeAudioGraph').mockResolvedValue({});
    let finishWindow;
    const requestWindow = vi.fn(() => new Promise((resolve) => { finishWindow = resolve; }));
    vi.stubGlobal('documentPictureInPicture', { requestWindow });
    renderPlayer();
    fireEvent.click(screen.getByText('Play fixture'));
    const audio = document.querySelector('audio');
    await waitFor(() => expect(audio.getAttribute('src')).toContain('sha256%3Atest'));
    fireEvent.click(screen.getByTestId('player-document-pip'));
    await waitFor(() => expect(finishWindow).toBeDefined());
    fireEvent.click(screen.getByTestId('player-hide'));
    const staleWindow = { close: vi.fn() };
    await act(async () => finishWindow(staleWindow));
    expect(staleWindow.close).toHaveBeenCalledOnce();
  });

  it('reports a rejected Picture-in-Picture request without stopping playback', async () => {
    vi.spyOn(audioGraph, 'resumeAudioGraph').mockResolvedValue({});
    vi.stubGlobal('documentPictureInPicture', {
      requestWindow: vi.fn(() => Promise.reject(new Error('denied'))),
    });
    renderPlayer();
    fireEvent.click(screen.getByText('Play fixture'));
    const audio = document.querySelector('audio');
    await waitFor(() => expect(audio.getAttribute('src')).toContain('sha256%3Atest'));
    fireEvent.play(audio);
    fireEvent.click(screen.getByTestId('player-document-pip'));
    await screen.findByText(/Picture-in-Picture could not open/u);
    expect(screen.getByTestId('player-toggle-playback')).toHaveAccessibleName('Pause local playback');
    window.documentPictureInPicture.requestWindow.mockImplementationOnce(() => new Promise(() => {}));
    fireEvent.click(screen.getByTestId('player-document-pip'));
    expect(screen.queryByText(/Picture-in-Picture could not open/u)).not.toBeInTheDocument();
  });

  it('clears stale Media Session position after Stop', async () => {
    const setPositionState = vi.fn();
    Object.defineProperty(navigator, 'mediaSession', {
      configurable: true,
      value: { setActionHandler: vi.fn(), setPositionState },
    });
    renderPlayer();
    fireEvent.click(screen.getByText('Play fixture'));
    const audio = document.querySelector('audio');
    await waitFor(() => expect(audio.getAttribute('src')).toContain('sha256%3Atest'));
    Object.defineProperty(audio, 'duration', { configurable: true, value: 120 });
    fireEvent.loadedMetadata(audio);
    expect(setPositionState).toHaveBeenLastCalledWith(expect.objectContaining({ duration: 120 }));
    fireEvent.click(screen.getByTestId('player-stop'));
    expect(setPositionState).toHaveBeenLastCalledWith();
  });

  it('mutes local browser playback without clearing the stream source', async () => {
    renderPlayer();

    fireEvent.click(screen.getByText('Play fixture'));

    const audio = document.querySelector('audio');
    await waitFor(() => {
      const src = audio.getAttribute('src') || '';
      expect(src).toContain(
        '/api/v0/streams/sha256%3Atest',
      );
    });

    fireEvent.click(screen.getByTestId('player-toggle-mute'));

    expect(audio.muted).toBe(true);
    expect(audio.getAttribute('src')).toContain(
      '/api/v0/streams/sha256%3Atest',
    );
    expect(window.localStorage.getItem('slskdn.player.localMuted')).toBe('true');
  });

  it('hides the player, keeps a restore control, and rejects playback when disabled', () => {
    window.localStorage.setItem(
      'slskdn:experience-preferences:v1',
      JSON.stringify({ playerVisible: false }),
    );

    renderPlayer();

    expect(screen.getByTestId('player-show')).toBeInTheDocument();
    expect(document.querySelector('audio')).not.toBeInTheDocument();

    fireEvent.click(screen.getByText('Play fixture'));

    expect(document.querySelector('audio')).not.toBeInTheDocument();

    fireEvent.click(screen.getByTestId('player-show'));
    expect(screen.getByTestId('player-hide')).toBeInTheDocument();
  });

  it('toggles player visibility from the player surface', () => {
    renderPlayer();

    fireEvent.click(screen.getByTestId('player-hide'));

    expect(screen.getByTestId('player-show')).toBeInTheDocument();
    expect(
      JSON.parse(window.localStorage.getItem('slskdn:experience-preferences:v1')),
    ).toEqual({ playerVisible: false });

    fireEvent.click(screen.getByTestId('player-show'));
    expect(screen.getByTestId('player-hide')).toBeInTheDocument();
  });

  it('does not clear now-playing on startup when the player was already hidden', () => {
    window.localStorage.setItem(
      'slskdn:experience-preferences:v1',
      JSON.stringify({ playerVisible: false }),
    );

    renderPlayer();

    expect(screen.getByTestId('player-show')).toBeInTheDocument();
    expect(nowPlaying.clearNowPlaying).not.toHaveBeenCalled();
  });

  it('restores the local mute preference for the PWA/browser session', () => {
    window.localStorage.setItem('slskdn.player.localMuted', 'true');

    renderPlayer();
    fireEvent.click(screen.getByText('Play fixture'));

    expect(document.querySelector('audio').muted).toBe(true);
  });

  it('opens collection and local file browser modals before playback starts', async () => {
    renderPlayer();

    expect(
      screen.getByTestId('player-open-collections-browser'),
    ).toBeInTheDocument();
    expect(screen.getByTestId('player-open-file-browser')).toBeInTheDocument();
    await screen.findByText('Pick a collection or local audio file');

    fireEvent.click(screen.getByTestId('player-open-collections-browser'));
    expect(
      screen.getByTestId('player-collection-browser-modal'),
    ).toBeInTheDocument();
    fireEvent.click(await screen.findByTestId('player-collection-row-collection-1'));
    expect(await screen.findByText('Collection stream.ogg')).toBeInTheDocument();

    fireEvent.click(screen.getAllByText('Close')[0]);
    fireEvent.click(screen.getByTestId('player-open-file-browser'));
    expect(screen.getByTestId('player-file-browser-modal')).toBeInTheDocument();
    expect(await screen.findByText('Downloads')).toBeInTheDocument();
    fireEvent.click(screen.getByTestId('player-file-folder-Downloads'));
    expect(await screen.findByText('Library stream.ogg')).toBeInTheDocument();
  });

  it('ignores malformed player collection and browser list payloads', async () => {
    collectionsAPI.getCollections.mockResolvedValueOnce({ data: { items: [] } });
    collectionsAPI.browseLibraryItems.mockResolvedValueOnce({
      data: {
        breadcrumbs: { path: '' },
        directories: { name: 'Downloads' },
        files: { fileName: 'Library stream.ogg' },
      },
    });

    renderPlayer();

    fireEvent.click(screen.getByTestId('player-open-collections-browser'));
    expect(
      screen.getByTestId('player-collection-browser-modal'),
    ).toBeInTheDocument();
    expect(screen.queryByText('Favorites')).not.toBeInTheDocument();

    fireEvent.click(screen.getAllByText('Close')[0]);
    fireEvent.click(screen.getByTestId('player-open-file-browser'));
    expect(screen.getByTestId('player-file-browser-modal')).toBeInTheDocument();

    await waitFor(() => {
      expect(collectionsAPI.browseLibraryItems).toHaveBeenCalled();
    });
    expect(screen.queryByText('Downloads')).not.toBeInTheDocument();
    expect(screen.queryByText('Library stream.ogg')).not.toBeInTheDocument();
  });

  it('searches the local file browser as a deduplicated explorer', async () => {
    renderPlayer();

    fireEvent.click(screen.getByTestId('player-open-file-browser'));
    fireEvent.change(screen.getByTestId('player-file-browser-search').querySelector('input'), {
      target: { value: 'library' },
    });

    expect(await screen.findByText('Library stream.ogg')).toBeInTheDocument();
    expect(screen.getByText(/2 duplicates collapsed/u)).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
  });

  it('switches the visual tile from album art to the MilkDrop canvas', async () => {
    renderPlayer();

    expect(screen.getByTestId('player-album-art')).toBeInTheDocument();

    fireEvent.click(screen.getByTestId('player-visual-tile'));

    await waitFor(() =>
      expect(document.querySelector('.player-visualizer-canvas')).toBeInTheDocument());
    expect(screen.queryByTestId('player-album-art')).not.toBeInTheDocument();
  });

  it('cycles the visual tile through visualizer and analyzer variants', async () => {
    renderPlayer();

    const tile = screen.getByTestId('player-visual-tile');

    fireEvent.click(tile);
    await waitFor(() =>
      expect(window.localStorage.getItem('slskdn.player.visualTileMode')).toBe('butterchurn'));
    await waitFor(() =>
      expect(document.querySelector('.player-visualizer-canvas')).toBeInTheDocument());

    fireEvent.click(tile);
    await waitFor(() =>
      expect(window.localStorage.getItem('slskdn.player.visualTileMode')).toBe('native-webgl2'));

    fireEvent.click(tile);
    await waitFor(() =>
      expect(window.localStorage.getItem('slskdn.player.visualTileMode')).toBe('native-webgpu'));

    fireEvent.click(tile);
    await waitFor(() =>
      expect(window.localStorage.getItem('slskdn.player.visualTileMode')).toBe('spectrum'));
    expect(within(tile).getByLabelText('Spectrum analyzer')).toBeInTheDocument();

    fireEvent.click(tile);
    await waitFor(() =>
      expect(window.localStorage.getItem('slskdn.player.visualTileMode')).toBe('scope'));
    expect(within(tile).getByLabelText('Oscilloscope')).toBeInTheDocument();

    fireEvent.click(screen.getByTestId('player-visual-tile-mode-butterchurn'));
    await waitFor(() =>
      expect(window.localStorage.getItem('slskdn.player.visualTileMode')).toBe('butterchurn'));
  });

  it('shows tile-level visualizer maximize controls even while analyzer bars are active', async () => {
    renderPlayer();
    window.localStorage.setItem('slskdn.player.visualizerEngine', 'native-webgl2');

    fireEvent.click(screen.getByTestId('player-visual-tile-mode-spectrum'));

    await waitFor(() =>
      expect(window.localStorage.getItem('slskdn.player.visualTileMode')).toBe('spectrum'));

    fireEvent.click(screen.getByTestId('player-visual-tile-mode-butterchurn'));
    await waitFor(() =>
      expect(window.localStorage.getItem('slskdn.player.visualTileMode')).toBe('butterchurn'));

    fireEvent.click(screen.getByTestId('player-visual-tile-mode-spectrum'));
    await waitFor(() =>
      expect(window.localStorage.getItem('slskdn.player.visualTileMode')).toBe('spectrum'));

    fireEvent.click(screen.getByTestId('player-visual-tile-fullwindow'));

    await waitFor(() => {
      expect(window.localStorage.getItem('slskdn.player.visualTileMode')).toBe('native-webgl2');
    });
    expect(document.querySelector('.player-visualizer-fullwindow')).toBeInTheDocument();
  });

  it('does not repeat the currently playing track in the queue preview', () => {
    renderPlayer();

    fireEvent.click(screen.getByText('Play fixture'));

    expect(screen.getByText('Local stream')).toBeInTheDocument();
    expect(document.querySelector('.player-queue')).not.toBeInTheDocument();

    fireEvent.click(screen.getByText('Play second fixture'));

    expect(screen.getByText('Second stream')).toBeInTheDocument();
    fireEvent.click(screen.getByText('Queue prior fixtures'));
    expect(screen.getByText('Local stream')).toBeInTheDocument();
    expect(document.querySelector('.player-queue')?.textContent).not.toContain(
      'Second stream',
    );
  });

  it('makes ListenBrainz autosave explicit and clearable', () => {
    renderPlayer();

    fireEvent.click(screen.getByTestId('player-open-integrations'));
    const tokenInput = screen.getByLabelText('ListenBrainz user token');

    fireEvent.change(tokenInput, { target: { value: ' token-1 ' } });

    expect(screen.getByTestId('player-listenbrainz-save-state')).toHaveTextContent(
      'saved automatically',
    );
    expect(window.sessionStorage.getItem('slskdn.listenbrainz.token')).toBe('token-1');
    expect(screen.getByTestId('player-close-integrations')).toHaveTextContent('Done');

    fireEvent.click(screen.getByTestId('player-clear-listenbrainz-token'));

    expect(tokenInput).toHaveValue('');
    expect(window.sessionStorage.getItem('slskdn.listenbrainz.token')).toBeNull();
  });

  it('shows and launches the configured external visualizer', async () => {
    renderPlayer();

    fireEvent.click(screen.getByTestId('player-open-integrations'));

    expect(
      await screen.findByText('Ready to launch on the slskdN host.'),
    ).toBeInTheDocument();
    expect(screen.getByText('/opt/MilkDrop3/MilkDrop 3.exe')).toBeInTheDocument();

    fireEvent.click(screen.getByTestId('player-launch-external-visualizer'));

    await waitFor(() => {
      expect(externalVisualizer.launchExternalVisualizer).toHaveBeenCalled();
    });
    expect(await screen.findByText('MilkDrop3 launched.')).toBeInTheDocument();
  });

  it('shows now-playing source badges and stores local discovery ratings', async () => {
    renderPlayer();

    fireEvent.click(screen.getByText('Play fixture'));

    expect(await screen.findByTestId('player-badge-source-Mesh')).toHaveTextContent(
      'Mesh',
    );
    expect(screen.getByTestId('player-badge-source-Soulseek')).toHaveTextContent(
      'Soulseek',
    );
    expect(screen.getByTestId('player-badge-confidence')).toHaveTextContent(
      '91% match',
    );
    expect(screen.getByTestId('player-badge-verified')).toHaveTextContent(
      'Verified',
    );

    fireEvent.click(screen.getByTestId('player-rating-5'));

    expect(screen.getByTestId('player-rating-controls')).toHaveTextContent(
      'Discovery boost',
    );
    expect(window.localStorage.getItem('slskdn.player.ratings')).toContain(
      '"content:sha256:test":5',
    );
    expect(window.localStorage.getItem('slskdn.discovery.shelf')).toContain(
      '"action":"promote-preview"',
    );

    fireEvent.click(screen.getByTestId('player-open-discovery-shelf'));

    expect(await screen.findByText('Discovery Shelf')).toBeInTheDocument();
    expect(screen.getByTestId('player-shelf-summary')).toHaveTextContent(
      '1local review items',
    );
    expect(screen.getByTestId('player-shelf-row-content:sha256:test')).toHaveTextContent(
      'Promote preview',
    );
    expect(screen.getByTestId('player-shelf-policy-preview')).toHaveTextContent(
      '1 promote',
    );
    expect(screen.getByTestId('player-shelf-policy-preview')).toHaveTextContent(
      '0 consensus gated',
    );
    fireEvent.click(screen.getByTestId('player-shelf-copy-policy-report'));
    expect(screen.getByText('Policy report prepared for 1 shelf items.')).toBeInTheDocument();
    fireEvent.click(screen.getByTestId('player-shelf-preview-content:sha256:test'));
    expect(screen.getByText('Promote preview prepared for Local stream. No files were moved or deleted.')).toBeInTheDocument();
    fireEvent.click(screen.getByTestId('player-close-discovery-shelf'));

    fireEvent.click(screen.getByTestId('player-rating-5'));

    expect(screen.getByTestId('player-rating-controls')).toHaveTextContent(
      'Not rated',
    );
  });

  it('ignores malformed now-playing source and tag lists', async () => {
    renderPlayer();

    fireEvent.click(screen.getByText('Play malformed fixture'));

    expect(await screen.findByText('Malformed stream')).toBeInTheDocument();
    expect(screen.queryByTestId('player-badge-source-Mesh')).not.toBeInTheDocument();
  });

  it('handles player keyboard shortcuts without stealing input typing', async () => {
    renderPlayer();

    fireEvent.click(screen.getByText('Play fixture'));
    const audio = document.querySelector('audio');
    await waitFor(() => {
      expect(audio.getAttribute('src')).toContain('/api/v0/streams/sha256%3Atest');
    });

    fireEvent.keyDown(window, { key: 'm' });
    expect(audio.muted).toBe(true);

    fireEvent.keyDown(window, { key: 'e' });
    expect(document.querySelector('.player-panel-eq')).toBeInTheDocument();

    fireEvent.keyDown(window, { key: 'v' });
    await waitFor(() =>
      expect(document.querySelector('.player-visualizer-canvas')).toBeInTheDocument());

    fireEvent.keyDown(window, { key: 'ArrowRight' });
    expect(audio.currentTime).toBe(30);

    fireEvent.click(screen.getByTestId('player-open-integrations'));
    const tokenInput = screen.getByLabelText('ListenBrainz user token');
    fireEvent.keyDown(tokenInput, { key: 'm' });

    expect(audio.muted).toBe(true);
  });

  it('opens smart radio seeds without starting a search automatically', async () => {
    renderPlayer();

    fireEvent.click(screen.getByText('Play fixture'));
    fireEvent.click(screen.getByTestId('player-open-radio'));

    expect(await screen.findByText('Smart Radio Seed')).toBeInTheDocument();
    expect(screen.getByTestId('player-radio-seed')).toHaveTextContent(
      'slskdN - Local stream',
    );
    expect(screen.getByText('Similar track seed')).toBeInTheDocument();
    expect(screen.getByText('slskdN Local stream')).toBeInTheDocument();
    expect(screen.getByText('Album neighborhood')).toBeInTheDocument();
    expect(screen.getByText('slskdN Fixture Album')).toBeInTheDocument();
    expect(screen.getByText('Artist and genre seed')).toBeInTheDocument();
    expect(screen.getByText('slskdN Fixture Genre')).toBeInTheDocument();
    expect(searches.createBatch).not.toHaveBeenCalled();

    fireEvent.click(screen.getByTestId('player-radio-start-searches'));
    await waitFor(() => {
      expect(searches.createBatch).toHaveBeenCalledWith({
        queries: [
          'slskdN Local stream',
          'slskdN Fixture Album',
          'slskdN Fixture Genre',
        ],
      });
    });
    expect(screen.getByText('Started 3 smart-radio searches.')).toBeInTheDocument();

    fireEvent.click(screen.getByTestId('player-radio-add-wishlist'));
    await waitFor(() => {
      expect(wishlistAPI.create).toHaveBeenCalledWith(
        expect.objectContaining({
          autoDownload: false,
          enabled: true,
          searchText: 'slskdN Local stream',
        }),
      );
    });
    expect(screen.getByText('Added 4 smart-radio seeds to Wishlist.')).toBeInTheDocument();

    expect(screen.queryByTestId('player-radio-send-inbox')).not.toBeInTheDocument();
  });

  it('manages the playback queue without removing the current track', async () => {
    renderPlayer();

    fireEvent.click(screen.getByText('Play fixture'));
    fireEvent.click(screen.getByText('Play second fixture'));
    fireEvent.click(screen.getByText('Play third fixture'));
    fireEvent.click(screen.getByText('Queue prior fixtures'));
    fireEvent.click(screen.getByTestId('player-open-queue'));

    expect(await screen.findByText('Playback Queue')).toBeInTheDocument();
    expect(screen.getByText('Now Playing')).toBeInTheDocument();
    expect(screen.getAllByText('Third stream')).toHaveLength(2);
    expect(screen.getByTestId('player-queue-row-sha256:second')).toHaveTextContent(
      'Second stream',
    );
    expect(screen.getByTestId('player-queue-row-sha256:test')).toHaveTextContent(
      'Local stream',
    );
    expect(screen.getByText('Recent')).toBeInTheDocument();

    fireEvent.click(screen.getByTestId('player-remove-queue-sha256:second'));

    expect(screen.queryByTestId('player-queue-row-sha256:second')).not.toBeInTheDocument();
    expect(screen.getAllByText('Third stream')).toHaveLength(2);

    fireEvent.click(screen.getByTestId('player-clear-upcoming'));

    expect(screen.getByText('No upcoming tracks.')).toBeInTheDocument();
    expect(screen.getAllByText('Third stream')).toHaveLength(2);

    fireEvent.click(screen.getByTestId('player-search-similar-candidates'));
    await waitFor(() => {
      expect(searches.createBatch).toHaveBeenCalledWith({
        queries: ['slskdN Second stream', 'slskdN Local stream'],
      });
    });
    expect(screen.getByText('Started 3 similar-track searches.')).toBeInTheDocument();

    fireEvent.click(screen.getByTestId('player-wishlist-similar-candidates'));
    await waitFor(() => {
      expect(wishlistAPI.create).toHaveBeenCalledWith(
        expect.objectContaining({
          autoDownload: false,
          enabled: true,
          searchText: 'slskdN Second stream',
        }),
      );
    });
    expect(screen.getByText('Added 2 similar-track seeds to Wishlist.')).toBeInTheDocument();

    fireEvent.click(screen.getByTestId('player-auto-queue-similar'));

    expect(screen.getByTestId('player-queue-row-sha256:second')).toHaveTextContent(
      'Second stream',
    );
    expect(screen.getByTestId('player-queue-row-sha256:test')).toHaveTextContent(
      'Local stream',
    );
  });

  it('records local listening history and shows browser stats', async () => {
    renderPlayer();

    fireEvent.click(screen.getByText('Play fixture'));
    const audio = document.querySelector('audio');
    await waitFor(() => expect(audio.getAttribute('src')).toContain('sha256%3Atest'));

    Object.defineProperty(audio, 'duration', {
      configurable: true,
      value: 120,
    });
    Object.defineProperty(audio, 'paused', {
      configurable: true,
      value: false,
    });
    fireEvent.play(audio);
    Object.defineProperty(audio, 'currentTime', {
      configurable: true,
      value: 61,
      writable: true,
    });
    fireEvent.timeUpdate(audio);

    fireEvent.click(screen.getByTestId('player-open-listening-stats'));

    expect(await screen.findByText('Listening Stats')).toBeInTheDocument();
    expect(screen.getByTestId('player-stats-summary')).toHaveTextContent(
      '1local plays recorded',
    );
    expect(screen.getByText('Top Artists')).toBeInTheDocument();
    expect(screen.getByText('Top Genres')).toBeInTheDocument();
    expect(screen.getByText('Recommendation Seeds')).toBeInTheDocument();
    expect(screen.getAllByText('slskdN').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Fixture Genre').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Local stream').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByTestId('player-stats-search-seed-Fixture Genre')).toBeInTheDocument();

    expect(
      screen.queryByTestId('player-stats-send-seeds-to-discovery-inbox'),
    ).not.toBeInTheDocument();

    fireEvent.click(screen.getByTestId('player-stats-start-seed-searches'));
    await waitFor(() => {
      expect(searches.createBatch).toHaveBeenCalledWith({
        queries: expect.arrayContaining(['Fixture Genre', 'Local stream']),
      });
    });
    expect(screen.getByText('Started 3 bounded listening seed searches.')).toBeInTheDocument();

    fireEvent.click(screen.getByTestId('player-stats-add-seeds-to-wishlist'));
    await waitFor(() => {
      expect(wishlistAPI.create).toHaveBeenCalledWith(
        expect.objectContaining({
          autoDownload: false,
          enabled: true,
          searchText: 'Fixture Genre',
        }),
      );
    });
    expect(screen.getByText(/Added .* listening seeds to Wishlist/)).toBeInTheDocument();

    window.sessionStorage.setItem('slskdn.listenbrainz.token', 'token-1');
    fireEvent.click(screen.getByTestId('player-listening-history-scrobble-recent'));
    await waitFor(() => {
      expect(fetch).toHaveBeenCalledWith(
        'https://api.listenbrainz.org/1/submit-listens',
        expect.objectContaining({
          method: 'POST',
        }),
      );
    });
    expect(screen.getByText('Submitted 1 recent listen to ListenBrainz.')).toBeInTheDocument();

    fireEvent.click(screen.getByTestId('player-clear-listening-history'));

    expect(screen.getByTestId('player-stats-summary')).toHaveTextContent(
      '0local plays recorded',
    );
  });

  it('imports pasted media-server listening history into browser stats', async () => {
    renderPlayer();

    const importedAt = new Date(Date.now() - 6 * 24 * 60 * 60 * 1000).toISOString();

    fireEvent.click(screen.getByText('Play fixture'));
    fireEvent.click(screen.getByTestId('player-open-listening-stats'));

    expect(await screen.findByText('Listening Stats')).toBeInTheDocument();

    fireEvent.change(screen.getByTestId('player-listening-history-import-text'), {
      target: {
        value: [
          'playedAt,artist,album,title,genre',
          `${importedAt},Imported Artist,Imported Album,Imported Track,Imported Genre`,
        ].join('\n'),
      },
    });
    fireEvent.click(screen.getByTestId('player-listening-history-import'));

    expect(screen.getByText('1 imported, 0 skipped as duplicates or incomplete rows.')).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId('player-stats-summary')).toHaveTextContent(
        '1local plays recorded',
      );
    });
    expect(screen.getAllByText('Imported Artist').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Imported Genre').length).toBeGreaterThanOrEqual(1);
  });
});
