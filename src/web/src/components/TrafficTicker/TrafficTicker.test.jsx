// <copyright file="TrafficTicker.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import '@testing-library/jest-dom';
import React from 'react';
import TrafficTicker from './TrafficTicker';
import userEvent from '@testing-library/user-event';
import { act, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const hubState = vi.hoisted(() => {
  const callbacks = {};
  const hub = {
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
  };

  return { callbacks, hub };
});

vi.mock('../../lib/hubFactory', () => ({
  createTransfersHubConnection: () => hubState.hub,
}));

describe('TrafficTicker', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    Object.keys(hubState.callbacks).forEach((key) => delete hubState.callbacks[key]);
    hubState.hub.start.mockResolvedValue(undefined);
  });

  it('explains the activity expansion controls without expanding on hover', async () => {
    const user = userEvent.setup();
    render(<TrafficTicker />);

    await act(async () => {
      await Promise.resolve();
    });
    await act(async () => {
      for (let index = 0; index < 12; index += 1) {
        hubState.callbacks.activity({
          direction: 'Download',
          filename: `Music/track-${index}.flac`,
          percentComplete: 0,
          size: 1_024,
          state: 'Queued',
          timestamp: new Date(2026, 9, 6, 10, index).toISOString(),
          username: 'alice',
        });
      }
    });

    const showMore = screen.getByRole('button', { name: 'Show 2 More' });
    await user.hover(showMore);
    expect(await screen.findByText(/Show 2 older transfer events/)).toBeInTheDocument();
    expect(screen.getAllByRole('listitem')).toHaveLength(10);

    await user.unhover(showMore);
    await user.click(showMore);
    const showLess = await screen.findByRole('button', { name: 'Show Less' });
    expect(screen.getAllByRole('listitem')).toHaveLength(12);
    await user.hover(showLess);
    expect(await screen.findByText(/Collapse the activity list to its 10 most recent/))
      .toBeInTheDocument();
  });
});
