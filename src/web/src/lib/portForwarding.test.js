// <copyright file="portForwarding.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as portForwarding from './portForwarding';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../config', () => ({
  urlBase: '',
}));

vi.mock('./session', () => ({
  authHeaders: vi.fn(() => ({ Authorization: 'Bearer token' })),
}));

describe('port forwarding api', () => {
  beforeEach(() => {
    global.fetch = vi.fn().mockResolvedValue({
      json: vi.fn().mockResolvedValue({ availablePorts: [] }),
      ok: true,
    });
  });

  it('requests a bounded available-port preview when a limit is supplied', async () => {
    await portForwarding.getAvailablePorts(1_024, 65_535, 100);

    expect(fetch).toHaveBeenCalledWith(
      '/api/v0/port-forwarding/available-ports?endPort=65535&startPort=1024&limit=100',
      expect.any(Object),
    );
  });
});
