// <copyright file="mediacore.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';
import * as mediacore from './mediacore';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    delete: vi.fn(),
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
  },
}));

describe('mediacore', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('uses the relative content-id stats path', async () => {
    api.get.mockResolvedValue({ data: { totalMappings: 0 } });

    await mediacore.getContentIdStats();

    expect(api.get).toHaveBeenCalledWith('/mediacore/contentid/stats');
  });

  it('uses the relative content-id resolve path', async () => {
    api.get.mockResolvedValue({ data: { contentId: 'cid-123' } });

    await mediacore.resolveContentId('artist:1');

    expect(api.get).toHaveBeenCalledWith(
      '/mediacore/contentid/resolve/artist%3A1',
    );
  });

  it('uses relative pod message storage paths', async () => {
    api.get.mockResolvedValue({ data: { results: [] } });
    api.post.mockResolvedValue({ data: { ok: true } });

    await mediacore.searchMessages('pod-1', 'needle', 'general', 10);
    await mediacore.rebuildSearchIndex();

    expect(api.get).toHaveBeenCalledWith('/podcore/messages/pod-1/search', {
      params: {
        channelId: 'general',
        limit: 10,
        query: 'needle',
      },
    });
    expect(api.post).toHaveBeenCalledWith('/podcore/messages/rebuild-index');
  });

  it('uses relative pod DHT and discovery paths', async () => {
    api.post.mockResolvedValue({ data: { registered: true } });
    api.get.mockResolvedValue({ data: { pods: [] } });

    await mediacore.publishPod({ podId: 'pod-1' });
    await mediacore.discoverPodsByName('ambient room');

    expect(api.post).toHaveBeenCalledWith('/podcore/dht/publish', {
      pod: { podId: 'pod-1' },
    });
    expect(api.get).toHaveBeenCalledWith(
      '/podcore/discovery/name/ambient%20room',
    );
  });

  it('uses relative pod membership, routing, signing, and verification paths', async () => {
    api.post.mockResolvedValue({ data: { ok: true } });
    api.get.mockResolvedValue({ data: { hasRole: true } });

    await mediacore.requestPodJoin({ podId: 'pod-1', peerId: 'peer-1' });
    await mediacore.routePodMessage({ messageId: 'msg-1' });
    await mediacore.signPodMessage({ messageId: 'msg-1' }, 'private-key');
    await mediacore.checkPodRole('pod-1', 'peer-1', 'admin');

    expect(api.post).toHaveBeenCalledWith('/podcore/membership/join', {
      podId: 'pod-1',
      peerId: 'peer-1',
    });
    expect(api.post).toHaveBeenCalledWith('/podcore/routing/route', {
      messageId: 'msg-1',
    });
    expect(api.post).toHaveBeenCalledWith('/podcore/signing/sign', {
      message: { messageId: 'msg-1' },
      privateKey: 'private-key',
    });
    expect(api.get).toHaveBeenCalledWith(
      '/podcore/verification/role/pod-1/peer-1/admin',
    );
  });

  it('uses relative pod backfill paths', async () => {
    api.post.mockResolvedValue({ data: { synced: true } });
    api.put.mockResolvedValue({ data: { updated: true } });

    await mediacore.syncPodBackfill('pod-1', { general: 123 });
    await mediacore.updateLastSeenTimestamp('pod-1', 'general', 456);

    expect(api.post).toHaveBeenCalledWith('/podcore/backfill/pod-1/sync', {
      general: 123,
    });
    expect(api.put).toHaveBeenCalledWith(
      '/podcore/backfill/pod-1/general/last-seen',
      456,
    );
  });

  it('uses relative pod opinion and channel paths', async () => {
    api.post.mockResolvedValue({ data: { ok: true } });
    api.delete.mockResolvedValue({ data: { deleted: true } });

    await mediacore.publishOpinion('pod-1', { contentId: 'cid-1' });
    await mediacore.deleteChannel('pod-1', 'general');

    expect(api.post).toHaveBeenCalledWith('/podcore/pod-1/opinions', {
      contentId: 'cid-1',
    });
    expect(api.delete).toHaveBeenCalledWith('/podcore/pod-1/channels/general');
  });

  it('uses relative pod content paths', async () => {
    api.post.mockResolvedValue({ data: { valid: true } });
    api.get.mockResolvedValue({ data: { items: [] } });

    await mediacore.validateContentIdForPod('content:1');
    await mediacore.searchContent('artist', 'music', 5);

    expect(api.post).toHaveBeenCalledWith(
      '/podcore/content/validate',
      '"content:1"',
    );
    expect(api.get).toHaveBeenCalledWith('/podcore/content/search', {
      params: {
        domain: 'music',
        limit: 5,
        query: 'artist',
      },
    });
  });
});
