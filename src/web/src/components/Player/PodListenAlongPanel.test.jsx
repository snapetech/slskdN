// <copyright file="PodListenAlongPanel.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import { createListeningPartyHubConnection } from '../../lib/hubFactory';
import * as listeningParty from '../../lib/listeningParty';
import PodListenAlongPanel from './PodListenAlongPanel';
import { usePlayer } from './PlayerContext';
import { act, cleanup, fireEvent, render, screen } from '@testing-library/react';
import React from 'react';

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
