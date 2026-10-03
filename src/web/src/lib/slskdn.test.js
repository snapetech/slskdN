// <copyright file="slskdn.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';
import * as slskdn from './slskdn';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

describe('slskdn', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns effective experimental feature gates from capabilities', async () => {
    const capabilities = {
      featureGates: {
        pods: { enabled: false, status: 'Disabled' },
      },
      features: [],
    };
    api.get.mockResolvedValue({ data: capabilities });

    await expect(slskdn.getCapabilities()).resolves.toEqual(capabilities);
    expect(api.get).toHaveBeenCalledWith('/capabilities');
  });

  it('keeps capabilities available when the endpoint is missing', async () => {
    api.get.mockRejectedValue({ response: { status: 404 } });

    await expect(slskdn.getCapabilities()).resolves.toEqual({
      featureGates: {},
      features: [],
    });
  });

  it('encodes dynamic route segments', async () => {
    api.get.mockResolvedValue({ data: { id: 'job/1' } });
    api.post.mockResolvedValue({ data: { success: true } });

    await slskdn.triggerMeshSync('alice/bob');
    await slskdn.getSwarmJob('job/1');

    expect(api.post).toHaveBeenCalledWith('/mesh/sync/alice%2Fbob');
    expect(api.get).toHaveBeenCalledWith('/multisource/jobs/job%2F1');
  });

  it('preserves the server error when mesh sync is rejected', async () => {
    api.post.mockRejectedValueOnce({
      message: 'Request failed with status code 400',
      response: { data: { error: 'Failed to sync with peer' } },
    });

    await expect(slskdn.triggerMeshSync('mesh-peer')).resolves.toEqual({
      error: 'Failed to sync with peer',
      success: false,
    });
  });

  it('normalizes the combined network snapshot and real peer lists', async () => {
    api.get.mockResolvedValue({
      data: {
        backfill: { active: 1 },
        capabilitiesJson: JSON.stringify({
          features: ['mesh_sync'],
          version: '1.2.3',
        }),
        capabilitiesVersion: 'slskdn/1.2.3+mesh',
        dht: { dhtNodeCount: 5, lanOnly: true },
        discoveredPeers: [
          {
            clientVersion: 'slskdn/1.2.2',
            lastSeen: '2026-07-15T00:00:00Z',
            username: 'discovered-peer',
          },
        ],
        hashDb: {
          currentSeqId: 12,
          databaseSizeBytes: 2_048,
          hashedFlacEntries: 25,
          totalFlacEntries: 100,
          totalHashEntries: 42,
        },
        mesh: { currentSeqId: 12, knownMeshPeers: 2 },
        meshPeers: [
          {
            lastSyncTime: '2026-07-15T00:00:00Z',
            latestSeqId: 11,
            username: 'mesh-peer',
          },
        ],
        swarmJobs: [
          {
            activeWorkers: 3,
            bytesDownloaded: 512,
            completedChunks: 2,
            fileSize: 1_024,
            id: 'job-1',
            totalChunks: 4,
          },
        ],
        transport: {
          activeDhtSessions: 5,
          activeOverlaySessions: 2,
          detectedNatType: 'FullCone',
        },
      },
    });

    const result = await slskdn.getSlskdnStats({ includePeers: true });

    expect(api.get).toHaveBeenCalledWith('/network/stats?includePeers=true');
    expect(result).toMatchObject({
      backfill: { isActive: true },
      capabilities: { features: ['mesh_sync'], version: '1.2.3' },
      dht: { isLanOnly: true },
      hashDb: {
        coveragePercent: 25,
        dbSizeBytes: 2_048,
        totalEntries: 42,
      },
      mesh: { connectedPeerCount: 2, localSeqId: 12 },
      transport: { dht: 5, natType: 'FullCone', overlay: 2 },
    });
    expect(result.meshPeers).toEqual([
      expect.objectContaining({ lastSeqId: 11, username: 'mesh-peer' }),
    ]);
    expect(result.discoveredPeers).toEqual([
      expect.objectContaining({
        username: 'discovered-peer',
        version: 'slskdn/1.2.2',
      }),
    ]);
    expect(result.swarmJobs).toEqual([
      expect.objectContaining({
        activeSources: 3,
        downloadedBytes: 512,
        jobId: 'job-1',
        progressPercent: 50,
        totalBytes: 1_024,
      }),
    ]);
  });

  it('returns a bounded empty snapshot for malformed network data', async () => {
    api.get.mockResolvedValue({ data: [] });

    await expect(slskdn.getSlskdnStats()).resolves.toMatchObject({
      discoveredPeers: [],
      meshPeers: [],
      swarmJobs: [],
    });
    expect(api.get).toHaveBeenCalledWith('/network/stats');
  });
});
