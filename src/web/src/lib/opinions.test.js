// <copyright file="opinions.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';
import * as opinions from './opinions';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    delete: vi.fn(),
    get: vi.fn(),
    post: vi.fn(),
  },
}));

describe('opinions', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('loads subject summaries through the relative opinions route', async () => {
    api.get.mockResolvedValue({ data: { total: 0 } });

    await opinions.getOpinionSummary({
      subjectId: 'alice',
      subjectType: 'User',
    });

    expect(api.get).toHaveBeenCalledWith('/opinions/summary', {
      params: {
        scope: 'global',
        subjectId: 'alice',
        subjectType: 'User',
      },
    });
  });

  it('lists and mutates opinions through relative routes', async () => {
    api.delete.mockResolvedValue({});
    api.get.mockResolvedValue({ data: [] });
    api.post.mockResolvedValue({ data: { id: 'opinion:1' } });

    await opinions.listOpinions({
      issuer: 'local:test',
      kind: 'Trust',
      scope: 'global',
      source: 'operator',
      subjectId: 'peer/1',
      subjectType: 'MeshPeer',
    });
    await opinions.submitOpinion({ subjectId: 'peer/1' });
    await opinions.deleteOpinion('opinion/1');

    expect(api.get).toHaveBeenCalledWith('/opinions', {
      params: {
        includeExpired: false,
        issuer: 'local:test',
        kind: 'Trust',
        limit: 100,
        scope: 'global',
        source: 'operator',
        subjectId: 'peer/1',
        subjectType: 'MeshPeer',
      },
    });
    expect(api.post).toHaveBeenCalledWith('/opinions', {
      subjectId: 'peer/1',
    });
    expect(api.delete).toHaveBeenCalledWith('/opinions/opinion%2F1');
  });
});
