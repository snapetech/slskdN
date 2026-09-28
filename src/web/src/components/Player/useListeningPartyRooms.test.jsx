// <copyright file="useListeningPartyRooms.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import { createListeningPartyHubConnection } from '../../lib/hubFactory';
import * as listeningParty from '../../lib/listeningParty';
import useListeningPartyRooms from './useListeningPartyRooms';
import { act, cleanup, renderHook } from '@testing-library/react';

vi.mock('../../lib/hubFactory', () => ({ createListeningPartyHubConnection: vi.fn() }));
vi.mock('../../lib/listeningParty', () => ({ getPartyState: vi.fn() }));

const createHub = () => ({
  invoke: vi.fn().mockResolvedValue(undefined),
  on: vi.fn(),
  onclose: vi.fn(),
  onreconnected: vi.fn(),
  onreconnecting: vi.fn(),
  start: vi.fn().mockResolvedValue(undefined),
  stop: vi.fn().mockResolvedValue(undefined),
});

describe('player-owned listening rooms', () => {
  beforeEach(() => {
    createListeningPartyHubConnection.mockImplementation(createHub);
    listeningParty.getPartyState.mockResolvedValue(null);
  });
  afterEach(() => { cleanup(); vi.resetAllMocks(); });

  it('does not mistake a previous render of another followed room for an ended empty room', async () => {
    let finishSnapshot;
    listeningParty.getPartyState.mockReturnValue(new Promise((resolve) => { finishSnapshot = resolve; }));
    const player = { clear: vi.fn(), followingParty: { podId: 'old', channelId: 'music', contentId: 'previous-track' } };
    const setFollowingParty = vi.fn();
    const { result } = renderHook(() => useListeningPartyRooms(player, setFollowingParty));
    await act(async () => result.current.observePartyRoom('new', 'music', vi.fn()));
    act(() => result.current.followParty({ podId: 'new', channelId: 'music' }));
    await act(async () => finishSnapshot(null));
    expect(player.clear).not.toHaveBeenCalled();
    expect(setFollowingParty).toHaveBeenLastCalledWith({ podId: 'new', channelId: 'music' });
  });

  it('bounds distinct room connections and disposes every owned hub on provider unmount', async () => {
    const { result, unmount } = renderHook(() => useListeningPartyRooms({}, vi.fn()));
    const overflow = vi.fn();
    await act(async () => {
      result.current.observePartyRoom('a', 'music', vi.fn());
      result.current.observePartyRoom('b', 'music', vi.fn());
      result.current.observePartyRoom('c', 'music', overflow);
    });
    expect(createListeningPartyHubConnection).toHaveBeenCalledTimes(2);
    expect(overflow).toHaveBeenCalledWith(expect.objectContaining({ error: 'Close another room before opening listen-along here.', pending: false }));
    const hubs = createListeningPartyHubConnection.mock.results.map(({ value }) => value);
    unmount();
    expect(hubs.map((hub) => hub.stop.mock.calls.length)).toEqual([1, 1]);
  });
});
