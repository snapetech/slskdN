// <copyright file="useListeningPartyBroadcast.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as listeningParty from '../../lib/listeningParty';
import useListeningPartyBroadcast from './useListeningPartyBroadcast';
import { act, cleanup, renderHook, waitFor } from '@testing-library/react';

vi.mock('../../lib/listeningParty', () => ({ getPartyState: vi.fn(), publishPartyState: vi.fn() }));
const config = { channelId: 'music', globalRadio: true, meshStreaming: true, podId: 'pod', user: 'host' };
const track = { contentId: 'first', title: 'First' };
let observer;
let releaseRoom;
let player;
const deferred = () => {
  let resolve;
  let reject;
  const promise = new Promise((yes, no) => { resolve = yes; reject = no; });
  return { promise, reject, resolve };
};
const start = async (result) => { await act(async () => result.current.publishBroadcast(config, 'play')); };

describe('persistent host publication', () => {
  beforeEach(() => {
    listeningParty.getPartyState.mockResolvedValue({ partyId: 'existing' });
    releaseRoom = vi.fn();
    player = { audioElement: { paused: false }, current: track, followParty: vi.fn(),
      getPlaybackPosition: () => 8, observePartyRoom: vi.fn((pod, channel, callback) => {
        observer = callback;
        return releaseRoom;
      }), playerVisible: true };
    listeningParty.publishPartyState.mockImplementation(async (podId, channelId, payload) => ({ ...payload, partyId: 'party', hostPeerId: 'authorized-host' }));
  });
  afterEach(() => { cleanup(); vi.resetAllMocks(); });

  it('requires an explicit start and publishes mapped play, pause and seek positions', async () => {
    const { result } = renderHook(() => useListeningPartyBroadcast(player));
    act(() => result.current.reportPlaybackEvent('play', 3));
    expect(listeningParty.publishPartyState).not.toHaveBeenCalled();
    await start(result);
    await act(async () => result.current.reportPlaybackEvent('pause', 42));
    await act(async () => result.current.reportPlaybackEvent('seek', 73));
    expect(listeningParty.publishPartyState.mock.calls.map((call) => [call[2].action, call[2].positionSeconds, call[2].partyId])).toEqual([
      ['play', 8, ''], ['pause', 42, 'party'], ['seek', 73, 'party'],
    ]);
    expect(player.followParty).toHaveBeenCalledWith(null);
  });

  it.each(['play', 'stop'])('preserves observed party identity for manual %s', async (action) => {
    const { result } = renderHook(() => useListeningPartyBroadcast(player));
    await act(async () => result.current.publishBroadcast({ ...config, partyId: 'observed' }, action));
    expect(listeningParty.publishPartyState.mock.calls[0][2].partyId).toBe('observed');
    expect(listeningParty.getPartyState).not.toHaveBeenCalled();
    expect(player.followParty).toHaveBeenCalledTimes(action === 'stop' ? 0 : 1);
  });

  it('refreshes unknown identity once for an explicit Stop', async () => {
    const { result } = renderHook(() => useListeningPartyBroadcast(player));
    await act(async () => result.current.publishBroadcast(config, 'stop'));
    expect(listeningParty.getPartyState).toHaveBeenCalledOnce();
    expect(listeningParty.publishPartyState.mock.calls[0][2]).toMatchObject({ action: 'stop', partyId: 'existing' });
    expect(result.current.broadcastStatus).toBeNull();
  });

  it('starts a paused host without announcing playback that did not occur', async () => {
    player.audioElement.paused = true;
    const { result } = renderHook(() => useListeningPartyBroadcast(player));
    await start(result);
    expect(listeningParty.publishPartyState.mock.calls[0][2]).toMatchObject({ action: 'pause', positionSeconds: 8 });
  });

  it('retains ownership across player rerenders and publishes replacement tracks', async () => {
    const { result, rerender } = renderHook(() => useListeningPartyBroadcast(player));
    await start(result);
    player = { ...player, current: { contentId: 'second', title: 'Second' } };
    rerender();
    await act(async () => result.current.reportPlaybackEvent('play', 0));
    expect(listeningParty.publishPartyState.mock.calls.at(-1)[2]).toMatchObject({ contentId: 'second', partyId: 'party', positionSeconds: 0 });
    expect(player.observePartyRoom).toHaveBeenCalledOnce();
    expect(releaseRoom).not.toHaveBeenCalled();
  });

  it('coalesces a burst to the latest update and uses the assigned party identity', async () => {
    const request = deferred();
    listeningParty.publishPartyState.mockReturnValueOnce(request.promise);
    const { result } = renderHook(() => useListeningPartyBroadcast(player));
    let initial;
    act(() => { initial = result.current.publishBroadcast(config, 'play'); });
    let latest;
    act(() => {
      for (let index = 0; index < 100; index++) latest = result.current.reportPlaybackEvent('seek', index);
    });
    expect(listeningParty.publishPartyState).toHaveBeenCalledOnce();
    await act(async () => { request.resolve({ partyId: 'assigned', hostPeerId: 'authorized-host' }); await initial; await latest; });
    expect(listeningParty.publishPartyState).toHaveBeenCalledTimes(2);
    expect(listeningParty.publishPartyState.mock.calls[1][2]).toMatchObject({ partyId: 'assigned', action: 'seek', positionSeconds: 99 });
  });

  it('serializes Stop after the in-flight update and rejects later automatic updates', async () => {
    const request = deferred();
    listeningParty.publishPartyState.mockReturnValueOnce(request.promise);
    const { result } = renderHook(() => useListeningPartyBroadcast(player));
    let initial;
    let stopped;
    act(() => { initial = result.current.publishBroadcast(config, 'play'); });
    act(() => {
      result.current.reportPlaybackEvent('seek', 60);
      stopped = result.current.stopBroadcast();
      result.current.reportPlaybackEvent('play', 70);
    });
    await act(async () => { request.resolve({ partyId: 'assigned' }); await initial; await stopped; });
    expect(listeningParty.publishPartyState.mock.calls.map((call) => call[2].action)).toEqual(['play', 'stop']);
    expect(listeningParty.publishPartyState.mock.calls[1][2]).toMatchObject({ partyId: 'assigned', positionSeconds: 0 });
    expect(result.current.broadcastStatus).toBeNull();
    expect(releaseRoom).toHaveBeenCalledOnce();
  });

  it('rejects a queued Stop when its preceding publication fails', async () => {
    const request = deferred();
    listeningParty.publishPartyState.mockReturnValueOnce(request.promise);
    const { result } = renderHook(() => useListeningPartyBroadcast(player));
    let initial;
    let stopped;
    act(() => {
      initial = result.current.publishBroadcast(config, 'play');
      stopped = result.current.stopBroadcast();
    });
    const initialFailure = initial.catch((error) => error);
    const stopFailure = stopped.catch((error) => error);
    await act(async () => {
      request.reject(new Error('Offline'));
      expect(await initialFailure).toHaveProperty('message', 'Offline');
      expect(await stopFailure).toHaveProperty('message', 'Offline');
    });
    expect(result.current.broadcastStatus).toMatchObject({ active: true });
    expect(result.current.broadcastStatus.error).toMatch(/failed/);
    expect(listeningParty.publishPartyState).toHaveBeenCalledOnce();
    await act(async () => result.current.retryBroadcast());
    expect(listeningParty.publishPartyState.mock.calls.at(-1)[2].action).toBe('stop');
    expect(result.current.broadcastStatus).toBeNull();
  });

  it('does not resurrect an ended room broadcast from later playback events', async () => {
    const { result } = renderHook(() => useListeningPartyBroadcast(player));
    await start(result);
    act(() => observer({ connected: true, pending: false, state: null }));
    act(() => result.current.reportPlaybackEvent('play', 80));
    expect(listeningParty.publishPartyState).toHaveBeenCalledOnce();
    expect(result.current.broadcastStatus).toMatchObject({ active: false });
  });

  it('preserves separate paused seek positions while deduplicating identical events', async () => {
    const { result } = renderHook(() => useListeningPartyBroadcast(player));
    await start(result);
    await act(async () => result.current.reportPlaybackEvent('pause', 8));
    await act(async () => result.current.reportPlaybackEvent('pause', 8));
    await act(async () => result.current.reportPlaybackEvent('pause', 26));
    expect(listeningParty.publishPartyState.mock.calls.map((call) => [call[2].action, call[2].positionSeconds])).toEqual([['play', 8], ['pause', 8], ['pause', 26]]);
  });

  it.each([null, { contentId: 'local:file' }, { contentId: 'remote', radioPartyId: 'remote-party' }])('stops when the source becomes unshareable: %s', async (current) => {
    const { result, rerender } = renderHook(() => useListeningPartyBroadcast(player));
    await start(result);
    await act(async () => { player = { ...player, current }; rerender(); });
    await waitFor(() => expect(result.current.broadcastStatus).toBeNull());
    expect(listeningParty.publishPartyState.mock.calls.at(-1)[2].action).toBe('stop');
    expect(result.current.broadcastStatus).toBeNull();
  });

  it('stops on hide without starting another connection or polling', async () => {
    const { result, rerender } = renderHook(() => useListeningPartyBroadcast(player));
    await start(result);
    await act(async () => { player = { ...player, playerVisible: false }; rerender(); });
    await waitFor(() => expect(result.current.broadcastStatus).toBeNull());
    expect(listeningParty.publishPartyState.mock.calls.at(-1)[2].action).toBe('stop');
    expect(player.observePartyRoom).toHaveBeenCalledOnce();
  });

  it('releases on revocation and never republishes received room state', async () => {
    const { result } = renderHook(() => useListeningPartyBroadcast(player));
    await start(result);
    act(() => observer({ error: 'Room access was revoked.', state: null }));
    act(() => result.current.reportPlaybackEvent('play', 50));
    expect(listeningParty.publishPartyState).toHaveBeenCalledOnce();
    expect(result.current.broadcastStatus).toMatchObject({ active: false, error: 'Room access was revoked.' });
    expect(releaseRoom).toHaveBeenCalledOnce();
  });

  it('releases when another host replaces the broadcast', async () => {
    const { result } = renderHook(() => useListeningPartyBroadcast(player));
    await start(result);
    act(() => observer({ state: { partyId: 'other', hostPeerId: 'other-host' } }));
    expect(result.current.broadcastStatus.active).toBe(false);
    expect(releaseRoom).toHaveBeenCalledOnce();
  });

  it('pauses automatic updates on failure and allows an explicit retry', async () => {
    const { result } = renderHook(() => useListeningPartyBroadcast(player));
    await start(result);
    listeningParty.publishPartyState.mockRejectedValueOnce(new Error('Offline'));
    await act(async () => result.current.reportPlaybackEvent('pause', 11));
    act(() => result.current.reportPlaybackEvent('seek', 45));
    expect(listeningParty.publishPartyState).toHaveBeenCalledTimes(2);
    expect(result.current.broadcastStatus.error).toMatch(/failed/);
    await act(async () => result.current.retryBroadcast());
    expect(result.current.broadcastStatus.error).toBe('');
    expect(listeningParty.publishPartyState).toHaveBeenCalledTimes(3);
  });

  it('paces writes without an idle polling timer', async () => {
    vi.useFakeTimers({ toFake: ['performance', 'setTimeout', 'clearTimeout'] });
    const times = [];
    listeningParty.publishPartyState.mockImplementation(async (podId, channelId, event) => {
      times.push(performance.now());
      return { ...event, partyId: 'party' };
    });
    const { result, unmount } = renderHook(() => useListeningPartyBroadcast(player));
    try {
      await start(result);
      act(() => {
        result.current.reportPlaybackEvent('pause', 10);
        result.current.reportPlaybackEvent('seek', 20);
      });
      await act(async () => vi.advanceTimersByTimeAsync(249));
      expect(times).toHaveLength(1);
      await act(async () => vi.advanceTimersByTimeAsync(1));
      expect(times).toHaveLength(2);
      await act(async () => vi.advanceTimersByTimeAsync(250));
      expect(times[1] - times[0]).toBeGreaterThanOrEqual(250);
      expect(times[2] - times[1]).toBeGreaterThanOrEqual(250);
      expect(vi.getTimerCount()).toBe(0);
    } finally {
      unmount();
      vi.useRealTimers();
    }
  });

  it('cancels a pacing timer and pending work on ownership loss', async () => {
    vi.useFakeTimers({ toFake: ['performance', 'setTimeout', 'clearTimeout'] });
    const { result, unmount } = renderHook(() => useListeningPartyBroadcast(player));
    try {
      await start(result);
      let update;
      act(() => { update = result.current.reportPlaybackEvent('seek', 50); });
      expect(vi.getTimerCount()).toBe(1);
      act(() => observer({ error: 'Room access was revoked.', state: null }));
      await act(async () => update);
      expect(vi.getTimerCount()).toBe(0);
      expect(listeningParty.publishPartyState).toHaveBeenCalledOnce();
      expect(releaseRoom).toHaveBeenCalledOnce();
    } finally {
      unmount();
      vi.useRealTimers();
    }
  });

  it('rejects a second host room until the first broadcast stops', async () => {
    const { result } = renderHook(() => useListeningPartyBroadcast(player));
    await start(result);
    await act(async () => { await expect(result.current.publishBroadcast({ ...config, podId: 'other' }, 'play')).rejects.toThrow(/Stop the active/); });
    expect(player.observePartyRoom).toHaveBeenCalledOnce();
  });

  it('aborts and releases owned resources on unmount', async () => {
    const { result, unmount } = renderHook(() => useListeningPartyBroadcast(player));
    await start(result);
    const signal = listeningParty.publishPartyState.mock.calls[0][3].signal;
    unmount();
    expect(signal.aborted).toBe(true);
    expect(releaseRoom).toHaveBeenCalledOnce();
  });
});
