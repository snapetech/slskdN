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

  it('encodes dynamic route segments', async () => {
    api.get.mockResolvedValue({ data: { id: 'job/1' } });
    api.post.mockResolvedValue({ data: { success: true } });

    await slskdn.triggerMeshSync('alice/bob');
    await slskdn.getSwarmJob('job/1');

    expect(api.post).toHaveBeenCalledWith('/mesh/sync/alice%2Fbob');
    expect(api.get).toHaveBeenCalledWith('/multisource/jobs/job%2F1');
  });
});
