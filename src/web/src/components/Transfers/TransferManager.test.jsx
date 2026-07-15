// <copyright file="TransferManager.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import '@testing-library/jest-dom';
import TransferManager from './TransferManager';
import React from 'react';
import { act, fireEvent, render, screen } from '@testing-library/react';
import { vi } from 'vitest';

const mocks = vi.hoisted(() => {
  const callbacks = {};
  return {
    callbacks,
    getAcceleratedMode: vi.fn(),
    getAutoReplaceStatus: vi.fn(),
    getChanges: vi.fn(),
    getHistory: vi.fn(),
    hub: {
      on: vi.fn((name, callback) => {
        callbacks[name] = callback;
      }),
      onclose: vi.fn((callback) => {
        callbacks.close = callback;
      }),
      onreconnected: vi.fn((callback) => {
        callbacks.reconnected = callback;
      }),
      onreconnecting: vi.fn((callback) => {
        callbacks.reconnecting = callback;
      }),
      start: vi.fn(),
      stop: vi.fn(),
    },
  };
});

vi.mock('../../lib/autoReplace', () => ({
  getAutoReplaceStatus: mocks.getAutoReplaceStatus,
}));

vi.mock('../../lib/hubFactory', () => ({
  createTransfersHubConnection: () => mocks.hub,
}));

vi.mock('../../lib/transfers', () => ({
  getAcceleratedMode: mocks.getAcceleratedMode,
  getChanges: mocks.getChanges,
  getHistory: mocks.getHistory,
}));

vi.mock('../Shared', () => ({
  PlaceholderSegment: ({ children }) => <div>{children}</div>,
}));

vi.mock('./TransferTable', () => ({
  default: ({ transfers = [] }) => (
    <div data-testid="transfer-table">{transfers.length}</div>
  ),
}));

vi.mock('./TransfersHeader', () => ({
  default: ({ onHideCompletedChange }) => (
    <button onClick={() => onHideCompletedChange(false)} type="button">
      Show completed
    </button>
  ),
}));

vi.mock('./RequestDetailModal', () => ({
  default: () => null,
}));

vi.mock('react-toastify', () => ({
  toast: {
    error: vi.fn(),
    info: vi.fn(),
  },
}));

const setHidden = (hidden) => {
  Object.defineProperty(document, 'hidden', {
    configurable: true,
    value: hidden,
  });
};

const flush = async () => {
  await act(async () => {
    await Promise.resolve();
    await Promise.resolve();
  });
};

