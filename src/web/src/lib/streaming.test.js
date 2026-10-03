// <copyright file="streaming.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';
import * as streaming from './streaming';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

describe('streaming', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('exchanges a share token for a ticket via the X-Share-Token header, not the URL', async () => {
    api.post.mockResolvedValue({ data: { ticket: 'opaque-ticket' } });

    const ticket = await streaming.createShareStreamTicket('content/1', 'secret-token');

    expect(ticket).toBe('opaque-ticket');
    expect(api.post).toHaveBeenCalledWith(
      '/streams/content%2F1/share-ticket',
      undefined,
      { headers: { 'X-Share-Token': 'secret-token' } },
    );
    // The secret must never appear in the request URL (arg 0).
    expect(api.post.mock.calls[0][0]).not.toContain('secret-token');
  });

  it('returns an empty string when the server does not return a ticket', async () => {
    api.post.mockResolvedValue({ data: {} });

    const ticket = await streaming.createShareStreamTicket('c1', 't');

    expect(ticket).toBe('');
  });

  it('exchanges a remote share token in a header and returns only a ticket URL', async () => {
    const fetch = vi.fn().mockResolvedValue({
      json: vi.fn().mockResolvedValue({ ticket: 'short-lived-ticket' }),
      ok: true,
    });
    vi.stubGlobal('fetch', fetch);

    const streamUrl = await streaming.createRemoteShareStreamUrl(
      'https://owner.example/slskd/api/v0/streams/sha256%3Atrack?token=long-lived-secret',
      'sha256:track',
      'long-lived-secret',
    );

    expect(fetch).toHaveBeenCalledWith(
      'https://owner.example/slskd/api/v0/streams/sha256%3Atrack/share-ticket',
      {
        cache: 'no-store',
        credentials: 'omit',
        headers: { 'X-Share-Token': 'long-lived-secret' },
        method: 'POST',
        mode: 'cors',
        redirect: 'error',
      },
    );
    expect(streamUrl).toBe(
      'https://owner.example/slskd/api/v0/streams/sha256%3Atrack?ticket=short-lived-ticket',
    );
    expect(streamUrl).not.toContain('long-lived-secret');
    expect(fetch.mock.calls[0][0]).not.toContain('long-lived-secret');
  });

  it('rejects malformed manifest URLs and unsuccessful ticket exchanges', async () => {
    await expect(
      streaming.createRemoteShareStreamUrl(
        'https://owner.example/other/path?token=secret',
        'sha256:track',
        'secret',
      ),
    ).rejects.toThrow('unsupported stream address');

    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false, status: 403 }));
    await expect(
      streaming.createRemoteShareStreamUrl(
        'https://owner.example/api/v0/streams/sha256%3Atrack?token=secret',
        'sha256:track',
        'secret',
      ),
    ).rejects.toThrow('HTTP 403');
  });

  it('passes cancellation to stream-ticket and playback-info requests', async () => {
    const controller = new AbortController();
    api.post.mockResolvedValue({ data: { ticket: 'opaque-ticket' } });
    api.get.mockResolvedValue({ data: { durationSeconds: 120 } });

    await streaming.createStreamTicket('content/1', controller.signal);
    await streaming.getPlaybackInfo('content/1', controller.signal);

    expect(api.post).toHaveBeenCalledWith(
      '/streams/content%2F1/ticket',
      undefined,
      { signal: controller.signal },
    );
    expect(api.get).toHaveBeenCalledWith(
      '/streams/content%2F1/playback-info',
      { signal: controller.signal },
    );
  });
});
