// <copyright file="pods.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import * as pods from './pods';

vi.mock('../config', () => ({
  urlBase: '',
}));

vi.mock('./session', () => ({
  authHeaders: vi.fn(() => ({ Authorization: 'Bearer token' })),
}));

const mockJsonResponse = (data) => ({
  json: vi.fn().mockResolvedValue(data),
  ok: true,
});

describe('pods api', () => {
  beforeEach(() => {
    global.fetch = vi.fn();
  });

  it('encodes pod and channel path segments', async () => {
    fetch.mockResolvedValue(mockJsonResponse([]));

    await pods.get('pod/a?b');
    await pods.getMessages('pod/a?b', 'general/#1');

    expect(fetch).toHaveBeenNthCalledWith(
      1,
      '/api/v0/pods/pod%2Fa%3Fb',
      expect.any(Object),
    );
    expect(fetch).toHaveBeenNthCalledWith(
      2,
      '/api/v0/pods/pod%2Fa%3Fb/channels/general%2F%231/messages',
      expect.any(Object),
    );
  });

  it('rejects malformed pod discovery list payloads', async () => {
    fetch.mockResolvedValue(mockJsonResponse({ pods: { podId: 'bad' } }));

    await expect(pods.discoverAll()).resolves.toEqual([]);
  });
});