describe('TransferManager reconciliation', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.clearAllMocks();
    Object.keys(mocks.callbacks).forEach((key) => delete mocks.callbacks[key]);
    mocks.getAcceleratedMode.mockResolvedValue({ enabled: false });
    mocks.getAutoReplaceStatus.mockResolvedValue({ enabled: false });
    mocks.getChanges.mockResolvedValue({ cursor: 100, transfers: [] });
    mocks.getHistory.mockResolvedValue({
      asOf: 100,
      hasMore: false,
      nextOffset: 0,
      transfers: [],
    });
    mocks.hub.start.mockResolvedValue(undefined);
    setHidden(false);
  });

  afterEach(() => {
    vi.useRealTimers();
    setHidden(false);
  });

  it('advances from an initial snapshot to overlapping cursor deltas', async () => {
    render(<TransferManager direction="download" />);
    await flush();

    expect(mocks.getChanges).toHaveBeenCalledTimes(1);
    expect(mocks.getChanges).toHaveBeenNthCalledWith(1, { since: null });

    await act(async () => {
      await vi.advanceTimersByTimeAsync(15_000);
    });

    expect(mocks.getChanges).toHaveBeenCalledTimes(2);
    expect(mocks.getChanges).toHaveBeenNthCalledWith(2, { since: 99 });
  });

  it('does not overlap a slow reconciliation request', async () => {
    let resolveInitial;
    mocks.getChanges.mockReturnValueOnce(
      new Promise((resolve) => {
        resolveInitial = resolve;
      }),
    );

    render(<TransferManager direction="download" />);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(45_000);
    });

    expect(mocks.getChanges).toHaveBeenCalledTimes(1);

    resolveInitial({ cursor: 100, transfers: [] });
    await flush();
    await act(async () => {
      await vi.advanceTimersByTimeAsync(15_000);
    });

    expect(mocks.getChanges).toHaveBeenCalledTimes(2);
  });

  it('replays realtime events that arrive before the initial snapshot', async () => {
    let resolveInitial;
    mocks.getChanges.mockReturnValueOnce(
      new Promise((resolve) => {
        resolveInitial = resolve;
      }),
    );

    render(<TransferManager direction="download" />);
    await act(async () => {
      mocks.callbacks.activity({
        direction: 'Download',
        filename: 'Music\\new.flac',
        id: 'transfer-1',
        state: 'Queued',
        username: 'listener',
      });
      resolveInitial({ cursor: 100, transfers: [] });
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(screen.getByTestId('transfer-table')).toHaveTextContent('1');
  });

  it('waits for the initial seed before loading completed history', async () => {
    let resolveInitial;
    mocks.getChanges.mockReturnValueOnce(
      new Promise((resolve) => {
        resolveInitial = resolve;
      }),
    );

    render(<TransferManager direction="download" />);
    fireEvent.click(screen.getByRole('button', { name: 'Show completed' }));
    await flush();
    expect(mocks.getHistory).not.toHaveBeenCalled();

    resolveInitial({
      counts: { download: 1, upload: 0 },
      cursor: 100,
      transfers: [],
    });
    await flush();

    expect(mocks.getHistory).toHaveBeenCalledTimes(1);
  });

  it('suspends hidden polling and catches up immediately when visible', async () => {
    setHidden(true);
    render(<TransferManager direction="download" />);
    await flush();
    expect(mocks.getChanges).not.toHaveBeenCalled();

    setHidden(false);
    await act(async () => {
      document.dispatchEvent(new Event('visibilitychange'));
      await Promise.resolve();
    });
    expect(mocks.getChanges).toHaveBeenCalledTimes(1);

    setHidden(true);
    await act(async () => {
      document.dispatchEvent(new Event('visibilitychange'));
      await vi.advanceTimersByTimeAsync(45_000);
    });
    expect(mocks.getChanges).toHaveBeenCalledTimes(1);

    setHidden(false);
    await act(async () => {
      document.dispatchEvent(new Event('visibilitychange'));
      await Promise.resolve();
    });
    expect(mocks.getChanges).toHaveBeenCalledTimes(2);
    expect(mocks.getChanges).toHaveBeenLastCalledWith({ since: 99 });
  });

  it('loads successful history only on request and advances the stable page', async () => {
    mocks.getChanges.mockResolvedValue({
      counts: { download: 3, upload: 0 },
      cursor: 100,
      transfers: [
        {
          direction: 'Download',
          filename: 'Music\\failed.flac',
          id: 'failed-1',
          state: 'Completed, Errored',
          username: 'listener',
        },
      ],
    });
    mocks.getHistory
      .mockResolvedValueOnce({
        asOf: 2_000,
        hasMore: true,
        nextOffset: 1,
        transfers: [
          {
            direction: 'Download',
            filename: 'Music\\completed-1.flac',
            id: 'completed-1',
            state: 'Completed, Succeeded',
            username: 'listener',
          },
        ],
      })
      .mockResolvedValueOnce({
        asOf: 2_000,
        hasMore: false,
        nextOffset: 2,
        transfers: [
          {
            direction: 'Download',
            filename: 'Music\\completed-2.flac',
            id: 'completed-2',
            state: 'Completed, Succeeded',
            username: 'listener',
          },
        ],
      });

    render(<TransferManager direction="download" />);
    await flush();
    expect(mocks.getHistory).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole('button', { name: 'Show completed' }));
    await flush();

    expect(mocks.getHistory).toHaveBeenNthCalledWith(1, {
      asOf: null,
      direction: 'download',
      limit: 250,
      offset: 0,
    });
    expect(screen.getByTestId('transfer-table')).toHaveTextContent('2');

    fireEvent.click(
      screen.getByRole('button', { name: 'Load older completed' }),
    );
    await flush();

    expect(mocks.getHistory).toHaveBeenNthCalledWith(2, {
      asOf: 2_000,
      direction: 'download',
      limit: 250,
      offset: 1,
    });
    expect(screen.getByTestId('transfer-table')).toHaveTextContent('3');
  });
});
