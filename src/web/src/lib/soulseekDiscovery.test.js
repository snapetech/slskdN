// <copyright file="soulseekDiscovery.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';
import * as soulseekDiscovery from './soulseekDiscovery';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    delete: vi.fn(),
    get: vi.fn(),
    post: vi.fn(),
  },
}));

describe('soulseekDiscovery', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('loads mesh rendezvous status from the versioned Soulseek route', async () => {
    api.get.mockResolvedValue({ data: { enabled: false } });

    await soulseekDiscovery.getMeshRendezvousStatus();

    expect(api.get).toHaveBeenCalledWith('/soulseek/mesh-rendezvous/status');
  });

  it('publishes the mesh rendezvous interest through the explicit opt-in route', async () => {
    api.post.mockResolvedValue({});

    await soulseekDiscovery.addMeshRendezvousInterest();

    expect(api.post).toHaveBeenCalledWith('/soulseek/mesh-rendezvous/interest');
  });

  it('removes the mesh rendezvous interest through the explicit opt-out route', async () => {
    api.delete.mockResolvedValue({});

    await soulseekDiscovery.removeMeshRendezvousInterest();

    expect(api.delete).toHaveBeenCalledWith('/soulseek/mesh-rendezvous/interest');
  });

  it('loads mesh rendezvous candidate users from the read-only route', async () => {
    api.get.mockResolvedValue({ data: [{ username: 'mesh-peer' }] });

    await soulseekDiscovery.getMeshRendezvousUsers();

    expect(api.get).toHaveBeenCalledWith('/soulseek/mesh-rendezvous/users');
  });

  it('discovers mesh rendezvous users and runtime capabilities from the active route', async () => {
    api.get.mockResolvedValue({ data: { users: [], capabilityRecords: [] } });

    await soulseekDiscovery.discoverMeshRendezvous();

    expect(api.get).toHaveBeenCalledWith('/soulseek/mesh-rendezvous/discover');
  });

  it('loads peer capability records from the runtime registry route', async () => {
    api.get.mockResolvedValue({ data: [{ username: 'mesh-peer' }] });

    await soulseekDiscovery.getPeerCapabilities();

    expect(api.get).toHaveBeenCalledWith('/soulseek/peer-capabilities');
  });
});
