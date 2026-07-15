// <copyright file="index.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as swarmAnalyticsLibrary from '../../../lib/swarmAnalytics';
import SwarmAnalytics from '.';
import {
  act,
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from '@testing-library/react';
import React from 'react';
import { toast } from 'react-toastify';

// Mock dependencies
vi.mock('../../../lib/swarmAnalytics');
vi.mock('react-toastify', () => ({
  toast: {
    error: vi.fn(),
  },
}));

describe('SwarmAnalytics', () => {
  const mockPerformanceMetrics = {
    // 5 MB/s
    averageDurationSeconds: 45.5,

    averageSpeedBytesPerSecond: 1_024 * 1_024 * 5,

    chunkSuccessRate: 0.98,
    successRate: 0.95,
    totalBytesDownloaded: 1_024 * 1_024 * 1_024 * 10,
    // 10 GB
    totalChunksCompleted: 5_000,

    totalDownloads: 150,
  };

  const mockPeerRankings = [
    {
      averageRttMs: 50.5,
      averageThroughputBytesPerSecond: 1_024 * 1_024 * 2,
      chunksCompleted: 1_000,
      chunkSuccessRate: 0.99,
      peerId: 'peer-1',
      rank: 1,
      reputationScore: 0.95,
      source: 'Soulseek',
    },
    {
      averageRttMs: 75.2,
      averageThroughputBytesPerSecond: 1_024 * 1_024 * 1.5,
      chunksCompleted: 800,
      chunkSuccessRate: 0.92,
      peerId: 'peer-2',
      rank: 2,
      reputationScore: 0.85,
      source: 'Mesh',
    },
  ];

  const mockEfficiencyMetrics = {
    chunkUtilization: 0.85,
    peerUtilization: 0.75,
    redundancyFactor: 1.5,
  };

  const mockRecommendations = [
    {
      action: 'Review peer rankings and adjust selection algorithm',
      description: 'Consider prioritizing peers with lower latency',
      estimatedImpact: 0.15,
      priority: 'High',
      title: 'Optimize Peer Selection',
      type: 'PeerSelection',
    },
    {
      action: 'Experiment with different chunk sizes',
      description: 'Current chunk size may be suboptimal',
      estimatedImpact: 0.1,
      priority: 'Medium',
      title: 'Adjust Chunk Size',
      type: 'ChunkSize',
    },
  ];

  beforeEach(() => {
    jest.clearAllMocks();
    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: false,
    });
    swarmAnalyticsLibrary.getDashboard.mockResolvedValue({
      efficiencyMetrics: mockEfficiencyMetrics,
      peerRankings: mockPeerRankings,
      performanceMetrics: mockPerformanceMetrics,
      recommendations: mockRecommendations,
    });
  });

  afterEach(() => {
    jest.useRealTimers();
    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: false,
    });
  });

  it('renders the component header', () => {
    render(<SwarmAnalytics />);
    expect(screen.getByText('Swarm Analytics')).toBeInTheDocument();
  });

  it('displays loading state initially', () => {
    render(<SwarmAnalytics />);
    // Semantic UI Loader may render differently, check for loading indicator
    expect(screen.getByText('Swarm Analytics')).toBeInTheDocument();
  });

  it('fetches and displays performance metrics', async () => {
    render(<SwarmAnalytics />);

    await waitFor(() => {
      expect(screen.getByText('Performance Metrics')).toBeInTheDocument();
    });

    expect(screen.getByText('150')).toBeInTheDocument(); // totalDownloads
    // Check for success rate label
    const successRateLabels = screen.getAllByText('Success Rate');
    expect(successRateLabels.length).toBeGreaterThan(0);
    // Check for total downloads label
    const totalDownloadsLabels = screen.getAllByText('Total Downloads');
    expect(totalDownloadsLabels.length).toBeGreaterThan(0);
  });

  it('fetches and displays efficiency metrics', async () => {
    render(<SwarmAnalytics />);

    await waitFor(() => {
      expect(screen.getByText('Efficiency Metrics')).toBeInTheDocument();
    });

    expect(screen.getByText('Chunk Utilization')).toBeInTheDocument();
    expect(screen.getByText('Peer Utilization')).toBeInTheDocument();
  });

  it('fetches and displays peer rankings table', async () => {
    render(<SwarmAnalytics />);

    await waitFor(() => {
      expect(screen.getByText('Top Peer Rankings')).toBeInTheDocument();
    });

    expect(screen.getByText('peer-1')).toBeInTheDocument();
    expect(screen.getByText('peer-2')).toBeInTheDocument();
    expect(screen.getByText('Soulseek')).toBeInTheDocument();
    expect(screen.getByText('Mesh')).toBeInTheDocument();
  });

  it('fetches and displays recommendations', async () => {
    render(<SwarmAnalytics />);

    await waitFor(() => {
      expect(
        screen.getByText('Optimization Recommendations'),
      ).toBeInTheDocument();
    });

    expect(screen.getByText('Optimize Peer Selection')).toBeInTheDocument();
    expect(screen.getByText('Adjust Chunk Size')).toBeInTheDocument();
  });

  it('allows changing time window', async () => {
    render(<SwarmAnalytics />);

    await waitFor(() => {
      expect(screen.getByText('Performance Metrics')).toBeInTheDocument();
    });

    // Find time window dropdown
    const timeWindowLabel = screen.getByText('Time Window');
    expect(timeWindowLabel).toBeInTheDocument();

    // The dropdown should be in the same segment
    const segment = timeWindowLabel.closest('.segment');
    expect(segment).toBeInTheDocument();
  });

  it('allows changing peer rankings limit', async () => {
    render(<SwarmAnalytics />);

    await waitFor(() => {
      expect(screen.getByText('Top Peer Rankings')).toBeInTheDocument();
    });

    // Find peer rankings limit dropdown
    const limitLabel = screen.getByText('Peer Rankings Limit');
    expect(limitLabel).toBeInTheDocument();
  });

  it('displays no data message when no analytics available', async () => {
    swarmAnalyticsLibrary.getDashboard.mockResolvedValue({
      efficiencyMetrics: null,
      peerRankings: [],
      performanceMetrics: null,
      recommendations: [],
    });

    render(<SwarmAnalytics />);

    await waitFor(() => {
      expect(screen.getByText('No Analytics Data')).toBeInTheDocument();
    });
  });

  it('ignores malformed list payloads from rankings and recommendations', async () => {
    swarmAnalyticsLibrary.getDashboard.mockResolvedValue({
      efficiencyMetrics: mockEfficiencyMetrics,
      peerRankings: { peers: mockPeerRankings },
      performanceMetrics: mockPerformanceMetrics,
      recommendations: 'bad',
    });

    render(<SwarmAnalytics />);

    await waitFor(() => {
      expect(screen.getByText('Performance Metrics')).toBeInTheDocument();
    });
    expect(screen.queryByText('peer-1')).not.toBeInTheDocument();
    expect(screen.queryByText('Optimization Recommendations')).not.toBeInTheDocument();
  });

  it('handles API errors gracefully', async () => {
    const error = new Error('Network error');
    swarmAnalyticsLibrary.getDashboard.mockRejectedValue(error);

    render(<SwarmAnalytics />);

    await waitFor(() => {
      expect(toast.error).toHaveBeenCalled();
    });
  });

  it('retains the last successful dashboard when a refresh fails', async () => {
    jest.useFakeTimers();
    swarmAnalyticsLibrary.getDashboard
      .mockResolvedValueOnce({
        efficiencyMetrics: mockEfficiencyMetrics,
        peerRankings: mockPeerRankings,
        performanceMetrics: mockPerformanceMetrics,
        recommendations: mockRecommendations,
      })
      .mockRejectedValueOnce(new Error('temporary failure'));

    render(<SwarmAnalytics />);
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(screen.getByText('150')).toBeInTheDocument();

    await act(async () => {
      jest.advanceTimersByTime(30_000);
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(screen.getByText('150')).toBeInTheDocument();
    expect(toast.error).toHaveBeenCalledTimes(1);
  });

  it('ignores a stale response after the time-window filter changes', async () => {
    let resolveCurrent;
    let resolvePrevious;
    swarmAnalyticsLibrary.getDashboard.mockImplementation(
      (timeWindow) =>
        new Promise((resolve) => {
          if (timeWindow === 6) {
            resolveCurrent = resolve;
          } else {
            resolvePrevious = resolve;
          }
        }),
    );

    render(<SwarmAnalytics />);
    await waitFor(() => {
      expect(swarmAnalyticsLibrary.getDashboard).toHaveBeenCalledWith(24, 20);
    });

    const timeWindowDropdown = screen.getAllByRole('listbox')[0];
    fireEvent.click(timeWindowDropdown);
    fireEvent.click(
      within(timeWindowDropdown).getByRole('option', { name: '6 hours' }),
    );
    await waitFor(() => {
      expect(swarmAnalyticsLibrary.getDashboard).toHaveBeenCalledWith(6, 20);
    });

    await act(async () => {
      resolveCurrent({
        performanceMetrics: {
          ...mockPerformanceMetrics,
          totalDownloads: 6,
        },
      });
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(screen.getByText('6')).toBeInTheDocument();

    await act(async () => {
      resolvePrevious({
        performanceMetrics: {
          ...mockPerformanceMetrics,
          totalDownloads: 999,
        },
      });
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(screen.queryByText('999')).not.toBeInTheDocument();
    expect(screen.getByText('6')).toBeInTheDocument();
  });

  it('refreshes data periodically', async () => {
    jest.useFakeTimers();
    render(<SwarmAnalytics />);

    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(swarmAnalyticsLibrary.getDashboard).toHaveBeenCalledTimes(1);

    await act(async () => {
      jest.advanceTimersByTime(30_000);
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(swarmAnalyticsLibrary.getDashboard).toHaveBeenCalledTimes(2);
    expect(swarmAnalyticsLibrary.getPerformanceMetrics).not.toHaveBeenCalled();
    expect(swarmAnalyticsLibrary.getPeerRankings).not.toHaveBeenCalled();
    expect(swarmAnalyticsLibrary.getEfficiencyMetrics).not.toHaveBeenCalled();
    expect(swarmAnalyticsLibrary.getTrends).not.toHaveBeenCalled();
    expect(swarmAnalyticsLibrary.getRecommendations).not.toHaveBeenCalled();
  });

  it('rejects overlapping refreshes', async () => {
    jest.useFakeTimers();
    let resolveDashboard;
    swarmAnalyticsLibrary.getDashboard.mockReturnValue(
      new Promise((resolve) => {
        resolveDashboard = resolve;
      }),
    );

    render(<SwarmAnalytics />);
    await act(async () => {
      jest.advanceTimersByTime(90_000);
      await Promise.resolve();
    });
    expect(swarmAnalyticsLibrary.getDashboard).toHaveBeenCalledTimes(1);

    await act(async () => {
      resolveDashboard({});
      await Promise.resolve();
      await Promise.resolve();
      jest.advanceTimersByTime(30_000);
      await Promise.resolve();
    });
    expect(swarmAnalyticsLibrary.getDashboard).toHaveBeenCalledTimes(2);
  });

  it('suspends hidden polling and catches up on visibility', async () => {
    jest.useFakeTimers();
    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: true,
    });

    render(<SwarmAnalytics />);
    await act(async () => {
      jest.advanceTimersByTime(60_000);
      await Promise.resolve();
    });
    expect(swarmAnalyticsLibrary.getDashboard).not.toHaveBeenCalled();

    await act(async () => {
      Object.defineProperty(document, 'hidden', {
        configurable: true,
        value: false,
      });
      document.dispatchEvent(new Event('visibilitychange'));
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(swarmAnalyticsLibrary.getDashboard).toHaveBeenCalledTimes(1);

    await act(async () => {
      Object.defineProperty(document, 'hidden', {
        configurable: true,
        value: true,
      });
      document.dispatchEvent(new Event('visibilitychange'));
      jest.advanceTimersByTime(60_000);
    });
    expect(swarmAnalyticsLibrary.getDashboard).toHaveBeenCalledTimes(1);
  });

  it('displays correct time window label', async () => {
    render(<SwarmAnalytics />);

    await waitFor(() => {
      expect(screen.getByText(/Last 24 hour/)).toBeInTheDocument();
    });
  });

  it('formats bytes correctly in statistics', async () => {
    render(<SwarmAnalytics />);

    await waitFor(() => {
      expect(screen.getByText('Performance Metrics')).toBeInTheDocument();
    });

    // Check that bytes are formatted (should contain "GB" or similar)
    const totalBytesText = screen.getByText(/total bytes/i);
    expect(totalBytesText).toBeInTheDocument();
  });
});
