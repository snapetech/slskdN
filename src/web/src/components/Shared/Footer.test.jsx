import '@testing-library/jest-dom';
import Footer from './Footer';
import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { vi } from 'vitest';

const { getBuild, getSlskdnStats, getStats, getSpeeds, isLoggedIn } = vi.hoisted(() => ({
  getBuild: vi.fn(),
  getSlskdnStats: vi.fn(),
  getSpeeds: vi.fn(),
  getStats: vi.fn(),
  isLoggedIn: vi.fn(),
}));

vi.mock('../../lib/application', () => ({
  getBuild,
}));

vi.mock('../../lib/mesh', () => ({
  getStats,
}));

vi.mock('../../lib/session', () => ({
  isLoggedIn,
}));

vi.mock('../../lib/slskdn', () => ({
  getSlskdnStats,
}));

vi.mock('../../lib/transfers', () => ({
  getSpeeds,
}));

describe('Footer', () => {
  beforeEach(() => {
    isLoggedIn.mockReturnValue(true);
    getStats.mockResolvedValue({
      dht: 12,
      natType: 'FullCone',
      overlay: 3,
    });
    getSlskdnStats.mockResolvedValue({
      backfill: { isActive: true },
      dht: { discoveredPeerCount: 23, dhtNodeCount: 12 },
      hashDb: { currentSeqId: 14638, totalEntries: 7_300 },
      mesh: { connectedPeerCount: 6, isSyncing: true },
      swarmJobs: [{ id: 'job-1' }],
    });
    getSpeeds.mockResolvedValue({
      mesh: 2_048,
      soulseek: 4_096,
      total: 6_144,
    });
    getBuild.mockResolvedValue({
      current: '0.0.0-slskdn.manual.test',
      full: '0.0.0-slskdn.manual.test (0.0.0-slskdn.manual.test)',
      isUpdateAvailable: false,
      latest: '0.0.0-slskdn.manual.test',
      latestTag: '0.0.0-slskdn.manual.test',
      latestUrl: 'https://github.com/snapetech/slskdn/releases/tag/test',
    });
    localStorage.setItem('slskdn-karma', '2');
  });

  afterEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('renders slskdN network stats in the footer', async () => {
    render(<Footer />);

    await waitFor(() => {
      expect(screen.getByText('23 dht')).toBeInTheDocument();
    });

    expect(screen.getByText('6 mesh')).toBeInTheDocument();
    expect(screen.getByText('7.3K hashes')).toBeInTheDocument();
    expect(screen.getByText('seq:14638')).toBeInTheDocument();
    expect(screen.getByText('1 swarm')).toBeInTheDocument();
    expect(screen.getByText('backfill')).toBeInTheDocument();
    expect(screen.getByText('+2')).toBeInTheDocument();
  });

  it('renders build info and checks for updates when logged out', async () => {
    isLoggedIn.mockReturnValue(false);
    getBuild.mockResolvedValue({
      current: '0.0.0-slskdn.manual.local',
      full: '0.0.0-slskdn.manual.local (0.0.0-slskdn.manual.local)',
      isUpdateAvailable: true,
      latest: '2026050500-slskdn.221',
      latestTag: 'build-main-2026050500-slskdn.221',
      latestUrl: 'https://github.com/snapetech/slskdn/releases/tag/build-main-2026050500-slskdn.221',
    });

    render(<Footer />);

    expect(await screen.findByText('0.0.0-slskdn.manual.local')).toBeInTheDocument();
    expect(screen.getByText('update 2026050500-slskdn.221')).toBeInTheDocument();
    expect(getBuild).toHaveBeenCalledWith({ checkForUpdates: true });
    expect(getStats).not.toHaveBeenCalled();
    expect(getSlskdnStats).not.toHaveBeenCalled();
    expect(getSpeeds).not.toHaveBeenCalled();
  });
});
