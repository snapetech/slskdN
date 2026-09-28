// <copyright file="PodListenAlongPanel.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import { createListeningPartyHubConnection } from '../../lib/hubFactory';
import * as listeningParty from '../../lib/listeningParty';
import PodListenAlongPanel from './PodListenAlongPanel';
import { usePlayer } from './PlayerContext';
import useListeningPartyRooms from './useListeningPartyRooms';
import { act, cleanup, fireEvent, render as renderView, screen } from '@testing-library/react';
import React, { createContext, useContext, useRef, useState } from 'react';

vi.mock('../../lib/hubFactory', () => ({
  createListeningPartyHubConnection: vi.fn(),
}));

vi.mock('../../lib/listeningParty', () => ({
  buildRadioStreamUrl: vi.fn(),
  getPartyDirectory: vi.fn(),
  getPartyState: vi.fn(),
  publishPartyState: vi.fn(),
}));

vi.mock('./PlayerContext', () => ({
  usePlayer: vi.fn(),
}));

const createHub = () => ({
  invoke: vi.fn().mockResolvedValue(undefined),
  on: vi.fn(),
  onclose: vi.fn(),
  onreconnected: vi.fn(),
  onreconnecting: vi.fn(),
  start: vi.fn().mockResolvedValue(undefined),
  stop: vi.fn().mockResolvedValue(undefined),
});

const player = {
  clear: vi.fn(),
  current: null,
  followParty: vi.fn(),
  pause: vi.fn(),
  playItem: vi.fn(),
};

// Exercise the production owner while retaining controllable media methods.
const TestPlayerContext = createContext(null);
const TestPlayer = ({ children }) => {
  const base = useRef(usePlayer()).current;
  const [followingParty, setFollowingParty] = useState(null);
  const methods = useListeningPartyRooms({ ...base, followingParty }, setFollowingParty);
  usePlayer.mockImplementation(() => useContext(TestPlayerContext));
  return <TestPlayerContext.Provider value={{ ...base, ...methods, followingParty }}>{children}</TestPlayerContext.Provider>;
};
const render = (ui) => renderView(ui, { wrapper: TestPlayer });

