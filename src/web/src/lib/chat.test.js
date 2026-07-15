// <copyright file="chat.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';
import * as chat from './chat';

vi.mock('./api', () => ({
  __esModule: true,
  default: {
    get: vi.fn(),
  },
}));

describe('chat api', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns only an explicit true activity response', async () => {
    api.get.mockResolvedValueOnce({ data: true });

    await expect(chat.hasUnAcknowledgedMessages()).resolves.toBe(true);
    expect(api.get).toHaveBeenCalledWith('/conversations/activity/unacknowledged');

    api.get.mockResolvedValueOnce({ data: { unexpected: true } });
    await expect(chat.hasUnAcknowledgedMessages()).resolves.toBe(false);
  });
});
