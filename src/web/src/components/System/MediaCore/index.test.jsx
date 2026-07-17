// <copyright file="index.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as mediacore from '../../../lib/mediacore';
import MediaCore, { areContentIdStatsEqual } from './index';
import React from 'react';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../../lib/mediacore', () => ({
  getConflictStrategies: vi.fn(),
  getContentIdStats: vi.fn(),
  getChannels: vi.fn(),
  getSupportedHashAlgorithms: vi.fn(),
}));

vi.mock('react-toastify', () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}));

describe('MediaCore', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    window.history.replaceState({}, '', '/system/mediacore');
    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: false,
    });
    mediacore.getContentIdStats.mockResolvedValue({
      mappingsByDomain: {},
      totalDomains: 0,
      totalMappings: 0,
    });
    mediacore.getSupportedHashAlgorithms.mockResolvedValue({
      algorithms: [],
      descriptions: {},
    });
    mediacore.getConflictStrategies.mockResolvedValue([]);
    mediacore.getChannels.mockResolvedValue([]);
  });

  afterEach(() => {
    vi.useRealTimers();
    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: false,
    });
  });

  it('renders a pod workflow index with safety framing', async () => {
    render(<MediaCore />);

    expect(await screen.findByText('Pod Workflow Index')).toBeInTheDocument();
    expect(screen.getByText(/Pod workflows mix read-only diagnostics/)).toBeInTheDocument();
    expect(screen.getByText('Workflow focus')).toBeInTheDocument();
    expect(screen.getAllByText('Show all pod workflows').length).toBeGreaterThan(0);
    expect(screen.getAllByText('DHT Publishing').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Verification').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Signing').length).toBeGreaterThan(0);
    expect(screen.getByText('Publishes metadata')).toBeInTheDocument();
    expect(screen.getAllByText('Handles key material').length).toBeGreaterThan(0);
    expect(screen.getByText('Read-only verification')).toBeInTheDocument();
    expect(screen.getByText('Publishes pod metadata')).toBeInTheDocument();
    expect(screen.getByText('Mutates local message storage')).toBeInTheDocument();
    expect(screen.getAllByText('Publishes opinion data').length).toBeGreaterThan(0);
    expect(screen.getByRole('link', { name: /DHT Publishing/ })).toHaveAttribute(
      'href',
      '/system/mediacore#podcore-dht-publishing',
    );
    expect(screen.getByText('Find pods first')).toBeInTheDocument();
    expect(screen.getByText('Advanced registry publishing controls')).toBeInTheDocument();
    expect(screen.getByText(/changes public DHT-visible pod metadata/)).toBeInTheDocument();
    expect(screen.getByText('Review pending requests first')).toBeInTheDocument();
    expect(screen.getByText('Advanced signed membership event controls')).toBeInTheDocument();
    expect(screen.getByText(/submit signed JSON payloads/)).toBeInTheDocument();
    expect(screen.getByText('Verify before generating signatures')).toBeInTheDocument();
    expect(screen.getByText('Advanced key material and signing controls')).toBeInTheDocument();
    expect(screen.getByText(/handle private keys/)).toBeInTheDocument();
    expect(screen.getByText('Review channels before changing them')).toBeInTheDocument();
    expect(screen.getByText('Advanced channel mutation controls')).toBeInTheDocument();
    expect(screen.getByText(/changes how pod messages are organized/)).toBeInTheDocument();
    expect(screen.getByText('Read pod opinions first')).toBeInTheDocument();
    expect(screen.getByText('Advanced opinion publishing controls')).toBeInTheDocument();
    expect(screen.getByText('Advanced affinity recalculation controls')).toBeInTheDocument();
    expect(screen.queryByText('Advanced content-linked pod creation controls')).not.toBeInTheDocument();
    expect(screen.getByText('Review storage before maintenance')).toBeInTheDocument();
    expect(screen.getByText('Advanced storage maintenance controls')).toBeInTheDocument();
    expect(screen.getByText('Advanced backfill sync controls')).toBeInTheDocument();
    expect(screen.getByText('Retrieve DHT metadata first')).toBeInTheDocument();
    expect(screen.getByText('Advanced DHT publishing controls')).toBeInTheDocument();
    expect(screen.getByText('Verify membership before changing it')).toBeInTheDocument();
    expect(screen.getByText('Advanced member mutation controls')).toBeInTheDocument();
    expect(screen.getByText('Advanced membership publishing controls')).toBeInTheDocument();
    expect(screen.getByText('Advanced membership cleanup controls')).toBeInTheDocument();
    expect(screen.getByText('Check routing state before sending')).toBeInTheDocument();
    expect(screen.getByText('Advanced message routing controls')).toBeInTheDocument();
    expect(screen.getByText('Advanced seen-state cleanup controls')).toBeInTheDocument();
    expect(screen.getByText('Advanced raw audio hash controls')).toBeInTheDocument();
    expect(screen.getByText('Advanced raw image hash controls')).toBeInTheDocument();
    expect(screen.getAllByText(/Similarity review and hashing statistics/).length).toBeGreaterThan(0);
    expect(screen.getByText('Advanced fresh DHT retrieval controls')).toBeInTheDocument();
    expect(screen.getByText('Advanced batch DHT retrieval controls')).toBeInTheDocument();
    expect(screen.getByText(/Cached single-descriptor retrieval is the default path/)).toBeInTheDocument();
    expect(screen.getByText(/Batch retrieval can fan out across multiple descriptors/)).toBeInTheDocument();
    expect(screen.getByText('Advanced similarity candidate search controls')).toBeInTheDocument();
    expect(screen.getByText(/Candidate search can scan registry entries/)).toBeInTheDocument();
  });

  it('hydrates stats after Strict Mode replays the polling effect', async () => {
    render(
      <React.StrictMode>
        <MediaCore />
      </React.StrictMode>,
    );

    expect(await screen.findByText('Pod Workflow Index')).toBeInTheDocument();
    expect(mediacore.getContentIdStats).toHaveBeenCalledTimes(1);
  });

  it('compares ContentID stats by rendered fields', () => {
    const stats = {
      mappingsByDomain: { audio: 2, video: 1 },
      totalDomains: 2,
      totalMappings: 3,
    };

    expect(areContentIdStatsEqual(stats, { ...stats })).toBe(true);
    expect(areContentIdStatsEqual(stats, {
      ...stats,
      mappingsByDomain: { audio: 1, video: 2 },
    })).toBe(false);
    expect(areContentIdStatsEqual(stats, {
      ...stats,
      totalMappings: 4,
    })).toBe(false);
  });

  it('focuses a pod workflow from the index card', async () => {
    render(<MediaCore />);

    fireEvent.click(await screen.findByRole('link', { name: /DHT Publishing/ }));

    expect(
      screen.getByText(/Showing DHT Publishing/),
    ).toBeInTheDocument();

    fireEvent.click(screen.getAllByText('Show all pod workflows').at(-1));

    expect(screen.queryByText(/Showing DHT Publishing/)).not.toBeInTheDocument();
  });

  it('fills read-first ContentID fields from examples', async () => {
    render(<MediaCore />);

    fireEvent.click(await screen.findByText('audio:track'));

    expect(screen.getAllByDisplayValue('mb:recording:12345').length).toBeGreaterThan(0);
    expect(screen.getAllByDisplayValue('content:audio:track:mb-12345').length).toBeGreaterThan(0);
    expect(
      screen.getByText(/Click any example to fill the read-only resolve and validation fields/),
    ).toBeInTheDocument();
  });

  it('treats malformed pod channel payloads as empty lists', async () => {
    mediacore.getChannels.mockResolvedValue('bad');

    render(<MediaCore />);

    fireEvent.change(await screen.findByPlaceholderText('Pod ID for channel management'), {
      target: { value: 'pod/with/slash' },
    });
    fireEvent.click(screen.getByText('Load Channels'));

    await waitFor(() => expect(mediacore.getChannels).toHaveBeenCalledWith('pod/with/slash'));
    expect(await screen.findByText('Load Pod Channels')).toBeInTheDocument();
    expect(screen.queryByText('Existing Channels')).not.toBeInTheDocument();
  });

  it('refreshes ContentID stats once per visible minute', async () => {
    vi.useFakeTimers();

    render(<MediaCore />);
    await act(async () => {
      await Promise.resolve();
    });

    expect(mediacore.getContentIdStats).toHaveBeenCalledTimes(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(59_999);
    });
    expect(mediacore.getContentIdStats).toHaveBeenCalledTimes(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    expect(mediacore.getContentIdStats).toHaveBeenCalledTimes(2);
  });

  it('does not overlap slow ContentID stats refreshes', async () => {
    vi.useFakeTimers();
    let resolveStats;
    mediacore.getContentIdStats.mockReturnValue(
      new Promise((resolve) => {
        resolveStats = resolve;
      }),
    );

    render(<MediaCore />);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(180_000);
    });

    expect(mediacore.getContentIdStats).toHaveBeenCalledTimes(1);

    resolveStats({
      mappingsByDomain: {},
      totalDomains: 0,
      totalMappings: 0,
    });
    await act(async () => {
      await Promise.resolve();
      await vi.advanceTimersByTimeAsync(60_000);
    });

    expect(mediacore.getContentIdStats).toHaveBeenCalledTimes(2);
  });

  it('suspends ContentID stats polling while hidden and catches up on visibility', async () => {
    vi.useFakeTimers();
    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: true,
    });

    render(<MediaCore />);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(120_000);
    });
    expect(mediacore.getContentIdStats).not.toHaveBeenCalled();

    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: false,
    });
    await act(async () => {
      document.dispatchEvent(new Event('visibilitychange'));
      await Promise.resolve();
    });
    expect(mediacore.getContentIdStats).toHaveBeenCalledTimes(1);

    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: true,
    });
    await act(async () => {
      document.dispatchEvent(new Event('visibilitychange'));
      await vi.advanceTimersByTimeAsync(120_000);
    });
    expect(mediacore.getContentIdStats).toHaveBeenCalledTimes(1);
  });
});