describe('PodListenAlongPanel directory polling', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: false,
    });
    createListeningPartyHubConnection.mockReturnValue(createHub());
    listeningParty.getPartyDirectory.mockResolvedValue([]);
    listeningParty.getPartyState.mockResolvedValue(null);
    usePlayer.mockReturnValue(player);
  });

  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
    vi.useRealTimers();
  });

  it.each([true, false])('exposes following state in compact=%s layout', async (compact) => {
    render(<PodListenAlongPanel channelId="music" compact={compact} podId="pod-a" user="listener" />);
    await act(async () => {});
    const follow = screen.getByRole('button', { name: compact ? 'Follow room broadcast' : 'Follow pod broadcast' });
    expect(follow).toHaveAttribute('aria-pressed', 'false');
    fireEvent.click(follow);
    expect(follow).toHaveAttribute('aria-pressed', 'true');
    fireEvent.click(follow);
    expect(follow).toHaveAttribute('aria-pressed', 'false');
  });

  it.each([[7.5, true], [7.99, false]])('aligns a host Pause from %s seconds without reloading an aligned source', async (position, shouldAlign) => {
    listeningParty.getPartyState.mockResolvedValue({ action: 'pause', contentId: 'track', positionSeconds: 8 });
    usePlayer.mockReturnValue({ ...player, current: { contentId: 'track' }, getPlaybackPosition: () => position });
    render(<PodListenAlongPanel channelId="music" compact podId="pod-a" user="listener" />);
    await act(async () => {});
    fireEvent.click(screen.getByRole('button', { name: 'Follow room broadcast' }));
    expect(player.pause).toHaveBeenCalledOnce();
    const expectedCall = [
      expect.objectContaining({ contentId: 'track' }),
      expect.objectContaining({ positionSeconds: 8, startPaused: true }),
    ];
    expect(player.playItem.mock.calls).toEqual(shouldAlign ? [expectedCall] : []);
  });

  it('exposes directory and streaming opt-in state in compact controls', async () => {
    render(<PodListenAlongPanel channelId="music" compact podId="pod-a" user="listener" />);
    await act(async () => {});
    const listed = screen.getByRole('button', { name: 'List room broadcast in mesh directory' });
    const streaming = screen.getByRole('button', { name: 'Allow mesh streaming for broadcast' });
    expect(listed).toHaveAttribute('aria-pressed', 'false');
    expect(streaming).toHaveAttribute('aria-pressed', 'false');
    expect(streaming).toBeDisabled();
    fireEvent.click(listed);
    expect(listed).toHaveAttribute('aria-pressed', 'true');
    expect(streaming).toBeEnabled();
    fireEvent.click(streaming);
    expect(streaming).toHaveAttribute('aria-pressed', 'true');
    fireEvent.click(listed);
    expect(listed).toHaveAttribute('aria-pressed', 'false');
    expect(streaming).toBeDisabled();
  });

  it.each([
    [429, null, 'Room updates are at capacity. Retry later.'],
    [404, null, 'This room is unavailable. Choose an existing room.'],
    [503, 'room_storage_unavailable', 'The room update could not be saved. Try again.'],
  ])('explains room publication status %s without losing retry controls', async (status, code, message) => {
    usePlayer.mockReturnValue({
      ...player,
      current: { contentId: 'server-track', title: 'Track' },
      getPlaybackPosition: () => 0,
    });
    listeningParty.publishPartyState.mockRejectedValueOnce({ response: { status, data: { code } } });
    render(<PodListenAlongPanel channelId="channel-a" compact podId="pod-a" user="user-a" />);
    await act(async () => { await Promise.resolve(); });
    const broadcast = screen.getByRole('button', { name: 'Broadcast current track to room' });
    await act(async () => fireEvent.click(broadcast));
    expect(screen.getByRole('alert')).toHaveTextContent(message);
    expect(broadcast).toBeEnabled();
  });

  it('offers retry after an initial connection failure and joins a fresh hub', async () => {
    const failed = createHub();
    failed.start.mockRejectedValueOnce(new Error('Offline'));
    const recovered = createHub();
    createListeningPartyHubConnection.mockReturnValueOnce(failed).mockReturnValueOnce(recovered);
    render(<PodListenAlongPanel channelId="channel-a" compact podId="pod-a" user="user-a" />);
    await act(async () => { await Promise.resolve(); });
    const retry = screen.getByRole('button', { name: 'Retry listen-along connection' });
    expect(retry).toBeEnabled();
    fireEvent.click(retry);
    await act(async () => { await Promise.resolve(); });
    expect(recovered.invoke).toHaveBeenCalledWith('JoinParty', 'pod-a', 'channel-a');
    expect(screen.getByLabelText('Listen Along live')).toBeInTheDocument();
    expect(failed.stop).toHaveBeenCalledOnce();
  });

  it('does not join after a pending startup is disposed', async () => {
    const hub = createHub();
    let finishStart;
    hub.start.mockReturnValueOnce(new Promise((resolve) => { finishStart = resolve; }));
    createListeningPartyHubConnection.mockReturnValueOnce(hub);
    const view = render(<PodListenAlongPanel channelId="channel-a" compact podId="pod-a" user="user-a" />);
    view.unmount();
    await act(async () => finishStart());
    const reconnect = hub.onreconnected.mock.calls[0][0];
    await act(async () => reconnect());
    expect(hub.invoke).not.toHaveBeenCalledWith('JoinParty', 'pod-a', 'channel-a');
    expect(hub.stop).toHaveBeenCalledOnce();
  });

  it('shows refresh failures and permits recovery even while the hub is live', async () => {
    listeningParty.getPartyState.mockRejectedValueOnce(new Error('Snapshot unavailable'));
    const hub = createHub();
    let finishJoin;
    hub.invoke.mockReturnValueOnce(new Promise((resolve) => { finishJoin = resolve; }));
    createListeningPartyHubConnection.mockReturnValueOnce(hub);
    render(<PodListenAlongPanel channelId="channel-a" compact podId="pod-a" user="user-a" />);
    await act(async () => { await Promise.resolve(); });
    await act(async () => finishJoin());
    listeningParty.getPartyState.mockRejectedValueOnce(new Error('Refresh unavailable'));
    await act(async () => hub.onreconnected.mock.calls[0][0]());
    expect(screen.getByRole('alert')).toHaveTextContent('Room state could not refresh');
    expect(screen.getByRole('button', { name: 'Retry listen-along connection' })).toBeEnabled();
  });

  it.each(['closed', 'rejoin failed'])('offers recovery after %s', async (failure) => {
    const hub = createHub();
    createListeningPartyHubConnection.mockReturnValueOnce(hub);
    render(<PodListenAlongPanel channelId="channel-a" compact podId="pod-a" user="user-a" />);
    await act(async () => { await Promise.resolve(); });
    await act(async () => {
      if (failure === 'closed') hub.onclose.mock.calls[0][0]();
      else {
        hub.invoke.mockRejectedValueOnce(new Error('Rejoin failed'));
        hub.onreconnecting.mock.calls[0][0]();
        await hub.onreconnected.mock.calls[0][0]();
      }
    });
    expect(screen.getByLabelText('Listen Along offline')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Retry listen-along connection' })).toBeEnabled();
    expect(screen.getByRole('alert')).toHaveTextContent(failure === 'closed' ? 'connection closed' : 'Could not rejoin');
  });

  it.each(['initial', 'reconnect'])('keeps revocation ahead of an in-flight %s join', async (kind) => {
    const hub = createHub();
    let finishJoin;
    const pending = new Promise((resolve) => { finishJoin = resolve; });
    if (kind === 'initial') hub.invoke.mockReturnValueOnce(pending);
    createListeningPartyHubConnection.mockReturnValueOnce(hub);
    render(<PodListenAlongPanel channelId="channel-a" compact podId="pod-a" user="user-a" />);
    await act(async () => { await Promise.resolve(); });
    let rejoin;
    if (kind === 'reconnect') {
      hub.invoke.mockReturnValueOnce(pending);
      await act(async () => { rejoin = hub.onreconnected.mock.calls[0][0](); });
    }
    await act(async () => hub.on.mock.calls.find(([event]) => event === 'partyAccessRevoked')[1]());
    await act(async () => { finishJoin(); if (rejoin) await rejoin; });
    expect(screen.getByRole('alert')).toHaveTextContent('Room access was revoked');
    expect(screen.getByLabelText('Listen Along offline')).toBeInTheDocument();
  });

  it('retains revoked access feedback across late room events until explicit rejoin', async () => {
    const hub = createHub();
    createListeningPartyHubConnection.mockReturnValueOnce(hub);
    render(<PodListenAlongPanel channelId="channel-a" compact podId="pod-a" user="user-a" />);
    await act(async () => { await Promise.resolve(); });
    const revoked = hub.on.mock.calls.find(([event]) => event === 'partyAccessRevoked')[1];
    const receive = hub.on.mock.calls.find(([event]) => event === 'partyState')[1];
    fireEvent.click(screen.getByRole('button', { name: 'Follow room broadcast' }));
    await act(async () => revoked());
    expect(screen.getByRole('alert')).toHaveTextContent('Room access was revoked');
    expect(screen.getByRole('button', { name: 'Follow room broadcast' })).toHaveAttribute('aria-pressed', 'false');
    expect(player.clear).toHaveBeenCalledOnce();
    await act(async () => receive({ action: 'play', contentId: 'stale', title: 'Queued stale event' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Room access was revoked');
    expect(screen.queryByText('Queued stale event')).not.toBeInTheDocument();
  });

  it('keeps live host events ahead of late snapshots', async () => {
    const hub = createHub();
    let finishSnapshot;
    listeningParty.getPartyState.mockReturnValue(new Promise((resolve) => { finishSnapshot = resolve; }));
    createListeningPartyHubConnection.mockReturnValueOnce(hub);
    render(<PodListenAlongPanel channelId="channel-a" compact podId="pod-a" user="user-a" />);
    await act(async () => { await Promise.resolve(); });
    await act(async () => hub.on.mock.calls.find(([event]) => event === 'partyState')[1]({ title: 'Fresh host state', action: 'play', contentId: 'fresh' }));
    await act(async () => finishSnapshot({ title: 'Stale snapshot', action: 'play', contentId: 'stale' }));
    expect(screen.getByText('Fresh host state')).toBeInTheDocument();
    expect(screen.queryByText('Stale snapshot')).not.toBeInTheDocument();
  });

  it('retains the followed room across navigation and reuses its connection on return', async () => {
    const hub = createHub();
    createListeningPartyHubConnection.mockReturnValueOnce(hub);
    listeningParty.getPartyState.mockResolvedValue({ action: 'play', contentId: 'track', positionSeconds: 0 });
    usePlayer.mockReturnValue({ ...player, getPlaybackPosition: () => 0 });
    const panel = <PodListenAlongPanel channelId="music" compact podId="pod-a" user="listener" />;
    const view = render(panel);
    await act(async () => {});
    fireEvent.click(screen.getByRole('button', { name: 'Follow room broadcast' }));
    view.rerender(<div>Browsing downloads</div>);
    expect(hub.stop).not.toHaveBeenCalled();
    const receive = hub.on.mock.calls.find(([event]) => event === 'partyState')[1];
    await act(async () => receive({ action: 'pause', contentId: 'track', positionSeconds: 16 }));
    expect(player.pause).toHaveBeenCalledOnce();
    expect(player.playItem).toHaveBeenLastCalledWith(expect.objectContaining({ contentId: 'track' }),
      expect.objectContaining({ positionSeconds: 16, startPaused: true }));
    view.rerender(panel);
    expect(screen.getByRole('button', { name: 'Follow room broadcast' })).toHaveAttribute('aria-pressed', 'true');
    expect(createListeningPartyHubConnection).toHaveBeenCalledOnce();
    fireEvent.click(screen.getByRole('button', { name: 'Follow room broadcast' }));
    view.rerender(<div>Browsing downloads</div>);
    expect(hub.stop).toHaveBeenCalledOnce();
    expect(hub.invoke).toHaveBeenCalledWith('LeaveParty', 'pod-a', 'music');
  });

  it('keeps another room revocation separate from the followed room', async () => {
    const followed = createHub();
    const viewed = createHub();
    createListeningPartyHubConnection.mockReturnValueOnce(followed).mockReturnValueOnce(viewed);
    listeningParty.getPartyState.mockResolvedValue({ action: 'play', contentId: 'track' });
    usePlayer.mockReturnValue({ ...player, getPlaybackPosition: () => 0 });
    const first = <PodListenAlongPanel channelId="music" compact podId="pod-a" user="listener" />;
    const view = render(first);
    await act(async () => {});
    fireEvent.click(screen.getByRole('button', { name: 'Follow room broadcast' }));
    view.rerender(<PodListenAlongPanel channelId="other" compact podId="pod-a" user="listener" />);
    await act(async () => {});
    expect(screen.getByRole('button', { name: 'Follow room broadcast' })).toHaveAttribute('aria-pressed', 'false');
    await act(async () => viewed.on.mock.calls.find(([event]) => event === 'partyAccessRevoked')[1]());
    expect(player.clear).not.toHaveBeenCalled();
    await act(async () => followed.on.mock.calls.find(([event]) => event === 'partyState')[1]({ action: 'pause', contentId: 'track', positionSeconds: 17 }));
    expect(player.pause).toHaveBeenCalledOnce();
    view.rerender(first);
    expect(screen.getByRole('button', { name: 'Follow room broadcast' })).toHaveAttribute('aria-pressed', 'true');
    expect(viewed.stop).toHaveBeenCalledOnce();
    expect(followed.stop).not.toHaveBeenCalled();
    expect(createListeningPartyHubConnection).toHaveBeenCalledTimes(2);
  });

  it('deduplicates snapshots after automatic reconnect and releases an offscreen host Stop', async () => {
    const hub = createHub();
    createListeningPartyHubConnection.mockReturnValueOnce(hub);
    const state = { action: 'play', contentId: 'track', sequence: 1 };
    listeningParty.getPartyState.mockResolvedValue(state);
    const view = render(<PodListenAlongPanel channelId="music" compact podId="pod-a" user="listener" />);
    await act(async () => {});
    fireEvent.click(screen.getByRole('button', { name: 'Follow room broadcast' }));
    view.rerender(<div>Browsing downloads</div>);
    await act(async () => hub.onreconnected.mock.calls[0][0]());
    expect(player.playItem).toHaveBeenCalledOnce();
    await act(async () => hub.on.mock.calls.find(([event]) => event === 'partyState')[1]({ action: 'stop', sequence: 2 }));
    expect(player.clear).toHaveBeenCalledOnce();
    expect(hub.stop).toHaveBeenCalledOnce();
  });

  it('does not automatically restore revoked access on reconnect', async () => {
    const hub = createHub();
    createListeningPartyHubConnection.mockReturnValueOnce(hub);
    render(<PodListenAlongPanel channelId="music" compact podId="pod-a" user="listener" />);
    await act(async () => {});
    await act(async () => hub.on.mock.calls.find(([event]) => event === 'partyAccessRevoked')[1]());
    hub.invoke.mockClear();
    await act(async () => hub.onreconnected.mock.calls[0][0]());
    expect(hub.invoke).not.toHaveBeenCalledWith('JoinParty', 'pod-a', 'music');
    expect(screen.getByRole('alert')).toHaveTextContent('Room access was revoked');
  });

  it('does not request the unrendered directory in compact mode', async () => {
    render(
      <PodListenAlongPanel
        channelId="channel-a"
        compact
        podId="pod-a"
        user="user-a"
      />,
    );

    await act(async () => {
      await vi.advanceTimersByTimeAsync(180_000);
    });

    expect(listeningParty.getPartyDirectory).not.toHaveBeenCalled();
  });

  it('pauses directory polling while hidden and refreshes when visible', async () => {
    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: true,
    });
    render(
      <PodListenAlongPanel
        channelId="channel-a"
        podId="pod-a"
        user="user-a"
      />,
    );

    await act(async () => {
      await vi.advanceTimersByTimeAsync(180_000);
    });
    expect(listeningParty.getPartyDirectory).not.toHaveBeenCalled();

    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: false,
    });
    await act(async () => {
      document.dispatchEvent(new Event('visibilitychange'));
      await Promise.resolve();
    });
    expect(listeningParty.getPartyDirectory).toHaveBeenCalledTimes(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(60_000);
    });
    expect(listeningParty.getPartyDirectory).toHaveBeenCalledTimes(2);
  });

  it('does not overlap slow directory requests', async () => {
    let completeRequest;
    listeningParty.getPartyDirectory.mockReturnValue(
      new Promise((resolve) => {
        completeRequest = resolve;
      }),
    );

    render(
      <PodListenAlongPanel
        channelId="channel-a"
        podId="pod-a"
        user="user-a"
      />,
    );
    await act(async () => {
      await vi.advanceTimersByTimeAsync(180_000);
    });
    expect(listeningParty.getPartyDirectory).toHaveBeenCalledTimes(1);

    await act(async () => {
      completeRequest([]);
      await Promise.resolve();
    });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(60_000);
    });
    expect(listeningParty.getPartyDirectory).toHaveBeenCalledTimes(2);
  });

  it('retains the last successful directory after a transient failure', async () => {
    listeningParty.getPartyDirectory
      .mockResolvedValueOnce([
        {
          allowMeshStreaming: false,
          contentId: 'content-a',
          hostPeerId: 'host-a',
          partyId: 'party-a',
          title: 'Track A',
        },
      ])
      .mockRejectedValueOnce(new Error('DHT unavailable'));

    render(
      <PodListenAlongPanel
        channelId="channel-a"
        podId="pod-a"
        user="user-a"
      />,
    );
    await act(async () => {
      await Promise.resolve();
    });
    expect(screen.getByText('Track A')).toBeInTheDocument();

    await act(async () => {
      await vi.advanceTimersByTimeAsync(60_000);
    });
    expect(screen.getByText('Track A')).toBeInTheDocument();
  });
});
