// <copyright file="wishlist.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';
import * as wishlist from './wishlist';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    delete: vi.fn(),
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
  },
}));

describe('wishlist', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('encodes wishlist route segments', async () => {
    api.delete.mockResolvedValue({});
    api.get.mockResolvedValue({ data: { id: 'wish/1' } });
    api.post.mockResolvedValue({ data: { responseCount: 1 } });
    api.put.mockResolvedValue({ data: { id: 'wish/1' } });

    await wishlist.get('wish/1');
    await wishlist.update('wish/1', {
      autoDownload: false,
      enabled: true,
      filter: {},
      maxResults: 10,
      searchText: 'rare track',
    });
    await wishlist.remove('wish/1');
    await wishlist.runSearch('wish/1');

    expect(api.get).toHaveBeenCalledWith('/wishlist/wish%2F1');
    expect(api.put).toHaveBeenCalledWith('/wishlist/wish%2F1', {
      autoDownload: false,
      enabled: true,
      filter: {},
      maxResults: 10,
      searchText: 'rare track',
    });
    expect(api.delete).toHaveBeenCalledWith('/wishlist/wish%2F1');
    expect(api.post).toHaveBeenCalledWith('/wishlist/wish%2F1/search');
  });

  it('manages ignored result routes with encoded identifiers', async () => {
    api.delete.mockResolvedValue({});
    api.get.mockResolvedValue({ data: [] });
    api.post.mockResolvedValue({ data: { id: 'rule/1' } });

    await wishlist.getIgnoredResults('wish/1');
    await wishlist.ignoreResult('wish/1', { directory: 'Artist/Album', username: 'peer' });
    await wishlist.removeIgnoredResult('wish/1', 'rule/1');

    expect(api.get).toHaveBeenCalledWith('/wishlist/wish%2F1/ignored-results');
    expect(api.post).toHaveBeenCalledWith('/wishlist/wish%2F1/ignored-results', {
      directory: 'Artist/Album',
      username: 'peer',
    });
    expect(api.delete).toHaveBeenCalledWith(
      '/wishlist/wish%2F1/ignored-results/rule%2F1',
    );
  });
});
