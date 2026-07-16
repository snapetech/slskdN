// <copyright file="index.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import Mesh from './index';
import * as mesh from '../../../lib/mesh';
import * as soulseekDiscovery from '../../../lib/soulseekDiscovery';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../../lib/mesh', () => ({
  getStats: vi.fn(),
}));

vi.mock('../../../lib/soulseekDiscovery', () => ({
  addMeshRendezvousInterest: vi.fn(),
  discoverMeshRendezvous: vi.fn(),
  getMeshRendezvousStatus: vi.fn(),
  getMeshRendezvousUsers: vi.fn(),
  removeMeshRendezvousInterest: vi.fn(),
}));

vi.mock('./MeshEvidencePolicy', () => ({
  default: () => <div>Mesh Evidence Policy</div>,
}));

vi.mock('./RealmSubjectIndexConflicts', () => ({
  default: () => <div>Realm Subject Index Conflicts</div>,
}));

const meshStats = {
  activeCircuits: 0,
  activeStreams: 0,
  bootstrapPeers: [],
  connectedPeers: 0,
  description: 'Mesh transport ready',
  health: 'Healthy',
  isolatedPeers: 0,
  lastDhtError: null,
  lastDhtPublishUtc: null,
  natType: 'Unknown',
  publicEndpoint: null,
  quorumPeers: 0,
  relayedPeers: 0,
  status: 'Healthy',
  totalPeers: 0,
  transportPreference: 'Auto',
};

