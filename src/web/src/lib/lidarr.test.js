// <copyright file="lidarr.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';
import * as lidarr from './lidarr';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

describe('lidarr', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('dedupes concurrent status requests and reuses the bounded cache', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-07-16T00:00:00Z'));
    api.get
      .mockResolvedValueOnce({ data: { appName: 'Lidarr', version: '2.0.0' } })
      .mockResolvedValueOnce({ data: { appName: 'Lidarr', version: '2.1.0' } });

    const [first, second] = await Promise.all([
      lidarr.getStatus(),
      lidarr.getStatus(),
    ]);
    const cached = await lidarr.getStatus();

    expect(api.get).toHaveBeenCalledTimes(1);
    expect(first).toBe(second);
    expect(cached).toBe(first);

    await vi.advanceTimersByTimeAsync(15_001);

    await expect(lidarr.getStatus()).resolves.toEqual({
      appName: 'Lidarr',
      version: '2.1.0',
    });
    expect(api.get).toHaveBeenCalledTimes(2);
  });
});
