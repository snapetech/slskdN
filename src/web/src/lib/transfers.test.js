// <copyright file="transfers.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';
import * as transfers from './transfers';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    get: vi.fn(),
  },
}));

describe('transfers api', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('requests an initial snapshot and then an encoded cursor delta', async () => {
    api.get.mockResolvedValue({
      data: { cursor: 1_234, transfers: [{ id: 'transfer-1' }] },
    });

    await expect(transfers.getChanges()).resolves.toEqual({
      counts: { download: 0, upload: 0 },
      cursor: 1_234,
      transfers: [{ id: 'transfer-1' }],
    });
    await transfers.getChanges({ since: 1_233 });

    expect(api.get).toHaveBeenNthCalledWith(
      1,
      '/transfers/changes?includeCompleted=false',
    );
    expect(api.get).toHaveBeenNthCalledWith(
      2,
      '/transfers/changes?since=1233',
    );
  });

  it('normalizes a malformed change response to bounded empty data', async () => {
    api.get.mockResolvedValue({ data: { cursor: 'invalid', transfers: {} } });

    await expect(transfers.getChanges()).resolves.toEqual({
      counts: { download: 0, upload: 0 },
      cursor: null,
      transfers: [],
    });
  });

  it('requests and normalizes a stable completed-history page', async () => {
    api.get.mockResolvedValue({
      data: {
        asOf: 2_000,
        hasMore: true,
        nextOffset: 275,
        transfers: [{ id: 'completed-1' }],
      },
    });

    await expect(
      transfers.getHistory({
        asOf: 2_000,
        direction: 'download',
        limit: 25,
        offset: 250,
      }),
    ).resolves.toEqual({
      asOf: 2_000,
      hasMore: true,
      nextOffset: 275,
      transfers: [{ id: 'completed-1' }],
    });
    expect(api.get).toHaveBeenCalledWith(
      '/transfers/history?direction=download&limit=25&offset=250&asOf=2000',
    );
  });
});
