// <copyright file="PodListenAlongPanel.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import { createListeningPartyHubConnection } from '../../lib/hubFactory';
import * as listeningParty from '../../lib/listeningParty';
import PodListenAlongPanel from './PodListenAlongPanel';
import { usePlayer } from './PlayerContext';
import { act, render, screen } from '@testing-library/react';
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
    vi.clearAllMocks();
    vi.useRealTimers();
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
