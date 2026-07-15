// <copyright file="collections.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';
import * as collections from './collections';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    delete: vi.fn(),
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
  },
}));

describe('collections', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('encodes collection and share group route segments', async () => {
    api.delete.mockResolvedValue({});
    api.get.mockResolvedValue({});
    api.post.mockResolvedValue({});
    api.put.mockResolvedValue({});

    await collections.getShareGroup('group/1');
    await collections.getShareGroupMembers('group/1', true);
    await collections.removeShareGroupMember('group/1', 'alice/bob');
    await collections.getCollection('collection/1');
    await collections.updateCollectionItem('item/1', { title: 'track' });
    await collections.getShare('share/1');
    await collections.getShareManifest('share/1', 'token/1');
    await collections.backfillShare('share/1');

    expect(api.get).toHaveBeenCalledWith('/sharegroups/group%2F1');
    expect(api.get).toHaveBeenCalledWith('/sharegroups/group%2F1/members?detailed=true');
    expect(api.delete).toHaveBeenCalledWith('/sharegroups/group%2F1/members/alice%2Fbob');
    expect(api.get).toHaveBeenCalledWith('/collections/collection%2F1');
    expect(api.put).toHaveBeenCalledWith('/collections/items/item%2F1', { title: 'track' });
    expect(api.get).toHaveBeenCalledWith('/share-grants/share%2F1');
    expect(api.get).toHaveBeenCalledWith('/share-grants/share%2F1/manifest', {
      headers: { 'X-Share-Token': 'token/1' },
    });
    expect(api.post).toHaveBeenCalledWith('/share-grants/share%2F1/backfill');
  });
});
