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
});