describe('System Mesh', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: false,
    });
    mesh.getStats.mockResolvedValue(meshStats);
    soulseekDiscovery.addMeshRendezvousInterest.mockResolvedValue({});
    soulseekDiscovery.discoverMeshRendezvous.mockResolvedValue({
      data: { capabilityRecords: [], users: [] },
    });
    soulseekDiscovery.removeMeshRendezvousInterest.mockResolvedValue({});
    soulseekDiscovery.getMeshRendezvousUsers.mockResolvedValue({ data: [] });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('pauses stats polling while hidden and catches up when visible', async () => {
    vi.useFakeTimers();
    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: true,
    });
    soulseekDiscovery.getMeshRendezvousStatus.mockResolvedValue({
      data: { enabled: false },
    });

    render(<Mesh />);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(90_000);
    });
    expect(mesh.getStats).not.toHaveBeenCalled();

    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: false,
    });
    await act(async () => {
      document.dispatchEvent(new Event('visibilitychange'));
      await Promise.resolve();
    });
    expect(mesh.getStats).toHaveBeenCalledTimes(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(30_000);
    });
    expect(mesh.getStats).toHaveBeenCalledTimes(2);

    Object.defineProperty(document, 'hidden', {
      configurable: true,
      value: true,
    });
    await act(async () => {
      document.dispatchEvent(new Event('visibilitychange'));
      await vi.advanceTimersByTimeAsync(90_000);
    });
    expect(mesh.getStats).toHaveBeenCalledTimes(2);
  });

  it('does not overlap slow stats requests', async () => {
    vi.useFakeTimers();
    let completeRequest;
    mesh.getStats.mockReturnValue(
      new Promise((resolve) => {
        completeRequest = resolve;
      }),
    );
    soulseekDiscovery.getMeshRendezvousStatus.mockResolvedValue({
      data: { enabled: false },
    });

    render(<Mesh />);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(90_000);
    });
    expect(mesh.getStats).toHaveBeenCalledTimes(1);

    await act(async () => {
      completeRequest(meshStats);
      await Promise.resolve();
    });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(30_000);
    });
    expect(mesh.getStats).toHaveBeenCalledTimes(2);
  });

  it('retains the last successful stats after a transient failure', async () => {
    vi.useFakeTimers();
    mesh.getStats
      .mockResolvedValueOnce(meshStats)
      .mockRejectedValueOnce(new Error('temporary failure'));
    soulseekDiscovery.getMeshRendezvousStatus.mockResolvedValue({
      data: { enabled: false },
    });

    render(<Mesh />);
    await act(async () => {
      await Promise.resolve();
    });
    expect(screen.getByText(/Network Health: Healthy/)).toBeInTheDocument();

    await act(async () => {
      await vi.advanceTimersByTimeAsync(30_000);
    });
    expect(screen.getByText(/Network Health: Healthy/)).toBeInTheDocument();
    expect(screen.queryByText('Failed to load mesh statistics')).not.toBeInTheDocument();
  });

  it('renders Soulseek rendezvous as disabled by default', async () => {
    soulseekDiscovery.getMeshRendezvousStatus.mockResolvedValue({
      data: {
        enabled: false,
        interestTag: 'slskdn-mesh-v1',
        privacy:
          'When enabled, adding the rendezvous interest publishes a recognizable slskdN mesh tag on this Soulseek account.',
      },
    });

    render(<Mesh />);

    expect(await screen.findByText('Soulseek Mesh Rendezvous')).toBeInTheDocument();
    expect(screen.getByText('Opt-in public rendezvous is disabled')).toBeInTheDocument();
    expect(screen.getByText('slskdn-mesh-v1')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Publish Interest/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /Load Candidates/i })).toBeDisabled();
  });

  it('publishes, removes, and loads rendezvous candidates when enabled', async () => {
    soulseekDiscovery.getMeshRendezvousStatus.mockResolvedValue({
      data: {
        enabled: true,
        interestTag: 'slskdn-mesh-v1',
        privacy:
          'When enabled, adding the rendezvous interest publishes a recognizable slskdN mesh tag on this Soulseek account.',
      },
    });
    soulseekDiscovery.discoverMeshRendezvous.mockResolvedValue({
      data: {
        capabilityRecords: [
          {
            features: ['mesh_sync'],
            nonce: 'nonce',
            overlayPort: 50305,
            peerId: 'peer-id',
            signed: true,
            username: 'mesh-peer',
          },
        ],
        users: [{ rating: 14, username: 'mesh-peer' }],
      },
    });

    render(<Mesh />);

    expect(await screen.findByText('Opt-in public rendezvous is enabled')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /Publish Interest/i }));
    await waitFor(() =>
      expect(soulseekDiscovery.addMeshRendezvousInterest).toHaveBeenCalled(),
    );

    fireEvent.click(screen.getByRole('button', { name: /Remove Interest/i }));
    await waitFor(() =>
      expect(soulseekDiscovery.removeMeshRendezvousInterest).toHaveBeenCalled(),
    );

    fireEvent.click(screen.getByRole('button', { name: /Load Candidates/i }));

    expect(await screen.findAllByText('mesh-peer')).toHaveLength(2);
    expect(
      screen.getByText((_, element) => element.textContent === 'Similarity rating: 14'),
    ).toBeInTheDocument();
    expect(screen.getByText(/peer-id/)).toBeInTheDocument();
  });

  it('ignores malformed rendezvous discovery list payloads', async () => {
    soulseekDiscovery.getMeshRendezvousStatus.mockResolvedValue({
      data: {
        enabled: true,
        interestTag: 'slskdn-mesh-v1',
      },
    });
    soulseekDiscovery.discoverMeshRendezvous.mockResolvedValue({
      data: {
        capabilityRecords: { peerId: 'peer-id' },
        users: { username: 'mesh-peer' },
      },
    });

    render(<Mesh />);

    expect(await screen.findByText('Soulseek Mesh Rendezvous')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /Load Candidates/i }));

    expect(
      await screen.findByText(
        'Discovered 0 Soulseek rendezvous candidate(s) and 0 runtime capability record(s).',
      ),
    ).toBeInTheDocument();
    expect(screen.queryByText('mesh-peer')).not.toBeInTheDocument();
    expect(screen.queryByText(/peer-id/)).not.toBeInTheDocument();
  });
});
